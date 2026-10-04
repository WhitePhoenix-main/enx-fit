using Microsoft.AspNetCore.Identity;

namespace enx_fit.Areas.Identity.Pages.Account.Manage;

public static class AccountMessages
{
    public static string PasswordHint(PasswordOptions rules)
    {
        var requirements = new List<string> { $"не менее {rules.RequiredLength} символов" };
        if (rules.RequireUppercase) requirements.Add("заглавная латинская буква");
        if (rules.RequireLowercase) requirements.Add("строчная латинская буква");
        if (rules.RequireDigit) requirements.Add("цифра");
        if (rules.RequireNonAlphanumeric) requirements.Add("специальный символ");
        if (rules.RequiredUniqueChars > 1) requirements.Add($"не менее {rules.RequiredUniqueChars} разных символов");
        return "Требования: " + string.Join(", ", requirements) + ".";
    }
    public static string PasswordError(IdentityError error, PasswordOptions rules) => error.Code switch {
        "PasswordMismatch" => "Текущий пароль неверен.",
        "PasswordTooShort" => $"Пароль должен содержать не менее {rules.RequiredLength} символов.",
        "PasswordRequiresNonAlphanumeric" => "Добавьте специальный символ, например ! или @.",
        "PasswordRequiresDigit" => "Добавьте хотя бы одну цифру.",
        "PasswordRequiresLower" => "Добавьте строчную латинскую букву.",
        "PasswordRequiresUpper" => "Добавьте заглавную латинскую букву.",
        "PasswordRequiresUniqueChars" => $"Используйте не менее {rules.RequiredUniqueChars} разных символов.",
        _ => "Не удалось сохранить пароль. Проверьте данные и повторите."
    };
}
