using FG.Evolution.Simulation.Config;
using FG.Evolution.Simulation.Engine.Graphics;
using Raylib_cs;

namespace FG.Evolution.Simulation.Simulation.Entities
{
    // Agente Evolutivo 2: tamanho fixo (sem crescimento por HP).
    // Sobrevive comendo Presas.
    public class Predador : AgenteEvolutivo
    {
        public Predador(float[] dnaHerdado = null) : base(dnaHerdado)
        {
            Tamanho = 10f;
            CorBase = Color.Maroon;
            CorBorda = Color.Black;
            Formato = FormatoVisual.QuadradoComNariz;
            IdSprite = "predador"; // Assets/Sprites/predador.png (ex: lagartixa)
        }

        public override void ResetarPosicaoECronometro(Config.Configuracao config, float x, float y)
        {
            base.ResetarPosicaoECronometro(config, x, y);
            Tamanho = config.TamanhoPredador;
        }

        // Predador parado perde vida mais rápido que a Presa (ver PenalidadeOciosidadePredadorPorTick).
        public override void AplicarDecaimentoDeHp(Configuracao config, bool estaParado)
        {
            Hp -= config.DecaimentoHpPorTick;

            if (estaParado)
            {
                Hp -= config.PenalidadeOciosidadePredadorPorTick;
            }

            if (Hp < 0f) Hp = 0f;
        }

        // Clona este Predador aplicando mutação simples (sem crossover) no DNA.
        public Predador ClonarEMutar(Configuracao config)
        {
            float[] dnaFilho = Cerebro.ClonarEMutarDna(config.ForcaMutacao);
            return new Predador(dnaFilho);
        }
    }
}

