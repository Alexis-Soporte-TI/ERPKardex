using ERPKardex.Data;
using ERPKardex.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;

namespace ERPKardex.Controllers
{
    // 1. HERENCIA
    public class PedServicioController : BaseController
    {
        private readonly ApplicationDbContext _context;

        public PedServicioController(ApplicationDbContext context)
        {
            _context = context;
        }

        public bool EsPeriodoValido(int periodoId)
        {
            return _context.PeriodosContables.Any(p => p.Id == periodoId && p.Estado == true);
        }

        #region VISTAS
        public IActionResult Index() => View();
        public IActionResult Registrar() => View();
        #endregion

        #region LISTADOS (GET)

        [HttpGet]
        public async Task<JsonResult> GetPedidosServicioData()
        {
            try
            {
                var miEmpresaId = EmpresaUsuarioId;
                var esGlobal = EsAdminGlobal;

                var query = from p in _context.PedServicios
                            join u in _context.Usuarios on p.UsuarioSolicitanteId equals u.Id
                            join e in _context.Estados on p.EstadoId equals e.Id
                            join emp in _context.Empresas on p.EmpresaId equals emp.Id
                            select new
                            {
                                p.Id,
                                p.EmpresaId,
                                Empresa = emp.Nombre,
                                emp.RazonSocial,
                                p.Numero,
                                FechaEmision = p.FechaEmision.GetValueOrDefault().ToString("dd/MM/yyyy HH:mm"),
                                FechaNecesaria = p.FechaNecesaria.GetValueOrDefault().ToString("dd/MM/yyyy"),
                                Solicitante = u.Nombre,
                                Estado = e.Nombre,
                                // Subconsulta CCs
                                CentrosInvolucrados = (from d in _context.DPedidosServicio
                                                       join cc in _context.CentroCostos on d.CentroCostoId equals cc.Id
                                                       where d.PedidoServicioId == p.Id
                                                       select cc.Codigo).Distinct().ToList()
                            };

                // Filtro Seguridad
                query = query.Where(x => x.EmpresaId == miEmpresaId);
                //if (!esGlobal)
                //{
                //    query = query.Where(x => x.EmpresaId == miEmpresaId);
                //}

                var listaRaw = await query.OrderByDescending(x => x.Numero).ToListAsync();

                var data = listaRaw.Select(x => new
                {
                    x.Id,
                    x.Numero,
                    x.FechaEmision,
                    x.FechaNecesaria,
                    CentroCosto = x.CentrosInvolucrados.Any() ? string.Join(", ", x.CentrosInvolucrados) : "N/A",
                    x.Empresa,
                    x.RazonSocial,
                    x.Solicitante,
                    x.Estado
                });

                return Json(new { status = true, data });
            }
            catch (Exception ex) { return Json(new { status = false, message = "Error: " + ex.Message }); }
        }

        [HttpGet]
        public JsonResult GetDetallePedido(int id)
        {
            try
            {
                var detalles = (from d in _context.DPedidosServicio
                                join cc in _context.CentroCostos on d.CentroCostoId equals cc.Id
                                join est in _context.Estados on d.EstadoId equals est.Id
                                where d.PedidoServicioId == id
                                select new
                                {
                                    d.Item,
                                    Descripcion = d.DescripcionServicio,
                                    d.UnidadMedida,
                                    Cantidad = d.Cantidad,
                                    CantidadAtendida = d.CantidadAtendida ?? 0,
                                    CentroCosto = cc.Codigo,
                                    Estado = est.Nombre,
                                    d.Lugar, // Mostramos lugar en el modal
                                    Ref = d.TablaReferencia == "DREQSERVICIO" ? ("Req. " + d.ItemReferencia) : "-"
                                }).OrderBy(x => x.Item).ToList();

                return Json(new { status = true, data = detalles });
            }
            catch (Exception ex) { return Json(new { status = false, message = ex.Message }); }
        }

        #endregion

        #region CARGA DE REQUERIMIENTOS (LÓGICA FILTRADA)

