namespace FCG.Catalogo.Domain.Exceptions
{
    public class BuscaIndisponivelException : Exception
    {
        public BuscaIndisponivelException(string message, Exception? innerException = null)
            : base(message, innerException) { }
    }
}
