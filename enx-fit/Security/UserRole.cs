using System.ComponentModel.DataAnnotations;

namespace enx_fit.Security;

public enum UserRole
{
    [Display(Name = "Гость")]
    Guest = 0,
    [Display(Name = "Пользователь")]
    User = 1,
    [Display(Name = "Тренер")]
    Trainer = 3,
    [Display(Name = "Модератор")]
    Moderator = 5,
    [Display(Name = "Администратор")]
    Administrator = 8,
    [Display(Name = "Главный администратор")]
    SuperAdministrator = 9
}
