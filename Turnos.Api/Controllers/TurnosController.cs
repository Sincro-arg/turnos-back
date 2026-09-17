using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Turnos.Api.Data;
using Turnos.Api.Models;

namespace Turnos.Api.Controllers
{
    [ApiController]
    [Route("api/turnos")]
    public class TurnosController : ControllerBase
    {
        private static readonly string[] ServiciosValidos = { "Corte", "Color", "Peinado" };

        private readonly AppDbContext _context;

        public TurnosController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<TurnoDto>>> GetTurnos([FromQuery] string? fecha)
        {
            var fechaFiltro = string.IsNullOrWhiteSpace(fecha)
                ? DateTime.Now.ToString("yyyy-MM-dd")
                : fecha;

            var turnos = await _context.Turnos
                .Where(t => t.Fecha == fechaFiltro)
                .OrderBy(t => t.Hora)
                .ToListAsync();

            return Ok(turnos.Select(ToDto));
        }

        [HttpPost]
        public async Task<ActionResult<TurnoDto>> CreateTurno([FromBody] TurnoInputDto dto)
        {
            var error = ValidarCampos(dto);
            if (error != null) return BadRequest(new { error });

            var choque = await _context.Turnos
                .AnyAsync(t => t.Fecha == dto.Fecha && t.Hora == dto.Hora);
            if (choque) return Conflict(new { error = $"Ya hay un turno a las {dto.Hora}" });

            var turno = new Turno
            {
                Id = Guid.NewGuid(),
                Cliente = dto.Cliente!,
                Telefono = dto.Telefono!,
                Servicio = dto.Servicio!,
                Fecha = dto.Fecha!,
                Hora = dto.Hora!,
            };

            _context.Turnos.Add(turno);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetTurnos), new { fecha = turno.Fecha }, ToDto(turno));
        }

        [HttpPut("{id}")]
        public async Task<ActionResult<TurnoDto>> UpdateTurno(Guid id, [FromBody] TurnoInputDto dto)
        {
            var turno = await _context.Turnos.FindAsync(id);
            if (turno == null) return NotFound(new { error = "Turno no encontrado" });

            var error = ValidarCampos(dto);
            if (error != null) return BadRequest(new { error });

            var choque = await _context.Turnos
                .AnyAsync(t => t.Id != id && t.Fecha == dto.Fecha && t.Hora == dto.Hora);
            if (choque) return Conflict(new { error = $"Ya hay un turno a las {dto.Hora}" });

            turno.Cliente = dto.Cliente!;
            turno.Telefono = dto.Telefono!;
            turno.Servicio = dto.Servicio!;
            turno.Fecha = dto.Fecha!;
            turno.Hora = dto.Hora!;

            await _context.SaveChangesAsync();

            return Ok(ToDto(turno));
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteTurno(Guid id)
        {
            var turno = await _context.Turnos.FindAsync(id);
            if (turno == null) return NotFound(new { error = "Turno no encontrado" });

            _context.Turnos.Remove(turno);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private static string? ValidarCampos(TurnoInputDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Cliente)) return "El campo 'cliente' es obligatorio.";
            if (string.IsNullOrWhiteSpace(dto.Telefono)) return "El campo 'telefono' es obligatorio.";
            if (string.IsNullOrWhiteSpace(dto.Fecha)) return "El campo 'fecha' es obligatorio.";
            if (string.IsNullOrWhiteSpace(dto.Hora)) return "El campo 'hora' es obligatorio.";
            if (string.IsNullOrWhiteSpace(dto.Servicio) || !ServiciosValidos.Contains(dto.Servicio))
                return "El campo 'servicio' debe ser Corte, Color o Peinado.";
            return null;
        }

        private static TurnoDto ToDto(Turno t) => new(
            t.Id.ToString(),
            t.Cliente,
            t.Telefono,
            t.Servicio,
            t.Fecha,
            t.Hora
        );
    }

    public record TurnoDto(string Id, string Cliente, string Telefono, string Servicio, string Fecha, string Hora);

    public record TurnoInputDto(string? Cliente, string? Telefono, string? Servicio, string? Fecha, string? Hora);
}
