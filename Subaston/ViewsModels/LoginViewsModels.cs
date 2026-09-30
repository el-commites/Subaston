using System.ComponentModel.DataAnnotations;

namespace Subaston.ViewsModels
{
    public class LoginViewsModels
    {
        [Required(ErrorMessage = "El correo es obligatorio.")]
        [EmailAddress(ErrorMessage = "El correo no es válido.")]
        public string? Correo { get; set; }
        [Required(ErrorMessage = "La contraseña es obligatoria.")]
        [DataType(DataType.Password)]
        public string? Contraseña { get; set; }
        [Display(Name = "Recordarme")]
        public bool Recordarme { get; set; }
    }
}
