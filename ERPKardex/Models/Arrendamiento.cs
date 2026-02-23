using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ERPKardex.Models
{
    [Table("arrendamiento")]
    public class Arrendamiento
    {
        [Key]
        public int Id { get; set; }

        [Column("moneda_id")] public int? MonedaId { get; set; }
        [Column("empresa_id")] public int EmpresaId { get; set; }

        [Column("direccion_local")] public string DireccionLocal { get; set; } = string.Empty;

        [Column("tipo_uso")] public string TipoUso { get; set; } = "Administrativo";

        [Column("fecha_inicio_contrato")] public DateTime? FechaInicioContrato { get; set; }

        [Column("fecha_fin_contrato")] public DateTime? FechaFinContrato { get; set; }

        [Column("monto_garantia")] public decimal? MontoGarantia { get; set; }

        [Column("monto_alquiler")] public decimal? MontoAlquiler { get; set; }

        [Column("dia_pago")] public int? DiaPago { get; set; }

        // Auditoría
        [Column("usuario_registro")] public int? UsuarioRegistro { get; set; }
        [Column("fecha_registro")] public DateTime? FechaRegistro { get; set; }
        public bool? Estado { get; set; }
    }
}
