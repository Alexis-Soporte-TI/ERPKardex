using ERPKardex.Data;
using ERPKardex.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;

namespace ERPKardex.Controllers
{
    // HERENCIA APLICADA
    public class PedCompraController : BaseController
    {
        private readonly ApplicationDbContext _context;

        public PedCompraController(ApplicationDbContext context)
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
        public async Task<JsonResult> GetPedidosCompraData()
        {
            try
            {
                var miEmpresaId = EmpresaUsuarioId;
                var esGlobal = EsAdminGlobal;

                // Consulta base
                var query = from p in _context.PedCompras
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
                                // Subconsulta de Centros de Costo
                                CentrosInvolucrados = (from d in _context.DPedidoCompras
                                                       join cc in _context.CentroCostos on d.CentroCostoId equals cc.Id
                                                       where d.PedidoCompraId == p.Id
                                                       select cc.Codigo).Distinct().ToList()
                            };

                // Filtro de seguridad usando BaseController
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
                var detalles = (from d in _context.DPedidoCompras
                                join cc in _context.CentroCostos on d.CentroCostoId equals cc.Id
                                join est in _context.Estados on d.EstadoId equals est.Id
                                where d.PedidoCompraId == id
                                select new
                                {
                                    d.Item,
                                    d.DescripcionLibre,
                                    d.UnidadMedida,
                                    d.CantidadAprobada,
                                    CantidadAtendida = d.CantidadAtendida ?? 0,
                                    d.ObservacionItem,
                                    Estado = est.Nombre,
                                    CentroCosto = cc.Codigo,
                                    d.Lugar,
                                    Ref = d.TablaReferencia == "DREQCOMPRA" ? ("Req. " + d.ItemReferencia) : "-"
                                }).OrderBy(x => x.Item).ToList();

                return Json(new { status = true, data = detalles });
            }
            catch (Exception ex) { return Json(new { status = false, message = ex.Message }); }
        }

        #endregion

        #region COMBOS Y UTILITARIOS

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
                int? almacenId = null;
                string almacenNombre = "";

                if (sucursalId != null)
                {
                    var almacen = _context.Almacenes
                        .FirstOrDefault(a => a.EmpresaId == empresaIdSeleccionada && a.SucursalId == sucursalId && a.Estado == true && (a.Codigo == "01" || a.Nombre.Contains("PRINCIPAL")));
                    almacenId = almacen?.Id;
                    almacenNombre = almacen?.Nombre ?? "";
                }

