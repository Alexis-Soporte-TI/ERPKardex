using ERPKardex.Data;
using ERPKardex.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ERPKardex.Controllers
{
    // Heredamos de BaseController para acceder a EmpresaUsuarioId y UsuarioActualId
    // NOTA: Este módulo es de uso universal; el usuario puede ver y asignar personal a cualquier empresa.
    public class PersonalController : BaseController
    {
        private readonly ApplicationDbContext _context;

        public PersonalController(ApplicationDbContext context)
        {
            _context = context;
        }

        #region VISTAS
        public IActionResult Index() => View();
        public IActionResult Registrar() => View();
        #endregion

        #region APIs - CATÁLOGOS / COMBOS

        // GET: Empresas (para poblar el combo en el formulario)
        [HttpGet]
        public async Task<JsonResult> GetEmpresas()
        {
            try
            {
                var data = await _context.Empresas
                    .Where(e => e.Estado == true)
                    .OrderBy(e => e.Nombre)
                    .Select(e => new { e.Id, e.Nombre, e.Ruc })
                    .ToListAsync();
                return Json(new { status = true, data });
            }
            catch (Exception ex)
            {
                return Json(new { status = false, message = ex.Message });
            }
        }

        #endregion

        #region APIs - LISTADO

        // GET: Listado de personal (usuario universal: se muestra de todas las empresas)
        [HttpGet]
        public async Task<JsonResult> GetData()
        {
            try
            {
                var data = await (from p in _context.Personal
                                  join e in _context.Empresas on p.EmpresaId equals e.Id into ej
                                  from e in ej.DefaultIfEmpty()
                                  where p.Estado == true
                                  orderby p.NombresCompletos
                                  select new
                                  {
                                      p.Id,
                                      p.Dni,
                                      p.NombresCompletos,
                                      p.Cargo,
                                      p.EmpresaId,
                                      Empresa = e != null ? e.Nombre : "",
                                      Ruc = e != null ? e.Ruc : "",
                                      p.Estado,
                                      p.FechaRegistro
                                  }).ToListAsync();

                return Json(new { status = true, data, message = "Personal retornado exitosamente." });
            }
            catch (Exception ex)
            {
                return Json(new { status = false, data = (object?)null, message = ex.Message });
            }
        }

        #endregion

        #region APIs - OBTENER POR ID

        [HttpGet]
        public async Task<JsonResult> GetPersonalById(int id)
        {
            try
            {
                var personal = await _context.Personal
                    .Where(p => p.Id == id)
                    .Select(p => new
                    {
                        p.Id,
                        p.Dni,
                        p.NombresCompletos,
                        p.Cargo,
                        p.EmpresaId,
                        p.Estado
                    })
                    .FirstOrDefaultAsync();

                if (personal == null)
                    return Json(new { status = false, message = "Personal no encontrado." });

                return Json(new { status = true, data = personal });
            }
            catch (Exception ex)
            {
                return Json(new { status = false, message = ex.Message });
            }
        }

        #endregion

        #region APIs - REGISTRAR / EDITAR / ELIMINAR

        // POST: Registrar personal (la empresa viene desde el combo de la vista)
        [HttpPost]
        public JsonResult Registrar(Personal personal)
        {
            try
            {
                // Validaciones mínimas
                if (personal.EmpresaId == null || personal.EmpresaId <= 0)
                    return Json(new { status = false, message = "Debe seleccionar una empresa." });

                if (string.IsNullOrWhiteSpace(personal.NombresCompletos))
                    return Json(new { status = false, message = "Los nombres completos son obligatorios." });

                if (!string.IsNullOrWhiteSpace(personal.Dni) && personal.Dni.Length != 8)
                    return Json(new { status = false, message = "El DNI debe tener 8 dígitos." });

                // Validar que la empresa exista y esté activa
                bool empresaValida = _context.Empresas.Any(e => e.Id == personal.EmpresaId && e.Estado == true);
                if (!empresaValida)
                    return Json(new { status = false, message = "La empresa seleccionada no es válida." });

                // Validar DNI único dentro de la empresa seleccionada (entre activos)
                if (!string.IsNullOrWhiteSpace(personal.Dni))
                {
                    bool dniExiste = _context.Personal.Any(p =>
                        p.Dni == personal.Dni &&
                        p.EmpresaId == personal.EmpresaId &&
                        p.Estado == true);

                    if (dniExiste)
                        return Json(new { status = false, message = $"Ya existe personal registrado con el DNI {personal.Dni} en la empresa seleccionada." });
                }

                // EmpresaId YA viene desde la vista; no lo sobreescribimos
                personal.Estado = true;
                personal.FechaRegistro = DateTime.Now;
                personal.NombresCompletos = personal.NombresCompletos?.ToUpper().Trim();
                personal.Cargo = personal.Cargo?.ToUpper().Trim();
                personal.Dni = personal.Dni?.Trim();

                _context.Personal.Add(personal);
                _context.SaveChanges();

                return Json(new { status = true, message = "Personal registrado exitosamente." });
            }
            catch (Exception ex)
            {
                return Json(new { status = false, message = "Error: " + (ex.InnerException?.Message ?? ex.Message) });
            }
        }

        // POST: Editar personal (la empresa puede cambiarse desde el combo de la vista)
        [HttpPost]
        public JsonResult Editar(Personal personal)
        {
            try
            {
                var existente = _context.Personal.FirstOrDefault(p => p.Id == personal.Id);

                if (existente == null)
                    return Json(new { status = false, message = "El personal no existe." });

                if (personal.EmpresaId == null || personal.EmpresaId <= 0)
                    return Json(new { status = false, message = "Debe seleccionar una empresa." });

                if (string.IsNullOrWhiteSpace(personal.NombresCompletos))
                    return Json(new { status = false, message = "Los nombres completos son obligatorios." });

                if (!string.IsNullOrWhiteSpace(personal.Dni) && personal.Dni.Length != 8)
                    return Json(new { status = false, message = "El DNI debe tener 8 dígitos." });

                // Validar que la empresa exista y esté activa
                bool empresaValida = _context.Empresas.Any(e => e.Id == personal.EmpresaId && e.Estado == true);
                if (!empresaValida)
                    return Json(new { status = false, message = "La empresa seleccionada no es válida." });

                // Validar DNI único en la empresa seleccionada (excluyendo el registro actual)
                if (!string.IsNullOrWhiteSpace(personal.Dni))
                {
                    bool dniExiste = _context.Personal.Any(p =>
                        p.Dni == personal.Dni &&
                        p.EmpresaId == personal.EmpresaId &&
                        p.Estado == true &&
                        p.Id != personal.Id);

                    if (dniExiste)
                        return Json(new { status = false, message = $"Ya existe otro personal con el DNI {personal.Dni} en la empresa seleccionada." });
                }

                // Campos editables (incluyendo EmpresaId, que ahora puede cambiarse)
                existente.Dni = personal.Dni?.Trim();
                existente.NombresCompletos = personal.NombresCompletos?.ToUpper().Trim();
                existente.Cargo = personal.Cargo?.ToUpper().Trim();
                existente.EmpresaId = personal.EmpresaId;
                // Estado y FechaRegistro NO se modifican desde aquí

                _context.SaveChanges();
                return Json(new { status = true, message = "Personal actualizado correctamente." });
            }
            catch (Exception ex)
            {
                return Json(new { status = false, message = "Error: " + (ex.InnerException?.Message ?? ex.Message) });
            }
        }

        // POST: Eliminación lógica (para el usuario se ve como física porque el listado filtra Estado = true)
        [HttpPost]
        public JsonResult Eliminar(int id)
        {
            try
            {
                var personal = _context.Personal.FirstOrDefault(p => p.Id == id);

                if (personal == null)
                    return Json(new { status = false, message = "El registro no existe." });

                // VALIDACIÓN DE HISTORIAL — descomenta cuando tengas la(s) tabla(s) referenciadas
                // Ejemplo: si el personal tiene activos asignados o movimientos, no se puede eliminar
                //
                // bool tieneAsignaciones = _context.AsignacionActivo.Any(a => a.PersonalId == id && a.Estado);
                // if (tieneAsignaciones)
                //     return Json(new { status = false, message = "No se puede eliminar: el personal tiene activos asignados." });
                //
                // bool tieneMovimientos = _context.MovimientoActivo.Any(m => m.PersonalId == id);
                // if (tieneMovimientos)
                //     return Json(new { status = false, message = "No se puede eliminar: el personal tiene movimientos registrados." });

                personal.Estado = false;
                _context.SaveChanges();

                return Json(new { status = true, message = "Personal eliminado correctamente." });
            }
            catch (Exception ex)
            {
                return Json(new { status = false, message = "Error: " + (ex.InnerException?.Message ?? ex.Message) });
            }
        }

        #endregion
    }
}