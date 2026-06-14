using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Factors.Web.Models.Entities;

public class ProductPrice
{
    [Key]
    public int Id { get; set; }

    [Required]
    public DateTime StartTime { get; set; }

    [Required]
    public DateTime EndTime { get; set; }

    [Required(ErrorMessage = "قیمت الزامی است")]
    [Range(0, double.MaxValue, ErrorMessage = "قیمت نمی‌تواند منفی باشد")]
    public decimal Price { get; set; }

    [Required]
    public int ProductId { get; set; }

    [Column("CreateDate")]
    public DateTime CreateDate { get; set; } = DateTime.UtcNow;

    [NotMapped]
    public string PersianStartDate => PersianDateService.ToPersian(StartTime);
    
    [NotMapped]
    public string PersianEndDate => PersianDateService.ToPersian(EndTime);

    // Navigation
    [ForeignKey(nameof(ProductId))]
    public virtual Product? Product { get; set; }
}
