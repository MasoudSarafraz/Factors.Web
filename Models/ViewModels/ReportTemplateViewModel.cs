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
    public ReportTemplateType TemplateType { get; set; }
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

    /// <summary>
    /// نوع قالب گزارش
    /// </summary>
    public ReportTemplateType TemplateType { get; set; } = ReportTemplateType.SingleFactor;

    [Required(ErrorMessage = "فایل قالب الزامی است")]
    public IFormFile TemplateFile { get; set; } = null!;
}

public class MarkerMappingViewModel
{
    public int TemplateId { get; set; }
    public string TemplateName { get; set; } = string.Empty;
    public ReportTemplateType TemplateType { get; set; }
    /// <summary>
    /// مارکرها به صورت سلسله‌مراتبی: مارکرهای لیستی + فرزندانشان
    /// </summary>
    public List<MarkerMappingGroup> Groups { get; set; } = new();
}

/// <summary>
/// گروه مارکرها: یک مارکر لیستی (هدر) + مارکرهای فرزندش
/// یا یک مارکر تکی مستقل
/// </summary>
public class MarkerMappingGroup
{
    /// <summary>
    /// مارکر اصلی گروه (لیستی یا تکی)
    /// </summary>
    public MarkerMappingItem MainMarker { get; set; } = new();

    /// <summary>
    /// مارکرهای فرزند (فقط برای مارکرهای لیستی)
    /// </summary>
    public List<MarkerMappingItem> ChildMarkers { get; set; } = new();

    /// <summary>
    /// آیا این گروه یک لیست است؟
    /// </summary>
    public bool IsList => MainMarker.DataType == MarkerDataType.List;
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
    public bool IsMapped => DataType == MarkerDataType.List || !string.IsNullOrWhiteSpace(PropertyPath);
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

/// <summary>
/// مدل صفحه تولید گزارش قدیمی - حذف خواهد شد
/// </summary>
[Obsolete("Use ReportBuilder controller instead")]
public class GenerateReportViewModel
{
    public int TemplateId { get; set; }
    public string TemplateName { get; set; } = string.Empty;

    [Required(ErrorMessage = "انتخاب فاکتور الزامی است")]
    public int FactorId { get; set; }
}

public class ReportTemplateEditViewModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "نام قالب الزامی است")]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Description { get; set; }

    /// <summary>
    /// نوع قالب گزارش
    /// </summary>
    public ReportTemplateType TemplateType { get; set; } = ReportTemplateType.SingleFactor;

    /// <summary>
    /// فایل قالب جدید (اختیاری — اگر خالی باشد فایل قبلی حفظ می‌شود)
    /// </summary>
    public IFormFile? TemplateFile { get; set; }

    public string OriginalFileName { get; set; } = string.Empty;
}

/// <summary>
/// مدل برای مودال انتخاب قالب در صفحه فاکتور
/// </summary>
public class SelectTemplateViewModel
{
    public int FactorId { get; set; }
    public List<TemplateInfo> Templates { get; set; } = new();
}

public class TemplateInfo
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public ReportTemplateType TemplateType { get; set; }
    public int MarkerCount { get; set; }
    public int MappedMarkerCount { get; set; }
}

public class ExtractedMarker
{
    public string Name { get; set; } = string.Empty;
    public MarkerDataType DataType { get; set; } = MarkerDataType.Single;
    public bool IsInTable { get; set; }
    public string? ParentListMarker { get; set; }
}

// ============================================================
// ویوومدل‌های مرکز گزارش‌سازی حرفه‌ای
// ============================================================

/// <summary>
/// مدل صفحه اصلی مرکز گزارش‌سازی
/// </summary>
public class ReportCenterViewModel
{
    /// <summary>
    /// لیست قالب‌های موجود (فیلتر شده بر اساس نوع)
    /// </summary>
    public List<TemplateInfo> Templates { get; set; } = new();

    /// <summary>
    /// فیلتر نوع قالب
    /// </summary>
    public ReportTemplateType? FilterTemplateType { get; set; }
}

/// <summary>
/// مدل تولید گزارش فاکتور تکی
/// </summary>
public class GenerateSingleFactorReportViewModel
{
    /// <summary>
    /// شناسه قالب انتخاب شده
    /// </summary>
    public int TemplateId { get; set; }

    /// <summary>
    /// شناسه فاکتور انتخاب شده
    /// </summary>
    public int FactorId { get; set; }
}

/// <summary>
/// مدل جستجوی فاکتور در مرکز گزارش‌سازی
/// </summary>
public class FactorSearchViewModel
{
    public string? Search { get; set; }
    public string? FromDateJalali { get; set; }
    public string? ToDateJalali { get; set; }
    public int? PersonId { get; set; }
}

