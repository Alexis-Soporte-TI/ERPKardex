using ERPKardex.Data;
using ERPKardex.Models;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

namespace ERPKardex.Controllers
{
    public class CuentasPorCobrarController : BaseController
    {
        private readonly ApplicationDbContext _context;

        public CuentasPorCobrarController(ApplicationDbContext context)
        {
            _context = context;
        }

        public bool EsPeriodoValido(int periodoId)
        {
            return _context.PeriodosContables.Any(p => p.Id == periodoId && p.Estado == true);
        }

        #region VISTAS
        public IActionResult IndexProvisiones() => View();
        public IActionResult IndexAnticipos() => View();
        public IActionResult IndexNotas() => View();
        public IActionResult RegistroAnticipo() => View();
        public IActionResult RegistroProvision() => View();
        public IActionResult RegistroNotaCredito() => View();
        public IActionResult RegistroNotaDebito() => View();
        public IActionResult AplicacionDocumentos() => View();
        #endregion

        #region APIs DE LISTADO (DATA)

        [HttpGet]
        public JsonResult GetProvisionesData(DateTime fechaInicio, DateTime fechaFin, int? clienteId)
        {
            try
            {
                var codigos = new List<string> { "FAC", "BOL", "RH", "REC" }; // Provisiones de Venta

                var data = (from d in _context.DocumentosCobrar
                            join c in _context.Clientes on d.ClienteId equals c.Id
                            join tdi in _context.TiposDocumentoIdentidad on c.TipoDocumentoIdentidadId equals tdi.Id
                            join t in _context.TiposDocumentoInterno on d.TipoDocumentoInternoId equals t.Id
                            join e in _context.Estados on d.EstadoId equals e.Id
                            join m in _context.Monedas on d.MonedaId equals m.Id
                            // Left Join a la Orden de Pedido
                            join op in _context.OrdenesPedido on d.OrdenPedidoId equals op.Id into opG
                            from op in opG.DefaultIfEmpty()

                            where d.EmpresaId == EmpresaUsuarioId
                               && d.FechaEmision >= fechaInicio && d.FechaEmision.Date <= fechaFin
                               && codigos.Contains(t.Codigo)
                               && (clienteId == null || d.ClienteId == clienteId)
                            orderby d.FechaEmision descending
                            select new
                            {
                                d.Id,
                                TipoDoc = t.Codigo,
                                Documento = d.Serie + "-" + d.Numero,
                                Cliente = c.RazonSocial,
                                TipoDocumentoIdentidad = tdi.Descripcion,
                                NumeroDocumento = c.NumeroDocumento,
                                Fecha = d.FechaEmision.ToString("dd/MM/yyyy HH:mm"),
                                Moneda = m.Simbolo,
                                Total = d.Total,
                                Saldo = d.Saldo, // Para saber cuánto nos falta cobrar
                                Estado = e.Nombre,
                                ColorEstado = e.Nombre == "Cancelado" ? "badge-success" : (e.Nombre == "Anulado" ? "badge-danger" : "badge-warning"),
                                Referencia = op != null ? "PED: " + op.Numero : "-",
                                TotalOrden = op != null ? op.Total : 0,
                            }).ToList();

                return Json(new { status = true, data = data });
            }
            catch (Exception ex) { return Json(new { status = false, message = ex.Message }); }
        }

