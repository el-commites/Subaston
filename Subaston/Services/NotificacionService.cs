using Subaston.Models.Models;

namespace Subaston.Services
{
    public class NotificacionService
    {
            private readonly Supabase.Client _supabase;

            public NotificacionService(Supabase.Client supabase)
            {
                _supabase = supabase;
            }

            public async Task CrearAsync(
                int idUsuario,
                string titulo,
                string mensaje,
                string tipo,
                string? urlDestino = null)
            {
                await _supabase
                    .From<Notificaciones>()
                    .Insert(new Notificaciones
                    {
                        IdUsuarioFK = idUsuario,
                        Titulo = titulo,
                        Mensaje = mensaje,
                        Tipo = tipo,
                        UrlDestino = urlDestino,
                        Leida = false,
                        FechaCreacion = DateTime.Now
                    });
            }

            public async Task CrearParaAdminsAsync(
                string titulo,
                string mensaje,
                string tipo,
                string? urlDestino = null)
            {
                var adminsResponse = await _supabase
                    .From<Usuarios>()
                    .Where(x => x.Rol == "Admin")
                    .Get();

                foreach (var admin in adminsResponse.Models)
                {
                    if (admin.IdUsuario.HasValue)
                    {
                        await CrearAsync(
                            (int)admin.IdUsuario.Value,
                            titulo,
                            mensaje,
                            tipo,
                            urlDestino);
                    }
                }
            }
    }
}
