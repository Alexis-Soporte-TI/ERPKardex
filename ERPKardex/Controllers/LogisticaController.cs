using ERPKardex.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ERPKardex.Controllers
{
    public class LogisticaController : BaseController
    {
        private readonly ApplicationDbContext _context;

        public LogisticaController(ApplicationDbContext context)
        {
            _context = context;
        }

        public IActionResult Dashboard()
        {
            return View();
        }

        [HttpGet]
        public async Task<JsonResult> GetEmpresas()
        {
            try
            {
                var esGlobal = EsAdminGlobal;

                var data = await _context.Empresas
                    .Where(e => e.Estado == true)
                    .OrderBy(e => e.Nombre)
                    .Select(e => new { e.Id, e.Nombre, e.Ruc })
                    .ToListAsync();

                if (!esGlobal)
                {
                    var miEmpresaId = EmpresaUsuarioId;
                    data = data.Where(e => e.Id == miEmpresaId).ToList();
                }

                return Json(new { status = true, data });
            }
            catch (Exception ex) { return Json(new { status = false, message = ex.Message }); }
        }

        [HttpGet]
        public async Task<JsonResult> GetDashboardData(int? empresaId, int? monedaId, DateTime? fechaInicio, DateTime? fechaFin)
        {
            try
            {
                var miEmpresaId = EmpresaUsuarioId;
                var esGlobal = EsAdminGlobal;
                int idEmpresaFiltro = esGlobal ? (empresaId ?? 0) : miEmpresaId;

                var fInicio = fechaInicio ?? DateTime.Now.AddMonths(-6);
                var fFin = fechaFin ?? DateTime.Now;

                // Moneda visualización (1: Soles, 2: Dólares)
                int monedaVisualizacion = (monedaId.HasValue && monedaId.Value == 2) ? 2 : 1;

                // =================================================================================
                // 1. CONSULTAS BASE (Todas las órdenes en el rango)
                // =================================================================================
                var qOC = _context.OrdenCompras.AsQueryable();
                var qOS = _context.OrdenServicios.AsQueryable();
                var qRC = _context.ReqCompras.AsQueryable();
                var qRS = _context.ReqServicios.AsQueryable();

                if (idEmpresaFiltro > 0)
                {
                    qOC = qOC.Where(x => x.EmpresaId == idEmpresaFiltro);
                    qOS = qOS.Where(x => x.EmpresaId == idEmpresaFiltro);
                    qRC = qRC.Where(x => x.EmpresaId == idEmpresaFiltro);
                    qRS = qRS.Where(x => x.EmpresaId == idEmpresaFiltro);
                }

                qOC = qOC.Where(x => x.FechaEmision >= fInicio && x.FechaEmision <= fFin);
                qOS = qOS.Where(x => x.FechaEmision >= fInicio && x.FechaEmision <= fFin);
                qRC = qRC.Where(x => x.FechaEmision >= fInicio && x.FechaEmision <= fFin);
                qRS = qRS.Where(x => x.FechaEmision >= fInicio && x.FechaEmision <= fFin);

                var estadosDb = await _context.Estados.ToListAsync();

                // Identificamos los estados que representan "Gasto Real" (Aprobado, Atendido Parcial, Atendido Total)
                var nombresValidos = new[] { "Aprobado", "Atendido Parcial", "Atendido Total" };
                var idsValidos = estadosDb.Where(e => nombresValidos.Contains(e.Nombre) && e.Tabla == "ORDEN").Select(e => e.Id).ToList();

                // =================================================================================
                // 2. TORTAS (DISTRIBUCIÓN OPERATIVA)
                // =================================================================================
                // Aquí sí mostramos todos los estados para ver el panorama completo (incluyendo pendientes/anulados)
                var chartRC = (await qRC.GroupBy(x => x.EstadoId).Select(g => new { Id = g.Key, Count = g.Count() }).ToListAsync())
                                .Select(x => new { name = estadosDb.FirstOrDefault(e => e.Id == x.Id)?.Nombre ?? "Desconocido", value = x.Count }).ToList();
                var chartRS = (await qRS.GroupBy(x => x.EstadoId).Select(g => new { Id = g.Key, Count = g.Count() }).ToListAsync())
                                .Select(x => new { name = estadosDb.FirstOrDefault(e => e.Id == x.Id)?.Nombre ?? "Desconocido", value = x.Count }).ToList();
                var chartOC = (await qOC.GroupBy(x => x.EstadoId).Select(g => new { Id = g.Key, Count = g.Count() }).ToListAsync())
                                .Select(x => new { name = estadosDb.FirstOrDefault(e => e.Id == x.Id)?.Nombre ?? "Desconocido", value = x.Count }).ToList();
                var chartOS = (await qOS.GroupBy(x => x.EstadoId).Select(g => new { Id = g.Key, Count = g.Count() }).ToListAsync())
                                .Select(x => new { name = estadosDb.FirstOrDefault(e => e.Id == x.Id)?.Nombre ?? "Desconocido", value = x.Count }).ToList();

                // =================================================================================
                // 3. EVOLUCIÓN MENSUAL (GASTO)
                // =================================================================================

                // Función local de conversión
                decimal Convertir(decimal monto, int monedaOrigen, decimal tc)
                {
                    if (tc <= 0) tc = 1;
                    if (monedaVisualizacion == 1) // A Soles
                    {
                        if (monedaOrigen == 1) return monto;
                        if (monedaOrigen == 2) return monto * tc;
                    }
                    else // A Dólares
                    {
                        if (monedaOrigen == 2) return monto;
                        if (monedaOrigen == 1) return monto / tc;
                    }
                    return monto;
                }

                // Filtramos la data cruda SOLO con estados válidos para calcular el dinero comprometido
                var dataOC = await qOC.Where(x => idsValidos.Contains(x.EstadoId.GetValueOrDefault())).Select(x => new { x.FechaEmision, Total = x.Total ?? 0, x.MonedaId, TC = x.TipoCambio ?? 1 }).ToListAsync();
                var dataOS = await qOS.Where(x => idsValidos.Contains(x.EstadoId.GetValueOrDefault())).Select(x => new { x.FechaEmision, Total = x.Total ?? 0, x.MonedaId, TC = x.TipoCambio ?? 1 }).ToListAsync();

                var fechasUnicas = dataOC.Select(x => new { x.FechaEmision.GetValueOrDefault().Year, x.FechaEmision.GetValueOrDefault().Month })
                    .Union(dataOS.Select(x => new { x.FechaEmision.GetValueOrDefault().Year, x.FechaEmision.GetValueOrDefault().Month }))
                    .OrderBy(x => x.Year).ThenBy(x => x.Month).ToList();

                var categoriasMes = new List<string>();
                var serieCompraEjec = new List<decimal>();
                var serieServEjec = new List<decimal>();

                foreach (var f in fechasUnicas)
                {
                    categoriasMes.Add(new DateTime(f.Year, f.Month, 1).ToString("MMM-yyyy"));

                    var mesOC = dataOC.Where(x => x.FechaEmision.GetValueOrDefault().Year == f.Year && x.FechaEmision.GetValueOrDefault().Month == f.Month);
                    var mesOS = dataOS.Where(x => x.FechaEmision.GetValueOrDefault().Year == f.Year && x.FechaEmision.GetValueOrDefault().Month == f.Month);

                    serieCompraEjec.Add(Math.Round(mesOC.Sum(x => Convertir(x.Total, x.MonedaId.GetValueOrDefault(), x.TC)), 2));
                    serieServEjec.Add(Math.Round(mesOS.Sum(x => Convertir(x.Total, x.MonedaId.GetValueOrDefault(), x.TC)), 2));
                }

                // =================================================================================
                // 4. RETORNO
                // =================================================================================
                return Json(new
                {
                    status = true,
                    kpis = new
                    {
                        montoTotal = serieCompraEjec.Sum() + serieServEjec.Sum(), // Suma solo de lo Aprobado/Atendido
                        countOC = await qOC.CountAsync() + await qOS.CountAsync(), // Conteo total (incluye todo)
                        countReq = await qRC.CountAsync() + await qRS.CountAsync()
                    },
                    charts = new
                    {
                        evolucionCategories = categoriasMes,
                        seriesCompra = serieCompraEjec,
                        seriesServicio = serieServEjec,
                        pieRC = chartRC,
                        pieRS = chartRS,
                        pieOC = chartOC,
                        pieOS = chartOS
                    }
                });
            }
            catch (Exception ex) { return Json(new { status = false, message = ex.Message }); }
        }
    }
}