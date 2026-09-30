using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Subaston.Models;
using Subaston.Models.Models;
using Subaston.ViewsModels;
using Subaston.Services;

namespace Subaston.Areas.Comprador.Controllers
{
    [Area("Comprador")]
    public class CompradorController : Controller
    {
        private readonly NotificacionService _notificacionService;
        private readonly Supabase.Client _supabase;
        private readonly IWebHostEnvironment _env;
        public CompradorController(Supabase.Client supabase, IWebHostEnvironment env, NotificacionService notificacionService)
        {
            _supabase = supabase;
            _env = env;
            _notificacionService = notificacionService;
        }

        //FUNCIONES
        public IActionResult MisFunciones()
        {
            return View();
        }
        public IActionResult RazonesBan()
        {
            return View();
        }

        // INDEX
        public async Task<IActionResult> Index(string buscar)
        {
            var response = await _supabase
                .From<Productos>()
                .Get();

            var productos = response.Models;

            // FINALIZAR SUBASTAS
            foreach (var producto in productos)
            {
                if (producto.FechaCierre.ToUniversalTime() < DateTime.UtcNow &&
                    producto.Estado != "Finalizada" &&
                    producto.Estado != "Pagado")
                {
                    var pujasResponse = await _supabase
                        .From<HistorialPujas>()
                        .Filter(
                            "idProducto_FK",
                            Supabase.Postgrest.Constants.Operator.Equals,
                            producto.IdProductos.ToString())
                        .Order(
                            "Monto",
                            Supabase.Postgrest.Constants.Ordering.Descending)
                        .Limit(1)
                        .Get();

                    var ultimaPuja =
                        pujasResponse.Models.FirstOrDefault();

                    // GUARDAR GANADOR
                    if (ultimaPuja != null)
                    {
                        producto.idGanador_FK =
                            ultimaPuja.idUsuario_FK;
                        await _notificacionService.CrearAsync(
                             ultimaPuja.idUsuario_FK,
                             "¡Ganaste la subasta!",
                             $"Ganaste '{producto.NombreDelProducto}'. Ya puedes realizar el pago.",
                             "Ganador",
                             "/Comprador/Comprador/Historial"
                        );

                    }
                    // NOTIFICAR AL VENDEDOR
                    if (producto.idVendedor_FK.HasValue)
                    {
                        await _notificacionService.CrearAsync(
                            producto.idVendedor_FK.Value,
                            "Tu subasta finalizó",
                            $"La subasta de '{producto.NombreDelProducto}' ha terminado. Espera el pago del ganador.",
                            "Subasta",
                            "/Vendedor/Vendedor/MisProductos"
                        );
                    }

                    producto.Estado = "Finalizada";

                    await producto.Update<Productos>();
                }
            }

            // SEPARAR ACTIVAS (no pisamos la lista completa)
            var activos = productos
                .Where(x => x.Estado == "Aceptado")
                .ToList();

            // BUSCADOR (solo aplica sobre las activas)
            if (!string.IsNullOrEmpty(buscar))
            {
                activos = activos
                    .Where(x =>
                        x.NombreDelProducto != null &&
                        x.NombreDelProducto
                            .ToLower()
                            .Contains(buscar.ToLower()))
                    .ToList();
                ViewBag.Buscar = buscar;
            }

            // FINALIZADAS
            var finalizados = productos
                .Where(x => x.Estado == "Finalizada")
                .ToList();

            // LISTA QUE SE MANDA A LA VISTA (activas + finalizadas)
            var paraMostrar = activos.Concat(finalizados).ToList();

            // IMÁGENES Y VENDEDOR
            foreach (var producto in paraMostrar)
            {
                var imagenResponse = await _supabase
                    .From<ProductoImagen>()
                    .Filter(
                        "idProducto_FK",
                        Supabase.Postgrest.Constants.Operator.Equals,
                        producto.IdProductos.ToString())
                    .Get();

                var imagen =
                    imagenResponse.Models.FirstOrDefault();

                if (imagen != null)
                {
                    producto.ImagenUrl =
                        imagen.UrlImagen;
                }

                if (producto.idVendedor_FK != null)
                {
                    var vendedorResponse = await _supabase
                        .From<Usuarios>()
                        .Filter(
                            "idUsuario",
                            Supabase.Postgrest.Constants.Operator.Equals,
                            producto.idVendedor_FK.ToString())
                        .Get();

                    var vendedor = vendedorResponse.Models.FirstOrDefault();
                    if (vendedor != null)
                    {
                        producto.VendedorApodo = vendedor.Apodo;
                        producto.VendedorFoto = vendedor.FotoPerfil;
                    }
                }
            }

            return View(paraMostrar);
        }

