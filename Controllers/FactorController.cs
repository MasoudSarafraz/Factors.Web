using Factors.Web.Data;
using Factors.Web.Models.Entities;
using Factors.Web.Models.ViewModels;
using Factors.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Factors.Web.Controllers;

[Authorize]
public class FactorController : Controller
{
    private readonly AppDbContext _context;
    private readonly IReportService _reportService;
    private readonly IReportTemplateService _reportTemplateService;
    private readonly ILogger<FactorController> _logger;

    public FactorController(AppDbContext context, IReportService reportService, IReportTemplateService reportTemplateService, ILogger<FactorController> logger)
    {
        _context = context;
        _reportService = reportService;
        _reportTemplateService = reportTemplateService;
        _logger = logger;
    }

    public async Task<IActionResult> Index(string? search, string? fromDateJalali, string? toDateJalali)
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

        var factorList = await query
            .OrderByDescending(f => f.CreateDate)
            .ToListAsync();

        var factors = factorList.Select(f => new FactorViewModel
        {
            Id = f.Id,
            PersonId = f.PersonId,
            PersonName = f.Person?.PersonName ?? "",
            PersianCreateDate = PersianDateService.ToPersian(f.CreateDate),
            // Only sum non-child items to avoid double-counting pack items
            TotalAmount = f.FactorItems?.Where(fi => !fi.ParentId.HasValue).Sum(fi => fi.Price * fi.Qty) ?? 0,
            TotalItems = f.FactorItems?.Count ?? 0
        }).ToList();

        var model = new FactorListViewModel
        {
            Factors = factors,
            SearchTerm = search ?? "",
            FromDateJalali = fromDateJalali ?? "",
            ToDateJalali = toDateJalali ?? ""
        };

        // Load print templates for the dropdown
        ViewBag.PrintTemplates = await SettingsController.GetPrintTemplatesAsync(_context);

        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        ViewBag.Persons = await _context.Persons
            .OrderBy(p => p.PersonName)
            .Select(p => new { p.Id, p.PersonName, p.IsIndividual })
            .ToListAsync();

        ViewBag.Products = await _context.Products
            .Include(p => p.Category)
            .OrderBy(p => p.Name)
            .Select(p => new { p.Id, p.Name, p.Code, CategoryName = p.Category!.Name })
            .ToListAsync();

        ViewBag.Packs = await _context.ProductPacks
            .Include(p => p.PackItems)
                .ThenInclude(pi => pi.Product)
            .OrderBy(p => p.PackName)
            .Select(p => new
            {
                p.Id,
                p.PackName,
                p.PackCode,
                Items = p.PackItems.Select(pi => new
                {
                    pi.ProductId,
                    ProductName = pi.Product!.Name,
                    pi.Qty,
                    pi.Price
                })
            })
            .ToListAsync();

        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateFactor([FromBody] FactorCreateViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return Json(new { success = false, message = "لطفاً تمام فیلدهای الزامی را پر کنید" });
        }

        if (model.Items == null || !model.Items.Any())
        {
            return Json(new { success = false, message = "فاکتور باید حداقل یک آیتم داشته باشد" });
        }

        using var transaction = await _context.Database.BeginTransactionAsync();

        try
        {
            var factor = new Factor
            {
                PersonId = model.PersonId,
                CreateDate = DateTime.UtcNow
            };

            _context.Factors.Add(factor);
            await _context.SaveChangesAsync();

            foreach (var item in model.Items)
            {
                var factorItem = new FactorItems
                {
                    FactorId = factor.Id,
                    SalableId = item.SalableId,
                    PackId = item.PackId,
                    ParentId = item.ParentId,
                    Qty = item.Qty,
                    Price = item.Price
                };

                _context.FactorItems.Add(factorItem);
            }

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            return Json(new { success = true, factorId = factor.Id, message = "فاکتور با موفقیت ثبت شد" });
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            return Json(new { success = false, message = "خطا در ثبت فاکتور: " + ex.Message });
        }
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        var factor = await _context.Factors
            .Include(f => f.Person)
            .Include(f => f.FactorItems)
            .FirstOrDefaultAsync(f => f.Id == id);

        if (factor == null)
            return NotFound();

        // Resolve product names for items
        var productIds = factor.FactorItems
            .Where(fi => fi.SalableId.HasValue)
            .Select(fi => fi.SalableId!.Value)
            .Distinct()
            .ToList();

        var packIds = factor.FactorItems
            .Where(fi => fi.PackId.HasValue)
            .Select(fi => fi.PackId!.Value)
            .Distinct()
            .ToList();

        var products = await _context.Products
            .Where(p => productIds.Contains(p.Id))
            .ToDictionaryAsync(p => p.Id, p => p.Name);

        var packs = await _context.ProductPacks
            .Where(p => packIds.Contains(p.Id))
            .ToDictionaryAsync(p => p.Id, p => p.PackName);

        // Only sum non-child items to avoid double-counting pack items
        var totalAmount = factor.FactorItems
            .Where(fi => !fi.ParentId.HasValue)
            .Sum(fi => fi.Price * fi.Qty);

        var model = new FactorViewModel
        {
            Id = factor.Id,
            PersonId = factor.PersonId,
            PersonName = factor.Person?.PersonName ?? "",
            PersianCreateDate = PersianDateService.ToPersian(factor.CreateDate, true),
            TotalAmount = totalAmount,
            TotalItems = factor.FactorItems.Count,
            Items = factor.FactorItems.Select(fi => new FactorItemViewModel
            {
                Id = fi.Id,
                SalableId = fi.SalableId,
                PackId = fi.PackId,
                ParentId = fi.ParentId,
                Qty = fi.Qty,
                Price = fi.Price,
                ProductName = fi.SalableId.HasValue
                    ? products.GetValueOrDefault(fi.SalableId.Value, "")
                    : fi.PackId.HasValue
                        ? packs.GetValueOrDefault(fi.PackId.Value, "")
                        : ""
            }).ToList()
        };

