using FCG.Catalogo.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FCG.Catalogo.Infrastructure.Mappings
{
    public class PedidoMapping : IEntityTypeConfiguration<PedidoEntity>
    {
        public void Configure(EntityTypeBuilder<PedidoEntity> builder)
        {
            builder.ToTable("Pedidos");

            builder.HasKey(p => p.Id);

            builder.Property(p => p.UsuarioId)
                .IsRequired();

            builder.Property(p => p.JogoId)
                .IsRequired();

            builder.Property(p => p.Preco)
                .IsRequired()
                .HasColumnType("decimal(18,2)");

            builder.Property(p => p.Status)
                .IsRequired();

            builder.Property(p => p.CriadoEm)
                .IsRequired();

            builder.Property(p => p.AtualizadoEm);

            builder.HasOne(p => p.Jogo)
                .WithMany()
                .HasForeignKey(p => p.JogoId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}