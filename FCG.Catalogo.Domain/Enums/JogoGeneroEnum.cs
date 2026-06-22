using System.ComponentModel;

namespace FCG.Catalogo.Domain.Enums
{
    public enum JogoGeneroEnum

    {
        [Description("0 - Ação")]
        Acao = 0,
        
        [Description("1 - Aventura")]
        Aventura = 1,

        [Description("2 - RPG")]
        RPG = 2,

        [Description("3 - Esporte")]
        Esporte = 3,

        [Description("4 - Simulação")]
        Simulacao = 4,

        [Description("5 - Estratégia")]
        Estrategia = 5,

        [Description("6 - Luta")]
        Luta = 6,

        [Description("7 - Corrida")]
        Corrida = 7,

        [Description("8 - Terror")]
        Terror = 8,
        
        [Description("9 - Puzzle")]
        Puzzle = 9
    }
}