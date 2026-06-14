using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Factors.Web.Models.Entities;

public class ProductPackItems
{
    [Key]
    public int Id { get; set; }

    [Required(ErrorMessage = "تعداد الزامی است")]
    [Range(1, int.MaxValue, ErrorMessage = "تعداد باید حداقل 1 باشد")]
    public int Qty { get; set; }

    [Required(ErrorMessage = "قیمت الزامی است")]
    [Range(0, double.MaxValue, ErrorMessage = "قیمت نمی‌تواند منفی باشد")]
    public decimal Price { get; set; }

    [Column("CreateDate")]
    public DateTime CreateDate { get; set; } = DateTime.UtcNow;

    [Required]
    public int ProductId { get; set; }

    [Required]
    public int PackId { get; set; }

    // Navigation
    [ForeignKey(nameof(ProductId))]
    public virtual Product? Product { get; set; }

    [ForeignKey(nameof(PackId))]
    public virtual ProductPack? Pack { get; set; }

    [NotMapped]
    public string ProductName => Product?.Name ?? "";

    [NotMapped]
    public decimal TotalPrice => Price * Qty;
}
