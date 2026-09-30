using Microsoft.Extensions.Hosting;
using Subaston.Models.Models;
using Supabase;
using Subaston.Services;

namespace Subaston.Services
{
    public class SubastaService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;

        public SubastaService(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                using var scope = _serviceProvider.CreateScope();

                var supabase = scope.ServiceProvider
                    .GetRequiredService<Supabase.Client>();
                var notificacionService = scope.ServiceProvider.GetRequiredService<NotificacionService>();

                // SOLO PRODUCTOS ACEPTADOS (no tocar Pagado ni Finalizada)
                var response = await supabase
                    .From<Productos>()
                    .Where(x => x.Estado == "Aceptado")
                    .Get();

                var productos = response.Models;

                foreach (var producto in productos)
                {
                    if (producto.FechaCierre.ToUniversalTime() <= DateTime.UtcNow)
                    {
                        // BUSCAR PUJA MÁS ALTA
                        var pujasResponse = await supabase
                            .From<HistorialPujas>()
                            .Where(x => x.idProducto_FK == producto.IdProductos)
                            .Order(x => x.Monto,
                                Supabase.Postgrest.Constants.Ordering.Descending)
                            .Limit(1)
                            .Get();

                        var ultimaPuja = pujasResponse.Models.FirstOrDefault();

                        if (ultimaPuja != null)
                        {
                            producto.idGanador_FK = ultimaPuja.idUsuario_FK;
                        }

                        // SOLO FINALIZAR — nunca sobreescribir "Pagado"
                        producto.Estado = "Finalizada";

                        await producto.Update<Productos>();
                        if (producto.idGanador_FK.HasValue)
                        {
                            await notificacionService.CrearAsync(
                                producto.idGanador_FK.Value,
                                "Ganaste la subasta",
                                $"Ganaste la subasta de '{producto.NombreDelProducto}'.",
                                "Subasta",
                                "/Comprador/Comprador/Historial"
                            );
                        }
                    }
                }

                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
            }
        }
    }
}