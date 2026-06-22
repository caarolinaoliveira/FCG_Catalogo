namespace FCG.Catalogo.Application.Events
{
    public class OrderPlacedEvent
    {
        public Guid UserId { get; set; }
        public Guid GameId { get; set; }
        public decimal Price { get; set; }
        public DateTime PlacedAt { get; set; }
    }
}