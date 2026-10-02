using System;
using FG.Evolution.Simulation.Config;
using FG.Evolution.Simulation.Engine.Core;
using FG.Evolution.Simulation.Engine.Graphics;

namespace FG.Evolution.Simulation.Simulation.Entities
{
    // Base comum para os dois agentes evolutivos do ecossistema (Presa e Predador):
    // possuem DNA/RedeNeural, HP percentual com decaimento por tick, penalidade de
    // ociosidade e um ciclo de vida (nascimento -> vida -> morte) ligado à Arena.
    public abstract class AgenteEvolutivo : ElementoCenario
    {
        private static readonly Random sorteador = new Random();

        public RedeNeural Cerebro { get; private set; }
        public float[] DNA => Cerebro.DNA;

        // HP em percentual (0 a 100).
        public float Hp { get; protected set; }
        public bool Vivo { get; set; }
        public int TempoVivo { get; set; }

        // --- Estado de animação de spritesheet (ignorado se IdSprite não tiver InfoSpriteSheet) ---

        // Direção cardinal atual, atualizada pela Arena a cada tick conforme o vetor de movimento.
        public DirecaoSprite DirecaoAtual { get; set; } = DirecaoSprite.Baixo;

        // Contadores internos de animação, avançados pela Arena a cada tick.
        private int ticksNoFrameAtual;
        private int indiceNoCicloCaminhada;
        private int ticksExibindoMorte;
        private DirecaoSprite? direcaoDoUltimoCiclo;
        public bool AnimacaoMorteConcluida { get; private set; }

        // Índice achatado (flat) do frame atual dentro da spritesheet, já resolvendo se é
        // um frame de caminhada ou o frame único de morte. -1 se não houver spritesheet.
        public int FrameSpriteAtual { get; private set; } = -1;

        // Avança a animação de caminhada em 1 tick. 'estaSeMovendo' controla se o frame avança
        // (parado = mantém o primeiro frame do ciclo, evitando "andar no lugar").
        public void AtualizarAnimacaoCaminhada(bool estaSeMovendo)
        {
            if (!SpriteSheets.TryObterInfo(IdSprite, out var info) ||
                !info.FramesCaminhadaPorDirecao.TryGetValue(DirecaoAtual, out var framesCiclo))
            {
                FrameSpriteAtual = -1;
                return;
            }

            // Cada direção pode ter uma quantidade diferente de frames no ciclo (ex: Cima com
            // 3 frames vs. as demais com 4). Ao trocar de direção, o índice precisa ser
            // reiniciado, senão pode ficar fora dos limites do novo ciclo (mais curto).
            if (direcaoDoUltimoCiclo != DirecaoAtual)
            {
                direcaoDoUltimoCiclo = DirecaoAtual;
                ticksNoFrameAtual = 0;
                indiceNoCicloCaminhada = 0;
            }

            if (!estaSeMovendo)
            {
                ticksNoFrameAtual = 0;
                indiceNoCicloCaminhada = 0;
            }
            else
            {
                ticksNoFrameAtual++;
                if (ticksNoFrameAtual >= info.TicksPorFrame)
                {
                    ticksNoFrameAtual = 0;
                    indiceNoCicloCaminhada = (indiceNoCicloCaminhada + 1) % framesCiclo.Length;
                }
            }

            indiceNoCicloCaminhada = Math.Clamp(indiceNoCicloCaminhada, 0, framesCiclo.Length - 1);
            FrameSpriteAtual = framesCiclo[indiceNoCicloCaminhada];
        }

        // Avança a animação de morte em 1 tick (frame único, apenas cronometrando quanto
        // tempo ele fica visível). Retorna true quando o tempo de exibição esgotou (a Arena
        // pode então remover o efeito da cena).
        public bool AtualizarAnimacaoMorte()
        {
            if (!SpriteSheets.TryObterInfo(IdSprite, out var info) ||
                !info.FrameMortePorDirecao.TryGetValue(DirecaoAtual, out var frameMorte))
            {
                AnimacaoMorteConcluida = true;
                return true;
            }

            FrameSpriteAtual = frameMorte;

            ticksExibindoMorte++;
            if (ticksExibindoMorte >= info.TicksExibicaoMorte)
            {
                AnimacaoMorteConcluida = true;
                return true;
            }

            return false;
        }

        protected AgenteEvolutivo(float[] dnaHerdado = null)
        {
            Cerebro = new RedeNeural(dnaHerdado);
        }

        public bool EstaMorto => Hp <= 0f;

        // Coloca o agente pronto para viver na posição (x, y) informada pela Arena
        // (que garante, via checagem de colisão, que nenhum ponto do mapa seja compartilhado
        // por duas entidades ao nascer). Ângulo, HP inicial e cronômetro são zerados aqui.
        public virtual void ResetarPosicaoECronometro(Configuracao config, float x, float y)
        {
            X = x;
            Y = y;
            Angulo = (float)(sorteador.NextDouble() * Math.PI * 2);

            Hp = config.HpInicialPercentual;
            Vivo = true;
            TempoVivo = 0;

            DirecaoAtual = DirecaoSprite.Baixo;
            ticksNoFrameAtual = 0;
            indiceNoCicloCaminhada = 0;
            ticksExibindoMorte = 0;
            direcaoDoUltimoCiclo = null;
            FrameSpriteAtual = -1;
            AnimacaoMorteConcluida = false;
        }

        // Aplica o decaimento natural de HP por tick, mais a penalidade extra de ociosidade
        // caso o agente esteja com velocidade vetorial zero (parado). A penalidade de
        // ociosidade é sobrescrita pelo Predador (que perde vida mais rápido parado do que a Presa).
        public virtual void AplicarDecaimentoDeHp(Configuracao config, bool estaParado)
        {
            Hp -= config.DecaimentoHpPorTick;

            if (estaParado)
            {
                Hp -= config.PenalidadeOciosidadePorTick;
            }

            if (Hp < 0f) Hp = 0f;
        }

        // Restaura o HP ao máximo ao se alimentar (Presa come Planta, Predador come Presa).
        public virtual void Alimentar(Configuracao config)
        {
            Hp = config.HpMaximoPercentual;
        }

        // Sofre dano percentual (ex: empurrão de Tijolo).
        public void ReceberDano(float danoPercentual)
        {
            Hp -= danoPercentual;
            if (Hp < 0f) Hp = 0f;
        }

        // Nível de HP (1 a 10) para exibição ao vivo sobre o agente. Arredonda o HP
        // percentual (0-100) para uma escala de 1 a 10, útil para observar visualmente
        // o quão perto da inanição um indivíduo está.
        public int NivelHp
        {
            get
            {
                float hpClamped = Math.Clamp(Hp, 0f, 100f);
                int nivel = (int)Math.Ceiling(hpClamped / 10f);
                return Math.Clamp(nivel, hpClamped > 0f ? 1 : 0, 10);
            }
        }

        public override Engine.Graphics.SnapshotVisual GerarSnapshot()
        {
            var snapshot = base.GerarSnapshot();
            snapshot.NivelHp = NivelHp;
            snapshot.Direcao = DirecaoAtual;
            snapshot.FrameSprite = FrameSpriteAtual;
            return snapshot;
        }
    }
}
