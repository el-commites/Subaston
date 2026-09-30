using Microsoft.AspNetCore.Mvc;
using MercadoPago.Client.Preference;
using MercadoPago.Client.Payment;
using MercadoPago.Config;
using MercadoPago.Resource.Preference;
using Subaston.Models.Models;
using Microsoft.Extensions.Configuration;
using System.Text.Json;
using Subaston.ViewsModels;
using Subaston.Services;

namespace Subaston.Controllers
{
    public class PagoController : Controller
    {
        private readonly Supabase.Client _supabase;
        private readonly IConfiguration _configuration;
        private readonly NotificacionService _notificacionService;

        public PagoController(Supabase.Client supabase, IConfiguration configuration, NotificacionService notificacionService)
        {
            _supabase = supabase;
            _configuration = configuration;
            _notificacionService = notificacionService;
        }

        public async Task<IActionResult> Pagar(int idProducto)
        {
            var accessToken = _configuration["MercadoPago:AccessToken"];
            if (string.IsNullOrWhiteSpace(accessToken))
            {
                TempData["Error"] = "Mercado Pago no esta configurado. Agrega MercadoPago:AccessToken.";
                return RedirectToAction("Historial", "Comprador", new { area = "Comprador" });
            }

            var correo = HttpContext.Session.GetString("Usuario");
            if (string.IsNullOrEmpty(correo))
                return RedirectToAction("Login", "Cuenta", new { area = "Cuentas" });

            var usuarioResponse = await _supabase
                .From<Usuarios>()
                .Filter("Correo", Supabase.Postgrest.Constants.Operator.Equals, correo)
                .Get();

            var usuario = usuarioResponse.Models.FirstOrDefault();
            if (usuario == null)
            {
                TempData["Error"] = "Usuario no encontrado";
                return RedirectToAction("Historial", "Comprador", new { area = "Comprador" });
            }

            var productoResponse = await _supabase
                .From<Productos>()
                .Where(x => x.IdProductos == idProducto)
                .Get();

            var producto = productoResponse.Models.FirstOrDefault();
            if (producto == null)
            {
                TempData["Error"] = "Producto no encontrado";
                return RedirectToAction("Historial", "Comprador", new { area = "Comprador" });
            }

            if (producto.idGanador_FK != usuario.IdUsuario)
            {
                TempData["Error"] = "No eres el ganador de esta subasta";
                return RedirectToAction("Historial", "Comprador", new { area = "Comprador" });
            }

            if (producto.Estado != "Finalizada")
            {
                TempData["Error"] = "La subasta aún no ha finalizado";
                return RedirectToAction("Detalles", "Comprador", new { area = "Comprador", id = idProducto });
            }

            decimal montoFinal = producto.PujaActual.HasValue
                ? (decimal)producto.PujaActual.Value
                : producto.PrecioInicial;

            MercadoPagoConfig.AccessToken = accessToken;

            var request = new PreferenceRequest
            {
                Items = new List<PreferenceItemRequest>
                {
                    new PreferenceItemRequest
                    {
                        Title = producto.NombreDelProducto ?? "Producto Subastón",
                        Quantity = 1,
                        CurrencyId = "MXN",
                        UnitPrice = montoFinal
                    }
                },
                BackUrls = new PreferenceBackUrlsRequest
                {
                    Success = Url.Action("PagoExitoso", "Pago", new { idProducto }, Request.Scheme),
                    Failure = Url.Action("PagoFallido", "Pago", new { idProducto }, Request.Scheme),
                    Pending = Url.Action("PagoPendiente", "Pago", new { idProducto }, Request.Scheme)
                },
                NotificationUrl = Url.Action("Webhook", "Pago", null, Request.Scheme),
                AutoReturn = "approved",
                ExternalReference = $"{idProducto}-{usuario.IdUsuario}"
            };

            Preference preference;
            try
            {
                var client = new PreferenceClient();
                preference = await client.CreateAsync(request);
            }
            catch
            {
                TempData["Error"] = "No se pudo iniciar el pago con Mercado Pago. Revisa las credenciales.";
                return RedirectToAction("Historial", "Comprador", new { area = "Comprador" });
            }

            bool isSandbox = accessToken.StartsWith("TEST-");
            string? redirectUrl = isSandbox ? preference.SandboxInitPoint : preference.InitPoint;

            if (string.IsNullOrWhiteSpace(redirectUrl))
            {
                TempData["Error"] = "Mercado Pago no devolvio una liga valida de pago.";
                return RedirectToAction("Historial", "Comprador", new { area = "Comprador" });
            }

            return Redirect(redirectUrl);
        }

