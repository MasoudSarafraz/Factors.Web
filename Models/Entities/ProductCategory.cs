using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Factors.Web.Models.Entities;

public class ProductCategory
{
    [Key]
    public int Id { get; set; }

    [Required(ErrorMessage = "نام دسته‌بندی الزامی است")]
    [StringLength(100, ErrorMessage = "نام دسته‌بندی نمی‌تواند بیشتر از 100 کاراکتر باشد")]
    [Column("Name")]
    public string Name { get; set; } = string.Empty;

    [Column("CreateDate")]
    public DateTime CreateDate { get; set; } = DateTime.UtcNow;

    [NotMapped]
    public string PersianCreateDate => PersianDateService.ToPersian(CreateDate);

    // Navigation
    public virtual ICollection<Product> Products { get; set; } = new List<Product>();
}
