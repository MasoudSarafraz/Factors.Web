using Factors.Web.Data;
using Factors.Web.Infrastructure;
using Factors.Web.Models.Entities;
using Factors.Web.Models.ViewModels;
using Factors.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Factors.Web.Controllers;

[Authorize]
[PermissionAuthorize("Report.View")]
public class ReportController : Controller
{
    private readonly AppDbContext _context;
    private readonly IReportService _reportService;

    public ReportController(AppDbContext context, IReportService reportService)
    {
        _context = context;
        _reportService = reportService;
    }

    public IActionResult Index()
    {
        ViewBag.Persons = _context.Persons.OrderBy(p => p.PersonName).ToList();
        ViewBag.Categories = _context.ProductCategories.OrderBy(c => c.Name).ToList();

        return View(new StatisticalReportQuery());
    }

    [HttpGet]
    public IActionResult GetData([FromQuery] StatisticalReportQuery query)
    {
        var result = _reportService.GetStatisticalData(query);
        return Json(result);
    }

    [HttpGet]
    public async Task<IActionResult> GetQuickStats()
    {
        var totalFactors = await _context.Factors.CountAsync();
        var totalPersons = await _context.Persons.CountAsync();
        var totalProducts = await _context.Products.CountAsync();
        var totalCategories = await _context.ProductCategories.CountAsync();

        var factorItems = await _context.FactorItems.ToListAsync();
        var totalSales = factorItems.Where(fi => !fi.ParentId.HasValue).Sum(fi => fi.Price * fi.Qty);

        return Json(new
        {
            totalFactors,
            totalPersons,
            totalProducts,
            totalCategories,
            totalSales
        });
    }
}
