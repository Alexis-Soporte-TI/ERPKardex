using System.ComponentModel.DataAnnotations.Schema;

namespace ERPKardex.Models
{
    [Table("Estado")]
    public class Estado
    {
        public int Id { get; set; }
        public string? Nombre { get; set; }
        public string? Tabla { get; set; }
    }
}
