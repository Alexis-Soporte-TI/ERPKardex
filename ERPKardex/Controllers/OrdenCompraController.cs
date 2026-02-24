using ERPKardex.Data;
using ERPKardex.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;

namespace ERPKardex.Controllers
{
    // 1. HERENCIA APLICADA
    public class OrdenCompraController : BaseController
    {
        private readonly ApplicationDbContext _context;

        public OrdenCompraController(ApplicationDbContext context)
        {
            _context = context;
        }

        public bool EsPeriodoValido(int periodoId)
        {
            using (var db = _context)
            {
                // Buscamos el periodo y verificamos que el estado sea true (Abierto)
                return db.PeriodosContables.Any(p => p.Id == periodoId && p.Estado == true);
            }
        }

        #region VISTAS
        public IActionResult Index() => View();
        public IActionResult Registrar() => View();
        #endregion

        #region 1. LISTADO (INDEX)

        [HttpGet]
        public async Task<JsonResult> GetOrdenesData()
        {
            try
            {
                var miEmpresaId = EmpresaUsuarioId; // BaseController
                var esGlobal = EsAdminGlobal;       // BaseController

                var query = from o in _context.OrdenCompras
                            join ent in _context.Proveedores on o.ProveedorId equals ent.Id
                            join tdi in _context.TiposDocumentoIdentidad on ent.TipoDocumentoIdentidadId equals tdi.Id
                            join est in _context.Estados on o.EstadoId equals est.Id
                            join mon in _context.Monedas on o.MonedaId equals mon.Id
                            join emp in _context.Empresas on o.EmpresaId equals emp.Id
                            orderby o.Id descending
                            select new
                            {
                                o.Id,
                                o.EmpresaId,
                                Empresa = emp.Nombre,
                                emp.RazonSocial,
                                o.Numero,
                                Fecha = o.FechaEmision.GetValueOrDefault().ToString("dd/MM/yyyy HH:mm"),
                                Proveedor = ent.RazonSocial,
                                TipoDocumentoIdentidad = tdi.Descripcion,
                                ent.NumeroDocumento,
                                Moneda = mon.Nombre,
                                Total = o.Total,
                                Estado = est.Nombre,
                                o.EstadoId,
                                o.Observacion
                            };

                // Filtro Base
                //if (!esGlobal)
                //{
                //    query = query.Where(x => x.EmpresaId == miEmpresaId);
                //}
                query = query.Where(x => x.EmpresaId == miEmpresaId);

                var listado = await query.ToListAsync();
                return Json(new { status = true, data = listado });
            }
            catch (Exception ex) { return Json(new { status = false, message = ex.Message }); }
        }

        [HttpGet]
        public JsonResult GetDetalleOrden(int id)
        {
            try
            {
                var detalles = (from d in _context.DOrdenCompras
                                join p in _context.Productos on d.ProductoId equals p.Id
                                where d.OrdenCompraId == id
                                select new
                                {
                                    d.Item,
                                    Producto = d.Descripcion,
                                    d.Lugar,
                                    d.UnidadMedida,
                                    d.Cantidad,
                                    d.PrecioUnitario,
                                    d.Total,
                                    d.TablaReferencia,
                                    d.IdReferencia
                                }).ToList();

                return Json(new { status = true, data = detalles });
            }
            catch (Exception ex) { return Json(new { status = false, message = ex.Message }); }
        }

        #endregion

        #region 2. DATOS PARA REGISTRO

        [HttpGet]
        public JsonResult GetCombosRegistro()
        {
            try
            {
                var miEmpresaId = EmpresaUsuarioId;
                var esGlobal = EsAdminGlobal;

                // Hacemos JOIN para obtener el Tipo de Documento
                var proveedores = (from p in _context.Proveedores
                                   join td in _context.TiposDocumentoIdentidad on p.TipoDocumentoIdentidadId equals td.Id
                                   where p.Estado == true && (p.EmpresaId == miEmpresaId)
                                   select new
                                   {
                                       p.Id,
                                       TipoDocumentoIdentidad = td.Descripcion,
                                       p.NumeroDocumento,
                                       p.RazonSocial,
                                   }).ToList();

                var monedas = _context.Monedas.Where(x => x.Estado == true).ToList();

                return Json(new { status = true, proveedores, monedas });
            }
            catch (Exception ex) { return Json(new { status = false, message = ex.Message }); }
        }

