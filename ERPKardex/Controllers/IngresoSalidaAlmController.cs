using ERPKardex.Data;
using ERPKardex.Models;
using ERPKardex.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;

namespace ERPKardex.Controllers
{
    // 1. Heredamos de BaseController
    public class IngresoSalidaAlmController : BaseController
    {
        private readonly ApplicationDbContext _context;

        public IngresoSalidaAlmController(ApplicationDbContext context)
        {
            _context = context;
        }

        public bool EsPeriodoValido(int periodoId)
        {
            return _context.PeriodosContables.Any(p => p.Id == periodoId && p.Estado == true);
        }

        #region VISTAS
        public IActionResult Index() => View();
        public IActionResult Registrar(int? id = null)
        {
            ViewData["MovimientoId"] = id;
            return View();
        }
        public IActionResult ReporteStock() => View();
        public IActionResult ReporteKardex() => View();
        public IActionResult Valorizacion() => View();
        public IActionResult ObtenerVistaRegistroEntidad()
        {
            return PartialView("_RegistroEntidad");
        }
        #endregion

        #region APIs Maestro-Detalle

        [HttpGet]
        public JsonResult GetMovimientosData(bool? Tipo, string CodigoProducto, string NombreProducto)
        {
            try
            {
                // Usamos la propiedad heredada
                var movimientos = (from isa in _context.IngresoSalidaAlms
                                   join mot in _context.Motivos on isa.MotivoId equals mot.Id
                                   join est in _context.Estados on isa.EstadoId equals est.Id
                                   join suc in _context.Sucursales on isa.SucursalId equals suc.Id
                                   join alm in _context.Almacenes on isa.AlmacenId equals alm.Id
                                   join emp in _context.Empresas on alm.EmpresaId equals emp.Id
                                   join tdi in _context.TiposDocumentoInterno on isa.TipoDocumentoInternoId equals tdi.Id
                                   join td in _context.TipoDocumentos on isa.TipoDocumentoId equals td.Id into joinDoc
                                   from tc in joinDoc.DefaultIfEmpty()
                                   join mon in _context.Monedas on isa.MonedaId equals mon.Id into joinMon
                                   from mo in joinMon.DefaultIfEmpty()
                                   where isa.EmpresaId == EmpresaUsuarioId
                                   where Tipo == null || isa.TipoMovimiento == Tipo.Value
                                   orderby isa.Fecha descending, isa.Numero descending

                                   let productos = _context.DIngresoSalidaAlms.Where(d => d.IngresoSalidaAlmId == isa.Id)
                                        .Select(d => new { d.CodProducto, d.DescripcionProducto })
                                        .ToList()
                                   where (string.IsNullOrEmpty(CodigoProducto) || productos.Any(p => p.CodProducto.Contains(CodigoProducto))) &&
                                         (string.IsNullOrEmpty(NombreProducto) || productos.Any(p => p.DescripcionProducto.Contains(NombreProducto)))

                                   select new IngresoSalidaAlmViewModel
                                   {
                                       Id = isa.Id,
                                       Fecha = isa.Fecha,
                                       Numero = isa.Numero,
                                       MotivoId = isa.MotivoId,
                                       CodMotivo = mot.Codigo,
                                       TipoMovimiento = mot.TipoMovimiento,
                                       Motivo = mot.Descripcion,
                                       TipoDocumentoId = isa.TipoDocumentoId,
                                       TipoDocumento = tc.Descripcion,
                                       SerieDocumento = isa.SerieDocumento,
                                       NumeroDocumento = isa.NumeroDocumento,
                                       FechaDocumento = isa.FechaDocumento,
                                       MonedaId = isa.MonedaId,
                                       Moneda = mo.Nombre,
                                       EstadoId = isa.EstadoId,
                                       Estado = est.Nombre,
                                       SucursalId = isa.SucursalId,
                                       Sucursal = suc.Nombre,
                                       AlmacenId = isa.AlmacenId,
                                       Almacen = alm.Nombre,
                                       UsuarioId = isa.UsuarioId,
                                       FechaRegistro = isa.FechaRegistro,
                                   }).ToList();

                return Json(new { data = movimientos, message = "Movimientos retornados exitosamente.", status = true });
            }
            catch (Exception ex)
            {
                return Json(new ApiResponse { data = null, message = ex.Message, status = false });
            }
        }

        [HttpGet]
        public JsonResult GetDetalleMovimiento(int id)
        {
            try
            {
                var detalles = _context.DIngresoSalidaAlms
                    .Where(d => d.IngresoSalidaAlmId == id)
                    .Select(d => new
                    {
                        d.Item,
                        Codigo = d.CodProducto,
                        Producto = d.DescripcionProducto,
                        Unidad = d.CodUnidadMedida,
                        d.Cantidad,
                        d.Precio,
                        d.Total
                    })
                    .OrderBy(d => d.Item)
                    .ToList();

                return Json(new { status = true, data = detalles });
            }
            catch (Exception ex)
            {
                return Json(new { status = false, message = "Error al obtener detalles: " + ex.Message });
            }
        }

        [HttpGet]
        public JsonResult GetMovimientoParaEditar(int id)
        {
            try
            {
                var estadoPendiente = _context.Estados.FirstOrDefault(e => e.Nombre == "Pendiente" && e.Tabla == "INGRESOSALIDAALM");

                var cabecera = _context.IngresoSalidaAlms
                    .Where(x => x.Id == id && x.EmpresaId == EmpresaUsuarioId)
                    .Select(x => new
                    {
                        x.Id, x.Fecha, x.TipoMovimiento, x.SucursalId, x.AlmacenId,
                        x.MotivoId, x.ProveedorId, x.FechaDocumento, x.TipoDocumentoId,
                        x.SerieDocumento, x.NumeroDocumento, x.PeriodoContableId, x.EstadoId,
                        x.MonedaId, x.IdReferencia, x.TablaReferencia
                    })
                    .FirstOrDefault();

                if (cabecera == null)
                    return Json(new { status = false, message = "Movimiento no encontrado." });

                if (estadoPendiente == null || cabecera.EstadoId != estadoPendiente.Id)
                    return Json(new { status = false, message = "Solo se pueden editar movimientos en estado Pendiente." });

                var detalles = (from d in _context.DIngresoSalidaAlms
                                join cc in _context.CentroCostos on d.CentroCostoId equals cc.Id into ccJoin
                                from cc in ccJoin.DefaultIfEmpty()
                                join ac in _context.Actividades on d.ActividadId equals ac.Id into acJoin
                                from ac in acJoin.DefaultIfEmpty()
                                where d.IngresoSalidaAlmId == id
                                orderby d.Item
                                select new
                                {
                                    d.Item, d.ProductoId, d.CodProducto, d.DescripcionProducto,
                                    d.CodUnidadMedida, d.Cantidad, d.Lote,
                                    d.FechaFabricacion, d.FechaVencimiento,
                                    d.CentroCostoId, d.ActividadId, d.PedidoInterno,
                                    d.IdReferencia, d.TablaReferencia,
                                    NombreCentroCosto = cc != null ? cc.Nombre : "",
                                    NombreActividad = ac != null ? ac.Nombre : ""
                                }).ToList();

                return Json(new { status = true, cabecera = cabecera, detalles = detalles });
            }
            catch (Exception ex)
            {
                return Json(new { status = false, message = ex.Message });
            }
        }

