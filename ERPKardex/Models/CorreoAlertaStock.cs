using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ERPKardex.Models
{
    [Table("correo_alerta_stock")]
    public class CorreoAlertaStock
    {
        [Key]
        public int Id { get; set; }
        [Column("empresa_id")]
        public int EmpresaId { get; set; }
        [Column("nombre")]
        public string Nombre { get; set; } = null!;
        [Column("email")]
        public string Email { get; set; } = null!;
        // SEMANAL o BISEMANAL
        [Column("frecuencia")]
        public string Frecuencia { get; set; } = "SEMANAL";
        // 1=Lunes, 2=Martes, ... 7=Domingo
        [Column("dia_envio")]
        public int DiaEnvio { get; set; } = 1;
        [Column("activo")]
        public bool Activo { get; set; } = true;
        [Column("fecha_registro")]
        public DateTime? FechaRegistro { get; set; }
    }
}
