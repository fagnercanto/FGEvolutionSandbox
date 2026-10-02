using FG.Evolution.Simulation.Engine.Core;
using FG.Evolution.Simulation.Engine.Graphics;
using Raylib_cs;

namespace FG.Evolution.Simulation.Simulation.Entities
{
    // Alimento para a Presa e obstáculo intransponível para o Predador.
    // Nasce periodicamente (a cada TicksParaNovaPlanta) e some do mapa ao ser comida.
    public class Planta : ElementoCenario
    {
        // Enquanto Viva, a Planta existe no mapa (como alimento/obstáculo).
        // Ao ser comida pela Presa, vira false e a Arena a remove da lista de Alimentos.
        public bool Viva { get; set; } = true;

        public Planta(float x, float y, float tamanho)
        {
            X = x;
            Y = y;
            Tamanho = tamanho;
            Angulo = 0f;

            CorBase = Color.Green;
            CorBorda = Color.Black;
            Formato = FormatoVisual.Circulo;
            IdSprite = "planta"; // Assets/Sprites/planta.png
        }
    }
}