        [HttpGet]
        public JsonResult BuscarFacturasPendientesIngreso(int proveedorId)
        {
            try
            {
                // 1. Definir qué tipos de doc son válidos para ingreso (FAC, BOL, Liquidación, Guia)
                // Usamos los códigos de tu BD
                var codigosValidos = new List<string> { "FAC", "BOL", "09" }; // 09 es GRR

                // 2. Buscar facturas de ese proveedor
                var facturas = (from d in _context.DocumentosPagar
                                join t in _context.TiposDocumentoInterno on d.TipoDocumentoInternoId equals t.Id
                                join est in _context.Estados on d.EstadoId equals est.Id
                                join mon in _context.Monedas on d.MonedaId equals mon.Id
                                where d.ProveedorId == proveedorId
                                   && d.EmpresaId == EmpresaUsuarioId
                                   && codigosValidos.Contains(t.Codigo)
                                   && est.Nombre != "Anulado"
                                select new
                                {
                                    d.Id,
                                    Tipo = t.Codigo,
                                    Numero = d.Serie + "-" + d.Numero,
                                    Fecha = d.FechaEmision,
                                    Moneda = mon.Simbolo,
                                    Total = d.Total,
                                }).ToList();

                // 3. FILTRAR: Excluir las que YA TIENEN ingreso registrado
                // Buscamos en 'ingresosalidaalm' si existe alguna cabecera activa con id_referencia = factura.Id
                var estadoAnuladoAlm = _context.Estados.FirstOrDefault(e => e.Nombre == "Anulado" && e.Tabla == "INGRESOSALIDAALM");
                int idAnulAlm = estadoAnuladoAlm?.Id ?? -1;

                var ingresosExistentes = _context.IngresoSalidaAlms
                    .Where(i => i.TablaReferencia == "DOCUMENTO_PAGAR" && i.EstadoId != idAnulAlm)
                    .Select(i => i.IdReferencia)
                    .ToList();

                var pendientes = facturas
                    .Where(f => !ingresosExistentes.Contains(f.Id))
                    .OrderByDescending(f => f.Fecha)
                    .ToList();

                return Json(new { status = true, data = pendientes });
            }
            catch (Exception ex) { return Json(new { status = false, message = ex.Message }); }
        }

        [HttpGet]
        public JsonResult GetDetallesFacturaParaIngreso(int facturaId)
        {
            try
            {
                // 1. OBTENER CABECERA DEL COMPROBANTE CON DESCRIPCIÓN DE TIPO DOC
                var cabecera = (from d in _context.DocumentosPagar
                                join t in _context.TiposDocumentoInterno on d.TipoDocumentoInternoId equals t.Id
                                where d.Id == facturaId
                                select new
                                {
                                    Fecha = d.FechaEmision.HasValue ? d.FechaEmision.Value.ToString("yyyy-MM-dd") : DateTime.Now.ToString("yyyy-MM-dd"),
                                    TipoDocId = d.TipoDocumentoInternoId,
                                    TipoDocNombre = t.Descripcion, // <--- NUEVO: Para mostrar en texto
                                    Serie = d.Serie,
                                    Numero = d.Numero,
                                    MonedaId = d.MonedaId
                                }).FirstOrDefault();

                if (cabecera == null) throw new Exception("No se encontró la información del comprobante.");

                // 2. OBTENER DETALLES (Con precios ocultos para valorización)
                var detalles = (from d in _context.DDocumentosPagar
                                join p in _context.Productos on d.ProductoId equals p.Id
                                where d.DocumentoPagarId == facturaId
                                select new
                                {
                                    ProductoId = d.ProductoId,
                                    Codigo = p.Codigo,
                                    Descripcion = p.DescripcionProducto,
                                    Unidad = d.UnidadMedida,
                                    Cantidad = d.Cantidad,
                                    Precio = d.PrecioUnitario,
                                    IdReferenciaOrden = d.IdReferencia,
                                    TablaReferenciaOrden = d.TablaReferencia
                                }).ToList();

                return Json(new { status = true, cabecera = cabecera, detalles = detalles });
            }
            catch (Exception ex) { return Json(new { status = false, message = ex.Message }); }
        }

