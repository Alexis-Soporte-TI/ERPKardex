using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ERPKardex.Models
{
    [Table("Grupo")]
    public class Grupo
    {
        [Key]
        public int Id { get; set; }
        [Column("codigo")]
        public string Codigo { get; set; } = null!;

        [Column("descripcion")]
        public string? Descripcion { get; set; }
        [Column("cuenta_id")]
        public string? CuentaId { get; set; }
    }
}