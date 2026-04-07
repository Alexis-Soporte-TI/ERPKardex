using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ERPKardex.Models
{
    [Table("dordencompra")]
    public class DOrdenCompra
    {
        [Key]
        public int Id { get; set; }

        [Column("ordencompra_id")]
        public int OrdenCompraId { get; set; }

        public string? Item { get; set; }

        [Column("producto_id")]
        public int? ProductoId { get; set; }

        // Snapshots
        public string? Descripcion { get; set; }
        [Column("unidad_medida")]
        public string? UnidadMedida { get; set; }


        [Column("cantidad", TypeName = "decimal(14,4)")]
        public decimal? Cantidad { get; set; }

        [Column("cantidad_atendida", TypeName = "decimal(14,4)")]
        public decimal? CantidadAtendida { get; set; }

        [Column("precio_unitario", TypeName = "decimal(19,10)")]
        public decimal? PrecioUnitario { get; set; }

        [Column("porc_descuento")]
        public decimal? PorcDescuento { get; set; }

        [Column("valor_venta", TypeName = "decimal(19,10)")]
        public decimal? ValorVenta { get; set; } // Subtotal sin IGV
        [Column("impuesto", TypeName = "decimal(19,10)")]
        public decimal? Impuesto { get; set; }   // Monto IGV
        [Column("total", TypeName = "decimal(19,10)")]
        public decimal? Total { get; set; }

        [Column("centro_costo_id")]
        public int? CentroCostoId { get; set; }
        public string? Lugar { get; set; }

        // Trazabilidad con el Pedido
        [Column("id_referencia")]
        public int? IdReferencia { get; set; } // ID de dpedcompra

        [Column("tabla_referencia")]
        public string? TablaReferencia { get; set; } // "DPEDCOMPRA"
        [Column("estado_id")]
        public int? EstadoId { get; set; }

        [Column("observacion_item")]
        public string? ObservacionItem { get; set; }

        [Column("empresa_id")]
        public int? EmpresaId { get; set; }
    }
}