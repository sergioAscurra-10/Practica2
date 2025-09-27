using PortalInmobiliario.Models;

namespace PortalInmobiliario.ViewModels;

public class InmuebleDetailViewModel
{
    public Inmueble Inmueble { get; set; } = new();
    public AgendarVisitaViewModel NuevaVisita { get; set; } = new();
    public bool PuedeReservar { get; set; }
}