namespace Factors.Web.Models.ViewModels;

public class ReportFilterViewModel
{
    public string ReportType { get; set; } = "factors";
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public string FromDateJalali { get; set; } = string.Empty;
    public string ToDateJalali { get; set; } = string.Empty;
    public int? PersonId { get; set; }
    public int? CategoryId { get; set; }
    public int? ProductId { get; set; }
    public string Format { get; set; } = "pdf";
}

public class DashboardViewModel
{
    public int TotalFactors { get; set; }
    public int TotalPersons { get; set; }
    public int TotalProducts { get; set; }
    public int TotalCategories { get; set; }
    public int TotalPacks { get; set; }
    public int TotalUsers { get; set; }
    public decimal TotalSalesAmount { get; set; }
    public List<FactorViewModel> RecentFactors { get; set; } = new();
    public List<TopProductViewModel> TopProducts { get; set; } = new();
    public List<MonthlySalesViewModel> MonthlySales { get; set; } = new();
    public List<CategorySalesViewModel> CategorySales { get; set; } = new();
    public List<DailySalesViewModel> DailySales { get; set; } = new();
    public List<TopCustomerViewModel> TopCustomers { get; set; } = new();
    public List<PersonTypeViewModel> PersonTypeDistribution { get; set; } = new();
}

public class TopProductViewModel
{
    public string ProductName { get; set; } = string.Empty;
    public int TotalQty { get; set; }
    public decimal TotalAmount { get; set; }
}

public class MonthlySalesViewModel
{
    public string Month { get; set; } = string.Empty;
    public int FactorCount { get; set; }
    public decimal TotalAmount { get; set; }
}

public class CategorySalesViewModel
{
    public string CategoryName { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public int TotalQty { get; set; }
}

public class DailySalesViewModel
{
    public DateTime Date { get; set; }
    public string PersianDate { get; set; } = string.Empty;
    public int FactorCount { get; set; }
    public decimal TotalAmount { get; set; }
}

public class TopCustomerViewModel
{
    public string PersonName { get; set; } = string.Empty;
    public int FactorCount { get; set; }
    public decimal TotalPurchase { get; set; }
}

public class PersonTypeViewModel
{
    public bool IsIndividual { get; set; }
    public int Count { get; set; }
    public string TypeName => IsIndividual ? "حقیقی" : "حقوقی";
}

// ============================================================
// Statistical Report Builder Models
// ============================================================

public class StatisticalReportQuery
{
    public string ReportType { get; set; } = "sales_trend";
    public string? FromDateJalali { get; set; }
    public string? ToDateJalali { get; set; }
    public string TimeGroup { get; set; } = "monthly";
    public int TopCount { get; set; } = 10;
    public string ChartType { get; set; } = "bar";
}

public class StatisticalDataResult
{
    public List<string> Labels { get; set; } = new();
    public List<ChartDataDataset> Datasets { get; set; } = new();
    public StatisticalSummary? Summary { get; set; }
}

public class ChartDataDataset
{
    public string Label { get; set; } = string.Empty;
    public List<decimal> Data { get; set; } = new();
    public string? Color { get; set; }
    public List<string>? Colors { get; set; }
}

public class StatisticalSummary
{
    public int TotalCount { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal AverageAmount { get; set; }
    public decimal MaxAmount { get; set; }
    public decimal MinAmount { get; set; }
}
