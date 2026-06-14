using Factors.Web.Models.ViewModels;

namespace Factors.Web.Services;

public interface IReportService
{
    DashboardViewModel GetDashboardData();
    StatisticalDataResult GetStatisticalData(StatisticalReportQuery query);
}
