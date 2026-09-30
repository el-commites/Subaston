using Microsoft.AspNetCore.Mvc;
using Subaston.Models.Models;
using Subaston.ViewsModels;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Subaston.Services;
using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using System.Text.Json;

namespace Subaston.Areas.Cuentas.Controllers
{
    [Area("Cuentas")]
    public class CuentaController : Controller
    {
        private readonly Supabase.Client _supabase;
        private readonly IEmailService _emailService;
        private readonly IDataProtector _protector;
        private readonly ITimeLimitedDataProtector _regProtector;
        private readonly IPasswordHasher<Usuarios> _passwordHasher;

        public CuentaController(
            Supabase.Client supabase,
            IEmailService emailService,
            IDataProtectionProvider provider,
            IPasswordHasher<Usuarios> passwordHasher)
        {
            _supabase = supabase;
            _emailService = emailService;
            _protector = provider.CreateProtector("Subaston.Auth");
            _regProtector = provider.CreateProtector("Subaston.Registration").ToTimeLimitedDataProtector();
            _passwordHasher = passwordHasher;
        }

        // LOGIN GET
        public IActionResult Login()
        {
            return View();
        }

        // REGISTRAR GET
        public IActionResult Registrar()
        {
            return View();
        }

        // POLITICAS DE PRIVACIDAD
        public IActionResult PoliticasPrivacidad()
        {
            return View();
        }

        // REGISTRAR POST    
        [HttpPost]
        public async Task<IActionResult> Registrar(
            RegistrarViewsModels model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // VERIFICAR SI YA EXISTE EL CORREO EN LA BD
            var existe = await _supabase
                .From<Usuarios>()
                .Where(x => x.Correo == model.Correo)
                .Get();

            if (existe.Models.Count > 0)
            {
                ModelState.AddModelError(
                    "",
                    "El correo ya está registrado");

                return View(model);
            }

            // HASHEAR CONTRASEÑA
            var dummyUser = new Usuarios { Correo = model.Correo };
            var passwordHash = _passwordHasher.HashPassword(dummyUser, model.Contraseña ?? "");

            var tempUser = new TempRegistroDto
            {
                Nombre = model.Nombre ?? "",
                Apodo = model.Apodo ?? "",
                Correo = model.Correo ?? "",
                PasswordHash = passwordHash,
                Rol = model.Rol ?? ""
            };

            // SERIALIZAR Y CIFRAR DATOS CON TIEMPO DE EXPIRACIÓN DE 24 HORAS
            string json = JsonSerializer.Serialize(tempUser);
            string encryptedData = _regProtector.Protect(json, TimeSpan.FromHours(24));

            // ENVIAR CORREO DE VERIFICACIÓN
            try
            {
                var callbackUrl = $"{Request.Scheme}://{Request.Host}/Cuentas/Cuenta/ConfirmarRegistro?datos={Uri.EscapeDataString(encryptedData ?? "")}";

                var body = $@"
                    <div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto; padding: 20px; border: 1px solid #eee; border-radius: 5px; background-color: #fdfdfd;'>
                        <div style='text-align: center; margin-bottom: 20px;'>
                            <h2 style='color: #111; margin: 0;'>SUBASTON</h2>
                            <p style='color: #666; font-size: 14px; margin: 5px 0 0 0;'>Tus coleccionables te esperan</p>
                        </div>
                        <hr style='border: none; border-top: 1px solid #eee; margin-bottom: 20px;' />
                        <h3 style='color: #333;'>¡Hola, {model.Nombre}! completemos tu registro</h3>
                        <p>Gracias por tu interés en registrarte en nuestra plataforma de subastas. Para finalizar tu registro y crear tu cuenta de forma segura, por favor confirma tu dirección de correo electrónico haciendo clic en el botón de abajo:</p>
                        <div style='text-align: center; margin: 30px 0;'>
                            <a href='{callbackUrl}' style='background-color: #28a745; color: white; padding: 12px 24px; text-decoration: none; border-radius: 4px; font-weight: bold; display: inline-block;'>Confirmar Correo y Crear Cuenta</a>
                        </div>
                        <p>Si el botón no funciona, puedes copiar y pegar el siguiente enlace en tu navegador:</p>
                        <p style='word-break: break-all; color: #666;'><a href='{callbackUrl}'>{callbackUrl}</a></p>
                        <hr style='border: none; border-top: 1px solid #eee; margin: 20px 0;' />
                        <p style='font-size: 11px; color: #999; text-align: center;'>Este es un correo automático generado por Subaston. Por favor no respondas a este mensaje.</p>
                    </div>";

                await _emailService.SendEmailAsync(model.Correo ?? "", "Completa tu registro en Subaston", body);

                TempData["Correcto"] = "Te hemos enviado un enlace de confirmación por correo. Por favor revisa tu bandeja de entrada para completar tu registro.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Ocurrió un error al enviar el correo de verificación: " + ex.Message;
            }

            return RedirectToAction("Login");
        }