        // Load print templates for the dropdown
        ViewBag.PrintTemplates = await SettingsController.GetPrintTemplatesAsync(_context);

        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Print(int id, int? templateId)
    {
        var factor = await _context.Factors
            .Include(f => f.Person)
            .Include(f => f.FactorItems)
            .FirstOrDefaultAsync(f => f.Id == id);

        if (factor == null)
            return NotFound();

        var productIds = factor.FactorItems
            .Where(fi => fi.SalableId.HasValue)
            .Select(fi => fi.SalableId!.Value)
            .Distinct()
            .ToList();

        var packIds = factor.FactorItems
            .Where(fi => fi.PackId.HasValue)
            .Select(fi => fi.PackId!.Value)
            .Distinct()
            .ToList();

        var products = await _context.Products
            .Where(p => productIds.Contains(p.Id))
            .ToDictionaryAsync(p => p.Id, p => p.Name);

        var packs = await _context.ProductPacks
            .Where(p => packIds.Contains(p.Id))
            .ToDictionaryAsync(p => p.Id, p => p.PackName);

        // Only sum non-child items to avoid double-counting pack items
        var totalAmount = factor.FactorItems
            .Where(fi => !fi.ParentId.HasValue)
            .Sum(fi => fi.Price * fi.Qty);

        var model = new FactorViewModel
        {
            Id = factor.Id,
            PersonId = factor.PersonId,
            PersonName = factor.Person?.PersonName ?? "",
            PersianCreateDate = PersianDateService.ToPersian(factor.CreateDate, true),
            TotalAmount = totalAmount,
            TotalItems = factor.FactorItems.Count,
            Items = factor.FactorItems.Select(fi => new FactorItemViewModel
            {
                Id = fi.Id,
                SalableId = fi.SalableId,
                PackId = fi.PackId,
                ParentId = fi.ParentId,
                Qty = fi.Qty,
                Price = fi.Price,
                ProductName = fi.SalableId.HasValue
                    ? products.GetValueOrDefault(fi.SalableId.Value, "")
                    : fi.PackId.HasValue
                        ? packs.GetValueOrDefault(fi.PackId.Value, "")
                        : ""
            }).ToList()
        };

        // Determine which template to use
        int? effectiveTemplateId = templateId;

        // If no specific template requested, use the first configured template (or old default)
        if (!effectiveTemplateId.HasValue)
        {
            var configuredIds = await SettingsController.GetSettingIntListAsync(_context, "FactorPrintTemplateIds");
            if (configuredIds.Any())
            {
                effectiveTemplateId = configuredIds.First();
            }
            else
            {
                // Fallback to old key
                effectiveTemplateId = await SettingsController.GetSettingIntAsync(_context, "DefaultFactorPrintTemplateId");
            }
        }

        // If a template is selected (and it's not the PDF option which is Id=0)
        if (effectiveTemplateId.HasValue && effectiveTemplateId.Value > 0)
        {
            try
            {
                var template = await _context.ReportTemplates.FindAsync(effectiveTemplateId.Value);
                if (template != null)
                {
                    // Generate Word report from template
                    var docBytes = await _reportTemplateService.GenerateReportAsync(effectiveTemplateId.Value, id);
                    return File(docBytes, "application/vnd.openxmlformats-officedocument.wordprocessingml.document", $"Factor-{id}.docx");
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to generate report from template {TemplateId}, falling back to default PDF", effectiveTemplateId.Value);
            }
        }

        // Fallback to default PDF
        var pdfBytes = _reportService.GenerateFactorReportPdf(model);
        return File(pdfBytes, "application/pdf", $"Factor-{id}.pdf");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> Delete(int id)
    {
        var factor = await _context.Factors
            .Include(f => f.FactorItems)
            .FirstOrDefaultAsync(f => f.Id == id);

        if (factor == null)
            return NotFound();

        _context.FactorItems.RemoveRange(factor.FactorItems);
        _context.Factors.Remove(factor);
        await _context.SaveChangesAsync();

        TempData["Success"] = "فاکتور با موفقیت حذف شد";
        return RedirectToAction("Index");
    }

    // API endpoints for AJAX
    [HttpGet]
    public async Task<IActionResult> GetProductCurrentPrice(int productId)
    {
        var now = DateTime.UtcNow;
        var price = await _context.ProductPrices
            .Where(pp => pp.ProductId == productId && pp.StartTime <= now && pp.EndTime >= now)
            .OrderByDescending(pp => pp.CreateDate)
            .FirstOrDefaultAsync();

        return Json(new { price = price?.Price ?? 0 });
    }

    [HttpGet]
    public async Task<IActionResult> GetPackInfo(int packId)
    {
        var pack = await _context.ProductPacks
            .Include(p => p.PackItems)
                .ThenInclude(pi => pi.Product)
            .FirstOrDefaultAsync(p => p.Id == packId);

        if (pack == null)
            return NotFound();

        return Json(new
        {
            id = pack.Id,
            packName = pack.PackName,
            packCode = pack.PackCode,
            items = pack.PackItems.Select(pi => new
            {
                productId = pi.ProductId,
                productName = pi.Product!.Name,
                qty = pi.Qty,
                price = pi.Price
            })
        });
    }
}
