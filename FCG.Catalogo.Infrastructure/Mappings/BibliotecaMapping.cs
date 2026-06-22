using FCG.Catalogo.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FCG.Catalogo.Infrastructure.Mappings
{
    public class BibliotecaMapping : IEntityTypeConfiguration<BibliotecaUsuarioEntity>
    {
        public void Configure(EntityTypeBuilder<BibliotecaUsuarioEntity> builder)
        {
            builder.ToTable("Bibliotecas");

            builder.HasKey(b => b.Id);

            builder.Property(b => b.UsuarioId)
                .IsRequired();

            builder.Property(b => b.PrecoPago)
                .IsRequired()
                .HasColumnType("decimal(18,2)");

            builder.Property(b => b.DataAquisicao)
                .IsRequired();

            builder.HasOne(b => b.Jogo)
                .WithMany(j => j.Bibliotecas)
                .HasForeignKey(b => b.JogoId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}