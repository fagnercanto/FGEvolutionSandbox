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

        // Nível de HP (1 a 10) para exibição ao vivo sobre Presas/Predadores.
        // Null para elementos sem HP (Tijolo, Planta).
        public int? NivelHp;

        // Identificador usado para localizar um sprite em Assets/Sprites/{IdSprite}.png.
        // Se nulo/vazio ou o arquivo não existir, o MotorGrafico usa o desenho vetorial (fallback).
        public string IdSprite;

        // --- Campos de animação (usados apenas quando IdSprite tem uma entrada em SpriteSheets.Animacoes) ---

        // Direção cardinal atual (para escolher os frames corretos na spritesheet).
        public DirecaoSprite Direcao;

        // Índice achatado (flat, 0-based) do frame atual a ser recortado da spritesheet.
        // -1 quando não há animação aplicável (o MotorGrafico cai no fallback de imagem única/vetor).
        public int FrameSprite;
    }
}
