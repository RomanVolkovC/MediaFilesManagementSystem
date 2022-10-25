using System.ComponentModel.DataAnnotations;

namespace MediaFilesManagementSystem.Data;

public enum Role : sbyte
{
    [Display(Name = "Пользователь")]
    User,
    [Display(Name = "Администратор")]
    Administrator
}
