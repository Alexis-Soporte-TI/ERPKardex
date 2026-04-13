using System.ComponentModel.DataAnnotations.Schema;

namespace ERPKardex.Models
{
    [Table("Empresa")]
    public class Empresa
    {
        public int Id { get; set; }
        public string? Ruc { get; set; }
        [Column("razon_social")]
        public string? RazonSocial { get; set; }
        public string? Nombre { get; set; }
        public string? Direccion { get; set; }
        public bool? Estado { get; set; }
        [Column("id_empresa_erpcorpsaf")]
        public int? IdEmpresaCorpsaf { get; set; }
    }
}
