using System.ComponentModel.DataAnnotations;

namespace Subaston.ViewsModels
{
    public class CrearProductoViewModel
    {
        [Required(ErrorMessage = "Escribe el nombre del producto.")]
        [StringLength(120, ErrorMessage = "El nombre no puede superar 120 caracteres.")]
        public string? NombreDelProducto { get; set; }

        [Required(ErrorMessage = "Describe el producto.")]
        [StringLength(1000, ErrorMessage = "La descripcion no puede superar 1000 caracteres.")]
        public string? Descripcion { get; set; }

        [Required(ErrorMessage = "Captura el precio inicial.")]
        [Range(1, 999999, ErrorMessage = "El precio inicial debe ser mayor a 0.")]
        public decimal? PrecioInicial { get; set; }

        [Required(ErrorMessage = "Selecciona la fecha de cierre.")]
        public DateTime? FechaCierre { get; set; }

        [Required(ErrorMessage = "Selecciona una categoria.")]
        public string? Categoria { get; set; }

        // Imagen
        public IFormFile? Imagen { get; set; }
    }
}

