using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Municipalidad.Api.Clients;
using Municipalidad.Api.Controllers;
using Municipalidad.Api.Infrastructure.Persistence;
using Municipalidad.Api.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection("Jwt"));
builder.Services.Configure<DnrpaOptions>(builder.Configuration.GetSection("Clients:DNRPA"));
builder.Services.Configure<ArcaOptions>(builder.Configuration.GetSection("Clients:ARCA"));
builder.Services.Configure<PagoFacilOptions>(builder.Configuration.GetSection("Clients:PagoFacil"));
builder.Services.Configure<MercadoPagoOptions>(builder.Configuration.GetSection("Clients:MercadoPago"));

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? "Data Source=municipalidad.db";
builder.Services.AddDbContext<MunicipalidadDbContext>(options => options.UseSqlite(connectionString));

builder.Services.AddHttpClient();

builder.Services.AddScoped<DnrpaClient>();
builder.Services.AddScoped<ArcaClient>();
builder.Services.AddScoped<IPagoFacilClient, PagoFacilClient>();
builder.Services.AddScoped<IMercadoPagoClient, MercadoPagoClient>();

builder.Services.AddScoped<VehicleRegistryService>();
builder.Services.AddScoped<TaxAssessmentService>();
builder.Services.AddScoped<BillingService>();
builder.Services.AddScoped<PaymentService>();
builder.Services.AddHostedService<DatabaseInitializer>();

var jwtKey = builder.Configuration.GetValue<string>("Jwt:Key") ?? "InsecureDevelopmentKey123!";
var issuer = builder.Configuration.GetValue<string>("Jwt:Issuer") ?? "Municipalidad";
var audience = builder.Configuration.GetValue<string>("Jwt:Audience") ?? "MunicipalidadClients";

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
}).AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateIssuerSigningKey = true,
        ValidateLifetime = true,
        ValidIssuer = issuer,
        ValidAudience = audience,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
    };
});

builder.Services.AddAuthorization();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
