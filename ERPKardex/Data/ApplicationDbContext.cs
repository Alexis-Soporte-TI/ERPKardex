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

        // Tablas Maestras
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

        // Tabla Principal
        public DbSet<Producto> Productos { get; set; }

        // Tabla de Detalle
        public DbSet<DetalleIngredienteActivo> DetallesIngredientesActivos { get; set; }
    }
}