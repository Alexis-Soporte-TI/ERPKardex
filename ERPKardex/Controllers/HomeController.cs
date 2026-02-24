using ERPKardex.Data;
using ERPKardex.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using System.Security.Claims;

namespace ERPKardex.Controllers
{
    public class HomeController : BaseController
    {
        private readonly ApplicationDbContext _context;
        public HomeController(ApplicationDbContext context)
        {
            _context = context;
        }
        [Authorize]
        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
        [AllowAnonymous]
        public IActionResult Login()
        {
            return View();
        }
        [AllowAnonymous]
        [HttpPost]
        public async Task<JsonResult> IniciarSesion(string ruc, string dni, string password)
        {
            try
            {
                // 1. Buscamos la combinación de Empresa + Usuario + Relación (empresa_usuario)
                // Hacemos el join manual ya que no usamos FKs estrictas en el modelo
                var datosUsuario = await (from e in _context.Empresas
                                          join eu in _context.EmpresaUsuarios on e.Id equals eu.EmpresaId
                                          join u in _context.Usuarios on eu.UsuarioId equals u.Id
                                          join tu in _context.TipoUsuarios on eu.TipoUsuarioId equals tu.Id
                                          where e.Ruc == ruc
                                          where u.Dni == dni
                                          where u.Password == password
                                          where u.Estado == true && e.Estado == true && eu.Estado == true
                                          select new
                                          {
                                              UsuarioId = u.Id,
                                              u.Nombre,
                                              u.Dni,
                                              EmpresaId = e.Id, // Este es el EmpresaId real
                                              RazonSocial = e.RazonSocial,
                                              eu.TipoUsuarioId, // El rol por si lo necesitas luego
                                              EmpresaUsuarioId = eu.Id,
                                              EsAdmin = tu.EsAdministrador,
                                          }).FirstOrDefaultAsync();

                if (datosUsuario == null)
                {
                    return Json(new ApiResponse { data = null, message = "Credenciales incorrectas o el usuario no pertenece a esta empresa.", status = false });
                }
                else
                {
                    // 2. Cargamos los Claims con la información de la empresa seleccionada
                    var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, datosUsuario.Nombre),
                new Claim(ClaimTypes.NameIdentifier, datosUsuario.UsuarioId.ToString()),
                new Claim("EmpresaId", datosUsuario.EmpresaId.ToString()), // EmpresaId recuperado del RUC
                new Claim("EmpresaUsuarioId", datosUsuario.EmpresaUsuarioId.ToString()), // GUARDAMOS ESTO
                new Claim("RazonSocial", datosUsuario.RazonSocial),
                new Claim("DNI", datosUsuario.Dni),
                new Claim("EsAdministrador", datosUsuario.EsAdmin.GetValueOrDefault() ? "true" : "false"), // LO GUARDAMOS COMO STRING
                new Claim(ClaimTypes.Role, datosUsuario.TipoUsuarioId.GetValueOrDefault().ToString())
            };

                    var claimsIdentity = new ClaimsIdentity(
                        claims, CookieAuthenticationDefaults.AuthenticationScheme);

                    var authProperties = new AuthenticationProperties
                    {
                        IsPersistent = true,
                        ExpiresUtc = DateTimeOffset.UtcNow.AddMinutes(60)
                    };

                    await HttpContext.SignInAsync(
                        CookieAuthenticationDefaults.AuthenticationScheme,
                        new ClaimsPrincipal(claimsIdentity),
                        authProperties);

                    return Json(new ApiResponse { data = null, message = "Bienvenido a " + datosUsuario.RazonSocial, status = true });
                }
            }
            catch (Exception ex)
            {
                return Json(new ApiResponse { data = null, message = ex.Message, status = false });
            }
        }
        [AllowAnonymous]
        [HttpPost]
        public async Task<IActionResult> Logout()
        {
            // 1. Eliminar la Cookie de Autenticación
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

            // 2. Eliminar los datos de la Sesión Estándar (Lista de Empresas, etc.)
            HttpContext.Session.Clear();

            // 3. Redirigir al usuario a la página de Login
            return RedirectToAction("Index", "Home");
        }
        [Authorize]
        [HttpGet]
        public JsonResult GetPeriodosContables()
        {
            try
            {
                var periodos = _context.PeriodosContables
                    .Where(p => p.Estado == true && p.EmpresaId == EmpresaUsuarioId)
                    .Select(p => new { p.Id, p.Anio, p.Mes })
                    .ToList();
                return Json(new ApiResponse { data = periodos, message = "Periodos obtenidos correctamente.", status = true });
            }
            catch (Exception ex)
            {
                return Json(new ApiResponse { data = null, message = ex.Message, status = false });
            }
        }
    }
}
