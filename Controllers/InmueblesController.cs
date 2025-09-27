using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using PortalInmobiliario.Data;
using PortalInmobiliario.Models;
using PortalInmobiliario.ViewModels;
using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authorization;

namespace PortalInmobiliario.Controllers
{
    // 1. Definimos una ruta base para todo el controlador.
    // Ahora, todas las acciones partirán de la URL /Inmuebles
    [Route("Inmuebles")]
    public class InmueblesController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        public InmueblesController(ApplicationDbContext context, UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // 2. Definimos la ruta para la acción Index.
        // [HttpGet] sin parámetros significa que responde a un GET en la ruta base "/Inmuebles".
        [HttpGet]
        public async Task<IActionResult> Index(CatalogoViewModel filtroViewModel, int pagina = 1)
        {
            if (filtroViewModel.PrecioMin.HasValue && filtroViewModel.PrecioMax.HasValue && filtroViewModel.PrecioMin > filtroViewModel.PrecioMax)
            {
                ModelState.AddModelError(nameof(filtroViewModel.PrecioMin), "El precio mínimo no puede ser mayor que el precio máximo.");
            }

            IQueryable<Inmueble> inmueblesQuery = _context.Inmuebles.Where(i => i.Activo);

            if (!string.IsNullOrEmpty(filtroViewModel.CiudadFiltro))
            {
                inmueblesQuery = inmueblesQuery.Where(i => i.Ciudad == filtroViewModel.CiudadFiltro);
            }

            if (filtroViewModel.TipoFiltro.HasValue)
            {
                inmueblesQuery = inmueblesQuery.Where(i => i.Tipo == filtroViewModel.TipoFiltro);
            }

            if (filtroViewModel.PrecioMin.HasValue)
            {
                inmueblesQuery = inmueblesQuery.Where(i => i.Precio >= filtroViewModel.PrecioMin);
            }

            if (filtroViewModel.PrecioMax.HasValue)
            {
                inmueblesQuery = inmueblesQuery.Where(i => i.Precio <= filtroViewModel.PrecioMax);
            }

            if (filtroViewModel.DormitoriosMin.HasValue)
            {
                inmueblesQuery = inmueblesQuery.Where(i => i.Dormitorios >= filtroViewModel.DormitoriosMin);
            }

            const int tamanoPagina = 6;
            var totalInmuebles = await inmueblesQuery.CountAsync();
            var inmueblesPaginados = await inmueblesQuery
                .Skip((pagina - 1) * tamanoPagina)
                .Take(tamanoPagina)
                .ToListAsync();

            var viewModel = new CatalogoViewModel
            {
                Inmuebles = inmueblesPaginados,
                PaginaActual = pagina,
                TotalPaginas = (int)Math.Ceiling(totalInmuebles / (double)tamanoPagina),
                CiudadFiltro = filtroViewModel.CiudadFiltro,
                TipoFiltro = filtroViewModel.TipoFiltro,
                PrecioMin = filtroViewModel.PrecioMin,
                PrecioMax = filtroViewModel.PrecioMax,
                DormitoriosMin = filtroViewModel.DormitoriosMin,
                Ciudades = new SelectList(await _context.Inmuebles.Select(i => i.Ciudad).Distinct().ToListAsync()),
                Tipos = new SelectList(Enum.GetValues(typeof(TipoInmueble)))
            };

            return View(viewModel);
        }

        [HttpGet("Details/{id:int}")]
        public async Task<IActionResult> Details(int id)
        {
            var inmueble = await _context.Inmuebles
                .FirstOrDefaultAsync(m => m.Id == id);

            if (inmueble == null)
            {
                return NotFound();
            }

            var reservaActiva = await _context.Reservas
                .AnyAsync(r => r.InmuebleId == id && r.FechaExpiracion > DateTime.UtcNow);

            var viewModel = new InmuebleDetailViewModel
            {
                Inmueble = inmueble,
                PuedeReservar = !reservaActiva,
                NuevaVisita = new AgendarVisitaViewModel
                {
                    InmuebleId = id,
                    FechaInicio = DateTime.Now.Date.AddHours(9),
                    FechaFin = DateTime.Now.Date.AddHours(10)
                }
            };

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> AgendarVisita(AgendarVisitaViewModel model)
        {
            if (ModelState.IsValid)
            {
                if (model.FechaInicio.Hour < 8 || model.FechaInicio.Hour >= 19 || model.FechaFin.Hour < 8 || model.FechaFin.Hour > 19)
                {
                    ModelState.AddModelError("NuevaVisita.FechaInicio", "Las visitas solo pueden agendarse entre las 08:00 y las 19:00.");
                }

                if (model.FechaInicio >= model.FechaFin)
                {
                    ModelState.AddModelError("NuevaVisita.FechaFin", "La fecha de fin debe ser posterior a la fecha de inicio.");
                }

                var haySolapamiento = await _context.Visitas
                    .AnyAsync(v => v.InmuebleId == model.InmuebleId &&
                                v.Estado != EstadoVisita.Cancelada &&
                                model.FechaInicio < v.FechaFin &&
                                model.FechaFin > v.FechaInicio);

                if (haySolapamiento)
                {
                    ModelState.AddModelError("", "El horario seleccionado ya no está disponible. Por favor, elija otro.");
                }

                if (ModelState.IsValid)
                {
                    var usuarioId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                    var visita = new Visita
                    {
                        InmuebleId = model.InmuebleId,
                        UsuarioId = usuarioId,
                        FechaInicio = model.FechaInicio,
                        FechaFin = model.FechaFin,
                        Notas = model.Notas,
                        Estado = EstadoVisita.Solicitada
                    };

                    _context.Add(visita);
                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] = "¡Visita solicitada con éxito! Nos pondremos en contacto para confirmar.";
                    return RedirectToAction("Details", new { id = model.InmuebleId });
                }
            }

            TempData["ErrorMessage"] = "No se pudo agendar la visita. Por favor, corrija los errores.";
            var inmueble = await _context.Inmuebles.FindAsync(model.InmuebleId);
            var reservaActiva = await _context.Reservas.AnyAsync(r => r.InmuebleId == model.InmuebleId && r.FechaExpiracion > DateTime.UtcNow);

            var viewModel = new InmuebleDetailViewModel
            {
                Inmueble = inmueble,
                PuedeReservar = !reservaActiva,
                NuevaVisita = model
            };

            return View("Details", viewModel);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> Reservar(int inmuebleId)
        {
            var reservaActiva = await _context.Reservas
                .AnyAsync(r => r.InmuebleId == inmuebleId && r.FechaExpiracion > DateTime.UtcNow);

            if (reservaActiva)
            {
                TempData["ErrorMessage"] = "Este inmueble ya ha sido reservado por otro usuario.";
                return RedirectToAction("Details", new { id = inmuebleId });
            }

            var usuarioId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var reserva = new Reserva
            {
                InmuebleId = inmuebleId,
                UsuarioId = usuarioId,
                FechaCreacion = DateTime.UtcNow,
                FechaExpiracion = DateTime.UtcNow.AddHours(48)
            };

            _context.Add(reserva);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "¡Inmueble reservado por 48 horas! Por favor, contacta al broker para finalizar el proceso.";
            return RedirectToAction("Details", new { id = inmuebleId });
        }
    }
}