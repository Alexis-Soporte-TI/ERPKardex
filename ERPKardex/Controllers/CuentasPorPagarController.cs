using ERPKardex.Data;
using ERPKardex.Models;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

namespace ERPKardex.Controllers
{
    public class CuentasPorPagarController : BaseController
    {
        private readonly ApplicationDbContext _context;

        public CuentasPorPagarController(ApplicationDbContext context)
        {
            _context = context;
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
        public JsonResult GetProvisionesData(DateTime fechaInicio, DateTime fechaFin, int? proveedorId)
        {
            try
            {
                var codigos = new List<string> { "FAC", "BOL", "RH", "REC" }; // Solo Provisiones

                var data = (from d in _context.DocumentosPagar
                            join p in _context.Proveedores on d.ProveedorId equals p.Id
                            join tdi in _context.TiposDocumentoIdentidad on p.TipoDocumentoIdentidadId equals tdi.Id
                            join t in _context.TiposDocumentoInterno on d.TipoDocumentoInternoId equals t.Id
                            join e in _context.Estados on d.EstadoId equals e.Id
                            join m in _context.Monedas on d.MonedaId equals m.Id
                            // Left Joins para Referencia de Orden
                            join oc in _context.OrdenCompras on d.OrdenCompraId equals oc.Id into ocG
                            from oc in ocG.DefaultIfEmpty()
                            join os in _context.OrdenServicios on d.OrdenServicioId equals os.Id into osG
                            from os in osG.DefaultIfEmpty()

                            where d.EmpresaId == EmpresaUsuarioId
                               && d.FechaEmision >= fechaInicio && d.FechaEmision.Value.Date <= fechaFin
                               && codigos.Contains(t.Codigo)
                               && (proveedorId == null || d.ProveedorId == proveedorId)
                            orderby d.FechaEmision descending
                            select new
                            {
                                d.Id,
                                TipoDoc = t.Codigo,
                                Documento = d.Serie + "-" + d.Numero,
                                Proveedor = p.RazonSocial,
                                TipoDocumentoIdentidad = tdi.Descripcion,
                                NumeroDocumento = p.NumeroDocumento,
                                Fecha = d.FechaEmision.GetValueOrDefault().ToString("dd/MM/yyyy HH:mm"),
                                Moneda = m.Simbolo,
                                Total = d.Total,
                                Saldo = d.Saldo, // VITAL: Para saber cuánto falta pagar
                                Estado = e.Nombre,
                                ColorEstado = e.Nombre == "Cancelado" ? "badge-success" : (e.Nombre == "Anulado" ? "badge-danger" : "badge-warning"),
                                // Referencia a la Orden
                                Referencia = oc != null ? "OC: " + oc.Numero : (os != null ? "OS: " + os.Numero : "-"),
                                TotalOrden = oc.Total,
                            }).ToList();

                return Json(new { status = true, data = data });
            }
            catch (Exception ex) { return Json(new { status = false, message = ex.Message }); }
        }

        [HttpGet]
        public IActionResult ImprimirProvision(int id)
        {
            // 1. Obtener el Documento Principal
            var documento = _context.DocumentosPagar.FirstOrDefault(x => x.Id == id);
            if (documento == null) return NotFound();

            // 2. Obtener Datos Relacionados (Llenamos los ViewBag para la vista)

            // Empresa (Asumiendo que tienes una forma de obtener la empresa del usuario actual)
            // Si no tienes el ID en sesión, úsalo directo del documento si existe la relación
            ViewBag.Empresa = _context.Empresas.FirstOrDefault(x => x.Id == documento.EmpresaId);

            // Proveedor
            ViewBag.Proveedor = _context.Proveedores.FirstOrDefault(x => x.Id == documento.ProveedorId);

            // Moneda
            ViewBag.Moneda = _context.Monedas.FirstOrDefault(x => x.Id == documento.MonedaId);

            // Usuario (El que creó el documento o el actual)
            ViewBag.Usuario = _context.Usuarios.FirstOrDefault(u => u.Nombre == User.Identity.Name); // O tu lógica de usuario

            // Detalles (Items) - Usamos dynamic o la clase si la tienes mapeada
            var detalles = (from d in _context.DDocumentosPagar
                            where d.DocumentoPagarId == id
                            select new
                            {
                                Item = d.Item,
                                Cantidad = d.Cantidad,
                                UnidadMedida = d.UnidadMedida,
                                Descripcion = d.Descripcion,
                                PrecioUnitario = d.PrecioUnitario,
                                Total = d.Total
                            }).ToList();

            // Convertimos a lista de objetos dinámicos para que la vista no se queje
            ViewBag.Detalles = detalles;

            // Estado para mostrar en el sello
            var estadoObj = _context.Estados.FirstOrDefault(e => e.Id == documento.EstadoId);
            ViewBag.Estado = estadoObj != null ? estadoObj.Nombre : "";

            // Tipos de documento para mostrar si es FACTURA, BOLETA, ETC.
            var tipoDoc = _context.TiposDocumentoInterno.FirstOrDefault(t => t.Id == documento.TipoDocumentoInternoId);
            ViewBag.NombreDocumento = tipoDoc != null ? tipoDoc.Descripcion.ToUpper() : "DOCUMENTO POR PAGAR";

            return View(documento);
        }

        [HttpGet]
        public JsonResult GetProvisionDetalleJson(int id)
        {
            try
            {
                // 1. Datos de Cabecera (Objeto Anónimo)
                var cabecera = (from d in _context.DocumentosPagar
                                join p in _context.Proveedores on d.ProveedorId equals p.Id
                                join m in _context.Monedas on d.MonedaId equals m.Id
                                join t in _context.TiposDocumentoInterno on d.TipoDocumentoInternoId equals t.Id
                                // Joins para referencias (Opcionales)
                                join oc in _context.OrdenCompras on d.OrdenCompraId equals oc.Id into ocG
                                from oc in ocG.DefaultIfEmpty()
                                join os in _context.OrdenServicios on d.OrdenServicioId equals os.Id into osG
                                from os in osG.DefaultIfEmpty()
                                where d.Id == id
                                select new
                                {
                                    documento = t.Codigo + " " + d.Serie + "-" + d.Numero,
                                    proveedor = p.RazonSocial,
                                    ruc = p.NumeroDocumento,
                                    fecha = d.FechaEmision.Value.ToString("dd/MM/yyyy"),
                                    moneda = m.Simbolo,
                                    subTotal = d.SubTotal,
                                    igv = d.MontoIgv,
                                    total = d.Total,
                                    obs = d.Observacion ?? "-",
                                    referencia = oc != null ? "O/C: " + oc.Numero : (os != null ? "O/S: " + os.Numero : "-")
                                }).FirstOrDefault();

                if (cabecera == null) return Json(new { status = false, message = "No encontrado" });

                // 2. Datos del Detalle (Lista Anónima)
                // Asegúrate que _context.DetalleDocumentosPagar es el nombre correcto en tu DbContext
                var detalles = (from det in _context.DDocumentosPagar
                                where det.DocumentoPagarId == id
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
            catch (Exception ex)
            {
                return Json(new { status = false, message = ex.Message });
            }
        }

        [HttpGet]
        public JsonResult GetAnticiposData(DateTime fechaInicio, DateTime fechaFin, int? proveedorId)
        {
            try
            {
                var data = (from d in _context.DocumentosPagar
                            join p in _context.Proveedores on d.ProveedorId equals p.Id
                            join t in _context.TiposDocumentoInterno on d.TipoDocumentoInternoId equals t.Id
                            join e in _context.Estados on d.EstadoId equals e.Id
                            join m in _context.Monedas on d.MonedaId equals m.Id
                            // Anticipos SIEMPRE tienen orden (según tu regla de negocio)
                            join oc in _context.OrdenCompras on d.OrdenCompraId equals oc.Id into ocG
                            from oc in ocG.DefaultIfEmpty()
                            join os in _context.OrdenServicios on d.OrdenServicioId equals os.Id into osG
                            from os in osG.DefaultIfEmpty()

                            where d.EmpresaId == EmpresaUsuarioId
                               && d.FechaEmision >= fechaInicio && d.FechaEmision.Value.Date <= fechaFin
                               && t.Codigo == "ANT" // Solo Anticipos
                               && (proveedorId == null || d.ProveedorId == proveedorId)
                            orderby d.FechaEmision descending
                            select new
                            {
                                d.Id,
                                Documento = d.Serie + "-" + d.Numero,
                                Proveedor = p.RazonSocial,
                                Fecha = d.FechaEmision.Value.ToString("dd/MM/yyyy HH:mm"),
                                Moneda = m.Simbolo,
                                Total = d.Total,
                                MontoUsado = d.MontoUsado,
                                Saldo = d.Saldo,
                                Estado = e.Nombre,
                                ColorEstado = d.Saldo == 0 ? "badge-success" : "badge-warning", // Verde si ya se pagó
                                Referencia = oc != null ? "OC: " + oc.Numero : (os != null ? "OS: " + os.Numero : "-"),
                                TotalOrden = oc.Total,
                            }).ToList();

                return Json(new { status = true, data = data });
            }
            catch (Exception ex) { return Json(new { status = false, message = ex.Message }); }
        }

        [HttpGet]
        public JsonResult GetNotasData(DateTime fechaInicio, DateTime fechaFin, int? proveedorId)
        {
            try
            {
                var codigos = new List<string> { "NC", "ND" };

                var data = (from d in _context.DocumentosPagar
                            join p in _context.Proveedores on d.ProveedorId equals p.Id
                            join t in _context.TiposDocumentoInterno on d.TipoDocumentoInternoId equals t.Id
                            join e in _context.Estados on d.EstadoId equals e.Id
                            join m in _context.Monedas on d.MonedaId equals m.Id
                            // JOIN CON EL DOCUMENTO PADRE (FACTURA)
                            join docRef in _context.DocumentosPagar on d.DocumentoReferenciaId equals docRef.Id into docRefG
                            from padre in docRefG.DefaultIfEmpty()
                            join tPadre in _context.TiposDocumentoInterno on padre.TipoDocumentoInternoId equals tPadre.Id into tPadreG
                            from tipoPadre in tPadreG.DefaultIfEmpty()

                            where d.EmpresaId == EmpresaUsuarioId
                               && d.FechaEmision >= fechaInicio && d.FechaEmision.Value.Date <= fechaFin
                               && codigos.Contains(t.Codigo)
                               && (proveedorId == null || d.ProveedorId == proveedorId)
                            orderby d.FechaEmision descending
                            select new
                            {
                                d.Id,
                                Tipo = t.Codigo,
                                Documento = d.Serie + "-" + d.Numero,
                                Proveedor = p.RazonSocial,
                                Fecha = d.FechaEmision.Value.ToString("dd/MM/yyyy HH:mm"),
                                Moneda = m.Simbolo,
                                Total = d.Total,
                                Estado = e.Nombre,
                                // Referencia Visual: A qué factura afecta
                                DocAfectado = padre != null ? (tipoPadre.Codigo + " " + padre.Serie + "-" + padre.Numero) : "---"
                            }).ToList();

                return Json(new { status = true, data = data });
            }
            catch (Exception ex) { return Json(new { status = false, message = ex.Message }); }
        }
        #endregion
        #region 1. UTILITARIOS Y BÚSQUEDAS (Sin cambios)
        // ... (Se mantienen igual BuscarOrdenesPendientes, GetDetallesOrden, BuscarFacturasProveedor) ...
        [HttpGet]
        public JsonResult BuscarOrdenesPendientes(string tipoOrigen)
        {
            try
            {
                var estadosOrdenValidos = new List<string> { "Aprobado", "Atendido Parcial", "Atendido Total" };
                var estadoAnuladoDoc = _context.Estados.FirstOrDefault(x => x.Tabla == "DOCUMENTO_PAGAR" && x.Nombre == "Anulado");
                int idAnulado = estadoAnuladoDoc?.Id ?? -1;
                var codigosFacturables = new List<string> { "FAC", "BOL", "RH", "REC" };

                if (tipoOrigen == "OC")
                {
                    var query = from o in _context.OrdenCompras
                                join p in _context.Proveedores on o.ProveedorId equals p.Id
                                join tdi in _context.TiposDocumentoIdentidad on p.TipoDocumentoIdentidadId equals tdi.Id
                                join e in _context.Estados on o.EstadoId equals e.Id
                                join m in _context.Monedas on o.MonedaId equals m.Id
                                where estadosOrdenValidos.Contains(e.Nombre)
                                where o.EmpresaId == EmpresaUsuarioId
                                orderby o.FechaEmision descending
                                select new
                                {
                                    o.Id,
                                    o.Numero,
                                    Proveedor = p.RazonSocial,
                                    TipoDocumentoIdentidad = tdi.Descripcion,
                                    p.NumeroDocumento,
                                    o.ProveedorId,
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
                        var totalYaFacturado = (from d in _context.DocumentosPagar
                                                join t in _context.TiposDocumentoInterno on d.TipoDocumentoInternoId equals t.Id
                                                where d.OrdenCompraId == item.Id
                                                   && d.EstadoId != idAnulado
                                                   && codigosFacturables.Contains(t.Codigo)
                                                select d.Total).Sum();

                        if (item.TotalOrden > (totalYaFacturado + 0.10m))
                            resultado.Add(item);
                    }
                    return Json(new { status = true, data = resultado });
                }
                else
                {
                    var query = from o in _context.OrdenServicios
                                join p in _context.Proveedores on o.ProveedorId equals p.Id
                                join tdi in _context.TiposDocumentoIdentidad on p.TipoDocumentoIdentidadId equals tdi.Id
                                join e in _context.Estados on o.EstadoId equals e.Id
                                join m in _context.Monedas on o.MonedaId equals m.Id
                                where estadosOrdenValidos.Contains(e.Nombre)
                                where o.EmpresaId == EmpresaUsuarioId
                                orderby o.FechaEmision descending
                                select new
                                {
                                    o.Id,
                                    o.Numero,
                                    Proveedor = p.RazonSocial,
                                    TipoDocumentoIdentidad = tdi.Descripcion,
                                    p.NumeroDocumento,
                                    o.ProveedorId,
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
                        var totalYaFacturado = (from d in _context.DocumentosPagar
                                                join t in _context.TiposDocumentoInterno on d.TipoDocumentoInternoId equals t.Id
                                                where d.OrdenServicioId == item.Id
                                                   && d.EstadoId != idAnulado
                                                   && codigosFacturables.Contains(t.Codigo)
                                                select d.Total).Sum();

                        if (item.TotalOrden > (totalYaFacturado + 0.10m))
                            resultado.Add(item);
                    }
                    return Json(new { status = true, data = resultado });
                }
            }
            catch (Exception ex) { return Json(new { status = false, message = ex.Message }); }
        }

        [HttpGet]
        public JsonResult GetDetallesOrden(int ordenId, string tipoOrigen)
        {
            try
            {
                object cabeceraOrden = null;
                object detalles = null;

                if (tipoOrigen == "OC")
                {
                    // Obtenemos cabecera para sacar la Condición de Pago
                    var ord = _context.OrdenCompras.Find(ordenId);
                    cabeceraOrden = new
                    {
                        CondicionPago = ord.CondicionPago ?? "CONTADO",
                        MonedaId = ord.MonedaId
                    };

                    detalles = _context.DOrdenCompras.Where(x => x.OrdenCompraId == ordenId)
                        .Select(x => new { x.Id, x.Item, Producto = x.Descripcion, x.UnidadMedida, Saldo = x.Cantidad, x.PrecioUnitario, x.Total }).ToList();
                }
                else
                {
                    var ord = _context.OrdenServicios.Find(ordenId);
                    cabeceraOrden = new
                    {
                        CondicionPago = ord.CondicionPago ?? "CONTADO",
                        MonedaId = ord.MonedaId
                    };

                    detalles = _context.DOrdenServicios.Where(x => x.OrdenServicioId == ordenId)
                        .Select(x => new { x.Id, x.Item, Producto = x.Descripcion, x.UnidadMedida, Saldo = x.Cantidad, x.PrecioUnitario, x.Total }).ToList();
                }

                return Json(new { status = true, cabecera = cabeceraOrden, data = detalles });
            }
            catch (Exception ex) { return Json(new { status = false, message = ex.Message }); }
        }

        [HttpGet]
        public JsonResult BuscarFacturasProveedor(int proveedorId)
        {
            try
            {
                var est = _context.Estados.FirstOrDefault(x => x.Tabla == "DOCUMENTO_PAGAR" && x.Nombre == "Por Pagar");
                var tipos = _context.TiposDocumentoInterno.Where(t => t.Codigo == "FAC" || t.Codigo == "BOL" || t.Codigo == "RH").Select(t => t.Id).ToList();
                var data = _context.DocumentosPagar.Where(x => x.ProveedorId == proveedorId && tipos.Contains(x.TipoDocumentoInternoId) && x.EstadoId == est.Id && x.Saldo > 0)
                    .OrderByDescending(x => x.FechaEmision).Select(x => new { x.Id, x.Serie, x.Numero, Fecha = x.FechaEmision.Value.ToString("dd/MM/yyyy HH:mm"), x.Total, x.Saldo, x.MonedaId }).ToList();
                return Json(new { status = true, data = data });
            }
            catch (Exception ex) { return Json(new { status = false, message = ex.Message }); }
        }
        #endregion

        #region 2. REGISTRO DE ANTICIPO
        [HttpPost]
        public JsonResult GuardarAnticipo(DocumentoPagar doc, string tipoOrigenOrden)
        {
            using (var transaction = _context.Database.BeginTransaction())
            {
                try
                {
                    if (doc.OrdenCompraId == null && doc.OrdenServicioId == null)
                        throw new Exception("Anticipo requiere Orden.");

                    if (doc.TipoCambio == null || doc.TipoCambio == 0) throw new Exception("El tipo de cambio no es válido.");

                    doc.EmpresaId = EmpresaUsuarioId;
                    doc.UsuarioRegistroId = UsuarioActualId;
                    doc.FechaRegistro = DateTime.Now;
                    //doc.FechaEmision = DateTime.Now; // Fecha del sistema o la que elija el usuario

                    // 1. Configurar Estados y Tipo
                    var estPorPagar = _context.Estados.FirstOrDefault(x => x.Tabla == "DOCUMENTO_PAGAR" && x.Nombre == "Por Pagar");
                    var tipoAnticipo = _context.TiposDocumentoInterno.First(x => x.Codigo == "ANT");

                    doc.EstadoId = estPorPagar.Id;
                    doc.Saldo = doc.Total;
                    doc.TipoDocumentoInternoId = tipoAnticipo.Id;

                    // ---------------------------------------------------------
                    // 2. GENERACIÓN DE CORRELATIVO (Serie ANT - Número D8)
                    // ---------------------------------------------------------
                    doc.Serie = "ANT";

                    // Buscamos el último documento de tipo ANT registrado en la empresa
                    var ultimoDoc = _context.DocumentosPagar
                        .Where(x => x.EmpresaId == EmpresaUsuarioId && x.TipoDocumentoInternoId == tipoAnticipo.Id)
                        .OrderByDescending(x => x.Id) // Ordenamos por ID para obtener el último insertado
                        .FirstOrDefault();

                    int nroSiguiente = 1; // Valor por defecto si es el primero

                    // Si existe uno previo, intentamos leer su número y sumar 1
                    if (ultimoDoc != null && int.TryParse(ultimoDoc.Numero, out int ultimoNro))
                    {
                        nroSiguiente = ultimoNro + 1;
                    }

                    // Asignamos con formato de 8 dígitos (Ej: 00000001, 00000002...)
                    doc.Numero = nroSiguiente.ToString("D8");
                    // ---------------------------------------------------------

                    _context.DocumentosPagar.Add(doc);
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

        #region 3. REGISTRO DE PROVISIÓN
        [HttpPost]
        public JsonResult GuardarProvision(DocumentoPagar doc, string codigoTipoDoc, string tipoOrigenOrden, string detallesJson)
        {
            using (var transaction = _context.Database.BeginTransaction())
            {
                try
                {
                    // 1. Validaciones
                    if (codigoTipoDoc == "PROV") throw new Exception("Provisión requiere documento físico.");
                    if (doc.OrdenCompraId == null && doc.OrdenServicioId == null) throw new Exception("Requiere Orden.");
                    if (doc.TipoCambio == null || doc.TipoCambio == 0) throw new Exception("El tipo de cambio no es válido.");

                    // 2. Validación Financiera (Misma lógica que ya teníamos)
                    decimal totalOrden = 0, totalPrevio = 0;
                    var estadoAnulado = _context.Estados.FirstOrDefault(x => x.Tabla == "DOCUMENTO_PAGAR" && x.Nombre == "Anulado");
                    int idAnulado = estadoAnulado?.Id ?? -1;
                    var codigosFacturables = new List<string> { "FAC", "BOL", "RH", "REC" };

                    if (doc.OrdenCompraId != null)
                    {
                        var orden = _context.OrdenCompras.Find(doc.OrdenCompraId);
                        totalOrden = orden.Total ?? 0;
                        totalPrevio = (from d in _context.DocumentosPagar
                                       join t in _context.TiposDocumentoInterno on d.TipoDocumentoInternoId equals t.Id
                                       where d.OrdenCompraId == doc.OrdenCompraId && d.EstadoId != idAnulado && codigosFacturables.Contains(t.Codigo)
                                       select d.Total).Sum();
                    }
                    else
                    {
                        var orden = _context.OrdenServicios.Find(doc.OrdenServicioId);
                        totalOrden = orden.Total ?? 0;
                        totalPrevio = (from d in _context.DocumentosPagar
                                       join t in _context.TiposDocumentoInterno on d.TipoDocumentoInternoId equals t.Id
                                       where d.OrdenServicioId == doc.OrdenServicioId && d.EstadoId != idAnulado && codigosFacturables.Contains(t.Codigo)
                                       select d.Total).Sum();
                    }

                    if (doc.Total > ((totalOrden - totalPrevio) + 1m))
                        throw new Exception("Monto excede saldo pendiente de la orden.");

                    // 3. Guardar Cabecera
                    var estadoInicial = _context.Estados.FirstOrDefault(x => x.Tabla == "DOCUMENTO_PAGAR" && x.Nombre == "Por Pagar");

                    doc.EmpresaId = EmpresaUsuarioId;
                    doc.UsuarioRegistroId = UsuarioActualId;
                    doc.FechaRegistro = DateTime.Now;
                    //doc.FechaEmision = DateTime.Now;
                    doc.EstadoId = estadoInicial.Id;
                    doc.Saldo = doc.Total;
                    doc.TipoDocumentoInternoId = _context.TiposDocumentoInterno.First(x => x.Codigo == codigoTipoDoc).Id;

                    _context.DocumentosPagar.Add(doc);
                    _context.SaveChanges(); // Obtenemos el ID del documento

                    // 4. Guardar Detalles (CORREGIDO: ASIGNACIÓN DE DATOS MAESTROS)
                    var listaDetalles = JsonConvert.DeserializeObject<List<DDocumentoPagar>>(detallesJson);
                    int correlativoItem = 1;

                    foreach (var det in listaDetalles)
                    {
                        det.Id = 0;
                        det.DocumentoPagarId = doc.Id;
                        det.Item = correlativoItem.ToString("D3");

                        // --- CORRECCIÓN CRÍTICA AQUÍ ---
                        // Recuperamos la info del origen para no perder la trazabilidad ni el ProductoId
                        if (det.IdReferencia != null)
                        {
                            if (tipoOrigenOrden == "OC")
                            {
                                var itemOrigen = _context.DOrdenCompras.Find(det.IdReferencia);
                                if (itemOrigen != null)
                                {
                                    det.ProductoId = itemOrigen.ProductoId;       // CLAVE para Almacén
                                    det.Descripcion = itemOrigen.Descripcion;
                                    det.UnidadMedida = itemOrigen.UnidadMedida;   // Mantenemos consistencia
                                    det.TablaReferencia = "DORDENCOMPRA";         // Para saber de dónde vino
                                }
                            }
                            else // OS
                            {
                                var itemOrigen = _context.DOrdenServicios.Find(det.IdReferencia);
                                if (itemOrigen != null)
                                {
                                    det.ProductoId = itemOrigen.ProductoId;
                                    det.Descripcion = itemOrigen.Descripcion;
                                    det.UnidadMedida = itemOrigen.UnidadMedida;
                                    det.TablaReferencia = "DORDENSERVICIO";
                                }
                            }
                        }
                        // -------------------------------

                        _context.DDocumentosPagar.Add(det);
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

        #region 4. MÓDULO DE APLICACIÓN (CORREGIDO CON ORDEN NUMERO)

        [HttpGet]
        public JsonResult GetDocumentosParaAplicacion(int proveedorId)
        {
            try
            {
                var estadoPorPagar = _context.Estados.FirstOrDefault(x => x.Tabla == "DOCUMENTO_PAGAR" && x.Nombre == "Por Pagar");
                var estadoDisponible = _context.Estados.FirstOrDefault(x => x.Tabla == "DOCUMENTO_PAGAR" && x.Nombre == "Disponible");
                int idActivo = estadoPorPagar?.Id ?? 0;

                // 1. PENDIENTES (FAC, BOL, RH) - Izquierda
                // Traemos Numero de Orden para visualización
                var pendientes = (from d in _context.DocumentosPagar
                                  join t in _context.TiposDocumentoInterno on d.TipoDocumentoInternoId equals t.Id
                                  // Left Joins Manuales para obtener numero de orden
                                  join oc in _context.OrdenCompras on d.OrdenCompraId equals oc.Id into ocG
                                  from oc in ocG.DefaultIfEmpty()
                                  join os in _context.OrdenServicios on d.OrdenServicioId equals os.Id into osG
                                  from os in osG.DefaultIfEmpty()
                                  where d.ProveedorId == proveedorId
                                     && d.EstadoId == idActivo
                                     && d.Saldo > 0
                                     && (t.Codigo == "FAC" || t.Codigo == "BOL" || t.Codigo == "RH")
                                  select new
                                  {
                                      d.Id,
                                      Documento = d.Serie + "-" + d.Numero,
                                      Tipo = t.Codigo,
                                      Fecha = d.FechaEmision.Value.ToString("dd/MM/yyyy HH:mm"),
                                      TotalOriginal = d.Total,
                                      SaldoActual = d.Saldo,
                                      OrdenNumero = oc != null ? oc.Numero : (os != null ? os.Numero : "--"),
                                      OrdenId = d.OrdenCompraId ?? d.OrdenServicioId // ID para el match
                                  }).ToList();

                // 2. DISPONIBLES (ANT) - Derecha
                // También traemos Numero de Orden para mostrar en lugar del ID
                var disponibles = (from d in _context.DocumentosPagar
                                   join t in _context.TiposDocumentoInterno on d.TipoDocumentoInternoId equals t.Id
                                   join oc in _context.OrdenCompras on d.OrdenCompraId equals oc.Id into ocG
                                   from oc in ocG.DefaultIfEmpty()
                                   join os in _context.OrdenServicios on d.OrdenServicioId equals os.Id into osG
                                   from os in osG.DefaultIfEmpty()
                                   where d.ProveedorId == proveedorId
                                      && d.EstadoId == estadoDisponible.Id
                                      && d.Saldo == 0
                                      && (d.Total - (d.MontoUsado ?? 0) > 0)
                                      && (t.Codigo == "ANT")
                                   select new
                                   {
                                       d.Id,
                                       Documento = d.Serie + "-" + d.Numero,
                                       Tipo = t.Codigo,
                                       Fecha = d.FechaEmision.Value.ToString("dd/MM/yyyy HH:mm"),
                                       d.Total,
                                       Saldo = d.Total - (d.MontoUsado ?? 0),
                                       OrdenNumero = oc != null ? oc.Numero : (os != null ? os.Numero : "--"),
                                       OrdenId = d.OrdenCompraId ?? d.OrdenServicioId // ID para el match
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

                // 1. CRUCES (Pagos con Anticipos o Notas de Crédito que reducen deuda)
                var cruces = (from a in _context.DocumentoPagarAplicaciones
                              join docAbono in _context.DocumentosPagar on a.DocumentoAbonoId equals docAbono.Id
                              join tipo in _context.TiposDocumentoInterno on docAbono.TipoDocumentoInternoId equals tipo.Id
                              where a.DocumentoCargoId == documentoId
                              select new
                              {
                                  Fecha = a.FechaAplicacion,
                                  Concepto = "PAGO / APLICACIÓN",
                                  Documento = tipo.Codigo + " " + docAbono.Serie + "-" + docAbono.Numero,
                                  Monto = a.MontoAplicado * -1, // Negativo porque reduce
                                  Color = "text-success"
                              }).ToList();

                // 2. NOTAS DE DÉBITO (Cargos adicionales que aumentan deuda)
                var notasDebito = (from d in _context.DocumentosPagar
                                   join t in _context.TiposDocumentoInterno on d.TipoDocumentoInternoId equals t.Id
                                   where d.DocumentoReferenciaId == documentoId && t.Codigo == "ND"
                                   select new
                                   {
                                       Fecha = d.FechaRegistro ?? DateTime.Now,
                                       Concepto = "CARGO ADICIONAL (ND)",
                                       Documento = t.Codigo + " " + d.Serie + "-" + d.Numero,
                                       Monto = d.Total, // Positivo porque suma
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
                    var estadoPorPagar = _context.Estados.FirstOrDefault(x => x.Tabla == "DOCUMENTO_PAGAR" && x.Nombre == "Por Pagar");
                    var estadoAgotado = _context.Estados.FirstOrDefault(x => x.Tabla == "DOCUMENTO_PAGAR" && x.Nombre == "Agotado");
                    var estadoPagado = _context.Estados.FirstOrDefault(x => x.Tabla == "DOCUMENTO_PAGAR" && x.Nombre == "Cancelado");

                    var c = _context.DocumentosPagar.Find(idCargo);
                    var a = _context.DocumentosPagar.Find(idAbono);

                    if (c.Saldo < montoAplicar)
                        throw new Exception($"El monto excede la deuda pendiente del comprobante ({c.Saldo}).");

                    decimal disponibleAnticipo = a.Total - (a.MontoUsado ?? 0);
                    if (disponibleAnticipo < montoAplicar)
                        throw new Exception($"El anticipo solo tiene {disponibleAnticipo} disponible para aplicar.");

                    c.Saldo -= montoAplicar;
                    if (c.Saldo <= 0)
                    {
                        c.EstadoId = estadoPagado?.Id; // Se canceló la deuda
                    }

                    a.MontoUsado = (a.MontoUsado ?? 0) + montoAplicar;

                    // Si ya se consumió todo el anticipo, cambiamos estado visual
                    if ((a.Total - a.MontoUsado) <= 0)
                    {
                        a.EstadoId = estadoAgotado?.Id; // Estado "Agotado"
                    }

                    _context.DocumentoPagarAplicaciones.Add(new DocumentoPagarAplicacion
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
        public JsonResult GuardarNotaCreditoDebito(DocumentoPagar doc, string codigoTipoDoc)
        {
            using (var transaction = _context.Database.BeginTransaction())
            {
                try
                {
                    if (doc.TipoCambio == null || doc.TipoCambio == 0) throw new Exception("El tipo de cambio no es válido.");

                    doc.EmpresaId = EmpresaUsuarioId;
                    doc.UsuarioRegistroId = UsuarioActualId;
                    //doc.FechaEmision = DateTime.Now;
                    doc.FechaRegistro = DateTime.Now;

                    var estFin = _context.Estados.FirstOrDefault(x => x.Tabla == "DOCUMENTO_PAGAR" && x.Nombre == "Cancelado");
                    var tipo = _context.TiposDocumentoInterno.First(x => x.Codigo == codigoTipoDoc);
                    doc.TipoDocumentoInternoId = tipo.Id;

                    if (doc.DocumentoReferenciaId == null) throw new Exception("Falta referencia.");
                    var docPadre = _context.DocumentosPagar.Find(doc.DocumentoReferenciaId);

                    if (codigoTipoDoc == "NC")
                    {
                        doc.EstadoId = estFin.Id; doc.Saldo = 0;
                        _context.DocumentosPagar.Add(doc); _context.SaveChanges();
                        if (doc.Total > docPadre.Saldo) throw new Exception("Monto NC excede saldo.");
                        docPadre.Saldo -= doc.Total;
                        _context.DocumentoPagarAplicaciones.Add(new DocumentoPagarAplicacion
                        {
                            EmpresaId = EmpresaUsuarioId,
                            UsuarioId = UsuarioActualId,
                            FechaAplicacion = DateTime.Now,
                            DocumentoCargoId = docPadre.Id,
                            DocumentoAbonoId = doc.Id,
                            MontoAplicado = doc.Total
                        });
                    }
                    else if (codigoTipoDoc == "ND")
                    {
                        doc.EstadoId = estFin.Id; doc.Saldo = 0;
                        _context.DocumentosPagar.Add(doc);
                        docPadre.Saldo += doc.Total;
                    }

                    if (docPadre.Saldo <= 0) docPadre.EstadoId = estFin.Id;
                    else docPadre.EstadoId = _context.Estados.First(x => x.Tabla == "DOCUMENTO_PAGAR" && x.Nombre == "Por Pagar").Id;

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