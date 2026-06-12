using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Factors.Web.Models.Entities;

public class FactorItems
{
    [Key]
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

    [Required]
    public int FactorId { get; set; }

    [NotMapped]
    public string ProductName { get; set; } = string.Empty;

    [NotMapped]
    public decimal TotalPrice => Price * Qty;

    // Navigation
    [ForeignKey(nameof(FactorId))]
    public virtual Factor? Factor { get; set; }

    [ForeignKey(nameof(SalableId))]
    public virtual Product? Product { get; set; }

    [ForeignKey(nameof(PackId))]
    public virtual ProductPack? ProductPack { get; set; }
}
