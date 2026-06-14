using System.ComponentModel.DataAnnotations;

namespace Factors.Web.Models.ViewModels;

public class CategoryViewModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "نام دسته‌بندی الزامی است")]
    [StringLength(100, ErrorMessage = "نام دسته‌بندی نمی‌تواند بیشتر از 100 کاراکتر باشد")]
    public string Name { get; set; } = string.Empty;

    public string PersianCreateDate { get; set; } = string.Empty;
    public int ProductCount { get; set; }
}

public class CategoryListViewModel
{
    public List<CategoryViewModel> Categories { get; set; } = new();
    public string SearchTerm { get; set; } = string.Empty;
}