        public async Task<IActionResult> PagoExitoso(
            int idProducto,
            string? payment_id = null,
            string? status = null,
            string? external_reference = null)
        {
            var accessToken = _configuration["MercadoPago:AccessToken"];
            if (!string.IsNullOrWhiteSpace(accessToken))
                MercadoPagoConfig.AccessToken = accessToken;

            bool pagoAprobado = false;

            if (!string.IsNullOrEmpty(payment_id) && long.TryParse(payment_id, out long mpPaymentId))
            {
                try
                {
                    var paymentClient = new PaymentClient();
                    var pago = await paymentClient.GetAsync(mpPaymentId);
                    pagoAprobado = pago?.Status == "approved";
                }
                catch
                {
                    pagoAprobado = status == "approved";
                }
            }
            else
            {
                pagoAprobado = status == "approved";
            }

            var productoResponse = await _supabase
                .From<Productos>()
                .Where(x => x.IdProductos == idProducto)
                .Get();

            var producto = productoResponse.Models.FirstOrDefault();

            var correo = HttpContext.Session.GetString("Usuario");
            Usuarios? usuario = null;
            if (!string.IsNullOrEmpty(correo))
            {
                var usuarioResponse = await _supabase
                    .From<Usuarios>()
                    .Filter("Correo", Supabase.Postgrest.Constants.Operator.Equals, correo)
                    .Get();
                usuario = usuarioResponse.Models.FirstOrDefault();
            }

            if (pagoAprobado && producto != null)
            {
                if (producto.Estado != "Pagado")
                {
                    producto.Estado = "Pagado";
                    await producto.Update<Productos>();

                    // NOTIFICAR AL COMPRADOR
                    if (usuario?.IdUsuario != null)
                    {
                        await _notificacionService.CrearAsync(
                            (int)usuario.IdUsuario,
                            "Pago confirmado",
                            $"Tu pago por '{producto.NombreDelProducto}' fue procesado. Recógelo en sucursal.",
                            "Pago",
                            "/Comprador/Comprador/Historial"
                        );
                    }

                    // NOTIFICAR AL VENDEDOR
                    if (producto.idVendedor_FK.HasValue)
                    {
                        await _notificacionService.CrearAsync(
                            producto.idVendedor_FK.Value,
                            "Tu producto fue pagado",
                            $"'{producto.NombreDelProducto}' fue pagado. Acércate a sucursal para entregarlo.",
                            "Pago",
                            "/Vendedor/Vendedor/MisProductos"
                        );
                    }

                    // NOTIFICAR A LOS ADMINS
                    await _notificacionService.CrearParaAdminsAsync(
                        "Nuevo pago recibido",
                        $"El producto '{producto.NombreDelProducto}' fue pagado exitosamente.",
                        "Pago",
                        "/Admin/Admin/Estadisticas"
                    );
                }

                var imagenResponse = await _supabase
                    .From<ProductoImagen>()
                    .Filter("idProducto_FK", Supabase.Postgrest.Constants.Operator.Equals, idProducto.ToString())
                    .Get();
                var imagen = imagenResponse.Models.FirstOrDefault();

                var sucursales = new List<SucursalInfo>
                {
                    new SucursalInfo
                    {
                        Nombre    = "Subastón Centro",
                        Direccion = "Calle Catalina Buendía 130, CTM VII Culhuacán, Coyoacán, 04490",
                        Ciudad    = "Ciudad de Mexico",
                        Telefono  = "55 7371 8965",
                        Horario   = "Lun–Vie 9:00–18:00 | Sáb 10:00–14:00",
                        MapUrl    = "https://maps.app.goo.gl/j9kw9j6SDAct5ADU8"
                    },
                    new SucursalInfo
                    {
                        Nombre    = "Subastón Sur",
                        Direccion = "Barrio San Francisco, Tlaxcala 15, San Jeronimo, Magdalena Contreras",
                        Ciudad    = "Ciudad de Mexico",
                        Telefono  = "55 8008 3541",
                        Horario   = "Lun–Vie 9:00–18:00 | Sáb 10:00–14:00",
                        MapUrl    = "https://maps.app.goo.gl/RuaV1YLRS3W8ZwFs6"
                    },
                    new SucursalInfo
                    {
                        Nombre    = "Subastón Centro",
                        Direccion = "Av. 11 62-Local 4, Cerro de la Estrella, Iztapalapa",
                        Ciudad    = "Ciudad de Mexico",
                        Telefono  = "55 6403 1998",
                        Horario   = "Lun–Vie 9:00–18:00 | Sáb 10:00–14:00",
                        MapUrl    = "https://maps.app.goo.gl/ABApySkTMmZZs6vz9"
                    }
                };

                var sucursalCercana = sucursales[idProducto % sucursales.Count];

                var ticket = new TicketViewModel
                {
                    IdProducto = idProducto,
                    NombreProducto = producto.NombreDelProducto ?? "Producto",
                    Categoria = producto.Categoria ?? "General",
                    ImagenUrl = imagen?.UrlImagen ?? "",
                    MontoPagado = producto.PujaActual.HasValue
                                        ? (decimal)producto.PujaActual.Value
                                        : producto.PrecioInicial,
                    NombreComprador = usuario?.Nombre ?? "Comprador",
                    ApodoComprador = usuario?.Apodo ?? "",
                    CorreoComprador = correo ?? "",
                    PaymentId = payment_id ?? "N/A",
                    FechaPago = DateTime.Now,
                    SucursalCercana = sucursalCercana
                };

                return View("TicketCompra", ticket);
            }
            else
            {
                TempData["Info"] = "El pago está siendo procesado o no fue aprobado aún.";
                return RedirectToAction("Historial", "Comprador", new { area = "Comprador" });
            }
        }

