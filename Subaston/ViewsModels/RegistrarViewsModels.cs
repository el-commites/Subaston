using System.ComponentModel.DataAnnotations;

namespace Subaston.ViewsModels
{
    public class RegistrarViewsModels
    {
        [Required(ErrorMessage = "El Nombre es obligatorio.")]
        public string? Nombre { get; set; }
        [Required(ErrorMessage = "El Apodo es obligatorio.")]
        public string? Apodo { get; set; }
        [Required(ErrorMessage = "El Correo es obligatorio.")]
        [EmailAddress(ErrorMessage = "El correo no es válido.")]
        public string? Correo { get; set; }
        [Required(ErrorMessage = "La contraseña es obligatoria.")]
        [StringLength(40, ErrorMessage = "La contraseña debe tener al menos {2} caracteres.", MinimumLength = 6)]
        [DataType(DataType.Password)]
        public string? Contraseña { get; set; }
        [Required(ErrorMessage = "La confirmación de contraseña es obligatoria.")]
        [DataType(DataType.Password)]
        [Compare("Contraseña", ErrorMessage = "Las contraseñas no coinciden.")]
        public string? ConfirmarContraseña { get; set; }
        [Required(ErrorMessage = "tiene que escoger uno es obligatorio.")]
        public string? Rol { get; set; }
        [Required(ErrorMessage = "Es obligatorio para avanzar.")]
        public bool Estaseguro { get; set; }
    }
}
