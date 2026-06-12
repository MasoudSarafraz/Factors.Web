using Factors.Web.Models.ViewModels;

namespace Factors.Web.Services;

public interface IReportService
{
    byte[] GenerateFactorReportPdf(FactorViewModel factor);
    byte[] GenerateSalesReportPdf(ReportFilterViewModel filter, List<FactorViewModel> factors);
    byte[] GenerateProductReportPdf(ReportFilterViewModel filter, List<ProductViewModel> products);
    DashboardViewModel GetDashboardData();
}
