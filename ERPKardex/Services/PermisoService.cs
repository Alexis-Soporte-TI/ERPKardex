using ERPKardex.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace ERPKardex.Services
{
    public interface IPermisoService
    {
        Task<bool> TienePermiso(string codigoPermiso);
        void LimpiarCacheUsuario(int empresaUsuarioId);
    }

    public class PermisoService : IPermisoService
    {
        private readonly ApplicationDbContext _context;
        private readonly IHttpContextAccessor _httpContext;
        private readonly IMemoryCache _cache;

        public PermisoService(ApplicationDbContext context, IHttpContextAccessor httpContext, IMemoryCache cache)
        {
            _context = context;
            _httpContext = httpContext;
            _cache = cache;
        }

        public async Task<bool> TienePermiso(string codigoPermiso)
        {
            // Aseguramos que el HttpContext no sea nulo
            var context = _httpContext.HttpContext;
            if (context == null || !context.User.Identity.IsAuthenticated) return false;

            var user = context.User;

            // 1. BYPASS ADMINISTRADOR (Comparación insensible a mayúsculas)
            var adminClaim = user.FindFirst("EsAdministrador");
            if (adminClaim != null && adminClaim.Value.Equals("true", StringComparison.OrdinalIgnoreCase))
                return true;

            // 2. OBTENER ID VÍNCULO (Usamos el valor que confirmamos en el DEBUG)
            var claimVinculo = user.FindFirst("EmpresaUsuarioId")?.Value;
            if (string.IsNullOrEmpty(claimVinculo) || !int.TryParse(claimVinculo, out int idVinculo))
                return false;

            // 3. CACHÉ
            string cacheKey = $"PERMISOS_EU_{idVinculo}";

            if (!_cache.TryGetValue(cacheKey, out List<string> misPermisos))
            {
                // 4. CONSULTA BD (Normalizamos a Mayúsculas y quitamos espacios desde la BD)
                misPermisos = await (from eup in _context.EmpresaUsuarioPermisos
                                     join p in _context.Permisos on eup.PermisoId equals p.Id
                                     where eup.EmpresaUsuarioId == idVinculo
                                        && p.Estado == true
                                     select p.Codigo.Trim().ToUpper()).ToListAsync();

                // Si la lista está vacía, guardamos una lista vacía para no re-consultar la BD 
                // pero podrías poner un log aquí si fuera necesario.
                var ops = new MemoryCacheEntryOptions().SetAbsoluteExpiration(TimeSpan.FromMinutes(20));
                _cache.Set(cacheKey, misPermisos, ops);
            }

            // 5. COMPARACIÓN FINAL (Siempre en Mayúsculas)
            return misPermisos.Contains(codigoPermiso.Trim().ToUpper());
        }

        public void LimpiarCacheUsuario(int empresaUsuarioId)
        {
            _cache.Remove($"PERMISOS_EU_{empresaUsuarioId}");
        }
    }
}