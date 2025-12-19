using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ERPKardex.Models
{
    [Table("formulacion_quimica")]
    public class FormulacionQuimica
    {
        [Key]
        [Column("codigo")]
        public string Codigo { get; set; } = null!;

        [Column("nombre")]
        public string? Nombre { get; set; }

        [Column("descripcion")]
        public string? Descripcion { get; set; }

        [Column("ejemplo")]
        public string? Ejemplo { get; set; }
    }
}