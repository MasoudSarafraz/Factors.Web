using System.ComponentModel.DataAnnotations;
using Factors.Web.Models.Entities;

namespace Factors.Web.Models.ViewModels;

/// <summary>
/// مدل نمایش قالب در لیست
/// </summary>
public class ReportTemplateListViewModel
{
    public List<ReportTemplateDetailViewModel> Templates { get; set; } = new();
}

public class ReportTemplateDetailViewModel
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string OriginalFileName { get; set; } = string.Empty;
    public string PersianCreateDate { get; set; } = string.Empty;
    public int MarkerCount { get; set; }
    public int MappedMarkerCount { get; set; }
}

/// <summary>
/// مدل آپلود قالب جدید
/// </summary>
public class ReportTemplateUploadViewModel
{
    [Required(ErrorMessage = "نام قالب الزامی است")]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Description { get; set; }

    [Required(ErrorMessage = "فایل قالب الزامی است")]
    public IFormFile TemplateFile { get; set; } = null!;
}

/// <summary>
/// مدل نقشه‌برداری مارکرها به پراپرتی‌ها
/// </summary>
public class MarkerMappingViewModel
{
    public int TemplateId { get; set; }
    public string TemplateName { get; set; } = string.Empty;
    public List<MarkerMappingItem> Markers { get; set; } = new();
}

public class MarkerMappingItem
{
    public int MarkerId { get; set; }
    public string MarkerName { get; set; } = string.Empty;
    public MarkerDataType DataType { get; set; }
    public MarkerDataSource DataSource { get; set; }
    public string? PropertyPath { get; set; }
    public bool IsMapped => !string.IsNullOrWhiteSpace(PropertyPath);

    /// <summary>
    /// لیست پراپرتی‌های موجود برای نقشه‌برداری (بر اساس DataSource)
    /// </summary>
    public List<PropertyOption> AvailableProperties { get; set; } = new();
}

public class PropertyOption
{
    public string Value { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public string Group { get; set; } = string.Empty;
}

/// <summary>
/// مدل ذخیره نقشه‌برداری
/// </summary>
public class SaveMarkerMappingViewModel
{
    public int TemplateId { get; set; }
    public List<SaveMarkerMappingItem> Mappings { get; set; } = new();
}

public class SaveMarkerMappingItem
{
    public int MarkerId { get; set; }
    public MarkerDataSource DataSource { get; set; }
    public string? PropertyPath { get; set; }
}

/// <summary>
/// مدل تولید گزارش از قالب
/// </summary>
public class GenerateReportViewModel
{
    public int TemplateId { get; set; }
    public string TemplateName { get; set; } = string.Empty;

    [Required(ErrorMessage = "انتخاب فاکتور الزامی است")]
    public int FactorId { get; set; }
}

/// <summary>
/// مارکر استخراج شده از فایل Word
/// </summary>
public class ExtractedMarker
{
    public string Name { get; set; } = string.Empty;
    public MarkerDataType DataType { get; set; } = MarkerDataType.Single;
    public bool IsInTable { get; set; }
    public string? ParentListMarker { get; set; }
}

/// <summary>
/// پراپرتی‌های قابل استفاده در نقشه‌برداری
/// </summary>
public static class AvailableProperties
{
    /// <summary>
    /// پراپرتی‌های مدل فاکتور (Single)
    /// </summary>
    public static List<PropertyOption> FactorProperties => new()
    {
        new() { Value = "Id", Label = "شماره فاکتور", Group = "فاکتور" },
        new() { Value = "PersonName", Label = "نام مشتری", Group = "فاکتور" },
        new() { Value = "PersianCreateDate", Label = "تاریخ شمسی", Group = "فاکتور" },
        new() { Value = "TotalAmount", Label = "جمع کل مبلغ", Group = "فاکتور" },
        new() { Value = "TotalItems", Label = "تعداد آیتم‌ها", Group = "فاکتور" },
        new() { Value = "TaxAmount", Label = "مالیات", Group = "فاکتور" },
        new() { Value = "TotalWithTax", Label = "جمع با مالیات", Group = "فاکتور" },
    };

    /// <summary>
    /// پراپرتی‌های مدل آیتم فاکتور (List)
    /// </summary>
    public static List<PropertyOption> FactorItemProperties => new()
    {
        new() { Value = "ProductName", Label = "نام کالا", Group = "آیتم فاکتور" },
        new() { Value = "Qty", Label = "تعداد", Group = "آیتم فاکتور" },
        new() { Value = "Price", Label = "قیمت واحد", Group = "آیتم فاکتور" },
        new() { Value = "TotalPrice", Label = "قیمت کل", Group = "آیتم فاکتور" },
        new() { Value = "RowNumber", Label = "شماره سطر", Group = "آیتم فاکتور" },
    };

    /// <summary>
    /// پراپرتی‌های مدل مشتری (Single)
    /// </summary>
    public static List<PropertyOption> PersonProperties => new()
    {
        new() { Value = "PersonName", Label = "نام شخص", Group = "مشتری" },
        new() { Value = "IsIndividual", Label = "نوع (حقیقی/حقوقی)", Group = "مشتری" },
        new() { Value = "Id", Label = "شناسه مشتری", Group = "مشتری" },
    };

    public static List<PropertyOption> GetPropertiesForDataSource(MarkerDataSource dataSource)
    {
        return dataSource switch
        {
            MarkerDataSource.Factor => FactorProperties,
            MarkerDataSource.FactorItem => FactorItemProperties,
            MarkerDataSource.Person => PersonProperties,
            _ => new List<PropertyOption>()
        };
    }
}
