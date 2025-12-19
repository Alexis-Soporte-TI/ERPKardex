using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ERPKardex.Models
{
    [Table("producto")]
    public class Producto
    {
        [Key]
        [Column("codigo")]
        public string Codigo { get; set; } = null!;
        [Column("empresa_id")]
        public int? EmpresaId { get; set; }
        [Column("cod_grupo")]
        public string? CodGrupo { get; set; }

        [Column("descripcion_grupo")]
        public string? DescripcionGrupo { get; set; }
        [Column("descripcion_comercial")]
        public string? DescripcionComercial { get; set; }

        [Column("cod_subgrupo")]
        public string? CodSubgrupo { get; set; }

        [Column("descripcion_subgrupo")]
        public string? DescripcionSubgrupo { get; set; }

        [Column("descripcion_producto")]
        public string? DescripcionProducto { get; set; }

        [Column("concentracion")]
        public decimal? Concentracion { get; set; }

        [Column("cod_formulacion_quimica")]
        public string? CodFormulacionQuimica { get; set; }

        [Column("lote")]
        public string? Lote { get; set; }

        [Column("fecha_fabricacion")]
        public DateTime? FechaFabricacion { get; set; }

        [Column("fecha_vencimiento")]
        public DateTime? FechaVencimiento { get; set; }

        [Column("cod_peligrosidad")]
        public string? CodPeligrosidad { get; set; }
        [Column("cod_unidad_medida")]
        public string? CodUnidadMedida { get; set; }
        [Column("marca_id")]
        public int? MarcaId { get; set; }
        [Column("modelo_id")]
        public int? ModeloId { get; set; }
        public string? Serie { get; set; }
        public decimal Cantidad { get; set; }

        [Column("es_activo_fijo")]
        public bool? EsActivoFijo { get; set; }

        [Column("estado")]
        public bool? Estado { get; set; }
    }
}