                return Json(new { status = true, sucursalId, sucursalNombre, almacenId, almacenNombre });
            }
            catch (Exception ex) { return Json(new { status = false, message = ex.Message }); }
        }

        #endregion

        #region LÓGICA DE REQUERIMIENTOS -> PEDIDO

        // LISTAR REQUERIMIENTOS "DISPONIBLES" (Aprobados o Parciales, con items pendientes)
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

                if (idParaFiltrar == 0) return Json(new { status = false, message = "Seleccione empresa." });

                // Estados Clave
                var estAprobadoREQ = _context.Estados.FirstOrDefault(e => e.Nombre == "Aprobado" && e.Tabla == "REQ")?.Id ?? 0;
                var estParcialREQ = _context.Estados.FirstOrDefault(e => e.Nombre == "Atendido Parcial" && e.Tabla == "REQ")?.Id ?? 0;
                var estPendienteDREQ = _context.Estados.FirstOrDefault(e => e.Nombre == "Pendiente" && e.Tabla == "DREQ")?.Id ?? 0;

                // LÓGICA FILTRADA: Traer REQ que (sea Aprobado O Parcial) Y (tenga al menos un detalle Pendiente)
                var data = (from r in _context.ReqCompras
                            join u in _context.Usuarios on r.UsuarioSolicitanteId equals u.Id
                            where r.EmpresaId == idParaFiltrar
                               && (r.EstadoId == estAprobadoREQ || r.EstadoId == estParcialREQ)
                               && _context.DReqCompras.Any(dr => dr.ReqCompraId == r.Id && dr.EstadoId == estPendienteDREQ)
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

        // DETALLES DISPONIBLES DE UN REQUERIMIENTO (SOLO PENDIENTES)
        [HttpGet]
        public JsonResult GetDetallesDeUnReq(int reqId)
        {
            try
            {
                var estPendienteDREQ = _context.Estados.FirstOrDefault(e => e.Nombre == "Pendiente" && e.Tabla == "DREQ")?.Id ?? 0;

                var detalles = (from d in _context.DReqCompras
                                join cc in _context.CentroCostos on d.CentroCostoId equals cc.Id
                                where d.ReqCompraId == reqId && d.EstadoId == estPendienteDREQ // FILTRO CRÍTICO
                                select new
                                {
                                    d.Item,
                                    d.DescripcionProducto,
                                    d.CantidadSolicitada,
                                    d.UnidadMedida,
                                    d.Lugar,
                                    CentroCosto = cc.Codigo
                                }).ToList();
                return Json(new { status = true, data = detalles });
            }
            catch (Exception ex) { return Json(new { status = false, message = ex.Message }); }
        }

        // CARGA BATCH PARA REGISTRAR (SOLO PENDIENTES)
        [HttpPost]
        public JsonResult GetDetallesBatch([FromBody] List<int> reqIds)
        {
            try
            {
                if (reqIds == null || reqIds.Count == 0) return Json(new { status = false, message = "Ningún requerimiento." });

                var estPendienteDREQ = _context.Estados.FirstOrDefault(e => e.Nombre == "Pendiente" && e.Tabla == "DREQ")?.Id ?? 0;

                var detalles = (from d in _context.DReqCompras
                                join cc in _context.CentroCostos on d.CentroCostoId equals cc.Id
                                where reqIds.Contains(d.ReqCompraId.GetValueOrDefault())
                                   && d.EstadoId == estPendienteDREQ // SOLO CARGAMOS LO QUE FALTA
                                select new
                                {
                                    ProductoId = d.ProductoId,
                                    DescripcionLibre = d.DescripcionProducto,
                                    UnidadMedida = d.UnidadMedida,
                                    Cantidad = d.CantidadSolicitada,
                                    CentroCostoId = d.CentroCostoId,
                                    NombreCentroCosto = cc.Codigo + " - " + cc.Nombre,
                                    Lugar = d.Lugar,
                                    ReqOrigenId = d.ReqCompraId,
                                    IdReferencia = d.Id,
                                    ItemReferencia = d.Item
                                }).ToList();

                return Json(new { status = true, data = detalles });
            }
            catch (Exception ex) { return Json(new { status = false, message = ex.Message }); }
        }

        #endregion

        #region TRANSACCIÓN PRINCIPAL: GUARDAR PEDIDO

        [HttpPost]
        public JsonResult GuardarPedido(PedCompra cabecera, string detallesJson)
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

                    // 1. CARGAR TODOS LOS ESTADOS NECESARIOS
                    var estGeneradoPED = _context.Estados.FirstOrDefault(e => e.Nombre == "Generado" && e.Tabla == "PED");
                    var estPendienteDREQ = _context.Estados.FirstOrDefault(e => e.Nombre == "Pendiente" && e.Tabla == "DREQ");
                    var estAtendidoDREQ = _context.Estados.FirstOrDefault(e => e.Nombre == "Atendido" && e.Tabla == "DREQ");
                    var estParcialREQ = _context.Estados.FirstOrDefault(e => e.Nombre == "Atendido Parcial" && e.Tabla == "REQ");
                    var estTotalREQ = _context.Estados.FirstOrDefault(e => e.Nombre == "Atendido Total" && e.Tabla == "REQ");
                    var estPendienteDPED = _context.Estados.FirstOrDefault(e => e.Nombre == "Pendiente" && e.Tabla == "DPED");

                    var tipoDoc = _context.TiposDocumentoInterno.FirstOrDefault(t => t.Codigo == "PED");

                    if (estGeneradoPED == null || estAtendidoDREQ == null || estParcialREQ == null || estTotalREQ == null || tipoDoc == null || estPendienteDPED == null)
                        throw new Exception("Falta configuración de Estados o Tipo Documento.");

                    // 2. CORRELATIVO
                    var ultimo = _context.PedCompras
                        .Where(x => x.EmpresaId == empresaId && x.TipoDocumentoInternoId == tipoDoc.Id)
                        .OrderByDescending(x => x.Numero)
                        .Select(x => x.Numero).FirstOrDefault();

                    int nro = 1;
                    if (!string.IsNullOrEmpty(ultimo))
                    {
                        var partes = ultimo.Split('-');
                        if (partes.Length > 1 && int.TryParse(partes[1], out int val)) nro = val + 1;
                    }

                    // 3. CABECERA PEDIDO
                    cabecera.Numero = $"PED-{nro.ToString("D10")}";
                    cabecera.TipoDocumentoInternoId = tipoDoc.Id;
                    cabecera.UsuarioSolicitanteId = usuarioId;
                    cabecera.UsuarioRegistro = usuarioId;
                    cabecera.FechaRegistro = DateTime.Now;
                    cabecera.EstadoId = estGeneradoPED.Id;
                    //cabecera.FechaEmision = DateTime.Now;
                    if (cabecera.FechaNecesaria == DateTime.MinValue) cabecera.FechaNecesaria = DateTime.Now;

                    _context.PedCompras.Add(cabecera);
                    _context.SaveChanges();

                    // 4. DETALLES Y ACTUALIZACIÓN DE ESTADOS
                    List<int> reqInvolucrados = new List<int>();

                    if (!string.IsNullOrEmpty(detallesJson))
                    {
                        var lista = JsonConvert.DeserializeObject<List<DPedCompra>>(detallesJson);
                        int item = 1;

                        foreach (var det in lista)
                        {
                            // A. Guardar Detalle Pedido
                            det.Id = 0;
                            det.PedidoCompraId = cabecera.Id;
                            det.EmpresaId = empresaId;
                            det.Item = item.ToString("D3");
                            det.CantidadAprobada = det.CantidadSolicitada;
                            det.CantidadAtendida = 0; // Inicialmente 0, se llena con OCO

                            // Estado del detalle del pedido (Usamos el mismo Generado de cabecera o Pendiente si tienes DPED)
                            // Si tienes estado "Pendiente" en 'DPED', úsalo aquí. Por ahora uso Generado.
                            det.EstadoId = estPendienteDPED.Id;

                            _context.DPedidoCompras.Add(det);

                            // B. Actualizar Detalle Requerimiento (Si viene de uno)
                            if (det.IdReferencia != null && det.IdReferencia > 0)
                            {
                                det.TablaReferencia = "DREQCOMPRA";

                                var dReq = _context.DReqCompras.Find(det.IdReferencia);
                                if (dReq != null)
                                {
                                    // CAMBIO CRÍTICO: El ítem pasa a ATENDIDO
                                    dReq.EstadoId = estAtendidoDREQ.Id;

                                    // Guardamos el ID del padre para recalcular al final
                                    if (dReq.ReqCompraId.HasValue && !reqInvolucrados.Contains(dReq.ReqCompraId.Value))
                                    {
                                        reqInvolucrados.Add(dReq.ReqCompraId.Value);
                                    }
                                }
                            }
                            item++;
                        }
                        _context.SaveChanges();
                    }

                    // 5. RECALCULAR ESTADO DE LAS CABECERAS DE REQUERIMIENTO
                    foreach (var reqId in reqInvolucrados)
                    {
                        // Verificar si queda ALGÚN ítem pendiente en este requerimiento
                        bool quedanPendientes = _context.DReqCompras
                            .Any(dr => dr.ReqCompraId == reqId && dr.EstadoId == estPendienteDREQ.Id);

                        var reqCabecera = _context.ReqCompras.Find(reqId);
                        if (reqCabecera != null)
                        {
                            if (quedanPendientes)
                            {
                                // Aún hay hijos vivos -> Parcial
                                reqCabecera.EstadoId = estParcialREQ.Id;
                            }
                            else
                            {
                                // Todos muertos (atendidos) -> Total
                                reqCabecera.EstadoId = estTotalREQ.Id;
                            }
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
        #region IMPRESIÓN
        [HttpGet]
        public async Task<IActionResult> Imprimir(int id)
        {
            try
            {
                // 1. OBTENER CABECERA
                var dataCabecera = await (from p in _context.PedCompras
                                          join e in _context.Empresas on p.EmpresaId equals e.Id
                                          // Tomamos al usuario que consolidó/registró el pedido de compra
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
                var detalles = await (from d in _context.DPedidoCompras
                                      join cc in _context.CentroCostos on d.CentroCostoId equals cc.Id into ccJoin
                                      from cc in ccJoin.DefaultIfEmpty()
                                      where d.PedidoCompraId == id
                                      select new
                                      {
                                          d.Item,
                                          Descripcion = d.DescripcionLibre,
                                          d.UnidadMedida,
                                          Cantidad = d.CantidadAprobada, // O la cantidad que manejes como consolidada
                                          d.Lugar,
                                          CentroCosto = cc != null ? cc.Nombre : ""
                                      }).ToListAsync();

                // 3. PASAR DATOS A LA VISTA
                ViewBag.Empresa = dataCabecera.Emp;
                ViewBag.Usuario = dataCabecera.Usu; // Este será nuestro "Consolidador"
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