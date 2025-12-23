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
                                   join mot in _context.Motivos on isa.CodMotivo equals mot.Codigo
                                   join est in _context.Estados on isa.EstadoId equals est.Id
                                   join suc in _context.Sucursales on isa.SucursalId equals suc.Id
                                   join alm in _context.Almacenes on isa.AlmacenId equals alm.Id
                                   join emp in _context.Empresas on alm.EmpresaId equals emp.Id
                                   join tc in _context.TipoDocumentos on isa.TipoDocumentoId equals tc.Id
                                   join mo in _context.Monedas on isa.MonedaId equals mo.Id
                                   // where emp.Id == 1
                                   select new IngresoSalidaAlmViewModel
                                   {
                                       Id = isa.Id,
                                       Fecha = isa.Fecha,
                                       Numero = isa.Numero,
                                       CodMotivo = isa.CodMotivo,
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
        public JsonResult GuardarMovimiento(IngresoSalidaAlm cabecera, string detallesJson)
        {
            using (var transaction = _context.Database.BeginTransaction())
            {
                try
                {
                    var ultimoRegistro = _context.IngresoSalidaAlms
                        .OrderByDescending(x => x.Numero)
                        .Select(x => x.Numero)
                        .FirstOrDefault();

                    int nuevoCorrelativo = 1;

                    if (!string.IsNullOrEmpty(ultimoRegistro))
                    {
                        if (int.TryParse(ultimoRegistro, out int ultimoNumero))
                        {
                            nuevoCorrelativo = ultimoNumero + 1;
                        }
                    }

                    cabecera.Numero = nuevoCorrelativo.ToString("D10");
                    cabecera.FechaRegistro = DateTime.Now;

                    _context.IngresoSalidaAlms.Add(cabecera);
                    _context.SaveChanges();

                    if (!string.IsNullOrEmpty(detallesJson))
                    {
                        var listaDetalles = JsonConvert.DeserializeObject<List<DIngresoSalidaAlm>>(detallesJson);
                        foreach (var detalle in listaDetalles)
                        {
                            detalle.Id = 0;
                            detalle.IngresoSalidaAlmId = cabecera.Id;
                            detalle.FechaRegistro = DateTime.Now;
                            _context.DIngresoSalidaAlms.Add(detalle);
                        }
                        _context.SaveChanges();
                    }

                    transaction.Commit();
                    return Json(new { status = true, message = "Movimiento registrado con el Nro: " + cabecera.Numero });
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
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
        public JsonResult GetProductos() =>
            Json(new { data = _context.Productos.Select(p => new { p.Codigo, p.DescripcionProducto, p.CodUnidadMedida }).ToList(), status = true });
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
    }
}