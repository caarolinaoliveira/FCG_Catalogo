using FCG.Catalogo.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FCG.Catalogo.Infrastructure.Mappings
{
    public class JogoMapping : IEntityTypeConfiguration<JogoEntity>
    {
        public void Configure(EntityTypeBuilder<JogoEntity> builder)
        {
            builder.ToTable("Jogos");

            builder.HasKey(j => j.Id);

            builder.Property(j => j.Titulo)
                .IsRequired()
                .HasMaxLength(200);

            builder.Property(j => j.Descricao)
                .HasMaxLength(500);

            builder.Property(j => j.Preco)
                .HasColumnType("decimal(18,2)");

            builder.Property(j => j.PrecoDesconto)
                .HasColumnType("decimal(18,2)");

            builder.Property(j => j.StatusJogo)
                .IsRequired();

            builder.HasMany(j => j.Bibliotecas)
                .WithOne(b => b.Jogo)
                .HasForeignKey(b => b.JogoId);
        }
    }
}