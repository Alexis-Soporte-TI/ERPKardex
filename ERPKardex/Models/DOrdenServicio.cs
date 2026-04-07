using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ERPKardex.Models
{
    [Table("dordenservicio")]
    public class DOrdenServicio
    {
        [Key]
        public int Id { get; set; }

        [Column("ordenservicio_id")]
        public int OrdenServicioId { get; set; }

        public string? Item { get; set; }

        [Column("producto_id")]
        public int? ProductoId { get; set; } // Servicio del catálogo

        public string? Descripcion { get; set; } // Detalle libre largo

        [Column("unidad_medida")]
        public string? UnidadMedida { get; set; }


        [Column("cantidad", TypeName = "decimal(14,4)")]
        public decimal? Cantidad { get; set; }

        [Column("cantidad_atendida", TypeName = "decimal(14,4)")]
        public decimal? CantidadAtendida { get; set; }

        [Column("precio_unitario", TypeName = "decimal(19,10)")]
        public decimal? PrecioUnitario { get; set; }

        [Column("valor_venta", TypeName = "decimal(19,10)")]
        public decimal? ValorVenta { get; set; }
        [Column("impuesto", TypeName = "decimal(19,10)")]
        public decimal? Impuesto { get; set; }
        [Column("total", TypeName = "decimal(19,10)")]
        public decimal? Total { get; set; }

        [Column("centro_costo_id")]
        public int? CentroCostoId { get; set; }
        public string? Lugar { get; set; }

        // Trazabilidad
        [Column("id_referencia")]
        public int? IdReferencia { get; set; } // ID de dpedservicio

        [Column("tabla_referencia")]
        public string? TablaReferencia { get; set; } // "DPEDSERVICIO"
        [Column("estado_id")]
        public int? EstadoId { get; set; }

        [Column("empresa_id")]
        public int? EmpresaId { get; set; }
    }
}