        [HttpPost]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> Webhook()
        {
            try
            {
                using var reader = new StreamReader(Request.Body);
                var body = await reader.ReadToEndAsync();

                var doc = JsonDocument.Parse(body);
                var root = doc.RootElement;

                if (!root.TryGetProperty("type", out var typeProp) ||
                    typeProp.GetString() != "payment")
                    return Ok();

                if (!root.TryGetProperty("data", out var dataProp) ||
                    !dataProp.TryGetProperty("id", out var idProp))
                    return Ok();

                var paymentId = idProp.GetInt64();
                var paymentClient = new PaymentClient();
                var pago = await paymentClient.GetAsync(paymentId);

                if (pago?.Status != "approved" || string.IsNullOrEmpty(pago.ExternalReference))
                    return Ok();

                var partes = pago.ExternalReference.Split('-');
                if (partes.Length < 2 ||
                    !int.TryParse(partes[0], out int idProducto) ||
                    !int.TryParse(partes[1], out int idUsuario))
                    return Ok();

                var response = await _supabase
                    .From<Productos>()
                    .Where(x => x.IdProductos == idProducto)
                    .Get();

                var producto = response.Models.FirstOrDefault();
                if (producto != null && producto.Estado != "Pagado")
                {
                    producto.Estado = "Pagado";
                    await producto.Update<Productos>();

                    // NOTIFICAR AL COMPRADOR
                    await _notificacionService.CrearAsync(
                        idUsuario,
                        "Pago confirmado",
                        $"Tu pago por '{producto.NombreDelProducto}' fue procesado. Recógelo en sucursal.",
                        "Pago",
                        "/Comprador/Comprador/Historial"
                    );

                    // NOTIFICAR AL VENDEDOR
                    if (producto.idVendedor_FK.HasValue)
                    {
                        await _notificacionService.CrearAsync(
                            producto.idVendedor_FK.Value,
                            "Tu producto fue pagado",
                            $"'{producto.NombreDelProducto}' fue pagado. Acércate a sucursal para entregarlo.",
                            "Pago",
                            "/Vendedor/Vendedor/MisProductos"
                        );
                    }

                    // NOTIFICAR A LOS ADMINS
                    await _notificacionService.CrearParaAdminsAsync(
                        "Nuevo pago recibido",
                        $"El producto '{producto.NombreDelProducto}' fue pagado exitosamente.",
                        "Pago",
                        "/Admin/Admin/Estadisticas"
                    );
                }
            }
            catch
            {
                // Siempre retornar 200 a Mercado Pago aunque haya error interno
            }

            return Ok();
        }

