using ERPKardex.Data;
using ERPKardex.Models;
using ERPKardex.Models.ViewModels;
using ERPKardex.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ERPKardex.Controllers
{
    // Heredamos de tu BaseController para usar EmpresaUsuarioId y UsuarioActualId
    public class ContabilidadController : BaseController
    {
        private readonly ApplicationDbContext _context;

        public ContabilidadController(ApplicationDbContext context)
        {
            _context = context;
        }

        // 1. GET: Renderiza la vista vacía
        public IActionResult CrearAsiento()
        {
            // Ya no hardcodeamos el 1. Lo pasamos a la vista por si acaso lo necesita el JS
            ViewBag.EmpresaId = EmpresaUsuarioId;
            return View();
        }

        // 2. GET: Llena los combos usando la sesión activa
        [HttpGet]
        public async Task<JsonResult> GetDatosCabecera()
        {
            // Usamos EmpresaUsuarioId de tu BaseController
            var periodos = await _context.PeriodosContables
                .Where(p => p.EmpresaId == EmpresaUsuarioId && p.Estado == true)
                .OrderByDescending(p => p.Anio).ThenByDescending(p => p.Mes)
                .Select(p => new { id = p.Id, texto = $"{p.Mes:D2}-{p.Anio}" })
                .ToListAsync();

            var origenes = await _context.OrigenesAsiento
                .Where(o => o.Estado == true)
                .Select(o => new { id = o.Id, texto = o.Codigo + " - " + o.Descripcion })
                .ToListAsync();

            var monedas = await _context.Monedas
                .Select(m => new { id = m.Id, texto = m.Nombre })
                .ToListAsync();

            return Json(new { periodos, origenes, monedas });
        }

        [HttpGet]
        public async Task<JsonResult> GetCuentas()
        {
            var cuentas = await _context.CuentasContables
                .Where(c => c.EmpresaId == EmpresaUsuarioId && c.EsMovimiento == true && c.Estado == true)
                .OrderBy(c => c.Codigo)
                .Select(c => new { id = c.Id, codigo = c.Codigo, nombre = c.Nombre })
                .ToListAsync();

            return Json(cuentas);
        }

        // =========================================================================================
        // 3. LA MAGIA DEL BORRADOR: Endpoint para importar una compra y armar el asiento precargado
        // =========================================================================================
        [HttpGet]
        public async Task<JsonResult> GenerarBorradorDocumentoPagar(int documentoPagarId)
        {
            try
            {
                // Buscamos la provisión operativa
                var documento = await _context.DocumentosPagar
                    .FirstOrDefaultAsync(d => d.Id == documentoPagarId && d.EmpresaId == EmpresaUsuarioId);

                if (documento == null)
                    return Json(new { status = false, message = "Documento no encontrado o no pertenece a la empresa." });

                // Obtenemos el origen "COMPRAS" (Asumiendo que es el código '02')
                var origenCompras = await _context.OrigenesAsiento.FirstOrDefaultAsync(o => o.Codigo == "02");

                // Armamos el ViewModel en memoria (No toca la BD contable aún)
                var borrador = new AsientoViewModel
                {
                    EmpresaId = EmpresaUsuarioId,
                    // Si tienes forma de calcular el periodo_id según la fecha, lo haces aquí. Por ahora mandamos 0 para que el contador elija.
                    PeriodoId = 0,
                    OrigenAsientoId = origenCompras?.Id ?? 0,
                    FechaContable = documento.FechaEmision.Value,
                    MonedaId = documento.MonedaId ?? 1,
                    Glosa = $"Provisión de Factura {documento.Serie}-{documento.Numero}",
                    Detalles = new List<DetalleAsientoViewModel>()
                };

                // LÍNEA 1: GASTO (DEBE) -> Total inafecto o base imponible
                // Aquí el contador deberá elegir la cuenta contable de gasto (ej. 60 o 63), se la mandamos vacía pero con monto.
                borrador.Detalles.Add(new DetalleAsientoViewModel
                {
                    CuentaContableId = 0,
                    ProveedorId = documento.ProveedorId,
                    SerieDocumento = documento.Serie,
                    NumeroDocumento = documento.Numero,
                    FechaEmision = documento.FechaEmision,
                    DebeSoles = documento.SubTotal,
                    HaberSoles = 0,
                    GlosaDetalle = "Reconocimiento del Gasto"
                });

                // LÍNEA 2: IGV (DEBE) -> Si el documento tiene IGV
                if (documento.MontoIgv > 0)
                {
                    // Intentamos buscar la cuenta de IGV automáticamente (Código 40111)
                    var cuentaIgv = await _context.CuentasContables.FirstOrDefaultAsync(c => c.Codigo == "40111" && c.EmpresaId == EmpresaUsuarioId);

                    borrador.Detalles.Add(new DetalleAsientoViewModel
                    {
                        CuentaContableId = cuentaIgv?.Id ?? 0,
                        ProveedorId = documento.ProveedorId,
                        SerieDocumento = documento.Serie,
                        NumeroDocumento = documento.Numero,
                        FechaEmision = documento.FechaEmision,
                        DebeSoles = documento.MontoIgv,
                        HaberSoles = 0,
                        GlosaDetalle = "IGV de la compra"
                    });
                }

                // LÍNEA 3: CUENTA POR PAGAR (HABER) -> Total a pagar al proveedor
                // Intentamos buscar la cuenta 4212 (Facturas por pagar)
                var cuentaPorPagar = await _context.CuentasContables.FirstOrDefaultAsync(c => c.Codigo == "4212" && c.EmpresaId == EmpresaUsuarioId);

                borrador.Detalles.Add(new DetalleAsientoViewModel
                {
                    CuentaContableId = cuentaPorPagar?.Id ?? 0,
                    ProveedorId = documento.ProveedorId,
                    SerieDocumento = documento.Serie,
                    NumeroDocumento = documento.Numero,
                    FechaEmision = documento.FechaEmision,
                    DebeSoles = 0,
                    HaberSoles = documento.Total,
                    GlosaDetalle = "Deuda con el proveedor"
                });

                // Devolvemos el borrador al Frontend. El Frontend pintará la tabla con estos datos.
                return Json(new { status = true, data = borrador });
            }
            catch (Exception ex)
            {
                return Json(new { status = false, message = "Error al generar borrador: " + ex.Message });
            }
        }

        // =========================================================================================
        // AUTOMATIZACIÓN DE VENTAS (CUENTAS POR COBRAR)
        // =========================================================================================

        [HttpGet]
        public async Task<JsonResult> GetVentasPendientes()
        {
            // Traemos facturas de ventas que NO tienen un asiento vinculado
            var contabilizadosIds = await _context.AsientosContables
                .Where(a => a.TablaReferencia == "documento_cobrar" && a.Estado == "REGISTRADO")
                .Select(a => a.IdReferencia)
                .ToListAsync();

            var pendientes = await _context.DocumentosCobrar
                .Where(d => d.EmpresaId == EmpresaUsuarioId && !contabilizadosIds.Contains(d.Id))
                .Select(d => new
                {
                    id = d.Id,
                    cliente = _context.Clientes.FirstOrDefault(c => c.Id == d.ClienteId).RazonSocial,
                    documento = d.Serie + "-" + d.Numero,
                    fecha = d.FechaEmision.ToString("dd/MM/yyyy"),
                    total = d.Total
                }).ToListAsync();

            return Json(new { status = true, data = pendientes });
        }

        [HttpGet]
        public async Task<JsonResult> GenerarBorradorDocumentoCobrar(int documentoCobrarId)
        {
            try
            {
                // Buscamos la factura de venta operativa
                var documento = await _context.DocumentosCobrar
                    .FirstOrDefaultAsync(d => d.Id == documentoCobrarId && d.EmpresaId == EmpresaUsuarioId);

                if (documento == null)
                    return Json(new { status = false, message = "Factura de venta no encontrada." });

                // Obtenemos el origen "VENTAS" (Ej: '01' o '14', asumiendo que tienes uno configurado)
                // Si no tienes uno específico, puedes ajustarlo. Usaremos "14" como ejemplo estándar contable de ventas.
                var origenVentas = await _context.OrigenesAsiento.FirstOrDefaultAsync(o => o.Codigo == "14" || o.Descripcion.Contains("VENTAS"));

                // Armamos el ViewModel en memoria
                var borrador = new AsientoViewModel
                {
                    EmpresaId = EmpresaUsuarioId,
                    PeriodoId = 0,
                    OrigenAsientoId = origenVentas?.Id ?? 0,
                    FechaContable = documento.FechaEmision,
                    MonedaId = documento.MonedaId ?? 1,
                    Glosa = $"Provisión Venta {documento.Serie}-{documento.Numero}",
                    IdReferencia = documento.Id, // <-- Vital para que el front sepa
                    Detalles = new List<DetalleAsientoViewModel>()
                };

                // LÍNEA 1: CUENTA POR COBRAR CLIENTE (DEBE) -> Total de la factura
                // Buscar cuenta 1212 o similar
                var cuentaCobrar = await _context.CuentasContables.FirstOrDefaultAsync(c => c.Codigo.StartsWith("1212") && c.EmpresaId == EmpresaUsuarioId);

                borrador.Detalles.Add(new DetalleAsientoViewModel
                {
                    CuentaContableId = cuentaCobrar?.Id ?? 0,
                    ClienteId = documento.ClienteId, // Ahora es Cliente, no Proveedor
                    SerieDocumento = documento.Serie,
                    NumeroDocumento = documento.Numero,
                    FechaEmision = documento.FechaEmision,
                    DebeSoles = documento.Total ?? 0,
                    HaberSoles = 0,
                    GlosaDetalle = "Por cobrar al cliente"
                });

                // LÍNEA 2: IGV (HABER) -> Si tiene IGV
                if ((documento.MontoIgv ?? 0) > 0)
                {
                    var cuentaIgv = await _context.CuentasContables.FirstOrDefaultAsync(c => c.Codigo == "40111" && c.EmpresaId == EmpresaUsuarioId);

                    borrador.Detalles.Add(new DetalleAsientoViewModel
                    {
                        CuentaContableId = cuentaIgv?.Id ?? 0,
                        ClienteId = documento.ClienteId,
                        SerieDocumento = documento.Serie,
                        NumeroDocumento = documento.Numero,
                        FechaEmision = documento.FechaEmision,
                        DebeSoles = 0,
                        HaberSoles = documento.MontoIgv ?? 0,
                        GlosaDetalle = "IGV de ventas"
                    });
                }

                // LÍNEA 3: INGRESO POR VENTA (HABER) -> Base imponible
                var cuentaIngreso = await _context.CuentasContables.FirstOrDefaultAsync(c => c.Codigo.StartsWith("70") && c.EmpresaId == EmpresaUsuarioId);

                borrador.Detalles.Add(new DetalleAsientoViewModel
                {
                    CuentaContableId = cuentaIngreso?.Id ?? 0,
                    ClienteId = documento.ClienteId,
                    SerieDocumento = documento.Serie,
                    NumeroDocumento = documento.Numero,
                    FechaEmision = documento.FechaEmision,
                    DebeSoles = 0,
                    HaberSoles = documento.SubTotal ?? 0,
                    GlosaDetalle = "Ingreso de la venta"
                });

                return Json(new { status = true, data = borrador });
            }
            catch (Exception ex)
            {
                return Json(new { status = false, message = "Error al generar borrador de venta: " + ex.Message });
            }
        }

        // =========================================================================================
        // 4. POST: Guardar el asiento final (Ya sea manual o venido de un borrador editado)
        // =========================================================================================
        [HttpPost]
        public async Task<JsonResult> GuardarAsiento([FromBody] AsientoViewModel model)
        {
            try
            {
                if (model == null || !model.Detalles.Any())
                    return Json(new { status = false, message = "El asiento no tiene detalles." });

                // Blindaje de seguridad: Forzamos el uso de la sesión actual
                model.EmpresaId = EmpresaUsuarioId;
                model.UsuarioRegistro = UsuarioActualId;

                decimal sumaDebe = model.Detalles.Sum(d => d.DebeSoles);
                decimal sumaHaber = model.Detalles.Sum(d => d.HaberSoles);

                if (sumaDebe != sumaHaber)
                    return Json(new { status = false, message = $"Descuadre. Debe: {sumaDebe} / Haber: {sumaHaber}" });

                // Validación Tipo de Cambio (Solo lo buscamos en BD)
                var tcVenta = await _context.TipoCambios
                                            .Where(x => x.Fecha.Date == model.FechaContable.Date)
                                            .Select(x => x.TcVenta)
                                            .FirstOrDefaultAsync();

                if (tcVenta <= 0)
                    return Json(new { status = false, message = $"No hay T.C. para la fecha {model.FechaContable:dd/MM/yyyy}." });

                // Correlativo Básico
                string anioMes = model.FechaContable.ToString("yyyyMM");
                int totalMes = await _context.AsientosContables.CountAsync(a => a.NumeroAsiento.StartsWith(anioMes) && a.EmpresaId == EmpresaUsuarioId);
                string correlativo = $"{anioMes}-{(totalMes + 1):D4}";

                using (var transaction = await _context.Database.BeginTransactionAsync())
                {
                    var cabecera = new AsientoContable
                    {
                        EmpresaId = model.EmpresaId,
                        PeriodoId = model.PeriodoId,
                        OrigenAsientoId = model.OrigenAsientoId,
                        NumeroAsiento = correlativo,
                        FechaContable = model.FechaContable,
                        MonedaId = model.MonedaId,
                        TipoCambio = tcVenta,
                        Glosa = model.Glosa,
                        TotalDebe = sumaDebe,
                        TotalHaber = sumaHaber,
                        Estado = "REGISTRADO",
                        IdReferencia = model.IdReferencia, // Aquí se guardaría el ID del documento operativo si vino del borrador
                        TablaReferencia = model.TablaReferencia, // "documento_pagar", "comprobante", etc.
                        UsuarioRegistro = model.UsuarioRegistro,
                        FechaRegistro = DateTime.Now
                    };

                    _context.AsientosContables.Add(cabecera);
                    await _context.SaveChangesAsync();

                    foreach (var item in model.Detalles)
                    {
                        var detalle = new DasientoContable
                        {
                            AsientoContableId = cabecera.Id,
                            CuentaContableId = item.CuentaContableId,
                            ProveedorId = item.ProveedorId,
                            ClienteId = item.ClienteId,
                            SerieDocumento = item.SerieDocumento,
                            NumeroDocumento = item.NumeroDocumento,
                            FechaEmision = item.FechaEmision,
                            DebeSoles = item.DebeSoles,
                            HaberSoles = item.HaberSoles,
                            GlosaDetalle = string.IsNullOrEmpty(item.GlosaDetalle) ? model.Glosa : item.GlosaDetalle
                        };
                        _context.DetallesAsiento.Add(detalle);
                    }

                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();

                    return Json(new { status = true, message = "Guardado con éxito", asiento = correlativo });
                }
            }
            catch (Exception ex)
            {
                return Json(new { status = false, message = "Error interno: " + ex.Message });
            }
        }
        // GET: /Contabilidad/GetAsientosRecientes
        [HttpGet]
        public async Task<JsonResult> GetAsientosRecientes(int periodoId)
        {
            try
            {
                // Hacemos un Join rápido con OrigenesAsiento para traer el nombre del origen
                var historial = await (from a in _context.AsientosContables
                                       join o in _context.OrigenesAsiento on a.OrigenAsientoId equals o.Id
                                       where a.EmpresaId == EmpresaUsuarioId && a.PeriodoId == periodoId
                                       orderby a.FechaRegistro descending
                                       select new
                                       {
                                           id = a.Id,
                                           numero = a.NumeroAsiento,
                                           fecha = a.FechaContable.ToString("dd/MM/yyyy"),
                                           origen = o.Descripcion,
                                           glosa = a.Glosa,
                                           debe = a.TotalDebe,
                                           haber = a.TotalHaber,
                                           estado = a.Estado
                                       }).Take(50).ToListAsync();

                return Json(new { status = true, data = historial });
            }
            catch (Exception ex)
            {
                return Json(new { status = false, message = "Error al cargar historial: " + ex.Message });
            }
        }
        // GET: /Contabilidad/ReportesFinancieros
        // Renderiza la vista (el esqueleto HTML)
        public IActionResult ReportesFinancieros()
        {
            return View();
        }

        // GET: /Contabilidad/GenerarBalanceGeneral
        [HttpGet]
        public async Task<JsonResult> GenerarBalanceGeneral(int periodoId)
        {
            try
            {
                // 1. Traemos la data cruda registrada en el periodo (Igual que en Estado de Resultados)
                var movimientos = await (from d in _context.DetallesAsiento
                                         join a in _context.AsientosContables on d.AsientoContableId equals a.Id
                                         join c in _context.CuentasContables on d.CuentaContableId equals c.Id
                                         where a.EmpresaId == EmpresaUsuarioId
                                               && a.PeriodoId <= periodoId // El balance es acumulativo, arrastra saldos hasta ese periodo
                                               && a.Estado == "REGISTRADO"
                                         select new
                                         {
                                             CodigoCuenta = c.Codigo,
                                             Debe = d.DebeSoles,
                                             Haber = d.HaberSoles
                                         }).ToListAsync();

                var balance = new BalanceGeneralViewModel();

                // 2. ACTIVOS (Suman por el Debe, restan por el Haber)

                // Activo Corriente: Efectivo (10), Cuentas por cobrar (12, 14, 16), Inventarios (20)
                balance.ActivosCorrientes = movimientos
                    .Where(m => m.CodigoCuenta.StartsWith("1") || m.CodigoCuenta.StartsWith("2"))
                    .Sum(m => m.Debe - m.Haber);

                // Activo No Corriente: Inmuebles, Maquinaria, Equipo (33), Intangibles (34), Depreciación restando (39)
                balance.ActivosNoCorrientes = movimientos
                    .Where(m => m.CodigoCuenta.StartsWith("3"))
                    .Sum(m => m.Debe - m.Haber);


                // 3. PASIVOS (Suman por el Haber, restan por el Debe)

                // Pasivo Corriente: Tributos (40), Remuneraciones (41), Proveedores (42, 46)
                // Para el MVP asumimos que todo el pasivo es corriente (a pagar en menos de 1 año)
                balance.PasivosCorrientes = movimientos
                    .Where(m => m.CodigoCuenta.StartsWith("4"))
                    .Sum(m => m.Haber - m.Debe);

                balance.PasivosNoCorrientes = 0; // Se aisla después si manejan préstamos bancarios a largo plazo (ej. cuenta 45 a +12 meses)


                // 4. PATRIMONIO (Suman por el Haber, restan por el Debe)

                // Capital (50), Reservas (58), Resultados Acumulados de años anteriores (59)
                balance.PatrimonioNeto = movimientos
                    .Where(m => m.CodigoCuenta.StartsWith("5"))
                    .Sum(m => m.Haber - m.Debe);


                // 5. CÁLCULO DE LA UTILIDAD DEL EJERCICIO (El puente entre ambos reportes)
                // Ingresos (Elemento 7) menos Gastos (Costos 69 + Administrativos 94 + Ventas 95 + Financieros 97 + Diversos 65)
                decimal ingresos = movimientos
                    .Where(m => m.CodigoCuenta.StartsWith("70") || m.CodigoCuenta.StartsWith("75") || m.CodigoCuenta.StartsWith("76") || m.CodigoCuenta.StartsWith("77"))
                    .Sum(m => m.Haber - m.Debe);

                decimal gastos = movimientos
                    .Where(m => m.CodigoCuenta.StartsWith("69") || m.CodigoCuenta.StartsWith("94") || m.CodigoCuenta.StartsWith("95") || m.CodigoCuenta.StartsWith("97") || m.CodigoCuenta.StartsWith("65"))
                    .Sum(m => m.Debe - m.Haber);

                balance.UtilidadDelEjercicio = ingresos - gastos;

                return Json(new { status = true, data = balance });
            }
            catch (Exception ex)
            {
                return Json(new { status = false, message = "Error al generar Balance General: " + ex.Message });
            }
        }

        // GET: /Contabilidad/GenerarEstadoResultados
        // El endpoint que hace la matemática pesada y devuelve el JSON
        [HttpGet]
        public async Task<JsonResult> GenerarEstadoResultados(int periodoId)
        {
            try
            {
                // 1. Traemos toda la data cruda del periodo seleccionado (Join de Asiento, Detalle y Cuenta)
                var movimientos = await (from d in _context.DetallesAsiento
                                         join a in _context.AsientosContables on d.AsientoContableId equals a.Id
                                         join c in _context.CuentasContables on d.CuentaContableId equals c.Id
                                         where a.EmpresaId == EmpresaUsuarioId
                                               && a.PeriodoId == periodoId
                                               && a.Estado == "REGISTRADO"
                                         select new
                                         {
                                             CodigoCuenta = c.Codigo,
                                             Debe = d.DebeSoles,
                                             Haber = d.HaberSoles
                                         }).ToListAsync();

                var reporte = new EstadoResultadosViewModel();

                // 2. Filtramos y sumamos según el PCGE (Plan Contable)
                // Nota Financiera: Ingresos = Haber - Debe | Gastos = Debe - Haber

                // A. INGRESOS
                // Ventas (Cuenta 70)
                reporte.VentasNetas = movimientos
                    .Where(m => m.CodigoCuenta.StartsWith("70"))
                    .Sum(m => m.Haber - m.Debe);

                // Otros Ingresos (Cuentas 75, 76, 77)
                reporte.OtrosIngresos = movimientos
                    .Where(m => m.CodigoCuenta.StartsWith("75") || m.CodigoCuenta.StartsWith("76") || m.CodigoCuenta.StartsWith("77"))
                    .Sum(m => m.Haber - m.Debe);

                // B. COSTOS DE VENTAS (Cuenta 69)
                reporte.CostoVentas = movimientos
                    .Where(m => m.CodigoCuenta.StartsWith("69"))
                    .Sum(m => m.Debe - m.Haber);

                // C. GASTOS (Usamos la contabilidad analítica, Elemento 9)
                // Gastos de Ventas (Cuenta 95)
                reporte.GastosVentas = movimientos
                    .Where(m => m.CodigoCuenta.StartsWith("95"))
                    .Sum(m => m.Debe - m.Haber);

                // Gastos Administrativos (Cuenta 94)
                reporte.GastosAdministrativos = movimientos
                    .Where(m => m.CodigoCuenta.StartsWith("94"))
                    .Sum(m => m.Debe - m.Haber);

                // Gastos Financieros (Cuenta 97)
                reporte.GastosFinancieros = movimientos
                    .Where(m => m.CodigoCuenta.StartsWith("97"))
                    .Sum(m => m.Debe - m.Haber);

                // D. OTROS GASTOS NETOS (Cuenta 65 - Aquí entra la famosa cerveza)
                reporte.OtrosGastosNetos = movimientos
                    .Where(m => m.CodigoCuenta.StartsWith("65"))
                    .Sum(m => m.Debe - m.Haber);

                // Retornamos el objeto completo. Las utilidades se calculan solas gracias al ViewModel.
                return Json(new { status = true, data = reporte });
            }
            catch (Exception ex)
            {
                return Json(new { status = false, message = "Error al generar el reporte: " + ex.Message });
            }
        }
        // 1. PARA EL BOTÓN DEL OJITO (Ver Asiento Guardado)
        [HttpGet]
        public async Task<JsonResult> GetAsientoCompleto(int id)
        {
            var cabecera = await _context.AsientosContables.FindAsync(id);
            if (cabecera == null) return Json(new { status = false });

            var detalles = await _context.DetallesAsiento
                .Include(d => d.AsientoContable) // Si tienes navigation properties
                .Where(d => d.AsientoContableId == id)
                .Select(d => new
                {
                    cuentaId = d.CuentaContableId,
                    cuentaNombre = _context.CuentasContables.FirstOrDefault(c => c.Id == d.CuentaContableId).Codigo + " - " + _context.CuentasContables.FirstOrDefault(c => c.Id == d.CuentaContableId).Nombre,
                    debe = d.DebeSoles,
                    haber = d.HaberSoles,
                    glosa = d.GlosaDetalle
                }).ToListAsync();

            return Json(new { status = true, cabecera = cabecera, detalles = detalles });
        }

        // 2. PARA LA AUTOMATIZACIÓN (Listar Provisiones Pendientes)
        [HttpGet]
        public async Task<JsonResult> GetProvisionesPendientes()
        {
            // Traemos facturas que NO tienen un asiento vinculado
            var contabilizadosIds = await _context.AsientosContables
                .Where(a => a.TablaReferencia == "documento_pagar" && a.Estado == "REGISTRADO")
                .Select(a => a.IdReferencia)
                .ToListAsync();

            var pendientes = await _context.DocumentosPagar
                .Where(d => d.EmpresaId == EmpresaUsuarioId && !contabilizadosIds.Contains(d.Id))
                .Select(d => new
                {
                    id = d.Id,
                    proveedor = _context.Proveedores.FirstOrDefault(p => p.Id == d.ProveedorId).RazonSocial,
                    documento = d.Serie + "-" + d.Numero,
                    fecha = d.FechaEmision.Value.ToString("dd/MM/yyyy"),
                    total = d.Total
                }).ToListAsync();

            return Json(new { status = true, data = pendientes });
        }
    }
}