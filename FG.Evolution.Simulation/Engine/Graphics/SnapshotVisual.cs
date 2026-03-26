using Raylib_cs;

namespace FG.Evolution.Simulation.Engine.Graphics
{
    public struct SnapshotVisual
    {
        public float X;
        public float Y;
        public float Tamanho;
        public float Angulo; // Necessário para calcular a direção do "nariz"
        public Color CorBase;
        public Color CorBorda;
        public FormatoVisual Formato;
    }
}
