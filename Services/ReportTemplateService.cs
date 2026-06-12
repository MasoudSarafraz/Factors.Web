using Factors.Web.Data;
using Factors.Web.Models.Entities;
using Factors.Web.Models.ViewModels;
using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;

namespace Factors.Web.Services;

public interface IReportTemplateService
{
    Task<ReportTemplate> UploadTemplateAsync(string name, string? description, IFormFile file);
    List<ExtractedMarker> ExtractMarkers(string filePath);
    Task SaveMarkerMappingsAsync(SaveMarkerMappingViewModel model);
    Task<MarkerMappingViewModel> GetMarkerMappingAsync(int templateId);
    Task<byte[]> GenerateReportAsync(int templateId, int factorId);
    Task<ReportTemplateListViewModel> GetTemplatesAsync();
    Task DeleteTemplateAsync(int templateId);
}

public class ReportTemplateService : IReportTemplateService
{
    private readonly AppDbContext _context;
    private readonly IWebHostEnvironment _env;
    private readonly ILogger<ReportTemplateService> _logger;

    private static readonly Regex MarkerRegex = new(@"\{(\w+)\}", RegexOptions.Compiled);

    public ReportTemplateService(
        AppDbContext context,
        IWebHostEnvironment env,
        ILogger<ReportTemplateService> logger)
    {
        _context = context;
        _env = env;
        _logger = logger;
    }

    public async Task<ReportTemplate> UploadTemplateAsync(string name, string? description, IFormFile file)
    {
        var templatesDir = Path.Combine(_env.ContentRootPath, "Uploads", "Templates");
        Directory.CreateDirectory(templatesDir);

        var fileName = $"{Guid.NewGuid():N}{Path.GetExtension(file.FileName)}";
        var filePath = Path.Combine(templatesDir, fileName);

        using (var stream = new FileStream(filePath, FileMode.Create))
        {
            await file.CopyToAsync(stream);
        }

        var extractedMarkers = ExtractMarkers(filePath);

        var template = new ReportTemplate
        {
            Name = name,
            Description = description,
            FilePath = filePath,
            OriginalFileName = file.FileName,
            CreateDate = DateTime.UtcNow
        };

        _context.ReportTemplates.Add(template);
        await _context.SaveChangesAsync();

        foreach (var marker in extractedMarkers)
        {
            var templateMarker = new ReportTemplateMarker
            {
                TemplateId = template.Id,
                MarkerName = marker.Name,
                DataType = marker.DataType,
                DataSource = marker.DataType == MarkerDataType.List
                    ? MarkerDataSource.FactorItem
                    : MarkerDataSource.Factor
            };
            _context.ReportTemplateMarkers.Add(templateMarker);
        }
        await _context.SaveChangesAsync();

        return template;
    }

    public List<ExtractedMarker> ExtractMarkers(string filePath)
    {
        var markers = new List<ExtractedMarker>();
        var seenMarkers = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        using var doc = WordprocessingDocument.Open(filePath, false);
        var body = doc.MainDocumentPart?.Document.Body;
        if (body == null) return markers;

        var elements = body.Elements().ToList();

        for (int i = 0; i < elements.Count; i++)
        {
            if (elements[i] is Paragraph para)
            {
                var text = para.InnerText ?? "";
                var matches = MarkerRegex.Matches(text);

                foreach (Match match in matches)
                {
                    var markerName = match.Groups[1].Value;
                    if (seenMarkers.Contains(markerName)) continue;
                    seenMarkers.Add(markerName);

                    // آیا جدولی بلافاصله بعد از این پاراگراف وجود دارد؟
                    var isList = (i + 1 < elements.Count && elements[i + 1] is Table);

                    markers.Add(new ExtractedMarker
                    {
                        Name = markerName,
                        DataType = isList ? MarkerDataType.List : MarkerDataType.Single,
                        IsInTable = false,
                        ParentListMarker = null
                    });

                    if (isList && elements[i + 1] is Table nextTable)
                    {
                        ExtractTableMarkers(nextTable, markers, seenMarkers, markerName);
                    }
                }
            }
            else if (elements[i] is Table standaloneTable)
            {
                // جدول بدون مارکر لیستی قبلش
                ExtractTableMarkers(standaloneTable, markers, seenMarkers, null);
            }
        }

        return markers;
    }

