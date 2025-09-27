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
    // 1. Definimos una ruta base para todo el controlador.
    // Ahora, todas las acciones partirán de la URL /Inmuebles
    [Route("Inmuebles")]
    public class InmueblesController : Controller
    {
        private readonly ApplicationDbContext _context;

        public InmueblesController(ApplicationDbContext context)
        {
            _context = context;
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

        // 3. Definimos una ruta EXPLÍCITA para la acción Details.
        // Esto responde a un GET en "/Inmuebles/Details/5" (por ejemplo).
        // La restricción {id:int} asegura que solo números coincidan.
        [HttpGet("Details/{id:int}")]
        public async Task<IActionResult> Details(int id) // Cambiado id? por id, ya que la ruta lo requiere.
        {
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