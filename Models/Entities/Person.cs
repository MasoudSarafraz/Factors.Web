using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Factors.Web.Models.Entities;

public class Person
{
    [Key]
    public int Id { get; set; }

    [Required(ErrorMessage = "نام شخص الزامی است")]
    [StringLength(150)]
    public string PersonName { get; set; } = string.Empty;

    public bool IsIndividual { get; set; } = true;

    [Column("CreateDate")]
    public DateTime CreateDate { get; set; } = DateTime.UtcNow;

    [NotMapped]
    public string PersonType => IsIndividual ? "حقیقی" : "حقوقی";

    [NotMapped]
    public string PersianCreateDate => PersianDateService.ToPersian(CreateDate);

    // Navigation
    public virtual ICollection<Factor> Factors { get; set; } = new List<Factor>();
}
