using ERPKardex.Data;
using ERPKardex.Models;
using ERPKardex.Services;
using ERPKardex.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

// Entity Framework Core
builder.Services.AddDbContext<ApplicationDbContext>((serviceProvider, options) =>
{
    // A. Recuperamos el interceptor que acabamos de registrar arriba
    var interceptor = serviceProvider.GetRequiredService<ERPKardex.Data.AuditoriaInterceptor>();

    // B. Obtenemos la cadena de conexión
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

    // C. Configuramos SQL Server Y agregamos el interceptor en la misma línea
    options.UseSqlServer(connectionString)
           .AddInterceptors(interceptor);
});

// Usar Cookies
builder.Services.AddAuthentication("Cookies")
    .AddCookie("Cookies", options =>
    {
        options.LoginPath = "/Home/Login";
        options.AccessDeniedPath = "/Home/AccesoDenegado";
    });

// Usar Session
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(60);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

builder.Services.AddControllersWithViews()
    .AddJsonOptions(options =>
    {
        // Esto obliga a que los decimales en el JSON siempre usen punto (.)
        options.JsonSerializerOptions.NumberHandling = System.Text.Json.Serialization.JsonNumberHandling.AllowReadingFromString;
    });

// Configurar el Worker automático
//builder.Services.AddHostedService<ERPKardex.Workers.VerificadorVencimientosWorker>();

// CACHÉ PARA PERMISOS
builder.Services.AddMemoryCache(); // Habilitar Caché en RAM
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ERPKardex.Data.AuditoriaInterceptor>();
builder.Services.AddScoped<ERPKardex.Services.IPermisoService, ERPKardex.Services.PermisoService>();

// CORREOS 
// Mapear la sección "SmtpSettings" a nuestra clase
builder.Services.Configure<SmtpSettings>(builder.Configuration.GetSection("SmtpSettings"));

// Registrar el servicio de correos
builder.Services.AddScoped<IEmailService, EmailService>();


// --- CONFIGURACI�N DE CULTURA H�BRIDA ---
var cultureInfo = new System.Globalization.CultureInfo("es-PE");

// Forzamos que los n�meros siempre usen PUNTO, sin importar que sea espa�ol
cultureInfo.NumberFormat.NumberDecimalSeparator = ".";
cultureInfo.NumberFormat.CurrencyDecimalSeparator = ".";

var supportedCultures = new[] { cultureInfo };

var localizationOptions = new RequestLocalizationOptions()
    .SetDefaultCulture(supportedCultures[0].Name)
    .AddSupportedCultures(supportedCultures.Select(c => c.Name).ToArray())
    .AddSupportedUICultures(supportedCultures.Select(c => c.Name).ToArray());

// Esto es lo que realmente har� que el JSON ignore la coma
localizationOptions.DefaultRequestCulture = new Microsoft.AspNetCore.Localization.RequestCulture(cultureInfo);

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

// Aplicar la configuraci�n
app.UseRequestLocalization(localizationOptions);
app.UseStaticFiles();
app.UseHttpsRedirection();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.UseSession();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();


app.Run();
