using ERPKardex.Data;
using ERPKardex.Models;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

namespace ERPKardex.Controllers
{
    public class ProductoController : Controller
    {
        private readonly ApplicationDbContext _context;
        public ProductoController(ApplicationDbContext context)
        {
            _context = context;
        }
        #region VISTAS
        public IActionResult Index()
        {
            return View();
        }
        public IActionResult Registrar()
        {
            return View();
        }
        public IActionResult Editar(string id)
        {
            return View();
        }
        public IActionResult Ver(string id)
        {
            return View();
        }
        public IActionResult ObtenerVistaRegistroGrupo()
        {
            // Retorna solo el pedazo de HTML sin layout
            return PartialView("_RegistrarGrupo");
        }
        public IActionResult ObtenerVistaRegistroSubgrupo()
        {
            return PartialView("_RegistrarSubgrupo");
        }
        public IActionResult ObtenerVistaRegistroUnidadMedida()
        {
            return PartialView("_RegistrarUnidadMedida");
        }
        public IActionResult ObtenerVistaRegistroFormulacion()
        {
            return PartialView("_RegistrarFormulacion");
        }
        public IActionResult ObtenerVistaRegistroPeligrosidad()
        {
            return PartialView("_RegistrarPeligrosidad");
        }
        public IActionResult ObtenerVistaRegistroEmpresa()
        {
            return PartialView("_RegistrarEmpresa");
        }
        public IActionResult ObtenerVistaRegistroMarca()
        {
            return PartialView("_RegistrarMarca");
        }
        public IActionResult ObtenerVistaRegistroModelo()
        {
            return PartialView("_RegistrarModelo");
        }
        // Retorna la vista parcial
        public IActionResult ObtenerVistaRegistroCuenta()
        {
            return PartialView("_RegistrarCuenta");
        }
        public IActionResult ObtenerVistaRegistroIA()
        {
            return PartialView("_RegistrarIngredienteActivo");
        }
        #endregion
        #region APIs
        // GET PRODUCTOS
        public JsonResult GetProductosData()
        {
            try
            {
                var productosData = _context.Productos.ToList();
                return Json(new { data = productosData, message = "Productos retornados exitosamente.", status = true });
            }
            catch (Exception ex)
            {
                return Json(new ApiResponse { data = null, message = ex.Message, status = false });
            }
        }
        // GET EMPRESAS
        public JsonResult GetEmpresaData()
        {
            try
            {
                var empresaData = _context.Empresas.ToList();
                return Json(new { data = empresaData, message = "Empresas retornadas exitosamente.", status = true });
            }
            catch (Exception ex)
            {
                return Json(new ApiResponse { data = null, message = ex.Message, status = false });
            }
        }
        // GET CUENTAS
        public JsonResult GetCuentaData()
        {
            try
            {
                var data = _context.Cuentas.ToList();
                return Json(new { data = data, status = true });
            }
            catch (Exception ex)
            {
                return Json(new { status = false, message = ex.Message });
            }
        }

        // GET GRUPOS FILTRADOS POR CUENTA
        public JsonResult GetGruposByCuenta(string cuentaId)
        {
            try
            {
                var data = _context.Grupos.Where(g => g.CuentaId == cuentaId).ToList();
                return Json(new { data = data, status = true });
            }
            catch (Exception ex)
            {
                return Json(new { status = false, message = ex.Message });
            }
        }
        // GET SUBGRUPOS
        // Filtrar Subgrupos por Grupo
        public JsonResult GetSubgruposByGrupo(string codGrupo)
        {
            try
            {
                var data = _context.Subgrupos.Where(s => s.CodGrupo == codGrupo).ToList();
                return Json(new { data = data, status = true, message = "Subgrupos retornados exitosamente." });
            }
            catch (Exception ex)
            {
                return Json(new { status = false, message = ex.Message });
            }
        }

