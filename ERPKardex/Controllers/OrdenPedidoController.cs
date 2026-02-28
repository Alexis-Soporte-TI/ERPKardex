using ERPKardex.Data;
using ERPKardex.Models;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

namespace ERPKardex.Controllers
{
    public class OrdenPedidoController : BaseController
    {
        private readonly ApplicationDbContext _context;

        public OrdenPedidoController(ApplicationDbContext context)
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

        #region OBTENCIÓN DE DATOS Y COMBOS (APIs)

        [HttpGet]
        public JsonResult GetCombosRegistro()
        {
            try
            {
                var estadoActivo = true;

                var empresas = _context.Empresas.Where(x => x.Estado == estadoActivo && x.Id == EmpresaUsuarioId).Select(x => new { x.Id, x.RazonSocial }).ToList();

                var clientes = _context.Clientes
                    .Where(x => x.Estado == estadoActivo && x.EmpresaId == EmpresaUsuarioId)
                    .Select(x => new
                    {
                        x.Id,
                        x.RazonSocial,
                        x.NumeroDocumento,
                        TipoDocumentoIdentidad = _context.TiposDocumentoIdentidad.FirstOrDefault(t => t.Id == x.TipoDocumentoIdentidadId).Descripcion
                    }).ToList();

                var monedas = _context.Monedas.Where(x => x.Estado == estadoActivo).Select(x => new { x.Id, x.Simbolo, x.Nombre }).ToList();

                var sucursales = _context.Sucursales.Where(x => x.Estado == estadoActivo && x.EmpresaId == EmpresaUsuarioId).Select(x => new { x.Id, x.Nombre }).ToList();

                var almacenes = _context.Almacenes.Where(x => x.Estado == estadoActivo && x.EmpresaId == EmpresaUsuarioId).Select(x => new { x.Id, x.Nombre, x.SucursalId }).ToList();

                return Json(new { status = true, empresas, clientes, monedas, sucursales, almacenes });
            }
            catch (Exception ex) { return Json(new { status = false, message = ex.Message }); }
        }

        [HttpGet]
        public JsonResult GetProductosSelect2(string q)
        {
            try
            {
                var query = _context.Productos.Where(x => x.Estado == true && x.EmpresaId == EmpresaUsuarioId);

                if (!string.IsNullOrEmpty(q))
                {
                    query = query.Where(x => x.DescripcionComercial.Contains(q) || x.Codigo.Contains(q));
                }

                var data = query.Select(x => new
                {
                    id = x.Id,
                    text = x.Codigo + " - " + (x.DescripcionComercial ?? x.DescripcionProducto),
                    codigo = x.Codigo,
                    unidad = x.CodUnidadMedida,
                    descripcion = x.DescripcionComercial ?? x.DescripcionProducto
                }).Take(30).ToList();

                return Json(new { status = true, items = data });
            }
            catch (Exception ex) { return Json(new { status = false, message = ex.Message }); }
        }

        [HttpGet]
        public JsonResult GetOrdenesData(DateTime fechaInicio, DateTime fechaFin, int? clienteId)
        {
            try
            {
                var data = (from o in _context.OrdenesPedido
                            join c in _context.Clientes on o.ClienteId equals c.Id
                            join e in _context.Estados on o.EstadoId equals e.Id
                            join m in _context.Monedas on o.MonedaId equals m.Id
                            where o.EmpresaId == EmpresaUsuarioId
                               && o.FechaEmision >= fechaInicio && o.FechaEmision.Value.Date <= fechaFin
                               && (clienteId == null || o.ClienteId == clienteId)
                            orderby o.FechaEmision descending
                            select new
                            {
                                o.Id,
                                o.Numero,
                                Fecha = o.FechaEmision.Value.ToString("dd/MM/yyyy HH:mm"),
                                Cliente = c.RazonSocial,
                                Ruc = c.NumeroDocumento,
                                Moneda = m.Simbolo,
                                Total = o.Total,
                                Estado = e.Nombre,
                                ColorEstado = e.Nombre == "Aprobado" ? "badge-success" : (e.Nombre == "Anulado" ? "badge-danger" : (e.Nombre == "Generado" ? "badge-secondary" : "badge-warning"))
                            }).ToList();

                return Json(new { status = true, data = data });
            }
            catch (Exception ex) { return Json(new { status = false, message = ex.Message }); }
        }

        #endregion

        #region REGISTRO Y GUARDADO

