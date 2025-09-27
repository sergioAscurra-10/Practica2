using Microsoft.AspNetCore.Mvc.Rendering;
using PortalInmobiliario.Models;
using System.ComponentModel.DataAnnotations;

namespace PortalInmobiliario.ViewModels;

public class CatalogoViewModel
{
    public List<Inmueble> Inmuebles { get; set; } = new();
    public int PaginaActual { get; set; }
    public int TotalPaginas { get; set; }
    public bool TienePaginaAnterior => PaginaActual > 1;
    public bool TienePaginaSiguiente => PaginaActual < TotalPaginas;

    // --- Filtros ---
    [Display(Name = "Ciudad")]
    public string? CiudadFiltro { get; set; }

    [Display(Name = "Tipo de Inmueble")]
    public TipoInmueble? TipoFiltro { get; set; }

    [Display(Name = "Precio Mínimo")]
    [Range(0, double.MaxValue, ErrorMessage = "El precio mínimo no puede ser negativo.")]
    public decimal? PrecioMin { get; set; }

    [Display(Name = "Precio Máximo")]
    [Range(0, double.MaxValue, ErrorMessage = "El precio máximo no puede ser negativo.")]
    public decimal? PrecioMax { get; set; }

    [Display(Name = "Mín. Dormitorios")]
    [Range(0, int.MaxValue, ErrorMessage = "El número de dormitorios no puede ser negativo.")]
    public int? DormitoriosMin { get; set; }

    public SelectList? Ciudades { get; set; }
    public SelectList? Tipos { get; set; }
}