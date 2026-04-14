using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ERPKardex.Models
{
    [Table("dpedservicio")]
    public class DPedServicio
    {
        [Key]
        public int Id { get; set; }
        [Column("pedservicio_id")]
        public int? PedidoServicioId { get; set; }
        [Column("producto_id")]
        public int? ProductoId { get; set; }
        [Column("centro_costo_id")]
        public int? CentroCostoId { get; set; }
        public string? Item { get; set; }
        [Column("descripcion_servicio")]
        public string? DescripcionServicio { get; set; }
        [Column("cantidad", TypeName = "decimal(14,4)")]
        public decimal? Cantidad { get; set; }
        [Column("cantidad_atendida", TypeName = "decimal(14,4)")]
        public decimal? CantidadAtendida { get; set; }
        [Column("unidad_medida")]
        public string? UnidadMedida { get; set; }
        [Column("id_referencia")]
        public int? IdReferencia { get; set; }
        [Column("tabla_referencia")]
        public string? TablaReferencia { get; set; }
        [Column("item_referencia")]
        public string? ItemReferencia { get; set; }
        public string? Lugar { get; set; }
        [Column("observacion_item")]
        public string? ObservacionItem { get; set; }
        [Column("estado_id")]
        public int EstadoId { get; set; }
        [Column("empresa_id")]
        public int? EmpresaId { get; set; }

        [Column("id_orden_produccion_erpcorpsaf")]
        public string? IdOrdenProduccionErpCorpsaf { get; set; }

        [Column("desc_orden_produccion_erpcorpsaf")]
        public string? DescOrdenProduccionErpCorpsaf { get; set; }
    }
}
