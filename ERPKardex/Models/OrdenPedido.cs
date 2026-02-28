using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ERPKardex.Models
{
    [Table("orden_pedido")]
    public class OrdenPedido
    {
        [Key] public int Id { get; set; }
        [Column("tipo_documento_interno_id")] public int? TipoDocumentoInternoId { get; set; }
        [Column("numero")] public string? Numero { get; set; }
        [Column("cliente_id")] public int? ClienteId { get; set; }
        [Column("fecha_emision")] public DateTime? FechaEmision { get; set; }
        [Column("fecha_entrega")] public DateTime? FechaEntrega { get; set; }
        [Column("moneda_id")] public int? MonedaId { get; set; }
        [Column("tipo_cambio", TypeName = "decimal(12,6)")] public decimal? TipoCambio { get; set; }
        [Column("condicion_pago")] public string? CondicionPago { get; set; }
        [Column("lugar_entrega")] public string? LugarEntrega { get; set; }
        [Column("sucursal_id")] public int? SucursalId { get; set; }
        [Column("almacen_id")] public int? AlmacenId { get; set; }
        [Column("observacion")] public string? Observacion { get; set; }
        [Column("incluye_igv")] public bool? IncluyeIgv { get; set; }

        // TOTALES ESTÁNDAR
        [Column("total_afecto")] public decimal? TotalAfecto { get; set; } = 0;
        [Column("total_inafecto")] public decimal? TotalInafecto { get; set; } = 0;
        [Column("igv_total")] public decimal? IgvTotal { get; set; } = 0;

        // CAMPOS SUNAT
        [Column("monto_exonerado")] public decimal? MontoExonerado { get; set; } = 0;
        [Column("monto_gratuito")] public decimal? MontoGratuito { get; set; } = 0;
        [Column("monto_isc")] public decimal? MontoIsc { get; set; } = 0;
        [Column("monto_icbper")] public decimal? MontoIcbper { get; set; } = 0;
        [Column("otros_cargos")] public decimal? OtrosCargos { get; set; } = 0;
        [Column("descuento_global")] public decimal? DescuentoGlobal { get; set; } = 0;

        [Column("total")] public decimal? Total { get; set; } = 0;

        // AUDITORÍA
        [Column("estado_id")] public int? EstadoId { get; set; }
        [Column("usuario_creacion_id")] public int? UsuarioCreacionId { get; set; }
        [Column("empresa_id")] public int? EmpresaId { get; set; }
        [Column("usuario_aprobador")] public int? UsuarioAprobador { get; set; }
        [Column("fecha_aprobacion")] public DateTime? FechaAprobacion { get; set; }
        [Column("fecha_registro")] public DateTime? FechaRegistro { get; set; }
        [Column("auditoria_ip")] public string? AuditoriaIp { get; set; }
        [Column("auditoria_mac")] public string? AuditoriaMac { get; set; }
        [Column("auditoria_ua")] public string? AuditoriaUa { get; set; }
        [Column("periodo_contable_id")] public int? PeriodoContableId { get; set; }
    }
}