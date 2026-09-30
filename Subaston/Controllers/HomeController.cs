using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Subaston.Models;
using Subaston.Models.Models;

namespace Subaston.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly Supabase.Client _supabase;

        public HomeController(ILogger<HomeController> logger, Supabase.Client supabase)
        {
            _logger = logger;
            _supabase = supabase;
        }

        public async Task<IActionResult> Index()
        {
            var response = await _supabase
                .From<Productos>()
                .Get();

            var productos = response.Models;

            // FINALIZAR SUBASTAS (misma lógica que CompradorController)
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

                    if (ultimaPuja != null)
                    {
                        producto.idGanador_FK =
                            ultimaPuja.idUsuario_FK;
                    }

                    producto.Estado = "Finalizada";

                    await producto.Update<Productos>();
                }
            }

            // SOLO ACTIVAS
            productos = productos
                .Where(x => x.Estado == "Aceptado")
                .ToList();

            // IMÁGENES Y VENDEDOR (igual que CompradorController)
            foreach (var producto in productos)
            {
                var imagenResponse = await _supabase
                    .From<ProductoImagen>()
                    .Filter(
                        "idProducto_FK",
                        Supabase.Postgrest.Constants.Operator.Equals,
                        producto.IdProductos.ToString())
                    .Get();

                var imagen = imagenResponse.Models.FirstOrDefault();

                if (imagen != null)
                {
                    producto.ImagenUrl = imagen.UrlImagen;
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

            return View(productos);
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}