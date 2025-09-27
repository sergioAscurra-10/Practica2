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
using PortalInmobiliario.Helpers; 
using Microsoft.Extensions.Caching.Distributed; 
using System.Text.Json;
namespace PortalInmobiliario.Controllers
{
    [Route("Inmuebles")]
    public class InmueblesController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly IDistributedCache _cache;

        public InmueblesController(ApplicationDbContext context, UserManager<IdentityUser> userManager, IDistributedCache cache)
        {
            _context = context;
            _userManager = userManager;
            _cache = cache; 
        }

        [HttpGet]
        public async Task<IActionResult> Index(CatalogoViewModel filtroViewModel, int pagina = 1)
        {
            
            if (IsViewModelEmpty(filtroViewModel))
            {
                var filtrosGuardados = HttpContext.Session.GetObject<CatalogoViewModel>("FiltrosCatalogo");
                if (filtrosGuardados != null)
                {
                    filtroViewModel = filtrosGuardados; 
                }
            }
            else
            {
                HttpContext.Session.SetObject("FiltrosCatalogo", filtroViewModel);
            }
            if (filtroViewModel.PrecioMin.HasValue && filtroViewModel.PrecioMax.HasValue && filtroViewModel.PrecioMin > filtroViewModel.PrecioMax)
            {
                ModelState.AddModelError(nameof(filtroViewModel.PrecioMin), "El precio mínimo no puede ser mayor que el precio máximo.");
            }
            
            string cacheKey = $"Inmuebles_{filtroViewModel.CiudadFiltro}_{filtroViewModel.TipoFiltro}_{filtroViewModel.PrecioMin}_{filtroViewModel.PrecioMax}_{filtroViewModel.DormitoriosMin}_{pagina}";
            
            string? cachedData = await _cache.GetStringAsync(cacheKey);
            List<Inmueble> inmueblesPaginados;
            int totalInmuebles;

            if (!string.IsNullOrEmpty(cachedData))
            {
                inmueblesPaginados = JsonSerializer.Deserialize<List<Inmueble>>(cachedData) ?? new List<Inmueble>();
               
                totalInmuebles = await BuildFilteredQuery(filtroViewModel).CountAsync();
            }
            else
            {
                IQueryable<Inmueble> inmueblesQuery = BuildFilteredQuery(filtroViewModel);
                
                totalInmuebles = await inmueblesQuery.CountAsync();
                
                inmueblesPaginados = await inmueblesQuery
                    .OrderBy(i => i.Id)
                    .Skip((pagina - 1) * 6)
                    .Take(6)
                    .ToListAsync();
                
                var cacheOptions = new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(60) 
                };
                await _cache.SetStringAsync(cacheKey, JsonSerializer.Serialize(inmueblesPaginados), cacheOptions);
            }
            
            var viewModel = new CatalogoViewModel
            {
                Inmuebles = inmueblesPaginados,
                PaginaActual = pagina,
                TotalPaginas = (int)Math.Ceiling(totalInmuebles / (double)6),
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
            var inmueble = await _context.Inmuebles.FirstOrDefaultAsync(m => m.Id == id);
            if (inmueble == null)
            {
                return NotFound();
            }

            HttpContext.Session.SetString("LastVisitedPropertyId", inmueble.Id.ToString());
            HttpContext.Session.SetString("LastVisitedPropertyTitle", inmueble.Titulo);
           
            
            var reservaActiva = await _context.Reservas.AnyAsync(r => r.InmuebleId == id && r.FechaExpiracion > DateTime.UtcNow);
            var viewModel = new InmuebleDetailViewModel
            {
                Inmueble = inmueble,
                PuedeReservar = !reservaActiva,
                NuevaVisita = new AgendarVisitaViewModel { InmuebleId = id, FechaInicio = DateTime.Now.Date.AddHours(9), FechaFin = DateTime.Now.Date.AddHours(10) }
            };

            return View(viewModel);
        }

