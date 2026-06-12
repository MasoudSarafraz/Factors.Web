using Factors.Web.Data;
using Factors.Web.Models.Entities;
using Factors.Web.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Factors.Web.Controllers;

[Authorize]
public class PackController : Controller
{
    private readonly AppDbContext _context;

    public PackController(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index(string? search)
    {
        var query = _context.ProductPacks
            .Include(p => p.PackItems)
                .ThenInclude(pi => pi.Product)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(p => p.PackName.Contains(search) || p.PackCode.Contains(search));
        }

        var packs = await query
            .OrderByDescending(p => p.CreateDate)
            .Select(p => new PackViewModel
            {
                Id = p.Id,
                PackName = p.PackName,
                PackCode = p.PackCode,
                PersianCreateDate = PersianDateService.ToPersian(p.CreateDate),
                TotalPrice = p.PackItems.Sum(pi => pi.Price * pi.Qty),
                Items = p.PackItems.Select(pi => new PackItemViewModel
                {
                    Id = pi.Id,
                    ProductId = pi.ProductId,
                    ProductName = pi.Product!.Name,
                    Qty = pi.Qty,
                    Price = pi.Price,
                    PackId = pi.PackId
                }).ToList()
            })
            .ToListAsync();

        var model = new PackListViewModel
        {
            Packs = packs,
            SearchTerm = search ?? ""
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(PackViewModel model)
    {
        if (!ModelState.IsValid)
        {
            TempData["Error"] = "لطفاً تمام فیلدهای الزامی را پر کنید";
            return RedirectToAction("Index");
        }

        var existsCode = await _context.ProductPacks.AnyAsync(p => p.PackCode == model.PackCode.Trim());
        if (existsCode)
        {
            TempData["Error"] = $"بسته‌ای با کد '{model.PackCode}' قبلاً ثبت شده است";
            return RedirectToAction("Index");
        }

        var pack = new ProductPack
        {
            PackName = model.PackName.Trim(),
            PackCode = model.PackCode.Trim(),
            CreateDate = DateTime.UtcNow
        };

        _context.ProductPacks.Add(pack);
        await _context.SaveChangesAsync();

        TempData["Success"] = "بسته با موفقیت ثبت شد";
        return RedirectToAction("Index");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(PackViewModel model)
    {
        if (!ModelState.IsValid)
        {
            TempData["Error"] = "لطفاً تمام فیلدهای الزامی را پر کنید";
            return RedirectToAction("Index");
        }

        var pack = await _context.ProductPacks.FindAsync(model.Id);
        if (pack == null)
            return NotFound();

        var existsCode = await _context.ProductPacks.AnyAsync(p => p.PackCode == model.PackCode.Trim() && p.Id != model.Id);
        if (existsCode)
        {
            TempData["Error"] = "کد بسته تکراری است";
            return RedirectToAction("Index");
        }

        pack.PackName = model.PackName.Trim();
        pack.PackCode = model.PackCode.Trim();
        await _context.SaveChangesAsync();

        TempData["Success"] = "بسته با موفقیت ویرایش شد";
        return RedirectToAction("Index");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> Delete(int id)
    {
        var pack = await _context.ProductPacks
            .Include(p => p.PackItems)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (pack == null)
            return NotFound();

        _context.ProductPackItems.RemoveRange(pack.PackItems);
        _context.ProductPacks.Remove(pack);
        await _context.SaveChangesAsync();

        TempData["Success"] = "بسته با موفقیت حذف شد";
        return RedirectToAction("Index");
    }

    // Pack Items Management
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddItem(PackItemViewModel model)
    {
        if (!ModelState.IsValid)
        {
            TempData["Error"] = "لطفاً تمام فیلدها را پر کنید";
            return RedirectToAction("Index");
        }

        var item = new ProductPackItems
        {
            ProductId = model.ProductId,
            PackId = model.PackId,
            Qty = model.Qty,
            Price = model.Price,
            CreateDate = DateTime.UtcNow
        };

        _context.ProductPackItems.Add(item);
        await _context.SaveChangesAsync();

        TempData["Success"] = "آیتم با موفقیت به بسته اضافه شد";
        return RedirectToAction("Index");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> DeleteItem(int id)
    {
        var item = await _context.ProductPackItems.FindAsync(id);
        if (item == null)
            return NotFound();

        _context.ProductPackItems.Remove(item);
        await _context.SaveChangesAsync();

        TempData["Success"] = "آیتم با موفقیت حذف شد";
        return RedirectToAction("Index");
    }

    [HttpGet]
    public async Task<IActionResult> GetById(int id)
    {
        var pack = await _context.ProductPacks
            .Include(p => p.PackItems)
                .ThenInclude(pi => pi.Product)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (pack == null)
            return NotFound();

        return Json(new
        {
            id = pack.Id,
            packName = pack.PackName,
            packCode = pack.PackCode,
            items = pack.PackItems.Select(pi => new
            {
                id = pi.Id,
                productId = pi.ProductId,
                productName = pi.Product!.Name,
                qty = pi.Qty,
                price = pi.Price
            })
        });
    }

    [HttpGet]
    public async Task<IActionResult> Search(string term)
    {
        var packs = await _context.ProductPacks
            .Where(p => p.PackName.Contains(term) || p.PackCode.Contains(term))
            .Select(p => new { id = p.Id, name = p.PackName, code = p.PackCode })
            .Take(20)
            .ToListAsync();

        return Json(packs);
    }
}
