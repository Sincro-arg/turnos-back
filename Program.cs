using Microsoft.AspNetCore.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using Turnos.Api.Data;
using Turnos.Api.Models;

var builder = WebApplication.CreateBuilder(args);

var portEnv = Environment.GetEnvironmentVariable("PORT");
if (!string.IsNullOrEmpty(portEnv))
{
    builder.WebHost.UseUrls($"http://0.0.0.0:{portEnv}");
}

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? "Data Source=turnos.db";

builder.Services.AddDbContext<AppDbContext>(options => options.UseSqlite(connectionString));

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Turnos API", Version = "v1" });
});

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAngular", policy =>
    {
        policy.WithOrigins("http://localhost:4200")
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

var app = builder.Build();

// ── Swagger solo en Development ──────────────────────────────────────────────
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Turnos API V1");
        c.RoutePrefix = "swagger";
    });
}

// Exception handler global: nunca un stack trace, siempre { "error": "..." }.
// Antes de CORS para que la respuesta de error tambien lleve los headers de CORS.
app.UseExceptionHandler(errApp => errApp.Run(async ctx =>
{
    ctx.Response.StatusCode = 500;
    ctx.Response.ContentType = "application/json";
    var ex = ctx.Features.Get<IExceptionHandlerFeature>()?.Error;
    var logger = ctx.RequestServices.GetRequiredService<ILogger<Program>>();
    logger.LogError(ex, "Unhandled exception on {Method} {Path}", ctx.Request.Method, ctx.Request.Path);
    await ctx.Response.WriteAsync("{\"error\":\"Error interno del servidor\"}");
}));

app.UseCors("AllowAngular");
app.UseRouting();
app.UseAuthorization();

app.MapControllers();

// Health check (sin auth)
app.MapGet("/health", () => Results.Ok(new { status = "healthy" }));

using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var db = services.GetRequiredService<AppDbContext>();

    await db.Database.MigrateAsync();

    if (!await db.Turnos.AnyAsync())
    {
        var hoy = DateTime.Now.ToString("yyyy-MM-dd");
        db.Turnos.AddRange(
            new Turno { Id = Guid.NewGuid(), Cliente = "Maria Lopez", Telefono = "1122334455", Servicio = "Corte", Fecha = hoy, Hora = "09:00" },
            new Turno { Id = Guid.NewGuid(), Cliente = "Juan Perez", Telefono = "1155667788", Servicio = "Color", Fecha = hoy, Hora = "10:30" },
            new Turno { Id = Guid.NewGuid(), Cliente = "Ana Gomez", Telefono = "1199887766", Servicio = "Peinado", Fecha = hoy, Hora = "12:00" },
            new Turno { Id = Guid.NewGuid(), Cliente = "Carlos Diaz", Telefono = "1166554433", Servicio = "Corte", Fecha = hoy, Hora = "15:00" }
        );
        await db.SaveChangesAsync();
    }
}

await app.RunAsync();
