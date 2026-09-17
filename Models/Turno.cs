using System.ComponentModel.DataAnnotations;

namespace Turnos.Api.Models
{
    public class Turno
    {
        public Guid Id { get; set; }

        [Required]
        public string Cliente { get; set; } = string.Empty;

        [Required]
        public string Telefono { get; set; } = string.Empty;

        // Restringido a "Corte" | "Color" | "Peinado"
        [Required]
        public string Servicio { get; set; } = string.Empty;

        // "YYYY-MM-DD"
        [Required]
        public string Fecha { get; set; } = string.Empty;

        // "HH:mm"
        [Required]
        public string Hora { get; set; } = string.Empty;
    }
}
