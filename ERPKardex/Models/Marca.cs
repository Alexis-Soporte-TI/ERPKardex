using System.ComponentModel.DataAnnotations.Schema;

namespace ERPKardex.Models
{
    [Table("Marca")]
    public class Marca
    {
        public int Id { get; set; }
        public string? Nombre { get; set; }
        public bool? Estado { get; set; }
    }
}
