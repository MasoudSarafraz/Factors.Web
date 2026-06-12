using Factors.Web.Data;
using Factors.Web.Models.Entities;
using Factors.Web.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Factors.Web.Controllers;

[Authorize]
public class ProductController : Controller
{
    private readonly AppDbContext _context;

    public ProductController(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index(string? search, int? categoryId)
    {
        var query = _context.Products
            .Include(p => p.Category)
            .Include(p => p.ProductPrices)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(p => p.Name.Contains(search) || p.Code.Contains(search));
        }

        if (categoryId.HasValue)
        {
            query = query.Where(p => p.CategoryId == categoryId.Value);
        }

        var productList = await query
            .OrderByDescending(p => p.CreateDate)
            .ToListAsync();

        var products = productList.Select(p => new ProductViewModel
        {
            Id = p.Id,
            Name = p.Name,
            Code = p.Code,
            CategoryId = p.CategoryId,
            CategoryName = p.Category?.Name ?? "",
            PersianCreateDate = PersianDateService.ToPersian(p.CreateDate),
            Prices = p.ProductPrices.Select(pp => new ProductPriceViewModel
            {
                Id = pp.Id,
                Price = pp.Price,
                StartTime = PersianDateService.ToPersian(pp.StartTime),
                EndTime = PersianDateService.ToPersian(pp.EndTime),
                ProductId = pp.ProductId
            }).ToList()
        }).ToList();

        var categories = await _context.ProductCategories
            .OrderBy(c => c.Name)
            .Select(c => new CategoryViewModel { Id = c.Id, Name = c.Name })
            .ToListAsync();

        var model = new ProductListViewModel
        {
            Products = products,
            Categories = categories,
            SearchTerm = search ?? "",
            FilterCategoryId = categoryId
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ProductViewModel model)
    {
        if (!ModelState.IsValid)
        {
            TempData["Error"] = "لطفاً تمام فیلدهای الزامی را پر کنید";
            return RedirectToAction("Index");
        }

        // Check duplicate code
        var existsCode = await _context.Products.AnyAsync(p => p.Code == model.Code.Trim());
        if (existsCode)
        {
            TempData["Error"] = $"محصولی با کد '{model.Code}' قبلاً ثبت شده است";
            return RedirectToAction("Index");
        }

        var product = new Product
        {
            Name = model.Name.Trim(),
            Code = model.Code.Trim(),
            CategoryId = model.CategoryId,
            CreateDate = DateTime.UtcNow
        };

        _context.Products.Add(product);
        await _context.SaveChangesAsync();

        // Save initial price if provided
        if (model.InitialPrice.HasValue && model.InitialPrice.Value > 0)
        {
            var price = new ProductPrice
            {
                ProductId = product.Id,
                Price = model.InitialPrice.Value,
                StartTime = DateTime.UtcNow,
                EndTime = DateTime.UtcNow.AddYears(1),
                CreateDate = DateTime.UtcNow
            };
            _context.ProductPrices.Add(price);
            await _context.SaveChangesAsync();
        }

        TempData["Success"] = "محصول با موفقیت ثبت شد";
        return RedirectToAction("Index");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(ProductViewModel model)
    {
        if (!ModelState.IsValid)
        {
            TempData["Error"] = "لطفاً تمام فیلدهای الزامی را پر کنید";
            return RedirectToAction("Index");
        }

        var product = await _context.Products.FindAsync(model.Id);
        if (product == null)
            return NotFound();

        // Check duplicate code (exclude self)
        var existsCode = await _context.Products.AnyAsync(p => p.Code == model.Code.Trim() && p.Id != model.Id);
        if (existsCode)
        {
            TempData["Error"] = "کد محصول تکراری است";
            return RedirectToAction("Index");
        }

        product.Name = model.Name.Trim();
        product.Code = model.Code.Trim();
        product.CategoryId = model.CategoryId;
        await _context.SaveChangesAsync();

        TempData["Success"] = "محصول با موفقیت ویرایش شد";
        return RedirectToAction("Index");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> Delete(int id)
    {
        var product = await _context.Products
            .Include(p => p.FactorItems)
            .Include(p => p.PackItems)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (product == null)
            return NotFound();

        if (product.FactorItems.Any() || product.PackItems.Any())
        {
            TempData["Error"] = "این محصول در فاکتور یا بسته استفاده شده و قابل حذف نیست";
            return RedirectToAction("Index");
        }

        _context.Products.Remove(product);
        await _context.SaveChangesAsync();

        TempData["Success"] = "محصول با موفقیت حذف شد";
        return RedirectToAction("Index");
    }

    // Product Price Management
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddPrice(ProductPriceViewModel model)
    {
        if (!ModelState.IsValid)
        {
            TempData["Error"] = "لطفاً تمام فیلدها را پر کنید";
            return RedirectToAction("Index");
        }

        var startDate = PersianDateService.ParsePersianDate(model.StartTime) ?? DateTime.UtcNow;
        var endDate = PersianDateService.ParsePersianDate(model.EndTime) ?? DateTime.UtcNow.AddMonths(6);

        var price = new ProductPrice
        {
            ProductId = model.ProductId,
            Price = model.Price,
            StartTime = startDate,
            EndTime = endDate,
            CreateDate = DateTime.UtcNow
        };

        _context.ProductPrices.Add(price);
        await _context.SaveChangesAsync();

        TempData["Success"] = "قیمت با موفقیت ثبت شد";
        return RedirectToAction("Index");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> DeletePrice(int id)
    {
        var price = await _context.ProductPrices.FindAsync(id);
        if (price == null)
            return NotFound();

        _context.ProductPrices.Remove(price);
        await _context.SaveChangesAsync();

        TempData["Success"] = "قیمت با موفقیت حذف شد";
        return RedirectToAction("Index");
    }

    [HttpGet]
    public async Task<IActionResult> GetById(int id)
    {
        var product = await _context.Products
            .Include(p => p.Category)
            .Include(p => p.ProductPrices)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (product == null)
            return NotFound();

        // Get current active price
        var now = DateTime.UtcNow;
        var currentPrice = product.ProductPrices
            .Where(pp => pp.StartTime <= now && pp.EndTime >= now)
            .OrderByDescending(pp => pp.CreateDate)
            .FirstOrDefault();

        return Json(new
        {
            id = product.Id,
            name = product.Name,
            code = product.Code,
            categoryId = product.CategoryId,
            categoryName = product.Category?.Name,
            currentPrice = currentPrice?.Price ?? 0,
            prices = product.ProductPrices.Select(pp => new
            {
                id = pp.Id,
                price = pp.Price,
                startTime = PersianDateService.ToPersian(pp.StartTime),
                endTime = PersianDateService.ToPersian(pp.EndTime)
            })
        });
    }

    [HttpGet]
    public async Task<IActionResult> GetCurrentPrice(int productId)
    {
        var now = DateTime.UtcNow;
        var price = await _context.ProductPrices
            .Where(pp => pp.ProductId == productId && pp.StartTime <= now && pp.EndTime >= now)
            .OrderByDescending(pp => pp.CreateDate)
            .FirstOrDefaultAsync();

        if (price == null)
            return Json(new { price = 0 });

        return Json(new { price = price.Price });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdatePrice(int productId, decimal newPrice)
    {
        var product = await _context.Products
            .Include(p => p.ProductPrices)
            .FirstOrDefaultAsync(p => p.Id == productId);

        if (product == null)
            return NotFound();

        var now = DateTime.UtcNow;

        // End current active prices by setting EndTime to now
        var activePrices = product.ProductPrices
            .Where(pp => pp.StartTime <= now && pp.EndTime >= now)
            .ToList();

        foreach (var activePrice in activePrices)
        {
            activePrice.EndTime = now.AddSeconds(-1);
            _context.ProductPrices.Update(activePrice);
        }

        // Create new price record
        var newPriceRecord = new ProductPrice
        {
            ProductId = productId,
            Price = newPrice,
            StartTime = now,
            EndTime = now.AddYears(1),
            CreateDate = now
        };
        _context.ProductPrices.Add(newPriceRecord);
        await _context.SaveChangesAsync();

        return Json(new { success = true, message = "قیمت با موفقیت بروزرسانی شد" });
    }

    [HttpGet]
    public async Task<IActionResult> Search(string term)
    {
        var products = await _context.Products
            .Include(p => p.Category)
            .Where(p => p.Name.Contains(term) || p.Code.Contains(term))
            .Select(p => new { id = p.Id, name = p.Name, code = p.Code, category = p.Category!.Name })
            .Take(20)
            .ToListAsync();

        return Json(products);
    }
}
