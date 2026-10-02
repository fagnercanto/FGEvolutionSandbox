namespace FG.Evolution.Simulation.Engine.Graphics
{
    // DTO simples usado para transportar, de forma thread-safe, os novos parâmetros
    // solicitados pelo usuário no Painel de Controle até o loop principal (Raylib).
    public class ParametrosArena
    {
        public int LarguraTela { get; set; }
        public int AlturaTela { get; set; }
        public int PopulacaoTijolos { get; set; }
        public int PopulacaoPlantas { get; set; }
        public int PopulacaoPredadores { get; set; }
        public int PopulacaoPresas { get; set; }
        public int FPS { get; set; }
    }
}