    private void ExtractTableMarkers(Table table, List<ExtractedMarker> markers, HashSet<string> seenMarkers, string? parentListMarker)
    {
        foreach (var row in table.Elements<TableRow>())
        {
            foreach (var cell in row.Elements<TableCell>())
            {
                var cellText = cell.InnerText ?? "";
                var matches = MarkerRegex.Matches(cellText);
                foreach (Match match in matches)
                {
                    var markerName = match.Groups[1].Value;
                    if (seenMarkers.Contains(markerName)) continue;
                    seenMarkers.Add(markerName);

                    // اگر parentListMarker وجود دارد، این مارکر زیرمجموعه لیست است → Single
                    // اگر parentListMarker null است، این مارکر در جدول مستقل است → Single
                    markers.Add(new ExtractedMarker
                    {
                        Name = markerName,
                        DataType = MarkerDataType.Single,
                        IsInTable = true,
                        ParentListMarker = parentListMarker
                    });
                }
            }
        }
    }

    public async Task SaveMarkerMappingsAsync(SaveMarkerMappingViewModel model)
    {
        foreach (var mapping in model.Mappings)
        {
            var marker = await _context.ReportTemplateMarkers.FindAsync(mapping.MarkerId);
            if (marker != null)
            {
                marker.DataSource = mapping.DataSource;
                marker.PropertyPath = mapping.PropertyPath;
            }
        }
        await _context.SaveChangesAsync();
    }

    public async Task<MarkerMappingViewModel> GetMarkerMappingAsync(int templateId)
    {
        var template = await _context.ReportTemplates
            .Include(t => t.Markers)
            .FirstOrDefaultAsync(t => t.Id == templateId);

        if (template == null) throw new ArgumentException("قالب یافت نشد");

        return new MarkerMappingViewModel
        {
            TemplateId = template.Id,
            TemplateName = template.Name,
            Markers = template.Markers.Select(m => new MarkerMappingItem
            {
                MarkerId = m.Id,
                MarkerName = m.MarkerName,
                DataType = m.DataType,
                DataSource = m.DataSource,
                PropertyPath = m.PropertyPath,
                AvailableProperties = AvailableProperties.GetPropertiesForDataSource(m.DataSource)
            }).ToList()
        };
    }

    public async Task<byte[]> GenerateReportAsync(int templateId, int factorId)
    {
        var template = await _context.ReportTemplates
            .Include(t => t.Markers)
            .FirstOrDefaultAsync(t => t.Id == templateId);

        if (template == null) throw new ArgumentException("قالب یافت نشد");

        var factor = await _context.Factors
            .Include(f => f.Person)
            .Include(f => f.FactorItems)
                .ThenInclude(fi => fi.Product)
            .FirstOrDefaultAsync(f => f.Id == factorId);

        if (factor == null) throw new ArgumentException("فاکتور یافت نشد");

        var tempOutput = Path.Combine(Path.GetTempPath(), $"report_{Guid.NewGuid():N}.docx");
        File.Copy(template.FilePath, tempOutput, true);

        try
        {
            using var doc = WordprocessingDocument.Open(tempOutput, true);
            var body = doc.MainDocumentPart?.Document.Body;
            if (body == null) throw new InvalidOperationException("فایل قالب نامعتبر است");

            var factorData = BuildFactorData(factor);
            var factorItemData = BuildFactorItemData(factor);
            var listMarkerNames = template.Markers
                .Where(m => m.DataType == MarkerDataType.List)
                .Select(m => m.MarkerName)
                .ToList();

            // ⚠️ ترتیب مهم است:
            // مرحله ۱: ابتدا مارکرهای لیستی و کلون سطرهای جدول (قبل از جایگزینی مارکرهای تکی)
            ProcessItemTables(body, factorItemData, listMarkerNames, factorData);

            // مرحله ۲: حذف پاراگراف‌های مارکر لیستی (مثل {Items})
            RemoveListMarkerParagraphs(body, listMarkerNames);

            // مرحله ۳: جایگزینی مارکرهای تکی در کل سند
            ReplaceAllMarkers(body, factorData);

            doc.MainDocumentPart!.Document.Save();
        }
        catch
        {
            if (File.Exists(tempOutput)) File.Delete(tempOutput);
            throw;
        }

        var result = await File.ReadAllBytesAsync(tempOutput);
        File.Delete(tempOutput);
        return result;
    }

