using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;
using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;
using ColumnAttribute = Supabase.Postgrest.Attributes.ColumnAttribute;
using TableAttribute = Supabase.Postgrest.Attributes.TableAttribute;

namespace Subaston.Models.Models
{
    [Table("ProductoImagen")]
    public class ProductoImagen : BaseModel
    {
        [PrimaryKey("idImagen")]
        public int IdImagen { get; set; }

        [Column("idProducto_FK")]
        public int IdProductoFK { get; set; }

        [Column("UrlImagen")]
        public string? UrlImagen { get; set; }
    }
}