        // LOGIN POST
        [HttpPost]
        public async Task<IActionResult> Login(
            LoginViewsModels model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // BUSCAR USUARIO POR CORREO
            var response = await _supabase
                .From<Usuarios>()
                .Where(x => x.Correo == model.Correo)
                .Get();

            var usuario = response.Models.FirstOrDefault();

            // VALIDAR CREDENCIALES
            if (usuario == null)
            {
                ViewBag.Error = "Credenciales incorrectas";
                return View(model);
            }

            bool contraseñaValida = false;
            try
            {
                var verificationResult = _passwordHasher.VerifyHashedPassword(usuario, usuario.Contraseña ?? "", model.Contraseña ?? "");
                if (verificationResult == PasswordVerificationResult.Success || verificationResult == PasswordVerificationResult.SuccessRehashNeeded)
                {
                    contraseñaValida = true;
                }
            }
            catch (System.FormatException)
            {
                // La contraseña no está en formato hash (es texto plano)
            }

            if (!contraseñaValida && usuario.Contraseña == model.Contraseña) // Fallback para contraseñas de texto plano previas
            {
                contraseñaValida = true;

                // Re-hashear y guardar contraseña encriptada
                usuario.Contraseña = _passwordHasher.HashPassword(usuario, model.Contraseña ?? "");
                await usuario.Update<Usuarios>();
            }

            if (!contraseñaValida)
            {
                ViewBag.Error = "Credenciales incorrectas";
                return View(model);
            }

            // VALIDAR SI ESTÁ VERIFICADO (Para cuentas en la BD de datos sin verificar)
            if (!usuario.Verificado)
            {
                ViewBag.Error = "Debes verificar tu correo electrónico para poder iniciar sesión. Si no recibiste el correo, puedes ";
                ViewBag.ReenviarCorreo = usuario.Correo;
                return View(model);
            }

            // GUARDAR SESIÓN
            HttpContext.Session.SetString(
                "Usuario",
                usuario.Correo ?? "");

            HttpContext.Session.SetString(
                "Apodo",
                usuario.Apodo ?? "");

            HttpContext.Session.SetString(
                "Rol",
                usuario.Rol ?? "");

            HttpContext.Session.SetInt32(
                "IdUsuario",
                (int)(usuario.IdUsuario ?? 0));

            HttpContext.Session.SetString(
                "FotoPerfil",
                usuario.FotoPerfil ?? "");

            // RECORDARME (SESIÓN PERSISTENTE)
            if (model.Recordarme)
            {
                var encryptedEmail = _protector.Protect(usuario.Correo ?? "");
                var cookieOptions = new CookieOptions
                {
                    Expires = DateTimeOffset.UtcNow.AddDays(30),
                    HttpOnly = true,
                    Secure = Request.IsHttps,
                    IsEssential = true,
                    Path = "/" // Asegura que esté disponible en todo el sitio
                };
                Response.Cookies.Append("Subaston_RememberMe", encryptedEmail, cookieOptions);
            }

            // REDIRECCIONES POR ROL
            if (usuario.Rol == "Admin")
            {
                return RedirectToAction(
                    "Index",
                    "Admin",
                    new { area = "Admin" });
            }

            if (usuario.Rol == "Vendedor")
            {
                return RedirectToAction(
                    "Index",
                    "Vendedor",
                    new { area = "Vendedor" });
            }

            if (usuario.Rol == "Comprador")
            {
                return RedirectToAction(
                    "Index",
                    "Comprador",
                    new { area = "Comprador" });
            }

            return RedirectToAction("Login");
        }

        // LOGOUT
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();

