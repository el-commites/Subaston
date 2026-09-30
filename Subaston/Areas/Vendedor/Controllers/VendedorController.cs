using Microsoft.AspNetCore.Mvc;
using Subaston.Models.Models;
using Subaston.ViewsModels;
using Subaston.Services;

namespace Subaston.Areas.Vendedor.Controllers
{
    [Area("Vendedor")]
    public class VendedorController : Controller
    {
        private readonly NotificacionService _notificacionService;
        private readonly Supabase.Client _supabase;
        private readonly IWebHostEnvironment _env;

        public VendedorController(Supabase.Client supabase, IWebHostEnvironment env, NotificacionService notificacionService)
        {
            _supabase = supabase;
            _env = env;
            _notificacionService = notificacionService;
            
        }

        // INDEX
        public IActionResult Index()
        {
            return View();
        }

        //RAZONES DE BANEO
        public IActionResult RazonesBaneo()
        {
            return View();
        }

        //SUCURSALES DISPONIBLES PARA ENTREGA DE PRODUCTOS
        public IActionResult Sucursales()
        {
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
                    Nombre    = "Subastón Iztapalapa",
                    Direccion = "Av. 11 62-Local 4, Cerro de la Estrella, Iztapalapa",
                    Ciudad    = "Ciudad de Mexico",
                    Telefono  = "55 6403 1998",
                    Horario   = "Lun–Vie 9:00–18:00 | Sáb 10:00–14:00",
                    MapUrl    = "https://maps.app.goo.gl/ABApySkTMmZZs6vz9"
                }
            };

