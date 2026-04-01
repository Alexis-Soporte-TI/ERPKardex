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

                var data = await query.OrderByDescending(x => x.a.Id)
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
                }
                await _context.SaveChangesAsync();
                return Json(new { status = true, message = $"Movimiento {codigo} registrado correctamente.", codigo });
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
    }
}