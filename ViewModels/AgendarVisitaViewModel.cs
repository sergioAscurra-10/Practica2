using System.ComponentModel.DataAnnotations;

namespace PortalInmobiliario.ViewModels;

public class AgendarVisitaViewModel
{
    [Required]
    public int InmuebleId { get; set; }

    [Required(ErrorMessage = "La fecha de inicio es obligatoria.")]
    [Display(Name = "Inicio de la visita")]
    public DateTime FechaInicio { get; set; }

    [Required(ErrorMessage = "La fecha de fin es obligatoria.")]
    [Display(Name = "Fin de la visita")]
    public DateTime FechaFin { get; set; }

    [StringLength(500)]
    public string? Notas { get; set; }
}