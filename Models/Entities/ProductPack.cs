using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Factors.Web.Models.Entities;

public class ProductPack
{
    [Key]
    public int Id { get; set; }

    [Required(ErrorMessage = "نام بسته الزامی است")]
    [StringLength(100)]
    public string PackName { get; set; } = string.Empty;

    [Required(ErrorMessage = "کد بسته الزامی است")]
    [StringLength(50)]
    public string PackCode { get; set; } = string.Empty;

    [Column("CreateDate")]
    public DateTime CreateDate { get; set; } = DateTime.UtcNow;

    [NotMapped]
    public string PersianCreateDate => PersianDateService.ToPersian(CreateDate);

    [NotMapped]
    public decimal TotalPrice => PackItems?.Sum(pi => pi.Price * pi.Qty) ?? 0;

    // Navigation
    public virtual ICollection<ProductPackItems> PackItems { get; set; } = new List<ProductPackItems>();
}
