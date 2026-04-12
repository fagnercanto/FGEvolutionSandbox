using FG.Evolution.Simulation.Engine.Core;
using FG.Evolution.Simulation.Engine.Graphics;
using Raylib_cs;

namespace FG.Evolution.Simulation.Simulation.Entities
{
    // A Comida agora herda do chassi físico da Engine
    public class Comida : ElementoCenario
    {
        public Comida(float x, float y, float tamanho)
        {
            this.X = x;
            this.Y = y;
            this.Tamanho = tamanho;

            // Configuração Visual Padrão
            this.CorBase = Color.Blue;
            this.CorBase = Color.Red;
            this.Formato = FormatoVisual.Circulo; // O nosso MotorGráfico já sabe como pintar isso!
        }
    }
}