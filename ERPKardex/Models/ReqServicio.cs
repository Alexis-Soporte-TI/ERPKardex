using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ERPKardex.Models
{
    [Table("reqservicio")]
    public class ReqServicio
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Column("tipo_documento_interno_id")]
        public int TipoDocumentoInternoId { get; set; }

        [Column("numero")]
        [StringLength(20)]
        public string? Numero { get; set; }

        [Column("fecha_emision")]
        public DateTime? FechaEmision { get; set; }

        [Column("fecha_necesaria")]
        public DateTime? FechaNecesaria { get; set; }

        [Column("usuario_solicitante_id")]
        public int UsuarioSolicitanteId { get; set; }

        [Column("observacion")]
        [StringLength(500)]
        public string? Observacion { get; set; }

        [Column("estado_id")]
        public int EstadoId { get; set; }

        [Column("empresa_id")]
        public int EmpresaId { get; set; }

        [Column("usuario_aprobador")]
        public int? UsuarioAprobador { get; set; }

        [Column("fecha_aprobacion")]
        public DateTime? FechaAprobacion { get; set; }

        [Column("usuario_registro")]
        public int? UsuarioRegistro { get; set; }

        [Column("fecha_registro")]
        public DateTime FechaRegistro { get; set; }
        [Column("periodo_contable_id")]
        public int? PeriodoContableId { get; set; }
        [Column("area_solicitante_id")]
        public int? AreaSolicitanteId { get; set; }
        [Column("personal_solicitante_id")]
        public int? PersonalSolicitanteId { get; set; }
    }
}