        #endregion

        #region 3. LÓGICA DE JALAR PEDIDOS (CEREBRO)

        // LISTAR PEDIDOS CON SALDO PENDIENTE
        [HttpGet]
        public JsonResult GetPedidosPendientes(int? empresaFiltroId)
        {
            try
            {
                var miEmpresaId = EmpresaUsuarioId;
                var esGlobal = EsAdminGlobal;
                int idEmpresa = esGlobal ? (empresaFiltroId ?? 0) : miEmpresaId;
                // POR AHORA
                idEmpresa = miEmpresaId;

                var estadosValidos = _context.Estados
                    .Where(e => (e.Nombre == "Generado" || e.Nombre == "Atendido Parcial") && e.Tabla == "PED") // Agregué "Atendido Parcial"
                    .Select(e => e.Id).ToList();

                // PEDIDOS CON SALDO REAL
                // Nota: Aquí solo filtramos si d.Solicitada > d.Atendida. 
                // No restamos lo "comprometido en borrador" aquí para mostrar todo lo que sea potencial,
                // la validación fina se hace al seleccionar.
                var pedidos = (from p in _context.PedCompras
                               join d in _context.DPedidoCompras on p.Id equals d.PedidoCompraId
                               where p.EmpresaId == idEmpresa
                                     && estadosValidos.Contains(p.EstadoId ?? 0)
                                     && d.CantidadSolicitada > (d.CantidadAtendida ?? 0)
                               group p by new { p.Id, p.Numero, p.FechaEmision, p.FechaNecesaria, p.Observacion } into g
                               select new
                               {
                                   g.Key.Id,
                                   g.Key.Numero,
                                   Fecha = g.Key.FechaEmision.GetValueOrDefault().ToString("dd/MM/yyyy HH:mm"),
                                   FechaNecesaria = g.Key.FechaNecesaria.GetValueOrDefault().ToString("dd/MM/yyyy"),
                                   g.Key.Observacion
                               }).ToList();

                return Json(new { status = true, data = pedidos });
            }
            catch (Exception ex) { return Json(new { status = false, message = ex.Message }); }
        }

        [HttpGet]
        public JsonResult GetCabeceraPedido(int pedidoId)
        {
            try
            {
                var pedido = _context.PedCompras.Where(p => p.Id == pedidoId)
                    .Select(p => new
                    {
                        p.SucursalId,
                        p.AlmacenId,
                        p.LugarDestino,
                        SucursalNombre = _context.Sucursales.Where(s => s.Id == p.SucursalId).Select(s => s.Nombre).FirstOrDefault(),
                        AlmacenNombre = _context.Almacenes.Where(a => a.Id == p.AlmacenId).Select(a => a.Nombre).FirstOrDefault()
                    }).FirstOrDefault();

                return Json(new { status = true, data = pedido });
            }
            catch (Exception ex) { return Json(new { status = false, message = ex.Message }); }
        }

