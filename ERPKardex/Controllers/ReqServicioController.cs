using ERPKardex.Data;
using ERPKardex.Models;
using ERPKardex.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;

namespace ERPKardex.Controllers
{
    // HERENCIA APLICADA
    public class ReqServicioController : BaseController
    {
        private readonly ApplicationDbContext _context;
        private readonly IEmailService _emailService;

        public ReqServicioController(ApplicationDbContext context, IEmailService emailService)
        {
            _context = context;
            _emailService = emailService;
        }

        public bool EsPeriodoValido(int periodoId)
        {
            return _context.PeriodosContables.Any(p => p.Id == periodoId && p.Estado == true);
        }

        #region 1. VISTAS
        public IActionResult Index() => View();
        public IActionResult Registrar() => View();
        #endregion

        #region 2. LISTADOS (GET)

        // LISTA PRINCIPAL
        [HttpGet]
        public JsonResult GetRequerimientosData()
        {
            try
            {
                var miEmpresaId = EmpresaUsuarioId;
                var esGlobal = EsAdminGlobal;

                var data = (from r in _context.ReqServicios
                            join usu in _context.Usuarios on r.UsuarioSolicitanteId equals usu.Id
                            join est in _context.Estados on r.EstadoId equals est.Id
                            join emp in _context.Empresas on r.EmpresaId equals emp.Id
                            join per in _context.PersonalSolicitante on r.PersonalSolicitanteId equals per.Id into perJoin
                            from per in perJoin.DefaultIfEmpty()
                                // Lógica de filtro limpia
                            where (r.EmpresaId == miEmpresaId)
                            orderby r.Id descending
                            select new
                            {
                                r.Id,
                                Empresa = emp.Nombre,
                                emp.RazonSocial,
                                r.Numero,
                                FechaEmision = r.FechaEmision.GetValueOrDefault().ToString("dd/MM/yyyy HH:mm"),
                                FechaNecesaria = r.FechaNecesaria.GetValueOrDefault().ToString("dd/MM/yyyy"),
                                Solicitante = per.Nombre != null ? per.Nombre : usu.Nombre,
                                Estado = est.Nombre,
                                r.EstadoId,
                                r.Observacion
                            }).ToList();

                return Json(new { status = true, data });
            }
            catch (Exception ex) { return Json(new { status = false, message = ex.Message }); }
        }

        // DETALLE MODAL
        [HttpGet]
        public async Task<JsonResult> GetDetalleReq(int id)
        {
            try
            {
                var cabecera = await _context.ReqServicios
                    .Where(x => x.Id == id)
                    .Select(x => new
                    {
                        x.Numero,
                        x.Observacion,
                        FechaNecesaria = x.FechaNecesaria.GetValueOrDefault().ToString("dd/MM/yyyy")
                    })
                    .FirstOrDefaultAsync();

                var detalles = await (from d in _context.DReqServicios
                                      join cc in _context.CentroCostos on d.CentroCostoId equals cc.Id into ccJoin
                                      from cc in ccJoin.DefaultIfEmpty()
                                      join est in _context.Estados on d.EstadoId equals est.Id into estJoin
                                      from est in estJoin.DefaultIfEmpty()
                                      where d.ReqServicioId == id
                                      select new
                                      {
                                          d.Item,
                                          d.DescripcionServicio,
                                          d.UnidadMedida,
                                          OrdenProduccion = d.IdOrdenProduccionErpCorpsaf != null ? d.DescOrdenProduccionErpCorpsaf : "",
                                          d.Lugar, // Campo LUGAR
                                          d.CantidadSolicitada,
                                          CentroCosto = cc != null ? cc.Nombre : "N/A",
                                          Estado = est != null ? est.Nombre : "-" // Estado del ítem
                                      }).ToListAsync();

                return Json(new { status = true, cabecera, detalles });
            }
            catch (Exception ex) { return Json(new { status = false, message = ex.Message }); }
        }

        // COMBO SERVICIOS
        [HttpGet]
        public async Task<JsonResult> GetServicios()
        {
            try
            {
                var miEmpresaId = EmpresaUsuarioId;

                var data = await _context.Productos
                    .Where(x => (x.EmpresaId == miEmpresaId) && x.Estado == true && x.Codigo.StartsWith("6"))
                    .Select(x => new { x.Id, x.Codigo, x.DescripcionProducto, x.DescripcionComercial, x.CodUnidadMedida })
                    .ToListAsync();

                return Json(new { status = true, data });
            }
            catch (Exception ex) { return Json(new { status = false, message = ex.Message }); }
        }

