using System.ComponentModel.DataAnnotations;

namespace Factors.Web.Models.ViewModels;

public class SettingsViewModel
{
    public List<int> FactorPrintTemplateIds { get; set; } = new();
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
    public List<int>? FactorPrintTemplateIds { get; set; }
    public string? CompanyName { get; set; }
    public string? CompanyPhone { get; set; }
    public string? CompanyAddress { get; set; }
    public string? CompanyEconomicCode { get; set; }
}

/// <summary>
/// Used in Factor views (via ViewBag) to render the print dropdown.
/// Id=0 means default PDF; Id>0 means a specific Word template.
/// </summary>
public class PrintTemplateOption
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
}
