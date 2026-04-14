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
        [Column("id_referencia")]
        public int? IdReferencia { get; set; }
        [Column("tabla_referencia")]
        public string? TablaReferencia { get; set; }
        public string? Item { get; set; }
        [Column("producto_id")]
        public int? ProductoId { get; set; }
        [Column("cod_producto")]
        public string? CodProducto { get; set; }
        [Column("descripcion_producto")]
        public string? DescripcionProducto { get; set; }
        [Column("cod_unidad_medida")]
        public string? CodUnidadMedida { get; set; }
        [Precision(12, 4)]
        public decimal? Cantidad { get; set; }
        [Column("tipo_documento_id")]
        public int? TipoDocumentoId { get; set; }
        [Column("serie_documento")]
        public string? SerieDocumento { get; set; }
        [Column("numero_documento")]
        public string? NumeroDocumento { get; set; }
        [Column("moneda_id")]
        public int? MonedaId { get; set; }
        [Column("tipo_cambio", TypeName = "decimal(19,10)")]
        public decimal? TipoCambio { get; set; }
        [Column("precio", TypeName = "decimal(19,10)")]
        public decimal? Precio { get; set; }
        [Column("subtotal", TypeName = "decimal(19,10)")]
        public decimal? Subtotal { get; set; }
        [Column("igv", TypeName = "decimal(19,10)")]
        public decimal? Igv { get; set; }
        [Column("total", TypeName = "decimal(19,10)")]
        public decimal? Total { get; set; }
        [Column("centro_costo_id")]
        public int? CentroCostoId { get; set; }
        [Column("actividad_id")]
        public int? ActividadId { get; set; }
        [Column("lote")]
        public string? Lote { get; set; }
        [Column("fecha_fabricacion")]
        public DateTime? FechaFabricacion { get; set; }
        [Column("fecha_vencimiento")]
        public DateTime? FechaVencimiento { get; set; }
        [Column("fecha_documento")]
        public DateTime? FechaDocumento { get; set; }
        [Column("usuario_id")]
        public int? UsuarioId { get; set; }
        [Column("fecha_registro")]
        public DateTime? FechaRegistro { get; set; }
        [Column("empresa_id")]
        public int? EmpresaId { get; set; }
        [Column("pedido_interno")]
        public string? PedidoInterno { get; set; }
        [Column("id_orden_produccion_erpcorpsaf")]
        public string? IdOrdenProduccionErpcorpsaf { get; set; }
        [Column("desc_orden_produccion_erpcorpsaf")]
        public string? DescOrdenProduccionErpcorpsaf { get; set; }
        [Column("id_actividad_erpcorpsaf")]
        public string? IdActividadErpcorpsaf { get; set; }
        [Column("desc_actividad_erpcorpsaf")]
        public string? DescActividadErpcorpsaf { get; set; }
        [Column("id_labor_erpcorpsaf")]
        public string? IdLaborErpcorpsaf { get; set; }
        [Column("desc_labor_erpcorpsaf")]
        public string? DescLaborErpcorpsaf { get; set; }
    }
}