        [HttpGet]
        public IActionResult ImprimirProvision(int id)
        {
            var documento = _context.DocumentosCobrar.FirstOrDefault(x => x.Id == id);
            if (documento == null) return NotFound();

            ViewBag.Empresa = _context.Empresas.FirstOrDefault(x => x.Id == documento.EmpresaId);
            ViewBag.Cliente = _context.Clientes.FirstOrDefault(x => x.Id == documento.ClienteId);
            ViewBag.Moneda = _context.Monedas.FirstOrDefault(x => x.Id == documento.MonedaId);
            ViewBag.Usuario = _context.Usuarios.FirstOrDefault(u => u.Id == documento.UsuarioRegistroId); // O adaptado a tu lógica User.Identity.Name

            var detalles = (from d in _context.DetallesDocumentoCobrar
                            where d.DocumentoCobrarId == id
                            select new
                            {
                                Item = d.Item,
                                Cantidad = d.Cantidad,
                                UnidadMedida = d.UnidadMedida,
                                Descripcion = d.Descripcion,
                                PrecioUnitario = d.PrecioUnitario,
                                Total = d.Total
                            }).ToList();

            ViewBag.Detalles = detalles;

            var estadoObj = _context.Estados.FirstOrDefault(e => e.Id == documento.EstadoId);
            ViewBag.Estado = estadoObj != null ? estadoObj.Nombre : "";

            var tipoDoc = _context.TiposDocumentoInterno.FirstOrDefault(t => t.Id == documento.TipoDocumentoInternoId);
            ViewBag.NombreDocumento = tipoDoc != null ? tipoDoc.Descripcion.ToUpper() : "DOCUMENTO POR COBRAR";

            return View(documento);
        }

        [HttpGet]
        public JsonResult GetProvisionDetalleJson(int id)
        {
            try
            {
                var cabecera = (from d in _context.DocumentosCobrar
                                join c in _context.Clientes on d.ClienteId equals c.Id
                                join m in _context.Monedas on d.MonedaId equals m.Id
                                join t in _context.TiposDocumentoInterno on d.TipoDocumentoInternoId equals t.Id
                                join op in _context.OrdenesPedido on d.OrdenPedidoId equals op.Id into opG
                                from op in opG.DefaultIfEmpty()
                                where d.Id == id
                                select new
                                {
                                    documento = t.Codigo + " " + d.Serie + "-" + d.Numero,
                                    cliente = c.RazonSocial,
                                    ruc = c.NumeroDocumento,
                                    fecha = d.FechaEmision.ToString("dd/MM/yyyy"),
                                    moneda = m.Simbolo,
                                    subTotal = d.SubTotal,
                                    igv = d.MontoIgv,
                                    total = d.Total,
                                    obs = d.Observacion ?? "-",
                                    referencia = op != null ? "PED: " + op.Numero : "-"
                                }).FirstOrDefault();

                if (cabecera == null) return Json(new { status = false, message = "No encontrado" });

                var detalles = (from det in _context.DetallesDocumentoCobrar
                                where det.DocumentoCobrarId == id
                                select new
                                {
                                    item = det.Item,
                                    producto = det.Descripcion,
                                    unidad = det.UnidadMedida,
                                    cantidad = det.Cantidad ?? 0,
                                    precio = det.PrecioUnitario ?? 0,
                                    importe = det.Total ?? 0
                                }).ToList();

                return Json(new { status = true, cabecera = cabecera, detalles = detalles });
            }
            catch (Exception ex) { return Json(new { status = false, message = ex.Message }); }
        }

        [HttpGet]
        public JsonResult GetAnticiposData(DateTime fechaInicio, DateTime fechaFin, int? clienteId)
        {
            try
            {
                var data = (from d in _context.DocumentosCobrar
                            join c in _context.Clientes on d.ClienteId equals c.Id
                            join t in _context.TiposDocumentoInterno on d.TipoDocumentoInternoId equals t.Id
                            join e in _context.Estados on d.EstadoId equals e.Id
                            join m in _context.Monedas on d.MonedaId equals m.Id
                            join op in _context.OrdenesPedido on d.OrdenPedidoId equals op.Id into opG
                            from op in opG.DefaultIfEmpty()

                            where d.EmpresaId == EmpresaUsuarioId
                               && d.FechaEmision >= fechaInicio && d.FechaEmision.Date <= fechaFin
                               && t.Codigo == "ANT" // Anticipos de clientes
                               && (clienteId == null || d.ClienteId == clienteId)
                            orderby d.FechaEmision descending
                            select new
                            {
                                d.Id,
                                Documento = d.Serie + "-" + d.Numero,
                                Cliente = c.RazonSocial,
                                Fecha = d.FechaEmision.ToString("dd/MM/yyyy HH:mm"),
                                Moneda = m.Simbolo,
                                Total = d.Total,
                                MontoUsado = d.MontoUsado,
                                Saldo = d.Saldo,
                                Estado = e.Nombre,
                                ColorEstado = d.Saldo == 0 ? "badge-success" : "badge-warning",
                                Referencia = op != null ? "PED: " + op.Numero : "-",
                                TotalOrden = op != null ? op.Total : 0,
                            }).ToList();

                return Json(new { status = true, data = data });
            }
            catch (Exception ex) { return Json(new { status = false, message = ex.Message }); }
        }

