using FCG.Catalogo.Domain.Entities;

namespace FCG.Catalogo.Application.Interfaces
{
    // Nunca lança exceção: se o Elasticsearch estiver fora do ar, só registra no log, para o CRUD não falhar.
    public interface IJogoIndexador
    {
        Task IndexarAsync(JogoEntity jogo);
        Task RemoverAsync(Guid id);
    }
}
