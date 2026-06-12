using System.ComponentModel.DataAnnotations;
using Factors.Web.Models.Entities;

namespace Factors.Web.Models.ViewModels;

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

    /// <summary>
    /// نام مارکر لیستی پدر (اگر این مارکر زیرمجموعه یک لیست است)
    /// </summary>
    public string? ParentListMarker { get; set; }

    /// <summary>
    /// آیا مارکر لیستی (ساختاری) است؟ → نیازی به نقشه‌برداری ندارد
    /// </summary>
    public bool IsListMarker => DataType == MarkerDataType.List;

    /// <summary>
    /// آیا مارکر زیرمجموعه یک جدول لیستی است؟ → منبع داده = آیتم فاکتور
    /// </summary>
    public bool IsListItem => !string.IsNullOrEmpty(ParentListMarker);

    public MarkerDataSource DataSource { get; set; }
    public string? PropertyPath { get; set; }
    public bool IsMapped => !string.IsNullOrWhiteSpace(PropertyPath);
    public List<PropertyOption> AvailableProperties { get; set; } = new();
}

public class PropertyOption
{
    public string Value { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public string Group { get; set; } = string.Empty;
}

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

public class GenerateReportViewModel
{
    public int TemplateId { get; set; }
    public string TemplateName { get; set; } = string.Empty;

    [Required(ErrorMessage = "انتخاب فاکتور الزامی است")]
    public int FactorId { get; set; }
}

public class ExtractedMarker
{
    public string Name { get; set; } = string.Empty;
    public MarkerDataType DataType { get; set; } = MarkerDataType.Single;
    public bool IsInTable { get; set; }
    public string? ParentListMarker { get; set; }
}

/// <summary>
/// پراپرتی‌های جامع قابل استفاده در نقشه‌برداری
/// </summary>
public static class AvailableProperties
{
    public static List<PropertyOption> FactorProperties => new()
    {
        // شناسایی
        new() { Value = "Id", Label = "شماره فاکتور", Group = "شناسایی" },
        new() { Value = "Invoicenumber", Label = "شماره پیش‌فاکتور", Group = "شناسایی" },

        // تاریخ
        new() { Value = "PersianCreateDate", Label = "تاریخ شمسی", Group = "تاریخ" },
        new() { Value = "date", Label = "تاریخ (کوتاه)", Group = "تاریخ" },

        // مشتری
        new() { Value = "PersonName", Label = "نام مشتری", Group = "مشتری" },
        new() { Value = "Buyerdetails", Label = "مشخصات خریدار", Group = "مشتری" },
        new() { Value = "PersonType", Label = "نوع مشتری (حقیقی/حقوقی)", Group = "مشتری" },

        // مبالغ
        new() { Value = "TotalAmount", Label = "جمع کل (بدون مالیات)", Group = "مبالغ" },
        new() { Value = "sum", Label = "جمع کل", Group = "مبالغ" },
        new() { Value = "Totalup", Label = "جمع بالایی", Group = "مبالغ" },
        new() { Value = "TaxAmount", Label = "مالیات", Group = "مبالغ" },
        new() { Value = "tax", Label = "مبلغ مالیات", Group = "مبالغ" },
        new() { Value = "TotalWithTax", Label = "جمع با مالیات", Group = "مبالغ" },
        new() { Value = "Totalsum", Label = "جمع نهایی", Group = "مبالغ" },

        // تعداد
        new() { Value = "TotalItems", Label = "تعداد آیتم‌ها", Group = "تعداد" },
        new() { Value = "TotalQuantity", Label = "تعداد کل اقلام", Group = "تعداد" },
    };

    public static List<PropertyOption> FactorItemProperties => new()
    {
        // نام و شرح
        new() { Value = "ProductName", Label = "نام کالا/محصول", Group = "نام" },
        new() { Value = "Descriptionofthepiece", Label = "شرح قلم", Group = "نام" },
        new() { Value = "ProductCode", Label = "کد محصول", Group = "نام" },
        new() { Value = "CategoryName", Label = "دسته‌بندی محصول", Group = "نام" },

        // تعداد
        new() { Value = "Qty", Label = "تعداد", Group = "تعداد" },
        new() { Value = "PartQty", Label = "تعداد قلم", Group = "تعداد" },

        // قیمت
        new() { Value = "Price", Label = "قیمت واحد", Group = "قیمت" },
        new() { Value = "price", Label = "قیمت واحد (کوتاه)", Group = "قیمت" },
        new() { Value = "TotalPrice", Label = "قیمت کل سطر", Group = "قیمت" },
        new() { Value = "total", Label = "قیمت کل (کوتاه)", Group = "قیمت" },

        // سطر
        new() { Value = "RowNumber", Label = "شماره سطر", Group = "سطر" },

        // بسته
        new() { Value = "IsPack", Label = "آیا بسته است؟", Group = "بسته" },
        new() { Value = "PackName", Label = "نام بسته", Group = "بسته" },
    };

    public static List<PropertyOption> PersonProperties => new()
    {
        new() { Value = "PersonName", Label = "نام شخص/شرکت", Group = "مشتری" },
        new() { Value = "Buyerdetails", Label = "مشخصات خریدار", Group = "مشتری" },
        new() { Value = "PersonType", Label = "نوع (حقیقی/حقوقی)", Group = "مشتری" },
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

    /// <summary>
    /// نقشه‌برداری خودکار مارکرهای شناخته‌شده
    /// </summary>
    public static (MarkerDataSource DataSource, string PropertyPath)? TryAutoMap(string markerName)
    {
        var map = new Dictionary<string, (MarkerDataSource, string)>(StringComparer.OrdinalIgnoreCase)
        {
            // مارکرهای فاکتور
            ["Invoicenumber"] = (MarkerDataSource.Factor, "Invoicenumber"),
            ["date"] = (MarkerDataSource.Factor, "date"),
            ["Buyerdetails"] = (MarkerDataSource.Factor, "Buyerdetails"),
            ["Totalup"] = (MarkerDataSource.Factor, "Totalup"),
            ["sum"] = (MarkerDataSource.Factor, "sum"),
            ["tax"] = (MarkerDataSource.Factor, "tax"),
            ["Totalsum"] = (MarkerDataSource.Factor, "Totalsum"),

            // مارکرهای آیتم فاکتور
            ["Descriptionofthepiece"] = (MarkerDataSource.FactorItem, "Descriptionofthepiece"),
            ["PartQty"] = (MarkerDataSource.FactorItem, "PartQty"),
            ["total"] = (MarkerDataSource.FactorItem, "total"),
            ["price"] = (MarkerDataSource.FactorItem, "price"),
        };

        return map.TryGetValue(markerName, out var mapping) ? mapping : null;
    }
}
