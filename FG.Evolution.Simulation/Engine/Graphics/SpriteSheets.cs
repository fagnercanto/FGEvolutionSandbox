using System.Collections.Generic;

namespace FG.Evolution.Simulation.Engine.Graphics
{
    // Descreve o layout de uma spritesheet usando ÍNDICES "achatados" (0-based, contando
    // da esquerda para a direita e de cima para baixo, célula a célula). Isso funciona mesmo
    // quando o grid não é regular (ex: uma direção com 3 frames e outra com 4, ou mortes
    // "soltas" espalhadas nas últimas linhas), bastando listar os índices certos.
    public class InfoSpriteSheet
    {
        public int LarguraFrame { get; set; }
        public int AlturaFrame { get; set; }

        // Quantidade de colunas do grid (necessário para converter índice achatado -> linha/coluna).
        public int Colunas { get; set; }

        // Sequência de índices achatados que compõem o ciclo de caminhada de cada direção.
        public Dictionary<DirecaoSprite, int[]> FramesCaminhadaPorDirecao { get; set; }

        // Índice achatado do frame único de "morte" para cada direção.
        public Dictionary<DirecaoSprite, int> FrameMortePorDirecao { get; set; }

        // Quantos ticks (frames de simulação) cada frame de animação fica visível
        // antes de avançar para o próximo. Menor = animação mais rápida.
        public int TicksPorFrame { get; set; } = 8;

        // Quantos ticks o frame de morte (único) fica visível antes do agente sumir de vez.
        public int TicksExibicaoMorte { get; set; } = 30;
    }

    // Configuração central de todas as spritesheets animadas conhecidas pelo motor gráfico.
    // Um IdSprite (ver ElementoCenario.IdSprite) que NÃO estiver neste dicionário é tratado
    // como imagem única estática (comportamento padrão já existente).
    public static class SpriteSheets
    {
        public static readonly Dictionary<string, InfoSpriteSheet> Animacoes = new()
        {
            // roach.png: grid de 4 colunas x 5 linhas (32x32 por frame), preenchido em
            // sequência (índice = linha * 4 + coluna), confirmado pelo layout:
            //   0  1  2  3   -> direita, direita, direita, direita
            //   4  5  6  7   -> cima, cima, cima, baixo
            //   8  9  10 11  -> baixo, baixo, baixo, esquerda
            //   12 13 14 15  -> esquerda, esquerda, esquerda, baixo-morta
            //   16 17 18 19  -> direita-morta, cima-morta, esquerda-morta, (vazio)
            ["presa"] = new InfoSpriteSheet
            {
                LarguraFrame = 32,
                AlturaFrame = 32,
                Colunas = 4,
                TicksPorFrame = 8,
                TicksExibicaoMorte = 30,
                FramesCaminhadaPorDirecao = new Dictionary<DirecaoSprite, int[]>
                {
                    [DirecaoSprite.Direita] = new[] { 0, 1, 2, 3 },
                    [DirecaoSprite.Cima] = new[] { 4, 5, 6 },
                    [DirecaoSprite.Baixo] = new[] { 7, 8, 9, 10 },
                    [DirecaoSprite.Esquerda] = new[] { 11, 12, 13, 14 },
                },
                FrameMortePorDirecao = new Dictionary<DirecaoSprite, int>
                {
                    [DirecaoSprite.Baixo] = 15,
                    [DirecaoSprite.Direita] = 16,
                    [DirecaoSprite.Cima] = 17,
                    [DirecaoSprite.Esquerda] = 18,
                }
            },

            // predador.png: grid de 4 colunas x 4 linhas (32x32 por frame), sem frames de
            // morte dedicados (a spritesheet não inclui pose de morte). Layout confirmado:
            //   0  1  2  3   -> baixo, baixo, baixo, baixo
            //   4  5  6  7   -> direita, direita, direita, direita
            //   8  9  10 11  -> esquerda, esquerda, esquerda, esquerda
            //   12 13 14 15  -> cima, cima, cima, cima
            ["predador"] = new InfoSpriteSheet
            {
                LarguraFrame = 32,
                AlturaFrame = 32,
                Colunas = 4,
                TicksPorFrame = 8,
                TicksExibicaoMorte = 30,
                FramesCaminhadaPorDirecao = new Dictionary<DirecaoSprite, int[]>
                {
                    [DirecaoSprite.Baixo] = new[] { 0, 1, 2, 3 },
                    [DirecaoSprite.Direita] = new[] { 4, 5, 6, 7 },
                    [DirecaoSprite.Esquerda] = new[] { 8, 9, 10, 11 },
                    [DirecaoSprite.Cima] = new[] { 12, 13, 14, 15 },
                },
                FrameMortePorDirecao = new Dictionary<DirecaoSprite, int>()
            },

            // tijolo.png: spritesheet plana (não direcional) de 6 colunas x 1 linha
            // (64x65 por frame). Layout confirmado:
            //   0 = normal, 1 = golpeado, 2-5 = sequência de explosão.
            // Não usa FramesCaminhadaPorDirecao/FrameMortePorDirecao (o Tijolo controla o
            // FrameSprite diretamente via Tijolo.FrameSpriteAtual); aqui só interessam
            // LarguraFrame/AlturaFrame/Colunas, usados pelo MotorGrafico para recortar o frame.
            ["tijolo"] = new InfoSpriteSheet
            {
                LarguraFrame = 64,
                AlturaFrame = 65,
                Colunas = 6,
                FramesCaminhadaPorDirecao = new Dictionary<DirecaoSprite, int[]>(),
                FrameMortePorDirecao = new Dictionary<DirecaoSprite, int>()
            }
        };

        public static bool TryObterInfo(string idSprite, out InfoSpriteSheet info)
        {
            return Animacoes.TryGetValue(idSprite, out info);
        }
    }
}
