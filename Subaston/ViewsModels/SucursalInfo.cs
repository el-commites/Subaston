namespace Subaston.ViewsModels
{
    // Información de una sucursal física de Subaston, usada para mostrar
    // a los vendedores dónde entregar el producto una vez aprobada su publicación.
    public class SucursalInfo
    {
        public string Nombre { get; set; } = "";
        public string Direccion { get; set; } = "";
        public string Ciudad { get; set; } = "";
        public string Telefono { get; set; } = "";
        public string Horario { get; set; } = "";
        public string MapUrl { get; set; } = "";
    }
}
