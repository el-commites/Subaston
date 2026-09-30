using Subaston.Models.Models;

namespace Subaston.ViewsModels
{
    public class HistorialViewModel
    {
        public bool Ganada { get; set; }

        public Productos Productos { get; set; }
        public decimal Monto { get; set; }
        public DateTime FechaPuja { get; set; }
    }
}