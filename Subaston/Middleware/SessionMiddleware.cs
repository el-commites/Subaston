using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.DependencyInjection;
using Subaston.Models.Models;
using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace Subaston.Middleware
{
    public class SessionMiddleware
    {
        private readonly RequestDelegate _next;

        public SessionMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context, ITempDataDictionaryFactory tempDataFactory, IDataProtectionProvider provider)
        {
            var ruta = context.Request.Path.Value?.ToLower() ?? "";
            var usuario = context.Session.GetString("Usuario");
            var rol = context.Session.GetString("Rol");

            // RESTAURAR SESIÓN CON COOKIE "RECORDARME"
            if (string.IsNullOrEmpty(usuario))
            {
                var cookie = context.Request.Cookies["Subaston_RememberMe"];
                if (!string.IsNullOrEmpty(cookie))
                {
                    try
                    {
                        var protector = provider.CreateProtector("Subaston.Auth");
                        var email = protector.Unprotect(cookie);

                        var supabase = context.RequestServices.GetRequiredService<Supabase.Client>();
                        var response = await supabase
                            .From<Usuarios>()
                            .Where(x => x.Correo == email)
                            .Get();

                        var dbUsuario = response.Models.FirstOrDefault();
                        if (dbUsuario != null && dbUsuario.Verificado)
                        {
                            // Restaurar la sesión
                            context.Session.SetString("Usuario", dbUsuario.Correo ?? "");
                            context.Session.SetString("Apodo", dbUsuario.Apodo ?? "");
                            context.Session.SetString("Rol", dbUsuario.Rol ?? "");
                            context.Session.SetInt32("IdUsuario", (int)(dbUsuario.IdUsuario ?? 0));
                            context.Session.SetString("FotoPerfil", dbUsuario.FotoPerfil ?? "");

                            // Actualizar variables locales para validaciones posteriores del middleware
                            usuario = dbUsuario.Correo;
                            rol = dbUsuario.Rol;
                        }
                    }
                    catch
                    {
                        // Cookie inválida, corrupta o expirada -> la eliminamos
                        context.Response.Cookies.Delete("Subaston_RememberMe");
                    }
                }
            }

            // --- DESHABILITAR CACHÉ EN RUTAS PROTEGIDAS PARA EL BOTÓN "ATRÁS" ---
            // Si el usuario sale (Logout), no debe poder regresar con las flechas del navegador.
            bool esProtegida = ruta.StartsWith("/admin") || 
                               ruta.StartsWith("/vendedor") || 
                               ruta.StartsWith("/comprador") || 
                               ruta.StartsWith("/pago");
            if (esProtegida)
            {
                context.Response.Headers["Cache-Control"] = "no-cache, no-store, must-revalidate";
                context.Response.Headers["Pragma"] = "no-cache";
                context.Response.Headers["Expires"] = "0";
            }

            // Rutas que no requieren sesión aunque sean del área Comprador
            var rutasPublicas = new[]
            {
                "/comprador/comprador/index",
            };

            bool esRutaPublica = rutasPublicas.Any(r => ruta.StartsWith(r)) ||
                                  ruta == "/comprador/comprador" ||
                                  ruta == "/comprador/comprador/";

            // --- REDIRECCIÓN AL LOGIN SI NO HAY SESIÓN ---
            // Si la ruta es protegida y no tiene un usuario en sesión, se le manda al login.
            bool requiereLogin =
                (ruta.StartsWith("/admin") ||
                 ruta.StartsWith("/vendedor") ||
                 ruta.StartsWith("/pago") ||
                 (ruta.StartsWith("/comprador") && !esRutaPublica))
                && string.IsNullOrEmpty(usuario);

            if (requiereLogin)
            {
                // Limpiar TempData para que no aparezca en Login
                var tempData = tempDataFactory.GetTempData(context);
                tempData.Clear();
                tempData.Save();

                context.Response.Redirect("/Cuentas/Cuenta/Login");
                return;
            }

            // --- EVITAR VOLVER A LOGIN O REGISTRO SI YA TIENE SESIÓN ---
            bool esRutaAcceso = ruta == "/cuentas/cuenta/login" || 
                                ruta == "/cuentas/cuenta/registrar" || 
                                ruta == "/cuentas/cuenta" || 
                                ruta == "/cuentas/cuenta/";
            if (!string.IsNullOrEmpty(usuario) && esRutaAcceso)
            {
                if (rol == "Admin")
                {
                    context.Response.Redirect("/Admin/Admin/Index");
                    return;
                }
                if (rol == "Vendedor")
                {
                    context.Response.Redirect("/Vendedor/Vendedor/Index");
                    return;
                }
                if (rol == "Comprador")
                {
                    context.Response.Redirect("/Comprador/Comprador/Index");
                    return;
                }
            }

            // --- PROTECCIÓN DE ÁREAS CRUZADAS ---
            if (!string.IsNullOrEmpty(usuario))
            {
                // ROL ADMIN
                if (ruta.StartsWith("/admin") && rol != "Admin")
                {
                    RedirectToRoleHome(context, rol);
                    return;
                }

                // ROL COMPRADOR (solo rutas protegidas)
                if (ruta.StartsWith("/comprador") && !esRutaPublica && rol != "Comprador")
                {
                    RedirectToRoleHome(context, rol);
                    return;
                }

                // ROL VENDEDOR
                if (ruta.StartsWith("/vendedor") && rol != "Vendedor")
                {
                    RedirectToRoleHome(context, rol);
                    return;
                }
            }

            await _next(context);
        }

        // --- AUXILIAR PARA REDIRECCIONAR AL INDEX DE SU ROL ---
        private void RedirectToRoleHome(HttpContext context, string? rol)
        {
            if (rol == "Admin")
            {
                context.Response.Redirect("/Admin/Admin/Index");
            }
            else if (rol == "Vendedor")
            {
                context.Response.Redirect("/Vendedor/Vendedor/Index");
            }
            else if (rol == "Comprador")
            {
                context.Response.Redirect("/Comprador/Comprador/Index");
            }
            else
            {
                context.Response.Redirect("/Cuentas/Cuenta/Login");
            }
        }
    }
}