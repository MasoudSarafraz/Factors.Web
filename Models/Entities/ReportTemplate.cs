using System.ComponentModel.DataAnnotations;

namespace Factors.Web.Models.Entities;

public class ReportTemplate
{
    [Key]
    public int Id { get; set; }

    [Required(ErrorMessage = "نام قالب الزامی است")]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Description { get; set; }

    /// <summary>
    /// مسیر فایل قالب Word روی سرور
    /// </summary>
    [Required]
    public string FilePath { get; set; } = string.Empty;

    /// <summary>
    /// نام فایل اصلی آپلود شده
    /// </summary>
    [MaxLength(300)]
    public string OriginalFileName { get; set; } = string.Empty;

    public DateTime CreateDate { get; set; } = DateTime.UtcNow;

    [NotMapped]
    public string PersianCreateDate => PersianDateService.ToPersian(CreateDate);

    /// <summary>
    /// لیست مارکرهای استخراج شده از قالب
    /// </summary>
    public virtual ICollection<ReportTemplateMarker> Markers { get; set; } = new List<ReportTemplateMarker>();
}

/// <summary>
/// نوع مارکر: تکی (Single) یا لیستی (List)
/// </summary>
public enum MarkerDataType
{
    /// <summary>مارکر تکی - یک مقدار ساده جایگزین می‌شود</summary>
    Single = 0,
    /// <summary>مارکر لیستی - بالای جدول قرار دارد و سطرهای جدول تکرار می‌شوند</summary>
    List = 1
}

/// <summary>
/// منبع داده برای مارکر - از کدام مدل داده پر شود
/// </summary>
public enum MarkerDataSource
{
    /// <summary>فاکتور (اطلاعات اصلی فاکتور)</summary>
    Factor = 0,
    /// <summary>آیتم‌های فاکتور (سطرهای جدول)</summary>
    FactorItem = 1,
    /// <summary>مشتری</summary>
    Person = 2
}

public class ReportTemplateMarker
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int TemplateId { get; set; }

    /// <summary>
    /// نام مارکر در فایل Word (بدون آکولاد) - مثلاً Buyerdetails
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string MarkerName { get; set; } = string.Empty;

    /// <summary>
    /// نوع مارکر: تکی یا لیستی
    /// </summary>
    public MarkerDataType DataType { get; set; } = MarkerDataType.Single;

    /// <summary>
    /// منبع داده: فاکتور، آیتم فاکتور، یا مشتری
    /// </summary>
    public MarkerDataSource DataSource { get; set; } = MarkerDataSource.Factor;

    /// <summary>
    /// نام پراپرتی که مارکر به آن متصل شده - مثلاً PersonName یا TotalAmount
    /// </summary>
    [MaxLength(200)]
    public string? PropertyPath { get; set; }

    /// <summary>
    /// آیا مارکر نقشه‌برداری شده؟
    /// </summary>
    [NotMapped]
    public bool IsMapped => !string.IsNullOrWhiteSpace(PropertyPath);

    // Navigation
    [ForeignKey(nameof(TemplateId))]
    public virtual ReportTemplate? Template { get; set; }
}
