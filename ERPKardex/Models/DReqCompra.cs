using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ERPKardex.Models
{
    [Table("dreqcompra")]
    public class DReqCompra
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Column("reqcompra_id")]
        public int? ReqCompraId { get; set; }

        [Column("item")]
        [StringLength(3)]
        public string Item { get; set; }

        [Column("producto_id")]
        public int ProductoId { get; set; }

        [Column("centro_costo_id")]
        public int? CentroCostoId { get; set; }

        [Column("descripcion_producto")]
        [StringLength(500)]
        public string? DescripcionProducto { get; set; }

        [Column("unidad_medida")]
        [StringLength(50)]
        public string? UnidadMedida { get; set; }

        [Column("cantidad_solicitada", TypeName = "decimal(14,4)")]
        public decimal CantidadSolicitada { get; set; }

        [Column("estado_id")]
        public int EstadoId { get; set; }
        public string? Lugar { get; set; }

        [Column("empresa_id")]
        public int EmpresaId { get; set; }

        [Column("id_orden_produccion_erpcorpsaf")]
        public string? IdOrdenProduccionErpCorpsaf { get; set; }

        [Column("desc_orden_produccion_erpcorpsaf")]
        public string? DescOrdenProduccionErpCorpsaf { get; set; }
    }
}
