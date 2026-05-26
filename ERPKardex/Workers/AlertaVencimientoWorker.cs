using ERPKardex.Data;
using ERPKardex.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ERPKardex.Workers
{
    public class AlertaVencimientoWorker : BackgroundService
    {
        private readonly IServiceProvider _services;
        private readonly ILogger<AlertaVencimientoWorker> _logger;

        public AlertaVencimientoWorker(IServiceProvider services, ILogger<AlertaVencimientoWorker> logger)
        {
            _services = services;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Worker de Alertas de Vencimiento iniciado.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await ProcesarAlertas();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error en el worker de alertas de vencimiento.");
                }

                // Esperar hasta la próxima medianoche para revisar de nuevo
                var ahora = DateTime.Now;
                var mañana = ahora.Date.AddDays(1);
                var espera = mañana - ahora;
                await Task.Delay(espera, stoppingToken);
            }
        }

        private async Task ProcesarAlertas()
        {
            using var scope = _services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();

            var hoy = DateTime.Today;
            int diaHoy = (int)hoy.DayOfWeek == 0 ? 7 : (int)hoy.DayOfWeek; // 1=Lun...7=Dom
            int semanaDelAño = System.Globalization.CultureInfo.CurrentCulture.Calendar
                .GetWeekOfYear(hoy, System.Globalization.CalendarWeekRule.FirstDay, DayOfWeek.Monday);

            // Obtener todos los destinatarios activos cuyo día de envío coincide con hoy
            var destinatarios = await context.CorreosAlertaStock
                .Where(c => c.Activo && c.DiaEnvio == diaHoy)
                .ToListAsync();

            if (!destinatarios.Any()) return;

            // Agrupar destinatarios por empresa
            var porEmpresa = destinatarios.GroupBy(d => d.EmpresaId);

            foreach (var grupoEmpresa in porEmpresa)
            {
                int empresaId = grupoEmpresa.Key;

                // Filtrar según frecuencia: SEMANAL = siempre, BISEMANAL = semanas pares
                var destFiltrados = grupoEmpresa
                    .Where(d => d.Frecuencia == "SEMANAL" || (d.Frecuencia == "BISEMANAL" && semanaDelAño % 2 == 0))
                    .Select(d => d.Email)
                    .ToList();

                if (!destFiltrados.Any()) continue;

                // Obtener umbrales configurados para esta empresa
                var config = await context.ConfigAlertasVencimiento
                    .FirstOrDefaultAsync(c => c.EmpresaId == empresaId);

                int mesesCritico = config?.MesesCritico ?? 18;
                int mesesAlerta = config?.MesesAlerta ?? 24;
                int diasCritico = mesesCritico * 30;
                int diasAlerta = mesesAlerta * 30;

                // Obtener el estado Aprobado
                var estadoAprobado = await context.Estados
                    .FirstOrDefaultAsync(e => e.Nombre == "Aprobado" && e.Tabla == "INGRESOSALIDAALM");
                int idAprobado = estadoAprobado?.Id ?? -1;

                // Calcular stock por lote
                var ingresos = (from d in context.DIngresoSalidaAlms
                                join c in context.IngresoSalidaAlms on d.IngresoSalidaAlmId equals c.Id
                                where c.EmpresaId == empresaId
                                   && c.EstadoId == idAprobado
                                   && c.TipoMovimiento == true
                                select new
                                {
                                    d.CodProducto,
                                    d.DescripcionProducto,
                                    d.Lote,
                                    d.FechaVencimiento,
                                    d.CodUnidadMedida,
                                    Cantidad = d.Cantidad ?? 0
                                })
                               .ToList()
                               .GroupBy(x => new { x.CodProducto, x.DescripcionProducto, x.Lote, x.FechaVencimiento, x.CodUnidadMedida })
                               .Select(g => new
                               {
                                   g.Key.CodProducto,
                                   g.Key.DescripcionProducto,
                                   g.Key.Lote,
                                   g.Key.FechaVencimiento,
                                   g.Key.CodUnidadMedida,
                                   TotalIngresado = g.Sum(x => x.Cantidad)
                               }).ToList();

                var salidas = (from d in context.DIngresoSalidaAlms
                               join c in context.IngresoSalidaAlms on d.IngresoSalidaAlmId equals c.Id
                               where c.EmpresaId == empresaId
                                  && c.EstadoId == idAprobado
                                  && c.TipoMovimiento == false
                               select new { d.CodProducto, d.Lote, Cantidad = d.Cantidad ?? 0 })
                              .ToList()
                              .GroupBy(x => new { x.CodProducto, x.Lote })
                              .ToDictionary(g => (g.Key.CodProducto, g.Key.Lote), g => g.Sum(x => x.Cantidad));

                var items = ingresos.Select(i =>
                {
                    var clave = (i.CodProducto, i.Lote);
                    decimal stock = i.TotalIngresado - (salidas.ContainsKey(clave) ? salidas[clave] : 0);
                    int? dias = i.FechaVencimiento.HasValue
                        ? (int)(i.FechaVencimiento.Value.Date - hoy).TotalDays
                        : (int?)null;

                    string estado = dias.HasValue
                        ? dias.Value < 0 ? "VENCIDO"
                        : dias.Value <= diasCritico ? "CRITICO"
                        : dias.Value <= diasAlerta ? "ALERTA"
                        : "OPTIMO"
                        : "SIN_FECHA";

                    return new { i.CodProducto, i.DescripcionProducto, Lote = i.Lote ?? "-", i.FechaVencimiento, i.CodUnidadMedida, Stock = stock, Dias = dias, Estado = estado };
                })
                .Where(x => x.Stock > 0 && x.Estado != "OPTIMO" && x.Estado != "SIN_FECHA")
                .OrderBy(x => x.Dias ?? int.MaxValue)
                .ToList();

                if (!items.Any()) continue;

                // Obtener nombre de la empresa
                var empresa = await context.Empresas.FindAsync(empresaId);
                string nombreEmpresa = empresa?.RazonSocial ?? $"Empresa #{empresaId}";

                string asunto = $"⚠️ Alerta de Vencimiento de Stock — {nombreEmpresa} — {hoy:dd/MM/yyyy}";
                string cuerpo = ConstruirHtml(nombreEmpresa, items, mesesCritico, mesesAlerta, hoy);

                await emailService.SendEmailAsync(destFiltrados, asunto, cuerpo);
                _logger.LogInformation("Alerta enviada a {n} destinatario(s) de empresa {e}.", destFiltrados.Count, nombreEmpresa);
            }
        }

        private static string ConstruirHtml(string empresa, IEnumerable<dynamic> items, int mesesCritico, int mesesAlerta, DateTime hoy)
        {
            var filas = new System.Text.StringBuilder();
            foreach (var i in items)
            {
                string color = i.Estado switch
                {
                    "VENCIDO" or "CRITICO" => "#fff0f0",
                    "ALERTA" => "#fffde7",
                    _ => "#ffffff"
                };
                string badge = i.Estado switch
                {
                    "VENCIDO" => "<span style='background:#dc3545;color:#fff;padding:2px 8px;border-radius:4px;font-size:12px;'>🔴 VENCIDO</span>",
                    "CRITICO" => "<span style='background:#dc3545;color:#fff;padding:2px 8px;border-radius:4px;font-size:12px;'>🔴 CRÍTICO</span>",
                    "ALERTA" => "<span style='background:#ffc107;color:#000;padding:2px 8px;border-radius:4px;font-size:12px;'>🟡 ALERTA</span>",
                    _ => ""
                };
                string fv = i.FechaVencimiento != null ? ((DateTime)i.FechaVencimiento).ToString("dd/MM/yyyy") : "—";
                string dias = i.Dias != null ? $"{i.Dias} días" : "—";

                filas.Append($@"
                    <tr style='background:{color};'>
                        <td style='padding:8px;border:1px solid #ddd;'>{i.CodProducto}</td>
                        <td style='padding:8px;border:1px solid #ddd;'>{i.DescripcionProducto}</td>
                        <td style='padding:8px;border:1px solid #ddd;text-align:center;'>{i.Lote}</td>
                        <td style='padding:8px;border:1px solid #ddd;text-align:right;'>{i.Stock:N4}</td>
                        <td style='padding:8px;border:1px solid #ddd;text-align:center;'>{fv}</td>
                        <td style='padding:8px;border:1px solid #ddd;text-align:center;'>{dias}</td>
                        <td style='padding:8px;border:1px solid #ddd;text-align:center;'>{badge}</td>
                    </tr>");
            }

            return $@"
<!DOCTYPE html>
<html>
<head><meta charset='utf-8'></head>
<body style='font-family:Arial,sans-serif;color:#333;max-width:900px;margin:auto;padding:20px;'>
    <div style='background:#1a1a2e;color:#fff;padding:20px;border-radius:8px 8px 0 0;'>
        <h2 style='margin:0;'>⚠️ Reporte de Alertas de Vencimiento</h2>
        <p style='margin:4px 0 0;opacity:.8;'>{empresa} — Generado el {hoy:dd/MM/yyyy}</p>
    </div>
    <div style='background:#f8f9fa;padding:12px 20px;border:1px solid #dee2e6;'>
        <span style='background:#dc3545;color:#fff;padding:3px 10px;border-radius:4px;margin-right:8px;'>🔴 CRÍTICO: &lt; {mesesCritico} meses</span>
        <span style='background:#ffc107;color:#000;padding:3px 10px;border-radius:4px;'>🟡 ALERTA: {mesesCritico}–{mesesAlerta} meses</span>
    </div>
    <table style='width:100%;border-collapse:collapse;margin-top:0;'>
        <thead>
            <tr style='background:#343a40;color:#fff;'>
                <th style='padding:10px;border:1px solid #555;'>Código</th>
                <th style='padding:10px;border:1px solid #555;'>Descripción</th>
                <th style='padding:10px;border:1px solid #555;'>Lote</th>
                <th style='padding:10px;border:1px solid #555;'>Stock</th>
                <th style='padding:10px;border:1px solid #555;'>Fecha Venc.</th>
                <th style='padding:10px;border:1px solid #555;'>Días</th>
                <th style='padding:10px;border:1px solid #555;'>Estado</th>
            </tr>
        </thead>
        <tbody>
            {filas}
        </tbody>
    </table>
    <p style='margin-top:20px;font-size:12px;color:#888;'>Este correo fue generado automáticamente por SYSSAF. No responder.</p>
</body>
</html>";
        }
    }
}