            // Eliminar cookie de Recordarme
            var cookieOptions = new CookieOptions { Path = "/" };
            Response.Cookies.Delete("Subaston_RememberMe", cookieOptions);

            return RedirectToAction(
                "Login",
                "Cuenta",
                new { area = "Cuentas" });
        }

        // CONFIRMAR REGISTRO GET
        public async Task<IActionResult> ConfirmarRegistro(string datos)
        {
            if (string.IsNullOrEmpty(datos))
            {
                TempData["Error"] = "El enlace de confirmación es inválido.";
                return RedirectToAction("Login");
            }

            try
            {
                string json = _regProtector.Unprotect(datos);
                var tempUser = JsonSerializer.Deserialize<TempRegistroDto>(json);

                if (tempUser == null)
                {
                    TempData["Error"] = "Los datos del enlace están corruptos.";
                    return RedirectToAction("Login");
                }

                // Verificar si se registró en la BD mientras tanto
                var existe = await _supabase
                    .From<Usuarios>()
                    .Where(x => x.Correo == tempUser.Correo)
                    .Get();

                if (existe.Models.Count > 0)
                {
                    TempData["Error"] = "Este correo electrónico ya ha sido registrado.";
                    return RedirectToAction("Login");
                }

                ViewBag.Nombre = tempUser.Nombre;
                ViewBag.Datos = datos;

                return View();
            }
            catch (Exception)
            {
                TempData["Error"] = "El enlace de registro ha expirado o es inválido.";
                return RedirectToAction("Login");
            }
        }

        // CONFIRMAR REGISTRO POST
        [HttpPost]
        public async Task<IActionResult> ConfirmarRegistroPost(string datos)
        {
            if (string.IsNullOrEmpty(datos))
            {
                TempData["Error"] = "El enlace es inválido.";
                return RedirectToAction("Login");
            }

            try
            {
                string json = _regProtector.Unprotect(datos);
                var tempUser = JsonSerializer.Deserialize<TempRegistroDto>(json);

                if (tempUser == null)
                {
                    TempData["Error"] = "Datos corruptos en el enlace.";
                    return RedirectToAction("Login");
                }

                // Doble validación de duplicados
                var existe = await _supabase
                    .From<Usuarios>()
                    .Where(x => x.Correo == tempUser.Correo)
                    .Get();

                if (existe.Models.Count > 0)
                {
                    TempData["Error"] = "Este correo electrónico ya se encuentra registrado.";
                    return RedirectToAction("Login");
                }

                // Crear usuario e insertar en la BD final
                var nuevoUsuario = new Usuarios
                {
                    Nombre = tempUser.Nombre,
                    Apodo = tempUser.Apodo,
                    Correo = tempUser.Correo,
                    Contraseña = tempUser.PasswordHash,
                    FechaRegistro = DateTime.Now,
                    Rol = tempUser.Rol,
                    Verificado = true, // Ya verificado
                    TokenVerificacion = null
                };

                await _supabase.From<Usuarios>().Insert(nuevoUsuario);

                TempData["Correcto"] = "¡Tu cuenta ha sido creada y verificada con éxito! Ya puedes iniciar sesión.";
                return RedirectToAction("Login");
            }
            catch (Exception)
            {
                TempData["Error"] = "El enlace de registro ha expirado o es inválido.";
                return RedirectToAction("Login");
            }
        }

        // CONFIRMAR CORREO GET (Para usuarios antiguos en la BD)
        public async Task<IActionResult> ConfirmarCorreo(string correo, string token)
        {
            if (string.IsNullOrEmpty(correo) || string.IsNullOrEmpty(token))
            {
                TempData["Error"] = "Parámetros de confirmación inválidos.";
                return RedirectToAction("Login");
            }

            // BUSCAR USUARIO
            var response = await _supabase
                .From<Usuarios>()
                .Where(x => x.Correo == correo)
                .Get();

            var usuario = response.Models.FirstOrDefault();

            if (usuario == null)
            {
                TempData["Error"] = "El usuario no existe.";
                return RedirectToAction("Login");
            }

            if (usuario.Verificado)
            {
                TempData["Correcto"] = "Tu correo ya ha sido verificado. Puedes iniciar sesión.";
                return RedirectToAction("Login");
            }

            if (usuario.TokenVerificacion != token)
            {
                TempData["Error"] = "El código o enlace de verificación no es válido o ha expirado.";
                return RedirectToAction("Login");
            }

            ViewBag.Correo = correo;
            ViewBag.Token = token;

            return View();
        }

