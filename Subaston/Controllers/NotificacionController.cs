using Microsoft.AspNetCore.Mvc;
using Subaston.Models.Models;

namespace Subaston.Controllers
{
    public class NotificacionesController : Controller
    {
        private readonly Supabase.Client _supabase;

        public NotificacionesController(Supabase.Client supabase)
        {
            _supabase = supabase;
        }

        [HttpGet]
        public async Task<IActionResult> Obtener()
        {
            var idUsuario = HttpContext.Session.GetInt32("IdUsuario");
            if (!idUsuario.HasValue)
                return Json(new List<object>());

            var response = await _supabase
                .From<Notificaciones>()
                .Where(x => x.IdUsuarioFK == idUsuario.Value)
                .Order(x => x.FechaCreacion, Supabase.Postgrest.Constants.Ordering.Descending)
                .Limit(20)
                .Get();

            var data = response.Models.Select(n => new
            {
                id = n.IdNotificacion,
                titulo = n.Titulo,
                mensaje = n.Mensaje,
                tipo = n.Tipo,
                leida = n.Leida,
                urlDestino = n.UrlDestino,
                // Mandamos ISO con indicador UTC para que JS lo parsee correctamente
                fecha = DateTime.SpecifyKind(n.FechaCreacion, DateTimeKind.Utc).ToString("o")
            });

            return Json(data);
        }

        [HttpGet]
        public async Task<IActionResult> ConteoNuevas()
        {
            var idUsuario = HttpContext.Session.GetInt32("IdUsuario");
            if (!idUsuario.HasValue)
                return Json(new { total = 0 });

            var response = await _supabase
                .From<Notificaciones>()
                .Where(x => x.IdUsuarioFK == idUsuario.Value)
                .Where(x => x.Leida == false)
                .Get();

            return Json(new { total = response.Models.Count });
        }

        [HttpPost]
        public async Task<IActionResult> MarcarComoLeidas()
        {
            var idUsuario = HttpContext.Session.GetInt32("IdUsuario");
            if (!idUsuario.HasValue)
                return Json(new { ok = false });

            var response = await _supabase
                .From<Notificaciones>()
                .Where(x => x.IdUsuarioFK == idUsuario.Value)
                .Where(x => x.Leida == false)
                .Get();

            if (response.Models.Any())
            {
                // Actualizar todas de una sola vez en lugar de una por una
                await _supabase
                    .From<Notificaciones>()
                    .Where(x => x.IdUsuarioFK == idUsuario.Value)
                    .Where(x => x.Leida == false)
                    .Set(x => x.Leida, true)
                    .Update();
            }

            return Json(new { ok = true });
        }
    }
}