namespace Subaston.ViewsModels
{
    public class TicketViewModel
    {
        public int IdProducto { get; set; }
        public string NombreProducto { get; set; } = "";
        public string Categoria { get; set; } = "";
        public string ImagenUrl { get; set; } = "";
        public decimal MontoPagado { get; set; }
        public string NombreComprador { get; set; } = "";
        public string ApodoComprador { get; set; } = "";
        public string CorreoComprador { get; set; } = "";
        public string PaymentId { get; set; } = "";
        public DateTime FechaPago { get; set; }

        // Sucursal más cercana (hardcodeada / lista estática por ahora)
        public SucursalInfo SucursalCercana { get; set; } = new();
    }
}
