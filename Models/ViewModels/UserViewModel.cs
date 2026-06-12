using System.ComponentModel.DataAnnotations;

namespace Factors.Web.Models.ViewModels;

public class UserViewModel
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public string Roles { get; set; } = string.Empty;
    public string PersianCreateDate { get; set; } = string.Empty;
    public DateTime? LastLoginDate { get; set; }
}

public class UserCreateViewModel
{
    [Required(ErrorMessage = "نام کامل الزامی است")]
    [StringLength(150)]
    public string FullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "نام کاربری الزامی است")]
    [StringLength(50)]
    public string Username { get; set; } = string.Empty;

    [Required(ErrorMessage = "ایمیل الزامی است")]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "رمز عبور الزامی است")]
    [StringLength(100, MinimumLength = 6, ErrorMessage = "رمز عبور باید حداقل 6 کاراکتر باشد")]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    [Required(ErrorMessage = "انتخاب نقش الزامی است")]
    public List<string> SelectedRoles { get; set; } = new();
}

public class UserEditViewModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "نام کامل الزامی است")]
    [StringLength(150)]
    public string FullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "ایمیل الزامی است")]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    public bool IsActive { get; set; }

    [StringLength(100, MinimumLength = 6, ErrorMessage = "رمز عبور باید حداقل 6 کاراکتر باشد")]
    [DataType(DataType.Password)]
    public string? NewPassword { get; set; }

    public List<string> SelectedRoles { get; set; } = new();
}

public class RoleViewModel
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}

public class UserListViewModel
{
    public List<UserViewModel> Users { get; set; } = new();
    public List<RoleViewModel> AvailableRoles { get; set; } = new();
    public string SearchTerm { get; set; } = string.Empty;
}
