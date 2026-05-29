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
        public DbSet<TipoUsuario> TipoUsuarios { get; set; }
        public DbSet<Usuario> Usuarios { get; set; }
        public DbSet<EmpresaUsuario> EmpresaUsuarios { get; set; }
        public DbSet<Permiso> Permisos { get; set; }
        public DbSet<EmpresaUsuarioPermiso> EmpresaUsuarioPermisos { get; set; }
        public DbSet<Grupo> Grupos { get; set; }
        public DbSet<Subgrupo> Subgrupos { get; set; }
        public DbSet<TipoExistencia> TipoExistencias { get; set; }
        public DbSet<TipoCuenta> TipoCuentas { get; set; }
        public DbSet<Cuenta> Cuentas { get; set; }
        public DbSet<UnidadMedida> UnidadesMedida { get; set; }
        public DbSet<FormulacionQuimica> FormulacionesQuimicas { get; set; }
        public DbSet<Peligrosidad> Peligrosidades { get; set; }
        public DbSet<IngredienteActivo> IngredientesActivos { get; set; }
        public DbSet<Marca> Marcas { get; set; }
        public DbSet<Modelo> Modelos { get; set; }
        public DbSet<TipoInsumo> TipoInsumos { get; set; }
        public DbSet<Producto> Productos { get; set; }
        public DbSet<DetalleIngredienteActivo> DetallesIngredientesActivos { get; set; }
        public DbSet<Sucursal> Sucursales { get; set; }
        public DbSet<Almacen> Almacenes { get; set; }
        public DbSet<StockAlmacen> StockAlmacenes { get; set; }
        public DbSet<Motivo> Motivos { get; set; }
        public DbSet<TipoDocumento> TipoDocumentos { get; set; }
        public DbSet<Moneda> Monedas { get; set; }
        public DbSet<IngresoSalidaAlm> IngresoSalidaAlms { get; set; }
        public DbSet<DIngresoSalidaAlm> DIngresoSalidaAlms { get; set; }
        public DbSet<Estado> Estados { get; set; }
        public DbSet<CentroCosto> CentroCostos { get; set; }
        public DbSet<Actividad> Actividades { get; set; }
        public DbSet<Proveedor> Proveedores { get; set; }
        public DbSet<Cliente> Clientes { get; set; }
        public DbSet<TipoDocumentoInterno> TiposDocumentoInterno { get; set; }
        public DbSet<ReqCompra> ReqCompras { get; set; }
        public DbSet<DReqCompra> DReqCompras { get; set; }
        public DbSet<ReqServicio> ReqServicios { get; set; }
        public DbSet<DReqServicio> DReqServicios { get; set; }
        public DbSet<PedCompra> PedCompras { get; set; }
        public DbSet<DPedCompra> DPedidoCompras { get; set; }
        public DbSet<PedServicio> PedServicios { get; set; }
        public DbSet<DPedServicio> DPedidosServicio { get; set; }
        public DbSet<OrdenCompra> OrdenCompras { get; set; }
        public DbSet<DOrdenCompra> DOrdenCompras { get; set; }
        public DbSet<OrdenServicio> OrdenServicios { get; set; }
        public DbSet<DOrdenServicio> DOrdenServicios { get; set; }
        public DbSet<Personal> Personal { get; set; }
        public DbSet<TipoActivo> TipoActivo { get; set; }
        public DbSet<Activo> Activo { get; set; }
        public DbSet<ActivoDetalle> ActivoDetalle { get; set; }
        public DbSet<TipoDocumentoActivo> TipoDocumentoActivo { get; set; }
        public DbSet<ActivoDocumento> ActivoDocumento { get; set; }
        public DbSet<GrupoActivo> GrupoActivo { get; set; }
        public DbSet<MovimientoActivo> MovimientoActivo { get; set; }
        public DbSet<DMovimientoActivo> DMovimientoActivo { get; set; }
        public DbSet<GpsVehiculo> GpsVehiculo { get; set; }
        public DbSet<MantenimientoVehiculo> MantenimientoVehiculo { get; set; }
        public DbSet<SeguroVehiculo> SeguroVehiculo { get; set; }
        public DbSet<InfraccionVehiculo> InfraccionVehiculo { get; set; }
        public DbSet<BitacoraKilometraje> BitacoraKilometraje { get; set; }
        public DbSet<TipoCambio> TipoCambios { get; set; }
        public DbSet<Banco> Bancos { get; set; }
        public DbSet<TipoOrdenPago> TipoOrdenPagos { get; set; }
        public DbSet<OrdenPago> OrdenPagos { get; set; }
        public DbSet<DocumentoPagar> DocumentosPagar { get; set; }
        public DbSet<DDocumentoPagar> DDocumentosPagar { get; set; }
        public DbSet<DocumentoPagarAplicacion> DocumentoPagarAplicaciones { get; set; }
        public DbSet<Origen> Origenes { get; set; }
        public DbSet<TipoDocumentoIdentidad> TiposDocumentoIdentidad { get; set; }
        public DbSet<TipoPersona> TiposPersona { get; set; }
        public DbSet<Pais> Paises { get; set; }
        public DbSet<Ciudad> Ciudades { get; set; }
        public DbSet<ConfiguracionGeneral> ConfiguracionesGenerales { get; set; }
        public DbSet<TipoDetraccion> TiposDetracciones { get; set; }
        public DbSet<Detraccion> Detracciones { get; set; }
        public DbSet<Arrendamiento> Arrendamientos { get; set; }
        public DbSet<CuotaArrendamiento> CuotaArrendamientos { get; set; }

        // TABLAS CONTABLES
        public DbSet<CuentaContable> CuentasContables { get; set; }
        public DbSet<PeriodoContable> PeriodosContables { get; set; }
        public DbSet<OrigenAsiento> OrigenesAsiento { get; set; }
        public DbSet<AsientoContable> AsientosContables { get; set; }
        public DbSet<DasientoContable> DetallesAsiento { get; set; }

        // MÓDULO DE VENTAS (CUENTAS POR COBRAR)
        public DbSet<OrdenPedido> OrdenesPedido { get; set; }
        public DbSet<DOrdenPedido> DetallesOrdenPedido { get; set; }
        public DbSet<DocumentoCobrar> DocumentosCobrar { get; set; }
        public DbSet<DDocumentoCobrar> DetallesDocumentoCobrar { get; set; }
        public DbSet<DocumentoCobrarAplicacion> AplicacionesDocumentoCobrar { get; set; }
        public DbSet<AreaSolicitante> AreaSolicitante { get; set; }
        public DbSet<PersonalSolicitante> PersonalSolicitante { get; set; }

        // MÓDULO ALERTAS DE VENCIMIENTO
        public DbSet<ConfigAlertaVencimiento> ConfigAlertasVencimiento { get; set; }
        public DbSet<CorreoAlertaStock> CorreosAlertaStock { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // =================================================================
            // CONFIGURACIÓN DE SEGURIDAD (SOLO RESTRICCIONES)
            // =================================================================

            // Restricción Única: Evitar que se asigne el mismo permiso 2 veces
            // al mismo vínculo de usuario.
            modelBuilder.Entity<EmpresaUsuarioPermiso>()
                .HasIndex(p => new { p.EmpresaUsuarioId, p.PermisoId })
                .IsUnique();

            // =================================================================
            // TIPOS DE DATOS MONETARIOS (Global)
            // =================================================================
            // 1. PRIMERO: Configuramos el default para TODOS los decimales
            foreach (var entityType in modelBuilder.Model.GetEntityTypes())
            {
                var properties = entityType.GetProperties()
                    .Where(p => p.ClrType == typeof(decimal) || p.ClrType == typeof(decimal?));

                foreach (var property in properties)
                {
                    // Verificamos si ya tiene una precisión definida (vía atributo o manualmente)
                    // Si GetPrecision() es null, es que EF todavía no tiene una regla específica
                    if (property.GetPrecision() == null && property.GetColumnType() == null)
                    {
                        property.SetPrecision(18);
                        property.SetScale(2);
                    }
                }
            }

            modelBuilder.Entity<ReqCompra>().ToTable(tb => tb.HasTrigger("trg_Audit_reqcompra"));
            modelBuilder.Entity<ReqServicio>().ToTable(tb => tb.HasTrigger("trg_Audit_reqservicio"));
            modelBuilder.Entity<PedCompra>().ToTable(tb => tb.HasTrigger("trg_Audit_pedcompra"));
            modelBuilder.Entity<PedServicio>().ToTable(tb => tb.HasTrigger("trg_Audit_pedservicio"));

            modelBuilder.Entity<OrdenCompra>().ToTable(tb => tb.HasTrigger("trg_Audit_ordencompra"));
            modelBuilder.Entity<OrdenServicio>().ToTable(tb => tb.HasTrigger("trg_Audit_ordenservicio"));

            modelBuilder.Entity<DocumentoPagar>().ToTable(tb => tb.HasTrigger("trg_Audit_documento_pagar"));
            modelBuilder.Entity<OrdenPago>().ToTable(tb => tb.HasTrigger("trg_Audit_orden_pago"));

            // grupo_activo -> tipo_activo
            modelBuilder.Entity<GrupoActivo>()
                .HasOne<TipoActivo>()
                .WithMany()
                .HasForeignKey(g => g.TipoActivoId)
                .OnDelete(DeleteBehavior.Restrict);

            // activo -> tipo_activo
            modelBuilder.Entity<Activo>()
                .HasOne<TipoActivo>()
                .WithMany()
                .HasForeignKey(a => a.TipoActivoId)
                .OnDelete(DeleteBehavior.Restrict);

            // activo -> grupo_activo
            modelBuilder.Entity<Activo>()
                .HasOne<GrupoActivo>()
                .WithMany()
                .HasForeignKey(a => a.GrupoActivoId)
                .OnDelete(DeleteBehavior.Restrict);

            // activo -> empresa
            modelBuilder.Entity<Activo>()
                .HasOne<Empresa>()
                .WithMany()
                .HasForeignKey(a => a.EmpresaId)
                .OnDelete(DeleteBehavior.Restrict);

            // activo_detalle -> activo
            modelBuilder.Entity<ActivoDetalle>()
                .HasOne<Activo>()
                .WithMany()
                .HasForeignKey(d => d.ActivoId)
                .OnDelete(DeleteBehavior.Cascade);

            // activo_documento -> activo
            modelBuilder.Entity<ActivoDocumento>()
                .HasOne<Activo>()
                .WithMany()
                .HasForeignKey(d => d.ActivoId)
                .OnDelete(DeleteBehavior.Cascade);

            // activo_documento -> tipo_documento_activo
            modelBuilder.Entity<ActivoDocumento>()
                .HasOne<TipoDocumentoActivo>()
                .WithMany()
                .HasForeignKey(d => d.TipoDocumentoActivoId)
                .OnDelete(DeleteBehavior.Restrict);

            // movimiento_activo -> empresa
            modelBuilder.Entity<MovimientoActivo>()
                .HasOne<Empresa>()
                .WithMany()
                .HasForeignKey(m => m.EmpresaId)
                .OnDelete(DeleteBehavior.Restrict);

            // movimiento_activo -> personal
            modelBuilder.Entity<MovimientoActivo>()
                .HasOne<Personal>()
                .WithMany()
                .HasForeignKey(m => m.PersonalId)
                .OnDelete(DeleteBehavior.Restrict);

            // dmovimiento_activo -> movimiento_activo
            modelBuilder.Entity<DMovimientoActivo>()
                .HasOne<MovimientoActivo>()
                .WithMany()
                .HasForeignKey(d => d.MovimientoActivoId)
                .OnDelete(DeleteBehavior.Cascade);

            // dmovimiento_activo -> activo
            modelBuilder.Entity<DMovimientoActivo>()
                .HasOne<Activo>()
                .WithMany()
                .HasForeignKey(d => d.ActivoId)
                .OnDelete(DeleteBehavior.Restrict);

            // bitacora_kilometraje -> activo
            modelBuilder.Entity<BitacoraKilometraje>()
                .HasOne<Activo>()
                .WithMany()
                .HasForeignKey(b => b.ActivoId)
                .OnDelete(DeleteBehavior.Cascade);

            // mantenimiento_vehiculo -> activo
            modelBuilder.Entity<MantenimientoVehiculo>()
                .HasOne<Activo>()
                .WithMany()
                .HasForeignKey(m => m.ActivoId)
                .OnDelete(DeleteBehavior.Cascade);

            // infraccion_vehiculo -> activo
            modelBuilder.Entity<InfraccionVehiculo>()
                .HasOne<Activo>()
                .WithMany()
                .HasForeignKey(i => i.ActivoId)
                .OnDelete(DeleteBehavior.Cascade);

            // seguro_vehiculo -> activo
            modelBuilder.Entity<SeguroVehiculo>()
                .HasOne<Activo>()
                .WithMany()
                .HasForeignKey(s => s.ActivoId)
                .OnDelete(DeleteBehavior.Cascade);

            // gps_vehiculo -> activo
            modelBuilder.Entity<GpsVehiculo>()
                .HasOne<Activo>()
                .WithMany()
                .HasForeignKey(g => g.ActivoId)
                .OnDelete(DeleteBehavior.Cascade);

            // Índice único: activo.codigo
            modelBuilder.Entity<Activo>()
                .HasIndex(a => a.Codigo)
                .IsUnique();

            // Índice único: movimiento_activo.codigo
            modelBuilder.Entity<MovimientoActivo>()
                .HasIndex(m => m.Codigo)
                .IsUnique();

            modelBuilder.Entity<CuotaArrendamiento>()
                .HasIndex(c => new { c.ArrendamientoId, c.PeriodoAnioMes })
                .IsUnique();
        }
    }
}