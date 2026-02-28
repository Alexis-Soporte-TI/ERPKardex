using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ERPKardex.Models
{
    [Table("documento_cobrar_aplicacion")]
    public class DocumentoCobrarAplicacion
    {
        [Key] public int Id { get; set; }
        [Column("empresa_id")] public int? EmpresaId { get; set; }
        [Column("documento_cargo_id")] public int DocumentoCargoId { get; set; }
        [Column("documento_abono_id")] public int DocumentoAbonoId { get; set; }
        [Column("monto_aplicado")] public decimal MontoAplicado { get; set; }
        [Column("fecha_aplicacion")] public DateTime? FechaAplicacion { get; set; }
        [Column("usuario_id")] public int? UsuarioId { get; set; }
    }
}