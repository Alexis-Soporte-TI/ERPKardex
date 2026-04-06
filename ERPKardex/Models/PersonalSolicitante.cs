using System.ComponentModel.DataAnnotations.Schema;

namespace ERPKardex.Models
{
    [Table("personal_solicitante")]
    public class PersonalSolicitante
    {
        public int Id { get; set; }
        public string? Nombre { get; set; }
        public bool? Estado { get; set; }
    }
}