/// <summary>
/// نتیجه جستجوی فاکتور
/// </summary>
public class FactorSearchResultItem
{
    public int Id { get; set; }
    public string PersonName { get; set; } = string.Empty;
    public string PersianDate { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public int TotalItems { get; set; }
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
        new() { Value = "PersianCreateDate", Label = "تاریخ شمسی (کامل)", Group = "تاریخ" },
        new() { Value = "date", Label = "تاریخ شمسی (کوتاه)", Group = "تاریخ" },
        new() { Value = "CreateDate", Label = "تاریخ میلادی", Group = "تاریخ" },

        // مشتری
        new() { Value = "PersonName", Label = "نام مشتری", Group = "مشتری" },
        new() { Value = "Buyerdetails", Label = "مشخصات خریدار", Group = "مشتری" },
        new() { Value = "PersonType", Label = "نوع مشتری (حقیقی/حقوقی)", Group = "مشتری" },
        new() { Value = "PersonId", Label = "شناسه مشتری", Group = "مشتری" },

        // مبالغ
        new() { Value = "TotalAmount", Label = "جمع کل (بدون مالیات)", Group = "مبالغ" },
        new() { Value = "sum", Label = "جمع کل", Group = "مبالغ" },
        new() { Value = "Totalup", Label = "جمع بالایی", Group = "مبالغ" },
        new() { Value = "TaxAmount", Label = "مبلغ مالیات", Group = "مبالغ" },
        new() { Value = "tax", Label = "مالیات", Group = "مبالغ" },
        new() { Value = "TaxPercent", Label = "درصد مالیات", Group = "مبالغ" },
        new() { Value = "TotalWithTax", Label = "جمع با مالیات", Group = "مبالغ" },
        new() { Value = "Totalsum", Label = "جمع نهایی", Group = "مبالغ" },

        // تعداد
        new() { Value = "TotalItems", Label = "تعداد آیتم‌ها (سطرها)", Group = "تعداد" },
        new() { Value = "TotalQuantity", Label = "تعداد کل اقلام", Group = "تعداد" },
    };

    public static List<PropertyOption> FactorItemProperties => new()
    {
        // نام و شرح
        new() { Value = "ProductName", Label = "نام کالا/محصول", Group = "نام و شرح" },
        new() { Value = "Descriptionofthepiece", Label = "شرح قلم (نام محصول)", Group = "نام و شرح" },
        new() { Value = "ProductCode", Label = "کد محصول", Group = "نام و شرح" },
        new() { Value = "CategoryName", Label = "دسته‌بندی محصول", Group = "نام و شرح" },

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
        new() { Value = "PackId", Label = "شناسه بسته", Group = "بسته" },

        // شناسه
        new() { Value = "ItemId", Label = "شناسه آیتم", Group = "شناسه" },
        new() { Value = "SalableId", Label = "شناسه محصول", Group = "شناسه" },
    };

    public static List<PropertyOption> PersonProperties => new()
    {
        // نام
        new() { Value = "PersonName", Label = "نام شخص/شرکت", Group = "نام" },
        new() { Value = "Buyerdetails", Label = "مشخصات خریدار", Group = "نام" },

        // نوع
        new() { Value = "PersonType", Label = "نوع (حقیقی/حقوقی)", Group = "نوع" },
        new() { Value = "IsIndividual", Label = "آیا حقیقی است؟", Group = "نوع" },

        // شناسه
        new() { Value = "Id", Label = "شناسه مشتری", Group = "شناسه" },

        // تاریخ
        new() { Value = "PersianCreateDate", Label = "تاریخ شمسی ثبت", Group = "تاریخ" },

        // آمار فاکتور
        new() { Value = "TotalFactors", Label = "تعداد فاکتورها", Group = "آمار" },
        new() { Value = "TotalPurchaseAmount", Label = "مجموع خرید", Group = "آمار" },
    };

    public static List<PropertyOption> ProductProperties => new()
    {
        // نام و کد
        new() { Value = "ProductName", Label = "نام محصول", Group = "نام و کد" },
        new() { Value = "ProductCode", Label = "کد محصول", Group = "نام و کد" },

        // دسته‌بندی
        new() { Value = "CategoryName", Label = "نام دسته‌بندی", Group = "دسته‌بندی" },

        // قیمت
        new() { Value = "CurrentPrice", Label = "قیمت فعلی", Group = "قیمت" },

        // شناسه
        new() { Value = "Id", Label = "شناسه محصول", Group = "شناسه" },

        // تاریخ
        new() { Value = "PersianCreateDate", Label = "تاریخ شمسی ثبت", Group = "تاریخ" },

        // آمار فروش
        new() { Value = "TotalSoldQty", Label = "تعداد فروش رفته", Group = "آمار فروش" },
        new() { Value = "TotalSoldAmount", Label = "مجموع فروش", Group = "آمار فروش" },
    };

    public static List<PropertyOption> ProductCategoryProperties => new()
    {
        new() { Value = "CategoryName", Label = "نام دسته‌بندی", Group = "نام" },
        new() { Value = "Id", Label = "شناسه دسته‌بندی", Group = "شناسه" },
        new() { Value = "ProductCount", Label = "تعداد محصولات", Group = "آمار" },
    };

    public static List<PropertyOption> ProductPackProperties => new()
    {
        new() { Value = "PackName", Label = "نام بسته", Group = "نام" },
        new() { Value = "PackCode", Label = "کد بسته", Group = "نام" },
        new() { Value = "Id", Label = "شناسه بسته", Group = "شناسه" },
        new() { Value = "TotalPackPrice", Label = "مجموع قیمت بسته", Group = "قیمت" },
        new() { Value = "ItemCount", Label = "تعداد اقلام بسته", Group = "آمار" },
    };

    public static List<PropertyOption> GetPropertiesForDataSource(MarkerDataSource dataSource)
    {
        return dataSource switch
        {
            MarkerDataSource.Factor => FactorProperties,
            MarkerDataSource.FactorItem => FactorItemProperties,
            MarkerDataSource.Person => PersonProperties,
            MarkerDataSource.Product => ProductProperties,
            MarkerDataSource.ProductCategory => ProductCategoryProperties,
            MarkerDataSource.ProductPack => ProductPackProperties,
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
