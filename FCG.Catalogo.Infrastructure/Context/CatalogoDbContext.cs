using Microsoft.EntityFrameworkCore;
using FCG.Catalogo.Domain.Entities;

namespace FCG.Catalogo.Infrastructure.Context
{
    public class CatalogoDbContext : DbContext
    {
        public CatalogoDbContext(DbContextOptions<CatalogoDbContext> options)
            : base(options)
        {
            ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;
            ChangeTracker.AutoDetectChangesEnabled = false;
        }

        public DbSet<JogoEntity> Jogos { get; set; }
        public DbSet<BibliotecaUsuarioEntity> Bibliotecas { get; set; }
        public DbSet<PedidoEntity> Pedidos { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.ApplyConfigurationsFromAssembly(
                typeof(CatalogoDbContext).Assembly
            );
        }

        public override Task<int> SaveChangesAsync(
            CancellationToken cancellationToken = default)
        {
            foreach (var entry in ChangeTracker.Entries()
                .Where(e => e.Entity.GetType()
                    .GetProperty("CriadoEm") != null))
            {
                if (entry.State == EntityState.Added)
                    entry.Property("CriadoEm").CurrentValue = DateTime.UtcNow;

                if (entry.State == EntityState.Modified)
                    entry.Property("CriadoEm").IsModified = false;
            }

            return base.SaveChangesAsync(cancellationToken);
        }
    }
}