        [HttpPost]
        public JsonResult GuardarMovimiento(IngresoSalidaAlm cabecera, string detallesJson)
        {
            using (var transaction = _context.Database.BeginTransaction())
            {
                try
                {
                    if (!EsPeriodoValido(cabecera.PeriodoContableId ?? 0))
                    {
                        throw new Exception("El periodo seleccionado ya no se encuentra abierto o es inválido. Por favor, actualice la página o seleccione otro periodo.");
                    }

                    // =================================================================================
                    // 1. CONFIGURACIÓN INICIAL Y VALIDACIONES BÁSICAS
                    // =================================================================================

                    cabecera.EmpresaId = EmpresaUsuarioId;
                    cabecera.UsuarioId = UsuarioActualId;
                    cabecera.FechaRegistro = DateTime.Now;
                    //cabecera.Fecha = DateTime.Now;

                    string codigoDoc = (cabecera.TipoMovimiento == true) ? "IALM" : "SALM";

                    // Estados necesarios
                    var estadoPendiente = _context.Estados.FirstOrDefault(e => e.Nombre == "Pendiente" && e.Tabla == "INGRESOSALIDAALM");
                    var estadoAnulado = _context.Estados.FirstOrDefault(e => e.Nombre == "Anulado" && e.Tabla == "INGRESOSALIDAALM"); // Para validación

                    if (estadoPendiente == null) throw new Exception("Estado 'Pendiente' no configurado para almacén.");

                    var tipoDocInterno = _context.TiposDocumentoInterno.FirstOrDefault(t => t.Codigo == codigoDoc && t.Estado == true);
                    if (tipoDocInterno == null) throw new Exception($"Tipo documento interno '{codigoDoc}' no existe.");

                    // =================================================================================
                    // 2. VALIDACIÓN CRÍTICA: NO DUPLICAR INGRESO POR MISMO COMPROBANTE
                    // =================================================================================

                    // Si es un Ingreso (Entrada) y viene amarrado a un documento (Factura)
                    if (cabecera.TipoMovimiento == true &&
                        cabecera.TablaReferencia == "DOCUMENTO_PAGAR" &&
                        cabecera.IdReferencia != null)
                    {
                        bool existeIngresoPrevio = _context.IngresoSalidaAlms
                            .Any(x => x.EmpresaId == EmpresaUsuarioId &&
                                      x.TipoMovimiento == true &&
                                      x.TablaReferencia == "DOCUMENTO_PAGAR" &&
                                      x.IdReferencia == cabecera.IdReferencia &&
                                      x.EstadoId != estadoAnulado.Id); // Ignoramos los anulados

                        if (existeIngresoPrevio)
                            throw new Exception("ERROR DE NEGOCIO: Este comprobante ya tiene registrado un Ingreso de Almacén. No se puede duplicar el ingreso.");
                    }

                    // =================================================================================
                    // 3. GENERACIÓN DE CORRELATIVO
                    // =================================================================================

                    var ultimoRegistro = _context.IngresoSalidaAlms
                        .Where(x => x.EmpresaId == EmpresaUsuarioId && x.TipoDocumentoInternoId == tipoDocInterno.Id)
                        .OrderByDescending(x => x.Numero)
                        .Select(x => x.Numero)
                        .FirstOrDefault();

                    int correlativo = 1;
                    if (!string.IsNullOrEmpty(ultimoRegistro))
                    {
                        var partes = ultimoRegistro.Split('-');
                        if (partes.Length > 1 && int.TryParse(partes[1], out int n)) correlativo = n + 1;
                    }

                    cabecera.TipoDocumentoInternoId = tipoDocInterno.Id;
                    cabecera.Numero = $"{codigoDoc}-{correlativo.ToString("D10")}";
                    cabecera.EstadoId = estadoPendiente.Id;

                    // Guardamos cabecera para obtener ID
                    _context.IngresoSalidaAlms.Add(cabecera);
                    _context.SaveChanges();

                    // =================================================================================
                    // 4. PROCESAMIENTO DE DETALLES Y STOCK
                    // =================================================================================

                    var listaDetalles = JsonConvert.DeserializeObject<List<DIngresoSalidaAlm>>(detallesJson);

                    // Estados para ÍTEMS (DORDEN)
                    var estItemParcial = _context.Estados.FirstOrDefault(x => x.Tabla == "DORDEN" && x.Nombre == "Atendido Parcial");
                    var estItemTotal = _context.Estados.FirstOrDefault(x => x.Tabla == "DORDEN" && x.Nombre == "Atendido Total");

                    // Variable para identificar la Orden de Compra Padre (si aplica)
                    int? idOrdenCompraPadre = null;

                    foreach (var det in listaDetalles)
                    {
                        // A. Guardar Detalle Almacén
                        det.Id = 0;
                        det.IngresoSalidaAlmId = cabecera.Id;
                        det.FechaRegistro = DateTime.Now;
                        det.EmpresaId = EmpresaUsuarioId;

                        if (det.Cantidad <= 0)
                        {
                            throw new Exception("ERROR: las cantidades de los detalles no pueden ser 0.");
                        }

                        // Si es ingreso, calculamos los totales basados en la Cantidad Real * Precio Factura
                        if (cabecera.TipoMovimiento == true)
                        {
                            decimal cant = det.Cantidad ?? 0;
                            decimal precio = det.Precio ?? 0; // Viene oculto desde el front

                            det.Subtotal = cant * precio;
                            det.Igv = det.Subtotal * 0.18m; // IGV Perú (Ajustable)
                            det.Total = det.Subtotal + det.Igv;
                        }

                        // Completar datos maestros si faltan
                        var prod = _context.Productos.Find(det.ProductoId);
                        if (prod != null)
                        {
                            det.CodProducto = prod.Codigo;
                            det.DescripcionProducto = prod.DescripcionProducto;
                            det.CodUnidadMedida = prod.CodUnidadMedida;
                        }

                        // Referencia Logística al ítem de orden
                        if (det.IdReferencia != null && string.IsNullOrEmpty(det.TablaReferencia))
                            det.TablaReferencia = (cabecera.TipoMovimiento == true) ? "DORDENCOMPRA" : null;

                        _context.DIngresoSalidaAlms.Add(det);

                        //// B. Actualizar Kardex/Stock (Lógica Estándar)
                        //var stock = _context.StockAlmacenes.FirstOrDefault(s => s.AlmacenId == cabecera.AlmacenId && s.ProductoId == det.ProductoId && s.EmpresaId == EmpresaUsuarioId);

                        //if (stock == null)
                        //{
                        //    if (cabecera.TipoMovimiento == false) throw new Exception($"Sin stock para producto {det.CodProducto}.");
                        //    stock = new StockAlmacen { AlmacenId = cabecera.AlmacenId ?? 0, ProductoId = det.ProductoId ?? 0, StockActual = 0, EmpresaId = EmpresaUsuarioId };
                        //    _context.StockAlmacenes.Add(stock);
                        //}

                        //if (cabecera.TipoMovimiento == true)
                        //{
                        //    stock.StockActual += det.Cantidad ?? 0;
                        //}
                        //else if (cabecera.TipoMovimiento == false)
                        //{
                        //    if (stock.StockActual >= det.Cantidad)
                        //    {
                        //        stock.StockActual -= det.Cantidad ?? 0;
                        //    }
                        //    else
                        //    {
                        //        throw new Exception($"Error: No hay stock suficiente para el producto: {det.DescripcionProducto}.");
                        //    }
                        //}

                        //stock.UltimaActualizacion = DateTime.Now;

                        //// C. Actualizar ÍTEM de la Orden (DORDEN)
                        //if (cabecera.TipoMovimiento == true && det.IdReferencia != null && det.TablaReferencia == "DORDENCOMPRA")
                        //{
                        //    var dOrden = _context.DOrdenCompras.Find(det.IdReferencia);
                        //    if (dOrden != null)
                        //    {
                        //        // Guardamos el ID de la cabecera para actualizarla al final
                        //        if (idOrdenCompraPadre == null) idOrdenCompraPadre = dOrden.OrdenCompraId;

                        //        // Actualizar atendido
                        //        dOrden.CantidadAtendida = (dOrden.CantidadAtendida ?? 0) + (det.Cantidad ?? 0);

                        //        // Actualizar estado del ítem (Tabla: DORDEN)
                        //        if (dOrden.CantidadAtendida >= dOrden.Cantidad) dOrden.EstadoId = estItemTotal.Id;
                        //        else dOrden.EstadoId = estItemParcial.Id;
                        //    }
                        //}
                    }

                    _context.SaveChanges();

                    // =================================================================================
                    // 5. ACTUALIZACIÓN CABECERA DE ORDEN (TABLA: ORDEN)
                    // =================================================================================

                    //if (idOrdenCompraPadre != null)
                    //{
                    //    var ordenCompra = _context.OrdenCompras.Find(idOrdenCompraPadre);
                    //    if (ordenCompra != null)
                    //    {
                    //        // Estados para CABECERA (ORDEN)
                    //        var estOrdenParcial = _context.Estados.FirstOrDefault(x => x.Tabla == "ORDEN" && x.Nombre == "Atendido Parcial");
                    //        var estOrdenTotal = _context.Estados.FirstOrDefault(x => x.Tabla == "ORDEN" && x.Nombre == "Atendido Total");

                    //        if (estOrdenParcial != null && estOrdenTotal != null)
                    //        {
                    //            // Verificar TODOS los ítems de esa orden
                    //            var itemsOrden = _context.DOrdenCompras.Where(x => x.OrdenCompraId == idOrdenCompraPadre).ToList();

                    //            bool todoAtendido = itemsOrden.All(x => (x.CantidadAtendida ?? 0) >= x.Cantidad);
                    //            bool algoAtendido = itemsOrden.Any(x => (x.CantidadAtendida ?? 0) > 0);

                    //            if (todoAtendido)
                    //            {
                    //                ordenCompra.EstadoId = estOrdenTotal.Id; // Cierre Total
                    //            }
                    //            else if (algoAtendido)
                    //            {
                    //                ordenCompra.EstadoId = estOrdenParcial.Id; // Cierre Parcial
                    //            }
                    //            // Si nada atendido, se queda en Aprobado (no tocamos)
                    //        }
                    //    }
                    //    _context.SaveChanges();
                    //}

                    transaction.Commit();
                    return Json(new { status = true, message = "Movimiento registrado correctamente." });
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    return Json(new { status = false, message = "Error: " + ex.Message });
                }
            }
        }

