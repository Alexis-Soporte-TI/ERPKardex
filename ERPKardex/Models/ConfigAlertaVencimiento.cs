using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ERPKardex.Models
{
    [Table("config_alerta_vencimiento")]
    public class ConfigAlertaVencimiento
    {
        [Key]
        public int Id { get; set; }
        [Column("empresa_id")]
        public int EmpresaId { get; set; }
        [Column("meses_critico")]
        public int MesesCritico { get; set; } = 18;
        [Column("meses_alerta")]
        public int MesesAlerta { get; set; } = 24;
        [Column("fecha_registro")]
        public DateTime? FechaRegistro { get; set; }
    }
}
