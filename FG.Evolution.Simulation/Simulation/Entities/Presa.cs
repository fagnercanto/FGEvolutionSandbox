using FG.Evolution.Simulation.Config;
using FG.Evolution.Simulation.Engine.Graphics;
using Raylib_cs;

namespace FG.Evolution.Simulation.Simulation.Entities
{
    // Agente Evolutivo 1: tamanho fixo, sobrevive fugindo de Predadores/Tijolos e comendo Plantas.
    public class Presa : AgenteEvolutivo
    {
        public Presa(float[] dnaHerdado = null) : base(dnaHerdado)
        {
            Tamanho = 10f;
            CorBase = Color.DarkBlue;
            CorBorda = Color.Black;
            Formato = FormatoVisual.QuadradoComNariz;
            IdSprite = "presa"; // Assets/Sprites/presa.png (ex: joaninha)
        }

        public override void ResetarPosicaoECronometro(Configuracao config, float x, float y)
        {
            base.ResetarPosicaoECronometro(config, x, y);
            Tamanho = config.TamanhoPresa;
        }

        // Clona esta Presa aplicando mutação simples (sem crossover) no DNA.
        public Presa ClonarEMutar(Configuracao config)
        {
            float[] dnaFilho = Cerebro.ClonarEMutarDna(config.ForcaMutacao);
            return new Presa(dnaFilho);
        }
    }
}