        [HttpGet]
        public JsonResult GetDetallesPedidoParaOrden(int pedidoId)
        {
            try
            {
                // 1. Ítems del Pedido
                var itemsPedido = (from d in _context.DPedidoCompras
                                   join cc in _context.CentroCostos on d.CentroCostoId equals cc.Id into joinCC
                                   from cc in joinCC.DefaultIfEmpty()
                                   where d.PedidoCompraId == pedidoId
                                   select new
                                   {
                                       d.Id,
                                       d.ProductoId,
                                       d.DescripcionLibre,
                                       d.UnidadMedida,
                                       d.CantidadSolicitada,
                                       d.CantidadAtendida,
                                       d.CentroCostoId,
                                       CentroCostoNombre = cc != null ? cc.Codigo : "-",
                                       d.Lugar,
                                       d.Item
                                   }).ToList();

                // 2. Comprometido en Borradores (Generado)
                var estadoGeneradoOC = _context.Estados.FirstOrDefault(e => e.Nombre == "Generado" && e.Tabla == "ORDEN")?.Id ?? 0;

                var comprometidos = (from doc in _context.DOrdenCompras
                                     join oc in _context.OrdenCompras on doc.OrdenCompraId equals oc.Id
                                     where oc.EstadoId == estadoGeneradoOC
                                        && doc.TablaReferencia == "DPEDCOMPRA"
                                     select new { doc.IdReferencia, doc.Cantidad })
                                    .ToList();

                // 3. Cálculo final
                var resultado = itemsPedido.Select(ip =>
                {
                    decimal cantComprometida = comprometidos.Where(x => x.IdReferencia == ip.Id).Sum(x => x.Cantidad ?? 0);
                    decimal saldoDisponible = (ip.CantidadSolicitada ?? 0) - ((ip.CantidadAtendida ?? 0) + cantComprometida);

                    return new
                    {
                        ip.Id,
                        ip.ProductoId,
                        ip.Item,
                        Descripcion = ip.DescripcionLibre,
                        ip.UnidadMedida,
                        ip.CentroCostoId,
                        ip.CentroCostoNombre,
                        ip.Lugar,
                        CantidadPedido = ip.CantidadSolicitada,
                        // Si saldo es negativo (sobre-atención previa), mostramos 0
                        SaldoDisponible = saldoDisponible > 0 ? saldoDisponible : 0
                    };
                })
                .Where(x => x.SaldoDisponible > 0) // Solo items con saldo
                .ToList();

                return Json(new { status = true, data = resultado });
            }
            catch (Exception ex) { return Json(new { status = false, message = ex.Message }); }
        }

        #endregion

        #region 4. GUARDAR Y APROBAR

