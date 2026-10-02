using FG.Evolution.Simulation.Engine.Graphics;
using Raylib_cs;

namespace FG.Evolution.Simulation.Engine.Core
{
    // 'abstract' significa que você nunca vai instanciar um "ElementoCenario" puro.
    // Você só instanciará coisas que herdam dele (Minion, Comida, etc).
    public abstract class ElementoCenario
    {
        // Propriedades Físicas Universais
        public float X { get; set; }
        public float Y { get; set; }
        public float Tamanho { get; set; }
        public float Angulo { get; set; }

        // Propriedades Visuais Universais
        public Color CorBase { get; set; }
        public Color CorBorda { get; set; }
        public FormatoVisual Formato { get; set; }

        // Identificador do sprite (nome de arquivo sem extensão) usado pelo MotorGrafico
        // para tentar carregar Assets/Sprites/{IdSprite}.png. Null = sem sprite (usa fallback vetorial).
        public string IdSprite { get; set; }

        // O Empacotador (A ponte para a Engine Gráfica)
        // Qualquer objeto no jogo agora sabe gerar o seu próprio Snapshot!
        public virtual SnapshotVisual GerarSnapshot()
        {
            return new SnapshotVisual
            {
                X = X,
                Y = Y,
                Tamanho = Tamanho,
                Angulo = Angulo,
                CorBase = CorBase,
                CorBorda = CorBorda,
                Formato = Formato,
                NivelHp = null,
                IdSprite = IdSprite
            };
        }
    }
}