        // Filtrar Modelos por Marca
        public JsonResult GetModelosByMarca(int marcaId)
        {
            try
            {
                var data = _context.Modelos.Where(m => m.MarcaId == marcaId).ToList();
                return Json(new { data = data, status = true, message = "Modelos retornados exitosamente." });
            }
            catch (Exception ex)
            {
                return Json(new { status = false, message = ex.Message });
            }
        }
        // GET UMEDIDA
        public JsonResult GetUMedidaData()
        {
            try
            {
                var umedidaData = _context.UnidadesMedida.ToList();
                return Json(new { data = umedidaData, message = "Unidades de medida retornadas exitosamente.", status = true });
            }
            catch (Exception ex)
            {
                return Json(new ApiResponse { data = null, message = ex.Message, status = false });
            }
        }
        // GET FQ
        public JsonResult GetFQData()
        {
            try
            {
                var fqsData = _context.FormulacionesQuimicas.ToList();
                return Json(new { data = fqsData, message = "FQs retornados exitosamente.", status = true });
            }
            catch (Exception ex)
            {
                return Json(new ApiResponse { data = null, message = ex.Message, status = false });
            }
        }
        // GET Peligrosidad
        public JsonResult GetPeligrosidadData()
        {
            try
            {
                var peligrosidadData = _context.Peligrosidades.ToList();
                return Json(new { data = peligrosidadData, message = "Peligrosidades retornadas exitosamente.", status = true });
            }
            catch (Exception ex)
            {
                return Json(new ApiResponse { data = null, message = ex.Message, status = false });
            }
        }
        [HttpGet]
        public JsonResult GetMarcaData()
        {
            try
            {
                var marcas = _context.Marcas
                    .OrderBy(m => m.Nombre)
                    .ToList();

                return Json(new
                {
                    data = marcas,
                    message = "Marcas retornadas exitosamente.",
                    status = true
                });
            }
            catch (Exception ex)
            {
                return Json(new { status = false, message = ex.Message });
            }
        }

        [HttpGet]
        public JsonResult GetModeloData()
        {
            try
            {
                var modelos = _context.Modelos.ToList();
                return Json(new
                {
                    data = modelos,
                    message = "Modelos retornados exitosamente.",
                    status = true
                });
            }
            catch (Exception ex)
            {
                return Json(new { status = false, message = ex.Message });
            }
        }
        public JsonResult GetIngredienteActivoData()
        {
            try
            {
                var data = _context.IngredientesActivos.ToList();
                return Json(new { data = data, status = true });
            }
            catch (Exception ex)
            {
                return Json(new ApiResponse { data = null, message = ex.Message, status = false });
            }
        }

        [HttpPost]
        public JsonResult RegistrarIngredienteActivo(IngredienteActivo ia)
        {
            try
            {
                _context.IngredientesActivos.Add(ia);
                _context.SaveChanges();
                return Json(new { status = true, message = "Ingrediente registrado" });
            }
            catch (Exception ex)
            {
                return Json(new ApiResponse { data = null, message = ex.Message, status = false });
            }
        }

