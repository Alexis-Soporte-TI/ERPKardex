using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ERPKardex.Models
{
    [Table("dorden_pedido")]
    public class DOrdenPedido
    {
        [Key] public int Id { get; set; }
        [Column("orden_pedido_id")] public int? OrdenPedidoId { get; set; }
        [Column("item")] public string? Item { get; set; }
        [Column("producto_id")] public int? ProductoId { get; set; }
        [Column("descripcion")] public string? Descripcion { get; set; }
        [Column("unidad_medida")] public string? UnidadMedida { get; set; }
        [Column("cantidad")] public decimal? Cantidad { get; set; }
        [Column("cantidad_atendida")] public decimal? CantidadAtendida { get; set; } = 0;
        [Column("precio_unitario")] public decimal? PrecioUnitario { get; set; }
        [Column("porc_descuento")] public decimal? PorcDescuento { get; set; } = 0;
        [Column("valor_venta")] public decimal? ValorVenta { get; set; }
        [Column("impuesto")] public decimal? Impuesto { get; set; }

        [Column("monto_isc")] public decimal? MontoIsc { get; set; } = 0;
        [Column("monto_icbper")] public decimal? MontoIcbper { get; set; } = 0;

        [Column("total")] public decimal? Total { get; set; }
        [Column("centro_costo_id")] public int? CentroCostoId { get; set; }
        [Column("estado_id")] public int? EstadoId { get; set; }
        [Column("id_referencia")] public int? IdReferencia { get; set; }
        [Column("tabla_referencia")] public string? TablaReferencia { get; set; }
        [Column("observacion_item")] public string? ObservacionItem { get; set; }
        [Column("empresa_id")] public int? EmpresaId { get; set; }
    }
}