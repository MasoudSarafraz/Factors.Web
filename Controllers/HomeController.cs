using Factors.Web.Infrastructure;
using Factors.Web.Models.ViewModels;
using Factors.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Factors.Web.Controllers;

[Authorize]
[PermissionAuthorize("Dashboard.View")]
public class HomeController : Controller
{
    private readonly IReportService _reportService;

    public HomeController(IReportService reportService)
    {
        _reportService = reportService;
    }

    public IActionResult Index()
    {
        var dashboard = _reportService.GetDashboardData();
        return View(dashboard);
    }
}
