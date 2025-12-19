using System.ComponentModel.DataAnnotations.Schema;

namespace ERPKardex.Models
{
    [Table("Modelo")]
    public class Modelo
    {
        public int Id { get; set; }
        public string? Nombre { get; set; }
        public bool? Estado { get; set; }
        [Column("marca_id")]
        public int? MarcaId { get; set; }
    }
}
