namespace FCG.Catalogo.Presentation.Configuration
{
    /// <summary>
    /// Nomes das roles emitidas no token JWT pelo serviço de Usuários (FCG_Usuarios).
    /// Os valores precisam ser idênticos aos semeados lá ("Usuario" e "Admin").
    /// </summary>
    public static class Papeis
    {
        public const string Usuario = "Usuario";
        public const string Admin = "Admin";
    }
}