        [HttpPost]
        public JsonResult ActualizarMovimiento(IngresoSalidaAlm cabecera, string detallesJson, int id)
        {
            using (var transaction = _context.Database.BeginTransaction())
            {
                try
                {
                    var movExistente = _context.IngresoSalidaAlms
                        .FirstOrDefault(x => x.Id == id && x.EmpresaId == EmpresaUsuarioId);

                    if (movExistente == null)
                        throw new Exception("Movimiento no encontrado.");

                    var estadoPendiente = _context.Estados.FirstOrDefault(e => e.Nombre == "Pendiente" && e.Tabla == "INGRESOSALIDAALM");
                    if (estadoPendiente == null || movExistente.EstadoId != estadoPendiente.Id)
                        throw new Exception("Solo se pueden editar movimientos en estado Pendiente.");

                    if (!EsPeriodoValido(cabecera.PeriodoContableId ?? 0))
                        throw new Exception("El periodo seleccionado ya no se encuentra abierto o es inválido.");

                    // Actualizar campos de cabecera (sin tocar TipoMovimiento, Numero, EstadoId, TipoDocumentoInternoId)
                    movExistente.Fecha = cabecera.Fecha;
                    movExistente.SucursalId = cabecera.SucursalId;
                    movExistente.AlmacenId = cabecera.AlmacenId;
                    movExistente.MotivoId = cabecera.MotivoId;
                    movExistente.ProveedorId = cabecera.ProveedorId;
                    movExistente.FechaDocumento = cabecera.FechaDocumento;
                    movExistente.TipoDocumentoId = cabecera.TipoDocumentoId;
                    movExistente.SerieDocumento = cabecera.SerieDocumento;
                    movExistente.NumeroDocumento = cabecera.NumeroDocumento;
                    movExistente.PeriodoContableId = cabecera.PeriodoContableId;
                    movExistente.MonedaId = cabecera.MonedaId;

                    // Eliminar detalles anteriores
                    var detallesAnteriores = _context.DIngresoSalidaAlms.Where(d => d.IngresoSalidaAlmId == id).ToList();
                    _context.DIngresoSalidaAlms.RemoveRange(detallesAnteriores);
                    _context.SaveChanges();

                    // Insertar nuevos detalles
                    var listaDetalles = JsonConvert.DeserializeObject<List<DIngresoSalidaAlm>>(detallesJson);

                    foreach (var det in listaDetalles)
                    {
                        det.Id = 0;
                        det.IngresoSalidaAlmId = id;
                        det.FechaRegistro = DateTime.Now;
                        det.EmpresaId = EmpresaUsuarioId;

                        if (det.Cantidad <= 0)
                            throw new Exception("Las cantidades de los detalles no pueden ser 0.");

                        if (movExistente.TipoMovimiento == true)
                        {
                            decimal cant = det.Cantidad ?? 0;
                            decimal precio = det.Precio ?? 0;
                            det.Subtotal = cant * precio;
                            det.Igv = det.Subtotal * 0.18m;
                            det.Total = det.Subtotal + det.Igv;
                        }

                        var prod = _context.Productos.Find(det.ProductoId);
                        if (prod != null)
                        {
                            det.CodProducto = prod.Codigo;
                            det.DescripcionProducto = prod.DescripcionProducto;
                            det.CodUnidadMedida = prod.CodUnidadMedida;
                        }

                        _context.DIngresoSalidaAlms.Add(det);
                    }

                    _context.SaveChanges();
                    transaction.Commit();

                    return Json(new { status = true, message = "Movimiento actualizado correctamente." });
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    return Json(new { status = false, message = "Error: " + ex.Message });
                }
            }
        }

        #endregion

        #region APROBACIÓN/RECHAZO DE MOVIMIENTOS
        // CAMBIAR ESTADO (Aprobar/Rechazar)
        [HttpPost]
        public JsonResult CambiarEstado(int id, string nombreEstado)
        {
            using (var transaction = _context.Database.BeginTransaction())
            {
                try
                {
                    var usuarioId = UsuarioActualId;
                    var estadoDb = _context.Estados.FirstOrDefault(e => e.Nombre == nombreEstado && e.Tabla == "INGRESOSALIDAALM");

                    if (estadoDb == null) return Json(new { status = false, message = "Estado no configurado." });

                    var cabecera = _context.IngresoSalidaAlms.Find(id);
                    if (cabecera == null) return Json(new { status = false, message = "Movimiento no encontrado." });

                    // Validación de seguridad: Evitar reprocesar si ya está en ese estado (Evita duplicar stock)
                    if (cabecera.EstadoId == estadoDb.Id)
                        return Json(new { status = false, message = $"El movimiento ya se encuentra en estado {nombreEstado}." });

                    // =================================================================================
                    // ACTUALIZACIÓN DE STOCK (SOLO SI SE ESTÁ APROBANDO)
                    // =================================================================================
                    if (nombreEstado == "Aprobado")
                    {
                        var detalles = _context.DIngresoSalidaAlms.Where(d => d.IngresoSalidaAlmId == id).ToList();

                        foreach (var det in detalles)
                        {
                            var stock = _context.StockAlmacenes.FirstOrDefault(s => s.AlmacenId == cabecera.AlmacenId && s.ProductoId == det.ProductoId && s.EmpresaId == EmpresaUsuarioId);

                            if (stock == null)
                            {
                                if (cabecera.TipoMovimiento == false) throw new Exception($"Sin stock para producto {det.CodProducto}.");
                                stock = new StockAlmacen { AlmacenId = cabecera.AlmacenId ?? 0, ProductoId = det.ProductoId ?? 0, StockActual = 0, EmpresaId = EmpresaUsuarioId };
                                _context.StockAlmacenes.Add(stock);
                            }

                            decimal stockActual = stock.StockActual ?? 0;
                            decimal cantidadMovimiento = det.Cantidad ?? 0;

                            if (cabecera.TipoMovimiento == true) // Es Ingreso
                            {
                                stock.StockActual = Math.Round(stockActual + cantidadMovimiento, 2, MidpointRounding.AwayFromZero);
                            }
                            else if (cabecera.TipoMovimiento == false) // Es Salida
                            {
                                if (stock.StockActual >= det.Cantidad)
                                {
                                    stock.StockActual = Math.Round(stockActual - cantidadMovimiento, 2, MidpointRounding.AwayFromZero);
                                }
                                else
                                {
                                    throw new Exception($"Error: No hay stock suficiente para el producto: {det.DescripcionProducto}. Stock actual: {stock.StockActual}");
                                }
                            }

                            stock.UltimaActualizacion = DateTime.Now;
                        }
                    }

                    // Cambiamos estado de la cabecera
                    cabecera.EstadoId = estadoDb.Id;
                    cabecera.UsuarioAprobador = usuarioId;
                    cabecera.FechaAprobacion = DateTime.Now;

                    _context.SaveChanges();
                    transaction.Commit();

                    string msgExtra = (nombreEstado == "Aprobado") ? " y Kardex actualizado." : ".";
                    return Json(new { status = true, message = $"Movimiento {nombreEstado} correctamente{msgExtra}" });
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    return Json(new { status = false, message = ex.Message });
                }
            }
        }
        #endregion

        #region API: ANULACIÓN DE MOVIMIENTO (NUEVO)

