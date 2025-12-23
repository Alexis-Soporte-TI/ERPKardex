using Microsoft.EntityFrameworkCore; // Necesario para [Precision]
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ERPKardex.Models
{
    [Table("dingresosalidaalm")]
    public class DIngresoSalidaAlm
    {
        [Key]
        public int Id { get; set; }
        [Column("ingresosalidaalm_id")]
        public int? IngresoSalidaAlmId { get; set; }
        public string? Item { get; set; }
        [Column("cod_producto")]
        public string? CodProducto { get; set; }
        [Column("descripcion_producto")]
        public string? DescripcionProducto { get; set; }
        [Column("cod_unidad_medida")]
        public string? CodUnidadMedida { get; set; }
        [Precision(12, 2)]
        public decimal? Cantidad { get; set; }
        [Column("tipo_documento_id")]
        public int? TipoDocumentoId { get; set; }
        [Column("serie_documento")]
        public string? SerieDocumento { get; set; }
        [Column("numero_documento")]
        public string? NumeroDocumento { get; set; }
        [Column("moneda_id")]
        public int? MonedaId { get; set; }
        [Column("tipo_cambio")]
        [Precision(12, 6)]
        public decimal? TipoCambio { get; set; }
        [Precision(19, 6)]
        public decimal? Precio { get; set; }
        public decimal? Subtotal { get; set; }
        public decimal? Igv { get; set; }
        public decimal? Total { get; set; }
        [Column("centro_costo_id")]
        public int? CentroCostoId { get; set; }
        [Column("actividad_id")]
        public int? ActividadId { get; set; }
        [Column("fecha_documento")]
        public DateTime? FechaDocumento { get; set; }
        [Column("usuario_id")]
        public int? UsuarioId { get; set; }
        [Column("fecha_registro")]
        public DateTime? FechaRegistro { get; set; }
    }
}