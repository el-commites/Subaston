using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;
using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;
using ColumnAttribute = Supabase.Postgrest.Attributes.ColumnAttribute;
using TableAttribute = Supabase.Postgrest.Attributes.TableAttribute;
namespace Subaston.Models.Models
{
    [Table("Productos")]
    public class Productos : BaseModel
    {
        [PrimaryKey("idProductos")]
        public int IdProductos { get; set; }
        [Required]
        [Column("NombreDelProducto")]
        public string? NombreDelProducto { get; set; }
        [Column("Descripcion")]
        public string? Descripcion { get; set; }
        [Column("PrecioInicial")]
        public decimal PrecioInicial { get; set; }
        [Column("PujaActual")]
        public int? PujaActual { get; set; }
        [Column("PujadorActual")]
        public int? PujadorActual { get; set; }
        [IgnoreDataMember]
        public string? PujadorActualApodo { get; set; }
        [Column("TotalPujas")]
        public int? TotalPujas { get; set; }
        [Column("FechaCierre")]
        public DateTime FechaCierre { get; set; }
        [Column("Categoria")]
        public string? Categoria { get; set; }
        [Column("Estado")]
        public string? Estado { get; set; }
        [IgnoreDataMember]
        public string? ImagenUrl { get; set; }
        [Column("idVendedor_FK")]
        public int? idVendedor_FK { get; set; }

        [Column("idGanador_FK")]
        public int? idGanador_FK { get; set; }

        [IgnoreDataMember]
        public string? VendedorApodo { get; set; }

        [IgnoreDataMember]
        public string? VendedorFoto { get; set; }
    }
}