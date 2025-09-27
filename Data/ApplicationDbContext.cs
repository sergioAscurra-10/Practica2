using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using PortalInmobiliario.Models; 

namespace PortalInmobiliario.Data;

public class ApplicationDbContext : IdentityDbContext
{
    public DbSet<Inmueble> Inmuebles { get; set; }
    public DbSet<Visita> Visitas { get; set; }
    public DbSet<Reserva> Reservas { get; set; }

    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {

    }
    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<Inmueble>(entity =>
        {
            entity.HasIndex(e => e.Codigo).IsUnique();

            entity.ToTable(tb => tb.HasCheckConstraint("CK_Inmueble_Precio", "Precio > 0"));
            entity.ToTable(tb => tb.HasCheckConstraint("CK_Inmueble_MetrosCuadrados", "MetrosCuadrados > 0"));
        });

        builder.Entity<Visita>(entity =>
        {
            entity.ToTable(tb => tb.HasCheckConstraint("CK_Visita_Fechas", "FechaFin > FechaInicio"));
        });


        builder.Entity<Inmueble>().HasData(
            new Inmueble
            {
                Id = 1,
                Codigo = "DEP-001",
                Titulo = "Moderno Departamento en el Centro",
                Tipo = TipoInmueble.Departamento,
                Ciudad = "Santiago",
                Direccion = "Av. Providencia 123",
                Dormitorios = 2,
                Banos = 2,
                MetrosCuadrados = 75.5,
                Precio = 450000,
                Activo = true,
                Imagen = "/images/depto1.jpg"
            },
            new Inmueble
            {
                Id = 2,
                Codigo = "CAS-001",
                Titulo = "Amplia Casa con Jardín",
                Tipo = TipoInmueble.Casa,
                Ciudad = "Valparaíso",
                Direccion = "Cerro Alegre 456",
                Dormitorios = 4,
                Banos = 3,
                MetrosCuadrados = 180,
                Precio = 780000,
                Activo = true,
                Imagen = "/images/casa1.jpg"
            },
            new Inmueble
            {
                Id = 3,
                Codigo = "OFI-001",
                Titulo = "Oficina Céntrica con Vista",
                Tipo = TipoInmueble.Oficina,
                Ciudad = "Santiago",
                Direccion = "Apoquindo 789",
                Dormitorios = 0,
                Banos = 2,
                MetrosCuadrados = 120,
                Precio = 950000,
                Activo = true,
                Imagen = "/images/oficina1.jpg"
            },
            new Inmueble
            {
                Id = 4,
                Codigo = "DEP-002",
                Titulo = "Acogedor Departamento en Barrio Lastarria",
                Tipo = TipoInmueble.Departamento,
                Ciudad = "Santiago",
                Direccion = "Merced 567",
                Dormitorios = 1,
                Banos = 1,
                MetrosCuadrados = 50,
                Precio = 320000,
                Activo = false, 
                Imagen = "/images/depto2.jpg"
            }
        );
    }


}