        [HttpPost]
        public JsonResult Guardar(OrdenCompra cabecera, string detallesJson)
        {
            using (var transaction = _context.Database.BeginTransaction())
            {
                try
                {
                    if (!EsPeriodoValido(cabecera.PeriodoContableId ?? 0))
                    {
                        throw new Exception("El periodo seleccionado ya no se encuentra abierto o es inválido. Por favor, actualice la página o seleccione otro periodo.");
                    }

                    var empresaId = EmpresaUsuarioId;
                    var usuarioId = UsuarioActualId;

                    if (cabecera.ProveedorId == 0) throw new Exception("Debe seleccionar un proveedor.");
                    if (cabecera.TipoCambio == null || cabecera.TipoCambio == 0) throw new Exception("El tipo de cambio no es válido.");

                    // Correlativo y datos fijos...
                    var estadoGenerado = _context.Estados.FirstOrDefault(e => e.Nombre == "Generado" && e.Tabla == "ORDEN");
                    var estadoPendiente = _context.Estados.FirstOrDefault(e => e.Nombre == "Pendiente" && e.Tabla == "DORDEN");
                    var tipoDoc = _context.TiposDocumentoInterno.FirstOrDefault(t => t.Codigo == "OCO");

                    if (estadoGenerado == null || tipoDoc == null || estadoPendiente == null) throw new Exception("Estados o tipo de documento no configurado");

                    var ultimo = _context.OrdenCompras
                        .Where(x => x.EmpresaId == empresaId && x.TipoDocumentoInternoId == tipoDoc.Id)
                        .OrderByDescending(x => x.Numero)
                        .Select(x => x.Numero).FirstOrDefault();

                    int nro = 1;
                    if (!string.IsNullOrEmpty(ultimo))
                    {
                        var partes = ultimo.Split('-');
                        if (partes.Length > 1 && int.TryParse(partes[1], out int correlativo)) nro = correlativo + 1;
                    }

                    cabecera.Numero = $"OCO-{nro.ToString("D10")}";
                    cabecera.TipoDocumentoInternoId = tipoDoc.Id;
                    cabecera.UsuarioCreacionId = usuarioId;
                    cabecera.EmpresaId = empresaId;
                    cabecera.EstadoId = estadoGenerado.Id;
                    cabecera.FechaRegistro = DateTime.Now;
                    //cabecera.FechaEmision = DateTime.Now;

                    // --------------------------------------------------------------------------------
                    // 3. CÁLCULO CONTABLE (BASE + IGV)
                    // --------------------------------------------------------------------------------
                    var listaDetalles = JsonConvert.DeserializeObject<List<DOrdenCompra>>(detallesJson);
                    decimal sumaBases = 0; // Suma de (Cant * Precio)

                    foreach (var det in listaDetalles)
                    {
                        sumaBases += (det.Cantidad ?? 0) * (det.PrecioUnitario ?? 0);
                    }

                    decimal igvTasa = 0.18m;

                    if (cabecera.IncluyeIgv == true)
                    {
                        // CASO A: NACIONAL (BASE + IGV)
                        // Lo que sumamos es la BASE IMPONIBLE (Total Afecto)
                        cabecera.TotalAfecto = sumaBases;
                        cabecera.IgvTotal = Math.Round(cabecera.TotalAfecto.Value * igvTasa, 2);
                        cabecera.Total = cabecera.TotalAfecto + cabecera.IgvTotal;
                        cabecera.TotalInafecto = 0;
                    }
                    else
                    {
                        // CASO B: IMPORTACIÓN (SIN IGV)
                        // Lo que sumamos es el TOTAL DIRECTO (Inafecto)
                        cabecera.Total = sumaBases;
                        cabecera.TotalInafecto = sumaBases;
                        cabecera.TotalAfecto = 0;
                        cabecera.IgvTotal = 0;
                    }

                    _context.OrdenCompras.Add(cabecera);
                    _context.SaveChanges();

                    // 4. GUARDAR DETALLES
                    int item = 1;
                    foreach (var det in listaDetalles)
                    {
                        det.Id = 0;
                        det.OrdenCompraId = cabecera.Id;
                        det.EstadoId = estadoPendiente.Id;
                        det.EmpresaId = empresaId;
                        det.Item = item.ToString("D3");

                        decimal subtotalLinea = (det.Cantidad ?? 0) * (det.PrecioUnitario ?? 0);

                        if (cabecera.IncluyeIgv == true)
                        {
                            // Nacional: El subtotal es Valor Venta. Se agrega impuesto.
                            det.ValorVenta = subtotalLinea;
                            det.Impuesto = subtotalLinea * igvTasa;
                            det.Total = det.ValorVenta + det.Impuesto;
                        }
                        else
                        {
                            // Importación: Sin impuesto.
                            det.ValorVenta = subtotalLinea;
                            det.Impuesto = 0;
                            det.Total = subtotalLinea;
                        }

                        //// Actualizar Saldos Pedido
                        //if (det.IdReferencia != null && det.TablaReferencia == "DPEDCOMPRA")
                        //{
                        //    var lineaPedido = _context.DPedidoCompras.Find(det.IdReferencia);
                        //    if (lineaPedido != null)
                        //    {
                        //        lineaPedido.CantidadAtendida = (lineaPedido.CantidadAtendida ?? 0) + det.Cantidad;
                        //        _context.DPedidoCompras.Update(lineaPedido);
                        //    }
                        //}

                        _context.DOrdenCompras.Add(det);
                        item++;
                    }
                    _context.SaveChanges();

                    transaction.Commit();
                    return Json(new { status = true, message = $"Orden {cabecera.Numero} generada correctamente." });
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    return Json(new { status = false, message = ex.Message });
                }
            }
        }

