using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;
using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;
using ColumnAttribute = Supabase.Postgrest.Attributes.ColumnAttribute;
using TableAttribute = Supabase.Postgrest.Attributes.TableAttribute;

namespace Subaston.Models.Models
{
    [Table("HistorialPujas")]
    public class HistorialPujas : BaseModel
    {
        [PrimaryKey("idHistorial", false)]
        public int idHistorial { get; set; }

        [Column("idProducto_FK")]
        public int idProducto_FK { get; set; }

        [Column("idUsuario_FK")]
        public int idUsuario_FK { get; set; }

        [Column("Monto")]
        public decimal Monto { get; set; }

        [Column("FechaPuja")]
        public DateTime FechaPuja { get; set; } = DateTime.Now;
    }
}
