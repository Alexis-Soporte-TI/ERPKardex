using System.ComponentModel.DataAnnotations.Schema;

namespace ERPKardex.Models
{
    [Table("Centro_costo")]
    public class CentroCosto
    {
        public int Id { get; set; }
        public string? Codigo { get; set; }
        public string? Nombre { get; set; }
        [Column("empresa_id")]
        public int? EmpresaId { get; set; }
        [Column("padre_id")]
        public int? PadreId { get; set; }
        [Column("es_imputable")]
        public bool? EsImputable { get; set; }
        public bool? Estado { get; set; }
        [Column("fecha_registro")]
        public DateTime FechaRegistro { get; set; }
    }
}
