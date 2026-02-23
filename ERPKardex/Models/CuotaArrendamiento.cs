using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ERPKardex.Models
{
    [Table("cuota_arrendamiento")]
    public class CuotaArrendamiento
    {
        [Key]
        public int Id { get; set; }

        [Column("arrendamiento_id")] public int ArrendamientoId { get; set; }
        [Column("moneda_id")] public int? MonedaId { get; set; }

        // Ej: "2025-01"
        [Column("periodo_anio_mes")] public string PeriodoAnioMes { get; set; } = string.Empty;

        [Column("fecha_vencimiento")] public DateTime FechaVencimiento { get; set; }

        [Column("monto_cuota")] public decimal MontoCuota { get; set; }

        // Datos del Pago
        [Column("fecha_pago")] public DateTime? FechaPago { get; set; }
        [Column("monto_pagado")] public decimal? MontoPagado { get; set; }
        [Column("numero_operacion")] public string? NumeroOperacion { get; set; }
        [Column("ruta_evidencia")] public string? RutaEvidencia { get; set; }
        [Column("observaciones")] public string? Observaciones { get; set; }

        // 0: Pendiente, 1: Pagado
        [Column("estado_pago")] public byte EstadoPago { get; set; }
    }
}
