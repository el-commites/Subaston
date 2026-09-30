using System.ComponentModel.DataAnnotations;

namespace Subaston.ViewsModels
{
    public class VerificacionViewsModels
    {
        [Required(ErrorMessage = "El correo es obligatorio.")]
        [EmailAddress(ErrorMessage = "El correo no es válido.")]
        public string? Correo { get; set; }
    }
}
