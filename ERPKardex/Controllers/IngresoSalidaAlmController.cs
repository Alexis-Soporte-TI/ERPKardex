using ERPKardex.Data;
using ERPKardex.Models;
using ERPKardex.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;

namespace ERPKardex.Controllers
{
    public class IngresoSalidaAlmController : Controller
    {
        private readonly ApplicationDbContext _context;
        public IngresoSalidaAlmController(ApplicationDbContext context)
        {
            _context = context;
        }
        #region VISTAS
        public IActionResult Index() => View();
        public IActionResult Registrar() => View();
        public IActionResult ReporteKardex() => View();
        #endregion 

        #region APIs Maestro-Detalle
        [HttpGet]
        public JsonResult GetMovimientosData()
        {
            try
            {
                var movimientos = (from isa in _context.IngresoSalidaAlms
                                   join mot in _context.Motivos on isa.MotivoId equals mot.Id
                                   join est in _context.Estados on isa.EstadoId equals est.Id
                                   join suc in _context.Sucursales on isa.SucursalId equals suc.Id
                                   join alm in _context.Almacenes on isa.AlmacenId equals alm.Id
                                   join emp in _context.Empresas on alm.EmpresaId equals emp.Id
                                   join td in _context.TipoDocumentos on isa.TipoDocumentoId equals td.Id into joinDoc
                                   from tc in joinDoc.DefaultIfEmpty()
                                   join mon in _context.Monedas on isa.MonedaId equals mon.Id into joinMon
                                   from mo in joinMon.DefaultIfEmpty()
                                       // where emp.Id == 1
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
        [HttpPost]
        [HttpPost]
        public JsonResult GuardarMovimiento(IngresoSalidaAlm cabecera, string detallesJson)
        {
            using (var transaction = _context.Database.BeginTransaction())
            {
                try
                {
                    // 1. Generar número correlativo (Algoritmo previo)
                    var ultimoRegistro = _context.IngresoSalidaAlms
                        .OrderByDescending(x => x.Numero)
                        .Select(x => x.Numero).FirstOrDefault();
                    int nuevoCorrelativo = string.IsNullOrEmpty(ultimoRegistro) ? 1 : int.Parse(ultimoRegistro) + 1;

                    cabecera.Numero = nuevoCorrelativo.ToString("D10");
                    cabecera.FechaRegistro = DateTime.Now;

                    _context.IngresoSalidaAlms.Add(cabecera);
                    _context.SaveChanges();

                    if (!string.IsNullOrEmpty(detallesJson))
                    {
                        var listaDetalles = JsonConvert.DeserializeObject<List<DIngresoSalidaAlm>>(detallesJson);

                        foreach (var detalle in listaDetalles)
                        {
                            // --- LÓGICA DE ACTUALIZACIÓN DE STOCK POR ALMACÉN ---

                            // Buscamos si el producto ya existe en ese almacén específico
                            var registroStock = _context.StockAlmacenes
                                .FirstOrDefault(s => s.AlmacenId == cabecera.AlmacenId && s.CodProducto == detalle.CodProducto);

                            if (registroStock == null)
                            {
                                // Si no existe, creamos la "ubicación" en el almacén
                                registroStock = new StockAlmacen
                                {
                                    AlmacenId = cabecera.AlmacenId ?? 0,
                                    CodProducto = detalle.CodProducto,
                                    StockActual = 0,
                                    UltimaActualizacion = DateTime.Now
                                };
                                _context.StockAlmacenes.Add(registroStock);
                            }

                            // Aplicamos el movimiento al stock
                            if (cabecera.TipoMovimiento == true) // 1: ENTRADA
                            {
                                registroStock.StockActual += detalle.Cantidad ?? 0;
                            }
                            else // 0: SALIDA
                            {
                                // OPCIONAL: Validar stock negativo antes de restar
                                if ((registroStock.StockActual - (detalle.Cantidad ?? 0)) < 0)
                                {
                                    throw new Exception($"Stock insuficiente para el producto {detalle.CodProducto}. Stock actual: {registroStock.StockActual}");
                                }
                                registroStock.StockActual -= detalle.Cantidad ?? 0;
                            }

                            registroStock.UltimaActualizacion = DateTime.Now;

                            // --- GUARDADO DEL DETALLE ---
                            detalle.Id = 0;
                            detalle.IngresoSalidaAlmId = cabecera.Id;
                            detalle.FechaRegistro = DateTime.Now;
                            _context.DIngresoSalidaAlms.Add(detalle);
                        }
                        _context.SaveChanges();
                    }

                    transaction.Commit();
                    return Json(new { status = true, message = "Movimiento " + cabecera.Numero + " registrado y stock actualizado." });
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    // El mensaje de "Stock insuficiente" llegará aquí y se mostrará en el SweetAlert
                    return Json(new { status = false, message = "Error: " + (ex.InnerException?.Message ?? ex.Message) });
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
        public JsonResult GetAlmacenesBySucursal(string codSucursal, int empresaId) =>
            Json(new { data = _context.Almacenes.Where(a => a.CodSucursal == codSucursal && a.EmpresaId == empresaId && a.Estado == true).ToList(), status = true });

        [HttpGet]
        public JsonResult GetMotivosData() => Json(new { data = _context.Motivos.Where(m => m.Estado == true).ToList(), status = true });

        [HttpGet]
        public JsonResult GetMonedaData() => Json(new { data = _context.Monedas.Where(m => m.Estado == true).ToList(), status = true });

        [HttpGet]
        public JsonResult GetTipoDocumentoData() => Json(new { data = _context.TipoDocumentos.Where(t => t.Estado == true).ToList(), status = true });
        #endregion

        #region APIs Filtrado de Productos (Cascada)
        [HttpGet]
        public JsonResult GetProductos() => Json(new { data = _context.Productos.Select(p => new { p.Codigo, p.DescripcionProducto, p.CodUnidadMedida }).ToList(), status = true });
        [HttpGet]
        public JsonResult GetStockProductoAlmacen(int? almacenId, string? codProducto)
        {
            try
            {
                if (almacenId != null && !string.IsNullOrEmpty(codProducto))
                {
                    var stockProducto = _context.StockAlmacenes.Where(sa => sa.AlmacenId == almacenId && sa.CodProducto == codProducto).Select(sa => sa.StockActual).FirstOrDefault();
                    return Json(new { data = stockProducto, status = true, message = "Stock de almacén recuperado exitosamente." });
                }

                return Json(new ApiResponse { data = null, status = false, message = "Falta escoger almacén y/o producto." });
            }
            catch (Exception ex)
            {
                return Json(new ApiResponse { data = null, status = false, message = ex.Message });
            }

        }

        #endregion
        [HttpGet]
        public JsonResult GetCentrosCosto()
        {
            try
            {
                var centrosCostosData = _context.CentroCostos.Where(c => c.Estado == true).ToList();
                return Json(new { data = centrosCostosData, status = true, message = "Centros de costo retornados exitosamente" });
            }
            catch (Exception ex)
            {
                return Json(new ApiResponse { data = null, status = false, message = ex.Message });
            }
        }
        [HttpGet]
        public JsonResult GetActividades()
        {
            try
            {
                var actividadesData = _context.Actividades.Where(a => a.Estado == true).ToList();
                return Json(new { data = actividadesData, status = true, message = "Actividades retornadas exitosamente" });
            }
            catch (Exception ex)
            {
                return Json(new ApiResponse { data = null, status = false, message = ex.Message });
            }
        }
        [HttpGet]
        public JsonResult GenerarKardexValorizado(DateTime fechaInicio, DateTime fechaFin, int almacenId, string codProducto, string metodo)
        {
            try
            {
                var movimientos = (from d in _context.DIngresoSalidaAlms
                                   join c in _context.IngresoSalidaAlms on d.IngresoSalidaAlmId equals c.Id
                                   join m in _context.Motivos on c.MotivoId equals m.Id
                                   join td in _context.TipoDocumentos on c.TipoDocumentoId equals td.Id into joinDoc
                                   from docRef in joinDoc.DefaultIfEmpty()
                                   where c.AlmacenId == almacenId && d.CodProducto == codProducto
                                         && c.Fecha <= fechaFin && c.EstadoId == 1
                                   orderby c.Fecha, c.FechaRegistro
                                   select new
                                   {
                                       c.Fecha,
                                       TipoDoc = docRef != null ? docRef.Descripcion : "S/D",
                                       c.SerieDocumento,
                                       c.NumeroDocumento,
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
                        // Si es el primer movimiento del rango, inyectamos la fila de Saldo Anterior
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
                                isHeader = true // Bandera para estilo CSS
                            });
                        }

                        reporte.Add(new
                        {
                            fecha = mov.Fecha.Value.ToString("dd/MM/yyyy"),
                            tipoDoc = mov.TipoDoc,
                            doc = $"{(mov.SerieDocumento ?? "---")}-{(mov.NumeroDocumento ?? "---")}",
                            motivo = mov.Motivo,
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
                return Json(new { status = true, data = reporte });
            }
            catch (Exception ex) { return Json(new { status = false, message = ex.Message }); }
        }
    }
}