using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ERPKardex.Models
{
    [Table("ddocumento_cobrar")]
    public class DDocumentoCobrar
    {
        [Key] public int Id { get; set; }
        [Column("documento_cobrar_id")] public int DocumentoCobrarId { get; set; }
        [Column("item")] public string? Item { get; set; }
        [Column("id_referencia")] public int? IdReferencia { get; set; }
        [Column("tabla_referencia")] public string? TablaReferencia { get; set; }
        [Column("producto_id")] public int? ProductoId { get; set; }
        [Column("descripcion")] public string? Descripcion { get; set; }
        [Column("unidad_medida")] public string? UnidadMedida { get; set; }
        [Column("cantidad")] public decimal? Cantidad { get; set; }
        [Column("precio_unitario")] public decimal? PrecioUnitario { get; set; }

        [Column("monto_isc")] public decimal? MontoIsc { get; set; } = 0;
        [Column("monto_icbper")] public decimal? MontoIcbper { get; set; } = 0;

        [Column("total")] public decimal? Total { get; set; }
    }
}