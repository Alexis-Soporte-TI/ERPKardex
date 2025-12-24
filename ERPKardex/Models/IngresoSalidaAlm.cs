using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ERPKardex.Models
{
    [Table("ingresosalidaalm")]
    public class IngresoSalidaAlm
    {
        [Key]
        public int Id { get; set; }
        public string? Cliente { get; set; }
        public DateTime? Fecha { get; set; }
        public string? Numero { get; set; }
        [Column("sucursal_id")]
        public int? SucursalId { get; set; }
        [Column("almacen_id")]
        public int? AlmacenId { get; set; }
        [Column("tipo_movimiento")]
        public bool? TipoMovimiento { get; set; }
        [Column("motivo_id")]
        public int? MotivoId { get; set; }
        [Column("fecha_documento")]
        public DateTime? FechaDocumento { get; set; }
        [Column("tipo_documento_id")]
        public int? TipoDocumentoId { get; set; }
        [Column("serie_documento")]
        public string? SerieDocumento { get; set; }
        [Column("numero_documento")]
        public string? NumeroDocumento { get; set; }
        [Column("moneda_id")]
        public int? MonedaId { get; set; }
        [Column("estado_id")]
        public int? EstadoId { get; set; }
        [Column("usuario_id")]
        public int? UsuarioId { get; set; }
        [Column("fecha_registro")]
        public DateTime? FechaRegistro { get; set; }
    }
}