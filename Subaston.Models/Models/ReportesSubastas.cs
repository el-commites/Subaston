using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;
using System.Runtime.Serialization;
using ColumnAttribute = Supabase.Postgrest.Attributes.ColumnAttribute;
using TableAttribute = Supabase.Postgrest.Attributes.TableAttribute;

namespace Subaston.Models.Models
{
    [Table("ReportesSubastas")]
    public class ReportesSubastas : BaseModel
    {
        [PrimaryKey("idReporte", false)]
        public int IdReporte { get; set; }

        [Column("idProducto_FK")]
        public int idProducto_FK { get; set; }

        [Column("idUsuario_FK")]
        public int idUsuario_FK { get; set; }

        [Column("Motivo")]
        public string? Motivo { get; set; }

        [Column("Comentario")]
        public string? Comentario { get; set; }

        [Column("Estado")]
        public string? Estado { get; set; }

        [Column("FechaReporte")]
        public DateTime FechaReporte { get; set; } = DateTime.Now;

        [IgnoreDataMember]
        public Productos? Producto { get; set; }

        [IgnoreDataMember]
        public Usuarios? Usuario { get; set; }
    }
}
