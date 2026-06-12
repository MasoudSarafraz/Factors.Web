using System.ComponentModel.DataAnnotations;

namespace Factors.Web.Models.ViewModels;

public class SettingsViewModel
{
    public int? DefaultFactorPrintTemplateId { get; set; }
    public List<ReportTemplateOption> ReportTemplates { get; set; } = new();
    public string CompanyName { get; set; } = "سیستم مدیریت فاکتور";
    public string CompanyPhone { get; set; } = "";
    public string CompanyAddress { get; set; } = "";
    public string CompanyEconomicCode { get; set; } = "";
}

public class ReportTemplateOption
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public int TemplateType { get; set; }
    public string TemplateTypeName { get; set; } = "";
}

public class SettingsSaveViewModel
{
    public int? DefaultFactorPrintTemplateId { get; set; }
    public string? CompanyName { get; set; }
    public string? CompanyPhone { get; set; }
    public string? CompanyAddress { get; set; }
    public string? CompanyEconomicCode { get; set; }
}