            return View(sucursales);
        }

        //GUIA DE CATEGORIAS
        public async Task<IActionResult> GuiaCategorias()
        {
            return View();
        }

        // MIS PRODUCTOS
        public async Task<IActionResult> MisProductos()
        {
            // OBTENER CORREO DEL USUARIO LOGEADO
            var correo =
                HttpContext.Session.GetString("Usuario");

            if (string.IsNullOrEmpty(correo))
            {
                return RedirectToAction(
                    "Login",
                    "Cuenta",
                    new { area = "Cuentas" });
            }

            // BUSCAR USUARIO
            var usuarioResponse = await _supabase
                .From<Usuarios>()
                .Where(x => x.Correo == correo)
                .Get();

            var usuario = usuarioResponse
                .Models
                .FirstOrDefault();

            if (usuario == null)
            {
                return RedirectToAction("Index");
            }

            // OBTENER PRODUCTOS DEL VENDEDOR
            var productosResponse = await _supabase
                .From<Productos>()
                .Where(x =>
                    x.idVendedor_FK == usuario.IdUsuario)
                .Get();

            var productos = productosResponse.Models;

            // CARGAR IMÁGENES
            foreach (var producto in productos)
            {
                var imagenResponse = await _supabase
                    .From<ProductoImagen>()
                    .Where(x =>
                        x.IdProductoFK ==
                        producto.IdProductos)
                    .Get();

                var imagen = imagenResponse
                    .Models
                    .FirstOrDefault();

                if (imagen != null)
                {
                    producto.ImagenUrl =
                        imagen.UrlImagen;
                }

                // OBTENER APODO DEL GANADOR ACTUAL
                if (producto.PujadorActual != null)
                {
                    var pujadorResponse = await _supabase
                        .From<Usuarios>()
                        .Where(x =>
                            x.IdUsuario ==
                            producto.PujadorActual)
                        .Get();

                    var pujador = pujadorResponse
                        .Models
                        .FirstOrDefault();

                    if (pujador != null)
                    {
                        producto.PujadorActualApodo =
                            pujador.Apodo;
                    }
                }
            }

            return View(productos);
        }

        // DETALLES PRODUCTO (VENDEDOR)
        public async Task<IActionResult> Detalles(int id)
        {
            // BUSCAR PRODUCTO
            var response = await _supabase
                .From<Productos>()
                .Where(x => x.IdProductos == id)
                .Get();

            var producto = response
                .Models
                .FirstOrDefault();

            if (producto == null)
            {
                return RedirectToAction("MisProductos");
            }

            // CAMBIAR A FINALIZADA SI YA PASÓ LA FECHA
            if (producto.FechaCierre.ToUniversalTime() < DateTime.UtcNow)
            {
                producto.Estado = "Finalizada";

                await producto.Update<Productos>();
            }

            // CARGAR IMAGEN
            var imagenResponse = await _supabase
                .From<ProductoImagen>()
                .Where(x => x.IdProductoFK == producto.IdProductos)
                .Get();

            var imagen = imagenResponse
                .Models
                .FirstOrDefault();

            if (imagen != null)
            {
                producto.ImagenUrl = imagen.UrlImagen;
            }

            // OBTENER USUARIO QUE VA GANANDO
            if (producto.PujadorActual != null)
            {
                var ganadorResponse = await _supabase
                    .From<Usuarios>()
                    .Where(x =>
                        x.IdUsuario == producto.PujadorActual)
                    .Get();

                var ganador = ganadorResponse
                    .Models
                    .FirstOrDefault();

                if (ganador != null)
                {
                    ViewBag.GanadorActual =
                        ganador.Apodo;
                    ViewBag.GanadorFoto =
                        ganador.FotoPerfil;
                }
            }

            // CONTADOR DE TIEMPO
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

        // CREAR PRODUCTO GET
        public IActionResult CrearProducto()
        {
            return View();
        }

        // CREAR PRODUCTO POST
        [HttpPost]
        public async Task<IActionResult> CrearProducto(
            CrearProductoViewModel model, int? timezoneOffset)
        {
            if (!ModelState.IsValid)
            {
                TempData["Error"] = "Revisa los campos del producto antes de publicarlo.";
                return View(model);
            }

            DateTime utcFechaCierre = (DateTime)model.FechaCierre;
            if (timezoneOffset.HasValue)
            {
                var offset = TimeSpan.FromMinutes(timezoneOffset.Value);
                var localTime = DateTime.SpecifyKind((DateTime)model.FechaCierre, DateTimeKind.Unspecified);
                var dto = new DateTimeOffset(localTime, -offset);
                utcFechaCierre = dto.UtcDateTime;
            }

            // VALIDAR FECHA
            if (utcFechaCierre <= DateTime.UtcNow)
            {
                ModelState.AddModelError(
                    "FechaCierre",
                    "La fecha debe ser mayor a la actual");

                TempData["Error"] = "La fecha de cierre debe ser mayor a la fecha actual.";
                return View(model);
            }

            // OBTENER USUARIO LOGEADO
            var correo =
                HttpContext.Session.GetString("Usuario");

            if (string.IsNullOrEmpty(correo))
            {
                return RedirectToAction(
                    "Login",
                    "Cuenta",
                    new { area = "Cuentas" });
            }

            // BUSCAR USUARIO
            var usuarioResponse = await _supabase
                .From<Usuarios>()
                .Where(x => x.Correo == correo)
                .Get();

            var usuario = usuarioResponse
                .Models
                .FirstOrDefault();

            if (usuario == null)
            {
                return RedirectToAction("Index");
            }

            string imageUrl = "";

            // SUBIR IMAGEN
            if (model.Imagen != null)
            {
                var extensionesPermitidas = new[] { ".jpg", ".jpeg", ".png", ".webp" };
                var extension = Path.GetExtension(model.Imagen.FileName).ToLowerInvariant();

                if (!extensionesPermitidas.Contains(extension))
                {
                    ModelState.AddModelError("Imagen", "La imagen debe ser JPG, PNG o WEBP.");
                    TempData["Error"] = "La imagen debe ser JPG, PNG o WEBP.";
                    return View(model);
                }

                if (model.Imagen.Length > 5 * 1024 * 1024)
                {
                    ModelState.AddModelError("Imagen", "La imagen no puede pesar mas de 5 MB.");
                    TempData["Error"] = "La imagen no puede pesar mas de 5 MB.";
                    return View(model);
                }

                var fileName =
                    Guid.NewGuid().ToString() +
                    Path.GetExtension(model.Imagen.FileName);

                using var stream =
                    model.Imagen.OpenReadStream();

                using var memoryStream =
                    new MemoryStream();

                await stream.CopyToAsync(memoryStream);

                var bytes = memoryStream.ToArray();

                // SUBIR A STORAGE
                await _supabase.Storage
                    .From("productos")
                    .Upload(bytes, fileName);

                // OBTENER URL
                imageUrl = _supabase.Storage
                    .From("productos")
                    .GetPublicUrl(fileName);
            }

            // CREAR PRODUCTO
            var producto = new Productos
            {
                NombreDelProducto = model.NombreDelProducto,

                Descripcion = model.Descripcion,

                PrecioInicial = (decimal)model.PrecioInicial,

                // INICIAR PUJA
                PujaActual = (int?)model.PrecioInicial,

                // TODAVIA NO HAY PUJADOR
                PujadorActual = null,

                // TOTAL DE PUJAS
                TotalPujas = 0,

                FechaCierre = utcFechaCierre,

                Categoria = model.Categoria,

                // EL ADMIN LO DEBE APROBAR
                Estado = "Pendiente",

                // GUARDAR VENDEDOR
                idVendedor_FK = (int?)usuario.IdUsuario,

                // IMAGEN TEMPORAL
                ImagenUrl = imageUrl
            };

            // GUARDAR PRODUCTO
            var respuesta = await _supabase
                .From<Productos>()
                .Insert(producto);

            var productoCreado =
                respuesta.Models.FirstOrDefault();

            // VALIDAR QUE SI SE CREO
            if (productoCreado == null)
            {
                ModelState.AddModelError(
                    "",
                    "No se pudo crear el producto");

                TempData["Error"] = "No se pudo crear el producto. Intenta de nuevo.";
                return View(model);
            }

            // VER ID
            Console.WriteLine(
                $"ID PRODUCTO: {productoCreado.IdProductos}");

            // GUARDAR IMAGEN
            if (!string.IsNullOrEmpty(imageUrl))
            {
                await _supabase
                    .From<ProductoImagen>()
                    .Insert(new ProductoImagen
                    {
                        IdProductoFK =
                            productoCreado.IdProductos,

                        UrlImagen = imageUrl
                    });
            }

            await _notificacionService.CrearParaAdminsAsync(
                    "Nueva publicación pendiente",
                    "Un vendedor envió un producto para revisión.",
                    "Admin",
                    "/Admin/Admin/ProductosPendientes"
            );

            TempData["Correcto"] = "Producto enviado a revision correctamente.";
            return RedirectToAction("MisProductos");
        }

        // HISTORIAL DE GANANCIAS
        public async Task<IActionResult> HistorialGanancias()
        {
            // OBTENER CORREO DEL USUARIO LOGEADO
            var correo = HttpContext.Session.GetString("Usuario");

            if (string.IsNullOrEmpty(correo))
            {
                return RedirectToAction(
                    "Login",
                    "Cuenta",
                    new { area = "Cuentas" });
            }

            // BUSCAR USUARIO
            var usuarioResponse = await _supabase
                .From<Usuarios>()
                .Where(x => x.Correo == correo)
                .Get();

            var usuario = usuarioResponse.Models.FirstOrDefault();

            if (usuario == null)
            {
                return RedirectToAction("Index");
            }

            // OBTENER PRODUCTOS PAGADOS DEL VENDEDOR
            var productosResponse = await _supabase
                .From<Productos>()
                .Where(x => x.idVendedor_FK == usuario.IdUsuario && x.Estado == "Pagado")
                .Get();

            var productos = productosResponse.Models;

            // CARGAR IMÁGENES
            foreach (var producto in productos)
            {
                var imagenResponse = await _supabase
                    .From<ProductoImagen>()
                    .Where(x => x.IdProductoFK == producto.IdProductos)
                    .Get();

                var imagen = imagenResponse.Models.FirstOrDefault();

                if (imagen != null)
                {
                    producto.ImagenUrl = imagen.UrlImagen;
                }
            }

            // CALCULAR GANANCIAS CON DESGLOSE COMPLETO
            decimal totalVentas = 0;
            decimal totalComisionSubaston = 0;
            decimal totalComisionMercadoPago = 0;
            decimal totalGananciasNetas = 0;

            foreach (var prod in productos)
            {
                var monto = prod.PujaActual.HasValue ? (decimal)prod.PujaActual.Value : prod.PrecioInicial;

                // 10% comisión Subaston
                var comisionSubaston = monto * 0.10m;

                // Comisión Mercado Pago: 3.49% + IVA (16% sobre el 3.49%) + $4.64 fijo
                var comisionMercadoPagoVariable = monto * 0.0349m;
                var ivaMercadoPago = comisionMercadoPagoVariable * 0.16m;
                var cargoFijoMercadoPago = 4.64m;
                var comisionMercadoPago = comisionMercadoPagoVariable + ivaMercadoPago + cargoFijoMercadoPago;

                totalVentas += monto;
                totalComisionSubaston += comisionSubaston;
                totalComisionMercadoPago += comisionMercadoPago;
                totalGananciasNetas += Math.Max(0, monto - comisionSubaston - comisionMercadoPago);
            }

            ViewBag.TotalVentas = totalVentas;
            ViewBag.ComisionSubaston = totalComisionSubaston;   // 10% Subaston
            ViewBag.ComisionMercadoPago = totalComisionMercadoPago; // Mercado Pago fees
            ViewBag.GananciasNetas = totalGananciasNetas;       // Neto vendedor
            ViewBag.PorcentajeSubaston = 10;
            ViewBag.PorcentajeMercadoPago = 3.49m;
            ViewBag.IvaMercadoPago = 16;
            ViewBag.CargoFijoMercadoPago = 4.64m;

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
