using Microsoft.AspNetCore.Mvc;
using Subaston.Models.Models;
using Subaston.Services;

namespace Subaston.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class AdminController : Controller
    {
        private readonly NotificacionService _notificacionService;
        private readonly Supabase.Client _supabase;

        public AdminController(Supabase.Client supabase, NotificacionService notificacionService)
        {
            _supabase = supabase;
            _notificacionService = notificacionService;
        }

        // PANEL ADMIN
        public async Task<IActionResult> Index()
        {
            var response = await _supabase
                .From<Productos>()
                .Where(x => x.Estado == "Pendiente")
                .Get();

            var productos = response.Models;

            return View(productos);
        }

        // USUARIOS (con bÃºsqueda)
        public async Task<IActionResult> Usuarios(string? buscar = null)
        {
            var response = await _supabase
                .From<Usuarios>()
                .Where(x => x.Rol != "Admin")
                .Get();

            var usuarios = response.Models;

            if (!string.IsNullOrWhiteSpace(buscar))
            {
                var b = buscar.ToLower();
                usuarios = usuarios.Where(u =>
                    (u.Nombre != null && u.Nombre.ToLower().Contains(b)) ||
                    (u.Correo != null && u.Correo.ToLower().Contains(b)) ||
                    (u.Apodo != null && u.Apodo.ToLower().Contains(b)) ||
                    (u.Rol != null && u.Rol.ToLower().Contains(b))
                ).ToList();
            }

            ViewBag.Buscar = buscar;
            return View(usuarios);
        }

        // BAN USUARIO
        public async Task<IActionResult> Ban(int id)
        {
            var response = await _supabase
                .From<Usuarios>()
                .Filter("idUsuario", Supabase.Postgrest.Constants.Operator.Equals, id)
                .Get();

            var usuario = response.Models.FirstOrDefault();

            if (usuario != null)
            {
                // 1. Obtener productos del usuario
                var productosResp = await _supabase
                    .From<Productos>()
                    .Filter("idVendedor_FK", Supabase.Postgrest.Constants.Operator.Equals, id.ToString())
                    .Get();

                foreach (var producto in productosResp.Models)
                {
                    int pid = producto.IdProductos;

                    // 2. Borrar reportes del producto
                    await _supabase.From<ReportesSubastas>()
                        .Filter("idProducto_FK", Supabase.Postgrest.Constants.Operator.Equals, pid.ToString())
                        .Delete();

                    // 3. Borrar pujas del producto
                    var pujas = await _supabase.From<HistorialPujas>()
                        .Filter("idProducto_FK", Supabase.Postgrest.Constants.Operator.Equals, pid.ToString())
                        .Get();

                    foreach (var puja in pujas.Models)
                    {
                        await _supabase.From<HistorialPujas>()
                            .Filter("idHistorial", Supabase.Postgrest.Constants.Operator.Equals, puja.idHistorial.ToString())
                            .Delete();
                    }

                    // 4. Borrar imÃ¡genes del producto
                    var imagenes = await _supabase.From<ProductoImagen>()
                        .Filter("idProducto_FK", Supabase.Postgrest.Constants.Operator.Equals, pid.ToString())
                        .Get();

                    foreach (var imagen in imagenes.Models)
                    {
                        await _supabase.From<ProductoImagen>()
                            .Filter("idImagen", Supabase.Postgrest.Constants.Operator.Equals, imagen.IdImagen.ToString())
                            .Delete();
                    }

                    // 5. Borrar el producto
                    await _supabase.From<Productos>()
                        .Filter("idProductos", Supabase.Postgrest.Constants.Operator.Equals, pid.ToString())
                        .Delete();
                }

                // 6. Borrar pujas que hizo el usuario como comprador
                var pujasUsuario = await _supabase.From<HistorialPujas>()
                    .Filter("idUsuario_FK", Supabase.Postgrest.Constants.Operator.Equals, id.ToString())
                    .Get();

                foreach (var puja in pujasUsuario.Models)
                {
                    await _supabase.From<HistorialPujas>()
                        .Filter("idHistorial", Supabase.Postgrest.Constants.Operator.Equals, puja.idHistorial.ToString())
                        .Delete();
                }

                // 7. Borrar reportes que hizo el usuario
                await _supabase.From<ReportesSubastas>()
                    .Filter("idUsuario_FK",
                        Supabase.Postgrest.Constants.Operator.Equals,
                        id.ToString())
                    .Delete();

                // 8. Limpiar productos donde aparece como pujador actual
                var productosPujador = await _supabase
                    .From<Productos>()
                    .Filter("PujadorActual",
                        Supabase.Postgrest.Constants.Operator.Equals,
                        id.ToString())
                    .Get();

                foreach (var producto in productosPujador.Models)
                {
                    producto.PujadorActual = null;

                    await _supabase
                        .From<Productos>()
                        .Update(producto);
                }

                // 9. Borrar notificaciones
                await _supabase
                    .From<Notificaciones>()
                    .Filter("idUsuario_FK",
                        Supabase.Postgrest.Constants.Operator.Equals,
                        id.ToString())
                    .Delete();

                // 10. Finalmente borrar el usuario
                await _supabase
                    .From<Usuarios>()
                    .Where(x => x.IdUsuario == usuario.IdUsuario)
                    .Delete();

                TempData["Correcto"] = "Usuario baneado correctamente.";
            }

            return RedirectToAction("Usuarios");
        }

        // VER DETALLES DE PRODUCTO (SOLO LECTURA PARA ADMIN)
        public async Task<IActionResult> DetallesProducto(int id)
        {
            var response = await _supabase
                .From<Productos>()
                .Filter("idProductos", Supabase.Postgrest.Constants.Operator.Equals, id.ToString())
                .Get();

            var producto = response.Models.FirstOrDefault();

            if (producto == null)
            {
                return RedirectToAction("ReportesSubastas");
            }

            // IMAGEN
            var imagenResponse = await _supabase
                .From<ProductoImagen>()
                .Filter("idProducto_FK", Supabase.Postgrest.Constants.Operator.Equals, producto.IdProductos.ToString())
                .Get();

            var imagen = imagenResponse.Models.FirstOrDefault();
            if (imagen != null)
            {
                producto.ImagenUrl = imagen.UrlImagen;
            }

            // VENDEDOR
            if (producto.idVendedor_FK != null)
            {
                var vendedorResponse = await _supabase
                    .From<Usuarios>()
                    .Filter("idUsuario", Supabase.Postgrest.Constants.Operator.Equals, producto.idVendedor_FK.ToString())
                    .Get();

                var vendedor = vendedorResponse.Models.FirstOrDefault();
                if (vendedor != null)
                {
                    ViewBag.VendedorNombre = vendedor.Apodo;
                    ViewBag.VendedorFoto = vendedor.FotoPerfil;
                }
            }

            // GANADOR ACTUAL
            if (producto.PujadorActual != null)
            {
                var ganadorResponse = await _supabase
                    .From<Usuarios>()
                    .Filter("idUsuario", Supabase.Postgrest.Constants.Operator.Equals, producto.PujadorActual.ToString())
                    .Get();

                var ganador = ganadorResponse.Models.FirstOrDefault();
                if (ganador != null)
                {
                    ViewBag.GanadorActual = ganador.Apodo;
                    ViewBag.GanadorFoto = ganador.FotoPerfil;
                }
            }

            // TIEMPO RESTANTE
            var tiempoRestante = producto.FechaCierre.ToUniversalTime() - DateTime.UtcNow;
            if (tiempoRestante.TotalSeconds > 0)
            {
                ViewBag.TiempoRestante = $"{tiempoRestante.Days}d {tiempoRestante.Hours}h {tiempoRestante.Minutes}m";
            }
            else
            {
                ViewBag.TiempoRestante = "Subasta finalizada";
            }

            return View(producto);
        }

        // VER PERFIL DE USUARIO
        public async Task<IActionResult> PerfilUsuario(int id)
        {
            var response = await _supabase
                .From<Usuarios>()
                .Filter("idUsuario", Supabase.Postgrest.Constants.Operator.Equals, id)
                .Get();

            var usuario = response.Models.FirstOrDefault();

            if (usuario == null)
                return RedirectToAction("Usuarios");

            // PRODUCTOS DEL USUARIO
            var productosResponse = await _supabase
                .From<Productos>()
                .Filter("idVendedor_FK", Supabase.Postgrest.Constants.Operator.Equals, id.ToString())
                .Get();

            ViewBag.Productos = productosResponse.Models;
            ViewBag.TotalProductos = productosResponse.Models.Count;

            // PUJAS DEL USUARIO
            var pujasResponse = await _supabase
                .From<HistorialPujas>()
                .Filter("idUsuario_FK", Supabase.Postgrest.Constants.Operator.Equals, id.ToString())
                .Get();

            ViewBag.TotalPujas = pujasResponse.Models.Count;

            var productosPujas = new Dictionary<int, string>();
            var resultadosPujas = new Dictionary<int, string>();
            foreach (var puja in pujasResponse.Models)
            {
                var productoResp = await _supabase
                    .From<Productos>()
                    .Filter("idProductos", Supabase.Postgrest.Constants.Operator.Equals, puja.idProducto_FK.ToString())
                    .Get();

                var producto = productoResp.Models.FirstOrDefault();
                productosPujas[puja.idHistorial] = producto?.NombreDelProducto ?? "Producto eliminado";

                if (producto == null)
                {
                    resultadosPujas[puja.idHistorial] = "Eliminado";
                }
                else if (producto.Estado != "Finalizada" && producto.Estado != "Pagado")
                {
                    resultadosPujas[puja.idHistorial] = "En curso";
                }
                else if (producto.idGanador_FK == usuario.IdUsuario)
                {
                    resultadosPujas[puja.idHistorial] = "Gano";
                }
                else
                {
                    resultadosPujas[puja.idHistorial] = "Perdio";
                }
            }

            ViewBag.Pujas = pujasResponse.Models;
            ViewBag.ProductosPujas = productosPujas;
            ViewBag.ResultadosPujas = resultadosPujas;

            return View(usuario);
        }

        // PRODUCTOS PENDIENTES
        public async Task<IActionResult> ProductosPendientes()
        {
            var response = await _supabase
                .From<Productos>()
                .Where(x => x.Estado == "Pendiente")
                .Get();

            var productos = response.Models;

            foreach (var producto in productos)
            {
                var imagenResponse = await _supabase
                    .From<ProductoImagen>()
                    .Where(x => x.IdProductoFK == producto.IdProductos)
                    .Get();

                var imagen = imagenResponse.Models.FirstOrDefault();
                if (imagen != null)
                    producto.ImagenUrl = imagen.UrlImagen;
            }

            return View(productos);
        }

        // ACEPTAR PRODUCTO
        public async Task<IActionResult> AceptarProducto(int id)
        {
            var response = await _supabase
                .From<Productos>()
                .Filter("idProductos", Supabase.Postgrest.Constants.Operator.Equals, id.ToString())
                .Get();

            var producto = response.Models.FirstOrDefault();

            if (producto != null)
            {
                producto.Estado = "Aceptado";
                await _supabase.From<Productos>().Update(producto);

                if (producto.idVendedor_FK.HasValue)
                {
                    await _notificacionService.CrearAsync(
                        producto.idVendedor_FK.Value,
                        "PublicaciÃ³n aprobada",
                        $"Tu producto '{producto.NombreDelProducto}' fue aprobado.",
                        "Aprobacion",
                        "/Vendedor/Vendedor/MisProductos"
                    );
                }

                TempData["Correcto"] = "Producto aceptado correctamente.";
            }

            return RedirectToAction("ProductosPendientes");
        }

        // RECHAZAR PRODUCTO
        public async Task<IActionResult> RechazarProducto(int id)
        {
            var response = await _supabase
                .From<Productos>()
                .Filter("idProductos", Supabase.Postgrest.Constants.Operator.Equals, id.ToString())
                .Get();

            var producto = response.Models.FirstOrDefault();

            if (producto != null)
            {
                var idVendedor = producto.idVendedor_FK;
                var nombreProducto = producto.NombreDelProducto;

                await _supabase
                    .From<ReportesSubastas>()
                    .Filter("idProducto_FK", Supabase.Postgrest.Constants.Operator.Equals, id.ToString())
                    .Delete();

                var historial = await _supabase
                    .From<HistorialPujas>()
                    .Filter("idProducto_FK", Supabase.Postgrest.Constants.Operator.Equals, id.ToString())
                    .Get();

                foreach (var puja in historial.Models)
                {
                    await _supabase
                        .From<HistorialPujas>()
                        .Filter("idHistorial", Supabase.Postgrest.Constants.Operator.Equals, puja.idHistorial.ToString())
                        .Delete();
                }

                var imagenes = await _supabase
                    .From<ProductoImagen>()
                    .Filter("idProducto_FK", Supabase.Postgrest.Constants.Operator.Equals, id.ToString())
                    .Get();

                foreach (var imagen in imagenes.Models)
                {
                    await _supabase
                        .From<ProductoImagen>()
                        .Filter("idImagen", Supabase.Postgrest.Constants.Operator.Equals, imagen.IdImagen.ToString())
                        .Delete();
                }

                await _supabase
                    .From<Productos>()
                    .Filter("idProductos", Supabase.Postgrest.Constants.Operator.Equals, id.ToString())
                    .Delete();

                if (idVendedor.HasValue)
                {
                    await _notificacionService.CrearAsync(
                        idVendedor.Value,
                        "PublicaciÃ³n rechazada",
                        $"Tu producto '{nombreProducto}' fue rechazado por administraciÃ³n.",
                        "Rechazo",
                        "/Vendedor/Vendedor/MisProductos"
                    );
                }

                TempData["Correcto"] = "Producto eliminado junto con sus pujas y reportes.";
            }

            return RedirectToAction("ProductosPendientes");
        }

        [HttpPost]
        public async Task<IActionResult> EliminarSubasta(int id, string regresar = "ProductosPendientes", int? idUsuario = null)
        {
            try
            {
                // Obtener producto antes de borrar para notificar al vendedor
                var productoResp = await _supabase
                    .From<Productos>()
                    .Filter("idProductos", Supabase.Postgrest.Constants.Operator.Equals, id.ToString())
                    .Get();

                var producto = productoResp.Models.FirstOrDefault();

                // Borrar reportes
                await _supabase
                    .From<ReportesSubastas>()
                    .Filter("idProducto_FK", Supabase.Postgrest.Constants.Operator.Equals, id.ToString())
                    .Delete();

                // Borrar pujas
                var historial = await _supabase
                    .From<HistorialPujas>()
                    .Filter("idProducto_FK", Supabase.Postgrest.Constants.Operator.Equals, id.ToString())
                    .Get();

                foreach (var puja in historial.Models)
                {
                    await _supabase
                        .From<HistorialPujas>()
                        .Filter("idHistorial", Supabase.Postgrest.Constants.Operator.Equals, puja.idHistorial.ToString())
                        .Delete();
                }

                // Borrar imÃ¡genes
                var imagenes = await _supabase
                    .From<ProductoImagen>()
                    .Filter("idProducto_FK", Supabase.Postgrest.Constants.Operator.Equals, id.ToString())
                    .Get();

                foreach (var imagen in imagenes.Models)
                {
                    await _supabase
                        .From<ProductoImagen>()
                        .Filter("idImagen", Supabase.Postgrest.Constants.Operator.Equals, imagen.IdImagen.ToString())
                        .Delete();
                }

                // Borrar producto
                await _supabase
                    .From<Productos>()
                    .Filter("idProductos", Supabase.Postgrest.Constants.Operator.Equals, id.ToString())
                    .Delete();

                // NOTIFICAR AL VENDEDOR
                if (producto?.idVendedor_FK.HasValue == true)
                {
                    await _notificacionService.CrearAsync(
                        producto.idVendedor_FK.Value,
                        "Tu subasta fue eliminada",
                        $"Tu producto '{producto.NombreDelProducto}' fue eliminado por administraciÃ³n.",
                        "Eliminacion",
                        "/Vendedor/Vendedor/MisProductos"
                    );
                }

                TempData["Correcto"] = "Subasta eliminada correctamente.";
            }
            catch
            {
                TempData["Error"] = "No se pudo eliminar la subasta.";
            }

            if (regresar == "ReportesSubastas")
                return RedirectToAction("ReportesSubastas");

            if (regresar == "PerfilUsuario" && idUsuario.HasValue)
                return RedirectToAction("PerfilUsuario", new { id = idUsuario.Value });

            return RedirectToAction("ProductosPendientes");
        }

        // ESTADÃSTICAS
        public async Task<IActionResult> Estadisticas()
        {
            var productosResponse = await _supabase.From<Productos>().Get();
            var productos = productosResponse.Models;

            var usuariosResponse = await _supabase.From<Usuarios>().Get();
            var usuarios = usuariosResponse.Models;

            var productosPagados = productos.Where(x => x.Estado == "Pagado").ToList();
            var productosActivos = productos.Where(x => x.Estado == "Aceptado").ToList();
            var productosPendientes = productos.Where(x => x.Estado == "Pendiente").ToList();
            var productosFinalizados = productos.Where(x => x.Estado == "Finalizada").ToList();

            decimal totalVentas = 0;
            decimal totalComisionSubaston = 0;
            decimal totalComisionMercadoPago = 0;
            decimal totalGananciaVendedores = 0;

            foreach (var prod in productosPagados)
            {
                var monto = prod.PujaActual.HasValue
                    ? (decimal)prod.PujaActual.Value
                    : prod.PrecioInicial;

                var comisionSubaston = monto * 0.10m;
                var comisionMercadoPagoVariable = monto * 0.0349m;
                var ivaMercadoPago = comisionMercadoPagoVariable * 0.16m;
                var cargoFijoMercadoPago = 4.64m;
                var comisionMercadoPago = comisionMercadoPagoVariable + ivaMercadoPago + cargoFijoMercadoPago;

                totalVentas += monto;
                totalComisionSubaston += comisionSubaston;
                totalComisionMercadoPago += comisionMercadoPago;
                totalGananciaVendedores += Math.Max(0, monto - comisionSubaston - comisionMercadoPago);
            }

            ViewBag.TotalVentas = totalVentas;
            ViewBag.ComisionPlataforma = totalComisionSubaston;
            ViewBag.ComisionMercadoPago = totalComisionMercadoPago;
            ViewBag.GananciaVendedores = totalGananciaVendedores;
            ViewBag.PorcentajeSubaston = 10;
            ViewBag.PorcentajeMercadoPago = 3.49m;
            ViewBag.IvaMercadoPago = 16;
            ViewBag.CargoFijoMercadoPago = 4.64m;
            ViewBag.CantPagados = productosPagados.Count;
            ViewBag.CantActivos = productosActivos.Count;
            ViewBag.CantPendientes = productosPendientes.Count;
            ViewBag.CantFinalizados = productosFinalizados.Count;
            ViewBag.TotalUsuarios = usuarios.Count;
            ViewBag.CantCompradores = usuarios.Count(x => x.Rol == "Comprador");
            ViewBag.CantVendedores = usuarios.Count(x => x.Rol == "Vendedor");
            ViewBag.CantAdmins = usuarios.Count(x => x.Rol == "Admin");

            var ventasPorCategoria = productosPagados
                .GroupBy(x => x.Categoria ?? "Sin CategorÃ­a")
                .Select(g => new
                {
                    Categoria = g.Key,
                    Monto = g.Sum(x => x.PujaActual.HasValue ? (decimal)x.PujaActual.Value : x.PrecioInicial)
                }).ToList();

            ViewBag.CategoriasNombres = ventasPorCategoria.Select(x => x.Categoria).ToList();
            ViewBag.CategoriasMontos = ventasPorCategoria.Select(x => x.Monto).ToList();
            ViewBag.EstadosNombres = new List<string> { "Activas", "Pendientes", "Pagadas", "Finalizadas" };
            ViewBag.EstadosValores = new List<int> { productosActivos.Count, productosPendientes.Count, productosPagados.Count, productosFinalizados.Count };

            return View();
        }

        // REPORTES
        public async Task<IActionResult> ReportesSubastas()
        {
            var response = await _supabase.From<ReportesSubastas>().Get();
            var reportes = response.Models.OrderByDescending(x => x.FechaReporte).ToList();

            foreach (var reporte in reportes)
            {
                var productoResponse = await _supabase
                    .From<Productos>()
                    .Filter("idProductos", Supabase.Postgrest.Constants.Operator.Equals, reporte.idProducto_FK.ToString())
                    .Get();

                var producto = productoResponse.Models.FirstOrDefault();
                reporte.Producto = producto;

                if (producto != null)
                {
                    var imagenResponse = await _supabase
                        .From<ProductoImagen>()
                        .Where(x => x.IdProductoFK == producto.IdProductos)
                        .Get();

                    var imagen = imagenResponse.Models.FirstOrDefault();
                    if (imagen != null)
                        producto.ImagenUrl = imagen.UrlImagen;
                }

                var usuarioResponse = await _supabase
                    .From<Usuarios>()
                    .Filter("idUsuario", Supabase.Postgrest.Constants.Operator.Equals, reporte.idUsuario_FK.ToString())
                    .Get();

                reporte.Usuario = usuarioResponse.Models.FirstOrDefault();
            }

            return View(reportes);
        }

        // MARCAR REPORTE REVISADO
        public async Task<IActionResult> MarcarReporteRevisado(int id)
        {
            var response = await _supabase
                .From<ReportesSubastas>()
                .Filter("idReporte", Supabase.Postgrest.Constants.Operator.Equals, id.ToString())
                .Get();

            var reporte = response.Models.FirstOrDefault();

            if (reporte != null)
            {
                reporte.Estado = "Revisado";
                await reporte.Update<ReportesSubastas>();

                await _notificacionService.CrearAsync(
                    reporte.idUsuario_FK,
                    "Reporte revisado",
                    "Tu reporte fue revisado por administraciÃ³n.",
                    "Reporte",
                    "/Comprador/Comprador/MisFunciones"
                );

                TempData["Correcto"] = "Reporte marcado como revisado.";
            }
            else
            {
                TempData["Error"] = "No se encontrÃ³ el reporte.";
            }

            return RedirectToAction("ReportesSubastas");
        }

        // LIMPIAR REPORTES REVISADOS
        [HttpPost]
        public async Task<IActionResult> LimpiarReportesRevisados()
        {
            try
            {
                var response = await _supabase
                    .From<ReportesSubastas>()
                    .Where(x => x.Estado == "Revisado")
                    .Get();

                foreach (var reporte in response.Models)
                {
                    await _supabase
                        .From<ReportesSubastas>()
                        .Filter("idReporte", Supabase.Postgrest.Constants.Operator.Equals, reporte.IdReporte.ToString())
                        .Delete();
                }

                TempData["Correcto"] = $"Se eliminaron {response.Models.Count} reportes revisados.";
            }
            catch
            {
                TempData["Error"] = "No se pudieron eliminar los reportes revisados.";
            }

            return RedirectToAction("ReportesSubastas");
        }

        // ENVIAR NOTIFICACIÃN PERSONALIZADA
        [HttpPost]
        public async Task<IActionResult> EnviarNotificacionPersonalizada(
            string destinatario, string apodo, string titulo, string mensaje)
        {
            if (string.IsNullOrWhiteSpace(titulo) || string.IsNullOrWhiteSpace(mensaje))
            {
                TempData["Error"] = "El tÃ­tulo y mensaje son obligatorios.";
                return RedirectToAction("ReportesSubastas");
            }

            if (destinatario == "usuario")
            {
                if (string.IsNullOrWhiteSpace(apodo))
                {
                    TempData["Error"] = "Escribe el apodo del usuario.";
                    return RedirectToAction("ReportesSubastas");
                }

                var response = await _supabase
                    .From<Usuarios>()
                    .Where(x => x.Apodo == apodo)
                    .Get();

                var user = response.Models.FirstOrDefault();
                if (user == null)
                {
                    TempData["Error"] = $"No se encontrÃ³ ningÃºn usuario con el apodo '{apodo}'.";
                    return RedirectToAction("ReportesSubastas");
                }

                await _notificacionService.CrearAsync(
                    (int)user.IdUsuario, titulo, mensaje, "Admin", null);

                TempData["Correcto"] = $"NotificaciÃ³n enviada a {apodo}.";
            }
            else if (destinatario == "compradores")
            {
                var response = await _supabase.From<Usuarios>()
                    .Where(x => x.Rol == "Comprador").Get();

                foreach (var u in response.Models.Where(u => u.IdUsuario.HasValue))
                    await _notificacionService.CrearAsync((int)u.IdUsuario, titulo, mensaje, "Admin", null);

                TempData["Correcto"] = $"NotificaciÃ³n enviada a {response.Models.Count} compradores.";
            }
            else if (destinatario == "vendedores")
            {
                var response = await _supabase.From<Usuarios>()
                    .Where(x => x.Rol == "Vendedor").Get();

                foreach (var u in response.Models.Where(u => u.IdUsuario.HasValue))
                    await _notificacionService.CrearAsync((int)u.IdUsuario, titulo, mensaje, "Admin", null);

                TempData["Correcto"] = $"NotificaciÃ³n enviada a {response.Models.Count} vendedores.";
            }
            else if (destinatario == "todos")
            {
                var response = await _supabase.From<Usuarios>()
                    .Where(x => x.Rol != "Admin").Get();

                foreach (var u in response.Models.Where(u => u.IdUsuario.HasValue))
                    await _notificacionService.CrearAsync((int)u.IdUsuario, titulo, mensaje, "Admin", null);

                TempData["Correcto"] = $"NotificaciÃ³n enviada a {response.Models.Count} usuarios.";
            }

            return RedirectToAction("ReportesSubastas");
        }
    }
}