        // DETALLES
        public async Task<IActionResult> Detalles(int id)
        {
            var response = await _supabase
                .From<Productos>()
                .Filter(
                    "idProductos",
                    Supabase.Postgrest.Constants.Operator.Equals,
                    id.ToString())
                .Get();

            var producto =
                response.Models.FirstOrDefault();

            if (producto == null)
            {
                return RedirectToAction("Index");
            }

            // IMAGEN
            var imagenResponse = await _supabase
                .From<ProductoImagen>()
                .Filter(
                    "idProducto_FK",
                    Supabase.Postgrest.Constants.Operator.Equals,
                    producto.IdProductos.ToString())
                .Get();

            var imagen =
                imagenResponse.Models.FirstOrDefault();

            if (imagen != null)
            {
                producto.ImagenUrl =
                    imagen.UrlImagen;
            }

            // USUARIO LOGEADO
            var correo =
                HttpContext.Session.GetString("Usuario");

            if (!string.IsNullOrEmpty(correo))
            {
                var usuarioResponse = await _supabase
                    .From<Usuarios>()
                    .Filter(
                        "Correo",
                        Supabase.Postgrest.Constants.Operator.Equals,
                        correo)
                    .Get();

                var usuario =
                    usuarioResponse.Models.FirstOrDefault();

                if (usuario != null)
                {
                    HttpContext.Session.SetInt32(
                        "IdUsuario",
                        (int)usuario.IdUsuario);

                    // VALIDAR SI VA GANANDO
                    if (producto.PujadorActual ==
                        usuario.IdUsuario)
                    {
                        ViewBag.VasGanando = true;
                    }
                    else
                    {
                        ViewBag.VasGanando = false;
                    }
                }
            }

            // GANADOR ACTUAL
            if (producto.PujadorActual != null)
            {
                var ganadorResponse = await _supabase
                    .From<Usuarios>()
                    .Filter(
                        "idUsuario",
                        Supabase.Postgrest.Constants.Operator.Equals,
                        producto.PujadorActual.ToString())
                    .Get();

                var ganador =
                    ganadorResponse.Models.FirstOrDefault();

                if (ganador != null)
                {
                    ViewBag.GanadorActual =
                        ganador.Apodo;
                    ViewBag.GanadorFoto =
                        ganador.FotoPerfil;
                }
            }

            // VENDEDOR
            if (producto.idVendedor_FK != null)
            {
                var vendedorResponse = await _supabase
                    .From<Usuarios>()
                    .Filter(
                        "idUsuario",
                        Supabase.Postgrest.Constants.Operator.Equals,
                        producto.idVendedor_FK.ToString())
                    .Get();

                var vendedor = vendedorResponse.Models.FirstOrDefault();
                if (vendedor != null)
                {
                    ViewBag.VendedorNombre = vendedor.Apodo;
                    ViewBag.VendedorFoto = vendedor.FotoPerfil;
                }
            }

            // TIEMPO RESTANTE
            var tiempoRestante =
                producto.FechaCierre.ToUniversalTime() - DateTime.UtcNow;

            if (tiempoRestante.TotalSeconds > 0)
            {
                ViewBag.TiempoRestante =
                    $"{tiempoRestante.Days}d " +
                    $"{tiempoRestante.Hours}h " +
                    $"{tiempoRestante.Minutes}m";
            }
            else
            {
                ViewBag.TiempoRestante =
                    "Subasta finalizada";
            }

            return View(producto);
        }