        [HttpPost]
        public JsonResult AnularMovimiento(int id)
        {
            using (var transaction = _context.Database.BeginTransaction())
            {
                try
                {
                    // 1. Obtener el movimiento y validar permisos
                    var cabecera = _context.IngresoSalidaAlms
                                           .FirstOrDefault(x => x.Id == id && x.EmpresaId == EmpresaUsuarioId);

                    if (cabecera == null) return Json(new { status = false, message = "Movimiento no encontrado o no autorizado." });

                    // 2. Validar Estado
                    var estadoAnulado = _context.Estados.FirstOrDefault(e => e.Nombre == "Anulado" && e.Tabla == "INGRESOSALIDAALM");
                    if (estadoAnulado == null) return Json(new { status = false, message = "Estado 'Anulado' no configurado en BD." });

                    if (cabecera.EstadoId == estadoAnulado.Id) return Json(new { status = false, message = "El movimiento ya se encuentra anulado." });

                    // 3. Obtener Detalles para revertir stock
                    var detalles = _context.DIngresoSalidaAlms.Where(d => d.IngresoSalidaAlmId == id).ToList();

                    foreach (var detalle in detalles)
                    {
                        var stock = _context.StockAlmacenes
                                            .FirstOrDefault(s => s.AlmacenId == cabecera.AlmacenId && s.ProductoId == detalle.ProductoId && s.EmpresaId == EmpresaUsuarioId);

                        if (stock == null) throw new Exception($"Error crítico: No se encontró registro de stock para el producto {detalle.CodProducto}.");

                        decimal cantidad = detalle.Cantidad ?? 0;

                        // LÓGICA INVERSA AL REGISTRO
                        if (cabecera.TipoMovimiento == true)
                        {
                            // Si FUE ENTRADA (True), al anular debemos RESTAR
                            if (stock.StockActual - cantidad < 0)
                            {
                                throw new Exception($"No se puede anular el ingreso {cabecera.Numero} porque el producto {detalle.CodProducto} ya fue consumido. Stock actual insuficiente para reversión.");
                            }
                            stock.StockActual -= cantidad;
                        }
                        else
                        {
                            // Si FUE SALIDA (False), al anular debemos SUMAR (Devolver al almacén)
                            stock.StockActual += cantidad;
                        }

                        stock.UltimaActualizacion = DateTime.Now;
                    }

                    // 4. Actualizar Estado Cabecera
                    cabecera.EstadoId = estadoAnulado.Id;
                    // Opcional: Podrías guardar quién anuló y cuándo en campos nuevos si los tuvieras
                    cabecera.UsuarioAnulacionId = UsuarioActualId;
                    cabecera.FechaAnulacion = DateTime.Now;

                    _context.SaveChanges();
                    transaction.Commit();

                    return Json(new { status = true, message = $"Movimiento {cabecera.Numero} anulado correctamente y stocks revertidos." });
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    return Json(new { status = false, message = "Error al anular: " + ex.Message });
                }
            }
        }

        #endregion

        #region APIs Combos Cabecera
        [HttpGet]
        public JsonResult GetEmpresas() => Json(new { data = _context.Empresas.ToList(), status = true });

        [HttpGet]
        public JsonResult GetSucursalesByEmpresa(int empresaId) =>
            Json(new { data = _context.Sucursales.Where(s => s.EmpresaId == empresaId && s.Estado == true).ToList(), status = true });

        [HttpGet]
        public JsonResult GetAlmacenesBySucursal(int sucursalId, int empresaId) =>
            Json(new { data = _context.Almacenes.Where(a => a.SucursalId == sucursalId && a.EmpresaId == empresaId && a.Estado == true).ToList(), status = true });

        [HttpGet]
        public JsonResult GetMotivosData() => Json(new { data = _context.Motivos.Where(m => m.Estado == true).ToList(), status = true });

        [HttpGet]
        public JsonResult GetMonedaData() => Json(new { data = _context.Monedas.Where(m => m.Estado == true).ToList(), status = true });

        [HttpGet]
        public JsonResult GetTipoDocumentoData() => Json(new { data = _context.TipoDocumentos.Where(t => t.Estado == true).ToList(), status = true });
        #endregion

        #region APIs Filtrado de Productos
        [HttpGet]
        public JsonResult GetProductos()
        {
            try
            {
                return Json(new
                {
                    data = _context.Productos
                        .Where(p => p.EmpresaId == EmpresaUsuarioId && p.Estado == true && !p.Codigo.StartsWith("6")) // <--- CAMBIO AQUÍ
                        .Select(p => new { p.Id, p.Codigo, p.DescripcionProducto, p.DescripcionComercial, p.CodUnidadMedida })
                        .ToList(),
                    status = true
                });
            }
            catch (Exception ex)
            {
                return Json(new { data = (object)null, status = false, message = ex.Message });
            }
        }

        [HttpGet]
        public JsonResult GetStockProductoAlmacen(int? almacenId, int? productoId)
        {
            try
            {
                if (almacenId != null && productoId != null)
                {
                    // Validamos también por empresa para mayor seguridad
                    var stockProducto = _context.StockAlmacenes
                        .Where(sa => sa.AlmacenId == almacenId && sa.ProductoId == productoId && sa.EmpresaId == EmpresaUsuarioId) // <--- CAMBIO AQUÍ
                        .Select(sa => sa.StockActual)
                        .FirstOrDefault();

                    return Json(new { data = stockProducto, status = true, message = "Stock recuperado." });
                }
                return Json(new ApiResponse { data = null, status = false, message = "Datos incompletos." });
            }
            catch (Exception ex)
            {
                return Json(new ApiResponse { data = null, status = false, message = ex.Message });
            }
        }
        #endregion

        #region APIs Auxiliares (Centros Costo, Actividades, Entidades, Kardex)
        [HttpGet]
        public JsonResult GetCentrosCosto()
        {
            var data = _context.CentroCostos.Where(c => c.EsImputable == true && c.Estado == true && c.EmpresaId == EmpresaUsuarioId).ToList(); // <--- CAMBIO AQUÍ
            return Json(new { data = data, status = true });
        }

        [HttpGet]
        public JsonResult GetActividades()
        {
            var data = _context.Actividades.Where(a => a.Estado == true && a.EmpresaId == EmpresaUsuarioId).ToList(); // <--- CAMBIO AQUÍ
            return Json(new { data = data, status = true });
        }

