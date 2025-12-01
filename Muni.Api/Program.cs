using Muni.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Muni.Application;
using MercadoPago.Config;
using Muni.Infrastructure.Integrations.Dnrpa;
using Microsoft.Extensions.Options;
using QuestPDF.Infrastructure;   // <-- agregado arriba del todo

var builder = WebApplication.CreateBuilder(args);
var cfg = builder.Configuration;
// Options de Pago Fácil (ya lo tenías)
builder.Services.Configure<PagoFacilOptions>(
    builder.Configuration.GetSection("PagoFacil"));

// DB
builder.Services.AddDbContext<MuniDbContext>(opt =>
    opt.UseSqlServer(builder.Configuration.GetConnectionString("Sql")));

builder.Services.AddScoped<IBillingService, BillingService>();

//SWAGGER
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

//DNRPA
builder.Services.Configure<DnrpaOptions>(
    builder.Configuration.GetSection("Dnrpa"));

builder.Services.AddHttpClient<IDnrpaClient, DnrpaClient>((sp, client) =>
{
    var opts = sp.GetRequiredService<IOptions<DnrpaOptions>>().Value;

    if (string.IsNullOrWhiteSpace(opts.BaseUrl))
        throw new InvalidOperationException("Dnrpa:BaseUrl no está configurado.");

    client.BaseAddress = new Uri(opts.BaseUrl);
    client.Timeout = TimeSpan.FromSeconds(opts.TimeoutSeconds);
});
builder.Services.AddScoped<IDnrpaImporter, DnrpaImporter>();

QuestPDF.Settings.License = LicenseType.Community;

// ---------- INICIALIZAR MERCADO PAGO SDK ----------
var mpAccessToken = builder.Configuration["MercadoPago:AccessToken"];
if (string.IsNullOrWhiteSpace(mpAccessToken))
    throw new InvalidOperationException("Falta MercadoPago:AccessToken en configuración.");

MercadoPagoConfig.AccessToken = mpAccessToken;
var prefix = mpAccessToken.Length >= 5 ? mpAccessToken[..5] : mpAccessToken;
Console.WriteLine($"[MP] AccessToken prefix={(mpAccessToken.StartsWith("TEST-") ? "TEST-" : prefix)} len={mpAccessToken.Length}");

// (Opcional) MercadoPagoConfig.CorporationId = "...";
// -----------------------------------------------

var app = builder.Build();
if (app.Environment.IsDevelopment()) { app.UseSwagger(); app.UseSwaggerUI(); }

// servir frontend estático
app.UseDefaultFiles();   // busca index.html por defecto
app.UseStaticFiles();    // habilita wwwroot
app.Use(async (ctx, next) =>
{
    ctx.Response.Headers.TryAdd("X-Content-Type-Options", "nosniff");
    await next();
});

app.MapControllers();
app.Run();

public class PagoFacilOptions { public string Empresa4 { get; set; } = "0000"; }