        [HttpGet]
        public JsonResult GetRequerimientosAprobados(int? empresaDestinoId)
        {
            try
            {
                var miEmpresaId = EmpresaUsuarioId;
                var esGlobal = EsAdminGlobal;
                int idParaFiltrar = esGlobal ? (empresaDestinoId ?? 0) : miEmpresaId;
                // POR AHORA
                idParaFiltrar = miEmpresaId;

                if (idParaFiltrar == 0) return Json(new { status = false, message = "Seleccione una empresa." });

                // ESTADOS CLAVE
                var estAprobadoREQ = _context.Estados.FirstOrDefault(e => e.Nombre == "Aprobado" && e.Tabla == "REQ")?.Id ?? 0;
                var estParcialREQ = _context.Estados.FirstOrDefault(e => e.Nombre == "Atendido Parcial" && e.Tabla == "REQ")?.Id ?? 0;
                var estPendienteDREQ = _context.Estados.FirstOrDefault(e => e.Nombre == "Pendiente" && e.Tabla == "DREQ")?.Id ?? 0;

                // Lógica: REQ (Aprobado o Parcial) con HIJOS Pendientes
                var data = (from r in _context.ReqServicios
                            join u in _context.Usuarios on r.UsuarioSolicitanteId equals u.Id
                            where r.EmpresaId == idParaFiltrar
                               && (r.EstadoId == estAprobadoREQ || r.EstadoId == estParcialREQ)
                               && _context.DReqServicios.Any(dr => dr.ReqServicioId == r.Id && dr.EstadoId == estPendienteDREQ)
                            orderby r.FechaRegistro descending
                            select new
                            {
                                r.Id,
                                r.Numero,
                                Fecha = r.FechaEmision.GetValueOrDefault().ToString("yyyy-MM-dd HH:mm"),
                                FechaNecesaria = r.FechaNecesaria.GetValueOrDefault().ToString("yyyy-MM-dd"),
                                Solicitante = u.Nombre,
                                r.Observacion
                            }).ToList();

                return Json(new { status = true, data });
            }
            catch (Exception ex) { return Json(new { status = false, message = ex.Message }); }
        }

        [HttpGet]
        public JsonResult GetDetallesDeUnReq(int reqId)
        {
            try
            {
                var estPendienteDREQ = _context.Estados.FirstOrDefault(e => e.Nombre == "Pendiente" && e.Tabla == "DREQ")?.Id ?? 0;

                var detalles = (from d in _context.DReqServicios
                                join cc in _context.CentroCostos on d.CentroCostoId equals cc.Id
                                where d.ReqServicioId == reqId && d.EstadoId == estPendienteDREQ // SOLO PENDIENTES
                                select new
                                {
                                    d.Item,
                                    d.DescripcionServicio,
                                    d.CantidadSolicitada,
                                    d.Lugar,
                                    CentroCosto = cc.Codigo,
                                    OrdenProduccion = d.IdOrdenProduccionErpCorpsaf != null ? d.DescOrdenProduccionErpCorpsaf : "",
                                }).ToList();
                return Json(new { status = true, data = detalles });
            }
            catch (Exception ex) { return Json(new { status = false, message = ex.Message }); }
        }

        [HttpPost]
        public JsonResult GetDetallesBatch([FromBody] List<int> reqIds)
        {
            try
            {
                if (reqIds == null || reqIds.Count == 0) return Json(new { status = false, message = "Ningún requerimiento seleccionado" });

                var estPendienteDREQ = _context.Estados.FirstOrDefault(e => e.Nombre == "Pendiente" && e.Tabla == "DREQ")?.Id ?? 0;

                var detalles = (from d in _context.DReqServicios
                                join cc in _context.CentroCostos on d.CentroCostoId equals cc.Id
                                where reqIds.Contains(d.ReqServicioId.GetValueOrDefault())
                                   && d.EstadoId == estPendienteDREQ // FILTRO CRÍTICO
                                select new
                                {
                                    ProductoId = d.ProductoId,
                                    DescripcionServicio = d.DescripcionServicio,
                                    UnidadMedida = d.UnidadMedida ?? "UND",
                                    Cantidad = d.CantidadSolicitada,
                                    CentroCostoId = d.CentroCostoId,
                                    NombreCentroCosto = cc.Codigo + " - " + cc.Nombre,
                                    Lugar = d.Lugar,
                                    d.IdOrdenProduccionErpCorpsaf,
                                    d.DescOrdenProduccionErpCorpsaf,

                                    // Referencias
                                    ReqOrigenId = d.ReqServicioId,
                                    IdReferencia = d.Id,
                                    ItemReferencia = d.Item
                                }).ToList();

                return Json(new { status = true, data = detalles });
            }
            catch (Exception ex) { return Json(new { status = false, message = ex.Message }); }
        }

        #endregion

        #region 3. GUARDAR PEDIDO (TRANSACCIÓN COMPLEJA)

