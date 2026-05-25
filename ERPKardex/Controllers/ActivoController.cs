using ERPKardex.Data;
using ERPKardex.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ERPKardex.Controllers
{
    public class ActivoController : BaseController
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public ActivoController(ApplicationDbContext context, IWebHostEnvironment webHostEnvironment)
        {
            _context = context;
            _webHostEnvironment = webHostEnvironment;
        }

        // =====================================================================
        // VISTAS
        // =====================================================================

        public IActionResult Index() => View();
        public IActionResult Vehiculos() => View();
        //public IActionResult Movimientos() => View();
        public IActionResult MovimientosComputo() => View();
        public IActionResult MovimientosVehiculos() => View();

        // =====================================================================
        // CATÁLOGOS / COMBOS
        // =====================================================================

        [HttpGet]
        public async Task<JsonResult> GetEmpresas()
        {
            try
            {
                var data = await _context.Empresas
                    .Where(e => e.Estado == true)
                    .OrderBy(e => e.Nombre)
                    .Select(e => new { e.Id, e.Nombre, e.Ruc })
                    .ToListAsync();
                return Json(new { status = true, data });
            }
            catch (Exception ex) { return Json(new { status = false, message = ex.Message }); }
        }

        [HttpGet]
        public async Task<JsonResult> GetTiposActivo()
        {
            try
            {
                var data = await _context.TipoActivo
                    .Where(t => t.Estado).OrderBy(t => t.Nombre)
                    .Select(t => new { t.Id, t.Codigo, t.Nombre }).ToListAsync();
                return Json(new { status = true, data });
            }
            catch (Exception ex) { return Json(new { status = false, message = ex.Message }); }
        }

        [HttpGet]
        public async Task<JsonResult> GetGrupos(int tipoActivoId)
        {
            try
            {
                var data = await _context.GrupoActivo
                    .Where(g => g.TipoActivoId == tipoActivoId && g.Estado).OrderBy(g => g.Nombre)
                    .Select(g => new { g.Id, g.Codigo, g.Nombre }).ToListAsync();
                return Json(new { status = true, data });
            }
            catch (Exception ex) { return Json(new { status = false, message = ex.Message }); }
        }

        [HttpGet]
        public async Task<JsonResult> GetPersonal(int? empresaId)
        {
            try
            {
                var query = _context.Personal.Where(p => p.Estado == true);
                if (empresaId.HasValue && empresaId > 0)
                    query = query.Where(p => p.EmpresaId == empresaId);
                var data = await query.OrderBy(p => p.NombresCompletos)
                    .Select(p => new { p.Id, p.Dni, p.NombresCompletos, p.EmpresaId }).ToListAsync();
                return Json(new { status = true, data });
            }
            catch (Exception ex) { return Json(new { status = false, message = ex.Message }); }
        }

        [HttpGet]
        public async Task<JsonResult> GetTiposDocumentoActivo()
        {
            try
            {
                var data = await _context.TipoDocumentoActivo
                    .Where(t => t.Estado).OrderBy(t => t.Nombre)
                    .Select(t => new { t.Id, t.Codigo, t.Nombre }).ToListAsync();
                return Json(new { status = true, data });
            }
            catch (Exception ex) { return Json(new { status = false, message = ex.Message }); }
        }

        // =====================================================================
        // ACTIVOS - LISTADO
        // =====================================================================

        [HttpGet]
        public async Task<JsonResult> GetActivos(string tipoCodigo, int? empresaId, int? grupoId, string? buscar)
        {
            try
            {
                var query = from a in _context.Activo
                            join t in _context.TipoActivo on a.TipoActivoId equals t.Id
                            join e in _context.Empresas on a.EmpresaId equals e.Id
                            join g in _context.GrupoActivo on a.GrupoActivoId equals g.Id into gj
                            from g in gj.DefaultIfEmpty()
                            where t.Codigo == tipoCodigo && a.Estado
                            select new { a, t, e, g };

                if (empresaId.HasValue && empresaId > 0)
                    query = query.Where(x => x.a.EmpresaId == empresaId);
                if (grupoId.HasValue && grupoId > 0)
                    query = query.Where(x => x.a.GrupoActivoId == grupoId);
                if (!string.IsNullOrWhiteSpace(buscar))
                {
                    buscar = buscar.ToLower();
                    query = query.Where(x =>
                        x.a.Codigo.ToLower().Contains(buscar) ||
                        (x.a.Marca != null && x.a.Marca.ToLower().Contains(buscar)) ||
                        (x.a.Modelo != null && x.a.Modelo.ToLower().Contains(buscar)) ||
                        (x.a.NumeroSerie != null && x.a.NumeroSerie.ToLower().Contains(buscar)) ||
                        (x.a.Placa != null && x.a.Placa.ToLower().Contains(buscar)) ||
                        (x.a.Subtipo != null && x.a.Subtipo.ToLower().Contains(buscar)));
                }

                var activos = await query.OrderByDescending(x => x.a.Id)
                    .Select(x => new
                    {
                        x.a.Id,
                        x.a.Codigo,
                        Tipo = x.t.Nombre,
                        Empresa = x.e.Nombre,
                        Ruc = x.e.Ruc,
                        Grupo = x.g != null ? x.g.Nombre : "",
                        x.a.Marca,
                        x.a.Modelo,
                        x.a.NumeroSerie,
                        x.a.Placa,
                        x.a.Subtipo,
                        x.a.AnioFabricacion,
                        x.a.EstadoUso,
                        x.a.Condicion
                    }).ToListAsync();

                // Obtener especificaciones para cada activo
                var activoIds = activos.Select(x => x.Id).ToList();
                var especificacionesPorActivo = await _context.ActivoDetalle
                    .Where(d => activoIds.Contains(d.ActivoId) && d.Estado)
                    .OrderBy(d => d.Orden)
                    .Select(d => new { d.ActivoId, d.Clave, d.Valor })
                    .ToListAsync();

                // Agrupar especificaciones por Activo
                var especDict = especificacionesPorActivo
                    .GroupBy(x => x.ActivoId)
                    .ToDictionary(g => g.Key, g => g.Select(x => new { x.Clave, x.Valor }).ToList());

                // Agregar especificaciones a cada activo
                var data = activos.Select(x => new
                {
                    x.Id,
                    x.Codigo,
                    x.Tipo,
                    x.Empresa,
                    x.Ruc,
                    x.Grupo,
                    x.Marca,
                    x.Modelo,
                    x.NumeroSerie,
                    x.Placa,
                    x.Subtipo,
                    x.AnioFabricacion,
                    x.EstadoUso,
                    x.Condicion,
                    Especificaciones = (object)(especDict.ContainsKey(x.Id) ? especDict[x.Id] : Enumerable.Empty<object>().ToList())
                }).ToList();

                return Json(new { status = true, data });
            }
            catch (Exception ex) { return Json(new { status = false, message = ex.Message }); }
        }

        // =====================================================================
        // ACTIVO - OBTENER POR ID
        // =====================================================================

        [HttpGet]
        public async Task<JsonResult> GetActivoById(int id)
        {
            try
            {
                var activo = await (from a in _context.Activo
                                    join t in _context.TipoActivo on a.TipoActivoId equals t.Id
                                    join e in _context.Empresas on a.EmpresaId equals e.Id
                                    join g in _context.GrupoActivo on a.GrupoActivoId equals g.Id into gj
                                    from g in gj.DefaultIfEmpty()
                                    where a.Id == id && a.Estado
                                    select new
                                    {
                                        a.Id,
                                        a.Codigo,
                                        a.TipoActivoId,
                                        TipoCodigo = t.Codigo,
                                        TipoNombre = t.Nombre,
                                        a.GrupoActivoId,
                                        GrupoNombre = g != null ? g.Nombre : "",
                                        a.EmpresaId,
                                        EmpresaNombre = e.Nombre,
                                        EmpresaRuc = e.Ruc,
                                        a.Descripcion,
                                        a.Marca,
                                        a.Modelo,
                                        a.NumeroSerie,
                                        a.Placa,
                                        a.Subtipo,
                                        a.AnioFabricacion,
                                        a.EstadoUso,
                                        a.Condicion
                                    }).FirstOrDefaultAsync();

                if (activo == null) return Json(new { status = false, message = "Activo no encontrado." });

                var especificaciones = await _context.ActivoDetalle
                    .Where(d => d.ActivoId == id && d.Estado).OrderBy(d => d.Orden)
                    .Select(d => new { d.Id, d.Clave, d.Valor, d.Orden }).ToListAsync();

                var documentos = await (from d in _context.ActivoDocumento
                                        join td in _context.TipoDocumentoActivo on d.TipoDocumentoActivoId equals td.Id
                                        where d.ActivoId == id && d.Estado
                                        select new
                                        {
                                            d.Id,
                                            d.TipoDocumentoActivoId,
                                            TipoDocumento = td.Nombre,
                                            d.NumeroDocumento,
                                            FechaEmision = d.FechaEmision.HasValue ? d.FechaEmision.Value.ToString("yyyy-MM-dd") : "",
                                            FechaVencimiento = d.FechaVencimiento.HasValue ? d.FechaVencimiento.Value.ToString("yyyy-MM-dd") : "",
                                            d.RutaArchivo,
                                            d.Observacion
                                        }).ToListAsync();

                // 1. Buscamos el último movimiento real del activo sin importar si es entrega o devolución
                var ultimoMovimiento = await (from dm in _context.DMovimientoActivo
                                              join m in _context.MovimientoActivo on dm.MovimientoActivoId equals m.Id
                                              join p in _context.Personal on m.PersonalId equals p.Id
                                              join e in _context.Empresas on m.EmpresaId equals e.Id
                                              where dm.ActivoId == id && dm.Estado && m.Estado
                                              orderby m.FechaMovimiento descending
                                              select new
                                              {
                                                  m.TipoMovimiento, // Agregamos este campo para evaluarlo
                                                  PersonalNombre = p.NombresCompletos,
                                                  PersonalDni = p.Dni,
                                                  Empresa = e.Nombre,
                                                  FechaEntrega = m.FechaMovimiento.ToString("dd/MM/yyyy"),
                                                  dm.Ubicacion
                                              }).FirstOrDefaultAsync();

                // 2. Evaluamos: si el último movimiento es nulo o NO es una entrega, la asignación es null
                object asignacion = null;

                if (ultimoMovimiento != null && ultimoMovimiento.TipoMovimiento == "ENTREGA")
                {
                    asignacion = new
                    {
                        ultimoMovimiento.PersonalNombre,
                        ultimoMovimiento.PersonalDni,
                        ultimoMovimiento.Empresa,
                        ultimoMovimiento.FechaEntrega,
                        ultimoMovimiento.Ubicacion
                    };
                }

                return Json(new { status = true, activo, especificaciones, documentos, asignacion });
            }
            catch (Exception ex) { return Json(new { status = false, message = ex.Message }); }
        }

        // =====================================================================
        // ACTIVO - GUARDAR (CREAR / EDITAR)
        // =====================================================================

        [HttpPost]
        public async Task<JsonResult> GuardarActivo(
            int id, string codigo, int tipoActivoId, int? grupoActivoId, int empresaId,
            string? descripcion, string? marca, string? modelo, string? numeroSerie,
            string? placa, string? subtipo, int? anioFabricacion, string estadoUso,
            string? condicion, string? especificacionesJson)
        {
            try
            {
                var especificaciones = new List<(string clave, string valor)>();
                if (!string.IsNullOrWhiteSpace(especificacionesJson))
                {
                    var items = System.Text.Json.JsonSerializer.Deserialize<List<Dictionary<string, string>>>(especificacionesJson);
                    if (items != null)
                        foreach (var item in items)
                        {
                            var clave = item.ContainsKey("clave") ? item["clave"] : "";
                            var valor = item.ContainsKey("valor") ? item["valor"] : "";
                            if (!string.IsNullOrWhiteSpace(clave)) especificaciones.Add((clave, valor));
                        }
                }

                if (id == 0)
                {
                    if (await _context.Activo.AnyAsync(a => a.Codigo == codigo && a.Estado))
                        return Json(new { status = false, message = $"El código '{codigo}' ya está en uso." });

                    var nuevo = new Activo
                    {
                        Codigo = codigo,
                        TipoActivoId = tipoActivoId,
                        GrupoActivoId = grupoActivoId,
                        EmpresaId = empresaId,
                        Descripcion = descripcion,
                        Marca = marca,
                        Modelo = modelo,
                        NumeroSerie = numeroSerie,
                        Placa = placa,
                        Subtipo = subtipo,
                        AnioFabricacion = anioFabricacion,
                        EstadoUso = estadoUso ?? "ACTIVO",
                        Condicion = condicion ?? "BUENA",
                        Estado = true,
                        FechaRegistro = DateTime.Now
                    };
                    _context.Activo.Add(nuevo);
                    await _context.SaveChangesAsync();

                    int orden = 1;
                    foreach (var (clave, valor) in especificaciones)
                    {
                        _context.ActivoDetalle.Add(new ActivoDetalle
                        {
                            ActivoId = nuevo.Id,
                            Clave = clave,
                            Valor = valor,
                            Orden = orden++,
                            Estado = true,
                            FechaRegistro = DateTime.Now
                        });
                    }
                    await _context.SaveChangesAsync();
                    return Json(new { status = true, message = "Activo creado correctamente.", id = nuevo.Id });
                }
                else
                {
                    var activo = await _context.Activo.FirstOrDefaultAsync(a => a.Id == id && a.Estado);
                    if (activo == null) return Json(new { status = false, message = "Activo no encontrado." });
                    if (await _context.Activo.AnyAsync(a => a.Codigo == codigo && a.Estado && a.Id != id))
                        return Json(new { status = false, message = $"El código '{codigo}' ya está en uso." });

                    activo.Codigo = codigo; activo.TipoActivoId = tipoActivoId; activo.GrupoActivoId = grupoActivoId;
                    activo.EmpresaId = empresaId; activo.Descripcion = descripcion; activo.Marca = marca;
                    activo.Modelo = modelo; activo.NumeroSerie = numeroSerie; activo.Placa = placa;
                    activo.Subtipo = subtipo; activo.AnioFabricacion = anioFabricacion;
                    activo.EstadoUso = estadoUso ?? activo.EstadoUso; activo.Condicion = condicion ?? activo.Condicion;

                    var espAnteriores = await _context.ActivoDetalle.Where(e => e.ActivoId == id && e.Estado).ToListAsync();
                    foreach (var esp in espAnteriores) esp.Estado = false;

                    int orden = 1;
                    foreach (var (clave, valor) in especificaciones)
                    {
                        _context.ActivoDetalle.Add(new ActivoDetalle
                        {
                            ActivoId = id,
                            Clave = clave,
                            Valor = valor,
                            Orden = orden++,
                            Estado = true,
                            FechaRegistro = DateTime.Now
                        });
                    }
                    await _context.SaveChangesAsync();
                    return Json(new { status = true, message = "Activo actualizado correctamente." });
                }
            }
            catch (Exception ex) { return Json(new { status = false, message = "Error: " + ex.Message }); }
        }

        // =====================================================================
        // ACTIVO - ELIMINAR
        // =====================================================================

        [HttpPost]
        public async Task<JsonResult> EliminarActivo(int id)
        {
            try
            {
                var activo = await _context.Activo.FirstOrDefaultAsync(a => a.Id == id && a.Estado);
                if (activo == null) return Json(new { status = false, message = "Activo no encontrado." });
                activo.Estado = false;
                await _context.SaveChangesAsync();
                return Json(new { status = true, message = "Activo eliminado correctamente." });
            }
            catch (Exception ex) { return Json(new { status = false, message = "Error: " + ex.Message }); }
        }

        // =====================================================================
        // DOCUMENTOS DE ACTIVO (CON SOPORTE DE ARCHIVO)
        // =====================================================================

        [HttpPost]
        public async Task<JsonResult> GuardarDocumento(
            int id, int activoId, int tipoDocumentoActivoId, string? numeroDocumento,
            DateTime? fechaEmision, DateTime? fechaVencimiento, string? observacion, IFormFile? archivo)
        {
            try
            {
                string? rutaArchivo = null;
                if (archivo != null && archivo.Length > 0)
                {
                    string folderPath = Path.Combine(_webHostEnvironment.WebRootPath, "uploads", "documentos_activo");
                    if (!Directory.Exists(folderPath)) Directory.CreateDirectory(folderPath);
                    string ext = Path.GetExtension(archivo.FileName);
                    string fileName = $"Doc_{activoId}_{Guid.NewGuid():N}{ext}";
                    string filePath = Path.Combine(folderPath, fileName);
                    using (var stream = new FileStream(filePath, FileMode.Create))
                        await archivo.CopyToAsync(stream);
                    rutaArchivo = "/uploads/documentos_activo/" + fileName;
                }

                if (id == 0)
                {
                    var doc = new ActivoDocumento
                    {
                        ActivoId = activoId,
                        TipoDocumentoActivoId = tipoDocumentoActivoId,
                        NumeroDocumento = numeroDocumento,
                        FechaEmision = fechaEmision,
                        FechaVencimiento = fechaVencimiento,
                        RutaArchivo = rutaArchivo,
                        Observacion = observacion,
                        Estado = true,
                        FechaRegistro = DateTime.Now
                    };
                    _context.ActivoDocumento.Add(doc);
                    await _context.SaveChangesAsync();
                    return Json(new { status = true, message = "Documento registrado correctamente." });
                }
                else
                {
                    var doc = await _context.ActivoDocumento.FirstOrDefaultAsync(d => d.Id == id && d.Estado);
                    if (doc == null) return Json(new { status = false, message = "Documento no encontrado." });
                    doc.TipoDocumentoActivoId = tipoDocumentoActivoId;
                    doc.NumeroDocumento = numeroDocumento;
                    doc.FechaEmision = fechaEmision;
                    doc.FechaVencimiento = fechaVencimiento;
                    doc.Observacion = observacion;
                    if (rutaArchivo != null) doc.RutaArchivo = rutaArchivo;
                    await _context.SaveChangesAsync();
                    return Json(new { status = true, message = "Documento actualizado correctamente." });
                }
            }
            catch (Exception ex) { return Json(new { status = false, message = "Error: " + ex.Message }); }
        }

        [HttpPost]
        public async Task<JsonResult> EliminarDocumento(int id)
        {
            try
            {
                var doc = await _context.ActivoDocumento.FirstOrDefaultAsync(d => d.Id == id && d.Estado);
                if (doc == null) return Json(new { status = false, message = "Documento no encontrado." });
                doc.Estado = false;
                await _context.SaveChangesAsync();
                return Json(new { status = true, message = "Documento eliminado correctamente." });
            }
            catch (Exception ex) { return Json(new { status = false, message = "Error: " + ex.Message }); }
        }

        // =====================================================================
        // MOVIMIENTOS - LISTADO (CORREGIDO)
        // =====================================================================

        [HttpGet]
        public async Task<JsonResult> GetMovimientos(string? tipoCodigo, int? empresaId, string? buscar, string? codigo)
        {
            try
            {
                var query = from m in _context.MovimientoActivo
                            join p in _context.Personal on m.PersonalId equals p.Id
                            join e in _context.Empresas on m.EmpresaId equals e.Id
                            where m.Estado
                            select new { m, p, e };

                if (empresaId.HasValue && empresaId > 0)
                    query = query.Where(x => x.m.EmpresaId == empresaId);

                if (!string.IsNullOrWhiteSpace(tipoCodigo))
                {
                    var movIds = await (from dm in _context.DMovimientoActivo
                                        join a in _context.Activo on dm.ActivoId equals a.Id
                                        join t in _context.TipoActivo on a.TipoActivoId equals t.Id
                                        where t.Codigo == tipoCodigo && dm.Estado
                                        select dm.MovimientoActivoId).Distinct().ToListAsync();
                    query = query.Where(x => movIds.Contains(x.m.Id));
                }

                if (!string.IsNullOrWhiteSpace(codigo))
                {
                    var movIds = await (from dm in _context.DMovimientoActivo
                                        join a in _context.Activo on dm.ActivoId equals a.Id
                                        where a.Codigo.Contains(codigo) && dm.Estado
                                        select dm.MovimientoActivoId).Distinct().ToListAsync();

                    query = query.Where(x => movIds.Contains(x.m.Id));
                }

                if (!string.IsNullOrWhiteSpace(buscar))
                {
                    buscar = buscar.ToLower();
                    query = query.Where(x =>
                        x.m.Codigo.ToLower().Contains(buscar) ||
                        x.p.NombresCompletos.ToLower().Contains(buscar) ||
                        (x.p.Dni != null && x.p.Dni.Contains(buscar)));
                }

                var movimientos = await query
                    .OrderByDescending(x => x.m.FechaMovimiento).ThenByDescending(x => x.m.Id)
                    .Select(x => new
                    {
                        x.m.Id,
                        x.m.Codigo,
                        x.m.TipoMovimiento,
                        Empresa = x.e.Nombre,
                        Personal = x.p.NombresCompletos,
                        PersonalDni = x.p.Dni,
                        FechaMovimiento = x.m.FechaMovimiento.ToString("dd/MM/yyyy"),
                        x.m.Observacion,
                        x.m.RutaActa,
                        Estado = x.m.Estado
                    }).Take(200).ToListAsync();

                var movIds2 = movimientos.Select(m => m.Id).ToList();
                var conteos = await _context.DMovimientoActivo
                    .Where(d => movIds2.Contains(d.MovimientoActivoId) && d.Estado)
                    .GroupBy(d => d.MovimientoActivoId)
                    .Select(g => new { MovId = g.Key, Cantidad = g.Count() }).ToListAsync();

                var data = movimientos.Select(m => new
                {
                    m.Id,
                    m.Codigo,
                    m.TipoMovimiento,
                    m.Empresa,
                    m.Personal,
                    m.PersonalDni,
                    m.FechaMovimiento,
                    m.Observacion,
                    m.RutaActa,
                    m.Estado,
                    CantidadActivos = conteos.FirstOrDefault(c => c.MovId == m.Id)?.Cantidad ?? 0
                }).ToList();

                return Json(new { status = true, data });
            }
            catch (Exception ex) { return Json(new { status = false, message = ex.Message }); }
        }

        // =====================================================================
        // MOVIMIENTO - OBTENER POR ID
        // =====================================================================

        [HttpGet]
        public async Task<JsonResult> GetMovimientoById(int id)
        {
            try
            {
                var mov = await (from m in _context.MovimientoActivo
                                 join p in _context.Personal on m.PersonalId equals p.Id
                                 join e in _context.Empresas on m.EmpresaId equals e.Id
                                 where m.Id == id && m.Estado
                                 select new
                                 {
                                     m.Id,
                                     m.Codigo,
                                     m.TipoMovimiento,
                                     m.EmpresaId,
                                     Empresa = e.Nombre,
                                     m.PersonalId,
                                     Personal = p.NombresCompletos,
                                     PersonalDni = p.Dni,
                                     FechaMovimiento = m.FechaMovimiento.ToString("yyyy-MM-dd"),
                                     m.RutaActa,
                                     m.Observacion
                                 }).FirstOrDefaultAsync();

                if (mov == null) return Json(new { status = false, message = "Movimiento no encontrado." });

                var detalle = await (from dm in _context.DMovimientoActivo
                                     join a in _context.Activo on dm.ActivoId equals a.Id
                                     where dm.MovimientoActivoId == id && dm.Estado
                                     select new
                                     {
                                         dm.Id,
                                         dm.ActivoId,
                                         a.Codigo,
                                         a.Marca,
                                         a.Modelo,
                                         a.NumeroSerie,
                                         a.Placa,
                                         a.Subtipo,
                                         dm.Ubicacion,
                                         dm.Observacion
                                     }).ToListAsync();

                return Json(new { status = true, movimiento = mov, detalle });
            }
            catch (Exception ex) { return Json(new { status = false, message = ex.Message }); }
        }

        // =====================================================================
        // MOVIMIENTO - DETALLE (formato cabecera/detalle para vista)
        // =====================================================================

        [HttpGet]
        public async Task<JsonResult> GetMovimientoDetalle(int id)
        {
            try
            {
                var cabecera = await (from m in _context.MovimientoActivo
                                      join p in _context.Personal on m.PersonalId equals p.Id
                                      join e in _context.Empresas on m.EmpresaId equals e.Id
                                      where m.Id == id && m.Estado
                                      select new
                                      {
                                          m.Id,
                                          m.Codigo,
                                          m.TipoMovimiento,
                                          Empresa = e.Nombre,
                                          Personal = p.NombresCompletos,
                                          PersonalDni = p.Dni,
                                          Fecha = m.FechaMovimiento.ToString("dd/MM/yyyy"),
                                          m.Observacion
                                      }).FirstOrDefaultAsync();

                if (cabecera == null) return Json(new { status = false, message = "Movimiento no encontrado." });

                var detalle = await (from dm in _context.DMovimientoActivo
                                     join a in _context.Activo on dm.ActivoId equals a.Id
                                     join t in _context.TipoActivo on a.TipoActivoId equals t.Id
                                     where dm.MovimientoActivoId == id && dm.Estado
                                     select new
                                     {
                                         dm.Id,
                                         dm.ActivoId,
                                         a.Codigo,
                                         Tipo = t.Nombre,
                                         a.Marca,
                                         a.Modelo,
                                         a.NumeroSerie,
                                         a.Placa,
                                         a.Subtipo,
                                         dm.Ubicacion,
                                         dm.Observacion
                                     }).ToListAsync();

                return Json(new { status = true, cabecera, detalle });
            }
            catch (Exception ex) { return Json(new { status = false, message = ex.Message }); }
        }

        // =====================================================================
        // MOVIMIENTO - GUARDAR
        // =====================================================================

        [HttpPost]
        public async Task<JsonResult> GuardarMovimiento(
            string tipoMovimiento, int empresaId, int personalId,
            DateTime fechaMovimiento, string? observacion, string detalleJson)
        {
            try
            {
                var detalleItems = System.Text.Json.JsonSerializer.Deserialize<List<Dictionary<string, object>>>(detalleJson);
                if (detalleItems == null || detalleItems.Count == 0)
                    return Json(new { status = false, message = "Debe agregar al menos un activo al movimiento." });

                var anio = DateTime.Now.Year;
                var prefijo = tipoMovimiento == "ENTREGA" ? "ENT" : "DEV";
                var ultimoCodigo = await _context.MovimientoActivo
                    .Where(m => m.Codigo.StartsWith($"MOV-{prefijo}-{anio}"))
                    .OrderByDescending(m => m.Codigo).Select(m => m.Codigo).FirstOrDefaultAsync();

                int correlativo = 1;
                if (!string.IsNullOrEmpty(ultimoCodigo))
                {
                    var partes = ultimoCodigo.Split('-');
                    if (partes.Length >= 4 && int.TryParse(partes[3], out int num)) correlativo = num + 1;
                }
                var codigo = $"MOV-{prefijo}-{anio}-{correlativo:D4}";

                var movimiento = new MovimientoActivo
                {
                    Codigo = codigo,
                    TipoMovimiento = tipoMovimiento,
                    EmpresaId = empresaId,
                    PersonalId = personalId,
                    FechaMovimiento = fechaMovimiento,
                    Observacion = observacion,
                    Estado = true,
                    FechaRegistro = DateTime.Now
                };
                _context.MovimientoActivo.Add(movimiento);
                await _context.SaveChangesAsync();

                foreach (var item in detalleItems)
                {
                    var activoId = Convert.ToInt32(item["activoId"].ToString());
                    var ubicacion = item.ContainsKey("ubicacion") ? item["ubicacion"]?.ToString() : null;
                    var obs = item.ContainsKey("observacion") ? item["observacion"]?.ToString() : null;

                    _context.DMovimientoActivo.Add(new DMovimientoActivo
                    {
                        MovimientoActivoId = movimiento.Id,
                        ActivoId = activoId,
                        Ubicacion = ubicacion,
                        Observacion = obs,
                        Estado = true,
                        FechaRegistro = DateTime.Now
                    });

                    var activo = await _context.Activo.FirstOrDefaultAsync(a => a.Id == activoId && a.Estado);

                    if (activo != null)
                    {
                        if (movimiento.TipoMovimiento == "ENTREGA")
                        {
                            activo.EstadoUso = "ACTIVO";
                        }
                        else if (movimiento.TipoMovimiento == "DEVOLUCION")
                        {
                            activo.EstadoUso = "STOCK";
                        }
                    }

                }
                await _context.SaveChangesAsync();
                return Json(new { status = true, message = $"Movimiento {codigo} registrado correctamente.", codigo });
            }
            catch (Exception ex) { return Json(new { status = false, message = "Error: " + ex.Message }); }
        }

        // =====================================================================
        // MOVIMIENTO - ACTUALIZAR (solo si no tiene acta firmada)
        // =====================================================================

        [HttpPost]
        public async Task<JsonResult> ActualizarMovimiento(
            int id, string tipoMovimiento, int empresaId, int personalId,
            DateTime fechaMovimiento, string? observacion, string detalleJson)
        {
            try
            {
                var mov = await _context.MovimientoActivo.FirstOrDefaultAsync(m => m.Id == id && m.Estado);
                if (mov == null) return Json(new { status = false, message = "Movimiento no encontrado." });
                if (!string.IsNullOrEmpty(mov.RutaActa))
                    return Json(new { status = false, message = "No se puede editar un movimiento que ya tiene acta firmada cargada." });

                var detalleItems = System.Text.Json.JsonSerializer.Deserialize<List<Dictionary<string, object>>>(detalleJson);
                if (detalleItems == null || detalleItems.Count == 0)
                    return Json(new { status = false, message = "Debe agregar al menos un activo al movimiento." });

                // Revertir EstadoUso de activos del detalle anterior
                var detallesAnteriores = await _context.DMovimientoActivo
                    .Where(d => d.MovimientoActivoId == id && d.Estado).ToListAsync();
                foreach (var d in detallesAnteriores)
                {
                    d.Estado = false;
                    var activo = await _context.Activo.FirstOrDefaultAsync(a => a.Id == d.ActivoId && a.Estado);
                    if (activo != null)
                        activo.EstadoUso = mov.TipoMovimiento == "ENTREGA" ? "STOCK" : "ACTIVO";
                }

                // Actualizar cabecera
                mov.TipoMovimiento = tipoMovimiento;
                mov.EmpresaId = empresaId;
                mov.PersonalId = personalId;
                mov.FechaMovimiento = fechaMovimiento;
                mov.Observacion = observacion;

                await _context.SaveChangesAsync();

                // Agregar nuevos detalles y actualizar EstadoUso
                foreach (var item in detalleItems)
                {
                    var activoId = Convert.ToInt32(item["activoId"].ToString());
                    var ubicacion = item.ContainsKey("ubicacion") ? item["ubicacion"]?.ToString() : null;
                    var obs = item.ContainsKey("observacion") ? item["observacion"]?.ToString() : null;

                    _context.DMovimientoActivo.Add(new DMovimientoActivo
                    {
                        MovimientoActivoId = mov.Id,
                        ActivoId = activoId,
                        Ubicacion = ubicacion,
                        Observacion = obs,
                        Estado = true,
                        FechaRegistro = DateTime.Now
                    });

                    var activo = await _context.Activo.FirstOrDefaultAsync(a => a.Id == activoId && a.Estado);
                    if (activo != null)
                        activo.EstadoUso = tipoMovimiento == "ENTREGA" ? "ACTIVO" : "STOCK";
                }

                await _context.SaveChangesAsync();
                return Json(new { status = true, message = $"Movimiento {mov.Codigo} actualizado correctamente." });
            }
            catch (Exception ex) { return Json(new { status = false, message = "Error: " + ex.Message }); }
        }

        [HttpPost]
        public async Task<JsonResult> EliminarMovimiento(int id)
        {
            try
            {
                var mov = await _context.MovimientoActivo.FirstOrDefaultAsync(m => m.Id == id && m.Estado);
                if (mov == null) return Json(new { status = false, message = "Movimiento no encontrado." });
                mov.Estado = false;
                var detalles = await _context.DMovimientoActivo.Where(d => d.MovimientoActivoId == id && d.Estado).ToListAsync();
                foreach (var d in detalles) d.Estado = false;
                await _context.SaveChangesAsync();
                return Json(new { status = true, message = "Movimiento eliminado correctamente." });
            }
            catch (Exception ex) { return Json(new { status = false, message = "Error: " + ex.Message }); }
        }

        // =====================================================================
        // BUSCAR ACTIVOS PARA MOVIMIENTO (CORREGIDO)
        // =====================================================================

        [HttpGet]
        public async Task<JsonResult> BuscarActivosParaMovimiento(string? tipoCodigo, int empresaId, string? buscar)
        {
            try
            {
                var query = from a in _context.Activo
                            join t in _context.TipoActivo on a.TipoActivoId equals t.Id
                            //where a.EmpresaId == empresaId 
                            where a.Estado
                            select new { a, t };

                if (!string.IsNullOrWhiteSpace(tipoCodigo))
                    query = query.Where(x => x.t.Codigo == tipoCodigo);

                if (!string.IsNullOrWhiteSpace(buscar))
                {
                    buscar = buscar.ToLower();
                    query = query.Where(x =>
                        x.a.Codigo.ToLower().Contains(buscar) ||
                        (x.a.Marca != null && x.a.Marca.ToLower().Contains(buscar)) ||
                        (x.a.Modelo != null && x.a.Modelo.ToLower().Contains(buscar)) ||
                        (x.a.NumeroSerie != null && x.a.NumeroSerie.ToLower().Contains(buscar)) ||
                        (x.a.Placa != null && x.a.Placa.ToLower().Contains(buscar)));
                }

                var data = await query.OrderBy(x => x.a.Codigo)
                    .Select(x => new
                    {
                        x.a.Id,
                        x.a.Codigo,
                        Tipo = x.t.Nombre,
                        x.a.Marca,
                        x.a.Modelo,
                        Serie = x.a.NumeroSerie,
                        x.a.Placa,
                        x.a.Subtipo,
                        x.a.EstadoUso
                    }).Take(50).ToListAsync();
                return Json(new { status = true, data });
            }
            catch (Exception ex) { return Json(new { status = false, message = ex.Message }); }
        }

        [HttpGet]
        public async Task<JsonResult> BuscarActivosDisponibles(string? tipoCodigo, int empresaId, string? buscar)
        {
            return await BuscarActivosParaMovimiento(tipoCodigo, empresaId, buscar);
        }

        // =====================================================================
        // VEHÍCULOS - FICHA COMPLETA
        // =====================================================================

        [HttpGet]
        public async Task<JsonResult> GetVehiculoFicha(int activoId)
        {
            try
            {
                var activo = await (from a in _context.Activo
                                    join e in _context.Empresas on a.EmpresaId equals e.Id
                                    join g in _context.GrupoActivo on a.GrupoActivoId equals g.Id into gj
                                    from g in gj.DefaultIfEmpty()
                                    where a.Id == activoId && a.Estado
                                    select new
                                    {
                                        a.Id,
                                        a.Codigo,
                                        a.Marca,
                                        a.Modelo,
                                        a.Placa,
                                        a.AnioFabricacion,
                                        a.EstadoUso,
                                        a.Condicion,
                                        Empresa = e.Nombre,
                                        EmpresaRuc = e.Ruc,
                                        Grupo = g != null ? g.Nombre : ""
                                    }).FirstOrDefaultAsync();
                if (activo == null) return Json(new { status = false, message = "Vehículo no encontrado." });

                var especificaciones = await _context.ActivoDetalle
                    .Where(d => d.ActivoId == activoId && d.Estado).OrderBy(d => d.Orden)
                    .Select(d => new { d.Id, d.Clave, d.Valor }).ToListAsync();

                var gps = await _context.GpsVehiculo
                    .Where(g => g.ActivoId == activoId && g.Estado)
                    .Select(g => new
                    {
                        g.Id,
                        g.EmpresaGps,
                        g.UrlAcceso,
                        g.Usuario,
                        g.Contrasena,
                        FechaVencimiento = g.FechaVencimiento.HasValue ? g.FechaVencimiento.Value.ToString("yyyy-MM-dd") : "",
                        g.Constancia,
                        g.Endoso
                    }).ToListAsync();

                var mantenimientos = await _context.MantenimientoVehiculo
                    .Where(m => m.ActivoId == activoId && m.Estado).OrderByDescending(m => m.Fecha)
                    .Select(m => new
                    {
                        m.Id,
                        Fecha = m.Fecha.ToString("dd/MM/yyyy"),
                        FechaISO = m.Fecha.ToString("yyyy-MM-dd"),
                        m.TipoMantenimiento,
                        m.KmMantenimiento,
                        m.KmAlServicio,
                        m.TrabajosEjecutados,
                        m.Precio,
                        m.Moneda,
                        m.Conductor,
                        m.Observacion
                    }).ToListAsync();

                var infracciones = await _context.InfraccionVehiculo
                    .Where(i => i.ActivoId == activoId && i.Estado).OrderByDescending(i => i.FechaOcurrencia)
                    .Select(i => new
                    {
                        i.Id,
                        i.Entidad,
                        i.NroPapeleta,
                        FechaOcurrencia = i.FechaOcurrencia.HasValue ? i.FechaOcurrencia.Value.ToString("dd/MM/yyyy") : "",
                        FechaOcurrenciaISO = i.FechaOcurrencia.HasValue ? i.FechaOcurrencia.Value.ToString("yyyy-MM-dd") : "",
                        i.CodigoInfraccion,
                        i.DescripcionFalta,
                        i.ConductorDatos,
                        i.RucDniInfractor,
                        i.Importe,
                        i.SituacionPago
                    }).ToListAsync();

                var seguros = await _context.SeguroVehiculo
                    .Where(s => s.ActivoId == activoId && s.Estado).OrderByDescending(s => s.FechaVigencia)
                    .Select(s => new
                    {
                        s.Id,
                        s.Aseguradora,
                        s.NroPoliza,
                        s.SumaAsegurada,
                        s.MonedaSuma,
                        s.PrimaIgv,
                        s.Clase,
                        s.Uso,
                        FechaInicio = s.FechaInicio.HasValue ? s.FechaInicio.Value.ToString("yyyy-MM-dd") : "",
                        FechaVigencia = s.FechaVigencia.HasValue ? s.FechaVigencia.Value.ToString("yyyy-MM-dd") : "",
                        s.NroPolizaLaPositiva,
                        s.NroPolizaRimac,
                        s.AjusteRimac
                    }).ToListAsync();

                var bitacoraKm = await _context.BitacoraKilometraje
                    .Where(b => b.ActivoId == activoId && b.Estado).OrderByDescending(b => b.Fecha).Take(50)
                    .Select(b => new
                    {
                        b.Id,
                        Fecha = b.Fecha.ToString("dd/MM/yyyy"),
                        FechaISO = b.Fecha.ToString("yyyy-MM-dd"),
                        b.Kilometraje,
                        b.Observacion
                    }).ToListAsync();

                var documentos = await (from d in _context.ActivoDocumento
                                        join td in _context.TipoDocumentoActivo on d.TipoDocumentoActivoId equals td.Id
                                        where d.ActivoId == activoId && d.Estado
                                        orderby d.FechaVencimiento descending
                                        select new
                                        {
                                            d.Id,
                                            d.TipoDocumentoActivoId,
                                            TipoDocumento = td.Nombre,
                                            d.NumeroDocumento,
                                            FechaEmision = d.FechaEmision.HasValue ? d.FechaEmision.Value.ToString("yyyy-MM-dd") : "",
                                            FechaVencimiento = d.FechaVencimiento.HasValue ? d.FechaVencimiento.Value.ToString("yyyy-MM-dd") : "",
                                            d.RutaArchivo,
                                            d.Observacion
                                        }).ToListAsync();

                var asignacion = await (from dm in _context.DMovimientoActivo
                                        join m in _context.MovimientoActivo on dm.MovimientoActivoId equals m.Id
                                        join p in _context.Personal on m.PersonalId equals p.Id
                                        where dm.ActivoId == activoId && dm.Estado && m.Estado && m.TipoMovimiento == "ENTREGA"
                                        orderby m.FechaMovimiento descending
                                        select new
                                        {
                                            PersonalNombre = p.NombresCompletos,
                                            PersonalDni = p.Dni,
                                            FechaEntrega = m.FechaMovimiento.ToString("dd/MM/yyyy"),
                                            dm.Ubicacion
                                        }).FirstOrDefaultAsync();

                return Json(new { status = true, activo, especificaciones, gps, mantenimientos, infracciones, seguros, bitacoraKm, documentos, asignacion });
            }
            catch (Exception ex) { return Json(new { status = false, message = ex.Message }); }
        }

        // =====================================================================
        // MANTENIMIENTO DE VEHÍCULOS
        // =====================================================================

        [HttpPost]
        public async Task<JsonResult> GuardarMantenimiento(int id, int activoId, DateTime fecha, string tipoMantenimiento, decimal? kmMantenimiento, decimal? kmAlServicio, string? trabajosEjecutados, decimal? precio, string? moneda, string? conductor, string? observacion)
        {
            try
            {
                if (id == 0)
                {
                    _context.MantenimientoVehiculo.Add(new MantenimientoVehiculo { ActivoId = activoId, Fecha = fecha, TipoMantenimiento = tipoMantenimiento, KmMantenimiento = kmMantenimiento, KmAlServicio = kmAlServicio, TrabajosEjecutados = trabajosEjecutados, Precio = precio, Moneda = moneda ?? "PEN", Conductor = conductor, Observacion = observacion, Estado = true, FechaRegistro = DateTime.Now });
                    await _context.SaveChangesAsync();
                    return Json(new { status = true, message = "Mantenimiento registrado correctamente." });
                }
                else
                {
                    var mtto = await _context.MantenimientoVehiculo.FirstOrDefaultAsync(m => m.Id == id && m.Estado);
                    if (mtto == null) return Json(new { status = false, message = "Mantenimiento no encontrado." });
                    mtto.Fecha = fecha; mtto.TipoMantenimiento = tipoMantenimiento; mtto.KmMantenimiento = kmMantenimiento; mtto.KmAlServicio = kmAlServicio; mtto.TrabajosEjecutados = trabajosEjecutados; mtto.Precio = precio; mtto.Moneda = moneda ?? "PEN"; mtto.Conductor = conductor; mtto.Observacion = observacion;
                    await _context.SaveChangesAsync();
                    return Json(new { status = true, message = "Mantenimiento actualizado correctamente." });
                }
            }
            catch (Exception ex) { return Json(new { status = false, message = "Error: " + ex.Message }); }
        }

        [HttpPost]
        public async Task<JsonResult> EliminarMantenimiento(int id) { try { var e = await _context.MantenimientoVehiculo.FirstOrDefaultAsync(m => m.Id == id && m.Estado); if (e == null) return Json(new { status = false, message = "No encontrado." }); e.Estado = false; await _context.SaveChangesAsync(); return Json(new { status = true, message = "Mantenimiento eliminado." }); } catch (Exception ex) { return Json(new { status = false, message = ex.Message }); } }

        // =====================================================================
        // INFRACCIONES DE VEHÍCULOS
        // =====================================================================

        [HttpPost]
        public async Task<JsonResult> GuardarInfraccion(int id, int activoId, string entidad, string? nroPapeleta, DateTime? fechaOcurrencia, string? codigoInfraccion, string? descripcionFalta, string? conductorDatos, string? rucDniInfractor, decimal? importe, string? situacionPago)
        {
            try
            {
                if (id == 0)
                {
                    _context.InfraccionVehiculo.Add(new InfraccionVehiculo { ActivoId = activoId, Entidad = entidad, NroPapeleta = nroPapeleta, FechaOcurrencia = fechaOcurrencia, CodigoInfraccion = codigoInfraccion, DescripcionFalta = descripcionFalta, ConductorDatos = conductorDatos, RucDniInfractor = rucDniInfractor, Importe = importe, SituacionPago = situacionPago ?? "PENDIENTE DE PAGO", Estado = true, FechaRegistro = DateTime.Now });
                    await _context.SaveChangesAsync();
                    return Json(new { status = true, message = "Infracción registrada correctamente." });
                }
                else
                {
                    var infr = await _context.InfraccionVehiculo.FirstOrDefaultAsync(i => i.Id == id && i.Estado);
                    if (infr == null) return Json(new { status = false, message = "Infracción no encontrada." });
                    infr.Entidad = entidad; infr.NroPapeleta = nroPapeleta; infr.FechaOcurrencia = fechaOcurrencia; infr.CodigoInfraccion = codigoInfraccion; infr.DescripcionFalta = descripcionFalta; infr.ConductorDatos = conductorDatos; infr.RucDniInfractor = rucDniInfractor; infr.Importe = importe; infr.SituacionPago = situacionPago;
                    await _context.SaveChangesAsync();
                    return Json(new { status = true, message = "Infracción actualizada correctamente." });
                }
            }
            catch (Exception ex) { return Json(new { status = false, message = "Error: " + ex.Message }); }
        }

        [HttpPost]
        public async Task<JsonResult> EliminarInfraccion(int id) { try { var e = await _context.InfraccionVehiculo.FirstOrDefaultAsync(i => i.Id == id && i.Estado); if (e == null) return Json(new { status = false, message = "No encontrada." }); e.Estado = false; await _context.SaveChangesAsync(); return Json(new { status = true, message = "Infracción eliminada." }); } catch (Exception ex) { return Json(new { status = false, message = ex.Message }); } }

        // =====================================================================
        // SEGUROS, GPS, BITÁCORA KM
        // =====================================================================

        [HttpPost]
        public async Task<JsonResult> GuardarSeguro(int id, int activoId, string? aseguradora, string? nroPoliza, decimal? sumaAsegurada, string? monedaSuma, decimal? primaIgv, string? clase, string? uso, DateTime? fechaInicio, DateTime? fechaVigencia, string? nroPolizaLaPositiva, string? nroPolizaRimac, decimal? ajusteRimac)
        {
            try
            {
                if (id == 0)
                {
                    _context.SeguroVehiculo.Add(new SeguroVehiculo { ActivoId = activoId, Aseguradora = aseguradora, NroPoliza = nroPoliza, SumaAsegurada = sumaAsegurada, MonedaSuma = monedaSuma ?? "USD", PrimaIgv = primaIgv, Clase = clase, Uso = uso, FechaInicio = fechaInicio, FechaVigencia = fechaVigencia, NroPolizaLaPositiva = nroPolizaLaPositiva, NroPolizaRimac = nroPolizaRimac, AjusteRimac = ajusteRimac, Estado = true, FechaRegistro = DateTime.Now });
                    await _context.SaveChangesAsync();
                    return Json(new { status = true, message = "Seguro registrado correctamente." });
                }
                else
                {
                    var seg = await _context.SeguroVehiculo.FirstOrDefaultAsync(s => s.Id == id && s.Estado);
                    if (seg == null) return Json(new { status = false, message = "Seguro no encontrado." });
                    seg.Aseguradora = aseguradora; seg.NroPoliza = nroPoliza; seg.SumaAsegurada = sumaAsegurada; seg.MonedaSuma = monedaSuma ?? "USD"; seg.PrimaIgv = primaIgv; seg.Clase = clase; seg.Uso = uso; seg.FechaInicio = fechaInicio; seg.FechaVigencia = fechaVigencia; seg.NroPolizaLaPositiva = nroPolizaLaPositiva; seg.NroPolizaRimac = nroPolizaRimac; seg.AjusteRimac = ajusteRimac;
                    await _context.SaveChangesAsync();
                    return Json(new { status = true, message = "Seguro actualizado correctamente." });
                }
            }
            catch (Exception ex) { return Json(new { status = false, message = "Error: " + ex.Message }); }
        }

        [HttpPost]
        public async Task<JsonResult> EliminarSeguro(int id) { try { var e = await _context.SeguroVehiculo.FirstOrDefaultAsync(s => s.Id == id && s.Estado); if (e == null) return Json(new { status = false, message = "No encontrado." }); e.Estado = false; await _context.SaveChangesAsync(); return Json(new { status = true, message = "Seguro eliminado." }); } catch (Exception ex) { return Json(new { status = false, message = ex.Message }); } }

        [HttpPost]
        public async Task<JsonResult> GuardarGps(int id, int activoId, string? empresaGps, string? urlAcceso, string? usuario, string? contrasena, DateTime? fechaVencimiento, string? constancia, string? endoso)
        {
            try
            {
                if (id == 0)
                {
                    _context.GpsVehiculo.Add(new GpsVehiculo { ActivoId = activoId, EmpresaGps = empresaGps, UrlAcceso = urlAcceso, Usuario = usuario, Contrasena = contrasena, FechaVencimiento = fechaVencimiento, Constancia = constancia, Endoso = endoso, Estado = true, FechaRegistro = DateTime.Now });
                    await _context.SaveChangesAsync();
                    return Json(new { status = true, message = "GPS registrado correctamente." });
                }
                else
                {
                    var gps = await _context.GpsVehiculo.FirstOrDefaultAsync(g => g.Id == id && g.Estado);
                    if (gps == null) return Json(new { status = false, message = "GPS no encontrado." });
                    gps.EmpresaGps = empresaGps; gps.UrlAcceso = urlAcceso; gps.Usuario = usuario; gps.Contrasena = contrasena; gps.FechaVencimiento = fechaVencimiento; gps.Constancia = constancia; gps.Endoso = endoso;
                    await _context.SaveChangesAsync();
                    return Json(new { status = true, message = "GPS actualizado correctamente." });
                }
            }
            catch (Exception ex) { return Json(new { status = false, message = "Error: " + ex.Message }); }
        }

        [HttpPost]
        public async Task<JsonResult> EliminarGps(int id) { try { var e = await _context.GpsVehiculo.FirstOrDefaultAsync(g => g.Id == id && g.Estado); if (e == null) return Json(new { status = false, message = "No encontrado." }); e.Estado = false; await _context.SaveChangesAsync(); return Json(new { status = true, message = "GPS eliminado." }); } catch (Exception ex) { return Json(new { status = false, message = ex.Message }); } }

        [HttpPost]
        public async Task<JsonResult> GuardarBitacoraKm(int id, int activoId, DateTime fecha, decimal? kilometraje, string? observacion)
        {
            try
            {
                if (id == 0)
                {
                    _context.BitacoraKilometraje.Add(new BitacoraKilometraje { ActivoId = activoId, Fecha = fecha, Kilometraje = kilometraje, Observacion = observacion, Estado = true, FechaRegistro = DateTime.Now });
                    await _context.SaveChangesAsync();
                    return Json(new { status = true, message = "Kilometraje registrado correctamente." });
                }
                else
                {
                    var bk = await _context.BitacoraKilometraje.FirstOrDefaultAsync(b => b.Id == id && b.Estado);
                    if (bk == null) return Json(new { status = false, message = "Registro no encontrado." });
                    bk.Fecha = fecha; bk.Kilometraje = kilometraje; bk.Observacion = observacion;
                    await _context.SaveChangesAsync();
                    return Json(new { status = true, message = "Kilometraje actualizado correctamente." });
                }
            }
            catch (Exception ex) { return Json(new { status = false, message = "Error: " + ex.Message }); }
        }

        [HttpPost]
        public async Task<JsonResult> EliminarBitacoraKm(int id) { try { var e = await _context.BitacoraKilometraje.FirstOrDefaultAsync(b => b.Id == id && b.Estado); if (e == null) return Json(new { status = false, message = "No encontrado." }); e.Estado = false; await _context.SaveChangesAsync(); return Json(new { status = true, message = "Registro eliminado." }); } catch (Exception ex) { return Json(new { status = false, message = ex.Message }); } }

        // =====================================================================
        //  IMPRESIÓN DE ACTAS
        // =====================================================================

        [HttpGet]
        public IActionResult ActaImpresion(int id)
        {
            var esVehiculo = _context.DMovimientoActivo
                .Join(_context.Activo, d => d.ActivoId, a => a.Id, (d, a) => new { d, a })
                .Join(_context.TipoActivo, x => x.a.TipoActivoId, t => t.Id, (x, t) => new { x.d, t })
                .Any(j => j.d.MovimientoActivoId == id && j.t.Codigo == "VEHICULO" && j.d.Estado);

            ViewBag.IdMovimiento = id;
            return esVehiculo ? View("ActaImpresionVehiculo") : View("ActaImpresion");
        }

        [HttpGet]
        public async Task<JsonResult> GetDatosActa(int idMovimiento)
        {
            try
            {
                var mov = await (from m in _context.MovimientoActivo
                                 join p in _context.Personal on m.PersonalId equals p.Id
                                 join e in _context.Empresas on m.EmpresaId equals e.Id
                                 where m.Id == idMovimiento
                                 select new
                                 {
                                     m.Id,
                                     m.TipoMovimiento,
                                     m.FechaMovimiento,
                                     m.Codigo,
                                     EmpresaNombre = e.Nombre ?? "SIN EMPRESA",
                                     EmpresaRuc = e.Ruc ?? "",
                                     EmpresaDir = e.Direccion ?? "",
                                     PersonalNombre = p.NombresCompletos ?? "SIN NOMBRE",
                                     PersonalCargo = p.Cargo ?? "Colaborador",
                                     PersonalDni = p.Dni ?? "-"
                                 }).FirstOrDefaultAsync();

                if (mov == null) return Json(new { status = false, message = "Movimiento no encontrado" });

                var rawDetalles = await (from dm in _context.DMovimientoActivo
                                         join a in _context.Activo on dm.ActivoId equals a.Id
                                         join t in _context.TipoActivo on a.TipoActivoId equals t.Id
                                         where dm.MovimientoActivoId == idMovimiento && dm.Estado
                                         select new
                                         {
                                             a.Id,
                                             a.Codigo,
                                             Tipo = t.Nombre,
                                             TipoCodigo = t.Codigo,
                                             Marca = a.Marca ?? "",
                                             Modelo = a.Modelo ?? "",
                                             Serie = a.NumeroSerie ?? a.Placa ?? "S/N",
                                             Anio = a.AnioFabricacion,
                                             Subtipo = a.Subtipo ?? "",
                                             Descripcion = a.Descripcion ?? "",
                                             ObservacionDetalle = dm.Observacion ?? "",
                                             Condicion = a.Condicion ?? "REGULAR",
                                             Ubicacion = dm.Ubicacion ?? ""
                                         }).ToListAsync();

                var itemsProcesados = new List<object>();
                string ubicacionGeneral = rawDetalles.FirstOrDefault()?.Ubicacion ?? "";

                foreach (var item in rawDetalles)
                {
                    string caracteristicas = "";
                    var specs = await _context.ActivoDetalle
                        .Where(e => e.ActivoId == item.Id && e.Estado).OrderBy(e => e.Orden).ToListAsync();

                    if (item.TipoCodigo == "VEHICULO")
                    {
                        var color = specs.FirstOrDefault(x => x.Clave == "color")?.Valor ?? "";
                        var motor = specs.FirstOrDefault(x => x.Clave == "motor")?.Valor ?? "";
                        var chasis = specs.FirstOrDefault(x => x.Clave == "chasis_nro_vin")?.Valor ?? "";
                        caracteristicas = $"AÑO: {item.Anio} | COLOR: {color} | MOTOR: {motor} | VIN: {chasis}";
                    }
                    else
                    {
                        if (specs.Any())
                        {
                            var partes = specs.Select(s => { var label = s.Clave.Replace("_", " ").ToUpper(); return $"{label}: {s.Valor}"; });
                            caracteristicas = string.Join(" | ", partes);
                        }
                        else
                            caracteristicas = !string.IsNullOrEmpty(item.Descripcion) ? item.Descripcion : item.ObservacionDetalle;
                    }

                    itemsProcesados.Add(new
                    {
                        item.Codigo,
                        item.Tipo,
                        Equipo = !string.IsNullOrEmpty(item.Subtipo) ? $"{item.Subtipo} {item.Marca}" : $"{item.Tipo} {item.Marca}",
                        item.Modelo,
                        Serie = item.Serie,
                        Caracteristicas = caracteristicas,
                        item.Condicion,
                        item.Ubicacion
                    });
                }

                var razonSocial = _context.Empresas.Where(e => e.Id == EmpresaUsuarioId).Select(e => e.RazonSocial).FirstOrDefault();
                var usuario = _context.Usuarios.Where(u => u.Id == UsuarioActualId).FirstOrDefault();

                var datosEmpresa = new { nombre = usuario.Nombre ?? "ADMINISTRACIÓN", cargo = usuario.Cargo ?? "TI", empresa = razonSocial, dni = usuario.Dni ?? "" };
                var datosPersonal = new { nombre = mov.PersonalNombre, cargo = mov.PersonalCargo, empresa = mov.EmpresaNombre, dni = mov.PersonalDni };
                bool esEntrega = mov.TipoMovimiento == "ENTREGA";

                var data = new
                {
                    titulo = "ACTA DE " + mov.TipoMovimiento,
                    codigo = mov.Codigo,
                    fecha = mov.FechaMovimiento.ToString("dd 'de' MMMM 'del' yyyy"),
                    emisor = esEntrega ? datosEmpresa : datosPersonal,
                    receptor = esEntrega ? datosPersonal : datosEmpresa,
                    ubicacion = ubicacionGeneral,
                    items = itemsProcesados,
                    esVehiculo = rawDetalles.Any(x => x.TipoCodigo == "VEHICULO")
                };

                return Json(new { status = true, data });
            }
            catch (Exception ex) { return Json(new { status = false, message = ex.Message }); }
        }

        [HttpPost]
        public async Task<JsonResult> SubirActa(int id, IFormFile archivo)
        {
            try
            {
                var mov = await _context.MovimientoActivo.FindAsync(id);
                if (mov == null) return Json(new { status = false, message = "Movimiento no encontrado" });
                if (archivo == null || archivo.Length == 0) return Json(new { status = false, message = "Seleccione un archivo válido." });

                string folderPath = Path.Combine(_webHostEnvironment.WebRootPath, "uploads", "actas");
                if (!Directory.Exists(folderPath)) Directory.CreateDirectory(folderPath);
                string ext = Path.GetExtension(archivo.FileName);
                string fileName = $"Acta_{mov.Codigo}_{Guid.NewGuid()}{ext}";
                string filePath = Path.Combine(folderPath, fileName);
                using (var stream = new FileStream(filePath, FileMode.Create))
                    await archivo.CopyToAsync(stream);
                mov.RutaActa = "/uploads/actas/" + fileName;
                await _context.SaveChangesAsync();
                return Json(new { status = true, message = "Acta subida correctamente." });
            }
            catch (Exception ex) { return Json(new { status = false, message = "Error: " + ex.Message }); }
        }
        // =====================================================================
        //  DASHBOARD / REPORTE DE FLOTA VEHICULAR
        //  ---------------------------------------------------------------------
        //  Estos endpoints son adicionales al ActivoController existente.
        //  Péguelos DENTRO de la clase ActivoController (antes de la llave final).
        //  No modifican ninguna lógica actual.
        // =====================================================================

        // ---------------------- VISTA ----------------------
        public IActionResult ReporteVehiculos() => View();

        // ---------------------- KPIs / CARDS ---------------
        [HttpGet]
        public async Task<JsonResult> GetResumenFlota(int? empresaId)
        {
            try
            {
                var hoy = DateTime.Today;
                var inicioMes = new DateTime(hoy.Year, hoy.Month, 1);
                var en30Dias = hoy.AddDays(30);

                // Sólo vehículos
                var vehiculosQuery = from a in _context.Activo
                                     join t in _context.TipoActivo on a.TipoActivoId equals t.Id
                                     where t.Codigo == "VEHICULO" && a.Estado
                                     select a;
                if (empresaId.HasValue && empresaId > 0)
                    vehiculosQuery = vehiculosQuery.Where(a => a.EmpresaId == empresaId);

                var vehiculosIds = await vehiculosQuery.Select(a => a.Id).ToListAsync();

                int totalVehiculos = vehiculosIds.Count;
                int enUso = await vehiculosQuery.CountAsync(a => a.EstadoUso == "ACTIVO");
                int enStock = await vehiculosQuery.CountAsync(a => a.EstadoUso == "STOCK");

                // Documentos por vencer en 30 días
                int docsPorVencer = await _context.ActivoDocumento
                    .Where(d => d.Estado && vehiculosIds.Contains(d.ActivoId)
                                && d.FechaVencimiento.HasValue
                                && d.FechaVencimiento.Value >= hoy
                                && d.FechaVencimiento.Value <= en30Dias)
                    .CountAsync();

                int docsVencidos = await _context.ActivoDocumento
                    .Where(d => d.Estado && vehiculosIds.Contains(d.ActivoId)
                                && d.FechaVencimiento.HasValue
                                && d.FechaVencimiento.Value < hoy)
                    .CountAsync();

                // GPS por vencer / vencidos
                int gpsPorVencer = await _context.GpsVehiculo
                    .Where(g => g.Estado && vehiculosIds.Contains(g.ActivoId)
                                && g.FechaVencimiento.HasValue
                                && g.FechaVencimiento.Value >= hoy
                                && g.FechaVencimiento.Value <= en30Dias)
                    .CountAsync();

                // Seguros por vencer
                int segurosPorVencer = await _context.SeguroVehiculo
                    .Where(s => s.Estado && vehiculosIds.Contains(s.ActivoId)
                                && s.FechaVigencia.HasValue
                                && s.FechaVigencia.Value >= hoy
                                && s.FechaVigencia.Value <= en30Dias)
                    .CountAsync();

                // Mantenimientos del mes y gasto total
                var mantenimientosMes = await _context.MantenimientoVehiculo
                    .Where(m => m.Estado && vehiculosIds.Contains(m.ActivoId)
                                && m.Fecha >= inicioMes && m.Fecha <= hoy)
                    .ToListAsync();

                int totalMantenimientosMes = mantenimientosMes.Count;
                decimal gastoMantenimientosMes = mantenimientosMes
                    .Where(m => m.Precio.HasValue).Sum(m => m.Precio ?? 0);

                // Infracciones pendientes
                var infraccionesPendientes = await _context.InfraccionVehiculo
                    .Where(i => i.Estado && vehiculosIds.Contains(i.ActivoId)
                                && (i.SituacionPago == null || i.SituacionPago == "PENDIENTE DE PAGO"))
                    .ToListAsync();

                int totalInfraccionesPendientes = infraccionesPendientes.Count;
                decimal montoInfraccionesPendientes = infraccionesPendientes
                    .Where(i => i.Importe.HasValue).Sum(i => i.Importe ?? 0);

                return Json(new
                {
                    status = true,
                    data = new
                    {
                        totalVehiculos,
                        enUso,
                        enStock,
                        docsPorVencer,
                        docsVencidos,
                        gpsPorVencer,
                        segurosPorVencer,
                        totalMantenimientosMes,
                        gastoMantenimientosMes,
                        totalInfraccionesPendientes,
                        montoInfraccionesPendientes
                    }
                });
            }
            catch (Exception ex) { return Json(new { status = false, message = ex.Message }); }
        }

        // ---------------------- EVENTOS PARA CALENDARIO ----------------------
        // Devuelve eventos en el formato FullCalendar:
        // { id, title, start, color, extendedProps: { tipo, activoId, placa, ... } }
        [HttpGet]
        public async Task<JsonResult> GetEventosCalendario(
            int? empresaId, int? activoId, DateTime? desde, DateTime? hasta, string? tipos)
        {
            try
            {
                var fechaDesde = desde ?? DateTime.Today.AddMonths(-3);
                var fechaHasta = hasta ?? DateTime.Today.AddMonths(6);
                var tiposSet = string.IsNullOrWhiteSpace(tipos)
                    ? new HashSet<string> { "MANTENIMIENTO", "DOCUMENTO", "INFRACCION", "GPS", "SEGURO" }
                    : tipos.Split(',').Select(t => t.Trim().ToUpper()).ToHashSet();

                // Base: vehículos filtrados
                var vehiculosQuery = from a in _context.Activo
                                     join t in _context.TipoActivo on a.TipoActivoId equals t.Id
                                     join e in _context.Empresas on a.EmpresaId equals e.Id
                                     where t.Codigo == "VEHICULO" && a.Estado
                                     select new { a, e };

                if (empresaId.HasValue && empresaId > 0)
                    vehiculosQuery = vehiculosQuery.Where(x => x.a.EmpresaId == empresaId);
                if (activoId.HasValue && activoId > 0)
                    vehiculosQuery = vehiculosQuery.Where(x => x.a.Id == activoId);

                var vehiculos = await vehiculosQuery
                    .Select(x => new
                    {
                        x.a.Id,
                        x.a.Codigo,
                        x.a.Placa,
                        x.a.Marca,
                        x.a.Modelo,
                        Empresa = x.e.Nombre
                    }).ToListAsync();

                var vehIds = vehiculos.Select(v => v.Id).ToList();
                var vehDict = vehiculos.ToDictionary(v => v.Id, v => v);

                var eventos = new List<object>();

                // ------ 1) MANTENIMIENTOS ------
                if (tiposSet.Contains("MANTENIMIENTO"))
                {
                    var mtos = await _context.MantenimientoVehiculo
                        .Where(m => m.Estado && vehIds.Contains(m.ActivoId)
                                    && m.Fecha >= fechaDesde && m.Fecha <= fechaHasta)
                        .Select(m => new
                        {
                            m.Id,
                            m.ActivoId,
                            m.Fecha,
                            m.TipoMantenimiento,
                            m.TrabajosEjecutados,
                            m.Precio,
                            m.Moneda,
                            m.KmAlServicio,
                            m.Conductor
                        }).ToListAsync();

                    foreach (var m in mtos)
                    {
                        var v = vehDict.ContainsKey(m.ActivoId) ? vehDict[m.ActivoId] : null;
                        if (v == null) continue;
                        eventos.Add(new
                        {
                            id = $"MTO-{m.Id}",
                            title = $"🔧 {v.Placa} - {m.TipoMantenimiento}",
                            start = m.Fecha.ToString("yyyy-MM-dd"),
                            color = "#17a2b8", // info
                            allDay = true,
                            extendedProps = new
                            {
                                tipo = "MANTENIMIENTO",
                                activoId = m.ActivoId,
                                codigoVeh = v.Codigo,
                                placa = v.Placa,
                                marcaModelo = $"{v.Marca} {v.Modelo}",
                                empresa = v.Empresa,
                                subtitulo = m.TipoMantenimiento,
                                detalle = m.TrabajosEjecutados ?? "",
                                importe = m.Precio,
                                moneda = m.Moneda ?? "PEN",
                                kmServicio = m.KmAlServicio,
                                conductor = m.Conductor ?? ""
                            }
                        });
                    }
                }

                // ------ 2) DOCUMENTOS (por fecha de vencimiento) ------
                if (tiposSet.Contains("DOCUMENTO"))
                {
                    var docs = await (from d in _context.ActivoDocumento
                                      join td in _context.TipoDocumentoActivo on d.TipoDocumentoActivoId equals td.Id
                                      where d.Estado && vehIds.Contains(d.ActivoId)
                                            && d.FechaVencimiento.HasValue
                                            && d.FechaVencimiento.Value >= fechaDesde
                                            && d.FechaVencimiento.Value <= fechaHasta
                                      select new
                                      {
                                          d.Id,
                                          d.ActivoId,
                                          d.NumeroDocumento,
                                          d.FechaVencimiento,
                                          d.FechaEmision,
                                          TipoDocumento = td.Nombre,
                                          d.RutaArchivo,
                                          d.Observacion
                                      }).ToListAsync();

                    var hoy = DateTime.Today;
                    foreach (var d in docs)
                    {
                        var v = vehDict.ContainsKey(d.ActivoId) ? vehDict[d.ActivoId] : null;
                        if (v == null) continue;
                        var fv = d.FechaVencimiento!.Value;
                        string color = fv < hoy ? "#dc3545"                          // rojo: vencido
                                        : (fv - hoy).TotalDays <= 15 ? "#fd7e14"    // naranja: por vencer
                                        : "#ffc107";                                 // amarillo: próximo
                        eventos.Add(new
                        {
                            id = $"DOC-{d.Id}",
                            title = $"📄 {v.Placa} - {d.TipoDocumento}",
                            start = fv.ToString("yyyy-MM-dd"),
                            color,
                            allDay = true,
                            extendedProps = new
                            {
                                tipo = "DOCUMENTO",
                                activoId = d.ActivoId,
                                codigoVeh = v.Codigo,
                                placa = v.Placa,
                                marcaModelo = $"{v.Marca} {v.Modelo}",
                                empresa = v.Empresa,
                                subtitulo = d.TipoDocumento,
                                numero = d.NumeroDocumento ?? "",
                                fechaEmision = d.FechaEmision.HasValue ? d.FechaEmision.Value.ToString("dd/MM/yyyy") : "",
                                detalle = d.Observacion ?? "",
                                rutaArchivo = d.RutaArchivo ?? "",
                                diasRestantes = (fv - hoy).Days
                            }
                        });
                    }
                }

                // ------ 3) INFRACCIONES ------
                if (tiposSet.Contains("INFRACCION"))
                {
                    var infrs = await _context.InfraccionVehiculo
                        .Where(i => i.Estado && vehIds.Contains(i.ActivoId)
                                    && i.FechaOcurrencia.HasValue
                                    && i.FechaOcurrencia.Value >= fechaDesde
                                    && i.FechaOcurrencia.Value <= fechaHasta)
                        .Select(i => new
                        {
                            i.Id,
                            i.ActivoId,
                            i.Entidad,
                            i.NroPapeleta,
                            i.FechaOcurrencia,
                            i.CodigoInfraccion,
                            i.DescripcionFalta,
                            i.ConductorDatos,
                            i.Importe,
                            i.SituacionPago
                        }).ToListAsync();

                    foreach (var i in infrs)
                    {
                        var v = vehDict.ContainsKey(i.ActivoId) ? vehDict[i.ActivoId] : null;
                        if (v == null) continue;
                        string color = (i.SituacionPago == null || i.SituacionPago == "PENDIENTE DE PAGO")
                            ? "#dc3545" : "#6c757d";
                        eventos.Add(new
                        {
                            id = $"INF-{i.Id}",
                            title = $"🚨 {v.Placa} - {i.Entidad}",
                            start = i.FechaOcurrencia!.Value.ToString("yyyy-MM-dd"),
                            color,
                            allDay = true,
                            extendedProps = new
                            {
                                tipo = "INFRACCION",
                                activoId = i.ActivoId,
                                codigoVeh = v.Codigo,
                                placa = v.Placa,
                                marcaModelo = $"{v.Marca} {v.Modelo}",
                                empresa = v.Empresa,
                                subtitulo = $"{i.Entidad} · {i.NroPapeleta}",
                                detalle = i.DescripcionFalta ?? "",
                                codigoInfraccion = i.CodigoInfraccion ?? "",
                                conductor = i.ConductorDatos ?? "",
                                importe = i.Importe,
                                moneda = "PEN",
                                situacion = i.SituacionPago ?? "PENDIENTE DE PAGO"
                            }
                        });
                    }
                }

                // ------ 4) GPS (vencimiento) ------
                if (tiposSet.Contains("GPS"))
                {
                    var gpss = await _context.GpsVehiculo
                        .Where(g => g.Estado && vehIds.Contains(g.ActivoId)
                                    && g.FechaVencimiento.HasValue
                                    && g.FechaVencimiento.Value >= fechaDesde
                                    && g.FechaVencimiento.Value <= fechaHasta)
                        .Select(g => new
                        {
                            g.Id,
                            g.ActivoId,
                            g.EmpresaGps,
                            g.FechaVencimiento,
                            g.Constancia,
                            g.Endoso
                        }).ToListAsync();

                    var hoy = DateTime.Today;
                    foreach (var g in gpss)
                    {
                        var v = vehDict.ContainsKey(g.ActivoId) ? vehDict[g.ActivoId] : null;
                        if (v == null) continue;
                        var fv = g.FechaVencimiento!.Value;
                        string color = fv < hoy ? "#dc3545" : (fv - hoy).TotalDays <= 15 ? "#fd7e14" : "#20c997";
                        eventos.Add(new
                        {
                            id = $"GPS-{g.Id}",
                            title = $"📡 {v.Placa} - GPS {g.EmpresaGps}",
                            start = fv.ToString("yyyy-MM-dd"),
                            color,
                            allDay = true,
                            extendedProps = new
                            {
                                tipo = "GPS",
                                activoId = g.ActivoId,
                                codigoVeh = v.Codigo,
                                placa = v.Placa,
                                marcaModelo = $"{v.Marca} {v.Modelo}",
                                empresa = v.Empresa,
                                subtitulo = $"Vence GPS · {g.EmpresaGps}",
                                detalle = $"Constancia: {g.Constancia ?? "-"} · Endoso: {g.Endoso ?? "-"}",
                                diasRestantes = (fv - hoy).Days
                            }
                        });
                    }
                }

                // ------ 5) SEGUROS (vencimiento) ------
                if (tiposSet.Contains("SEGURO"))
                {
                    var segs = await _context.SeguroVehiculo
                        .Where(s => s.Estado && vehIds.Contains(s.ActivoId)
                                    && s.FechaVigencia.HasValue
                                    && s.FechaVigencia.Value >= fechaDesde
                                    && s.FechaVigencia.Value <= fechaHasta)
                        .Select(s => new
                        {
                            s.Id,
                            s.ActivoId,
                            s.Aseguradora,
                            s.NroPoliza,
                            s.SumaAsegurada,
                            s.MonedaSuma,
                            s.FechaVigencia
                        }).ToListAsync();

                    var hoy = DateTime.Today;
                    foreach (var s in segs)
                    {
                        var v = vehDict.ContainsKey(s.ActivoId) ? vehDict[s.ActivoId] : null;
                        if (v == null) continue;
                        var fv = s.FechaVigencia!.Value;
                        string color = fv < hoy ? "#dc3545" : (fv - hoy).TotalDays <= 30 ? "#fd7e14" : "#6610f2";
                        eventos.Add(new
                        {
                            id = $"SEG-{s.Id}",
                            title = $"🛡️ {v.Placa} - {s.Aseguradora}",
                            start = fv.ToString("yyyy-MM-dd"),
                            color,
                            allDay = true,
                            extendedProps = new
                            {
                                tipo = "SEGURO",
                                activoId = s.ActivoId,
                                codigoVeh = v.Codigo,
                                placa = v.Placa,
                                marcaModelo = $"{v.Marca} {v.Modelo}",
                                empresa = v.Empresa,
                                subtitulo = $"Vence póliza · {s.Aseguradora}",
                                detalle = $"Póliza: {s.NroPoliza ?? "-"} · Suma: {s.MonedaSuma} {s.SumaAsegurada:N2}",
                                diasRestantes = (fv - hoy).Days
                            }
                        });
                    }
                }

                // ------ 6) PROYECCIONES DE MANTENIMIENTO ------
                // ------ 6) PROYECCIONES DE MANTENIMIENTO (BASADO EN HISTORIAL REAL) ------
                if (tiposSet.Contains("PROYECCION"))
                {
                    // 1. Obtenemos la regla de cada vehículo (ej. cada 5000 km)
                    var specsMto = await _context.ActivoDetalle
                        .Where(d => d.Estado && d.Clave == "rango_km_mantenimiento" && vehIds.Contains(d.ActivoId))
                        .ToListAsync();

                    // 2. Traemos la bitácora para saber el KM actual y calcular la velocidad diaria
                    var bitacoras = await _context.BitacoraKilometraje
                        .Where(b => b.Estado && vehIds.Contains(b.ActivoId) && b.Kilometraje.HasValue)
                        .OrderBy(b => b.Fecha)
                        .Select(b => new { b.ActivoId, b.Fecha, b.Kilometraje })
                        .ToListAsync();

                    // 3. NUEVO: Traemos el ÚLTIMO mantenimiento registrado de cada vehículo
                    var ultimosMantenimientos = await _context.MantenimientoVehiculo
                        .Where(m => m.Estado && vehIds.Contains(m.ActivoId))
                        .GroupBy(m => m.ActivoId)
                        .Select(g => g.OrderByDescending(m => m.Fecha).FirstOrDefault())
                        .ToListAsync();

                    foreach (var v in vehiculos)
                    {
                        // A) Verificamos regla
                        var spec = specsMto.FirstOrDefault(s => s.ActivoId == v.Id);
                        if (spec == null || !decimal.TryParse(spec.Valor, out decimal rangoKm) || rangoKm <= 0)
                            continue;

                        // B) Verificamos bitácora
                        var vBitacoras = bitacoras.Where(b => b.ActivoId == v.Id).ToList();
                        if (!vBitacoras.Any()) continue;

                        var ultimoRegistro = vBitacoras.Last();
                        decimal kmActual = ultimoRegistro.Kilometraje.Value;

                        // C) NUEVO: Calcular la Meta basándonos en el historial
                        decimal proxMantenimientoKm;
                        var ultimoMto = ultimosMantenimientos.FirstOrDefault(m => m.ActivoId == v.Id);

                        if (ultimoMto != null && (ultimoMto.KmMantenimiento > 0 || ultimoMto.KmAlServicio > 0))
                        {
                            // Priorizamos el KM programado, si no existe usamos el KM real al que entró al taller
                            decimal baseKm = (ultimoMto.KmMantenimiento ?? 0) > 0
                                                ? ultimoMto.KmMantenimiento.Value
                                                : (ultimoMto.KmAlServicio ?? 0);

                            proxMantenimientoKm = baseKm + rangoKm;

                            // Seguridad: Si ya superó la fecha por falta de registro, calculamos el siguiente intervalo
                            while (proxMantenimientoKm <= kmActual)
                            {
                                proxMantenimientoKm += rangoKm;
                            }
                        }
                        else
                        {
                            // Si el vehículo es nuevo y NO tiene mantenimientos previos, usamos la predicción simple
                            proxMantenimientoKm = Math.Ceiling(kmActual / rangoKm) * rangoKm;
                            if (proxMantenimientoKm == kmActual) proxMantenimientoKm += rangoKm;
                        }

                        // D) Calculamos el promedio diario de recorrido
                        decimal kmPromedioDiario = 50; // Fallback para vehículos sin historial suficiente
                        if (vBitacoras.Count > 1)
                        {
                            var primerRegistro = vBitacoras.First();
                            var diasDiff = (ultimoRegistro.Fecha - primerRegistro.Fecha).TotalDays;
                            if (diasDiff > 0)
                            {
                                var kmDiff = kmActual - primerRegistro.Kilometraje.Value;
                                if (kmDiff > 0) kmPromedioDiario = (decimal)(kmDiff / (decimal)diasDiff);
                            }
                        }

                        // E) Proyectamos la fecha
                        decimal kmFaltantes = proxMantenimientoKm - kmActual;
                        int diasFaltantes = (int)Math.Ceiling(kmFaltantes / kmPromedioDiario);

                        DateTime fechaProyectada = ultimoRegistro.Fecha.AddDays(diasFaltantes);

                        if (fechaProyectada >= fechaDesde && fechaProyectada <= fechaHasta)
                        {
                            string msjBase = ultimoMto != null
                                ? $"Basado en su último mantenimiento a los {(ultimoMto.KmMantenimiento > 0 ? ultimoMto.KmMantenimiento : ultimoMto.KmAlServicio):N0} km"
                                : "Vehículo sin mantenimientos previos registrados";

                            eventos.Add(new
                            {
                                id = $"PROY-{v.Id}",
                                title = $"⚙️ {v.Placa} - Proy. {proxMantenimientoKm:N0} km",
                                start = fechaProyectada.ToString("yyyy-MM-dd"),
                                color = "#6f42c1",
                                allDay = true,
                                extendedProps = new
                                {
                                    tipo = "PROYECCION",
                                    activoId = v.Id,
                                    codigoVeh = v.Codigo,
                                    placa = v.Placa,
                                    marcaModelo = $"{v.Marca} {v.Modelo}",
                                    empresa = v.Empresa,
                                    subtitulo = $"Próximo Mto: {proxMantenimientoKm:N0} km",
                                    detalle = $"{msjBase}.<br>Al ritmo actual de <b>{kmPromedioDiario:N1} km/día</b>, se alcanzará este hito el {fechaProyectada:dd/MM/yyyy}.",
                                    diasRestantes = (fechaProyectada - DateTime.Today).Days
                                }
                            });
                        }
                    }
                }

                return Json(new { status = true, data = eventos });
            }
            catch (Exception ex) { return Json(new { status = false, message = ex.Message }); }
        }

        // ---------------------- PRÓXIMOS VENCIMIENTOS (timeline) ----------------------
        [HttpGet]
        public async Task<JsonResult> GetProximosVencimientos(int? empresaId, int dias = 30)
        {
            try
            {
                var hoy = DateTime.Today;
                var limite = hoy.AddDays(dias);

                var vehiculosQuery = from a in _context.Activo
                                     join t in _context.TipoActivo on a.TipoActivoId equals t.Id
                                     where t.Codigo == "VEHICULO" && a.Estado
                                     select a;
                if (empresaId.HasValue && empresaId > 0)
                    vehiculosQuery = vehiculosQuery.Where(a => a.EmpresaId == empresaId);
                var vehIds = await vehiculosQuery.Select(a => a.Id).ToListAsync();

                var docs = await (from d in _context.ActivoDocumento
                                  join td in _context.TipoDocumentoActivo on d.TipoDocumentoActivoId equals td.Id
                                  join a in _context.Activo on d.ActivoId equals a.Id
                                  where d.Estado && vehIds.Contains(d.ActivoId)
                                        && d.FechaVencimiento.HasValue
                                        && d.FechaVencimiento.Value <= limite
                                  select new
                                  {
                                      tipo = "DOCUMENTO",
                                      concepto = td.Nombre,
                                      placa = a.Placa,
                                      codigoVeh = a.Codigo,
                                      fecha = d.FechaVencimiento!.Value,
                                      numero = d.NumeroDocumento
                                  }).ToListAsync();

                var gpss = await (from g in _context.GpsVehiculo
                                  join a in _context.Activo on g.ActivoId equals a.Id
                                  where g.Estado && vehIds.Contains(g.ActivoId)
                                        && g.FechaVencimiento.HasValue
                                        && g.FechaVencimiento.Value <= limite
                                  select new
                                  {
                                      tipo = "GPS",
                                      concepto = "Vencimiento GPS " + (g.EmpresaGps ?? ""),
                                      placa = a.Placa,
                                      codigoVeh = a.Codigo,
                                      fecha = g.FechaVencimiento!.Value,
                                      numero = g.Constancia
                                  }).ToListAsync();

                var segs = await (from s in _context.SeguroVehiculo
                                  join a in _context.Activo on s.ActivoId equals a.Id
                                  where s.Estado && vehIds.Contains(s.ActivoId)
                                        && s.FechaVigencia.HasValue
                                        && s.FechaVigencia.Value <= limite
                                  select new
                                  {
                                      tipo = "SEGURO",
                                      concepto = "Póliza " + (s.Aseguradora ?? ""),
                                      placa = a.Placa,
                                      codigoVeh = a.Codigo,
                                      fecha = s.FechaVigencia!.Value,
                                      numero = s.NroPoliza
                                  }).ToListAsync();

                var todo = docs.Concat(gpss).Concat(segs)
                    .OrderBy(x => x.fecha)
                    .Select(x => new
                    {
                        x.tipo,
                        x.concepto,
                        x.placa,
                        x.codigoVeh,
                        fecha = x.fecha.ToString("yyyy-MM-dd"),
                        fechaFmt = x.fecha.ToString("dd/MM/yyyy"),
                        diasRestantes = (x.fecha - hoy).Days,
                        x.numero,
                        estado = x.fecha < hoy ? "VENCIDO"
                                 : (x.fecha - hoy).TotalDays <= 7 ? "CRITICO"
                                 : (x.fecha - hoy).TotalDays <= 15 ? "URGENTE"
                                 : "PROXIMO"
                    }).ToList();

                return Json(new { status = true, data = todo });
            }
            catch (Exception ex) { return Json(new { status = false, message = ex.Message }); }
        }

        // ---------------------- GRÁFICO: MANTENIMIENTOS POR MES ----------------------
        [HttpGet]
        public async Task<JsonResult> GetMantenimientosPorMes(int? empresaId, int meses = 12)
        {
            try
            {
                var hoy = DateTime.Today;
                var inicio = new DateTime(hoy.Year, hoy.Month, 1).AddMonths(-(meses - 1));

                var vehIdsQuery = from a in _context.Activo
                                  join t in _context.TipoActivo on a.TipoActivoId equals t.Id
                                  where t.Codigo == "VEHICULO" && a.Estado
                                  select a;
                if (empresaId.HasValue && empresaId > 0)
                    vehIdsQuery = vehIdsQuery.Where(a => a.EmpresaId == empresaId);
                var vehIds = await vehIdsQuery.Select(a => a.Id).ToListAsync();

                var raw = await _context.MantenimientoVehiculo
                    .Where(m => m.Estado && vehIds.Contains(m.ActivoId) && m.Fecha >= inicio)
                    .Select(m => new { m.Fecha, m.Precio, m.TipoMantenimiento })
                    .ToListAsync();

                // Agrupamos en memoria
                var porMes = Enumerable.Range(0, meses).Select(i =>
                {
                    var mesRef = inicio.AddMonths(i);
                    var items = raw.Where(r => r.Fecha.Year == mesRef.Year && r.Fecha.Month == mesRef.Month).ToList();
                    return new
                    {
                        label = mesRef.ToString("MMM yy", new System.Globalization.CultureInfo("es-PE")),
                        mes = mesRef.ToString("yyyy-MM"),
                        cantidad = items.Count,
                        gasto = items.Sum(x => x.Precio ?? 0m)
                    };
                }).ToList();

                var porTipo = raw.GroupBy(r => r.TipoMantenimiento ?? "SIN TIPO")
                    .Select(g => new { tipo = g.Key, cantidad = g.Count(), gasto = g.Sum(x => x.Precio ?? 0m) })
                    .OrderByDescending(x => x.cantidad).ToList();

                return Json(new { status = true, porMes, porTipo });
            }
            catch (Exception ex) { return Json(new { status = false, message = ex.Message }); }
        }

        // ---------------------- GRÁFICO: KILOMETRAJE MENSUAL POR VEHÍCULO ----------------------
        [HttpGet]
        public async Task<JsonResult> GetKilometrajeMensual(int? empresaId, int? activoId, int meses = 6)
        {
            try
            {
                var hoy = DateTime.Today;
                var inicio = new DateTime(hoy.Year, hoy.Month, 1).AddMonths(-(meses - 1));

                var vehQuery = from a in _context.Activo
                               join t in _context.TipoActivo on a.TipoActivoId equals t.Id
                               where t.Codigo == "VEHICULO" && a.Estado
                               select a;
                if (empresaId.HasValue && empresaId > 0)
                    vehQuery = vehQuery.Where(a => a.EmpresaId == empresaId);
                if (activoId.HasValue && activoId > 0)
                    vehQuery = vehQuery.Where(a => a.Id == activoId);

                var vehs = await vehQuery.Select(a => new { a.Id, a.Placa, a.Codigo }).ToListAsync();
                var vehIds = vehs.Select(v => v.Id).ToList();

                var regs = await _context.BitacoraKilometraje
                    .Where(b => b.Estado && vehIds.Contains(b.ActivoId) && b.Fecha >= inicio
                                && b.Kilometraje.HasValue)
                    .Select(b => new { b.ActivoId, b.Fecha, b.Kilometraje })
                    .ToListAsync();

                // Etiquetas de meses
                var labels = Enumerable.Range(0, meses)
                    .Select(i => inicio.AddMonths(i).ToString("MMM yy", new System.Globalization.CultureInfo("es-PE")))
                    .ToList();

                // Por cada vehículo: km recorrido por mes = max(mes) - max(mes-1)
                var datasets = new List<object>();
                foreach (var v in vehs)
                {
                    var maxPorMes = new decimal?[meses];
                    for (int i = 0; i < meses; i++)
                    {
                        var mesRef = inicio.AddMonths(i);
                        var ultimo = regs.Where(r => r.ActivoId == v.Id
                                                     && r.Fecha.Year == mesRef.Year
                                                     && r.Fecha.Month == mesRef.Month)
                                         .OrderByDescending(r => r.Fecha)
                                         .Select(r => r.Kilometraje)
                                         .FirstOrDefault();
                        maxPorMes[i] = ultimo;
                    }

                    var data = new decimal?[meses];
                    decimal? ultimoConocido = null;
                    for (int i = 0; i < meses; i++)
                    {
                        if (maxPorMes[i].HasValue && ultimoConocido.HasValue)
                            data[i] = maxPorMes[i] - ultimoConocido;
                        else
                            data[i] = 0;
                        if (maxPorMes[i].HasValue) ultimoConocido = maxPorMes[i];
                    }

                    // sólo incluir si hubo algún registro
                    if (data.Any(d => d > 0))
                    {
                        datasets.Add(new { label = v.Placa ?? v.Codigo, data });
                    }
                }

                return Json(new { status = true, labels, datasets });
            }
            catch (Exception ex) { return Json(new { status = false, message = ex.Message }); }
        }

        // ---------------------- INFRACCIONES PENDIENTES (tabla) ----------------------
        [HttpGet]
        public async Task<JsonResult> GetInfraccionesPendientes(int? empresaId)
        {
            try
            {
                var vehQuery = from a in _context.Activo
                               join t in _context.TipoActivo on a.TipoActivoId equals t.Id
                               where t.Codigo == "VEHICULO" && a.Estado
                               select a;
                if (empresaId.HasValue && empresaId > 0)
                    vehQuery = vehQuery.Where(a => a.EmpresaId == empresaId);
                var vehIds = await vehQuery.Select(a => a.Id).ToListAsync();

                var data = await (from i in _context.InfraccionVehiculo
                                  join a in _context.Activo on i.ActivoId equals a.Id
                                  where i.Estado && vehIds.Contains(i.ActivoId)
                                        && (i.SituacionPago == null || i.SituacionPago == "PENDIENTE DE PAGO")
                                  orderby i.FechaOcurrencia descending
                                  select new
                                  {
                                      i.Id,
                                      placa = a.Placa,
                                      codigoVeh = a.Codigo,
                                      i.Entidad,
                                      i.NroPapeleta,
                                      FechaOcurrencia = i.FechaOcurrencia.HasValue
                                            ? i.FechaOcurrencia.Value.ToString("dd/MM/yyyy") : "",
                                      i.CodigoInfraccion,
                                      i.DescripcionFalta,
                                      i.ConductorDatos,
                                      i.Importe,
                                      i.SituacionPago
                                  }).ToListAsync();

                return Json(new { status = true, data });
            }
            catch (Exception ex) { return Json(new { status = false, message = ex.Message }); }
        }

        // ---------------------- COMBO DE VEHÍCULOS (para filtro) ----------------------
        [HttpGet]
        public async Task<JsonResult> GetVehiculosCombo(int? empresaId)
        {
            try
            {
                var q = from a in _context.Activo
                        join t in _context.TipoActivo on a.TipoActivoId equals t.Id
                        where t.Codigo == "VEHICULO" && a.Estado
                        select a;
                if (empresaId.HasValue && empresaId > 0)
                    q = q.Where(a => a.EmpresaId == empresaId);
                var data = await q.OrderBy(a => a.Placa)
                    .Select(a => new { a.Id, a.Codigo, a.Placa, a.Marca, a.Modelo })
                    .ToListAsync();
                return Json(new { status = true, data });
            }
            catch (Exception ex) { return Json(new { status = false, message = ex.Message }); }
        }
    }
}