using HospitalNoShow.API.Extensions;
using HospitalNoShow.API.Middleware;
using HospitalNoShow.Infrastructure.Extensions;
using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);

// ── Servis Kayıtları ──────────────────────────────────────────────────────────

// Infrastructure: EF Core, Identity, UnitOfWork
builder.Services.AddInfrastructure(builder.Configuration);

// Application: Auth, Appointment, NoShow servisleri
builder.Services.AddApplicationServices(builder.Configuration);

// JWT Authentication & Authorization
builder.Services.AddJwtAuthentication(builder.Configuration);

// Controllers
builder.Services.AddControllers();
builder.Services.Configure<ApiBehaviorOptions>(options =>
{
    // Model validation hatalarını standart formatla döndür
    options.InvalidModelStateResponseFactory = context =>
    {
        var errors = context.ModelState
            .Where(e => e.Value?.Errors.Count > 0)
            .SelectMany(e => e.Value!.Errors.Select(x => x.ErrorMessage))
            .ToList();

        return new BadRequestObjectResult(new { errors });
    };
});

// Swagger
builder.Services.AddSwaggerWithJwt();

// CORS - Frontend için (gerekirse genişletilebilir)
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader());
});

var app = builder.Build();

// ── Middleware Pipeline ───────────────────────────────────────────────────────

// Global exception handling (en dışta olmalı)
app.UseMiddleware<ExceptionHandlingMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Hospital No-Show API v1");
        c.RoutePrefix = string.Empty; // Swagger ana sayfada açılır
    });
}

app.UseHttpsRedirection();
app.UseCors("AllowAll");

// Authentication & Authorization sırası önemli!
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapGet("/health", () => Results.Ok(new { status = "healthy" })).AllowAnonymous();

// ── Database Migration & Seed ─────────────────────────────────────────────────
await app.MigrateAndSeedAsync();

app.Run();