        // PUJAR
        [HttpPost]
        public async Task<IActionResult> Pujar(
            int idProducto,
            decimal nuevaPuja)
        {
            // PRODUCTO
            var response = await _supabase
                .From<Productos>()
                .Filter(
                    "idProductos",
                    Supabase.Postgrest.Constants.Operator.Equals,
                    idProducto.ToString())
                .Get();

            var producto =
                response.Models.FirstOrDefault();

            if (producto == null)
            {
                TempData["Error"] =
                    "Producto no encontrado";

                return RedirectToAction("Index");
            }

            // VALIDAR FECHA
            if (producto.FechaCierre.ToUniversalTime() < DateTime.UtcNow)
            {
                TempData["Error"] =
                    "La subasta ya terminó";

                return RedirectToAction(
                    "Detalles",
                    new { id = idProducto });
            }

            // VALIDAR PUJA
            if (nuevaPuja <= producto.PujaActual)
            {
                TempData["Error"] =
                    "La puja debe ser mayor";

                return RedirectToAction(
                    "Detalles",
                    new { id = idProducto });
            }

            // USUARIO LOGEADO
            var correo =
                HttpContext.Session.GetString("Usuario");

            if (string.IsNullOrEmpty(correo))
            {
                return RedirectToAction(
                    "Login",
                    "Cuenta",
                    new { area = "Cuentas" });
            }

            // USUARIO
            var usuarioResponse = await _supabase
                .From<Usuarios>()
                .Filter(
                    "Correo",
                    Supabase.Postgrest.Constants.Operator.Equals,
                    correo)
                .Get();

            var usuario =
                usuarioResponse.Models.FirstOrDefault();

            if (usuario == null)
            {
                return RedirectToAction("Index");
            }

            // NO PUJAR TU PRODUCTO
            if (producto.idVendedor_FK ==
                usuario.IdUsuario)
            {
                TempData["Error"] =
                    "No puedes pujar tu producto";

                return RedirectToAction(
                    "Detalles",
                    new { id = idProducto });
            }

            // ACTUALIZAR PRODUCTO

            var pujadorAnterior = producto.PujadorActual;

            producto.PujaActual =
                (int?)nuevaPuja;

            producto.PujadorActual =
                (int?)usuario.IdUsuario;

            producto.TotalPujas += 1;

            await producto.Update<Productos>();

            // GUARDAR HISTORIAL
            await _supabase
                .From<HistorialPujas>()
                .Insert(new HistorialPujas
                {
                    idProducto_FK =
                        producto.IdProductos,

                    idUsuario_FK =
                        (int)usuario.IdUsuario,

                    Monto = nuevaPuja,

                    FechaPuja = DateTime.Now
                });

            if (pujadorAnterior.HasValue && pujadorAnterior.Value != usuario.IdUsuario)
            {
                await _notificacionService.CrearAsync(
                    pujadorAnterior.Value,
                    "Superaron tu puja",
                    $"Alguien hizo una oferta mayor en '{producto.NombreDelProducto}'.",
                    "Subasta",
                    "/Comprador/Comprador/Detalles/" + producto.IdProductos
                );
            }

            TempData["Correcto"] =
                "Puja realizada correctamente";

            return RedirectToAction(
                "Detalles",
                new { id = idProducto });
        }

        [HttpPost]
        public async Task<IActionResult> Reportar(int idProducto, string motivo, string? comentario, string? regresar)
        {
            if (string.IsNullOrWhiteSpace(motivo))
            {
                TempData["Error"] = "Escribe el motivo del reporte.";
                return regresar == "Index"
                    ? RedirectToAction("Index")
                    : RedirectToAction("Detalles", new { id = idProducto });
            }

            var correo = HttpContext.Session.GetString("Usuario");

            if (string.IsNullOrEmpty(correo))
            {
                return RedirectToAction(
                    "Login",
                    "Cuenta",
                    new { area = "Cuentas" });
            }

            var usuarioResponse = await _supabase
                .From<Usuarios>()
                .Filter(
                    "Correo",
                    Supabase.Postgrest.Constants.Operator.Equals,
                    correo)
                .Get();

            var usuario = usuarioResponse.Models.FirstOrDefault();

            if (usuario == null)
            {
                TempData["Error"] = "No se pudo identificar tu usuario para levantar el reporte.";
                return regresar == "Index"
                    ? RedirectToAction("Index")
                    : RedirectToAction("Detalles", new { id = idProducto });
            }

            await _supabase
                .From<ReportesSubastas>()
                .Insert(new ReportesSubastas
                {
                    idProducto_FK = idProducto,
                    idUsuario_FK = (int)usuario.IdUsuario,
                    Motivo = motivo,
                    Comentario = comentario,
                    Estado = "Pendiente",
                    FechaReporte = DateTime.Now
                });
            await _notificacionService.CrearParaAdminsAsync(
                "Nuevo reporte",
                "Un comprador reportó una subasta.",
                "Reporte",
                "/Admin/Admin/ReportesSubastas"
            );

            TempData["Correcto"] = "Reporte enviado. Un administrador revisara la subasta.";
            return regresar == "Index"
                ? RedirectToAction("Index")
                : RedirectToAction("Detalles", new { id = idProducto });
        }