        [HttpGet]
        public JsonResult GetNotasData(DateTime fechaInicio, DateTime fechaFin, int? clienteId)
        {
            try
            {
                var codigos = new List<string> { "NC", "ND" };

                var data = (from d in _context.DocumentosCobrar
                            join c in _context.Clientes on d.ClienteId equals c.Id
                            join t in _context.TiposDocumentoInterno on d.TipoDocumentoInternoId equals t.Id
                            join e in _context.Estados on d.EstadoId equals e.Id
                            join m in _context.Monedas on d.MonedaId equals m.Id
                            // JOIN CON EL DOCUMENTO PADRE (FACTURA DE VENTA)
                            join docRef in _context.DocumentosCobrar on d.DocumentoReferenciaId equals docRef.Id into docRefG
                            from padre in docRefG.DefaultIfEmpty()
                            join tPadre in _context.TiposDocumentoInterno on padre.TipoDocumentoInternoId equals tPadre.Id into tPadreG
                            from tipoPadre in tPadreG.DefaultIfEmpty()

                            where d.EmpresaId == EmpresaUsuarioId
                               && d.FechaEmision >= fechaInicio && d.FechaEmision.Date <= fechaFin
                               && codigos.Contains(t.Codigo)
                               && (clienteId == null || d.ClienteId == clienteId)
                            orderby d.FechaEmision descending
                            select new
                            {
                                d.Id,
                                Tipo = t.Codigo,
                                Documento = d.Serie + "-" + d.Numero,
                                Cliente = c.RazonSocial,
                                Fecha = d.FechaEmision.ToString("dd/MM/yyyy HH:mm"),
                                Moneda = m.Simbolo,
                                Total = d.Total,
                                Estado = e.Nombre,
                                DocAfectado = padre != null ? (tipoPadre.Codigo + " " + padre.Serie + "-" + padre.Numero) : "---"
                            }).ToList();

                return Json(new { status = true, data = data });
            }
            catch (Exception ex) { return Json(new { status = false, message = ex.Message }); }
        }
        #endregion

