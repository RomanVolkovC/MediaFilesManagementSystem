using System.ComponentModel.DataAnnotations;

namespace MediaFilesManagementSystem.Data;

public enum VideoFileState : byte
{
    [Display(Name = "Нет")]
    None,
    [Display(Name = "Добавляется")]
    Adding,
    [Display(Name = "Заменяет")]
    Replacing,
    [Display(Name = "Заменяется")]
    BeingReplaced,
    [Display(Name = "Удаляется")]
    Deleting
}
