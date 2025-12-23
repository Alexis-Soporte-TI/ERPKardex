using ERPKardex.Models;
using Microsoft.EntityFrameworkCore;

namespace ERPKardex.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }
        public DbSet<Empresa> Empresas { get; set; }
        public DbSet<Grupo> Grupos { get; set; }
        public DbSet<Subgrupo> Subgrupos { get; set; }
        public DbSet<Cuenta> Cuentas { get; set; }
        public DbSet<UnidadMedida> UnidadesMedida { get; set; }
        public DbSet<FormulacionQuimica> FormulacionesQuimicas { get; set; }
        public DbSet<Peligrosidad> Peligrosidades { get; set; }
        public DbSet<IngredienteActivo> IngredientesActivos { get; set; }
        public DbSet<Marca> Marcas { get; set; }
        public DbSet<Modelo> Modelos { get; set; }
        public DbSet<Producto> Productos { get; set; }
        public DbSet<DetalleIngredienteActivo> DetallesIngredientesActivos { get; set; }
        public DbSet<Sucursal> Sucursales { get; set; }
        public DbSet<Almacen> Almacenes { get; set; }
        public DbSet<Motivo> Motivos { get; set; }
        public DbSet<TipoDocumento> TipoDocumentos { get; set; }
        public DbSet<Moneda> Monedas { get; set; }
        public DbSet<IngresoSalidaAlm> IngresoSalidaAlms { get; set; }
        public DbSet<DIngresoSalidaAlm> DIngresoSalidaAlms { get; set; }
        public DbSet<Estado> Estados { get; set; }
        public DbSet<CentroCosto> CentroCostos { get; set; }
        public DbSet<Actividad> Actividades { get; set; }
    }
}