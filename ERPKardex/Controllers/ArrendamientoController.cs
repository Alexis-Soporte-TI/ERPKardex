using ERPKardex.Data;
using ERPKardex.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ERPKardex.Controllers
{
    public class ArrendamientoController : BaseController
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _env; // Para guardar archivos

        public ArrendamientoController(ApplicationDbContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        #region VISTAS
        public IActionResult Index() => View();
        public IActionResult Calendario() => View(); // Reporte Global

        public IActionResult Registrar(int id = 0)
        {
            ViewBag.Id = id;
            return View();
        }
        #endregion

        #region API AUXILIARES
        [HttpGet]
        public async Task<JsonResult> GetEmpresas()
        {
            // Asumiendo que existe _context.Empresas
            var data = await _context.Empresas
                                     .Where(x => x.Estado == true) // Si tienes campo estado
                                     .Select(x => new { x.Id, x.RazonSocial })
                                     .OrderBy(x => x.RazonSocial)
                                     .ToListAsync();
            return Json(new { status = true, data });
        }
        [HttpGet]
        public async Task<JsonResult> GetMonedas()
        {
            // Asumiendo que existe _context.Monedas
            var data = await _context.Monedas
                                     .Where(x => x.Estado == true) // Si tienes campo estado
                                     .Select(x => new { x.Id, x.Simbolo, x.Nombre })
                                     .OrderBy(x => x.Id)
                                     .ToListAsync();
            return Json(new { status = true, data });
        }
        #endregion

        #region API LISTADO (Reporte Principal)
        [HttpGet]
        public async Task<JsonResult> GetData()
        {
            try
            {
                // YA NO FILTRAMOS POR EmpresaUsuarioId
                var query = from a in _context.Arrendamientos
                            join mo in _context.Monedas on a.MonedaId equals mo.Id
                            join e in _context.Empresas on a.EmpresaId equals e.Id
                            where a.Estado == true
                            orderby e.RazonSocial, a.Id descending
                            select new
                            {
                                a.Id,
                                Empresa = e.RazonSocial, // Nuevo campo para mostrar
                                Moneda = mo.Nombre,
                                MonedaSimbolo = mo.Simbolo,
                                a.DireccionLocal,
                                a.TipoUso,
                                FechaInicio = a.FechaInicioContrato.HasValue ? a.FechaInicioContrato.Value.ToString("dd/MM/yyyy") : "-",
                                FechaFin = a.FechaFinContrato.HasValue ? a.FechaFinContrato.Value.ToString("dd/MM/yyyy") : "INDEFINIDO",
                                Monto = a.MontoAlquiler,
                                DiaPago = a.DiaPago ?? 0,
                                a.Estado
                            };

                return Json(new { status = true, data = await query.ToListAsync() });
            }
            catch (Exception ex) { return Json(new { status = false, message = ex.Message }); }
        }
        #endregion

        #region API CRUD ARRENDAMIENTO
        [HttpGet]
        public async Task<JsonResult> Obtener(int id)
        {
            var ent = await _context.Arrendamientos.FindAsync(id);
            if (ent == null) return Json(new { status = false, message = "No encontrado" });
            return Json(new { status = true, data = ent });
        }

        [HttpPost]
        public async Task<JsonResult> Guardar(Arrendamiento modelo)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                if (modelo.Id == 0)
                {
                    modelo.FechaRegistro = DateTime.Now;
                    modelo.UsuarioRegistro = UsuarioActualId;
                    modelo.Estado = true;
                    _context.Arrendamientos.Add(modelo);
                    await _context.SaveChangesAsync();

                    await GenerarCuotasLogica(modelo.Id);
                }
                else
                {
                    var db = await _context.Arrendamientos.FindAsync(modelo.Id);
                    if (db == null) return Json(new { status = false, message = "No encontrado" });

                    bool fechaFinReducida = modelo.FechaFinContrato.HasValue &&
                                           (!db.FechaFinContrato.HasValue || modelo.FechaFinContrato < db.FechaFinContrato);

                    // CAMBIO: Permitir cambiar la empresa si se equivocaron
                    db.EmpresaId = modelo.EmpresaId;

                    db.DireccionLocal = modelo.DireccionLocal;
                    db.TipoUso = modelo.TipoUso;
                    db.FechaInicioContrato = modelo.FechaInicioContrato;
                    db.FechaFinContrato = modelo.FechaFinContrato;
                    db.MontoGarantia = modelo.MontoGarantia;
                    db.MontoAlquiler = modelo.MontoAlquiler;
                    db.DiaPago = modelo.DiaPago;
                    db.Estado = modelo.Estado;

                    await _context.SaveChangesAsync();

                    if (fechaFinReducida)
                    {
                        var cuotasBasura = await _context.CuotaArrendamientos
                            .Where(x => x.ArrendamientoId == modelo.Id
                                     && x.EstadoPago == 0
                                     && x.FechaVencimiento > modelo.FechaFinContrato)
                            .ToListAsync();

                        if (cuotasBasura.Any())
                        {
                            _context.CuotaArrendamientos.RemoveRange(cuotasBasura);
                            await _context.SaveChangesAsync();
                        }
                    }
                }

                await transaction.CommitAsync();
                return Json(new { status = true, message = "Arrendamiento guardado correctamente." });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return Json(new { status = false, message = "Error: " + ex.Message });
            }
        }
        #endregion

        #region API CUOTAS Y PAGOS (Operación Sensible)

        [HttpPost]
        public async Task<JsonResult> ProyectarCuotas(int id)
        {
            try
            {
                await GenerarCuotasLogica(id);
                return Json(new { status = true, message = "Cuotas proyectadas correctamente." });
            }
            catch (Exception ex) { return Json(new { status = false, message = ex.Message }); }
        }

        private async Task GenerarCuotasLogica(int arrendamientoId)
        {
            var arr = await _context.Arrendamientos.FindAsync(arrendamientoId);
            if (arr == null || arr.Estado != true || arr.MontoAlquiler == null) return;

            DateTime fechaInicio = arr.FechaInicioContrato ?? DateTime.Today;
            DateTime fechaFin = arr.FechaFinContrato ?? DateTime.Today.AddMonths(12); // Si es indefinido, proyectamos 1 año

            // Si es indefinido, siempre aseguramos que haya 12 meses futuros desde HOY
            if (arr.FechaFinContrato == null)
            {
                fechaFin = DateTime.Today.AddMonths(12);
            }

            // Recorremos mes a mes
            var iterator = fechaInicio;
            while (iterator <= fechaFin)
            {
                // Calcular fecha de vencimiento exacta (ej: día 15 del mes)
                int dia = arr.DiaPago ?? 1;
                int diasEnMes = DateTime.DaysInMonth(iterator.Year, iterator.Month);
                int diaReal = dia > diasEnMes ? diasEnMes : dia; // Si paga el 30 y febrero trae 28

                DateTime vencimiento = new DateTime(iterator.Year, iterator.Month, diaReal);
                string periodo = iterator.ToString("yyyy-MM");

                // Verificamos si ya existe para no duplicar (Lazy Check)
                bool existe = await _context.CuotaArrendamientos
                    .AnyAsync(x => x.ArrendamientoId == arrendamientoId && x.PeriodoAnioMes == periodo);

                if (!existe)
                {
                    var nuevaCuota = new CuotaArrendamiento
                    {
                        ArrendamientoId = arrendamientoId,
                        PeriodoAnioMes = periodo,
                        FechaVencimiento = vencimiento,
                        MontoCuota = arr.MontoAlquiler.Value,
                        MonedaId = arr.MonedaId,
                        EstadoPago = 0 // Pendiente
                    };
                    _context.CuotaArrendamientos.Add(nuevaCuota);
                }

                iterator = iterator.AddMonths(1);
            }
            await _context.SaveChangesAsync();
        }

        [HttpGet]
        public async Task<JsonResult> GetCuotas(int id)
        {
            var data = await _context.CuotaArrendamientos
                .Join(_context.Monedas, c => c.MonedaId, m => m.Id, (c, m) => new { c, m })
                .Where(x => x.c.ArrendamientoId == id)
                .OrderBy(x => x.c.FechaVencimiento)
                .Select(x => new
                {
                    x.c.Id,
                    Periodo = x.c.PeriodoAnioMes,
                    Vencimiento = x.c.FechaVencimiento.ToString("dd/MM/yyyy"),
                    MonedaNombre = x.m.Nombre,
                    MonedaSimbolo = x.m.Simbolo,
                    Monto = x.c.MontoCuota,
                    Estado = x.c.EstadoPago, // 0 Rojo, 1 Verde
                    FechaPago = x.c.FechaPago.HasValue ? x.c.FechaPago.Value.ToString("dd/MM/yyyy") : "-",
                    x.c.NumeroOperacion,
                    x.c.RutaEvidencia
                }).ToListAsync();

            return Json(new { status = true, data });
        }

        [HttpPost]
        public async Task<JsonResult> RegistrarPago(int idCuota, string fechaPago, string nroOperacion, IFormFile? archivo)
        {
            try
            {
                var cuota = await _context.CuotaArrendamientos.FindAsync(idCuota);
                if (cuota == null) return Json(new { status = false, message = "Cuota no encontrada" });

                // Subida de Archivo (Si existe)
                string rutaRelativa = "";
                if (archivo != null)
                {
                    string folder = Path.Combine(_env.WebRootPath, "uploads", "arrendamientos");
                    if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);

                    string fileName = Guid.NewGuid().ToString() + Path.GetExtension(archivo.FileName);
                    string filePath = Path.Combine(folder, fileName);

                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await archivo.CopyToAsync(stream);
                    }
                    rutaRelativa = "/uploads/arrendamientos/" + fileName;
                    cuota.RutaEvidencia = rutaRelativa;
                }

                cuota.FechaPago = DateTime.Parse(fechaPago);
                cuota.NumeroOperacion = nroOperacion;
                cuota.MontoPagado = cuota.MontoCuota; // Asumimos pago completo
                cuota.EstadoPago = 1; // VERDE

                await _context.SaveChangesAsync();
                return Json(new { status = true, message = "Pago registrado correctamente." });
            }
            catch (Exception ex) { return Json(new { status = false, message = ex.Message }); }
        }
        #endregion

        #region REPORTE CALENDARIO GLOBAL
        [HttpGet]
        public async Task<JsonResult> GetDataCalendario(DateTime start, DateTime end)
        {
            var eventos = await (from c in _context.CuotaArrendamientos
                                 join m in _context.Monedas on c.MonedaId equals m.Id
                                 join a in _context.Arrendamientos on c.ArrendamientoId equals a.Id
                                 join e in _context.Empresas on a.EmpresaId equals e.Id // JOIN NUEVO
                                 where c.FechaVencimiento >= start && c.FechaVencimiento <= end
                                 // Eliminamos filtro de usuario
                                 select new
                                 {
                                     // CAMBIO: Concatenamos Empresa al Título
                                     title = e.Nombre + " - " + a.DireccionLocal,
                                     start = c.FechaVencimiento.ToString("yyyy-MM-dd"),
                                     color = c.EstadoPago == 1 ? "#28a745" : "#dc3545",
                                     extendedProps = new
                                     {
                                         empresa = e.Nombre,
                                         razonSocial = e.RazonSocial, // Dato extra por si acaso
                                         monto = c.MontoCuota,
                                         monedaNombre = m.Nombre,
                                         monedaSimbolo = m.Simbolo,
                                         estado = c.EstadoPago == 1 ? "PAGADO" : "PENDIENTE"
                                     }
                                 }).ToListAsync();

            return Json(eventos);
        }
        #endregion
    }
}