        // HISTORIAL
        public async Task<IActionResult> Historial()
        {
            // USUARIO LOGEADO
            var correo =
                HttpContext.Session.GetString("Usuario");

            if (string.IsNullOrEmpty(correo))
            {
                return RedirectToAction(
                    "Login",
                    "Cuenta",
                    new { area = "Cuentas" });
            }

            // USUARIO
            var usuarioResponse = await _supabase
                .From<Usuarios>()
                .Filter(
                    "Correo",
                    Supabase.Postgrest.Constants.Operator.Equals,
                    correo)
                .Get();

            var usuario =
                usuarioResponse.Models.FirstOrDefault();

            if (usuario == null)
            {
                return RedirectToAction("Index");
            }

            // GUARDAR ID EN SESSION
            HttpContext.Session.SetInt32(
                "IdUsuario",
                (int)usuario.IdUsuario);

            // HISTORIAL
            var historialResponse = await _supabase
                .From<HistorialPujas>()
                .Filter(
                    "idUsuario_FK",
                    Supabase.Postgrest.Constants.Operator.Equals,
                    usuario.IdUsuario.ToString())
                .Get();

            var historial =
                historialResponse.Models;

            // PRODUCTOS
            var productosResponse = await _supabase
                .From<Productos>()
                .Get();

            var productos =
                productosResponse.Models;

            // CARGAR IMÁGENES
            foreach (var producto in productos)
            {
                var imagenResponse = await _supabase
                    .From<ProductoImagen>()
                    .Filter(
                        "idProducto_FK",
                        Supabase.Postgrest.Constants.Operator.Equals,
                        producto.IdProductos.ToString())
                    .Get();

                var imagen =
                    imagenResponse.Models.FirstOrDefault();

                if (imagen != null)
                {
                    producto.ImagenUrl =
                        imagen.UrlImagen;
                }
            }

            // VIEWMODEL - agrupar por producto, mostrar la puja más alta del usuario por producto
            var datos = historial
                .GroupBy(h => h.idProducto_FK)
                .Select(g =>
                {
                    var mejorPuja = g.OrderByDescending(h => h.Monto).First();
                    var producto = productos.FirstOrDefault(p => p.IdProductos == g.Key);
                    return new HistorialViewModel
                    {
                        Productos = producto,
                        Monto = mejorPuja.Monto,
                        FechaPuja = mejorPuja.FechaPuja,
                        Ganada = producto?.idGanador_FK == usuario.IdUsuario
                    };
                })
                .Where(h => h.Productos != null)
                .OrderByDescending(h => h.FechaPuja)
                .ToList();

            return View(datos);
        }

        // FINALIZADAS
        public async Task<IActionResult> Finalizadas()
        {
            var response = await _supabase
                .From<Productos>()
                .Filter(
                    "Estado",
                    Supabase.Postgrest.Constants.Operator.Equals,
                    "Finalizada")
                .Get();

            var productos = response.Models;

            // IMÁGENES
            foreach (var producto in productos)
            {
                var imagenResponse = await _supabase
                    .From<ProductoImagen>()
                    .Filter(
                        "idProducto_FK",
                        Supabase.Postgrest.Constants.Operator.Equals,
                        producto.IdProductos.ToString())
                    .Get();

                var imagen =
                    imagenResponse.Models.FirstOrDefault();

                if (imagen != null)
                {
                    producto.ImagenUrl =
                        imagen.UrlImagen;
                }
            }

            return View(productos);
        }

