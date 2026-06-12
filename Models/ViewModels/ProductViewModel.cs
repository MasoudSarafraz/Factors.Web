using System.ComponentModel.DataAnnotations;

namespace Factors.Web.Models.ViewModels;

public class ProductViewModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "نام محصول الزامی است")]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "کد محصول الزامی است")]
    [StringLength(50)]
    public string Code { get; set; } = string.Empty;

    [Required(ErrorMessage = "دسته‌بندی محصول الزامی است")]
    public int CategoryId { get; set; }

    /// <summary>
    /// قیمت اولیه محصول (اختیاری - فقط در زمان ایجاد)
    /// </summary>
    [Range(0, double.MaxValue, ErrorMessage = "قیمت نمی‌تواند منفی باشد")]
    public decimal? InitialPrice { get; set; }

    public string CategoryName { get; set; } = string.Empty;
    public string PersianCreateDate { get; set; } = string.Empty;
    public List<ProductPriceViewModel> Prices { get; set; } = new();
}

public class ProductPriceViewModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "قیمت الزامی است")]
    [Range(0, double.MaxValue, ErrorMessage = "قیمت نمی‌تواند منفی باشد")]
    public decimal Price { get; set; }

    [Required]
    public string StartTime { get; set; } = string.Empty;

    [Required]
    public string EndTime { get; set; } = string.Empty;

    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
}

public class ProductListViewModel
{
    public List<ProductViewModel> Products { get; set; } = new();
    public List<CategoryViewModel> Categories { get; set; } = new();
    public string SearchTerm { get; set; } = string.Empty;
    public int? FilterCategoryId { get; set; }
}
