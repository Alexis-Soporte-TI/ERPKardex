using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ERPKardex.Models
{
    [Table("periodo_contable")]
    public class PeriodoContable
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Column("empresa_id")]
        public int EmpresaId { get; set; }

        [Column("anio")]
        public int Anio { get; set; }

        [Column("mes")]
        public int Mes { get; set; }

        [Column("estado")]
        public bool Estado { get; set; } = true;

        [Column("fecha_registro")]
        public DateTime? FechaRegistro { get; set; }
    }
}