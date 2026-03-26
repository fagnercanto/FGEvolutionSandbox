using FG.Evolution.Simulation.Config;
using FG.Evolution.Simulation.Engine.Graphics;
using FG.Evolution.Simulation.Simulation.World;

// Adicione aqui os usings corretos para a sua Arena e Configuração de acordo com as pastas
// Exemplo: using FG.Evolution.Simulation.Simulation.World;
// Exemplo: using FG.Evolution.Simulation.Config;
using Raylib_cs;

namespace FG.Evolution.Simulation
{
    class Program
    {
        static void Main(string[] args)
        {
            // 1. Inicialização do Sistema Operacional (Janela)
            // Se você usa uma classe Config estática, ajuste os nomes aqui se necessário.
            Raylib.InitWindow(800, 600, "FG Sandbox Engine - V1");
            Raylib.SetTargetFPS(60);

            // 2. Instanciação das Configurações e Motores
            Configuracao maletaConfig = new Configuracao(); // Cria as regras do jogo

            // Injeta a maleta na Arena (Resolve o erro do sublinhado vermelho!)
            Arena arena = new Arena(maletaConfig);
            MotorGrafico motor = new MotorGrafico();

            // 3. O Game Loop (Onde o tempo acontece)
            while (!Raylib.WindowShouldClose())
            {
                // FASE A: O Mundo Pensa e se Move (Zero Gráficos)
                arena.AtualizarMundo();

                // FASE B: A Fotografia do Estado Atual (O Transporte)
                var cenaVisual = arena.ObterCenaVisual();

                // FASE C: O Pintor Cego Trabalha (Zero Lógica)
                motor.RenderizarCena(cenaVisual, arena.GeracaoAtual);
            }

            // 4. Desligamento Limpo
            Raylib.CloseWindow();
        }
    }
}