        // CONFIRMAR CORREO POST (Para usuarios antiguos en la BD)
        [HttpPost]
        public async Task<IActionResult> ConfirmarCorreoPost(string correo, string token)
        {
            if (string.IsNullOrEmpty(correo) || string.IsNullOrEmpty(token))
            {
                TempData["Error"] = "Parámetros de confirmación inválidos.";
                return RedirectToAction("Login");
            }

            var response = await _supabase
                .From<Usuarios>()
                .Where(x => x.Correo == correo)
                .Get();

            var usuario = response.Models.FirstOrDefault();

            if (usuario == null)
            {
                TempData["Error"] = "El usuario no existe.";
                return RedirectToAction("Login");
            }

            if (usuario.Verificado)
            {
                TempData["Correcto"] = "Tu correo ya ha sido verificado. Puedes iniciar sesión.";
                return RedirectToAction("Login");
            }

            if (usuario.TokenVerificacion == token)
            {
                usuario.Verificado = true;
                usuario.TokenVerificacion = null; // Limpiar token una vez verificado

                await usuario.Update<Usuarios>();

                TempData["Correcto"] = "¡Correo verificado con éxito! Ya puedes iniciar sesión.";
            }
            else
            {
                TempData["Error"] = "El código o enlace de verificación no es válido o ha expirado.";
            }

            return RedirectToAction("Login");
        }

        // REENVIAR VERIFICACIÓN GET (Para usuarios ya en la BD que no están verificados)
        public async Task<IActionResult> ReenviarVerificacion(string correo)
        {
            if (string.IsNullOrEmpty(correo))
            {
                TempData["Error"] = "El correo es inválido.";
                return RedirectToAction("Login");
            }

            var response = await _supabase
                .From<Usuarios>()
                .Where(x => x.Correo == correo)
                .Get();

            var usuario = response.Models.FirstOrDefault();

            if (usuario == null)
            {
                TempData["Error"] = "El usuario no existe.";
                return RedirectToAction("Login");
            }

            if (usuario.Verificado)
            {
                TempData["Correcto"] = "Tu correo ya está verificado. Puedes iniciar sesión.";
                return RedirectToAction("Login");
            }

            // Generar o regenerar token
            if (string.IsNullOrEmpty(usuario.TokenVerificacion))
            {
                usuario.TokenVerificacion = Guid.NewGuid().ToString();
                await usuario.Update<Usuarios>();
            }

            // Reenviar correo
            try
            {
                var callbackUrl = $"{Request.Scheme}://{Request.Host}/Cuentas/Cuenta/ConfirmarCorreo?correo={Uri.EscapeDataString(usuario.Correo ?? "")}&token={usuario.TokenVerificacion}";

                var body = $@"
                    <div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto; padding: 20px; border: 1px solid #eee; border-radius: 5px; background-color: #fdfdfd;'>
                        <div style='text-align: center; margin-bottom: 20px;'>
                            <h2 style='color: #111; margin: 0;'>SUBASTON</h2>
                            <p style='color: #666; font-size: 14px; margin: 5px 0 0 0;'>Tus coleccionables te esperan</p>
                        </div>
                        <hr style='border: none; border-top: 1px solid #eee; margin-bottom: 20px;' />
                        <h3 style='color: #333;'>Verificación de tu cuenta</h3>
                        <p>Hemos recibido una solicitud para reenviar el correo de verificación de tu cuenta en Subaston. Para poder iniciar sesión y participar en las subastas, por favor confirma tu dirección de correo electrónico haciendo clic en el botón de abajo:</p>
                        <div style='text-align: center; margin: 30px 0;'>
                            <a href='{callbackUrl}' style='background-color: #28a745; color: white; padding: 12px 24px; text-decoration: none; border-radius: 4px; font-weight: bold; display: inline-block;'>Confirmar Correo Electrónico</a>
                        </div>
                        <p>Si el botón no funciona, puedes copiar y pegar el siguiente enlace en tu navegador:</p>
                        <p style='word-break: break-all; color: #666;'><a href='{callbackUrl}'>{callbackUrl}</a></p>
                        <hr style='border: none; border-top: 1px solid #eee; margin: 20px 0;' />
                        <p style='font-size: 11px; color: #999; text-align: center;'>Este es un correo automático generado por Subaston. Por favor no respondas a este mensaje.</p>
                    </div>";

                await _emailService.SendEmailAsync(usuario.Correo ?? "", "Reenvío de verificación - Subaston", body);

                TempData["Correcto"] = "Correo de verificación reenviado. Revisa tu bandeja de entrada.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Error al enviar el correo: " + ex.Message;
            }

            return RedirectToAction("Login");
        }