        [HttpPost]
        public JsonResult GuardarPedido(PedServicio cabecera, string detallesJson)
        {
            using (var transaction = _context.Database.BeginTransaction())
            {
                try
                {
                    if (!EsPeriodoValido(cabecera.PeriodoContableId ?? 0))
                    {
                        throw new Exception("El periodo seleccionado ya no se encuentra abierto o es inválido. Por favor, actualice la página o seleccione otro periodo.");
                    }

                    int usuarioId = UsuarioActualId;
                    int empresaId = cabecera.EmpresaId.GetValueOrDefault();
                    if (empresaId == 0) throw new Exception("Empresa inválida.");

                    // 1. CARGAR ESTADOS
                    var estGeneradoPED = _context.Estados.FirstOrDefault(e => e.Nombre == "Generado" && e.Tabla == "PED");
                    var estAtendidoDREQ = _context.Estados.FirstOrDefault(e => e.Nombre == "Atendido" && e.Tabla == "DREQ");
                    var estPendienteDREQ = _context.Estados.FirstOrDefault(e => e.Nombre == "Pendiente" && e.Tabla == "DREQ");
                    var estParcialREQ = _context.Estados.FirstOrDefault(e => e.Nombre == "Atendido Parcial" && e.Tabla == "REQ");
                    var estTotalREQ = _context.Estados.FirstOrDefault(e => e.Nombre == "Atendido Total" && e.Tabla == "REQ");
                    var estPendienteDPED = _context.Estados.FirstOrDefault(e => e.Nombre == "Pendiente" && e.Tabla == "DPED");
                    var tipoDoc = _context.TiposDocumentoInterno.FirstOrDefault(t => t.Codigo == "PS");

                    if (estGeneradoPED == null || estAtendidoDREQ == null || estParcialREQ == null || estTotalREQ == null || tipoDoc == null || estPendienteDPED == null)
                        throw new Exception("Falta configuración.");

                    // 2. CORRELATIVO
                    var ultimo = _context.PedServicios
                        .Where(x => x.EmpresaId == empresaId && x.TipoDocumentoInternoId == tipoDoc.Id)
                        .OrderByDescending(x => x.Numero)
                        .Select(x => x.Numero).FirstOrDefault();

                    int nro = 1;
                    if (!string.IsNullOrEmpty(ultimo))
                    {
                        var partes = ultimo.Split('-');
                        if (partes.Length > 1 && int.TryParse(partes[1], out int val)) nro = val + 1;
                    }

                    // 3. CABECERA
                    cabecera.Numero = $"PS-{nro.ToString("D10")}";
                    cabecera.TipoDocumentoInternoId = tipoDoc.Id;
                    cabecera.UsuarioSolicitanteId = usuarioId;
                    cabecera.UsuarioRegistro = usuarioId;
                    cabecera.FechaRegistro = DateTime.Now;
                    cabecera.EstadoId = estGeneradoPED.Id;
                    //cabecera.FechaEmision = DateTime.Now;
                    if (cabecera.FechaNecesaria == DateTime.MinValue) cabecera.FechaNecesaria = DateTime.Now;

                    _context.PedServicios.Add(cabecera);
                    _context.SaveChanges();

                    // 4. DETALLES Y ACTUALIZACIÓN REQS
                    List<int> reqInvolucrados = new List<int>();

                    if (!string.IsNullOrEmpty(detallesJson))
                    {
                        var lista = JsonConvert.DeserializeObject<List<DPedServicio>>(detallesJson);
                        int item = 1;

                        foreach (var det in lista)
                        {
                            // Guardar en Pedido
                            det.Id = 0;
                            det.PedidoServicioId = cabecera.Id;
                            det.EmpresaId = empresaId;
                            det.Item = item.ToString("D3");
                            det.CantidadAtendida = 0;
                            det.EstadoId = estPendienteDPED.Id;

                            _context.DPedidosServicio.Add(det);

                            // Actualizar Requerimiento Origen
                            if (det.IdReferencia != null && det.IdReferencia > 0)
                            {
                                det.TablaReferencia = "DREQSERVICIO";
                                var dReq = _context.DReqServicios.Find(det.IdReferencia);
                                if (dReq != null)
                                {
                                    // CAMBIO CRÍTICO: ÍTEM A ATENDIDO
                                    dReq.EstadoId = estAtendidoDREQ.Id;

                                    if (dReq.ReqServicioId.HasValue && !reqInvolucrados.Contains(dReq.ReqServicioId.Value))
                                    {
                                        reqInvolucrados.Add(dReq.ReqServicioId.Value);
                                    }
                                }
                            }
                            item++;
                        }
                        _context.SaveChanges();
                    }

                    // 5. RECALCULAR CABECERAS DE REQUERIMIENTOS
                    foreach (var reqId in reqInvolucrados)
                    {
                        // Verificamos si queda algún hijo pendiente
                        bool quedanPendientes = _context.DReqServicios
                            .Any(dr => dr.ReqServicioId == reqId && dr.EstadoId == estPendienteDREQ.Id);

                        var reqCabecera = _context.ReqServicios.Find(reqId);
                        if (reqCabecera != null)
                        {
                            if (quedanPendientes)
                                reqCabecera.EstadoId = estParcialREQ.Id;
                            else
                                reqCabecera.EstadoId = estTotalREQ.Id;
                        }
                    }
                    _context.SaveChanges();

                    transaction.Commit();
                    return Json(new { status = true, message = $"Pedido {cabecera.Numero} generado correctamente." });
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    return Json(new { status = false, message = "Error: " + ex.Message });
                }
            }
        }