        [HttpPost]
        public JsonResult AprobarOrden(int id)
        {
            using (var transaction = _context.Database.BeginTransaction())
            {
                try
                {
                    var usuarioId = UsuarioActualId; // Usamos BaseController
                    var orden = _context.OrdenCompras.Find(id);
                    if (orden == null) throw new Exception("Orden no encontrada.");

                    var estadoAprobado = _context.Estados.FirstOrDefault(e => e.Nombre == "Aprobado" && e.Tabla == "ORDEN");
                    var estadoGenerado = _context.Estados.FirstOrDefault(e => e.Nombre == "Generado" && e.Tabla == "ORDEN");

                    if (orden.EstadoId != estadoGenerado.Id) throw new Exception("Solo se pueden aprobar órdenes en estado Generado.");

                    // 1. CARGAMOS LOS ESTADOS DEL DETALLE DE PEDIDO (DPED)
                    // Asegúrate de haber corrido el INSERT SQL que te pasé antes
                    var estDPedPendiente = _context.Estados.FirstOrDefault(e => e.Nombre == "Pendiente" && e.Tabla == "DPED")?.Id ?? 0;
                    var estDPedParcial = _context.Estados.FirstOrDefault(e => e.Nombre == "Atendido Parcial" && e.Tabla == "DPED")?.Id ?? 0;
                    var estDPedTotal = _context.Estados.FirstOrDefault(e => e.Nombre == "Atendido Total" && e.Tabla == "DPED")?.Id ?? 0;

                    var detalles = _context.DOrdenCompras.Where(d => d.OrdenCompraId == id).ToList();

                    // 2. ACTUALIZAR SALDOS Y ESTADOS DE LOS ÍTEMS DEL PEDIDO
                    foreach (var det in detalles)
                    {
                        if (det.IdReferencia != null && det.TablaReferencia == "DPEDCOMPRA")
                        {
                            var pedItem = _context.DPedidoCompras.Find(det.IdReferencia);
                            if (pedItem != null)
                            {
                                // A. Actualizamos cantidad atendida
                                pedItem.CantidadAtendida = (pedItem.CantidadAtendida ?? 0) + (det.Cantidad ?? 0);

                                // B. Determinamos el nuevo estado del ÍTEM (Lógica Granular)
                                if (pedItem.CantidadAtendida >= pedItem.CantidadSolicitada)
                                {
                                    // Se atendió todo o hubo sobreatención -> TOTAL
                                    pedItem.EstadoId = estDPedTotal;
                                }
                                else if (pedItem.CantidadAtendida > 0)
                                {
                                    // Se atendió algo, pero falta -> PARCIAL
                                    pedItem.EstadoId = estDPedParcial;
                                }
                                // Si es 0 se queda como estaba (Pendiente/Generado)
                            }
                        }
                    }
                    _context.SaveChanges();

                    // 3. VERIFICAR ESTADO DE PEDIDOS (CABECERA)
                    var pedidosInvolucrados = detalles
                        .Where(d => d.TablaReferencia == "DPEDCOMPRA" && d.IdReferencia != null)
                        .Select(d => _context.DPedidoCompras.Where(dp => dp.Id == d.IdReferencia).Select(dp => dp.PedidoCompraId).FirstOrDefault())
                        .Distinct().ToList();

                    var estTotalPED = _context.Estados.FirstOrDefault(e => e.Nombre == "Atendido Total" && e.Tabla == "PED");
                    var estParcialPED = _context.Estados.FirstOrDefault(e => e.Nombre == "Atendido Parcial" && e.Tabla == "PED");

                    foreach (var pedId in pedidosInvolucrados)
                    {
                        var pedido = _context.PedCompras.Find(pedId);

                        // Lógica Cabecera: Si CUALQUIER item sigue teniendo saldo pendiente -> La cabecera es PARCIAL
                        bool quedaAlgoPendiente = _context.DPedidoCompras
                            .Any(dp => dp.PedidoCompraId == pedId && (dp.CantidadAtendida ?? 0) < (dp.CantidadSolicitada ?? 0));

                        if (!quedaAlgoPendiente) pedido.EstadoId = estTotalPED.Id;
                        else pedido.EstadoId = estParcialPED.Id;
                    }

                    // 4. APROBAR ORDEN
                    orden.EstadoId = estadoAprobado.Id;
                    orden.UsuarioAprobador = usuarioId;
                    orden.FechaAprobacion = DateTime.Now;
                    _context.SaveChanges();

                    transaction.Commit();
                    return Json(new { status = true, message = "Orden Aprobada." });
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    return Json(new { status = false, message = ex.Message });
                }
            }
        }

