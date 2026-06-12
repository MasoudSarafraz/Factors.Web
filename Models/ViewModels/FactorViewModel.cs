using System.ComponentModel.DataAnnotations;

namespace Factors.Web.Models.ViewModels;

public class FactorViewModel
{
    public int Id { get; set; }
    public int PersonId { get; set; }
    public string PersonName { get; set; } = string.Empty;
    public string PersianCreateDate { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public int TotalItems { get; set; }
    public List<FactorItemViewModel> Items { get; set; } = new();
}

public class FactorCreateViewModel
{
    [Required(ErrorMessage = "انتخاب مشتری الزامی است")]
    public int PersonId { get; set; }

    public List<FactorItemViewModel> Items { get; set; } = new();
}

public class FactorItemViewModel
{
    public int Id { get; set; }

    public int? SalableId { get; set; }
    public int? PackId { get; set; }
    public int? ParentId { get; set; }

    [Required(ErrorMessage = "تعداد الزامی است")]
    [Range(1, int.MaxValue, ErrorMessage = "تعداد باید حداقل 1 باشد")]
    public int Qty { get; set; }

    [Required(ErrorMessage = "قیمت الزامی است")]
    [Range(0, double.MaxValue, ErrorMessage = "قیمت نمی‌تواند منفی باشد")]
    public decimal Price { get; set; }

    public string ProductName { get; set; } = string.Empty;
    public decimal TotalPrice => Price * Qty;
}

public class FactorListViewModel
{
    public List<FactorViewModel> Factors { get; set; } = new();
    public string SearchTerm { get; set; } = string.Empty;
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
}
