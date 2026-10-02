using System;
using FG.Evolution.Simulation.Engine.Core;
using FG.Evolution.Simulation.Engine.Graphics;
using Raylib_cs;

namespace FG.Evolution.Simulation.Simulation.Entities
{
    // Obstáculo dinâmico. Nasce apenas no tick zero (população fixa, sem respawn).
    // Ao sofrer impacto, desloca-se no vetor oposto ao impacto e acumula dano.
    // Após 3 impactos, quebra e desaparece do mapa.
    public class Tijolo : ElementoCenario
    {
        // tijolo.png: spritesheet de 6 frames em uma única linha (índices 0-5):
        //   0 = normal, 1 = golpeado, 2-5 = sequência de explosão (ver SpriteSheets.cs).
        private const int FrameNormal = 0;
        private const int FrameGolpeado = 1;
        private static readonly int[] FramesExplosao = { 2, 3, 4, 5 };

        // Quantos ticks o frame "golpeado" fica visível até o tijolo assumir a nova
        // posição e voltar ao frame normal (caso não tenha quebrado neste impacto).
        private const int TicksExibicaoGolpe = 10;

        // Quantos ticks cada frame da sequência de explosão fica visível.
        private const int TicksPorFrameExplosao = 6;

        private int ticksGolpeRestantes;
        private int indiceFrameExplosao;
        private int ticksNoFrameExplosaoAtual;

        // Índice do frame atual na spritesheet (0-5), consumido por GerarSnapshot().
        public int FrameSpriteAtual { get; private set; } = FrameNormal;

        // True quando a sequência de explosão (frames 2-5) terminou de tocar e o
        // tijolo já pode ser removido de fato do mapa.
        public bool AnimacaoExplosaoConcluida { get; private set; }

        public int ImpactosSofridos { get; private set; }
        public bool Quebrado { get; private set; }

        public Tijolo(float x, float y, float tamanho)
        {
            X = x;
            Y = y;
            Tamanho = tamanho;
            Angulo = 0f;

            CorBase = Color.Brown;
            CorBorda = Color.Black;
            Formato = FormatoVisual.Quadrado;
            IdSprite = "tijolo"; // Assets/Sprites/tijolo.png
        }

        // Aplica um impacto vindo da direção (dirX, dirY) — o tijolo se desloca
        // 'deslocamentoPx' pixels no vetor OPOSTO ao impacto e acumula um golpe.
        // Retorna true se o tijolo quebrou (atingiu o limite de impactos) neste golpe.
        public bool ReceberImpacto(float dirX, float dirY, float deslocamentoPx)
        {
            if (Quebrado)
            {
                return true;
            }

            float magnitude = (float)Math.Sqrt((dirX * dirX) + (dirY * dirY));
            if (magnitude > 0.0001f)
            {
                float vetorOpostoX = -dirX / magnitude;
                float vetorOpostoY = -dirY / magnitude;

                X += vetorOpostoX * deslocamentoPx;
                Y += vetorOpostoY * deslocamentoPx;
            }

            ImpactosSofridos++;

            if (ImpactosSofridos >= 3)
            {
                Quebrado = true;
                indiceFrameExplosao = 0;
                ticksNoFrameExplosaoAtual = 0;
                FrameSpriteAtual = FramesExplosao[0];
            }
            else
            {
                ticksGolpeRestantes = TicksExibicaoGolpe;
                FrameSpriteAtual = FrameGolpeado;
            }

            return Quebrado;
        }

        // Avança a animação em 1 tick: cronometra o frame de "golpeado" (voltando ao
        // normal quando o tempo esgota) ou avança a sequência de explosão quando quebrado.
        public void AtualizarAnimacao()
        {
            if (Quebrado)
            {
                if (AnimacaoExplosaoConcluida)
                {
                    return;
                }

                ticksNoFrameExplosaoAtual++;
                if (ticksNoFrameExplosaoAtual >= TicksPorFrameExplosao)
                {
                    ticksNoFrameExplosaoAtual = 0;
                    indiceFrameExplosao++;

                    if (indiceFrameExplosao >= FramesExplosao.Length)
                    {
                        AnimacaoExplosaoConcluida = true;
                        return;
                    }
                }

                FrameSpriteAtual = FramesExplosao[indiceFrameExplosao];
                return;
            }

            if (ticksGolpeRestantes > 0)
            {
                ticksGolpeRestantes--;
                FrameSpriteAtual = ticksGolpeRestantes == 0 ? FrameNormal : FrameGolpeado;
            }
        }

        public override Engine.Graphics.SnapshotVisual GerarSnapshot()
        {
            var snapshot = base.GerarSnapshot();
            snapshot.FrameSprite = FrameSpriteAtual;
            return snapshot;
        }
    }
}
