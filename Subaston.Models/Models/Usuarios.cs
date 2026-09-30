using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;
using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;
using ColumnAttribute = Supabase.Postgrest.Attributes.ColumnAttribute;
using TableAttribute = Supabase.Postgrest.Attributes.TableAttribute;

namespace Subaston.Models.Models
{
    /// <summary>
    /// Representa un usuario registrado en la plataforma.
    /// Mapeado a la tabla "Usuarios" en Supabase.
    /// </summary>
    [Table("Usuarios")]
    public class Usuarios : BaseModel
    {
        [PrimaryKey("idUsuario", false)]
        public long? IdUsuario { get; set; }
        [Required]
        [Column("Nombre")]
        public string? Nombre { get; set; }
        [Required]
        [Column("Apodo")]
        public string? Apodo { get; set; }
        [Required]
        [Column("Correo")]
        public string? Correo { get; set; }
        [Required]
        [Column("Contraseña")]
        public string? Contraseña { get; set; }
        [Column("FechaRegistro")]
        public DateTime FechaRegistro { get; set; } = DateTime.Now;
        [Required]
        [Column("Rol")]
        public string? Rol { get; set; }
        [IgnoreDataMember]
        public bool Baneado { get; set; }
        [Column("FotoPerfil")]
        public string? FotoPerfil { get; set; }
        [Column("Verificado")]
        public bool Verificado { get; set; } = false;

        [Column("TokenVerificacion")]
        public string? TokenVerificacion { get; set; }
    }
}