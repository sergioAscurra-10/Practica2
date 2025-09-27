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

namespace PortalInmobiliario.Controllers
{
    [Route("[controller]")]
    public class InmueblesController : Controller
    {
        private readonly ApplicationDbContext _context;

        public InmueblesController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index(CatalogoViewModel filtroViewModel, int pagina = 1)
        {
            // --- Validación Server-Side ---
            if (filtroViewModel.PrecioMin.HasValue && filtroViewModel.PrecioMax.HasValue && filtroViewModel.PrecioMin > filtroViewModel.PrecioMax)
            {
                ModelState.AddModelError(nameof(filtroViewModel.PrecioMin), "El precio mínimo no puede ser mayor que el precio máximo.");
            }

            // --- Query Base ---
            IQueryable<Inmueble> inmueblesQuery = _context.Inmuebles.Where(i => i.Activo);

            // --- Aplicar Filtros ---
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

            // --- Paginación ---
            const int tamanoPagina = 6;
            var totalInmuebles = await inmueblesQuery.CountAsync();
            var inmueblesPaginados = await inmueblesQuery
                .Skip((pagina - 1) * tamanoPagina)
                .Take(tamanoPagina)
                .ToListAsync();

            // --- Preparar ViewModel para la Vista ---
            var viewModel = new CatalogoViewModel
            {
                Inmuebles = inmueblesPaginados,
                PaginaActual = pagina,
                TotalPaginas = (int)Math.Ceiling(totalInmuebles / (double)tamanoPagina),
                // Asignamos los valores de los filtros para que se mantengan en el formulario
                CiudadFiltro = filtroViewModel.CiudadFiltro,
                TipoFiltro = filtroViewModel.TipoFiltro,
                PrecioMin = filtroViewModel.PrecioMin,
                PrecioMax = filtroViewModel.PrecioMax,
                DormitoriosMin = filtroViewModel.DormitoriosMin,
                // Llenamos los SelectLists para los dropdowns de filtros
                Ciudades = new SelectList(await _context.Inmuebles.Select(i => i.Ciudad).Distinct().ToListAsync()),
                Tipos = new SelectList(Enum.GetValues(typeof(TipoInmueble)))
            };

            return View(viewModel);
        }

        // GET: Inmuebles/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var inmueble = await _context.Inmuebles
                .FirstOrDefaultAsync(m => m.Id == id);
            
            if (inmueble == null)
            {
                return NotFound();
            }

            return View(inmueble);
        }
    }
}