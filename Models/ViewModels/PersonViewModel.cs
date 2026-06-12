using System.ComponentModel.DataAnnotations;

namespace Factors.Web.Models.ViewModels;

public class PersonViewModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "نام شخص الزامی است")]
    [StringLength(150)]
    public string PersonName { get; set; } = string.Empty;

    public bool IsIndividual { get; set; } = true;
    public string PersonType => IsIndividual ? "حقیقی" : "حقوقی";
    public string PersianCreateDate { get; set; } = string.Empty;
    public int FactorCount { get; set; }
}

public class PersonListViewModel
{
    public List<PersonViewModel> Persons { get; set; } = new();
    public string SearchTerm { get; set; } = string.Empty;
}