        [HttpGet]
        public JsonResult GetProveedores()
        {
            var miEmpresaId = EmpresaUsuarioId;
            var esGlobal = EsAdminGlobal;

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
            return Json(new { data = proveedores, status = true });
        }
        [HttpGet]
        public JsonResult GenerarKardexValorizado(DateTime fechaInicio, DateTime fechaFin, int almacenId, int productoId, string metodo)
        {
            try
            {
                // 1. OBTENER METADATOS (AHORA CON JOIN DIRECTO)

                // Datos Empresa
                var datosEmpresa = (from a in _context.Almacenes
                                    join s in _context.Sucursales on a.SucursalId equals s.Id
                                    join e in _context.Empresas on s.EmpresaId equals e.Id
                                    where a.Id == almacenId
                                    select new { e.Ruc, e.RazonSocial, NombreSucursal = s.Nombre }).FirstOrDefault();

                // Datos Producto + Grupo + TIPO EXISTENCIA (La clave del éxito)
                var datosProducto = (from p in _context.Productos
                                     join g in _context.Grupos on p.GrupoId equals g.Id
                                     // Join con la nueva tabla
                                     join te in _context.TipoExistencias on g.TipoExistenciaId equals te.Id
                                     join u in _context.UnidadesMedida on p.CodUnidadMedida equals u.Codigo
                                     where p.Id == productoId
                                     select new
                                     {
                                         p.Codigo,
                                         p.DescripcionProducto,
                                         p.DescripcionComercial,
                                         UnidadCodigo = u.Codigo,
                                         UnidadNombre = u.Descripcion,
                                         // Datos directos de BD, cero lógica hardcoded
                                         TipoExistenciaCod = te.Codigo,
                                         TipoExistenciaDesc = te.Nombre
                                     }).FirstOrDefault();

                // VALIDACIÓN DE SEGURIDAD
                // Si el grupo no tiene asignado un tipo de existencia, usamos un valor por defecto para no romper el reporte
                string codigoSunatTabla5 = datosProducto?.TipoExistenciaCod ?? "99";
                string nombreSunatTabla5 = datosProducto?.TipoExistenciaDesc ?? "OTROS (SIN CLASIFICAR)";

                // =================================================================================
                // 2. TU LÓGICA DE MOVIMIENTOS (INTACTA)
                // =================================================================================

                var estadoAprobado = _context.Estados.FirstOrDefault(e => e.Nombre == "Aprobado" && e.Tabla == "INGRESOSALIDAALM");
                if (estadoAprobado == null) throw new Exception("Estado Aprobado no configurado.");

                var movimientos = (from d in _context.DIngresoSalidaAlms
                                   join c in _context.IngresoSalidaAlms on d.IngresoSalidaAlmId equals c.Id
                                   join m in _context.Motivos on c.MotivoId equals m.Id
                                   join td in _context.TipoDocumentos on c.TipoDocumentoId equals td.Id into joinDoc
                                   from docRef in joinDoc.DefaultIfEmpty()
                                   where c.AlmacenId == almacenId && d.ProductoId == productoId
                                           && c.Fecha <= fechaFin && c.EstadoId == estadoAprobado.Id
                                   orderby c.Fecha, c.FechaRegistro
                                   select new
                                   {
                                       c.Fecha,
                                       // IMPORTANTE: Para SUNAT Tabla 10, necesitamos el CÓDIGO del tipo doc (ej: 01, 09), no solo la descripción
                                       CodigoTipoDoc = docRef != null ? docRef.Codigo : "00",
                                       TipoDoc = docRef != null ? docRef.Descripcion : "S/D",
                                       SerieDocumento = c.SerieDocumento != null ? c.SerieDocumento : "S/D",
                                       NumeroDocumento = c.NumeroDocumento != null ? c.NumeroDocumento : "S/D",
                                       // IMPORTANTE: Para SUNAT Tabla 12, necesitamos el CÓDIGO del motivo
                                       CodigoMotivo = m.Codigo,
                                       Motivo = m.Descripcion,
                                       m.TipoMovimiento,
                                       d.Cantidad,
                                       d.Precio,
                                       d.Total
                                   }).ToList();

                decimal saldoCant = 0;
                decimal saldoCostoTotal = 0;
                decimal costoPromedio = 0;
                var capasPeps = new List<(decimal Cantidad, decimal Precio)>();
                var reporte = new List<object>();

                // Variables para el Saldo Anterior
                decimal antCant = 0;
                decimal antCostoTotal = 0;

                foreach (var mov in movimientos)
                {
                    decimal cantMov = mov.Cantidad ?? 0;
                    decimal precioMov = mov.Precio ?? 0;
                    decimal cantEntrada = 0, costoUEntrada = 0, totalEntrada = 0;
                    decimal cantSalida = 0, costoUSalida = 0, totalSalida = 0;

                    if (mov.TipoMovimiento == true) // ENTRADA
                    {
                        cantEntrada = cantMov;
                        costoUEntrada = precioMov;
                        totalEntrada = cantEntrada * costoUEntrada;
                        saldoCant += cantEntrada;
                        saldoCostoTotal += totalEntrada;

                        if (metodo == "PEPS") capasPeps.Add((cantEntrada, costoUEntrada));
                        else costoPromedio = saldoCant > 0 ? saldoCostoTotal / saldoCant : 0;
                    }
                    else // SALIDA
                    {
                        cantSalida = cantMov;
                        if (metodo == "PEPS")
                        {
                            decimal cantPorRetirar = cantSalida;
                            decimal costoAcumuladoSalida = 0;
                            while (cantPorRetirar > 0 && capasPeps.Count > 0)
                            {
                                var capa = capasPeps[0];
                                if (capa.Cantidad <= cantPorRetirar)
                                {
                                    costoAcumuladoSalida += (capa.Cantidad * capa.Precio);
                                    cantPorRetirar -= capa.Cantidad;
                                    capasPeps.RemoveAt(0);
                                }
                                else
                                {
                                    costoAcumuladoSalida += (cantPorRetirar * capa.Precio);
                                    capasPeps[0] = (capa.Cantidad - cantPorRetirar, capa.Precio);
                                    cantPorRetirar = 0;
                                }
                            }
                            totalSalida = costoAcumuladoSalida;
                            costoUSalida = cantSalida > 0 ? totalSalida / cantSalida : 0;
                        }
                        else
                        {
                            costoUSalida = costoPromedio;
                            totalSalida = cantSalida * costoUSalida;
                        }
                        saldoCant -= cantSalida;
                        saldoCostoTotal -= totalSalida;
                        if (metodo == "PROM") costoPromedio = saldoCant > 0 ? saldoCostoTotal / saldoCant : 0;
                    }

                    // LÓGICA DE SALDO ANTERIOR
                    if (mov.Fecha < fechaInicio)
                    {
                        antCant = saldoCant;
                        antCostoTotal = saldoCostoTotal;
                    }
                    else // MOVIMIENTOS DENTRO DEL RANGO
                    {
                        if (reporte.Count == 0 && fechaInicio > movimientos.Min(x => x.Fecha))
                        {
                            reporte.Add(new
                            {
                                fecha = "-",
                                tipoDoc = "-",
                                doc = "-",
                                motivo = "SALDO ANTERIOR AL " + fechaInicio.ToString("dd/MM/yyyy"),
                                eCant = "-",
                                eCosto = "-",
                                eTotal = "-",
                                sCant = "-",
                                sCosto = "-",
                                sTotal = "-",
                                finalCant = antCant.ToString("N2"),
                                finalCosto = (antCant > 0 ? antCostoTotal / antCant : 0).ToString("N4"),
                                finalTotal = antCostoTotal.ToString("N2"),
                                isHeader = true
                            });
                        }

                        reporte.Add(new
                        {
                            fecha = mov.Fecha.GetValueOrDefault().ToString("dd/MM/yyyy"),
                            // AQUÍ USAMOS EL CÓDIGO PARA SUNAT (TABLA 10)
                            tipoDoc = mov.CodigoTipoDoc + " - " + mov.TipoDoc,
                            doc = $"{(mov.SerieDocumento ?? "---")}-{(mov.NumeroDocumento ?? "---")}",
                            // AQUÍ USAMOS EL CÓDIGO DE MOTIVO (TABLA 12) CONCATENADO CON DESCRIPCIÓN
                            motivo = mov.CodigoMotivo + " - " + mov.Motivo,
                            eCant = cantEntrada > 0 ? cantEntrada.ToString("N2") : "-",
                            eCosto = cantEntrada > 0 ? costoUEntrada.ToString("N4") : "-",
                            eTotal = cantEntrada > 0 ? totalEntrada.ToString("N2") : "-",
                            sCant = cantSalida > 0 ? cantSalida.ToString("N2") : "-",
                            sCosto = cantSalida > 0 ? costoUSalida.ToString("N4") : "-",
                            sTotal = cantSalida > 0 ? totalSalida.ToString("N2") : "-",
                            finalCant = saldoCant.ToString("N2"),
                            finalCosto = (metodo == "PEPS" ? (saldoCant > 0 ? saldoCostoTotal / saldoCant : 0) : costoPromedio).ToString("N4"),
                            finalTotal = saldoCostoTotal.ToString("N2"),
                            isHeader = false
                        });
                    }
                }

                // =================================================================================
                // 3. RETORNO CON METADATOS EXTRA
                // =================================================================================

                return Json(new
                {
                    status = true,
                    data = reporte,
                    // Agregamos el objeto extra para llenar la cabecera en el JS
                    infoProducto = new
                    {
                        codigo = datosProducto?.Codigo ?? "S/D",
                        descripcion = datosProducto?.DescripcionProducto ?? "S/D",
                        descripcionComercial = datosProducto?.DescripcionComercial ?? "S/D",
                        unidad = (datosProducto?.UnidadCodigo ?? "NIU") + " - " + (datosProducto?.UnidadNombre ?? "UNIDAD"), // Tabla 6
                        tipo = codigoSunatTabla5 + " - " + nombreSunatTabla5, // Tabla 5 Calculada
                        rucEmpresa = datosEmpresa?.Ruc ?? "",
                        razonSocial = datosEmpresa?.RazonSocial ?? "",
                        sucursal = datosEmpresa?.NombreSucursal ?? ""
                    }
                });
            }
            catch (Exception ex) { return Json(new { status = false, message = ex.Message }); }
        }
        [HttpPost]
        public async Task<JsonResult> GuardarEntidadRapido(Proveedor modelo)
        {
            try
            {
                // 2. Validar si ya existe en esta empresa
                var existe = await _context.Proveedores
                    .AnyAsync(x => x.NumeroDocumento == modelo.NumeroDocumento && x.EmpresaId == EmpresaUsuarioId);

                if (existe)
                    return Json(new { status = false, message = "El número de documento ya está registrado en su empresa." });

                // 3. Asignar valores por defecto
                modelo.EmpresaId = EmpresaUsuarioId;
                modelo.Estado = true;

                _context.Proveedores.Add(modelo);
                await _context.SaveChangesAsync();

                // 4. Retornamos el ID para seleccionarlo automáticamente en el combo
                return Json(new
                {
                    status = true,
                    message = "Entidad registrada correctamente",
                    id = modelo.Id
                });
            }
            catch (Exception ex)
            {
                return Json(new { status = false, message = "Error: " + ex.Message });
            }
        }
        #endregion
        #region VALORIZACION (Amarre Guía - Factura)
        [HttpGet]
        public JsonResult GetIngresosPendientesValorizacion()
        {
            try
            {
                // Buscamos: Entradas (TipoMov=1) Activas (Estado=Aprobado) y Sin Referencia (No valorizadas)
                var estadoAprobado = _context.Estados.FirstOrDefault(e => e.Nombre == "Aprobado" && e.Tabla == "INGRESOSALIDAALM");

                var data = (from i in _context.IngresoSalidaAlms
                            join alm in _context.Almacenes on i.AlmacenId equals alm.Id
                            join prov in _context.Proveedores on i.ProveedorId equals prov.Id // Join opcional si permites ingresos sin proveedor
                            where i.EmpresaId == EmpresaUsuarioId
                               && i.TipoMovimiento == true // Solo Entradas
                               && i.EstadoId == estadoAprobado.Id
                               && (i.TablaReferencia == null || i.TablaReferencia == "") // Lo vital: Que no tenga amarre
                            orderby i.Fecha descending
                            select new
                            {
                                i.Id,
                                Fecha = i.Fecha.HasValue ? i.Fecha.Value.ToString("dd/MM/yyyy") : "-",
                                Numero = i.Numero,
                                Documento = i.SerieDocumento + "-" + i.NumeroDocumento,
                                Almacen = alm.Nombre,
                                ProveedorId = i.ProveedorId,
                                Proveedor = prov.RazonSocial,
                                // Un indicador visual de cuántos items tiene
                                Items = _context.DIngresoSalidaAlms.Count(d => d.IngresoSalidaAlmId == i.Id)
                            }).ToList();

                return Json(new { status = true, data = data });
            }
            catch (Exception ex)
            {
                return Json(new { status = false, message = ex.Message });
            }
        }
        [HttpPost]
        public JsonResult ProcesarValorizacion(int ingresoId, int facturaId)
        {
            using (var transaction = _context.Database.BeginTransaction())
            {
                try
                {
                    // 1. VALIDAR PERTENENCIA A LA EMPRESA (SEGURIDAD)
                    var guia = _context.IngresoSalidaAlms.FirstOrDefault(x => x.Id == ingresoId && x.EmpresaId == EmpresaUsuarioId);
                    var factura = _context.DocumentosPagar.FirstOrDefault(x => x.Id == facturaId && x.EmpresaId == EmpresaUsuarioId);

                    if (guia == null || factura == null)
                        throw new Exception("Documentos no encontrados o no tienes permisos sobre ellos.");

                    if (guia.TipoMovimiento == false)
                        throw new Exception("Solo se pueden valorizar Entradas (Ingresos), no Salidas.");

                    // 2. ACTUALIZAR CABECERA DE LA GUÍA (VINCULACIÓN)
                    guia.IdReferencia = factura.Id;
                    guia.TablaReferencia = "DOCUMENTO_PAGAR";
                    guia.ProveedorId = factura.ProveedorId; // Forzamos que coincida el proveedor
                    guia.MonedaId = factura.MonedaId;       // La guía adopta la moneda de la factura

                    // Nota: Si quisieras sincronizar fechas, este es el momento, pero usualmente 
                    // la fecha de ingreso es distinta a la fecha de emisión de factura.

                    // 3. ACTUALIZAR DETALLES (EL MATCH DE PRECIOS)
                    var detallesGuia = _context.DIngresoSalidaAlms.Where(x => x.IngresoSalidaAlmId == guia.Id).ToList();
                    var detallesFactura = _context.DDocumentosPagar.Where(x => x.DocumentoPagarId == factura.Id).ToList();

                    bool huboCoincidencia = false;

                    foreach (var itemGuia in detallesGuia)
                    {
                        // BUSCAMOS EL PRODUCTO EN LA FACTURA
                        // Usamos FirstOrDefault: Si en la factura hay 2 líneas del mismo producto, tomamos la primera.
                        var itemFacturaMatch = detallesFactura.FirstOrDefault(x => x.ProductoId == itemGuia.ProductoId);

                        if (itemFacturaMatch != null)
                        {
                            huboCoincidencia = true;

                            // A. ACTUALIZAMOS DATOS FINANCIEROS
                            itemGuia.Precio = itemFacturaMatch.PrecioUnitario; // Precio de la factura
                            itemGuia.MonedaId = factura.MonedaId;              // Moneda de la factura
                            itemGuia.TipoCambio = factura.TipoCambio;          // TC de la factura (Vital para Kardex valorizado)

                            // B. RECALCULAMOS TOTALES (Usando la Cantidad REAL de la Guía)
                            decimal cantidadReal = itemGuia.Cantidad ?? 0;
                            decimal precioFactura = itemGuia.Precio ?? 0;

                            itemGuia.Subtotal = cantidadReal * precioFactura;
                        }
                        else
                        {
                            // OPCIONAL: ¿Qué hacemos si el producto llegó pero NO está en la factura?
                            // Opción A: Dejarlo con precio 0 (Coste cero).
                            // Opción B: Lanzar error.
                            // Por ahora, mantenemos el precio con el que se registró (probablemente 0) o no hacemos nada.
                        }
                    }

                    if (!huboCoincidencia)
                    {
                        // Si no coincidió ningún producto, es probable que se hayan equivocado de factura
                        throw new Exception("No se encontraron coincidencias de productos entre la Guía y la Factura seleccionada.");
                    }

                    // 4. GUARDAR CAMBIOS
                    _context.SaveChanges();
                    transaction.Commit();

                    return Json(new { status = true, message = "Valorización aplicada correctamente. El Kardex ha sido actualizado." });
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    return Json(new { status = false, message = "Error al valorizar: " + ex.Message });
                }
            }
        }

