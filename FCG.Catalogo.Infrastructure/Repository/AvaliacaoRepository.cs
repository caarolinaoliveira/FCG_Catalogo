using FCG.Catalogo.Domain.Entities;
using FCG.Catalogo.Domain.Interfaces;
using MongoDB.Driver;

namespace FCG.Catalogo.Infrastructure.Repository
{
    public class AvaliacaoRepository : IAvaliacaoRepository
    {
        private readonly IMongoCollection<AvaliacaoEntity> _collection;

        public AvaliacaoRepository(IMongoDatabase database)
        {
            _collection = database.GetCollection<AvaliacaoEntity>("avaliacoes");
        }

        public async Task AdicionarAsync(AvaliacaoEntity avaliacao)
        {
            await _collection.InsertOneAsync(avaliacao);
        }

        public async Task<List<AvaliacaoEntity>> ObterPorJogoIdAsync(Guid jogoId)
        {
            return await _collection
                .Find(a => a.JogoId == jogoId)
                .SortByDescending(a => a.CriadoEm)
                .ToListAsync();
        }
    }
}
