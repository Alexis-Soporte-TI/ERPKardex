using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ERPKardex.Models
{
    [Table("peligrosidad")]
    public class Peligrosidad
    {
        [Key]
        [Column("codigo")]
        public string Codigo { get; set; } = null!;

        [Column("clase")]
        public string? Clase { get; set; }

        [Column("banda_color")]
        public string? BandaColor { get; set; }

        [Column("descripcion")]
        public string? Descripcion { get; set; }

        [Column("nivel_riesgo")]
        public string? NivelRiesgo { get; set; }

        [Column("uso_senasa")]
        public bool? UsoSenasa { get; set; }
    }
}