        [HttpPost]
        public JsonResult GuardarPedido(OrdenPedido orden, string detallesJson)
        {
            using (var transaction = _context.Database.BeginTransaction())
            {
                try
                {
                    if (!EsPeriodoValido(orden.PeriodoContableId ?? 0))
                        throw new Exception("El periodo contable seleccionado ya no se encuentra activo.");

                    if (string.IsNullOrEmpty(detallesJson) || detallesJson == "[]")
                        throw new Exception("Debe agregar al menos un producto al pedido.");

                    orden.EmpresaId = EmpresaUsuarioId;
                    orden.UsuarioCreacionId = UsuarioActualId;
                    orden.FechaRegistro = DateTime.Now;

                    var estadoGenerado = _context.Estados.FirstOrDefault(x => x.Tabla == "ORDEN_PEDIDO" && x.Nombre == "Generado");
                    orden.EstadoId = estadoGenerado?.Id;

                    // Lógica para Correlativo de OP
                    var tipoDoc = _context.TiposDocumentoInterno.FirstOrDefault(x => x.Codigo == "OP");
                    if (tipoDoc != null)
                    {
                        orden.TipoDocumentoInternoId = tipoDoc.Id;
                        int nro = (tipoDoc.UltimoCorrelativo ?? 0) + 1;
                        orden.Numero = "OP-" + nro.ToString("D8");
                        tipoDoc.UltimoCorrelativo = nro;
                    }
                    else
                    {
                        orden.Numero = "OP-AUTO";
                    }

                    _context.OrdenesPedido.Add(orden);
                    _context.SaveChanges();

                    var listaDetalles = JsonConvert.DeserializeObject<List<DOrdenPedido>>(detallesJson);
                    int correlativoItem = 1;

                    foreach (var det in listaDetalles)
                    {
                        det.Id = 0;
                        det.OrdenPedidoId = orden.Id;
                        det.Item = correlativoItem.ToString("D3");
                        det.EmpresaId = EmpresaUsuarioId;
                        det.EstadoId = estadoGenerado?.Id;

                        _context.DetallesOrdenPedido.Add(det);
                        correlativoItem++;
                    }

                    _context.SaveChanges();
                    transaction.Commit();

                    return Json(new { status = true, message = $"Pedido {orden.Numero} registrado correctamente." });
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    return Json(new { status = false, message = ex.Message });
                }
            }
        }

        #endregion

        #region APROBACIÓN E IMPRESIÓN

        [HttpPost]
        public JsonResult AprobarOrden(int id)
        {
            using (var transaction = _context.Database.BeginTransaction())
            {
                try
                {
                    var usuarioId = UsuarioActualId;
                    var orden = _context.OrdenesPedido.Find(id);
                    if (orden == null) throw new Exception("Pedido de venta no encontrado.");

                    var estadoAprobado = _context.Estados.FirstOrDefault(e => e.Nombre == "Aprobado" && e.Tabla == "ORDEN_PEDIDO");
                    var estadoGenerado = _context.Estados.FirstOrDefault(e => e.Nombre == "Generado" && e.Tabla == "ORDEN_PEDIDO");

                    if (orden.EstadoId != estadoGenerado?.Id) throw new Exception("Solo se pueden aprobar pedidos en estado Generado.");

                    // A diferencia de compras, aquí no actualizamos un documento padre (Requerimiento)
                    // porque el pedido de venta nace de cero. Solo aprobamos la orden directamente.

                    orden.EstadoId = estadoAprobado.Id;
                    orden.UsuarioAprobador = usuarioId;
                    orden.FechaAprobacion = DateTime.Now;

                    _context.SaveChanges();
                    transaction.Commit();

                    return Json(new { status = true, message = "Pedido Aprobado exitosamente." });
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    return Json(new { status = false, message = ex.Message });
                }
            }
        }

        [HttpGet]
        public IActionResult Imprimir(int id)
        {
            var orden = _context.OrdenesPedido.FirstOrDefault(x => x.Id == id);
            if (orden == null) return NotFound();

            ViewBag.Empresa = _context.Empresas.FirstOrDefault(x => x.Id == orden.EmpresaId);
            ViewBag.Cliente = _context.Clientes.FirstOrDefault(x => x.Id == orden.ClienteId);
            ViewBag.Moneda = _context.Monedas.FirstOrDefault(x => x.Id == orden.MonedaId);

            var estadoObj = _context.Estados.FirstOrDefault(e => e.Id == orden.EstadoId);
            ViewBag.Estado = estadoObj != null ? estadoObj.Nombre : "";

            var detalles = _context.DetallesOrdenPedido.Where(d => d.OrdenPedidoId == id).ToList();
            ViewBag.Detalles = detalles;

            return View(orden);
        }

        #endregion
    }
}