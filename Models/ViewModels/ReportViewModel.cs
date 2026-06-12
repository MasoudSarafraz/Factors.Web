namespace Factors.Web.Models.ViewModels;

public class ReportFilterViewModel
{
    public string ReportType { get; set; } = "factors";
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public int? PersonId { get; set; }
    public int? CategoryId { get; set; }
    public int? ProductId { get; set; }
    public string Format { get; set; } = "pdf"; // pdf, excel
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
