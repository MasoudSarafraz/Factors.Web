using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Factors.Web.Models.Entities;

public class Factor
{
    [Key]
    public int Id { get; set; }

    [Column("CreateDate")]
    public DateTime CreateDate { get; set; } = DateTime.UtcNow;

    [Required(ErrorMessage = "انتخاب مشتری الزامی است")]
    public int PersonId { get; set; }

    [NotMapped]
    public string PersianCreateDate => PersianDateService.ToPersian(CreateDate);

    [NotMapped]
    public string PersonName => Person?.PersonName ?? "";

    [NotMapped]
    public decimal TotalAmount => FactorItems?.Sum(fi => fi.Price * fi.Qty) ?? 0;

    [NotMapped]
    public int TotalItems => FactorItems?.Count ?? 0;

    // Navigation
    [ForeignKey(nameof(PersonId))]
    public virtual Person? Person { get; set; }

    public virtual ICollection<FactorItems> FactorItems { get; set; } = new List<FactorItems>();
}