        [HttpPost("AgendarVisita")]
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
                    .AnyAsync(v => v.InmuebleId == model.InmuebleId && v.Estado != EstadoVisita.Cancelada && model.FechaInicio < v.FechaFin && model.FechaFin > v.FechaInicio);
                if (haySolapamiento)
                {
                    ModelState.AddModelError("", "El horario seleccionado ya no está disponible. Por favor, elija otro.");
                }

                if (ModelState.IsValid)
                {
                    var visita = new Visita
                    {
                        InmuebleId = model.InmuebleId,
                        UsuarioId = User.FindFirstValue(ClaimTypes.NameIdentifier)!,
                        FechaInicio = model.FechaInicio,
                        FechaFin = model.FechaFin,
                        Notas = model.Notas,
                        Estado = EstadoVisita.Solicitada
                    };
                    _context.Add(visita);
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "¡Visita solicitada con éxito!";
                    return RedirectToAction("Details", new { id = model.InmuebleId });
                }
            }
            TempData["ErrorMessage"] = "No se pudo agendar la visita. Corrija los errores.";
            var inmueble = await _context.Inmuebles.FindAsync(model.InmuebleId);
            if (inmueble == null) { return NotFound(); }
            var reservaActiva = await _context.Reservas.AnyAsync(r => r.InmuebleId == model.InmuebleId && r.FechaExpiracion > DateTime.UtcNow);
            var viewModel = new InmuebleDetailViewModel { Inmueble = inmueble, PuedeReservar = !reservaActiva, NuevaVisita = model };
            return View("Details", viewModel);
        }

        [HttpPost("Reservar")]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> Reservar(int inmuebleId)
        {
            var reservaActiva = await _context.Reservas.AnyAsync(r => r.InmuebleId == inmuebleId && r.FechaExpiracion > DateTime.UtcNow);
            if (reservaActiva)
            {
                TempData["ErrorMessage"] = "Este inmueble ya ha sido reservado.";
                return RedirectToAction("Details", new { id = inmuebleId });
            }
            var reserva = new Reserva
            {
                InmuebleId = inmuebleId,
                UsuarioId = User.FindFirstValue(ClaimTypes.NameIdentifier)!,
                FechaCreacion = DateTime.UtcNow,
                FechaExpiracion = DateTime.UtcNow.AddHours(48)
            };
            _context.Add(reserva);
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "¡Inmueble reservado por 48 horas!";
            return RedirectToAction("Details", new { id = inmuebleId });
        }
        private IQueryable<Inmueble> BuildFilteredQuery(CatalogoViewModel filtroViewModel)
        {
            IQueryable<Inmueble> query = _context.Inmuebles.Where(i => i.Activo);

            if (!string.IsNullOrEmpty(filtroViewModel.CiudadFiltro))
                query = query.Where(i => i.Ciudad == filtroViewModel.CiudadFiltro);
            if (filtroViewModel.TipoFiltro.HasValue)
                query = query.Where(i => i.Tipo == filtroViewModel.TipoFiltro);
            if (filtroViewModel.PrecioMin.HasValue)
                query = query.Where(i => i.Precio >= filtroViewModel.PrecioMin);
            if (filtroViewModel.PrecioMax.HasValue)
                query = query.Where(i => i.Precio <= filtroViewModel.PrecioMax);
            if (filtroViewModel.DormitoriosMin.HasValue)
                query = query.Where(i => i.Dormitorios >= filtroViewModel.DormitoriosMin);
            
            return query;
        }
        
        private bool IsViewModelEmpty(CatalogoViewModel vm)
        {
            return string.IsNullOrEmpty(vm.CiudadFiltro) &&
                   !vm.TipoFiltro.HasValue &&
                   !vm.PrecioMin.HasValue &&
                   !vm.PrecioMax.HasValue &&
                   !vm.DormitoriosMin.HasValue;
        }
    }
}