        // COMBO CENTROS COSTO
        [HttpGet]
        public async Task<JsonResult> GetCentrosCosto()
        {
            try
            {
                var miEmpresaId = EmpresaUsuarioId;
                var esGlobal = EsAdminGlobal;

                var data = await _context.CentroCostos
                    .Where(x => x.EmpresaId == miEmpresaId && x.Estado == true && x.EsImputable == true)
                    .Select(x => new { x.Id, x.Codigo, x.Nombre })
                    .OrderBy(x => x.Codigo)
                    .ToListAsync();

                return Json(new { status = true, data });
            }
            catch (Exception ex) { return Json(new { status = false, message = ex.Message }); }
        }

        [HttpGet]
        public JsonResult GetAreasSolicitante()
        {
            try
            {
                var data = (from r in _context.AreaSolicitante
                            where r.Estado == true
                            select new
                            {
                                r.Id,
                                r.Nombre,
                            }).ToList();

                return Json(new { status = true, data });
            }
            catch (Exception ex) { return Json(new { status = false, message = ex.Message }); }
        }

        [HttpGet]
        public JsonResult GetPersonalSolicitante()
        {
            try
            {
                var data = (from r in _context.PersonalSolicitante
                            where r.Estado == true
                            select new
                            {
                                r.Id,
                                r.Nombre,
                            }).ToList();

                return Json(new { status = true, data });
            }
            catch (Exception ex) { return Json(new { status = false, message = ex.Message }); }
        }

        #endregion

        #region 3. TRANSACCIONES (POST)

