using Factors.Web.Data;
using Factors.Web.Models.Entities;
using Factors.Web.Models.ViewModels;
using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using System.Linq;

namespace Factors.Web.Services;

public interface IReportTemplateService
{
    Task<ReportTemplate> UploadTemplateAsync(string name, string? description, IFormFile file, ReportTemplateType templateType);
    List<ExtractedMarker> ExtractMarkers(string filePath);
    Task SaveMarkerMappingsAsync(SaveMarkerMappingViewModel model);
    Task<MarkerMappingViewModel> GetMarkerMappingAsync(int templateId);
    Task<byte[]> GenerateReportAsync(int templateId, int factorId);
    Task<byte[]> GenerateBatchReportAsync(int templateId, List<int> factorIds);
    Task<ReportTemplateListViewModel> GetTemplatesAsync();
    Task DeleteTemplateAsync(int templateId);
    Task<List<TemplateInfo>> GetAvailableTemplatesAsync(ReportTemplateType? templateType = null);
    Task<List<FactorSearchResultItem>> SearchFactorsAsync(string? search, string? fromDateJalali, string? toDateJalali, int? personId);
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

    public async Task<ReportTemplate> UploadTemplateAsync(string name, string? description, IFormFile file, ReportTemplateType templateType)
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
            TemplateType = templateType,
            CreateDate = DateTime.UtcNow
        };

        _context.ReportTemplates.Add(template);
        await _context.SaveChangesAsync();

        foreach (var marker in extractedMarkers)
        {
            MarkerDataSource dataSource;
            string? propertyPath = null;

            if (marker.DataType == MarkerDataType.List)
            {
                dataSource = templateType switch
                {
                    ReportTemplateType.ProductList => MarkerDataSource.Product,
                    ReportTemplateType.PersonList => MarkerDataSource.Person,
                    _ => MarkerDataSource.FactorItem
                };
                propertyPath = null;
            }
            else if (marker.ParentListMarker != null)
            {
                dataSource = templateType switch
                {
                    ReportTemplateType.ProductList => MarkerDataSource.Product,
                    ReportTemplateType.PersonList => MarkerDataSource.Person,
                    _ => MarkerDataSource.FactorItem
                };
            }
            else
            {
                dataSource = templateType switch
                {
                    ReportTemplateType.ProductList => MarkerDataSource.Product,
                    ReportTemplateType.PersonList => MarkerDataSource.Person,
                    _ => MarkerDataSource.Factor
                };
            }

            var autoMap = AvailableProperties.TryAutoMap(marker.Name);
            if (autoMap.HasValue && marker.DataType != MarkerDataType.List)
            {
                dataSource = autoMap.Value.DataSource;
                propertyPath = autoMap.Value.PropertyPath;
            }

            var templateMarker = new ReportTemplateMarker
            {
                TemplateId = template.Id,
                MarkerName = marker.Name,
                DataType = marker.DataType,
                DataSource = dataSource,
                PropertyPath = propertyPath,
                ParentListMarker = marker.ParentListMarker
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
                if (marker.DataType == MarkerDataType.List) continue;

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

        var allMarkers = template.Markers.ToList();

        var groups = new List<MarkerMappingGroup>();

        var listMarkers = allMarkers.Where(m => m.DataType == MarkerDataType.List).ToList();
        foreach (var listMarker in listMarkers)
        {
            var childMarkers = allMarkers
                .Where(m => m.ParentListMarker == listMarker.MarkerName)
                .Select(m => new MarkerMappingItem
                {
                    MarkerId = m.Id,
                    MarkerName = m.MarkerName,
                    DataType = m.DataType,
                    ParentListMarker = m.ParentListMarker,
                    DataSource = m.DataSource,
                    PropertyPath = m.PropertyPath,
                    AvailableProperties = AvailableProperties.GetPropertiesForDataSource(m.DataSource)
                }).ToList();

            groups.Add(new MarkerMappingGroup
            {
                MainMarker = new MarkerMappingItem
                {
                    MarkerId = listMarker.Id,
                    MarkerName = listMarker.MarkerName,
                    DataType = listMarker.DataType,
                    DataSource = listMarker.DataSource,
                    PropertyPath = listMarker.PropertyPath,
                    AvailableProperties = new List<PropertyOption>()
                },
                ChildMarkers = childMarkers
            });
        }

        var standaloneMarkers = allMarkers
            .Where(m => m.DataType == MarkerDataType.Single && m.ParentListMarker == null)
            .ToList();

        foreach (var marker in standaloneMarkers)
        {
            groups.Add(new MarkerMappingGroup
            {
                MainMarker = new MarkerMappingItem
                {
                    MarkerId = marker.Id,
                    MarkerName = marker.MarkerName,
                    DataType = marker.DataType,
                    DataSource = marker.DataSource,
                    PropertyPath = marker.PropertyPath,
                    AvailableProperties = AvailableProperties.GetPropertiesForDataSource(marker.DataSource)
                },
                ChildMarkers = new List<MarkerMappingItem>()
            });
        }

        return new MarkerMappingViewModel
        {
            TemplateId = template.Id,
            TemplateName = template.Name,
            TemplateType = template.TemplateType,
            Groups = groups
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
                    .ThenInclude(p => p!.Category)
            .Include(f => f.FactorItems)
                .ThenInclude(fi => fi.ProductPack)
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

            // مرحله ۱: ابتدا مارکرهای لیستی و کلون سطرهای جدول
            ProcessItemTables(body, factorItemData, listMarkerNames, factorData);

            // مرحله ۲: حذف پاراگراف‌های مارکر لیستی
            RemoveListMarkerParagraphs(body, listMarkerNames);

            // مرحله ۳: جایگزینی مارکرهای تکی در کل سند (شامل هدر و فوتر)
            var mainPart = doc.MainDocumentPart!;
            ReplaceAllMarkersInPart(mainPart, factorData);

            mainPart.Document.Save();
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

    /// <summary>
    /// جایگزینی مارکرها در تمام بخش‌های سند (بدنه، هدر، فوتر)
    /// </summary>
    private void ReplaceAllMarkersInPart(MainDocumentPart mainPart, Dictionary<string, string> replacements)
    {
        // بدنه اصلی
        foreach (var para in mainPart.Document.Body!.Descendants<Paragraph>())
        {
            ReplaceMarkersInParagraph(para, replacements);
        }

        // هدرها و فوترها
        foreach (var headerPart in mainPart.HeaderParts)
        {
            foreach (var para in headerPart.Header.Descendants<Paragraph>())
            {
                ReplaceMarkersInParagraph(para, replacements);
            }
        }

        foreach (var footerPart in mainPart.FooterParts)
        {
            foreach (var para in footerPart.Footer.Descendants<Paragraph>())
            {
                ReplaceMarkersInParagraph(para, replacements);
            }
        }
    }

    private Dictionary<string, string> BuildFactorData(Factor factor)
    {
        var taxRate = 0.09m;
        var totalAmount = factor.FactorItems?.Sum(fi => fi.Price * fi.Qty) ?? 0;
        var taxAmount = totalAmount * taxRate;
        var totalWithTax = totalAmount + taxAmount;
        var totalQty = factor.FactorItems?.Sum(fi => fi.Qty) ?? 0;
        var personType = factor.Person?.IsIndividual == true ? "حقیقی" : (factor.Person?.IsIndividual == false ? "حقوقی" : "");

        return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["Id"] = factor.Id.ToString(),
            ["Invoicenumber"] = factor.Id.ToString(),

            ["PersianCreateDate"] = PersianDateService.ToPersian(factor.CreateDate, true),
            ["date"] = PersianDateService.ToPersian(factor.CreateDate),
            ["CreateDate"] = factor.CreateDate.ToString("yyyy/MM/dd"),

            ["PersonName"] = factor.Person?.PersonName ?? "",
            ["Buyerdetails"] = factor.Person?.PersonName ?? "",
            ["PersonType"] = personType,
            ["PersonId"] = factor.PersonId.ToString(),

            ["TotalAmount"] = totalAmount.ToString("N0"),
            ["sum"] = totalAmount.ToString("N0"),
            ["Totalup"] = totalAmount.ToString("N0"),
            ["TaxAmount"] = taxAmount.ToString("N0"),
            ["tax"] = taxAmount.ToString("N0"),
            ["TaxPercent"] = "9",
            ["TotalWithTax"] = totalWithTax.ToString("N0"),
            ["Totalsum"] = totalWithTax.ToString("N0"),

            ["TotalItems"] = (factor.FactorItems?.Count ?? 0).ToString(),
            ["TotalQuantity"] = totalQty.ToString("N0"),
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
            var productCode = item.Product?.Code ?? "";
            var categoryName = item.Product?.Category?.Name ?? "";
            var isPack = item.PackId.HasValue;
            var packName = item.ProductPack?.PackName ?? "";

            result.Add(new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["ProductName"] = productName,
                ["Descriptionofthepiece"] = productName,
                ["ProductCode"] = productCode,
                ["CategoryName"] = categoryName,

                ["Qty"] = item.Qty.ToString("N0"),
                ["PartQty"] = item.Qty.ToString("N0"),

                ["Price"] = item.Price.ToString("N0"),
                ["price"] = item.Price.ToString("N0"),
                ["TotalPrice"] = (item.Price * item.Qty).ToString("N0"),
                ["total"] = (item.Price * item.Qty).ToString("N0"),

                ["RowNumber"] = (i + 1).ToString(),

                ["IsPack"] = isPack ? "بله" : "خیر",
                ["PackName"] = packName,
                ["PackId"] = item.PackId?.ToString() ?? "",

                ["ItemId"] = item.Id.ToString(),
                ["SalableId"] = item.SalableId?.ToString() ?? "",
            });
        }

        return result;
    }

    /// <summary>
    /// پردازش جدول‌های آیتم - کلون سطر داده برای هر آیتم فاکتور
    /// </summary>
    private void ProcessItemTables(Body body, List<Dictionary<string, string>> factorItemData, List<string> listMarkerNames, Dictionary<string, string> factorData)
    {
        var itemMarkerNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "ProductName", "Qty", "Price", "TotalPrice", "RowNumber",
            "Descriptionofthepiece", "PartQty", "price", "total",
            "ProductCode", "CategoryName", "IsPack", "PackName"
        };

        var tables = body.Elements<Table>().ToList();

        foreach (var table in tables)
        {
            var rows = table.Elements<TableRow>().ToList();
            if (rows.Count < 2) continue;

            TableRow? dataRow = null;

            for (int r = rows.Count - 1; r >= 1; r--)
            {
                var rowText = rows[r].InnerText ?? "";
                if (MarkerRegex.IsMatch(rowText))
                {
                    dataRow = rows[r];
                    break;
                }
            }

            if (dataRow == null) continue;

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

            var templateRow = (TableRow)dataRow.CloneNode(true);

            dataRow.Remove();

            if (factorItemData.Count == 0)
            {
                var emptyRow = (TableRow)templateRow.CloneNode(true);
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
    /// فقط مارکرها رو خالی می‌کنه تا ساختار و فاصله‌های پاراگراف حفظ بشه
    /// </summary>
    private void RemoveListMarkerParagraphs(Body body, List<string> listMarkerNames)
    {
        if (listMarkerNames.Count == 0) return;

        foreach (var para in body.Elements<Paragraph>().ToList())
        {
            var text = (para.InnerText ?? "").Trim();
            foreach (var markerName in listMarkerNames)
            {
                if (text.Equals($"{{{markerName}}}", StringComparison.OrdinalIgnoreCase))
                {
                    ClearAllText(para);
                    break;
                }
            }
        }
    }

    /// <summary>
    /// جایگزینی تمام مارکرها در کل سند (فقط بدنه)
    /// </summary>
    private void ReplaceAllMarkers(Body body, Dictionary<string, string> replacements)
    {
        foreach (var para in body.Descendants<Paragraph>())
        {
            ReplaceMarkersInParagraph(para, replacements);
        }
    }

    /// <summary>
    /// جایگزینی مارکرها در یک پاراگراف با حفظ کامل فرمت‌ها
    /// رویکرد: اول Runهای تقسیم‌شده رو ادغام کن، بعد فقط متن هر Run رو عوض کن
    /// </summary>
    private void ReplaceMarkersInParagraph(Paragraph paragraph, Dictionary<string, string> replacements)
    {
        var fullText = paragraph.InnerText ?? "";
        if (!MarkerRegex.IsMatch(fullText)) return;

        // مرحله ۱: ادغام Runهایی که یک مارکر رو تقسیم کردن
        MergeSplitMarkerRuns(paragraph);

        // مرحله ۲: جایگزینی مارکرها در هر Run به صورت مستقل (حفظ فرمت Run)
        foreach (var run in paragraph.Elements<Run>().ToList())
        {
            var runText = run.InnerText;
            if (string.IsNullOrEmpty(runText) || !MarkerRegex.IsMatch(runText)) continue;

            var replacedText = runText;
            foreach (var kvp in replacements)
            {
                var pattern = @"\{" + Regex.Escape(kvp.Key) + @"\}";
                replacedText = Regex.Replace(replacedText, pattern, kvp.Value ?? "", RegexOptions.IgnoreCase);
            }

            if (replacedText == runText) continue;

            // فقط متن‌های داخل Run رو عوض کن - فرمت Run حفظ میشه
            SetRunText(run, replacedText);
        }
    }

    /// <summary>
    /// تنظیم متن یک Run با حفظ کامل تمام عناصر غیرمتنی (RunProperties, Break, TabChar و غیره)
    /// </summary>
    private void SetRunText(Run run, string newText)
    {
        // حذف فقط عناصر Text از Run
        var textElements = run.Elements<Text>().ToList();
        foreach (var t in textElements)
        {
            t.Remove();
        }

        // اضافه کردن متن جدید
        var newTextNode = new Text(newText);
        newTextNode.SetAttribute(new DocumentFormat.OpenXml.OpenXmlAttribute("xml:space", "", "preserve"));

        // اگر RunProperties وجود دارد، متن را بعد از آن اضافه کن
        // در غیر این صورت، در ابتدای Run
        var runProps = run.RunProperties;
        if (runProps != null)
        {
            run.InsertAfter(newTextNode, runProps);
        }
        else
        {
            run.InsertAt(newTextNode, 0);
        }
    }

    /// <summary>
    /// ادغام Runهایی که یک مارکر رو بین خودشون تقسیم کردن
    /// مثلاً اگر Run1="{Buyer" و Run2="details}" باشند، ادغام میشن به یک Run
    /// </summary>
    private void MergeSplitMarkerRuns(Paragraph paragraph)
    {
        var runs = paragraph.Elements<Run>().ToList();
        if (runs.Count < 2) return;

        var fullText = paragraph.InnerText ?? "";
        var matches = MarkerRegex.Matches(fullText);
        if (matches.Count == 0) return;

        // ساخت نقشه کاراکتر→Run
        var charToRun = new (int runIndex, int charInRun)[fullText.Length];
        int charPos = 0;
        for (int ri = 0; ri < runs.Count; ri++)
        {
            var runText = runs[ri].InnerText ?? "";
            for (int ci = 0; ci < runText.Length; ci++)
            {
                if (charPos < fullText.Length)
                {
                    charToRun[charPos] = (ri, ci);
                    charPos++;
                }
            }
        }

        // پیدا کردن مارکرهایی که بین چند Run تقسیم شدن
        // از آخر به اول پردازش می‌کنیم تا اندیس‌ها به هم نریزه
        var mergeRanges = new List<(int startRunIdx, int endRunIdx)>();

        foreach (Match match in matches)
        {
            if (match.Index + match.Length - 1 >= charToRun.Length) continue;
            var startRunIdx = charToRun[match.Index].runIndex;
            var endRunIdx = charToRun[match.Index + match.Length - 1].runIndex;

            if (startRunIdx != endRunIdx)
            {
                mergeRanges.Add((startRunIdx, endRunIdx));
            }
        }

        // از آخر به اول ادغام کن
        mergeRanges.Sort((a, b) => b.startRunIdx.CompareTo(a.startRunIdx));

        foreach (var (startIdx, endIdx) in mergeRanges)
        {
            if (startIdx >= runs.Count || endIdx >= runs.Count) continue;

            var affectedRuns = new List<Run>();
            for (int ri = startIdx; ri <= endIdx && ri < runs.Count; ri++)
            {
                affectedRuns.Add(runs[ri]);
            }

            if (affectedRuns.Count < 2) continue;

            var mergedText = string.Concat(affectedRuns.Select(r => r.InnerText));
            var firstRun = affectedRuns[0];

            // قرار دادن متن ادغام‌شده در اولین Run
            SetRunText(firstRun, mergedText);

            // حذف Runهای اضافی (از آخر به اول)
            for (int i = affectedRuns.Count - 1; i >= 1; i--)
            {
                affectedRuns[i].Remove();
            }

            // به‌روزرسانی لیست runs
            runs = paragraph.Elements<Run>().ToList();
        }
    }

    /// <summary>
    /// پاک کردن متن یک پاراگراف (حفظ فرمت اولین Run)
    /// </summary>
    private void ClearAllText(Paragraph paragraph)
    {
        var runs = paragraph.Elements<Run>().ToList();
        if (runs.Count == 0) return;

        var firstRun = runs[0];
        SetRunText(firstRun, "");

        // حذف بقیه Runها
        foreach (var run in runs.Skip(1).ToList())
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
                TemplateType = t.TemplateType,
                MarkerCount = t.Markers?.Count ?? 0,
                MappedMarkerCount = t.Markers?.Count(m => m.DataType == MarkerDataType.List || !string.IsNullOrWhiteSpace(m.PropertyPath)) ?? 0
            }).ToList()
        };
    }

    public async Task<List<TemplateInfo>> GetAvailableTemplatesAsync(ReportTemplateType? templateType = null)
    {
        var query = _context.ReportTemplates
            .Include(t => t.Markers)
            .AsQueryable();

        if (templateType.HasValue)
        {
            query = query.Where(t => t.TemplateType == templateType.Value);
        }

        var templates = await query
            .OrderByDescending(t => t.CreateDate)
            .ToListAsync();

        return templates.Select(t => new TemplateInfo
        {
            Id = t.Id,
            Name = t.Name,
            Description = t.Description,
            TemplateType = t.TemplateType,
            MarkerCount = t.Markers?.Count ?? 0,
            MappedMarkerCount = t.Markers?.Count(m => m.DataType == MarkerDataType.List || !string.IsNullOrWhiteSpace(m.PropertyPath)) ?? 0
        }).ToList();
    }

    public async Task<List<FactorSearchResultItem>> SearchFactorsAsync(string? search, string? fromDateJalali, string? toDateJalali, int? personId)
    {
        DateTime? fromDate = PersianDateService.ParsePersianDate(fromDateJalali);
        DateTime? toDate = PersianDateService.ParsePersianDate(toDateJalali);

        var query = _context.Factors
            .Include(f => f.Person)
            .Include(f => f.FactorItems)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(f => f.Person!.PersonName.Contains(search) || f.Id.ToString() == search);
        }

        if (fromDate.HasValue)
        {
            query = query.Where(f => f.CreateDate >= fromDate.Value);
        }

        if (toDate.HasValue)
        {
            query = query.Where(f => f.CreateDate <= toDate.Value.AddDays(1));
        }

        if (personId.HasValue)
        {
            query = query.Where(f => f.PersonId == personId.Value);
        }

        var factors = await query
            .OrderByDescending(f => f.CreateDate)
            .Take(100)
            .ToListAsync();

        return factors.Select(f => new FactorSearchResultItem
        {
            Id = f.Id,
            PersonName = f.Person?.PersonName ?? "",
            PersianDate = PersianDateService.ToPersian(f.CreateDate),
            TotalAmount = f.FactorItems?.Sum(fi => fi.Price * fi.Qty) ?? 0,
            TotalItems = f.FactorItems?.Count ?? 0
        }).ToList();
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

    /// <summary>
    /// تولید گزارش گروهی - هر فاکتور یک فایل Word، خروجی ZIP
    /// </summary>
    public async Task<byte[]> GenerateBatchReportAsync(int templateId, List<int> factorIds)
    {
        var template = await _context.ReportTemplates.FindAsync(templateId);
        if (template == null) throw new ArgumentException("قالب یافت نشد");

        using var ms = new MemoryStream();
        using (var archive = new System.IO.Compression.ZipArchive(ms, System.IO.Compression.ZipArchiveMode.Create, true))
        {
            foreach (var factorId in factorIds)
            {
                try
                {
                    var docBytes = await GenerateReportAsync(templateId, factorId);
                    var entry = archive.CreateEntry($"Factor-{factorId}.docx", System.IO.Compression.CompressionLevel.Fastest);
                    using var entryStream = entry.Open();
                    await entryStream.WriteAsync(docBytes);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "خطا در تولید گزارش فاکتور {FactorId}، رد شد", factorId);
                }
            }
        }

        return ms.ToArray();
    }
}
