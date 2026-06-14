using Factors.Web.Data;
using Factors.Web.Models.Entities;
using Factors.Web.Models.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace Factors.Web.Services;

public class ReportService : IReportService
{
    private readonly AppDbContext _context;

    public ReportService(AppDbContext context)
    {
        _context = context;
    }

    public DashboardViewModel GetDashboardData()
    {
        var factors = _context.Factors
            .Include(f => f.Person)
            .Include(f => f.FactorItems)
            .OrderByDescending(f => f.CreateDate)
            .ToList();

        var dashboard = new DashboardViewModel
        {
            TotalFactors = factors.Count,
            TotalPersons = _context.Persons.Count(),
            TotalProducts = _context.Products.Count(),
            TotalCategories = _context.ProductCategories.Count(),
            TotalPacks = _context.ProductPacks.Count(),
            TotalUsers = _context.Users.Count(),
            TotalSalesAmount = factors.Sum(f => (f.FactorItems?.Where(fi => !fi.ParentId.HasValue).Sum(fi => fi.Price * fi.Qty) ?? 0)),
            RecentFactors = factors.Take(10).Select(f => new FactorViewModel
            {
                Id = f.Id,
                PersonId = f.PersonId,
                PersonName = f.Person?.PersonName ?? "",
                PersianCreateDate = PersianDateService.ToPersian(f.CreateDate),
                TotalAmount = f.FactorItems?.Where(fi => !fi.ParentId.HasValue).Sum(fi => fi.Price * fi.Qty) ?? 0,
                TotalItems = f.FactorItems?.Count ?? 0
            }).ToList()
        };

        // Top Products (materialize first because SQLite cannot aggregate decimal)
        var factorItemsData = _context.FactorItems
            .Where(fi => fi.SalableId != null)
            .Include(fi => fi.Product)
            .ToList();

        var topProducts = factorItemsData
            .Where(fi => fi.Product != null)
            .GroupBy(fi => new { fi.SalableId, fi.Product!.Name })
            .Select(g => new TopProductViewModel
            {
                ProductName = g.Key.Name,
                TotalQty = g.Sum(x => x.Qty),
                TotalAmount = g.Sum(x => x.Price * x.Qty)
            })
            .OrderByDescending(x => x.TotalAmount)
            .Take(5)
            .ToList();

        dashboard.TopProducts = topProducts;

        // Monthly Sales (last 6 months)
        var sixMonthsAgo = DateTime.UtcNow.AddMonths(-6);
        var monthlySales = factors
            .Where(f => f.CreateDate >= sixMonthsAgo)
            .GroupBy(f => new { f.CreateDate.Year, f.CreateDate.Month })
            .Select(g => new MonthlySalesViewModel
            {
                Month = $"{g.Key.Year}/{g.Key.Month:00}",
                FactorCount = g.Count(),
                TotalAmount = g.Sum(f => f.FactorItems?.Where(fi => !fi.ParentId.HasValue).Sum(fi => fi.Price * fi.Qty) ?? 0)
            })
            .OrderBy(x => x.Month)
            .ToList();

        dashboard.MonthlySales = monthlySales;

        // Sales by Category
        var categorySales = factorItemsData
            .Where(fi => fi.Product != null && fi.Product.Category != null)
            .GroupBy(fi => fi.Product!.Category!.Name)
            .Select(g => new CategorySalesViewModel
            {
                CategoryName = g.Key,
                TotalAmount = g.Sum(x => x.Price * x.Qty),
                TotalQty = g.Sum(x => x.Qty)
            })
            .OrderByDescending(x => x.TotalAmount)
            .ToList();
        dashboard.CategorySales = categorySales;

        // Daily Sales (last 30 days)
        var thirtyDaysAgo = DateTime.UtcNow.AddDays(-30);
        var dailySales = factors
            .Where(f => f.CreateDate >= thirtyDaysAgo)
            .GroupBy(f => f.CreateDate.Date)
            .Select(g => new DailySalesViewModel
            {
                Date = g.Key,
                PersianDate = PersianDateService.ToPersian(g.Key),
                FactorCount = g.Count(),
                TotalAmount = g.Sum(f => f.FactorItems?.Where(fi => !fi.ParentId.HasValue).Sum(fi => fi.Price * fi.Qty) ?? 0)
            })
            .OrderBy(x => x.Date)
            .ToList();
        dashboard.DailySales = dailySales;

        // Top Customers
        var topCustomers = factors
            .Where(f => f.Person != null)
            .GroupBy(f => new { f.PersonId, f.Person!.PersonName })
            .Select(g => new TopCustomerViewModel
            {
                PersonName = g.Key.PersonName,
                FactorCount = g.Count(),
                TotalPurchase = g.Sum(f => f.FactorItems?.Where(fi => !fi.ParentId.HasValue).Sum(fi => fi.Price * fi.Qty) ?? 0)
            })
            .OrderByDescending(x => x.TotalPurchase)
            .Take(5)
            .ToList();
        dashboard.TopCustomers = topCustomers;

        // Person Type Distribution
        var personTypes = _context.Persons
            .GroupBy(p => p.IsIndividual)
            .Select(g => new PersonTypeViewModel
            {
                IsIndividual = g.Key,
                Count = g.Count()
            })
            .ToList();
        dashboard.PersonTypeDistribution = personTypes;

        return dashboard;
    }

