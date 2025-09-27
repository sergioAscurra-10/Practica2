using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PortalInmobiliario.Data;
using PortalInmobiliario.Models;
using Microsoft.Extensions.Caching.Distributed;

namespace PortalInmobiliario.Areas.Broker.Controllers;

[Area("Broker")]
[Authorize(Roles = "Broker")] // <-- La magia está aquí. Solo usuarios con rol "Broker" pueden acceder.
public class BrokerController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly IDistributedCache _cache;

    public BrokerController(ApplicationDbContext context, IDistributedCache cache)
    {
        _context = context;
        _cache = cache;
    }

    // GET: /Broker/
    public IActionResult Index()
    {
        // Panel principal o dashboard
        return View();
    }

    // GET: /Broker/Inmuebles
    public async Task<IActionResult> Inmuebles()
    {
        var inmuebles = await _context.Inmuebles.ToListAsync();
        return View(inmuebles);
    }
    
    
    public IActionResult Create()
    {
        return View();
    }
    
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Inmueble inmueble)
    {
        if (ModelState.IsValid)
        {
            _context.Add(inmueble);
            await _context.SaveChangesAsync();
            await _cache.RemoveAsync("Inmuebles_");
            TempData["SuccessMessage"] = "Inmueble creado con éxito.";
            return RedirectToAction(nameof(Inmuebles));
        }
        return View(inmueble);
    }

    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null) return NotFound();
        var inmueble = await _context.Inmuebles.FindAsync(id);
        if (inmueble == null) return NotFound();
        return View(inmueble);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, Inmueble inmueble)
    {
        if (id != inmueble.Id) return NotFound();

        if (ModelState.IsValid)
        {
            _context.Update(inmueble);
            await _context.SaveChangesAsync();
            await _cache.RemoveAsync("Inmuebles_"); 
            TempData["SuccessMessage"] = "Inmueble actualizado con éxito.";
            return RedirectToAction(nameof(Inmuebles));
        }
        return View(inmueble);
    }

    public async Task<IActionResult> Agenda()
    {
        var hoy = DateTime.Today;
        var visitas = await _context.Visitas
            .Include(v => v.Inmueble)
            .Include(v => v.Usuario)
            .Where(v => v.FechaInicio.Date == hoy && v.Estado != EstadoVisita.Cancelada)
            .OrderBy(v => v.FechaInicio)
            .ToListAsync();
        return View(visitas);
    }

    [HttpPost]
    public async Task<IActionResult> ConfirmarVisita(int id)
    {
        var visita = await _context.Visitas.FindAsync(id);
        if (visita != null)
        {
            visita.Estado = EstadoVisita.Confirmada;
            await _context.SaveChangesAsync();
        }
        return RedirectToAction(nameof(Agenda));
    }

    [HttpPost]
    public async Task<IActionResult> CancelarVisita(int id)
    {
        var visita = await _context.Visitas.FindAsync(id);
        if (visita != null)
        {
            visita.Estado = EstadoVisita.Cancelada;
            await _context.SaveChangesAsync();
        }
        return RedirectToAction(nameof(Agenda));
    }
    
    public async Task<IActionResult> Reservas()
    {
        var reservasActivas = await _context.Reservas
            .Include(r => r.Inmueble)
            .Include(r => r.Usuario)
            .Where(r => r.FechaExpiracion > DateTime.UtcNow)
            .ToListAsync();
        return View(reservasActivas);
    }
    
    [HttpPost]
    public async Task<IActionResult> LiberarReserva(int id)
    {
        var reserva = await _context.Reservas.FindAsync(id);
        if (reserva != null)
        {
            reserva.FechaExpiracion = DateTime.UtcNow.AddMinutes(-1);
            await _context.SaveChangesAsync();
        }
        return RedirectToAction(nameof(Reservas));
    }
}