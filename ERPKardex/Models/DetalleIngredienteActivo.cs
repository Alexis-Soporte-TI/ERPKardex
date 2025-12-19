using System.ComponentModel.DataAnnotations.Schema;

namespace ERPKardex.Models
{
    [Table("detalle_ingrediente_activo")]
    public class DetalleIngredienteActivo
    {
        public int Id { get; set; }
        [Column("cod_producto")]
        public string? CodProducto { get; set; }

        [Column("ingrediente_activo_id")]
        public int? IngredienteActivoId { get; set; }
        public decimal? Porcentaje { get; set; }
    }
}