    public StatisticalDataResult GetStatisticalData(StatisticalReportQuery query)
    {
        var result = new StatisticalDataResult();

        switch (query.ReportType)
        {
            case "sales_trend":
                result = GetSalesTrendData(query);
                break;
            case "category_distribution":
                result = GetCategoryDistributionData(query);
                break;
            case "product_performance":
                result = GetProductPerformanceData(query);
                break;
            case "customer_analysis":
                result = GetCustomerAnalysisData(query);
                break;
            case "factor_statistics":
                result = GetFactorStatisticsData(query);
                break;
            default:
                result.Labels = new List<string>();
                result.Datasets = new List<ChartDataDataset>();
                break;
        }

        return result;
    }

    private DateTime? ParseJalaliDate(string? jalali)
    {
        if (string.IsNullOrWhiteSpace(jalali)) return null;
        return PersianDateService.ParsePersianDate(jalali);
    }

    private IQueryable<Factor> ApplyDateFilter(IQueryable<Factor> query, StatisticalReportQuery filter)
    {
        var fromDate = ParseJalaliDate(filter.FromDateJalali);
        var toDate = ParseJalaliDate(filter.ToDateJalali);

        if (fromDate.HasValue)
            query = query.Where(f => f.CreateDate >= fromDate.Value);
        if (toDate.HasValue)
            query = query.Where(f => f.CreateDate <= toDate.Value.AddDays(1));

        return query;
    }

    private StatisticalDataResult GetSalesTrendData(StatisticalReportQuery query)
    {
        var factors = ApplyDateFilter(_context.Factors.Include(f => f.FactorItems), query).ToList();

        var grouped = query.TimeGroup switch
        {
            "daily" => factors.GroupBy(f => f.CreateDate.Date).OrderBy(g => g.Key).Select(g => new
            {
                Label = PersianDateService.ToPersian(g.Key),
                Count = g.Count(),
                Amount = g.Sum(f => f.FactorItems?.Where(fi => !fi.ParentId.HasValue).Sum(fi => fi.Price * fi.Qty) ?? 0)
            }).ToList(),
            "monthly" => factors.GroupBy(f => new { f.CreateDate.Year, f.CreateDate.Month }).OrderBy(g => g.Key.Year).ThenBy(g => g.Key.Month).Select(g => new
            {
                Label = $"{g.Key.Year}/{g.Key.Month:00}",
                Count = g.Count(),
                Amount = g.Sum(f => f.FactorItems?.Where(fi => !fi.ParentId.HasValue).Sum(fi => fi.Price * fi.Qty) ?? 0)
            }).ToList(),
            _ => factors.GroupBy(f => new { f.CreateDate.Year, f.CreateDate.Month }).OrderBy(g => g.Key.Year).ThenBy(g => g.Key.Month).Select(g => new
            {
                Label = $"{g.Key.Year}/{g.Key.Month:00}",
                Count = g.Count(),
                Amount = g.Sum(f => f.FactorItems?.Where(fi => !fi.ParentId.HasValue).Sum(fi => fi.Price * fi.Qty) ?? 0)
            }).ToList()
        };

        return new StatisticalDataResult
        {
            Labels = grouped.Select(g => g.Label).ToList(),
            Datasets = new List<ChartDataDataset>
            {
                new() { Label = "مبلغ فروش (ریال)", Data = grouped.Select(g => (decimal)g.Amount).ToList(), Color = "#4361ee" },
                new() { Label = "تعداد فاکتور", Data = grouped.Select(g => (decimal)g.Count).ToList(), Color = "#2ec4b6" }
            }
        };
    }

    private StatisticalDataResult GetCategoryDistributionData(StatisticalReportQuery query)
    {
        var factorItems = _context.FactorItems
            .Include(fi => fi.Product)
                .ThenInclude(p => p!.Category)
            .ToList();

        var grouped = factorItems
            .Where(fi => fi.Product?.Category != null)
            .GroupBy(fi => fi.Product!.Category!.Name)
            .Select(g => new
            {
                Label = g.Key,
                Amount = g.Sum(x => x.Price * x.Qty),
                Qty = g.Sum(x => x.Qty)
            })
            .OrderByDescending(x => x.Amount)
            .ToList();

        var colors = new[] { "#4361ee", "#2ec4b6", "#ff9f1c", "#e71d36", "#7c3aed", "#ec4899", "#3a86ff", "#0ead8c" };

        return new StatisticalDataResult
        {
            Labels = grouped.Select(g => g.Label).ToList(),
            Datasets = new List<ChartDataDataset>
            {
                new() { Label = "مبلغ فروش", Data = grouped.Select(g => g.Amount).ToList(), Colors = grouped.Select((_, i) => colors[i % colors.Length]).ToList() }
            }
        };
    }

