using System.ComponentModel.DataAnnotations;

namespace Factors.Web.Models.ViewModels;

public class PackViewModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "نام بسته الزامی است")]
    [StringLength(100)]
    public string PackName { get; set; } = string.Empty;

    [Required(ErrorMessage = "کد بسته الزامی است")]
    [StringLength(50)]
    public string PackCode { get; set; } = string.Empty;

    public string PersianCreateDate { get; set; } = string.Empty;
    public decimal TotalPrice { get; set; }
    public List<PackItemViewModel> Items { get; set; } = new();
}

public class PackItemViewModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "محصول الزامی است")]
    public int ProductId { get; set; }

    public string ProductName { get; set; } = string.Empty;

    [Required(ErrorMessage = "تعداد الزامی است")]
    [Range(1, int.MaxValue, ErrorMessage = "تعداد باید حداقل 1 باشد")]
    public int Qty { get; set; }

    [Required(ErrorMessage = "قیمت الزامی است")]
    [Range(0, double.MaxValue, ErrorMessage = "قیمت نمی‌تواند منفی باشد")]
    public decimal Price { get; set; }

    public int PackId { get; set; }
    public decimal TotalPrice => Price * Qty;
}

public class PackListViewModel
{
    public List<PackViewModel> Packs { get; set; } = new();
    public string SearchTerm { get; set; } = string.Empty;
}
