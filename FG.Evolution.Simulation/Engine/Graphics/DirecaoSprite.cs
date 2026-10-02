namespace FG.Evolution.Simulation.Engine.Graphics
{
    // Direção cardinal usada para escolher a linha correta em uma spritesheet
    // com 4 direções de caminhada (ex: roach.png). Derivada do ângulo de movimento
    // do agente, "quantizado" em 4 quadrantes de 90°.
    public enum DirecaoSprite
    {
        Direita,
        Baixo,
        Esquerda,
        Cima
    }
}
