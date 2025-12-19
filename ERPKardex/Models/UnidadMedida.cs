using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ERPKardex.Models
{
    [Table("Unidad_Medida")]
    public class UnidadMedida
    {
        [Key]
        [Column("codigo")]
        public string Codigo { get; set; } = null!;

        [Column("descripcion")]
        public string? Descripcion { get; set; }
    }
}