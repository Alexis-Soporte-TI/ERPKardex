using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ERPKardex.Models
{
    [Table("ingrediente_activo")]
    public class IngredienteActivo
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Column("descripcion")]
        public string? Descripcion { get; set; }
    }
}