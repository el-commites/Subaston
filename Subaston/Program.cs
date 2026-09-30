using Microsoft.AspNetCore.Diagnostics.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Subaston.AccesoData.Data;
using Supabase;
using Subaston.Middleware;
using Subaston.Services;
using MercadoPago.Config;
using Subaston.Models.Models;

var builder = WebApplication.CreateBuilder(args);

// MVC
builder.Services.AddControllersWithViews();

// SUPABASE
var url = builder.Configuration["Supabase:Url"];
var key = builder.Configuration["Supabase:AnonKey"];

var options = new SupabaseOptions
{
    AutoConnectRealtime = true,
};

var supabase = new Supabase.Client(url, key, options);

builder.Services.AddSingleton(supabase);

builder.Services.AddHostedService<SubastaService>();

builder.Services.AddScoped<NotificacionService>();

builder.Services.AddHttpContextAccessor();

// MERCADO PAGO
MercadoPagoConfig.AccessToken = builder.Configuration["MercadoPago:AccessToken"];

// IIS
builder.WebHost.UseIIS();

// SESIONES
builder.Services.AddDistributedMemoryCache();

builder.Services.AddSession(options =>
{
    options.IdleTimeout =
        TimeSpan.FromMinutes(30);

    options.Cookie.HttpOnly = true;

    options.Cookie.IsEssential = true;
});

// BASE DE DATOS
var connectionString = builder.Configuration
    .GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException(
        "Connection string 'DefaultConnection' not found.");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(connectionString));

// IDENTITY
builder.Services
    .AddDefaultIdentity<IdentityUser>(options =>
    {
        options.SignIn.RequireConfirmedAccount = false;
    })
    .AddEntityFrameworkStores<ApplicationDbContext>();

// LOGIN PERSONALIZADO
builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath =
        "/Cuentas/Cuenta/Login";
});

//DATA PROTECTIO Y ENCRIPTACION
builder.Services.AddDataProtection();
builder.Services.AddScoped<IPasswordHasher<Usuarios>, PasswordHasher<Usuarios>>();

//SERVICO DE CORREO
builder.Services.AddTransient<IEmailService, EmailService>();

builder.Services.AddDatabaseDeveloperPageExceptionFilter();

var app = builder.Build();

// HTTP
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");

    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles();

app.UseRouting();

// SESSION
app.UseSession();

// AUTH
app.UseAuthentication();

app.UseAuthorization();

// MIDDLEWARE DE SESIÓN
app.UseMiddleware<SessionMiddleware>();

// RUTAS
app.MapControllerRoute(
    name: "areas",
    pattern:
    "{area:exists}/{controller=Home}/{action=Index}/{id?}");

app.MapControllerRoute(
    name: "default",
    pattern:
    "{controller=Home}/{action=Index}/{id?}");

app.MapRazorPages();

app.Run();