        // VERIFICACION GET (Olvido de contraseña)
        public IActionResult Verificacion()
        {
            var usuarioLogueado = HttpContext.Session.GetString("Usuario");
            if (!string.IsNullOrEmpty(usuarioLogueado))
            {
                // Si el usuario ya está logueado, redirige directo a CambioContra
                return RedirectToAction("CambioContra", new { correo = usuarioLogueado });
            }
            return View();
        }

        // VERIFICACION POST (Olvido de contraseña: envía correo de recuperación)
        [HttpPost]
        public async Task<IActionResult> Verificacion(
            VerificacionViewsModels model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // BUSCAR USUARIO
            var response = await _supabase
                .From<Usuarios>()
                .Where(x => x.Correo == model.Correo)
                .Get();

            var usuario = response.Models.FirstOrDefault();

            if (usuario == null)
            {
                ModelState.AddModelError(
                    "",
                    "El correo no está registrado");

                return View(model);
            }

            // Generar token para cambio de contraseña
            var token = Guid.NewGuid().ToString();
            usuario.TokenVerificacion = token;
            await usuario.Update<Usuarios>();

            // Enviar correo de restablecimiento
            try
            {
                var callbackUrl = $"{Request.Scheme}://{Request.Host}/Cuentas/Cuenta/RestablecerContrasena?correo={Uri.EscapeDataString(usuario.Correo ?? "")}&token={token}";

                var body = $@"
                    <div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto; padding: 20px; border: 1px solid #eee; border-radius: 5px; background-color: #fdfdfd;'>
                        <div style='text-align: center; margin-bottom: 20px;'>
                            <h2 style='color: #111; margin: 0;'>SUBASTON</h2>
                            <p style='color: #666; font-size: 14px; margin: 5px 0 0 0;'>Tus coleccionables te esperan</p>
                        </div>
                        <hr style='border: none; border-top: 1px solid #eee; margin-bottom: 20px;' />
                        <h3 style='color: #333;'>Restablecimiento de Contraseña</h3>
                        <p>Hemos recibido una solicitud para cambiar la contraseña de tu cuenta en Subaston. Para poder establecer una nueva contraseña, por favor haz clic en el botón de abajo:</p>
                        <div style='text-align: center; margin: 30px 0;'>
                            <a href='{callbackUrl}' style='background-color: #00B8D9; color: #000; padding: 12px 24px; text-decoration: none; border-radius: 4px; font-weight: bold; display: inline-block;'>Restablecer Contraseña</a>
                        </div>
                        <p>Si el botón no funciona, puedes copiar y pegar el siguiente enlace en tu navegador:</p>
                        <p style='word-break: break-all; color: #666;'><a href='{callbackUrl}'>{callbackUrl}</a></p>
                        <hr style='border: none; border-top: 1px solid #eee; margin: 20px 0;' />
                        <p style='font-size: 11px; color: #999; text-align: center;'>Este es un correo automático generado por Subaston. Si no solicitaste este cambio, puedes ignorar este mensaje.</p>
                    </div>";

                await _emailService.SendEmailAsync(usuario.Correo ?? "", "Restablece tu contraseña - Subaston", body);

                TempData["Correcto"] = "Te hemos enviado un correo para restablecer tu contraseña. Revisa tu bandeja de entrada.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Error al enviar el correo de recuperación: " + ex.Message;
            }

            return RedirectToAction("Login");
        }

