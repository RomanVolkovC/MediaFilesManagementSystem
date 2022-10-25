using Microsoft.EntityFrameworkCore;

using System.ComponentModel.DataAnnotations;

namespace MediaFilesManagementSystem.Data;

[Index(nameof(Name), IsUnique = true)]
public class User
{
    public int Id { get; set; }

    [Display(Name = "Введите имя: ")]
    [Required(ErrorMessage = "Требуется указать логин.")]
    [DataType(DataType.Text)]
    [StringLength(30, MinimumLength = 1, ErrorMessage = "Длина имени должна быть от 1 до 30 символов.")]
    public string Name { get; set; }

    [Display(Name = "Введите пароль: ")]
    [Required(ErrorMessage = "Требуется указать пароль.")]
    [DataType(DataType.Password)]
    [StringLength(30, MinimumLength = 10, ErrorMessage = "Длина пароля должна быть от 10 до 30 символов.")]
    public string Password { get; set; }

    [Display(Name = "Введите роль: ")]
    [Required(ErrorMessage = "Требуется указать роль.")]
    [EnumDataType(typeof(Role))]
    public Role Role { get; set; }
}
