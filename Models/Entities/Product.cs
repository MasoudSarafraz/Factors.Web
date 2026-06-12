using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Factors.Web.Models.Entities;

public class Product
{
    [Key]
    public int Id { get; set; }

    [Required(ErrorMessage = "نام محصول الزامی است")]
    [StringLength(100, ErrorMessage = "نام محصول نمی‌تواند بیشتر از 100 کاراکتر باشد")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "کد محصول الزامی است")]
    [StringLength(50, ErrorMessage = "کد محصول نمی‌تواند بیشتر از 50 کاراکتر باشد")]
    public string Code { get; set; } = string.Empty;

    [Required(ErrorMessage = "دسته‌بندی محصول الزامی است")]
    public int CategoryId { get; set; }

    [Column("CreateDate")]
    public DateTime CreateDate { get; set; } = DateTime.UtcNow;

    [NotMapped]
    public string PersianCreateDate => PersianDateService.ToPersian(CreateDate);

    // Navigation
    [ForeignKey(nameof(CategoryId))]
    public virtual ProductCategory? Category { get; set; }

    public virtual ICollection<ProductPrice> ProductPrices { get; set; } = new List<ProductPrice>();
    public virtual ICollection<ProductPackItems> PackItems { get; set; } = new List<ProductPackItems>();
    public virtual ICollection<FactorItems> FactorItems { get; set; } = new List<FactorItems>();
}
