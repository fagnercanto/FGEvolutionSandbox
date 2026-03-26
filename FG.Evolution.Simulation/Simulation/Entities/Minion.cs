using FG.Evolution.Simulation.Engine.Core;
using FG.Evolution.Simulation.Engine.Graphics;
using Raylib_cs;

namespace FG.Evolution.Simulation.Simulation.Entities
{
    // NOVO: O Minion agora herda as propriedades físicas do chassi da Engine
    public class Minion : ElementoCenario
    {
        private static Random sorteador = new Random();

        // Propriedades Biológicas (Exclusivas do Minion)
        public float[] DNA { get; private set; }
        public int TempoVivo { get; set; }
        public bool Vivo { get; set; }

        // CONSTRUTOR
        public Minion(float[] dna = null)
        {
            // 1. Configuração do DNA (Mantém o seu código atual)
            if (dna == null)
            {
                DNA = new float[12];
                for (int i = 0; i < 12; i++)
                {
                    DNA[i] = (float)(sorteador.NextDouble() * 2 - 1);
                }
            }
            else
            {
                DNA = dna;
            }

            // 2. NOVO: Configuração do Chassi Visual herdado
            Tamanho = 15; // Ou Config.TamanhoMinion, se já tiver importado
            CorBase = Color.Green;
            CorBorda = Color.Black;
            Formato = FormatoVisual.QuadradoComNariz;
        }

        // --------------------------------------------------------
        // NASCIMENTO: Coloca o minion no meio da tela pronto pra viver
        // --------------------------------------------------------
        public void ResetarPosicaoECronometro()
        {
            // Nascem em um ponto fixo (ex: 50, 50), mas com uma "tremidinha" de -15 a 15 pixels
            X = 50 + sorteador.Next(-15, 16);
            Y = 50 + sorteador.Next(-15, 16);

            // Cada um nasce olhando para um lado totalmente aleatório (0 a 360 graus)
            Angulo = (float)(sorteador.NextDouble() * System.Math.PI * 2);

            Vivo = true;
            TempoVivo = 0;
        }

        // --------------------------------------------------------
        // CÉREBRO: Onde a mágica (multiplicação de matrizes) acontece
        // --------------------------------------------------------
        public string Pensar(float distanciaAlvo, float anguloAlvo)
        {
            // O DNA agora usa a distância da comida e o ângulo para ela.
            // Índice 0, 1 e 2: Pesos e Viés para virar o volante à Esquerda
            float vontadeGirar = (distanciaAlvo * DNA[0]) + (anguloAlvo * DNA[1]) + DNA[2];

            // Índice 3, 4 e 5: Pesos e Viés para pisar no Acelerador
            float vontadeFrente = (distanciaAlvo * DNA[3]) + (anguloAlvo * DNA[4]) + DNA[5];

            // Índice 6, 7 e 8: Pesos e Viés para virar o volante à Direita
            float vontadeAtacar = (distanciaAlvo * DNA[6]) + (anguloAlvo * DNA[7]) + DNA[8];

            // Índice 9, 10 e 11: Pesos e Viés para puxar o Freio de Mão
            float vontadeFugir = (distanciaAlvo * DNA[9]) + (anguloAlvo * DNA[10]) + DNA[11];

            float maiorVontade = vontadeGirar;
            string acaoVencedora = "Girar";

            if (vontadeFrente > maiorVontade) { maiorVontade = vontadeFrente; acaoVencedora = "Frente"; }
            if (vontadeAtacar > maiorVontade) { maiorVontade = vontadeAtacar; acaoVencedora = "Atacar"; }
            if (vontadeFugir > maiorVontade) { maiorVontade = vontadeFugir; acaoVencedora = "Fugir"; }

            return acaoVencedora;
        }

        // --------------------------------------------------------
        // EVOLUÇÃO: Clonar este minion e adicionar pequenas mutações
        // --------------------------------------------------------
        public Minion ClonarEMutar()
        {
            float forcaMutacao = 0.1f; // Varia até 10% para cima ou para baixo
            float[] dnaFilho = new float[12];

            for (int i = 0; i < DNA.Length; i++)
            {
                // Usando o nosso 'sorteador' estático para garantir mutações únicas
                float mutacao = (float)(sorteador.NextDouble() * 2 - 1) * forcaMutacao;
                dnaFilho[i] = DNA[i] + mutacao;
            }

            return new Minion(dnaFilho);
        }
    }
}