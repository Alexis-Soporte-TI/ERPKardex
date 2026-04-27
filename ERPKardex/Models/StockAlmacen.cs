using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ERPKardex.Models
{
    [Table("Stock_almacen")]
    public class StockAlmacen
    {
        [Key]
        public int Id { get; set; }
        [Column("almacen_id")]
        public int AlmacenId { get; set; }
        [Column("producto_id")]
        public int? ProductoId { get; set; }
        [Column("stock_actual")]
        [Precision(14, 4)]
        public decimal? StockActual { get; set; }
        [Column("empresa_id")]
        public int? EmpresaId { get; set; }
        [Column("ultima_actualizacion")]
        public DateTime? UltimaActualizacion { get; set; }
        [Column("precio_unitario")]
        [Precision(10, 2)]
        public decimal? PrecioUnitario { get; set; }
    }
}