        // GUARDAR REQUERIMIENTO
        [HttpPost]
        public async Task<JsonResult> Guardar(ReqServicio cabecera, string detallesJson)
        {
            using (var transaction = _context.Database.BeginTransaction())
            {
                try
                {
                    if (!EsPeriodoValido(cabecera.PeriodoContableId ?? 0))
                    {
                        throw new Exception("El periodo seleccionado ya no se encuentra abierto o es inválido. Por favor, actualice la página o seleccione otro periodo.");
                    }

                    var empresaId = EmpresaUsuarioId;
                    var usuarioId = UsuarioActualId;

                    // 1. ESTADOS
                    var estadoPendienteREQ = _context.Estados.FirstOrDefault(e => e.Nombre == "Pendiente" && e.Tabla == "REQ");
                    var estadoPendienteDREQ = _context.Estados.FirstOrDefault(e => e.Nombre == "Pendiente" && e.Tabla == "DREQ");
                    var tipoDoc = _context.TiposDocumentoInterno.FirstOrDefault(t => t.Codigo == "RS");

                    if (estadoPendienteREQ == null || estadoPendienteDREQ == null || tipoDoc == null)
                        throw new Exception("Faltan configurar estados o documentos (RS).");

                    // 2. CORRELATIVO
                    var ultimo = _context.ReqServicios
                        .Where(x => x.EmpresaId == empresaId && x.TipoDocumentoInternoId == tipoDoc.Id)
                        .OrderByDescending(x => x.Numero)
                        .Select(x => x.Numero).FirstOrDefault();

                    int nro = 1;
                    if (!string.IsNullOrEmpty(ultimo))
                    {
                        var partes = ultimo.Split('-');
                        if (partes.Length > 1 && int.TryParse(partes[1], out int correlativo)) nro = correlativo + 1;
                    }

                    // 3. CABECERA
                    cabecera.Numero = $"RS-{nro.ToString("D10")}";
                    cabecera.TipoDocumentoInternoId = tipoDoc.Id;
                    cabecera.UsuarioSolicitanteId = usuarioId;
                    cabecera.EmpresaId = empresaId;
                    cabecera.UsuarioRegistro = usuarioId;
                    cabecera.FechaRegistro = DateTime.Now;
                    cabecera.EstadoId = estadoPendienteREQ.Id;
                    //cabecera.FechaEmision = DateTime.Now;
                    if (cabecera.FechaNecesaria == DateTime.MinValue) cabecera.FechaNecesaria = DateTime.Now.AddDays(1);

                    _context.ReqServicios.Add(cabecera);
                    _context.SaveChanges();

                    // 4. DETALLES
                    if (!string.IsNullOrEmpty(detallesJson))
                    {
                        var lista = JsonConvert.DeserializeObject<List<DReqServicio>>(detallesJson);
                        int item = 1;

                        foreach (var det in lista)
                        {
                            det.Id = 0;
                            det.ReqServicioId = cabecera.Id;
                            det.EmpresaId = empresaId;
                            det.Item = item.ToString("D3");
                            det.EstadoId = estadoPendienteDREQ.Id; // Estado inicial del ítem

                            // En servicio, la descripción la escribe el usuario.
                            // Solo si viene vacía (raro), buscamos la del maestro como fallback.
                            if (string.IsNullOrEmpty(det.DescripcionServicio))
                            {
                                var prod = _context.Productos.Find(det.ProductoId);
                                if (prod != null)
                                {
                                    det.DescripcionServicio = prod.DescripcionProducto;
                                    det.UnidadMedida = prod.CodUnidadMedida;
                                }
                            }
                            else
                            {
                                // Si el usuario escribió la descripción, solo completamos la unidad si falta
                                if (string.IsNullOrEmpty(det.UnidadMedida))
                                {
                                    var prod = _context.Productos.Find(det.ProductoId);
                                    if (prod != null) det.UnidadMedida = prod.CodUnidadMedida;
                                }
                            }

                            if (det.IdOrdenProduccionErpCorpsaf == null)
                            {
                                det.DescOrdenProduccionErpCorpsaf = null;
                            }

                            _context.DReqServicios.Add(det);
                            item++;
                        }
                        _context.SaveChanges();
                    }

                    transaction.Commit();

                    var empresa = await _context.Empresas.FirstOrDefaultAsync(e => e.Id == cabecera.EmpresaId);

                    if (empresa != null && empresa.Ruc == "20612680842")
                    {
                        // 5. ENVÍO DE CORREO 
                        try
                        {
                            // 1. Consultas asíncronas para no bloquear el servidor
                            var areaSolicitante = await _context.AreaSolicitante
                                .FirstOrDefaultAsync(ar => ar.Id == cabecera.AreaSolicitanteId);

                            var personalSolicitante = await _context.PersonalSolicitante
                                .FirstOrDefaultAsync(ar => ar.Id == cabecera.PersonalSolicitanteId);

                            // 2. Manejo de nulos (Cambia ".Nombre" por el nombre real de tus columnas en BD)
                            string nombreArea = areaSolicitante != null ? areaSolicitante.Nombre : "No especificada";
                            string nombrePersonal = personalSolicitante != null ? personalSolicitante.Nombre : "No especificado";

                            // Preparamos los datos del correo
                            var destinatarios = new List<string> { "valarcon@corpsaf.com", "roliva@corpsaf.com" };
                            var asunto = $"Nuevo Requerimiento de Servicio: {cabecera.Numero}";

                            // 3. Agregamos el Área y Personal al cuerpo del correo
                            var cuerpo = $@"
                            <div style='font-family: Arial, sans-serif; color: #333;'>
                                <h2 style='color: #0056b3;'>Nuevo Requerimiento Generado</h2>
                                <p>Se ha registrado exitosamente en el sistema un nuevo requerimiento de servicio.</p>
                                <ul>
                                    <li><strong>Número:</strong> {cabecera.Numero}</li>
                                    <li><strong>Área Solicitante:</strong> {nombreArea}</li>
                                    <li><strong>Personal Solicitante:</strong> {nombrePersonal}</li>
                                    <li><strong>Fecha Necesaria:</strong> {cabecera.FechaNecesaria.GetValueOrDefault().ToString("dd/MM/yyyy")}</li>
                                </ul>
                                <p>Por favor, ingrese al ERP para revisarlo y aprobarlo haciendo clic en el siguiente botón:</p>
                                <br>
                                <a href='https://syssaf.ynnovacorp.com' 
                                   style='background-color: #0056b3; color: white; padding: 10px 20px; text-decoration: none; border-radius: 5px; font-weight: bold;'>
                                   Ir a SYSSAF
                                </a>
                                <br><br>
                                <p style='font-size: 12px; color: #777;'>
                                    Si el botón no funciona, copie y pegue la siguiente dirección en su navegador: <br>
                                    <a href='https://syssaf.ynnovacorp.com'>https://syssaf.ynnovacorp.com</a>
                                </p>
                            </div>";

                            // Usamos el servicio asíncrono
                            await _emailService.SendEmailAsync(destinatarios, asunto, cuerpo, isHtml: true);
                        }
                        catch (Exception exCorreo)
                        {
                            // Si el correo falla, NO hacemos rollback.
                            return Json(new { status = true, message = $"Requerimiento {cabecera.Numero} generado, pero el correo no se pudo enviar." });
                        }
                    }

                    // Si todo sale perfecto (Guardado + Correo enviado)
                    return Json(new { status = true, message = $"Requerimiento {cabecera.Numero} generado correctamente." });
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    return Json(new { status = false, message = ex.Message });
                }
            }
        }