        [HttpPost]
        public JsonResult RechazarOrden(int id)
        {
            try
            {
                var usuarioId = UsuarioActualId;
                var orden = _context.OrdenCompras.Find(id);
                var estadoAnulado = _context.Estados.FirstOrDefault(e => e.Nombre == "Anulado" && e.Tabla == "ORDEN");

                if (orden != null && estadoAnulado != null)
                {
                    orden.EstadoId = estadoAnulado.Id;
                    orden.UsuarioAprobador = usuarioId;
                    orden.FechaAprobacion = DateTime.Now;
                    _context.SaveChanges();
                    return Json(new { status = true, message = "Orden anulada." });
                }
                return Json(new { status = false, message = "Error al anular." });
            }
            catch (Exception ex) { return Json(new { status = false, message = ex.Message }); }
        }

        #endregion
        #region 4. IMPRESIÓN
        [HttpGet]
        public async Task<IActionResult> Imprimir(int id)
        {
            try
            {
                // 1. CABECERA
                var dataCabecera = await (from o in _context.OrdenCompras
                                          join e in _context.Empresas on o.EmpresaId equals e.Id
                                          join prov in _context.Proveedores on o.ProveedorId equals prov.Id
                                          join mon in _context.Monedas on o.MonedaId equals mon.Id
                                          join u in _context.Usuarios on o.UsuarioCreacionId equals u.Id
                                          join est in _context.Estados on o.EstadoId equals est.Id

                                          // LEFT JOIN para obtener al Aprobador/Rechazador
                                          join ua in _context.Usuarios on o.UsuarioAprobador equals ua.Id into joinAprob
                                          from uAprob in joinAprob.DefaultIfEmpty()

                                          where o.Id == id
                                          select new
                                          {
                                              Orden = o,
                                              Empresa = e,
                                              Proveedor = prov,
                                              Moneda = mon,
                                              Usuario = u,
                                              Estado = est.Nombre,

                                              // Datos del Aprobador
                                              AprobadorNombre = uAprob != null ? uAprob.Nombre : null,
                                              AprobadorCargo = uAprob != null ? uAprob.Cargo : null,
                                              FechaResolucion = o.FechaAprobacion
                                          }).FirstOrDefaultAsync();

                if (dataCabecera == null) return NotFound();

                // 2. DETALLES
                var detalles = await (from d in _context.DOrdenCompras
                                      join cc in _context.CentroCostos on d.CentroCostoId equals cc.Id into ccJoin
                                      from cc in ccJoin.DefaultIfEmpty()
                                      where d.OrdenCompraId == id
                                      select new
                                      {
                                          d.Item,
                                          d.Descripcion,
                                          d.UnidadMedida,
                                          d.Cantidad,
                                          d.PrecioUnitario,
                                          d.Total,
                                          d.Lugar,
                                          CentroCosto = cc != null ? cc.Nombre : ""
                                      }).ToListAsync();

                // 3. Pasar a la vista
                ViewBag.Empresa = dataCabecera.Empresa;
                ViewBag.Proveedor = dataCabecera.Proveedor;
                ViewBag.Moneda = dataCabecera.Moneda;
                ViewBag.Usuario = dataCabecera.Usuario;
                ViewBag.Estado = dataCabecera.Estado;

                // Datos del Aprobador para la firma
                ViewBag.AprobadorNombre = dataCabecera.AprobadorNombre;
                ViewBag.AprobadorCargo = dataCabecera.AprobadorCargo;
                ViewBag.FechaResolucion = dataCabecera.FechaResolucion;

                ViewBag.Detalles = detalles;

                return View(dataCabecera.Orden);
            }
            catch (Exception ex) { return Content($"Error: {ex.Message}"); }
        }
        #endregion
    }
}