using System.ComponentModel.DataAnnotations.Schema;

namespace ERPKardex.Models
{
    [Table("area_solicitante")]
    public class AreaSolicitante
    {
        public int Id { get; set; }
        public string? Nombre { get; set; }
        public bool? Estado { get; set; }
    }
}
