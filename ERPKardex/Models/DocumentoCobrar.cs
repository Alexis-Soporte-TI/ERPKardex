using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ERPKardex.Models
{
    [Table("documento_cobrar")]
    public class DocumentoCobrar
    {
        [Key] public int Id { get; set; }
        [Column("empresa_id")] public int EmpresaId { get; set; }
        [Column("cliente_id")] public int ClienteId { get; set; }
        [Column("tipo_documento_interno_id")] public int TipoDocumentoInternoId { get; set; }

        [Column("orden_pedido_id")] public int? OrdenPedidoId { get; set; }
        [Column("documento_referencia_id")] public int? DocumentoReferenciaId { get; set; }

        [Column("serie")] public string? Serie { get; set; }
        [Column("numero")] public string? Numero { get; set; }
        [Column("fecha_emision")] public DateTime FechaEmision { get; set; }
        [Column("fecha_vencimiento")] public DateTime? FechaVencimiento { get; set; }
        [Column("moneda_id")] public int? MonedaId { get; set; }
        [Column("tipo_cambio", TypeName = "decimal(12,6)")] public decimal? TipoCambio { get; set; }

        // IMPORTES
        [Column("subtotal")] public decimal? SubTotal { get; set; } = 0;
        [Column("monto_igv")] public decimal? MontoIgv { get; set; } = 0;
        [Column("monto_inafecto")] public decimal? MontoInafecto { get; set; } = 0;

        [Column("monto_exonerado")] public decimal? MontoExonerado { get; set; } = 0;
        [Column("monto_gratuito")] public decimal? MontoGratuito { get; set; } = 0;
        [Column("monto_isc")] public decimal? MontoIsc { get; set; } = 0;
        [Column("monto_icbper")] public decimal? MontoIcbper { get; set; } = 0;
        [Column("otros_cargos")] public decimal? OtrosCargos { get; set; } = 0;
        [Column("descuento_global")] public decimal? DescuentoGlobal { get; set; } = 0;

        [Column("total")] public decimal? Total { get; set; } = 0;
        [Column("saldo")] public decimal? Saldo { get; set; } = 0;
        [Column("monto_usado")] public decimal? MontoUsado { get; set; } = 0;

        // AUDITORÍA
        [Column("estado_id")] public int? EstadoId { get; set; }
        [Column("observacion")] public string? Observacion { get; set; }
        [Column("usuario_registro_id")] public int? UsuarioRegistroId { get; set; }
        [Column("fecha_registro")] public DateTime? FechaRegistro { get; set; }
        [Column("auditoria_ip")] public string? AuditoriaIp { get; set; }
        [Column("auditoria_mac")] public string? AuditoriaMac { get; set; }
        [Column("auditoria_ua")] public string? AuditoriaUa { get; set; }
        [Column("periodo_contable_id")] public int? PeriodoContableId { get; set; }
    }
}