using Factors.Web.Data;
using Factors.Web.Infrastructure;
using Factors.Web.Models.Entities;
using Factors.Web.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Factors.Web.Controllers;

[Authorize]
[PermissionAuthorize("Category.View")]
public class CategoryController : Controller
{
    private readonly AppDbContext _context;

    public CategoryController(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index(string? search)
    {
        var query = _context.ProductCategories
            .Include(c => c.Products)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(c => c.Name.Contains(search));
        }

        var categoryList = await query
            .OrderByDescending(c => c.CreateDate)
            .ToListAsync();

        var categories = categoryList.Select(c => new CategoryViewModel
        {
            Id = c.Id,
            Name = c.Name,
            PersianCreateDate = PersianDateService.ToPersian(c.CreateDate),
            ProductCount = c.Products.Count
        }).ToList();

        var model = new CategoryListViewModel
        {
            Categories = categories,
            SearchTerm = search ?? ""
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [PermissionAuthorize("Category.Create")]
    public async Task<IActionResult> Create(CategoryViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return RedirectToAction("Index");
        }

        // Check duplicate
        var exists = await _context.ProductCategories
            .AnyAsync(c => c.Name == model.Name.Trim());
        if (exists)
        {
            TempData["Error"] = $"دسته‌بندی با نام '{model.Name}' قبلاً ثبت شده است";
            return RedirectToAction("Index");
        }

        var category = new ProductCategory
        {
            Name = model.Name.Trim(),
            CreateDate = DateTime.UtcNow
        };

        _context.ProductCategories.Add(category);
        await _context.SaveChangesAsync();

        TempData["Success"] = "دسته‌بندی با موفقیت ثبت شد";
        return RedirectToAction("Index");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [PermissionAuthorize("Category.Edit")]
    public async Task<IActionResult> Edit(CategoryViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return RedirectToAction("Index");
        }

        var category = await _context.ProductCategories.FindAsync(model.Id);
        if (category == null)
            return NotFound();

        // Check duplicate (exclude self)
        var exists = await _context.ProductCategories
            .AnyAsync(c => c.Name == model.Name.Trim() && c.Id != model.Id);
        if (exists)
        {
            TempData["Error"] = "نام دسته‌بندی تکراری است";
            return RedirectToAction("Index");
        }

        category.Name = model.Name.Trim();
        await _context.SaveChangesAsync();

        TempData["Success"] = "دسته‌بندی با موفقیت ویرایش شد";
        return RedirectToAction("Index");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [PermissionAuthorize("Category.Delete")]
    public async Task<IActionResult> Delete(int id)
    {
        var category = await _context.ProductCategories
            .Include(c => c.Products)
            .FirstOrDefaultAsync(c => c.Id == id);

        if (category == null)
            return NotFound();

        if (category.Products.Any())
        {
            TempData["Error"] = "این دسته‌بندی دارای محصول است و قابل حذف نیست";
            return RedirectToAction("Index");
        }

        _context.ProductCategories.Remove(category);
        await _context.SaveChangesAsync();

        TempData["Success"] = "دسته‌بندی با موفقیت حذف شد";
        return RedirectToAction("Index");
    }

    [HttpGet]
    public async Task<IActionResult> GetById(int id)
    {
        var category = await _context.ProductCategories.FindAsync(id);
        if (category == null)
            return NotFound();

        return Json(new { id = category.Id, name = category.Name });
    }
}
