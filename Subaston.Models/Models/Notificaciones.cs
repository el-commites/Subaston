using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;
using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;
using ColumnAttribute = Supabase.Postgrest.Attributes.ColumnAttribute;
using TableAttribute = Supabase.Postgrest.Attributes.TableAttribute;


namespace Subaston.Models.Models
{
    [Table("Notificaciones")]
    public class Notificaciones : BaseModel
    {
        [PrimaryKey("idNotificacion")]
        public long IdNotificacion { get; set; }

        [Column("idUsuario_FK")]
        public int IdUsuarioFK { get; set; }

        [Column("Titulo")]
        public string? Titulo { get; set; }

        [Column("Mensaje")]
        public string? Mensaje { get; set; }

        [Column("Tipo")]
        public string? Tipo { get; set; }

        [Column("Leida")]
        public bool Leida { get; set; } = false;

        [Column("UrlDestino")]
        public string? UrlDestino { get; set; }

        [Column("FechaCreacion")]
        public DateTime FechaCreacion { get; set; } = DateTime.Now;
    }
}