        [HttpPost]
        public JsonResult EliminarValorizacion(int ingresoId)
        {
            using (var transaction = _context.Database.BeginTransaction())
            {
                try
                {
                    var guia = _context.IngresoSalidaAlms.FirstOrDefault(x => x.Id == ingresoId && x.EmpresaId == EmpresaUsuarioId);

                    if (guia == null) throw new Exception("Guía no encontrada.");
                    if (string.IsNullOrEmpty(guia.TablaReferencia)) throw new Exception("Esta guía no está valorizada.");

                    // 1. DESVINCULAR CABECERA
                    guia.IdReferencia = null;
                    guia.TablaReferencia = null;

                    // Opcional: ¿Regresar moneda a Soles por defecto? 
                    // guia.MonedaId = 1; 

                    // 2. LIMPIAR PRECIOS EN DETALLES (Volver a Costo Cero)
                    var detalles = _context.DIngresoSalidaAlms.Where(x => x.IngresoSalidaAlmId == guia.Id).ToList();
                    foreach (var det in detalles)
                    {
                        det.Precio = 0;
                        det.Subtotal = 0;
                        det.Igv = 0;
                        det.Total = 0;
                        det.TipoCambio = null; // Quitar TC para que no afecte promedios erróneamente
                    }

                    _context.SaveChanges();
                    transaction.Commit();

                    return Json(new { status = true, message = "Valorización eliminada (Guía desvinculada)." });
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    return Json(new { status = false, message = ex.Message });
                }
            }
        }