        #region 1. UTILITARIOS Y BÚSQUEDAS
        [HttpGet]
        public JsonResult GetCombosRegistro()
        {
            try
            {
                var miEmpresaId = EmpresaUsuarioId;
                var esGlobal = EsAdminGlobal;

                // Hacemos JOIN para obtener el Tipo de Documento
                var clientes = (from p in _context.Clientes
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

                return Json(new { status = true, clientes, monedas });
            }
            catch (Exception ex) { return Json(new { status = false, message = ex.Message }); }
        }

        [HttpGet]
        public JsonResult BuscarOrdenesPendientes()
        {
            try
            {
                var estadosOrdenValidos = new List<string> { "Aprobado", "Atendido Parcial", "Atendido Total" };
                var estadoAnuladoDoc = _context.Estados.FirstOrDefault(x => x.Tabla == "DOCUMENTO_COBRAR" && x.Nombre == "Anulado");
                int idAnulado = estadoAnuladoDoc?.Id ?? -1;
                var codigosFacturables = new List<string> { "FAC", "BOL", "RH", "REC" };

                // Como en ventas solo usamos OrdenPedido, simplificamos la consulta
                var query = from o in _context.OrdenesPedido
                            join c in _context.Clientes on o.ClienteId equals c.Id
                            join tdi in _context.TiposDocumentoIdentidad on c.TipoDocumentoIdentidadId equals tdi.Id
                            join e in _context.Estados on o.EstadoId equals e.Id
                            join m in _context.Monedas on o.MonedaId equals m.Id
                            where estadosOrdenValidos.Contains(e.Nombre)
                            where o.EmpresaId == EmpresaUsuarioId
                            orderby o.FechaEmision descending
                            select new
                            {
                                o.Id,
                                o.Numero,
                                Cliente = c.RazonSocial,
                                TipoDocumentoIdentidad = tdi.Descripcion,
                                NumeroDocumento = c.NumeroDocumento,
                                o.ClienteId,
                                o.MonedaId,
                                MonedaNombre = m.Simbolo,
                                Fecha = o.FechaEmision.Value.ToString("dd/MM/yyyy HH:mm"),
                                SubTotal = o.TotalAfecto,
                                Igv = o.IgvTotal,
                                TotalOrden = o.Total
                            };

                var listaOrdenes = query.ToList();
                var resultado = new List<object>();

                foreach (var item in listaOrdenes)
                {
                    var totalYaFacturado = (from d in _context.DocumentosCobrar
                                            join t in _context.TiposDocumentoInterno on d.TipoDocumentoInternoId equals t.Id
                                            where d.OrdenPedidoId == item.Id
                                               && d.EstadoId != idAnulado
                                               && codigosFacturables.Contains(t.Codigo)
                                            select d.Total).Sum();

                    if (item.TotalOrden > (totalYaFacturado + 0.10m))
                        resultado.Add(item);
                }
                return Json(new { status = true, data = resultado });
            }
            catch (Exception ex) { return Json(new { status = false, message = ex.Message }); }
        }

        [HttpGet]
        public JsonResult GetDetallesOrden(int ordenId)
        {
            try
            {
                var ord = _context.OrdenesPedido.Find(ordenId);
                if (ord == null) throw new Exception("Orden no encontrada.");

                var cabeceraOrden = new
                {
                    CondicionPago = ord.CondicionPago ?? "CONTADO",
                    MonedaId = ord.MonedaId
                };

                var detalles = _context.DetallesOrdenPedido
                    .Where(x => x.OrdenPedidoId == ordenId)
                    .Select(x => new { x.Id, x.Item, Producto = x.Descripcion, x.UnidadMedida, Saldo = x.Cantidad, x.PrecioUnitario, x.Total })
                    .ToList();

                return Json(new { status = true, cabecera = cabeceraOrden, data = detalles });
            }
            catch (Exception ex) { return Json(new { status = false, message = ex.Message }); }
        }

        [HttpGet]
        public JsonResult BuscarFacturasCliente(int clienteId)
        {
            try
            {
                var est = _context.Estados.FirstOrDefault(x => x.Tabla == "DOCUMENTO_COBRAR" && x.Nombre == "Por Pagar"); // En cobranzas es 'Por Cobrar', pero mantengo tu nombre de DB
                var tipos = _context.TiposDocumentoInterno.Where(t => t.Codigo == "FAC" || t.Codigo == "BOL" || t.Codigo == "RH").Select(t => t.Id).ToList();

                var data = _context.DocumentosCobrar
                    .Where(x => x.ClienteId == clienteId && tipos.Contains(x.TipoDocumentoInternoId) && x.EstadoId == est.Id && x.Saldo > 0)
                    .OrderByDescending(x => x.FechaEmision)
                    .Select(x => new { x.Id, x.Serie, x.Numero, Fecha = x.FechaEmision.ToString("dd/MM/yyyy HH:mm"), x.Total, x.Saldo, x.MonedaId })
                    .ToList();

                return Json(new { status = true, data = data });
            }
            catch (Exception ex) { return Json(new { status = false, message = ex.Message }); }
        }
        #endregion

        #region 2. REGISTRO DE ANTICIPO
        [HttpPost]
        public JsonResult GuardarAnticipo(DocumentoCobrar doc)
        {
            using (var transaction = _context.Database.BeginTransaction())
            {
                try
                {
                    if (!EsPeriodoValido(doc.PeriodoContableId ?? 0))
                        throw new Exception("El periodo seleccionado ya no se encuentra abierto o es inválido.");

                    if (doc.OrdenPedidoId == null)
                        throw new Exception("El anticipo de venta requiere un Pedido asociado.");

                    if (doc.TipoCambio == null || doc.TipoCambio == 0)
                        throw new Exception("El tipo de cambio no es válido.");

                    doc.EmpresaId = EmpresaUsuarioId;
                    doc.UsuarioRegistroId = UsuarioActualId;
                    doc.FechaRegistro = DateTime.Now;

                    var estPorCobrar = _context.Estados.FirstOrDefault(x => x.Tabla == "DOCUMENTO_COBRAR" && x.Nombre == "Por Pagar"); // Ajusta el string si creaste "Por Cobrar"
                    var tipoAnticipo = _context.TiposDocumentoInterno.First(x => x.Codigo == "ANT");

                    doc.EstadoId = estPorCobrar?.Id;
                    doc.Saldo = doc.Total;
                    doc.TipoDocumentoInternoId = tipoAnticipo.Id;
                    doc.Serie = "ANT";

                    // Correlativo automático D8
                    var ultimoDoc = _context.DocumentosCobrar
                        .Where(x => x.EmpresaId == EmpresaUsuarioId && x.TipoDocumentoInternoId == tipoAnticipo.Id)
                        .OrderByDescending(x => x.Id)
                        .FirstOrDefault();

                    int nroSiguiente = 1;
                    if (ultimoDoc != null && int.TryParse(ultimoDoc.Numero, out int ultimoNro))
                    {
                        nroSiguiente = ultimoNro + 1;
                    }

                    doc.Numero = nroSiguiente.ToString("D8");

                    _context.DocumentosCobrar.Add(doc);
                    _context.SaveChanges();
                    transaction.Commit();

                    return Json(new { status = true, message = $"Anticipo {doc.Serie}-{doc.Numero} registrado correctamente." });
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    return Json(new { status = false, message = ex.Message });
                }
            }
        }
        #endregion

        #region 3. REGISTRO DE PROVISIÓN (FACTURA / BOLETA)
        [HttpPost]
        public JsonResult GuardarProvision(DocumentoCobrar doc, string codigoTipoDoc, string detallesJson)
        {
            using (var transaction = _context.Database.BeginTransaction())
            {
                try
                {
                    if (!EsPeriodoValido(doc.PeriodoContableId ?? 0))
                        throw new Exception("El periodo seleccionado ya no se encuentra abierto o es inválido.");

                    if (codigoTipoDoc == "PROV") throw new Exception("Provisión requiere documento físico.");
                    if (doc.OrdenPedidoId == null) throw new Exception("Requiere Pedido de Venta.");
                    if (doc.TipoCambio == null || doc.TipoCambio == 0) throw new Exception("El tipo de cambio no es válido.");

                    // Validación Financiera
                    var estadoAnulado = _context.Estados.FirstOrDefault(x => x.Tabla == "DOCUMENTO_COBRAR" && x.Nombre == "Anulado");
                    int idAnulado = estadoAnulado?.Id ?? -1;
                    var codigosFacturables = new List<string> { "FAC", "BOL", "RH", "REC" };

                    var orden = _context.OrdenesPedido.Find(doc.OrdenPedidoId);
                    decimal totalOrden = orden.Total ?? 0;

                    decimal totalPrevio = (from d in _context.DocumentosCobrar
                                           join t in _context.TiposDocumentoInterno on d.TipoDocumentoInternoId equals t.Id
                                           where d.OrdenPedidoId == doc.OrdenPedidoId && d.EstadoId != idAnulado && codigosFacturables.Contains(t.Codigo)
                                           select d.Total).Sum() ?? 0;

                    if (doc.Total > ((totalOrden - totalPrevio) + 1m))
                        throw new Exception("Monto excede saldo pendiente del pedido.");

                    // Guardar Cabecera
                    var estadoInicial = _context.Estados.FirstOrDefault(x => x.Tabla == "DOCUMENTO_COBRAR" && x.Nombre == "Por Pagar"); // O "Por Cobrar"

                    doc.EmpresaId = EmpresaUsuarioId;
                    doc.UsuarioRegistroId = UsuarioActualId;
                    doc.FechaRegistro = DateTime.Now;
                    doc.EstadoId = estadoInicial?.Id;
                    doc.Saldo = doc.Total;
                    doc.TipoDocumentoInternoId = _context.TiposDocumentoInterno.First(x => x.Codigo == codigoTipoDoc).Id;

                    _context.DocumentosCobrar.Add(doc);
                    _context.SaveChanges();

                    // Guardar Detalles
                    var listaDetalles = JsonConvert.DeserializeObject<List<DDocumentoCobrar>>(detallesJson);
                    int correlativoItem = 1;

                    foreach (var det in listaDetalles)
                    {
                        det.Id = 0;
                        det.DocumentoCobrarId = doc.Id;
                        det.Item = correlativoItem.ToString("D3");

                        // Recuperamos la info del origen (Pedido de Venta)
                        if (det.IdReferencia != null)
                        {
                            var itemOrigen = _context.DetallesOrdenPedido.Find(det.IdReferencia);
                            if (itemOrigen != null)
                            {
                                det.ProductoId = itemOrigen.ProductoId;
                                det.Descripcion = itemOrigen.Descripcion;
                                det.UnidadMedida = itemOrigen.UnidadMedida;
                                det.TablaReferencia = "DORDEN_PEDIDO";
                            }
                        }

                        _context.DetallesDocumentoCobrar.Add(det);
                        correlativoItem++;
                    }

                    _context.SaveChanges();
                    transaction.Commit();

                    return Json(new { status = true, message = "Comprobante registrado correctamente." });
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    return Json(new { status = false, message = ex.Message });
                }
            }
        }
        #endregion

        #region 4. MÓDULO DE APLICACIÓN

        [HttpGet]
        public JsonResult GetDocumentosParaAplicacion(int clienteId)
        {
            try
            {
                var estadoPorPagar = _context.Estados.FirstOrDefault(x => x.Tabla == "DOCUMENTO_COBRAR" && x.Nombre == "Por Pagar"); // Por Cobrar
                var estadoDisponible = _context.Estados.FirstOrDefault(x => x.Tabla == "DOCUMENTO_COBRAR" && x.Nombre == "Disponible");
                int idActivo = estadoPorPagar?.Id ?? 0;

                // 1. PENDIENTES (FAC, BOL, RH) - Izquierda
                var pendientes = (from d in _context.DocumentosCobrar
                                  join t in _context.TiposDocumentoInterno on d.TipoDocumentoInternoId equals t.Id
                                  join op in _context.OrdenesPedido on d.OrdenPedidoId equals op.Id into opG
                                  from op in opG.DefaultIfEmpty()
                                  where d.ClienteId == clienteId
                                     && d.EstadoId == idActivo
                                     && d.Saldo > 0
                                     && (t.Codigo == "FAC" || t.Codigo == "BOL" || t.Codigo == "RH")
                                  select new
                                  {
                                      d.Id,
                                      Documento = d.Serie + "-" + d.Numero,
                                      Tipo = t.Codigo,
                                      Fecha = d.FechaEmision.ToString("dd/MM/yyyy HH:mm"),
                                      TotalOriginal = d.Total,
                                      SaldoActual = d.Saldo,
                                      OrdenNumero = op != null ? op.Numero : "--",
                                      OrdenId = d.OrdenPedidoId
                                  }).ToList();

                // 2. DISPONIBLES (ANT) - Derecha
                var disponibles = (from d in _context.DocumentosCobrar
                                   join t in _context.TiposDocumentoInterno on d.TipoDocumentoInternoId equals t.Id
                                   join op in _context.OrdenesPedido on d.OrdenPedidoId equals op.Id into opG
                                   from op in opG.DefaultIfEmpty()
                                   where d.ClienteId == clienteId
                                      && d.EstadoId == estadoDisponible.Id
                                      && d.Saldo == 0
                                      && (d.Total - (d.MontoUsado ?? 0) > 0)
                                      && (t.Codigo == "ANT")
                                   select new
                                   {
                                       d.Id,
                                       Documento = d.Serie + "-" + d.Numero,
                                       Tipo = t.Codigo,
                                       Fecha = d.FechaEmision.ToString("dd/MM/yyyy HH:mm"),
                                       d.Total,
                                       Saldo = d.Total - (d.MontoUsado ?? 0),
                                       OrdenNumero = op != null ? op.Numero : "--",
                                       OrdenId = d.OrdenPedidoId
                                   }).ToList();

                return Json(new { status = true, pendientes = pendientes, disponibles = disponibles });
            }
            catch (Exception ex) { return Json(new { status = false, message = ex.Message }); }
        }

        [HttpGet]
        public JsonResult GetHistorialDocumento(int documentoId)
        {
            try
            {
                var historial = new List<object>();

                // 1. CRUCES (Pagos con Anticipos)
                var cruces = (from a in _context.AplicacionesDocumentoCobrar
                              join docAbono in _context.DocumentosCobrar on a.DocumentoAbonoId equals docAbono.Id
                              join tipo in _context.TiposDocumentoInterno on docAbono.TipoDocumentoInternoId equals tipo.Id
                              where a.DocumentoCargoId == documentoId
                              select new
                              {
                                  Fecha = a.FechaAplicacion,
                                  Concepto = "COBRO / APLICACIÓN",
                                  Documento = tipo.Codigo + " " + docAbono.Serie + "-" + docAbono.Numero,
                                  Monto = a.MontoAplicado * -1,
                                  Color = "text-success"
                              }).ToList();

                // 2. NOTAS DE DÉBITO
                var notasDebito = (from d in _context.DocumentosCobrar
                                   join t in _context.TiposDocumentoInterno on d.TipoDocumentoInternoId equals t.Id
                                   where d.DocumentoReferenciaId == documentoId && t.Codigo == "ND"
                                   select new
                                   {
                                       Fecha = d.FechaRegistro ?? DateTime.Now,
                                       Concepto = "CARGO ADICIONAL (ND)",
                                       Documento = t.Codigo + " " + d.Serie + "-" + d.Numero,
                                       Monto = d.Total,
                                       Color = "text-danger"
                                   }).ToList();

                historial.AddRange(cruces);
                historial.AddRange(notasDebito);

                var resultado = historial.OrderByDescending(x => ((dynamic)x).Fecha)
                    .Select(x => new
                    {
                        Fecha = ((dynamic)x).Fecha.ToString("dd/MM/yyyy HH:mm"),
                        ((dynamic)x).Concepto,
                        Doc = ((dynamic)x).Documento,
                        Monto = ((decimal)((dynamic)x).Monto).ToString("N2"),
                        ((dynamic)x).Color
                    }).ToList();

                return Json(new { status = true, data = resultado });
            }
            catch (Exception ex) { return Json(new { status = false, message = ex.Message }); }
        }

        [HttpPost]
        public JsonResult GuardarAplicacion(int idCargo, int idAbono, decimal montoAplicar)
        {
            using (var transaction = _context.Database.BeginTransaction())
            {
                try
                {
                    if (montoAplicar <= 0) throw new Exception("Monto inválido");

                    var estadoPagado = _context.Estados.FirstOrDefault(x => x.Tabla == "DOCUMENTO_COBRAR" && x.Nombre == "Cancelado");
                    var estadoAgotado = _context.Estados.FirstOrDefault(x => x.Tabla == "DOCUMENTO_COBRAR" && x.Nombre == "Agotado");

                    var c = _context.DocumentosCobrar.Find(idCargo);
                    var a = _context.DocumentosCobrar.Find(idAbono);

                    if (c.Saldo < montoAplicar)
                        throw new Exception($"El monto excede la deuda pendiente del comprobante ({c.Saldo}).");

                    decimal disponibleAnticipo = (a.Total ?? 0) - (a.MontoUsado ?? 0);
                    if (disponibleAnticipo < montoAplicar)
                        throw new Exception($"El anticipo solo tiene {disponibleAnticipo} disponible para aplicar.");

                    c.Saldo -= montoAplicar;
                    if (c.Saldo <= 0)
                    {
                        c.EstadoId = estadoPagado?.Id;
                    }

                    a.MontoUsado = (a.MontoUsado ?? 0) + montoAplicar;

                    if (((a.Total ?? 0) - a.MontoUsado) <= 0)
                    {
                        a.EstadoId = estadoAgotado?.Id;
                    }

                    _context.AplicacionesDocumentoCobrar.Add(new DocumentoCobrarAplicacion
                    {
                        EmpresaId = EmpresaUsuarioId,
                        DocumentoCargoId = idCargo,
                        DocumentoAbonoId = idAbono,
                        MontoAplicado = montoAplicar,
                        FechaAplicacion = DateTime.Now,
                        UsuarioId = UsuarioActualId
                    });

                    _context.SaveChanges();
                    transaction.Commit();
                    return Json(new { status = true, message = "Aplicación exitosa." });
                }
                catch (Exception ex) { transaction.Rollback(); return Json(new { status = false, message = ex.Message }); }
            }
        }
        #endregion

        #region 5. REGISTRO NOTAS
        [HttpPost]
        public JsonResult GuardarNotaCreditoDebito(DocumentoCobrar doc, string codigoTipoDoc)
        {
            using (var transaction = _context.Database.BeginTransaction())
            {
                try
                {
                    if (!EsPeriodoValido(doc.PeriodoContableId ?? 0))
                        throw new Exception("El periodo seleccionado ya no se encuentra abierto o es inválido.");

                    if (doc.TipoCambio == null || doc.TipoCambio == 0)
                        throw new Exception("El tipo de cambio no es válido.");

                    doc.EmpresaId = EmpresaUsuarioId;
                    doc.UsuarioRegistroId = UsuarioActualId;
                    doc.FechaRegistro = DateTime.Now;

                    var estFin = _context.Estados.FirstOrDefault(x => x.Tabla == "DOCUMENTO_COBRAR" && x.Nombre == "Cancelado");
                    var tipo = _context.TiposDocumentoInterno.First(x => x.Codigo == codigoTipoDoc);
                    doc.TipoDocumentoInternoId = tipo.Id;

                    if (doc.DocumentoReferenciaId == null) throw new Exception("Falta referencia.");
                    var docPadre = _context.DocumentosCobrar.Find(doc.DocumentoReferenciaId);

                    if (codigoTipoDoc == "NC")
                    {
                        doc.EstadoId = estFin?.Id;
                        doc.Saldo = 0;
                        _context.DocumentosCobrar.Add(doc);
                        _context.SaveChanges();

                        if (doc.Total > docPadre.Saldo)
                            throw new Exception("El monto de la Nota de Crédito excede el saldo de la factura.");

                        docPadre.Saldo -= doc.Total;

                        _context.AplicacionesDocumentoCobrar.Add(new DocumentoCobrarAplicacion
                        {
                            EmpresaId = EmpresaUsuarioId,
                            UsuarioId = UsuarioActualId,
                            FechaAplicacion = DateTime.Now,
                            DocumentoCargoId = docPadre.Id,
                            DocumentoAbonoId = doc.Id,
                            MontoAplicado = doc.Total ?? 0
                        });
                    }
                    else if (codigoTipoDoc == "ND")
                    {
                        doc.EstadoId = estFin?.Id;
                        doc.Saldo = 0;
                        _context.DocumentosCobrar.Add(doc);
                        docPadre.Saldo += doc.Total;
                    }

                    if (docPadre.Saldo <= 0)
                        docPadre.EstadoId = estFin?.Id;
                    else
                        docPadre.EstadoId = _context.Estados.First(x => x.Tabla == "DOCUMENTO_COBRAR" && x.Nombre == "Por Pagar").Id; // O "Por Cobrar"

                    _context.SaveChanges();
                    transaction.Commit();
                    return Json(new { status = true, message = "Nota registrada." });
                }
                catch (Exception ex) { transaction.Rollback(); return Json(new { status = false, message = ex.Message }); }
            }
        }
        #endregion
    }
}