        #endregion

        #region COMBOS
        [HttpGet]
        public JsonResult GetEmpresaData()
        {
            var data = _context.Empresas.Where(x => x.Estado == true).Select(x => new { x.Id, x.Ruc, x.RazonSocial }).ToList();
            return Json(new { status = true, data });
        }

        [HttpGet]
        public JsonResult GetDatosDefaultEmpresa(int empresaIdSeleccionada)
        {
            try
            {
                var sucursal = _context.Sucursales
                    .FirstOrDefault(s => s.EmpresaId == empresaIdSeleccionada && s.Estado == true && (s.Codigo == "001" || s.Nombre.Contains("PRINCIPAL")));

                int? sucursalId = sucursal?.Id;
                string sucursalNombre = sucursal?.Nombre ?? "";

                // En servicios el almacén es opcional o a veces no aplica, pero lo enviamos si existe por defecto
                int? almacenId = null;
                string almacenNombre = "";
                if (sucursalId != null)
                {
                    var almacen = _context.Almacenes.FirstOrDefault(a => a.EmpresaId == empresaIdSeleccionada && a.SucursalId == sucursalId && a.Estado == true && (a.Codigo == "01" || a.Nombre.Contains("PRINCIPAL")));
                    almacenId = almacen?.Id;
                    almacenNombre = almacen?.Nombre ?? "";
                }

                return Json(new { status = true, sucursalId, sucursalNombre, almacenId, almacenNombre });
            }
            catch (Exception ex) { return Json(new { status = false, message = ex.Message }); }
        }
        #endregion
        #region IMPRESIÓN
        [HttpGet]
        public async Task<IActionResult> Imprimir(int id)
        {
            try
            {
                // 1. OBTENER CABECERA
                var dataCabecera = await (from p in _context.PedServicios
                                          join e in _context.Empresas on p.EmpresaId equals e.Id
                                          // Tomamos al usuario que consolidó/registró el pedido de servicio
                                          join u in _context.Usuarios on p.UsuarioRegistro equals u.Id
                                          join est in _context.Estados on p.EstadoId equals est.Id
                                          where p.Id == id
                                          select new
                                          {
                                              Ped = p,
                                              Emp = e,
                                              Usu = u,
                                              NombreEstado = est.Nombre
                                          }).FirstOrDefaultAsync();

                if (dataCabecera == null) return NotFound();

                // 2. OBTENER DETALLES
                var detalles = await (from d in _context.DPedidosServicio
                                      join cc in _context.CentroCostos on d.CentroCostoId equals cc.Id into ccJoin
                                      from cc in ccJoin.DefaultIfEmpty()
                                      where d.PedidoServicioId == id
                                      select new
                                      {
                                          d.Item,
                                          Descripcion = d.DescripcionServicio, // Usamos la propiedad correcta de DPedidosServicio
                                          d.UnidadMedida,
                                          Cantidad = d.Cantidad, // Cantidad del pedido
                                          d.Lugar,
                                          CentroCosto = cc != null ? cc.Nombre : ""
                                      }).ToListAsync();

                // 3. PASAR DATOS A LA VISTA
                ViewBag.Empresa = dataCabecera.Emp;
                ViewBag.Usuario = dataCabecera.Usu; // Este es nuestro "Consolidador"
                ViewBag.Estado = dataCabecera.NombreEstado;
                ViewBag.Detalles = detalles;

                return View(dataCabecera.Ped);
            }
            catch (Exception ex)
            {
                return Content($"Error al generar formato: {ex.Message}");
            }
        }
        #endregion
    }
}