        [HttpGet]
        public JsonResult GetReporteStock(int? almacenId)
        {
            try
            {
                // 1. Obtener el estado Aprobado
                var estadoAprobado = _context.Estados.FirstOrDefault(e => e.Nombre == "Aprobado" && e.Tabla == "INGRESOSALIDAALM");
                int idAprobado = estadoAprobado?.Id ?? -1;

                // 2. Calcular el Costo Promedio Ponderado de Entradas (CORREGIDO PARA EF CORE)
                // Paso A: Proyectamos las operaciones básicas antes de agrupar para que SQL lo entienda bien
                var queryCostos = from d in _context.DIngresoSalidaAlms
                                  join c in _context.IngresoSalidaAlms on d.IngresoSalidaAlmId equals c.Id
                                  where c.EmpresaId == EmpresaUsuarioId
                                     && c.TipoMovimiento == true
                                     && c.EstadoId == idAprobado
                                     && (!almacenId.HasValue || c.AlmacenId == almacenId.Value)
                                  select new
                                  {
                                      ProductoId = d.ProductoId,
                                      Cantidad = d.Cantidad ?? 0,
                                      CostoTotalLinea = (d.Cantidad ?? 0) * (d.Precio ?? 0)
                                  };

                // Paso B: Agrupamos y hacemos las sumas (Esto sí se traduce perfecto a SQL)
                var totalesAgrupados = queryCostos
                    .GroupBy(x => x.ProductoId)
                    .Select(g => new
                    {
                        ProductoId = g.Key,
                        TotalCant = g.Sum(x => x.Cantidad),
                        TotalCosto = g.Sum(x => x.CostoTotalLinea)
                    })
                    .ToList(); // <-- Clave: Lo traemos a la memoria de C#

                // Paso C: Hacemos la división final de forma segura en memoria y armamos el diccionario
                var costosPromedio = totalesAgrupados.ToDictionary(
                    x => x.ProductoId,
                    x => x.TotalCant > 0 ? x.TotalCosto / x.TotalCant : 0
                );

                // 3. Consultar la tabla de StockAlmacenes que tiene el SALDO REAL ACTUAL
                var queryStock = from sa in _context.StockAlmacenes
                                 join p in _context.Productos on sa.ProductoId equals p.Id
                                 where sa.EmpresaId == EmpresaUsuarioId
                                    && p.Estado == true
                                    && (!almacenId.HasValue || sa.AlmacenId == almacenId.Value)
                                 select new
                                 {
                                     p.Id,
                                     p.Codigo,
                                     p.CodGrupo,
                                     p.DescripcionGrupo,
                                     p.DescripcionComercial,
                                     p.CodSubgrupo,
                                     p.DescripcionSubgrupo,
                                     p.DescripcionProducto,
                                     p.CodUnidadMedida,
                                     StockReal = sa.StockActual
                                 };

                var listaStockBD = queryStock.ToList();

                // 4. Armar la respuesta cruzando el Stock Real con su Costo Promedio calculado
                var reporte = listaStockBD.Select(s => new
                {
                    s.Codigo,
                    s.CodGrupo,
                    s.DescripcionGrupo,
                    s.DescripcionComercial,
                    s.CodSubgrupo,
                    s.DescripcionSubgrupo,
                    s.DescripcionProducto,
                    s.CodUnidadMedida,
                    Cantidad = s.StockReal,

                    PrecioUnitario = costosPromedio.ContainsKey(s.Id) ? Math.Round(costosPromedio[s.Id], 4) : 0,

                    ValorizacionTotal = Math.Round((s.StockReal * (costosPromedio.ContainsKey(s.Id) ? costosPromedio[s.Id] : 0)).Value, 2)
                }).OrderBy(x => x.DescripcionProducto).ToList();

                return Json(new { data = reporte, message = "Reporte de stock generado exitosamente.", status = true });
            }
            catch (Exception ex)
            {
                return Json(new { data = (object)null, message = ex.Message, status = false });
            }
        }

        #endregion
        #region 4. IMPRESIÓN (SIN VALORIZACIÓN)
        [HttpGet]
        public async Task<IActionResult> Imprimir(int id)
        {
            try
            {
                // 1. OBTENER CABECERA
                var dataCabecera = await (from isa in _context.IngresoSalidaAlms
                                          join e in _context.Empresas on isa.EmpresaId equals e.Id
                                          join a in _context.Almacenes on isa.AlmacenId equals a.Id
                                          join s in _context.Sucursales on isa.SucursalId equals s.Id
                                          join u in _context.Usuarios on isa.UsuarioId equals u.Id
                                          join est in _context.Estados on isa.EstadoId equals est.Id
                                          join mot in _context.Motivos on isa.MotivoId equals mot.Id
                                          // Hacemos LEFT JOIN al proveedor por si es una salida sin entidad asignada
                                          join prov in _context.Proveedores on isa.ProveedorId equals prov.Id into provJoin
                                          from prov in provJoin.DefaultIfEmpty()
                                          where isa.Id == id && isa.EmpresaId == EmpresaUsuarioId
                                          select new
                                          {
                                              Movimiento = isa,
                                              Empresa = e,
                                              Almacen = a.Nombre,
                                              Sucursal = s.Nombre,
                                              Usuario = u,
                                              Estado = est.Nombre,
                                              Motivo = mot.Descripcion,
                                              Proveedor = prov,
                                              // Definimos el título dinámicamente según el tipo de movimiento
                                              TituloImpresion = isa.TipoMovimiento == true ? "INGRESO DE ALMACÉN" : "SALIDA DE ALMACÉN"
                                          }).FirstOrDefaultAsync();

                if (dataCabecera == null) return NotFound("El movimiento no existe o no tiene permisos.");

                // 2. OBTENER DETALLES (Estrictamente campos logísticos, NADA financiero)
                var detalles = await (from d in _context.DIngresoSalidaAlms
                                      where d.IngresoSalidaAlmId == id
                                      orderby d.Item
                                      select new
                                      {
                                          d.Item,
                                          d.CodProducto,
                                          d.DescripcionProducto,
                                          d.CodUnidadMedida,
                                          d.Cantidad
                                      }).ToListAsync();

                // 3. PASAR A LA VISTA (ViewBag)
                ViewBag.Empresa = dataCabecera.Empresa;
                ViewBag.Almacen = dataCabecera.Almacen;
                ViewBag.Sucursal = dataCabecera.Sucursal;
                ViewBag.Usuario = dataCabecera.Usuario;
                ViewBag.Estado = dataCabecera.Estado;
                ViewBag.Motivo = dataCabecera.Motivo;
                ViewBag.Proveedor = dataCabecera.Proveedor;
                ViewBag.TituloImpresion = dataCabecera.TituloImpresion;
                ViewBag.Detalles = detalles;

                return View(dataCabecera.Movimiento);
            }
            catch (Exception ex)
            {
                return Content($"Error al generar el documento: {ex.Message}");
            }
        }
        #endregion
    }
}