        // CAMBIAR ESTADO
        [HttpPost]
        public JsonResult CambiarEstado(int id, string nombreEstado)
        {
            try
            {
                var usuarioId = UsuarioActualId;
                var estadoDb = _context.Estados.FirstOrDefault(e => e.Nombre == nombreEstado && e.Tabla == "REQ");

                if (estadoDb == null) return Json(new { status = false, message = "Estado no configurado." });

                var req = _context.ReqServicios.Find(id);
                if (req == null) return Json(new { status = false, message = "No encontrado." });

                req.EstadoId = estadoDb.Id;
                req.UsuarioAprobador = usuarioId;
                req.FechaAprobacion = DateTime.Now;
                _context.SaveChanges();

                return Json(new { status = true, message = $"Requerimiento {nombreEstado} correctamente." });
            }
            catch (Exception ex) { return Json(new { status = false, message = ex.Message }); }
        }

        #endregion
        [HttpGet]
        public async Task<IActionResult> Imprimir(int id)
        {
            try
            {
                // 1. OBTENER CABECERA
                var dataCabecera = await (from r in _context.ReqServicios
                                          join e in _context.Empresas on r.EmpresaId equals e.Id
                                          join u in _context.Usuarios on r.UsuarioSolicitanteId equals u.Id
                                          join est in _context.Estados on r.EstadoId equals est.Id

                                          // LEFT JOIN para obtener datos del Aprobador/Rechazador
                                          join ua in _context.Usuarios on r.UsuarioAprobador equals ua.Id into joinAprob
                                          from uAprob in joinAprob.DefaultIfEmpty()

                                          where r.Id == id
                                          select new
                                          {
                                              Req = r,
                                              Emp = e,
                                              Usu = u,
                                              NombreEstado = est.Nombre,

                                              // Datos de la persona que resolvió el documento
                                              AprobadorNombre = uAprob != null ? uAprob.Nombre : null,
                                              AprobadorCargo = uAprob != null ? uAprob.Cargo : null,
                                              FechaResolucion = r.FechaAprobacion // Fecha única para aprobación o rechazo
                                          }).FirstOrDefaultAsync();

                if (dataCabecera == null) return NotFound();

                // 2. OBTENER DETALLES
                var detalles = await (from d in _context.DReqServicios
                                      join cc in _context.CentroCostos on d.CentroCostoId equals cc.Id into ccJoin
                                      from cc in ccJoin.DefaultIfEmpty()
                                      where d.ReqServicioId == id
                                      select new
                                      {
                                          d.Item,
                                          d.DescripcionServicio,
                                          d.UnidadMedida,
                                          d.CantidadSolicitada,
                                          d.Lugar,
                                          CentroCosto = cc != null ? cc.Nombre : ""
                                      }).ToListAsync();

                // 3. PASAR DATOS A LA VISTA
                ViewBag.Empresa = dataCabecera.Emp;
                ViewBag.Usuario = dataCabecera.Usu;
                ViewBag.Estado = dataCabecera.NombreEstado;

                // Datos del Aprobador
                ViewBag.AprobadorNombre = dataCabecera.AprobadorNombre;
                ViewBag.AprobadorCargo = dataCabecera.AprobadorCargo;
                ViewBag.FechaResolucion = dataCabecera.FechaResolucion;

                ViewBag.Detalles = detalles;

                return View(dataCabecera.Req);
            }
            catch (Exception ex)
            {
                return Content($"Error al generar formato: {ex.Message}");
            }
        }
    }
}