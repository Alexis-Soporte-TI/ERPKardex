using ERPKardex.Data;
using ERPKardex.Models;
using ERPKardex.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ERPKardex.Controllers
{
    // [Authorize] <-- Descomenta cuando termines de probar
    public class SeguridadController : BaseController
    {
        private readonly ApplicationDbContext _context;
        private readonly IPermisoService _permisoService;
        public SeguridadController(ApplicationDbContext context, IPermisoService permisoService)
        {
            _context = context;
            _permisoService = permisoService;
        }

        public IActionResult Usuarios()
        {
            return View();
        }

        // 1. LISTADO (Se mantiene igual: muestra VÍNCULOS)
        [HttpGet]
        public async Task<JsonResult> GetUsuariosEmpresa()
        {
            try
            {
                var miEmpresaId = EmpresaUsuarioId;
                var esAdmin = EsAdminGlobal;

                var query = from eu in _context.EmpresaUsuarios
                            join u in _context.Usuarios on eu.UsuarioId equals u.Id
                            join tu in _context.TipoUsuarios on eu.TipoUsuarioId equals tu.Id
                            join e in _context.Empresas on eu.EmpresaId equals e.Id
                            where eu.Estado == true
                            select new
                            {
                                IdVinculo = eu.Id, // ID de la relación
                                u.Dni,
                                u.Nombre,
                                u.Cargo,
                                u.Email,
                                u.Telefono,
                                Rol = tu.Nombre,
                                tu.EsAdministrador,
                                EmpresaId = e.Id,
                                Empresa = e.RazonSocial
                            };

                // Si no es admin global, solo ve los de su empresa actual
                if (!esAdmin) query = query.Where(x => x.EmpresaId == miEmpresaId);

                var lista = await query.OrderBy(x => x.Empresa).ThenBy(x => x.Nombre).ToListAsync();
                return Json(new { status = true, data = lista });
            }
            catch (Exception ex) { return Json(new { status = false, message = ex.Message }); }
        }

        // 2. NUEVO MÉTODO: BUSCAR GLOBALMENTE
        [HttpGet]
        public async Task<JsonResult> BuscarDniGlobal(string dni)
        {
            var user = await _context.Usuarios.FirstOrDefaultAsync(u => u.Dni == dni);
            if (user != null)
            {
                return Json(new
                {
                    status = true,
                    encontrado = true,
                    data = new { user.Nombre, user.Cargo, user.Email, user.Telefono }
                });
            }
            return Json(new { status = true, encontrado = false });
        }

        // 3. GUARDAR (Lógica Multi-Empresa)
        [HttpPost]
        public async Task<JsonResult> GuardarUsuario(
            string dni, string nombre, string cargo, string email, string telefono,
            string password, int tipoUsuarioId, int? empresaIdDestino)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var esAdmin = EsAdminGlobal;
                var miEmpresaId = EmpresaUsuarioId;

                // Definir empresa destino: Si mandan una, úsala; si no, usa la actual.
                int targetEmpresaId = (esAdmin && empresaIdDestino.HasValue) ? empresaIdDestino.Value : miEmpresaId;

                int usuarioIdGlobal = 0;

                // PASO A: GESTIÓN DEL USUARIO GLOBAL
                var usuarioExistente = await _context.Usuarios.FirstOrDefaultAsync(u => u.Dni == dni);

                if (usuarioExistente != null)
                {
                    // EXISTE: Actualizamos datos básicos (Nombre, Cargo, etc.)
                    // Esto actualiza la "ficha maestra" de la persona.
                    usuarioExistente.Nombre = nombre;
                    usuarioExistente.Cargo = cargo;
                    usuarioExistente.Email = email;
                    usuarioExistente.Telefono = telefono;
                    usuarioExistente.Estado = true; // En caso de que estuviera dado de baja globalmente, lo reactivamos

                    // Solo actualizamos password si escribieron algo
                    if (!string.IsNullOrEmpty(password)) usuarioExistente.Password = password;

                    _context.Usuarios.Update(usuarioExistente);
                    usuarioIdGlobal = usuarioExistente.Id;
                }
                else
                {
                    // NO EXISTE: Lo creamos
                    if (string.IsNullOrEmpty(password))
                        return Json(new { status = false, message = "La contraseña es obligatoria para usuarios nuevos." });

                    var nuevoUser = new Usuario
                    {
                        Dni = dni,
                        Nombre = nombre,
                        Cargo = cargo,
                        Email = email,
                        Telefono = telefono,
                        Password = password,
                        Estado = true,
                        FechaRegistro = DateTime.Now
                    };
                    _context.Usuarios.Add(nuevoUser);
                    await _context.SaveChangesAsync();
                    usuarioIdGlobal = nuevoUser.Id;
                }

                // PASO B: GESTIÓN DEL VÍNCULO (EMPRESA_USUARIO)
                var vinculo = await _context.EmpresaUsuarios
                    .FirstOrDefaultAsync(x => x.EmpresaId == targetEmpresaId && x.UsuarioId == usuarioIdGlobal);

                if (vinculo != null)
                {
                    // YA EXISTE EN ESTA EMPRESA
                    if (!vinculo.Estado.GetValueOrDefault())
                    {
                        // Estaba eliminado, lo reactivamos
                        vinculo.Estado = true;
                        vinculo.TipoUsuarioId = tipoUsuarioId;
                        _context.EmpresaUsuarios.Update(vinculo);
                    }
                    else
                    {
                        // Ya estaba activo, solo actualizamos el rol
                        vinculo.TipoUsuarioId = tipoUsuarioId;
                        _context.EmpresaUsuarios.Update(vinculo);
                    }
                }
                else
                {
                    // NO EXISTE EN ESTA EMPRESA -> CREAMOS LA RELACIÓN (ASIGNACIÓN)
                    _context.EmpresaUsuarios.Add(new EmpresaUsuario
                    {
                        EmpresaId = targetEmpresaId,
                        UsuarioId = usuarioIdGlobal,
                        TipoUsuarioId = tipoUsuarioId,
                        Estado = true
                    });
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
                return Json(new { status = true, message = "Usuario asignado/actualizado correctamente." });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return Json(new { status = false, message = ex.Message });
            }
        }

        // =====================================================================
        // 2. GESTIÓN DE PERMISOS (EL ÁRBOL)
        // =====================================================================

        [HttpGet]
        public async Task<JsonResult> GetArbolPermisos(int idVinculo)
        {
            try
            {
                // 1. Traemos TODOS los permisos del sistema
                var todosLosPermisos = await _context.Permisos
                    .Where(p => p.Estado == true)
                    .OrderBy(p => p.Orden)
                    .Select(p => new
                    {
                        p.Id,
                        p.Codigo,
                        p.Descripcion,
                        p.PadreId
                    }).ToListAsync();

                // 2. Traemos LOS QUE TIENE ASIGNADOS este usuario (Join explícito)
                var permisosAsignados = await (from eup in _context.EmpresaUsuarioPermisos
                                               where eup.EmpresaUsuarioId == idVinculo
                                               select eup.PermisoId).ToListAsync();

                // 3. Armamos una estructura plana con un flag "Asignado"
                // El JavaScript se encargará de pintarlo como árbol visualmente
                var data = todosLosPermisos.Select(p => new
                {
                    p.Id,
                    p.Codigo,
                    p.Descripcion,
                    p.PadreId,
                    Activo = permisosAsignados.Contains(p.Id) // Checkbox True/False
                }).ToList();

                return Json(new { status = true, data });
            }
            catch (Exception ex) { return Json(new { status = false, message = ex.Message }); }
        }

        [HttpPost]
        public async Task<JsonResult> TogglePermiso(int idVinculo, int idPermiso, bool activar)
        {
            try
            {
                var relacion = await _context.EmpresaUsuarioPermisos
                    .FirstOrDefaultAsync(x => x.EmpresaUsuarioId == idVinculo && x.PermisoId == idPermiso);

                if (activar)
                {
                    if (relacion == null)
                    {
                        _context.EmpresaUsuarioPermisos.Add(new EmpresaUsuarioPermiso
                        {
                            EmpresaUsuarioId = idVinculo,
                            PermisoId = idPermiso
                        });
                    }
                }
                else
                {
                    if (relacion != null)
                    {
                        _context.EmpresaUsuarioPermisos.Remove(relacion);
                    }
                }

                await _context.SaveChangesAsync();

                // --- SOLUCIÓN AQUÍ ---
                // Al limpiar el caché, la próxima vez que el usuario cargue el menú, 
                // el servicio se verá obligado a consultar la BD y traerá los cambios.
                _permisoService.LimpiarCacheUsuario(idVinculo);
                // ---------------------

                return Json(new { status = true });
            }
            catch (Exception ex) { return Json(new { status = false, message = ex.Message }); }
        }

        [HttpGet]
        public async Task<JsonResult> GetTiposUsuario()
        {
            var roles = await _context.TipoUsuarios.Where(x => x.Estado == true).ToListAsync();
            return Json(new { status = true, data = roles });
        }
        // =====================================================================
        // 3. ELIMINACIÓN Y DAR DE BAJA
        // =====================================================================

        [HttpPost]
        public async Task<JsonResult> RemoverDeEmpresa(int idVinculo)
        {
            try
            {
                var vinculo = await _context.EmpresaUsuarios.FindAsync(idVinculo);
                if (vinculo == null) return Json(new { status = false, message = "Vínculo no encontrado." });

                // Soft delete: Desactivar solo el vínculo con esta empresa
                vinculo.Estado = false;
                _context.EmpresaUsuarios.Update(vinculo);
                await _context.SaveChangesAsync();

                // Limpiamos la caché del menú para que se le cierre el acceso
                _permisoService.LimpiarCacheUsuario(idVinculo);

                return Json(new { status = true, message = "Usuario removido de la empresa exitosamente." });
            }
            catch (Exception ex) { return Json(new { status = false, message = ex.Message }); }
        }

        [HttpPost]
        public async Task<JsonResult> DesactivarUsuarioGlobal(string dni)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var usuario = await _context.Usuarios.FirstOrDefaultAsync(u => u.Dni == dni);
                if (usuario == null) return Json(new { status = false, message = "Usuario no encontrado." });

                // 1. Desactivamos al usuario maestro (Estado = false / 0)
                usuario.Estado = false;
                _context.Usuarios.Update(usuario);

                // 2. Por seguridad, también desactivamos TODOS sus vínculos en cualquier empresa
                var vinculos = await _context.EmpresaUsuarios.Where(v => v.UsuarioId == usuario.Id).ToListAsync();
                foreach (var v in vinculos)
                {
                    v.Estado = false;
                    _context.EmpresaUsuarios.Update(v);
                    _permisoService.LimpiarCacheUsuario(v.Id);
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return Json(new { status = true, message = "El usuario fue dado de baja del sistema globalmente." });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return Json(new { status = false, message = ex.Message });
            }
        }
    }
}