        // RESTABLECER CONTRASEÑA GET (Punto de entrada desde el correo para usuarios deslogueados)
        public async Task<IActionResult> RestablecerContrasena(string correo, string token)
        {
            if (string.IsNullOrEmpty(correo) || string.IsNullOrEmpty(token))
            {
                TempData["Error"] = "Parámetros de restablecimiento inválidos.";
                return RedirectToAction("Login");
            }

            var response = await _supabase
                .From<Usuarios>()
                .Where(x => x.Correo == correo)
                .Get();

            var usuario = response.Models.FirstOrDefault();

            if (usuario == null || usuario.TokenVerificacion != token)
            {
                TempData["Error"] = "El enlace de restablecimiento es inválido o ha expirado.";
                return RedirectToAction("Login");
            }

            var model = new CambioContraViewsModels
            {
                Correo = correo,
                Token = token
            };

            return View("CambioContra", model);
        }

        // CAMBIO CONTRA GET (Acceso directo para usuarios logueados)
        public IActionResult CambioContra(string correo)
        {
            var usuarioLogueado = HttpContext.Session.GetString("Usuario");

            if (!string.IsNullOrEmpty(usuarioLogueado))
            {
                // Si está logueado, debe cambiar su propio correo
                if (usuarioLogueado != correo)
                {
                    return RedirectToAction("Index", "Home", new { area = "" });
                }

                return View(new CambioContraViewsModels
                {
                    Correo = correo
                });
            }

            // Si no está logueado, no puede entrar aquí directamente sin el token
            TempData["Error"] = "Acceso no autorizado.";
            return RedirectToAction("Login");
        }

        // CAMBIO CONTRA POST (Procesa el cambio para logueados y no logueados)
        [HttpPost]
        public async Task<IActionResult> CambioContra(
            CambioContraViewsModels model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // BUSCAR USUARIO
            var response = await _supabase
                .From<Usuarios>()
                .Where(x => x.Correo == model.Correo)
                .Get();

            var usuario = response.Models.FirstOrDefault();

            if (usuario == null)
            {
                ModelState.AddModelError(
                    "",
                    "El correo no está registrado");

                return View(model);
            }

            var usuarioLogueado = HttpContext.Session.GetString("Usuario");

            if (!string.IsNullOrEmpty(usuarioLogueado))
            {
                // CASO 1: USUARIO LOGUEADO
                if (usuarioLogueado != model.Correo)
                {
                    TempData["Error"] = "Acceso no autorizado.";
                    return RedirectToAction("Login");
                }

                // Cambiar contraseña
                usuario.Contraseña = _passwordHasher.HashPassword(usuario, model.NuevaContraseña ?? "");
                await usuario.Update<Usuarios>();

                TempData["Correcto"] = "Contraseña actualizada correctamente.";

                // Redirigir según el rol del usuario que sigue logueado
                var rol = HttpContext.Session.GetString("Rol");
                if (rol == "Admin")
                {
                    return RedirectToAction("Index", "Admin", new { area = "Admin" });
                }
                if (rol == "Vendedor")
                {
                    return RedirectToAction("Index", "Vendedor", new { area = "Vendedor" });
                }
                if (rol == "Comprador")
                {
                    return RedirectToAction("Index", "Comprador", new { area = "Comprador" });
                }

                return RedirectToAction("Index", "Home", new { area = "" });
            }
            else
            {
                // CASO 2: USUARIO NO LOGUEADO (Restablecimiento por correo)
                if (string.IsNullOrEmpty(model.Token) || usuario.TokenVerificacion != model.Token)
                {
                    TempData["Error"] = "El enlace de restablecimiento ha expirado o es inválido.";
                    return RedirectToAction("Login");
                }

                // Cambiar contraseña y limpiar token de verificación usado
                usuario.Contraseña = _passwordHasher.HashPassword(usuario, model.NuevaContraseña ?? "");
                usuario.TokenVerificacion = null;
                await usuario.Update<Usuarios>();

                TempData["Correcto"] = "Contraseña restablecida correctamente. Ya puedes iniciar sesión.";
                return RedirectToAction("Login");
            }
        }
    }

    // DTO TEMPORAL PARA REGISTRO SIN GUARDAR EN LA BD ANTES DE CONFIRMAR
    public class TempRegistroDto
    {
        public string Nombre { get; set; } = "";
        public string Apodo { get; set; } = "";
        public string Correo { get; set; } = "";
        public string PasswordHash { get; set; } = "";
        public string Rol { get; set; } = "";
    }
}
