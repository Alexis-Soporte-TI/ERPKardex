using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ERPKardex.Models
{
    [Table("ordencompra")]
    public class OrdenCompra
    {
        [Key]
        public int Id { get; set; }

        [Column("tipo_documento_interno_id")]
        public int? TipoDocumentoInternoId { get; set; }

        public string? Numero { get; set; }

        [Column("proveedor_id")]
        public int? ProveedorId { get; set; } // Proveedor

        [Column("fecha_emision")]
        public DateTime? FechaEmision { get; set; }

        [Column("fecha_entrega")]
        public DateTime? FechaEntrega { get; set; }

        [Column("moneda_id")]
        public int? MonedaId { get; set; }

        [Column("tipo_cambio", TypeName = "decimal(12,6)")]
        public decimal? TipoCambio { get; set; }

        [Column("condicion_pago")]
        public string? CondicionPago { get; set; }

        [Column("lugar_destino")]
        public string? LugarDestino { get; set; }

        [Column("sucursal_id")]
        public int? SucursalId { get; set; }

        [Column("almacen_id")]
        public int? AlmacenId { get; set; }
        public string? Observacion { get; set; }

        [Column("incluye_igv")]
        public bool? IncluyeIgv { get; set; } // Tu check de Importación/Impuestos

        // Totales Cabecera
        [Column("total_afecto")]
        public decimal? TotalAfecto { get; set; }

        [Column("total_inafecto")]
        public decimal? TotalInafecto { get; set; }

        [Column("igv_total")]
        public decimal? IgvTotal { get; set; }

        public decimal? Total { get; set; }

        [Column("estado_id")]
        public int? EstadoId { get; set; }

        [Column("usuario_creacion_id")]
        public int? UsuarioCreacionId { get; set; }

        [Column("empresa_id")]
        public int? EmpresaId { get; set; }

        [Column("usuario_aprobador")]
        public int? UsuarioAprobador { get; set; }

        [Column("fecha_aprobacion")]
        public DateTime? FechaAprobacion { get; set; }

        [Column("fecha_registro")]
        public DateTime? FechaRegistro { get; set; }
        [Column("periodo_contable_id")]
        public int? PeriodoContableId { get; set; }
    }
}