    private StatisticalDataResult GetProductPerformanceData(StatisticalReportQuery query)
    {
        var factorItems = _context.FactorItems
            .Include(fi => fi.Product)
            .ToList();

        var grouped = factorItems
            .Where(fi => fi.Product != null)
            .GroupBy(fi => fi.Product!.Name)
            .Select(g => new
            {
                Label = g.Key,
                Qty = g.Sum(x => x.Qty),
                Amount = g.Sum(x => x.Price * x.Qty)
            })
            .OrderByDescending(x => x.Amount)
            .Take(query.TopCount > 0 ? query.TopCount : 10)
            .ToList();

        return new StatisticalDataResult
        {
            Labels = grouped.Select(g => g.Label).ToList(),
            Datasets = new List<ChartDataDataset>
            {
                new() { Label = "مبلغ فروش (ریال)", Data = grouped.Select(g => g.Amount).ToList(), Color = "#4361ee" },
                new() { Label = "تعداد فروش", Data = grouped.Select(g => (decimal)g.Qty).ToList(), Color = "#ff9f1c" }
            }
        };
    }

    private StatisticalDataResult GetCustomerAnalysisData(StatisticalReportQuery query)
    {
        var factors = ApplyDateFilter(_context.Factors.Include(f => f.Person).Include(f => f.FactorItems), query).ToList();

        var grouped = factors
            .Where(f => f.Person != null)
            .GroupBy(f => f.Person!.PersonName)
            .Select(g => new
            {
                Label = g.Key,
                Count = g.Count(),
                Amount = g.Sum(f => f.FactorItems?.Where(fi => !fi.ParentId.HasValue).Sum(fi => fi.Price * fi.Qty) ?? 0)
            })
            .OrderByDescending(x => x.Amount)
            .Take(query.TopCount > 0 ? query.TopCount : 10)
            .ToList();

        return new StatisticalDataResult
        {
            Labels = grouped.Select(g => g.Label).ToList(),
            Datasets = new List<ChartDataDataset>
            {
                new() { Label = "مجموع خرید (ریال)", Data = grouped.Select(g => g.Amount).ToList(), Color = "#2ec4b6" },
                new() { Label = "تعداد فاکتور", Data = grouped.Select(g => (decimal)g.Count).ToList(), Color = "#7c3aed" }
            }
        };
    }

    private StatisticalDataResult GetFactorStatisticsData(StatisticalReportQuery query)
    {
        var factors = ApplyDateFilter(_context.Factors.Include(f => f.FactorItems), query).ToList();

        var avgAmount = factors.Count > 0 ? factors.Average(f => f.FactorItems?.Where(fi => !fi.ParentId.HasValue).Sum(fi => fi.Price * fi.Qty) ?? 0) : 0;
        var maxAmount = factors.Count > 0 ? factors.Max(f => f.FactorItems?.Where(fi => !fi.ParentId.HasValue).Sum(fi => fi.Price * fi.Qty) ?? 0) : 0;
        var minAmount = factors.Count > 0 ? factors.Min(f => f.FactorItems?.Where(fi => !fi.ParentId.HasValue).Sum(fi => fi.Price * fi.Qty) ?? 0) : 0;
        var totalAmount = factors.Sum(f => f.FactorItems?.Where(fi => !fi.ParentId.HasValue).Sum(fi => fi.Price * fi.Qty) ?? 0);

        // Range distribution
        var ranges = new List<string> { "0 - 1M", "1M - 5M", "5M - 10M", "10M - 50M", "50M - 100M", "100M+" };
        var rangeCounts = new List<decimal>
        {
            factors.Count(f => (f.FactorItems?.Where(fi => !fi.ParentId.HasValue).Sum(fi => fi.Price * fi.Qty) ?? 0) < 1000000),
            factors.Count(f => { var a = f.FactorItems?.Where(fi => !fi.ParentId.HasValue).Sum(fi => fi.Price * fi.Qty) ?? 0; return a >= 1000000 && a < 5000000; }),
            factors.Count(f => { var a = f.FactorItems?.Where(fi => !fi.ParentId.HasValue).Sum(fi => fi.Price * fi.Qty) ?? 0; return a >= 5000000 && a < 10000000; }),
            factors.Count(f => { var a = f.FactorItems?.Where(fi => !fi.ParentId.HasValue).Sum(fi => fi.Price * fi.Qty) ?? 0; return a >= 10000000 && a < 50000000; }),
            factors.Count(f => { var a = f.FactorItems?.Where(fi => !fi.ParentId.HasValue).Sum(fi => fi.Price * fi.Qty) ?? 0; return a >= 50000000 && a < 100000000; }),
            factors.Count(f => (f.FactorItems?.Where(fi => !fi.ParentId.HasValue).Sum(fi => fi.Price * fi.Qty) ?? 0) >= 100000000)
        };

        return new StatisticalDataResult
        {
            Labels = ranges,
            Datasets = new List<ChartDataDataset>
            {
                new() { Label = "تعداد فاکتور", Data = rangeCounts, Color = "#4361ee" }
            },
            Summary = new StatisticalSummary
            {
                TotalCount = factors.Count,
                TotalAmount = totalAmount,
                AverageAmount = avgAmount,
                MaxAmount = maxAmount,
                MinAmount = minAmount
            }
        };
    }
}
