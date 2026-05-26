using ERPKardex.Data;
using ERPKardex.Models;
using ERPKardex.Services;
using ERPKardex.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ERPKardex.Controllers
{
    // [Authorize] <-- Descomenta cuando termines de probar
    public class SeguridadController : BaseController
    {
        private readonly ApplicationDbContext _context;
        private readonly IPermisoService _permisoService;
        private readonly IEmailService _emailService;
        public SeguridadController(ApplicationDbContext context, IPermisoService permisoService, IEmailService emailService)
        {
            _context = context;
            _permisoService = permisoService;
            _emailService = emailService;
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

        #region ALERTAS DE VENCIMIENTO

        public IActionResult AlertasVencimiento()
        {
            if (!EsAdminGlobal) return RedirectToAction("AccesoDenegado", "Home");
            return View();
        }

        [HttpGet]
        public async Task<JsonResult> GetConfigAlertas()
        {
            try
            {
                var empresas = await _context.Empresas.Where(e => e.Estado == true).OrderBy(e => e.RazonSocial).ToListAsync();
                var configs = await _context.ConfigAlertasVencimiento.ToListAsync();
                var data = empresas.Select(e =>
                {
                    var cfg = configs.FirstOrDefault(c => c.EmpresaId == e.Id);
                    return new
                    {
                        EmpresaId = e.Id,
                        Empresa = e.RazonSocial,
                        MesesCritico = cfg?.MesesCritico ?? 18,
                        MesesAlerta = cfg?.MesesAlerta ?? 24,
                        ConfigId = cfg?.Id
                    };
                });
                return Json(new { status = true, data });
            }
            catch (Exception ex) { return Json(new { status = false, message = ex.Message }); }
        }

        [HttpPost]
        public async Task<JsonResult> GuardarConfigAlerta(int empresaId, int mesesCritico, int mesesAlerta)
        {
            try
            {
                if (mesesCritico >= mesesAlerta)
                    return Json(new { status = false, message = "Los meses de CRÍTICO deben ser menores que los de ALERTA." });

                var cfg = await _context.ConfigAlertasVencimiento.FirstOrDefaultAsync(c => c.EmpresaId == empresaId);
                if (cfg == null)
                {
                    cfg = new ConfigAlertaVencimiento { EmpresaId = empresaId, FechaRegistro = DateTime.Now };
                    _context.ConfigAlertasVencimiento.Add(cfg);
                }
                cfg.MesesCritico = mesesCritico;
                cfg.MesesAlerta = mesesAlerta;
                await _context.SaveChangesAsync();
                return Json(new { status = true, message = "Configuración guardada." });
            }
            catch (Exception ex) { return Json(new { status = false, message = ex.Message }); }
        }

        [HttpGet]
        public async Task<JsonResult> GetResumenEnvioManual()
        {
            try
            {
                var empresas = await _context.Empresas.Where(e => e.Estado == true).OrderBy(e => e.RazonSocial).ToListAsync();
                var correos  = await _context.CorreosAlertaStock.Where(c => c.Activo).ToListAsync();

                var data = empresas.Select(e => new {
                    EmpresaId         = e.Id,
                    Empresa           = e.RazonSocial,
                    TotalDestinatarios = correos.Count(c => c.EmpresaId == e.Id)
                }).ToList();

                return Json(new { status = true, data });
            }
            catch (Exception ex) { return Json(new { status = false, message = ex.Message }); }
        }

        [HttpGet]
        public async Task<JsonResult> GetCorreosAlerta(int? empresaId)
        {
            try
            {
                var query = _context.CorreosAlertaStock
                    .Join(_context.Empresas, c => c.EmpresaId, e => e.Id, (c, e) => new
                    {
                        c.Id, c.EmpresaId, Empresa = e.RazonSocial,
                        c.Nombre, c.Email, c.Frecuencia, c.DiaEnvio, c.Activo
                    })
                    .AsQueryable();
                if (empresaId.HasValue) query = query.Where(x => x.EmpresaId == empresaId.Value);
                var data = await query.OrderBy(x => x.Empresa).ThenBy(x => x.Nombre).ToListAsync();
                return Json(new { status = true, data });
            }
            catch (Exception ex) { return Json(new { status = false, message = ex.Message }); }
        }

        [HttpPost]
        public async Task<JsonResult> GuardarCorreoAlerta(int? id, int empresaId, string nombre, string email, string frecuencia, int diaEnvio, bool activo)
        {
            try
            {
                CorreoAlertaStock correo;
                if (id.HasValue && id.Value > 0)
                {
                    correo = await _context.CorreosAlertaStock.FindAsync(id.Value);
                    if (correo == null) return Json(new { status = false, message = "Registro no encontrado." });
                }
                else
                {
                    correo = new CorreoAlertaStock { FechaRegistro = DateTime.Now };
                    _context.CorreosAlertaStock.Add(correo);
                }
                correo.EmpresaId = empresaId;
                correo.Nombre = nombre;
                correo.Email = email;
                correo.Frecuencia = frecuencia;
                correo.DiaEnvio = diaEnvio;
                correo.Activo = activo;
                await _context.SaveChangesAsync();
                return Json(new { status = true, message = "Destinatario guardado." });
            }
            catch (Exception ex) { return Json(new { status = false, message = ex.Message }); }
        }

        [HttpPost]
        public async Task<JsonResult> EnviarEmailPrueba(string email, string estados)
        {
            if (!EsAdminGlobal) return Json(new { status = false, message = "Acceso denegado." });
            try
            {
                var filtros = (estados ?? "VENCIDO,CRITICO,ALERTA,OPTIMO,SIN_FECHA")
                    .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                    .ToHashSet();

                var empresaId = EmpresaUsuarioId;
                var empresa   = await _context.Empresas.FindAsync(empresaId);
                var (body, total) = await BuildEmailBody(empresaId, filtros);

                var html = WrapEmailHtml(body, empresa?.RazonSocial ?? $"ID {empresaId}", esPrueba: true);
                await _emailService.SendEmailAsync(new List<string> { email },
                    "🧪 [PRUEBA] Alerta de Vencimiento de Stock - SYSSAF", html);

                return Json(new { status = true, message = $"Correo de prueba enviado a {email}. {total} producto(s) incluidos." });
            }
            catch (Exception ex) { return Json(new { status = false, message = $"Error al enviar: {ex.Message}" }); }
        }

        [HttpPost]
        public async Task<JsonResult> EnviarCorreoManual(int empresaId, string estados)
        {
            if (!EsAdminGlobal) return Json(new { status = false, message = "Acceso denegado." });
            try
            {
                var destinatarios = await _context.CorreosAlertaStock
                    .Where(c => c.EmpresaId == empresaId && c.Activo)
                    .Select(c => c.Email)
                    .ToListAsync();

                if (destinatarios.Count == 0)
                    return Json(new { status = false, message = "Esta empresa no tiene destinatarios configurados." });

                var filtros = (estados ?? "VENCIDO,CRITICO,ALERTA,OPTIMO,SIN_FECHA")
                    .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                    .ToHashSet();

                var empresa = await _context.Empresas.FindAsync(empresaId);
                var (body, total) = await BuildEmailBody(empresaId, filtros);

                var html = WrapEmailHtml(body, empresa?.RazonSocial ?? $"ID {empresaId}", esPrueba: false);
                await _emailService.SendEmailAsync(destinatarios,
                    $"⚠️ Alerta de Vencimiento de Stock — {empresa?.RazonSocial ?? $"Empresa {empresaId}"}", html);

                return Json(new { status = true, message = $"Correo enviado a {destinatarios.Count} destinatario(s). {total} producto(s) incluidos." });
            }
            catch (Exception ex) { return Json(new { status = false, message = $"Error al enviar: {ex.Message}" }); }
        }

        private async Task<(string body, int total)> BuildEmailBody(int empresaId, HashSet<string> filtros)
        {
            var config = await _context.ConfigAlertasVencimiento.FirstOrDefaultAsync(c => c.EmpresaId == empresaId);
            int diasCritico = (config?.MesesCritico ?? 18) * 30;
            int diasAlerta  = (config?.MesesAlerta  ?? 24) * 30;

            var hoy = DateTime.Today;
            var idAprobado = (await _context.Estados.FirstOrDefaultAsync(e => e.Nombre == "Aprobado" && e.Tabla == "INGRESOSALIDAALM"))?.Id ?? -1;

            var ingresos = (from d in _context.DIngresoSalidaAlms
                            join c in _context.IngresoSalidaAlms on d.IngresoSalidaAlmId equals c.Id
                            where c.EmpresaId == empresaId && c.EstadoId == idAprobado && c.TipoMovimiento == true
                            select new { d.CodProducto, d.DescripcionProducto, d.Lote, d.FechaVencimiento, Cantidad = d.Cantidad ?? 0 })
                           .ToList()
                           .GroupBy(x => new { x.CodProducto, x.DescripcionProducto, x.Lote, x.FechaVencimiento })
                           .Select(g => new { g.Key.CodProducto, g.Key.DescripcionProducto, g.Key.Lote, g.Key.FechaVencimiento, Total = g.Sum(x => x.Cantidad) })
                           .ToList();

            var salidasDict = (from d in _context.DIngresoSalidaAlms
                               join c in _context.IngresoSalidaAlms on d.IngresoSalidaAlmId equals c.Id
                               where c.EmpresaId == empresaId && c.EstadoId == idAprobado && c.TipoMovimiento == false
                               select new { d.CodProducto, d.Lote, Cantidad = d.Cantidad ?? 0 })
                              .ToList()
                              .GroupBy(x => new { x.CodProducto, x.Lote })
                              .ToDictionary(g => (g.Key.CodProducto ?? "", g.Key.Lote ?? ""), g => g.Sum(x => x.Cantidad));

            var filas = ingresos
                .Select(i => {
                    var salida = salidasDict.TryGetValue((i.CodProducto ?? "", i.Lote ?? ""), out var s) ? s : 0;
                    var stock  = i.Total - salida;
                    if (stock <= 0) return null;
                    int? dias  = i.FechaVencimiento.HasValue ? (int?)(i.FechaVencimiento.Value.Date - hoy).TotalDays : null;
                    string est = dias == null ? "SIN_FECHA" : dias < 0 ? "VENCIDO" : dias < diasCritico ? "CRITICO" : dias < diasAlerta ? "ALERTA" : "OPTIMO";
                    return new { Codigo = i.CodProducto ?? "-", Descripcion = i.DescripcionProducto ?? i.CodProducto ?? "-", Lote = i.Lote ?? "-", Stock = stock, FechaVenc = i.FechaVencimiento, Dias = dias, Estado = est };
                })
                .Where(x => x != null && filtros.Contains(x.Estado))
                .OrderBy(x => x!.Dias.HasValue ? 0 : 1).ThenBy(x => x!.Dias).ThenBy(x => x!.Descripcion)
                .ToList();

            if (filas.Count == 0)
                return ("<p style='padding:20px;color:#27ae60;font-size:15px;text-align:center'><strong>✅ No hay productos que coincidan con los filtros seleccionados.</strong></p>", 0);

            static string BgEst(string e) => e switch { "VENCIDO" => "#c0392b", "CRITICO" => "#e74c3c", "ALERTA" => "#f39c12", "OPTIMO" => "#27ae60", _ => "#95a5a6" };
            static string LblEst(string e) => e switch { "VENCIDO" => "🔴 VENCIDO", "CRITICO" => "🔴 CRÍTICO", "ALERTA" => "🟡 ALERTA", "OPTIMO" => "🟢 ÓPTIMO", _ => "⚪ SIN FECHA" };

            var rows = string.Join("", filas.Select((f, idx) => {
                var bg      = idx % 2 == 0 ? "#ffffff" : "#f8f9fa";
                var diasStr = f!.Dias.HasValue ? (f.Dias < 0 ? $"Vencido hace {-f.Dias} d." : $"{f.Dias} días") : "—";
                return $"<tr style='background:{bg}'><td style='padding:6px 8px;border-bottom:1px solid #e0e0e0;font-family:monospace;font-size:12px'>{f.Codigo}</td><td style='padding:6px 8px;border-bottom:1px solid #e0e0e0'>{f.Descripcion}</td><td style='padding:6px 8px;border-bottom:1px solid #e0e0e0;text-align:center'>{f.Lote}</td><td style='padding:6px 8px;border-bottom:1px solid #e0e0e0;text-align:right;font-weight:bold'>{f.Stock:N2}</td><td style='padding:6px 8px;border-bottom:1px solid #e0e0e0;text-align:center'>{f.FechaVenc?.ToString("dd/MM/yyyy") ?? "—"}</td><td style='padding:6px 8px;border-bottom:1px solid #e0e0e0;text-align:center'>{diasStr}</td><td style='padding:4px 8px;border-bottom:1px solid #e0e0e0;text-align:center;background:{BgEst(f.Estado)};color:#fff;font-weight:bold;font-size:11px'>{LblEst(f.Estado)}</td></tr>";
            }));

            var counts = new[] { "VENCIDO", "CRITICO", "ALERTA", "OPTIMO", "SIN_FECHA" }
                .Select(e => (Estado: e, N: filas.Count(x => x!.Estado == e))).Where(x => x.N > 0);
            var badges = string.Join("", counts.Select(x =>
                $"<span style='background:{BgEst(x.Estado)};color:#fff;padding:4px 12px;border-radius:4px;font-size:12px;font-weight:bold;margin:2px'>{LblEst(x.Estado)}: {x.N}</span>"));

            var body = $@"<div style='display:flex;gap:4px;padding:12px 16px;background:#f0f0f0;flex-wrap:wrap'>{badges}</div>
                <table style='width:100%;border-collapse:collapse;font-size:13px'>
                    <thead><tr style='background:#2c3e50;color:#fff'>
                        <th style='padding:8px;text-align:left'>Código</th>
                        <th style='padding:8px;text-align:left'>Nombre / Descripción</th>
                        <th style='padding:8px;text-align:center'>Lote</th>
                        <th style='padding:8px;text-align:right'>Stock</th>
                        <th style='padding:8px;text-align:center'>Fecha Venc.</th>
                        <th style='padding:8px;text-align:center'>Días para Vencer</th>
                        <th style='padding:8px;text-align:center'>Estado</th>
                    </tr></thead>
                    <tbody>{rows}</tbody>
                </table>";

            return (body, filas.Count);
        }

        private static string WrapEmailHtml(string bodyContent, string empresaNombre, bool esPrueba)
        {
            var titulo  = esPrueba ? "⚠️ Alerta de Vencimiento de Stock — CORREO DE PRUEBA" : "⚠️ Alerta de Vencimiento de Stock";
            var aviso   = esPrueba
                ? "<p style='padding:10px 16px;background:#fff3cd;margin:0;font-size:13px'>📧 Este es un <strong>correo de prueba</strong>. El correo automático tendrá exactamente este mismo formato.</p>"
                : "";
            return $@"<div style='font-family:Arial,sans-serif;max-width:860px;margin:auto;border:1px solid #ddd'>
                <div style='background:#2c3e50;color:#fff;padding:16px'>
                    <h2 style='margin:0;font-size:18px'>{titulo}</h2>
                    <p style='margin:4px 0 0;font-size:13px;opacity:.8'>Empresa: {empresaNombre} · Fecha: {DateTime.Today:dd/MM/yyyy}</p>
                </div>
                {aviso}
                {bodyContent}
                <p style='padding:10px 16px;font-size:11px;color:#999;margin:0;background:#f8f9fa'>Generado automáticamente por SYSSAF · ERP Kardex</p>
            </div>";
        }

        [HttpPost]
        public async Task<JsonResult> EliminarCorreoAlerta(int id)
        {
            try
            {
                var correo = await _context.CorreosAlertaStock.FindAsync(id);
                if (correo == null) return Json(new { status = false, message = "No encontrado." });
                _context.CorreosAlertaStock.Remove(correo);
                await _context.SaveChangesAsync();
                return Json(new { status = true, message = "Eliminado correctamente." });
            }
            catch (Exception ex) { return Json(new { status = false, message = ex.Message }); }
        }

        #endregion
    }
}