        public async Task<IActionResult> VerTicket(int idProducto)
        {
            var correo = HttpContext.Session.GetString("Usuario");
            if (string.IsNullOrEmpty(correo))
                return RedirectToAction("Login", "Cuenta", new { area = "Cuentas" });

            var usuarioResponse = await _supabase
                .From<Usuarios>()
                .Filter("Correo", Supabase.Postgrest.Constants.Operator.Equals, correo)
                .Get();
            var usuario = usuarioResponse.Models.FirstOrDefault();

            if (usuario == null)
            {
                TempData["Error"] = "Usuario no encontrado";
                return RedirectToAction("Historial", "Comprador", new { area = "Comprador" });
            }

            var productoResponse = await _supabase
                .From<Productos>()
                .Where(x => x.IdProductos == idProducto)
                .Get();
            var producto = productoResponse.Models.FirstOrDefault();

            if (producto == null)
            {
                TempData["Error"] = "Producto no encontrado";
                return RedirectToAction("Historial", "Comprador", new { area = "Comprador" });
            }

            if (producto.idGanador_FK != usuario.IdUsuario)
            {
                TempData["Error"] = "No eres el ganador de esta subasta";
                return RedirectToAction("Historial", "Comprador", new { area = "Comprador" });
            }

            if (producto.Estado != "Pagado")
            {
                TempData["Error"] = "El producto aún no ha sido pagado";
                return RedirectToAction("Historial", "Comprador", new { area = "Comprador" });
            }

            var imagenResponse = await _supabase
                .From<ProductoImagen>()
                .Filter("idProducto_FK", Supabase.Postgrest.Constants.Operator.Equals, idProducto.ToString())
                .Get();
            var imagen = imagenResponse.Models.FirstOrDefault();

            var sucursales = new List<SucursalInfo>
            {
                new SucursalInfo
                {
                    Nombre    = "Subastón Centro",
                    Direccion = "Calle Catalina Buendía 130, CTM VII Culhuacán, Coyoacán, 04490",
                    Ciudad    = "Ciudad de Mexico",
                    Telefono  = "55 7371 8965",
                    Horario   = "Lun–Vie 9:00–18:00 | Sáb 10:00–14:00",
                    MapUrl    = "https://maps.app.goo.gl/j9kw9j6SDAct5ADU8"
                },
                new SucursalInfo
                {
                    Nombre    = "Subastón Sur",
                    Direccion = "Barrio San Francisco, Tlaxcala 15, San Jeronimo, Magdalena Contreras",
                    Ciudad    = "Ciudad de Mexico",
                    Telefono  = "55 8008 3541",
                    Horario   = "Lun–Vie 9:00–18:00 | Sáb 10:00–14:00",
                    MapUrl    = "https://maps.app.goo.gl/RuaV1YLRS3W8ZwFs6"
                },
                new SucursalInfo
                {
                    Nombre    = "Subastón Centro",
                    Direccion = "Av. 11 62-Local 4, Cerro de la Estrella, Iztapalapa",
                    Ciudad    = "Ciudad de Mexico",
                    Telefono  = "55 6403 1998",
                    Horario   = "Lun–Vie 9:00–18:00 | Sáb 10:00–14:00",
                    MapUrl    = "https://maps.app.goo.gl/ABApySkTMmZZs6vz9"
                }
            };

            var sucursalCercana = sucursales[idProducto % sucursales.Count];

            var ticket = new TicketViewModel
            {
                IdProducto = idProducto,
                NombreProducto = producto.NombreDelProducto ?? "Producto",
                Categoria = producto.Categoria ?? "General",
                ImagenUrl = imagen?.UrlImagen ?? "",
                MontoPagado = producto.PujaActual.HasValue
                                    ? (decimal)producto.PujaActual.Value
                                    : producto.PrecioInicial,
                NombreComprador = usuario?.Nombre ?? "Comprador",
                ApodoComprador = usuario?.Apodo ?? "",
                CorreoComprador = correo ?? "",
                PaymentId = "Reimpresión",
                FechaPago = producto.FechaCierre,
                SucursalCercana = sucursalCercana
            };

            return View("TicketCompra", ticket);
        }

        public IActionResult PagoFallido(int idProducto)
        {
            TempData["Error"] = "El pago no pudo procesarse. Intenta de nuevo.";
            return RedirectToAction("Historial", "Comprador", new { area = "Comprador" });
        }

        public IActionResult PagoPendiente(int idProducto)
        {
            TempData["Info"] = "Tu pago está pendiente de confirmación.";
            return RedirectToAction("Historial", "Comprador", new { area = "Comprador" });
        }
    }
}