        // ---- ACCIÓN: Ver mi perfil ----
        public async Task<IActionResult> MiPerfil()
        {
            var idUsuario = HttpContext.Session.GetInt32("IdUsuario");
            if (!idUsuario.HasValue)
                return RedirectToAction("Login", "Cuenta", new { area = "Cuentas" });

            var response = await _supabase
                .From<Usuarios>()
                .Filter("idUsuario", Supabase.Postgrest.Constants.Operator.Equals, idUsuario.Value.ToString())
                .Get();

            var usuario = response.Models.FirstOrDefault();
            if (usuario == null)
                return RedirectToAction("Index");

            return View(usuario);
        }


        // ---- ACCIÓN: Actualizar nombre y apodo ----
        [HttpPost]
        public async Task<IActionResult> ActualizarPerfil(string nombre, string apodo)
        {
            var idUsuario = HttpContext.Session.GetInt32("IdUsuario");
            if (!idUsuario.HasValue)
                return RedirectToAction("Login", "Cuenta", new { area = "Cuentas" });

            var response = await _supabase
                .From<Usuarios>()
                .Filter("idUsuario", Supabase.Postgrest.Constants.Operator.Equals, idUsuario.Value.ToString())
                .Get();

            var usuario = response.Models.FirstOrDefault();
            if (usuario != null)
            {
                usuario.Nombre = nombre?.Trim();
                usuario.Apodo = apodo?.Trim();

                try
                {
                    await _supabase.From<Usuarios>().Update(usuario);
                    HttpContext.Session.SetString("Apodo", apodo?.Trim() ?? "");
                    TempData["Correcto"] = "Perfil actualizado correctamente.";
                }
                catch (Exception)
                {
                    TempData["Error"] = "Ese apodo ya esta en uso, elige otro.";
                }
            }
            else
            {
                TempData["Error"] = "No se pudo actualizar el perfil.";
            }

            return RedirectToAction("MiPerfil");
        }


        // ---- ACCIÓN: Subir foto de perfil ----
        [HttpPost]
        public async Task<IActionResult> ActualizarFoto(IFormFile foto)
        {
            var idUsuario = HttpContext.Session.GetInt32("IdUsuario");
            if (!idUsuario.HasValue)
                return RedirectToAction("Login", "Cuenta", new { area = "Cuentas" });

            if (foto == null || foto.Length == 0)
            {
                TempData["Error"] = "No se recibió ninguna imagen.";
                return RedirectToAction("MiPerfil");
            }

            if (foto.Length > 2 * 1024 * 1024)
            {
                TempData["Error"] = "La imagen no puede superar 2 MB.";
                return RedirectToAction("MiPerfil");
            }

            var extensionesPermitidas = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
            var extension = Path.GetExtension(foto.FileName).ToLowerInvariant();
            if (!extensionesPermitidas.Contains(extension))
            {
                TempData["Error"] = "Formato no permitido. Usa JPG, PNG o GIF.";
                return RedirectToAction("MiPerfil");
            }

            try
            {
                // Guardar en wwwroot/imagenes/perfiles/
                var carpeta = Path.Combine(_env.WebRootPath, "imagenes", "perfiles");
                Directory.CreateDirectory(carpeta);

                var nombreArchivo = $"perfil_{idUsuario.Value}{extension}";
                var rutaCompleta = Path.Combine(carpeta, nombreArchivo);

                using (var stream = new FileStream(rutaCompleta, FileMode.Create))
                {
                    await foto.CopyToAsync(stream);
                }

                var urlFoto = $"/imagenes/perfiles/{nombreArchivo}?v={DateTime.Now.Ticks}";

                // Actualizar en BD
                var response = await _supabase
                    .From<Usuarios>()
                    .Filter("idUsuario", Supabase.Postgrest.Constants.Operator.Equals, idUsuario.Value.ToString())
                    .Get();

                var usuario = response.Models.FirstOrDefault();
                if (usuario != null)
                {
                    usuario.FotoPerfil = urlFoto;
                    await _supabase.From<Usuarios>().Update(usuario);
                    HttpContext.Session.SetString("FotoPerfil", urlFoto);
                    TempData["Correcto"] = "Foto de perfil actualizada.";
                }
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Error al subir la imagen: " + ex.Message;
            }

            return RedirectToAction("MiPerfil");
        }
    }
}