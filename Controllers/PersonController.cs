using Factors.Web.Data;
using Factors.Web.Infrastructure;
using Factors.Web.Models.Entities;
using Factors.Web.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Factors.Web.Controllers;

[Authorize]
[PermissionAuthorize("Person.View")]
public class PersonController : Controller
{
    private readonly AppDbContext _context;

    public PersonController(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index(string? search)
    {
        var query = _context.Persons
            .Include(p => p.Factors)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(p => p.PersonName.Contains(search));
        }

        var personList = await query
            .OrderByDescending(p => p.CreateDate)
            .ToListAsync();

        var persons = personList.Select(p => new PersonViewModel
        {
            Id = p.Id,
            PersonName = p.PersonName,
            IsIndividual = p.IsIndividual,
            PersianCreateDate = PersianDateService.ToPersian(p.CreateDate),
            FactorCount = p.Factors.Count
        }).ToList();

        var model = new PersonListViewModel
        {
            Persons = persons,
            SearchTerm = search ?? ""
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [PermissionAuthorize("Person.Create")]
    public async Task<IActionResult> Create(PersonViewModel model)
    {
        if (!ModelState.IsValid)
        {
            TempData["Error"] = "لطفاً نام شخص را وارد کنید";
            return RedirectToAction("Index");
        }

        var person = new Person
        {
            PersonName = model.PersonName.Trim(),
            IsIndividual = model.IsIndividual,
            CreateDate = DateTime.UtcNow
        };

        _context.Persons.Add(person);
        await _context.SaveChangesAsync();

        TempData["Success"] = "شخص با موفقیت ثبت شد";
        return RedirectToAction("Index");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [PermissionAuthorize("Person.Edit")]
    public async Task<IActionResult> Edit(PersonViewModel model)
    {
        if (!ModelState.IsValid)
        {
            TempData["Error"] = "لطفاً نام شخص را وارد کنید";
            return RedirectToAction("Index");
        }

        var person = await _context.Persons.FindAsync(model.Id);
        if (person == null)
            return NotFound();

        person.PersonName = model.PersonName.Trim();
        person.IsIndividual = model.IsIndividual;
        await _context.SaveChangesAsync();

        TempData["Success"] = "شخص با موفقیت ویرایش شد";
        return RedirectToAction("Index");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [PermissionAuthorize("Person.Delete")]
    public async Task<IActionResult> Delete(int id)
    {
        var person = await _context.Persons
            .Include(p => p.Factors)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (person == null)
            return NotFound();

        if (person.Factors.Any())
        {
            TempData["Error"] = "این شخص دارای فاکتور است و قابل حذف نیست";
            return RedirectToAction("Index");
        }

        _context.Persons.Remove(person);
        await _context.SaveChangesAsync();

        TempData["Success"] = "شخص با موفقیت حذف شد";
        return RedirectToAction("Index");
    }

    [HttpGet]
    public async Task<IActionResult> GetById(int id)
    {
        var person = await _context.Persons.FindAsync(id);
        if (person == null)
            return NotFound();

        return Json(new { id = person.Id, personName = person.PersonName, isIndividual = person.IsIndividual });
    }

    [HttpGet]
    public async Task<IActionResult> Search(string term)
    {
        var persons = await _context.Persons
            .Where(p => p.PersonName.Contains(term))
            .Select(p => new { id = p.Id, name = p.PersonName, type = p.IsIndividual ? "حقیقی" : "حقوقی" })
            .Take(20)
            .ToListAsync();

        return Json(persons);
    }
}
