using System.ComponentModel.DataAnnotations;

namespace Subaston.ViewsModels
{
    public class CambioContraViewsModels
    {
        [Required(ErrorMessage = "El Correo es obligatorio.")]
        [EmailAddress(ErrorMessage = "El correo no es válido.")]
        public string? Correo { get; set; }
        [Required(ErrorMessage = "La contraseña es obligatoria.")]
        [StringLength(40, ErrorMessage = "La contraseña debe tener al menos {2} caracteres.", MinimumLength = 6)]
        [DataType(DataType.Password)]
        [Display(Name = "Nueva Contraseña")]
        [Compare("ConfirmarNuevaContraseña", ErrorMessage = "Las contraseñas no coinciden.")]
        public string? NuevaContraseña { get; set; }
        [Required(ErrorMessage = "La confirmación de contraseña es obligatoria.")]
        [DataType(DataType.Password)]
        [Display(Name = "Confirmar Nueva Contraseña")]
        public string? ConfirmarNuevaContraseña { get; set; }

        public string? Token { get; set; }
    }
}