    private Dictionary<string, string> BuildFactorData(Factor factor)
    {
        var taxRate = 0.09m;
        var totalAmount = factor.FactorItems?.Sum(fi => fi.Price * fi.Qty) ?? 0;
        var taxAmount = totalAmount * taxRate;
        var totalWithTax = totalAmount + taxAmount;

        return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            // پراپرتی‌های استاندارد
            ["Id"] = factor.Id.ToString(),
            ["PersonName"] = factor.Person?.PersonName ?? "",
            ["PersianCreateDate"] = PersianDateService.ToPersian(factor.CreateDate, true),
            ["TotalAmount"] = totalAmount.ToString("N0"),
            ["TotalItems"] = (factor.FactorItems?.Count ?? 0).ToString(),
            ["TaxAmount"] = taxAmount.ToString("N0"),
            ["TotalWithTax"] = totalWithTax.ToString("N0"),
            // مارکرهای نمونه فایل کاربر
            ["Buyerdetails"] = factor.Person?.PersonName ?? "",
            ["date"] = PersianDateService.ToPersian(factor.CreateDate, true),
            ["Invoicenumber"] = factor.Id.ToString(),
            ["Totalup"] = totalAmount.ToString("N0"),
            ["sum"] = totalAmount.ToString("N0"),
            ["tax"] = taxAmount.ToString("N0"),
            ["Totalsum"] = totalWithTax.ToString("N0"),
        };
    }

    private List<Dictionary<string, string>> BuildFactorItemData(Factor factor)
    {
        var items = factor.FactorItems?.ToList() ?? new List<FactorItems>();
        var result = new List<Dictionary<string, string>>();

        for (int i = 0; i < items.Count; i++)
        {
            var item = items[i];
            var productName = item.Product?.Name ?? item.ProductName;

            result.Add(new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["ProductName"] = productName,
                ["Qty"] = item.Qty.ToString("N0"),
                ["Price"] = item.Price.ToString("N0"),
                ["TotalPrice"] = (item.Price * item.Qty).ToString("N0"),
                ["RowNumber"] = (i + 1).ToString(),
                // مارکرهای نمونه فایل کاربر
                ["Descriptionofthepiece"] = productName,
                ["PartQty"] = item.Qty.ToString("N0"),
                ["price"] = item.Price.ToString("N0"),
                ["total"] = (item.Price * item.Qty).ToString("N0"),
            });
        }

        return result;
    }

    /// <summary>
    /// پردازش جدول‌های آیتم - کلون سطر داده برای هر آیتم فاکتور
    /// </summary>
    private void ProcessItemTables(Body body, List<Dictionary<string, string>> factorItemData, List<string> listMarkerNames, Dictionary<string, string> factorData)
    {
        // نام مارکرهایی که مربوط به آیتم فاکتور هستند
        var itemMarkerNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "ProductName", "Qty", "Price", "TotalPrice", "RowNumber",
            "Descriptionofthepiece", "PartQty", "price", "total"
        };

        var tables = body.Elements<Table>().ToList();

        foreach (var table in tables)
        {
            var rows = table.Elements<TableRow>().ToList();
            if (rows.Count < 2) continue;

            // پیدا کردن سطر داده (آخرین سطری که حاوی مارکر باشد)
            TableRow? dataRow = null;
            int dataRowIndex = -1;

            for (int r = rows.Count - 1; r >= 1; r--)
            {
                var rowText = rows[r].InnerText ?? "";
                if (MarkerRegex.IsMatch(rowText))
                {
                    dataRow = rows[r];
                    dataRowIndex = r;
                    break;
                }
            }

            if (dataRow == null) continue;

            // بررسی آیا مارکرهای این سطر مربوط به آیتم فاکتور هستند
            var rowMarkerNames = new List<string>();
            foreach (var cell in dataRow.Elements<TableCell>())
            {
                var cellText = cell.InnerText ?? "";
                var matches = MarkerRegex.Matches(cellText);
                foreach (Match match in matches)
                {
                    rowMarkerNames.Add(match.Groups[1].Value);
                }
            }

            var isItemTable = rowMarkerNames.Any(name => itemMarkerNames.Contains(name));
            if (!isItemTable) continue;

            // کلون سطر الگو قبل از هر تغییر
            var templateRow = (TableRow)dataRow.CloneNode(true);

            // حذف سطر داده اصلی
            dataRow.Remove();

            // اگر آیتمی وجود ندارد، حداقل یک سطر خالی اضافه کن
            if (factorItemData.Count == 0)
            {
                var emptyRow = (TableRow)templateRow.CloneNode(true);
                // پاک کردن متن مارکرها
                foreach (var cell in emptyRow.Elements<TableCell>())
                {
                    foreach (var para in cell.Elements<Paragraph>())
                    {
                        ClearAllText(para);
                    }
                }
                table.AppendChild(emptyRow);
            }
            else
            {
                // اضافه کردن سطر برای هر آیتم
                for (int i = 0; i < factorItemData.Count; i++)
                {
                    var itemData = factorItemData[i];
                    itemData["RowNumber"] = (i + 1).ToString();

                    var newRow = (TableRow)templateRow.CloneNode(true);

                    foreach (var cell in newRow.Elements<TableCell>())
                    {
                        foreach (var para in cell.Elements<Paragraph>())
                        {
                            ReplaceMarkersInParagraph(para, itemData);
                        }
                    }

                    table.AppendChild(newRow);
                }
            }
        }
    }

    /// <summary>
    /// حذف پاراگراف‌های مارکر لیستی
    /// </summary>
    private void RemoveListMarkerParagraphs(Body body, List<string> listMarkerNames)
    {
        if (listMarkerNames.Count == 0) return;

        var toRemove = new List<Paragraph>();
        foreach (var para in body.Elements<Paragraph>())
        {
            var text = (para.InnerText ?? "").Trim();
            foreach (var markerName in listMarkerNames)
            {
                if (text.Equals($"{{{markerName}}}", StringComparison.OrdinalIgnoreCase))
                {
                    toRemove.Add(para);
                    break;
                }
            }
        }

        foreach (var para in toRemove)
        {
            para.Remove();
        }
    }

    /// <summary>
    /// جایگزینی تمام مارکرها در کل سند
    /// </summary>
    private void ReplaceAllMarkers(Body body, Dictionary<string, string> replacements)
    {
        // جایگزینی در تمام پاراگراف‌ها (شامل پاراگراف‌های داخل جدول)
        foreach (var para in body.Descendants<Paragraph>())
        {
            ReplaceMarkersInParagraph(para, replacements);
        }
    }

    /// <summary>
    /// جایگزینی مارکرها در یک پاراگراف - مقاوم در برابر Runهای تقسیم‌شده
    /// </summary>
    private void ReplaceMarkersInParagraph(Paragraph paragraph, Dictionary<string, string> replacements)
    {
        var fullText = paragraph.InnerText ?? "";
        if (!MarkerRegex.IsMatch(fullText)) return;

        // جمع‌آوری فرمت اولین Run برای حفظ استایل
        var firstRun = paragraph.Elements<Run>().FirstOrDefault();
        var runProperties = firstRun?.RunProperties?.CloneNode(true) as RunProperties;

        // ساخت متن جایگزین‌شده
        var replacedText = fullText;
        foreach (var kvp in replacements)
        {
            var pattern = @"\{" + Regex.Escape(kvp.Key) + @"\}";
            replacedText = Regex.Replace(replacedText, pattern, kvp.Value, RegexOptions.IgnoreCase);
        }

        // اگر تغییری نکرده، خروج
        if (replacedText == fullText) return;

        // حذف تمام Runهای قدیمی
        foreach (var run in paragraph.Elements<Run>().ToList())
        {
            run.Remove();
        }

        // اضافه کردن Run جدید با متن جایگزین‌شده
        var newRun = new Run();
        if (runProperties != null)
        {
            newRun.RunProperties = runProperties;
        }
        var newText = new Text(replacedText)
        {
            Space = SpaceProcessingModeValues.Preserve
        };
        newRun.AppendChild(newText);
        paragraph.AppendChild(newRun);
    }

    /// <summary>
    /// پاک کردن متن یک پاراگراف
    /// </summary>
    private void ClearAllText(Paragraph paragraph)
    {
        foreach (var run in paragraph.Elements<Run>().ToList())
        {
            run.Remove();
        }
    }

    public async Task<ReportTemplateListViewModel> GetTemplatesAsync()
    {
        var templates = await _context.ReportTemplates
            .Include(t => t.Markers)
            .OrderByDescending(t => t.CreateDate)
            .ToListAsync();

        return new ReportTemplateListViewModel
        {
            Templates = templates.Select(t => new ReportTemplateDetailViewModel
            {
                Id = t.Id,
                Name = t.Name,
                Description = t.Description,
                OriginalFileName = t.OriginalFileName,
                PersianCreateDate = PersianDateService.ToPersian(t.CreateDate),
                MarkerCount = t.Markers?.Count ?? 0,
                MappedMarkerCount = t.Markers?.Count(m => !string.IsNullOrWhiteSpace(m.PropertyPath)) ?? 0
            }).ToList()
        };
    }

    public async Task DeleteTemplateAsync(int templateId)
    {
        var template = await _context.ReportTemplates
            .Include(t => t.Markers)
            .FirstOrDefaultAsync(t => t.Id == templateId);

        if (template == null) return;

        if (!string.IsNullOrEmpty(template.FilePath) && File.Exists(template.FilePath))
        {
            File.Delete(template.FilePath);
        }

        _context.ReportTemplates.Remove(template);
        await _context.SaveChangesAsync();
    }
}
