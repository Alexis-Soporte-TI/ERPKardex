using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace ERPKardex.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly ILogger<AuthController> _logger;

        public AuthController(ILogger<AuthController> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Endpoint para refrescar la sesión del usuario (extiende la duración de la cookie de autenticación)
        /// </summary>
        [HttpPost("refresh-session")]
        public IActionResult RefreshSession()
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized(new { status = false, message = "Usuario no autenticado" });
                }

                _logger.LogInformation($"Session refreshed for user {userId} at {DateTime.Now}");

                // La cookie se refresca automáticamente en ASP.NET Core cuando se accede a User.Identity
                // Solo necesitamos retornar OK para confirmar que el usuario aún está autenticado
                return Ok(new { status = true, message = "Sesión refrescada correctamente", timestamp = DateTime.UtcNow });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al refrescar la sesión");
                return StatusCode(500, new { status = false, message = "Error al refrescar la sesión" });
            }
        }

        /// <summary>
        /// Endpoint para verificar si la sesión aún es válida
        /// </summary>
        [HttpPost("verify-session")]
        public IActionResult VerifySession()
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized(new { status = false, message = "Sesión expirada" });
                }

                return Ok(new { status = true, message = "Sesión válida", timestamp = DateTime.UtcNow });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al verificar la sesión");
                return StatusCode(500, new { status = false, message = "Error al verificar la sesión" });
            }
        }
    }
}
