using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ERPKardex.Models
{
    [Table("Subgrupo")]
    public class Subgrupo
    {
        [Key]
        public int Id { get; set; }
        [Column("codigo")]
        public string Codigo { get; set; } = null!;

        [Column("descripcion")]
        public string? Descripcion { get; set; }
        [Column("grupo_id")]
        public int? GrupoId { get; set; }

        [Column("cod_grupo")]
        public string? CodGrupo { get; set; }

        [Column("descripcion_grupo")]
        public string? DescripcionGrupo { get; set; }

        [Column("observacion")]
        public string? Observacion { get; set; }
    }
}