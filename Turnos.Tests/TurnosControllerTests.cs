using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Turnos.Api.Controllers;
using Turnos.Api.Data;
using Turnos.Api.Models;
using Xunit;

namespace Turnos.Tests;

public class TurnosControllerTests
{
    private static TurnosController BuildController(out AppDbContext db)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"turnos-{Guid.NewGuid()}")
            .Options;
        db = new AppDbContext(options);
        return new TurnosController(db);
    }

    private static TurnoInputDto NuevoInput(string hora, string servicio = "Corte", string fecha = "2026-09-20") =>
        new("Maria Lopez", "1122334455", servicio, fecha, hora);

    [Fact]
    public async Task GetTurnos_DevuelveLosDeLaFechaOrdenadosPorHora()
    {
        var ctrl = BuildController(out var db);
        db.Turnos.AddRange(
            new Turno { Id = Guid.NewGuid(), Cliente = "B", Telefono = "1", Servicio = "Corte", Fecha = "2026-09-20", Hora = "15:00" },
            new Turno { Id = Guid.NewGuid(), Cliente = "A", Telefono = "2", Servicio = "Color", Fecha = "2026-09-20", Hora = "09:00" },
            new Turno { Id = Guid.NewGuid(), Cliente = "C", Telefono = "3", Servicio = "Peinado", Fecha = "2026-09-21", Hora = "10:00" }
        );
        await db.SaveChangesAsync();

        var result = await ctrl.GetTurnos("2026-09-20");

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var turnos = Assert.IsAssignableFrom<IEnumerable<TurnoDto>>(ok.Value).ToList();
        Assert.Equal(2, turnos.Count);
        Assert.Equal("09:00", turnos[0].Hora);
        Assert.Equal("15:00", turnos[1].Hora);
    }

    [Fact]
    public async Task CreateTurno_ConDatosValidos_DevuelveCreatedConElDto()
    {
        var ctrl = BuildController(out var db);

        var result = await ctrl.CreateTurno(NuevoInput("11:00"));

        var created = Assert.IsType<CreatedAtActionResult>(result.Result);
        var dto = Assert.IsType<TurnoDto>(created.Value);
        Assert.Equal("Maria Lopez", dto.Cliente);
        Assert.Equal("11:00", dto.Hora);
        Assert.Single(await db.Turnos.ToListAsync());
    }

    [Fact]
    public async Task CreateTurno_ConServicioInvalido_DevuelveBadRequest()
    {
        var ctrl = BuildController(out _);

        var result = await ctrl.CreateTurno(NuevoInput("11:00", servicio: "Manicura"));

        var bad = Assert.IsType<BadRequestObjectResult>(result.Result);
        var body = Assert.IsAssignableFrom<object>(bad.Value);
        var error = body.GetType().GetProperty("error")!.GetValue(body) as string;
        Assert.False(string.IsNullOrWhiteSpace(error));
    }

    [Fact]
    public async Task CreateTurno_SinCliente_DevuelveBadRequest()
    {
        var ctrl = BuildController(out _);

        var result = await ctrl.CreateTurno(NuevoInput("11:00") with { Cliente = null });

        var bad = Assert.IsType<BadRequestObjectResult>(result.Result);
        var error = bad.Value!.GetType().GetProperty("error")!.GetValue(bad.Value) as string;
        Assert.False(string.IsNullOrWhiteSpace(error));
    }

    [Fact]
    public async Task CreateTurno_SinTelefono_DevuelveBadRequest()
    {
        var ctrl = BuildController(out _);

        var result = await ctrl.CreateTurno(NuevoInput("11:00") with { Telefono = null });

        var bad = Assert.IsType<BadRequestObjectResult>(result.Result);
        var error = bad.Value!.GetType().GetProperty("error")!.GetValue(bad.Value) as string;
        Assert.False(string.IsNullOrWhiteSpace(error));
    }

    [Fact]
    public async Task CreateTurno_SinFecha_DevuelveBadRequest()
    {
        var ctrl = BuildController(out _);

        var result = await ctrl.CreateTurno(NuevoInput("11:00") with { Fecha = null });

        var bad = Assert.IsType<BadRequestObjectResult>(result.Result);
        var error = bad.Value!.GetType().GetProperty("error")!.GetValue(bad.Value) as string;
        Assert.False(string.IsNullOrWhiteSpace(error));
    }

    [Fact]
    public async Task CreateTurno_SinHora_DevuelveBadRequest()
    {
        var ctrl = BuildController(out _);

        var result = await ctrl.CreateTurno(NuevoInput("11:00") with { Hora = null });

        var bad = Assert.IsType<BadRequestObjectResult>(result.Result);
        var error = bad.Value!.GetType().GetProperty("error")!.GetValue(bad.Value) as string;
        Assert.False(string.IsNullOrWhiteSpace(error));
    }

    [Fact]
    public async Task CreateTurno_EnHorarioYaOcupado_DevuelveConflict()
    {
        var ctrl = BuildController(out var db);
        db.Turnos.Add(new Turno { Id = Guid.NewGuid(), Cliente = "X", Telefono = "1", Servicio = "Corte", Fecha = "2026-09-20", Hora = "11:00" });
        await db.SaveChangesAsync();

        var result = await ctrl.CreateTurno(NuevoInput("11:00"));

        var conflict = Assert.IsType<ConflictObjectResult>(result.Result);
        var error = conflict.Value!.GetType().GetProperty("error")!.GetValue(conflict.Value) as string;
        Assert.Equal("Ya hay un turno a las 11:00", error);
    }

    [Fact]
    public async Task UpdateTurno_ConDatosValidos_ActualizaYDevuelveElDto()
    {
        var ctrl = BuildController(out var db);
        var turno = new Turno { Id = Guid.NewGuid(), Cliente = "Original", Telefono = "1", Servicio = "Corte", Fecha = "2026-09-20", Hora = "09:00" };
        db.Turnos.Add(turno);
        await db.SaveChangesAsync();

        var result = await ctrl.UpdateTurno(turno.Id, NuevoInput("10:00", servicio: "Color"));

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var dto = Assert.IsType<TurnoDto>(ok.Value);
        Assert.Equal("10:00", dto.Hora);
        Assert.Equal("Color", dto.Servicio);
    }

    [Fact]
    public async Task UpdateTurno_ConCampoObligatorioFaltante_DevuelveBadRequest()
    {
        var ctrl = BuildController(out var db);
        var turno = new Turno { Id = Guid.NewGuid(), Cliente = "Original", Telefono = "1", Servicio = "Corte", Fecha = "2026-09-20", Hora = "09:00" };
        db.Turnos.Add(turno);
        await db.SaveChangesAsync();

        var result = await ctrl.UpdateTurno(turno.Id, NuevoInput("10:00") with { Cliente = null });

        var bad = Assert.IsType<BadRequestObjectResult>(result.Result);
        var error = bad.Value!.GetType().GetProperty("error")!.GetValue(bad.Value) as string;
        Assert.False(string.IsNullOrWhiteSpace(error));
    }

    [Fact]
    public async Task UpdateTurno_ConServicioInvalido_DevuelveBadRequest()
    {
        var ctrl = BuildController(out var db);
        var turno = new Turno { Id = Guid.NewGuid(), Cliente = "Original", Telefono = "1", Servicio = "Corte", Fecha = "2026-09-20", Hora = "09:00" };
        db.Turnos.Add(turno);
        await db.SaveChangesAsync();

        var result = await ctrl.UpdateTurno(turno.Id, NuevoInput("10:00", servicio: "Manicura"));

        var bad = Assert.IsType<BadRequestObjectResult>(result.Result);
        var error = bad.Value!.GetType().GetProperty("error")!.GetValue(bad.Value) as string;
        Assert.False(string.IsNullOrWhiteSpace(error));
    }

    [Fact]
    public async Task UpdateTurno_ConIdInexistente_DevuelveNotFound()
    {
        var ctrl = BuildController(out _);

        var result = await ctrl.UpdateTurno(Guid.NewGuid(), NuevoInput("10:00"));

        var notFound = Assert.IsType<NotFoundObjectResult>(result.Result);
        var error = notFound.Value!.GetType().GetProperty("error")!.GetValue(notFound.Value) as string;
        Assert.Equal("Turno no encontrado", error);
    }

    [Fact]
    public async Task DeleteTurno_ConIdExistente_LoBorraYDevuelveNoContent()
    {
        var ctrl = BuildController(out var db);
        var turno = new Turno { Id = Guid.NewGuid(), Cliente = "A", Telefono = "1", Servicio = "Corte", Fecha = "2026-09-20", Hora = "09:00" };
        db.Turnos.Add(turno);
        await db.SaveChangesAsync();

        var result = await ctrl.DeleteTurno(turno.Id);

        Assert.IsType<NoContentResult>(result);
        Assert.Empty(await db.Turnos.ToListAsync());
    }

    [Fact]
    public async Task DeleteTurno_ConIdInexistente_DevuelveNotFound()
    {
        var ctrl = BuildController(out _);

        var result = await ctrl.DeleteTurno(Guid.NewGuid());

        var notFound = Assert.IsType<NotFoundObjectResult>(result);
        var error = notFound.Value!.GetType().GetProperty("error")!.GetValue(notFound.Value) as string;
        Assert.Equal("Turno no encontrado", error);
    }
}