        // POST Registrar empresa
        [HttpPost]
        public JsonResult RegistrarEmpresa(Empresa empresa)
        {
            try
            {
                if (_context.Empresas.Any(e => e.Ruc == empresa.Ruc))
                {
                    return Json(new ApiResponse { data = null, message = "Ya existe una empresa registrada con el RUC ingresado.", status = false });
                }

                _context.Empresas.Add(empresa);
                _context.SaveChanges();
                return Json(new ApiResponse { data = null, message = "Empresa registrada exitosamente.", status = true });
            }
            catch (Exception ex)
            {
                return Json(new ApiResponse { data = null, message = ex.Message, status = false });
            }
        }
        // POST Registrar cuenta
        [HttpPost]
        public JsonResult RegistrarCuenta(Cuenta cuenta)
        {
            try
            {
                if (_context.Cuentas.Any(c => c.Codigo == cuenta.Codigo))
                {
                    return Json(new { status = false, message = "El código de cuenta ya existe." });
                }

                _context.Cuentas.Add(cuenta);
                _context.SaveChanges();
                return Json(new { status = true, message = "Cuenta registrada exitosamente." });
            }
            catch (Exception ex)
            {
                return Json(new { status = false, message = "Error: " + ex.Message });
            }
        }
        // POST Registrar Grupo
        [HttpPost]
        public JsonResult RegistrarGrupo(Grupo grupo)
        {
            try
            {
                if (_context.Grupos.Any(g => g.Codigo == grupo.Codigo))
                    return Json(new { status = false, message = "El código de grupo ya existe." });

                _context.Grupos.Add(grupo);
                _context.SaveChanges();
                return Json(new { status = true, message = "Grupo registrado exitosamente." });
            }
            catch (Exception ex)
            {
                return Json(new { status = false, message = ex.Message });
            }
        }
        [HttpPost]
        public JsonResult RegistrarSubgrupo(Subgrupo subgrupo)
        {
            try
            {
                if (_context.Subgrupos.Any(s => s.Codigo == subgrupo.Codigo))
                {
                    return Json(new { status = false, message = "El código de subgrupo ya existe." });
                }

                _context.Subgrupos.Add(subgrupo);
                _context.SaveChanges();
                return Json(new { status = true, message = "Subgrupo registrado exitosamente." });
            }
            catch (Exception ex)
            {
                return Json(new { status = false, message = ex.Message });
            }
        }
        [HttpPost]
        public JsonResult RegistrarUnidadMedida(UnidadMedida unidad)
        {
            try
            {
                if (_context.UnidadesMedida.Any(u => u.Codigo == unidad.Codigo))
                {
                    return Json(new { status = false, message = "El código de unidad ya existe." });
                }

                _context.UnidadesMedida.Add(unidad);
                _context.SaveChanges();
                return Json(new { status = true, message = "Unidad de medida registrada exitosamente." });
            }
            catch (Exception ex)
            {
                return Json(new { status = false, message = ex.Message });
            }
        }
        [HttpPost]
        public JsonResult RegistrarFormulacion(FormulacionQuimica formulacion)
        {
            try
            {
                if (_context.FormulacionesQuimicas.Any(f => f.Codigo == formulacion.Codigo))
                {
                    return Json(new { status = false, message = "El código de formulación ya existe." });
                }

                _context.FormulacionesQuimicas.Add(formulacion);
                _context.SaveChanges();
                return Json(new { status = true, message = "Formulación registrada exitosamente." });
            }
            catch (Exception ex)
            {
                return Json(new { status = false, message = ex.Message });
            }
        }
        [HttpPost]
        public JsonResult RegistrarPeligrosidad(Peligrosidad peligrosidad)
        {
            try
            {
                if (_context.Peligrosidades.Any(p => p.Codigo == peligrosidad.Codigo))
                {
                    return Json(new { status = false, message = "El código de peligrosidad ya existe." });
                }

                _context.Peligrosidades.Add(peligrosidad);
                _context.SaveChanges();
                return Json(new { status = true, message = "Nivel de peligrosidad registrado correctamente." });
            }
            catch (Exception ex)
            {
                return Json(new { status = false, message = ex.Message });
            }
        }
        [HttpPost]
        public JsonResult RegistrarMarca(Marca marca)
        {
            try
            {
                // Opcional: Validar si el nombre ya existe
                if (_context.Marcas.Any(m => m.Nombre == marca.Nombre))
                {
                    return Json(new { status = false, message = "Esta marca ya se encuentra registrada." });
                }

                _context.Marcas.Add(marca);
                _context.SaveChanges();
                return Json(new { status = true, message = "Marca registrada exitosamente." });
            }
            catch (Exception ex)
            {
                return Json(new { status = false, message = ex.Message });
            }
        }
        [HttpPost]
        public JsonResult RegistrarModelo(Modelo modelo)
        {
            try
            {
                // Validar si ya existe el modelo para esa marca específica
                if (_context.Modelos.Any(m => m.Nombre == modelo.Nombre && m.MarcaId == modelo.MarcaId))
                {
                    return Json(new { status = false, message = "Este modelo ya existe para la marca seleccionada." });
                }

                _context.Modelos.Add(modelo);
                _context.SaveChanges();
                return Json(new { status = true, message = "Modelo registrado correctamente." });
            }
            catch (Exception ex)
            {
                return Json(new { status = false, message = ex.Message });
            }
        }
        [HttpPost]
        public JsonResult RegistrarProductoCompleto(Producto producto, string detallesJson)
        {
            using (var transaction = _context.Database.BeginTransaction())
            {
                try
                {
                    // 1. Generación Automática del Código
                    // El prefijo es: CodGrupo + CodSubgrupo
                    string prefijo = $"{producto.CodGrupo}{producto.CodSubgrupo}";

                    // Buscamos el último producto cuyo código inicie con ese prefijo
                    var ultimoProducto = _context.Productos
                        .Where(p => p.Codigo.StartsWith(prefijo))
                        .OrderByDescending(p => p.Codigo)
                        .FirstOrDefault();

                    int nuevoCorrelativo = 1;

                    if (ultimoProducto != null)
                    {
                        // Extraemos los últimos 5 dígitos del código actual
                        string parteCorrelativa = ultimoProducto.Codigo.Substring(prefijo.Length);
                        if (int.TryParse(parteCorrelativa, out int ultimoCorrelativo))
                        {
                            nuevoCorrelativo = ultimoCorrelativo + 1;
                        }
                    }

                    // Formateamos: Prefijo + Correlativo con ceros a la izquierda (5 dígitos)
                    producto.Codigo = $"{prefijo}{nuevoCorrelativo.ToString("D5")}";

                    // 2. Validar si por algún motivo extraño el código generado ya existe
                    if (_context.Productos.Any(p => p.Codigo == producto.Codigo))
                        return Json(new { status = false, message = $"Error crítico: El código generado {producto.Codigo} ya existe." });

                    // --- LÓGICA DE DESCRIPCIÓN COMERCIAL ---

                    if (string.IsNullOrEmpty(producto.DescripcionComercial))
                    {
                        // Caso A: Producto con Identificación Técnica (Marca, Modelo, Serie)
                        if (!string.IsNullOrEmpty(producto.DescripcionProducto) &&
                            producto.MarcaId != null &&
                            producto.ModeloId != null &&
                            !string.IsNullOrEmpty(producto.Serie))
                        {
                            // Obtenemos los nombres de marca y modelo para la concatenación
                            var marcaNom = _context.Marcas.Find(producto.MarcaId)?.Nombre;
                            var modeloNom = _context.Modelos.Find(producto.ModeloId)?.Nombre;

                            producto.DescripcionComercial = $"{producto.DescripcionProducto} {marcaNom} {modeloNom} {producto.Serie}".ToUpper();
                        }
                        // Caso B: Producto Insumo/Químico (Concentración + Formulación)
                        else if (!string.IsNullOrEmpty(producto.DescripcionProducto) &&
                                 producto.Concentracion != null &&
                                 !string.IsNullOrEmpty(producto.CodFormulacionQuimica))
                        {
                            // Obtenemos el nombre de la formulación
                            var fqNom = _context.FormulacionesQuimicas.FirstOrDefault(f => f.Codigo == producto.CodFormulacionQuimica)?.Nombre;

                            producto.DescripcionComercial = $"{producto.DescripcionProducto} {producto.Concentracion} {fqNom}".ToUpper();
                        }
                        else
                        {
                            // IMPORTANTE: Si no cumple ninguno de los dos bloques de campos obligatorios, queda NULL
                            producto.DescripcionComercial = null;
                        }
                    }

                    // 3. Guardar el Producto (Maestro)
                    _context.Productos.Add(producto);
                    _context.SaveChanges();

                    // 4. Procesar Detalles (Ingredientes Activos)
                    if (!string.IsNullOrEmpty(detallesJson))
                    {
                        var listaDetalles = JsonConvert.DeserializeObject<List<DetalleIngredienteActivo>>(detallesJson);
                        foreach (var detalle in listaDetalles)
                        {
                            detalle.Id = 0; // Para que SQL maneje el IDENTITY
                            detalle.CodProducto = producto.Codigo; // El código autogenerado arriba
                            _context.DetallesIngredientesActivos.Add(detalle);
                        }
                        _context.SaveChanges();
                    }

                    transaction.Commit();
                    return Json(new
                    {
                        status = true,
                        message = $"Producto registrado exitosamente con el código: {producto.Codigo}",
                        codigoGenerado = producto.Codigo
                    });
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    return Json(new { status = false, message = "Error: " + ex.InnerException?.Message ?? ex.Message });
                }
            }
        }
        #endregion
    }
}
