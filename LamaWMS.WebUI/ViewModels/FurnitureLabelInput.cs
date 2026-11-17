using System.ComponentModel.DataAnnotations;

namespace LamaWMS.WebUI.ViewModels;

public class FurnitureLabelInput
{
    [Required(ErrorMessage = "Укажите наименование")]
    [StringLength(200)]
    [Display(Name = "Наименование")]
    public string Name { get; set; } = "";

    [Required(ErrorMessage = "Укажите комплектацию")]
    [StringLength(200)]
    [Display(Name = "Комплектация")]
    public string Complectation { get; set; } = "";

    [Required(ErrorMessage = "Укажите обозначение")]
    [StringLength(100)]
    [Display(Name = "Обозначение")]
    public string Designation { get; set; } = "";

    [Required(ErrorMessage = "Укажите функциональное назначение")]
    [StringLength(500)]
    [Display(Name = "Функциональное назначение")]
    public string FunctionalPurpose { get; set; } = "";

    [Required(ErrorMessage = "Укажите декларацию")]
    [StringLength(200)]
    [Display(Name = "Декларация соответствия")]
    public string DeclarationNumber { get; set; } = "";

    [Display(Name = "Декларация действует с")]
    public DateOnly DeclarationValidFrom { get; set; }

    [Display(Name = "Декларация действует по")]
    public DateOnly DeclarationValidTo { get; set; }

    [Required(ErrorMessage = "Укажите номер заявки")]
    [StringLength(100)]
    [Display(Name = "Номер заявки")]
    public string RequestNumber { get; set; } = "";

    [Required(ErrorMessage = "Укажите группу ткани")]
    [StringLength(30)]
    [Display(Name = "Группа ткани")]
    public string FabricGroup { get; set; } = "";

    [Display(Name = "Дата изготовления")]
    public DateOnly ManufactureDate { get; set; } = DateOnly.FromDateTime(DateTime.Today);

    [Range(1, 100, ErrorMessage = "Количество: от 1 до 100")]
    [Display(Name = "Количество копий")]
    public int Copies { get; set; } = 1;
}
