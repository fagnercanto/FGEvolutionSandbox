using FG.Evolution.Simulation.Config;
using FG.Evolution.Simulation.Engine.Core;
using FG.Evolution.Simulation.Engine.Graphics;
using FG.Evolution.Simulation.Simulation.Entities;
using Raylib_cs;
using System.Collections.Generic;
using System.Linq;

namespace FG.Evolution.Simulation.Simulation.World
{
    public class Arena
    {
        // 1. A Arena agora possui a maleta de regras do universo
        public Configuracao Config { get; private set; }

        // 2. Controle da População
        public List<Minion> Populacao { get; private set; }
        public int GeracaoAtual { get; private set; }

        // --- NOVO: AS COORDENADAS DO ALVO ---
        public int ComidaX { get; private set; }
        public int ComidaY { get; private set; }

        // O Construtor agora só pede a maleta de configuração
        public Arena(Configuracao config)
        {
            Config = config;
            GeracaoAtual = 1;
            // Coloca a comida fixa no canto inferior direito
            ComidaX = config.LarguraTela - 100;
            ComidaY = config.AlturaTela - 100;
            // --- TENTA LER O ARQUIVO SALVO (Sua persistência continua intacta) ---
            float[] dnaBase = null;
            if (File.Exists("melhor_dna.txt"))
            {
                string arquivo = File.ReadAllText("melhor_dna.txt");
                string[] pedacos = arquivo.Split(';');
                dnaBase = new float[12];
                for (int i = 0; i < 12; i++)
                {
                    dnaBase[i] = float.Parse(pedacos[i]);
                }
            }

            // --- CRIA A GERAÇÃO 1 (Usando o número que vem da maleta) ---
            Populacao = new List<Minion>();
            for (int i = 0; i < Config.TamanhoPopulacao; i++)
            {
                Minion novoMinion;

                if (dnaBase != null) novoMinion = new Minion(dnaBase).ClonarEMutar();
                else novoMinion = new Minion(); // Aleatório

                novoMinion.ResetarPosicaoECronometro();
                Populacao.Add(novoMinion);
            }
        }

        // O coração da simulação. O Main vai chamar isso 60 vezes por segundo.
        public void AtualizarMundo()
        {
            // Lendo as medidas direto da maleta
            int limiteEsquerdo = Config.EspessuraParede;
            int limiteSuperior = Config.EspessuraParede;
            int limiteDireito = Config.LarguraTela - Config.EspessuraParede - Config.TamanhoMinion;
            int limiteInferior = Config.AlturaTela - Config.EspessuraParede - Config.TamanhoMinion;

            // As engrenagens do motor do tanque
            float velocidadeRotacao = 0.15f;
            float velocidadeMovimento = 5.0f;

            int minionsVivos = 0;

            foreach (var m in Populacao)
            {
                if (!m.Vivo) continue;

                minionsVivos++;
                m.TempoVivo++;

                // 1. Sensores e Decisão (A NOVA ARQUITETURA)
                // O utilitário Sensor faz a matemática pesada e entrega apenas os dados limpos
                var leitura = Sensor.LerAmbiente(m, ComidaX, ComidaY);
                string decisao = m.Pensar(leitura.Distancia, leitura.DiferencaAngulo);

                // 2. Movimento (Física de Tanque)
                if (decisao == "Frente")
                {
                    // Acelera na direção do nariz
                    m.X += (float)Math.Cos(m.Angulo) * velocidadeMovimento;
                    m.Y += (float)Math.Sin(m.Angulo) * velocidadeMovimento;
                }
                else if (decisao == "Girar")
                {
                    // Vira o volante para a Esquerda
                    m.Angulo -= velocidadeRotacao;
                }
                else if (decisao == "Atacar")
                {
                    // Vira o volante para a Direita
                    m.Angulo += velocidadeRotacao;
                }
                // Se a decisão for "Fugir", ele simplesmente não entra em nenhum if e fica parado freando.

                // 3. Checagem de Morte (Parede)
                bool bateuNaParede = m.X <= limiteEsquerdo || m.X >= limiteDireito || m.Y <= limiteSuperior || m.Y >= limiteInferior;

                // 3.1 Checagem de Sucesso (Comida)
                bool achouComida = m.X < ComidaX + Config.TamanhoMinion &&
                                   m.X + Config.TamanhoMinion > ComidaX &&
                                   m.Y < ComidaY + Config.TamanhoMinion &&
                                   m.Y + Config.TamanhoMinion > ComidaY;

                // 3.2 Checagem de Velhice
                bool morreuDeVelho = m.TempoVivo >= Config.TempoMaximoGeracao;

                // Se bateu na parede, ficou velho, OU ACHOU A COMIDA, o motor desliga!
                if (bateuNaParede || morreuDeVelho || achouComida)
                {
                    m.Vivo = false;
                }
            }

            // 4. Evolução (Se todos morreram)
            if (minionsVivos == 0)
            {
                EvoluirGeracao();
            }
        }
        private void EvoluirGeracao()
        {
            // 1. O PÓDIO: Ordenamos toda a população da menor distância para a maior
            var ranking = Populacao.OrderBy(m => CalcularDistanciaAteComida(m)).ToList();

            // 2. A ELITE: Pegamos os 5 melhores (O nosso Top 5)
            int tamanhoElite = 5;
            var elite = ranking.Take(tamanhoElite).ToList();

            // 3. O REI: Guardamos apenas o Top 1 no ficheiro para salvar o progresso
            Minion rei = elite[0];
            string dnaTexto = string.Join(";", rei.DNA);
            File.WriteAllText("melhor_dna.txt", dnaTexto);

            // 4. A ROLETA GENÉTICA: Criar a nova geração misturando a Elite
            Minion[] novaPopulacao = new Minion[Config.TamanhoPopulacao];
            Random sorteadorPai = new Random(); // Para escolher quem do Top 5 vai reproduzir

            for (int i = 0; i < Config.TamanhoPopulacao; i++)
            {
                // Sorteia um dos 5 campeões para ser o pai deste bebé
                Minion paiSorteado = elite[sorteadorPai.Next(elite.Count)];

                // Clona e muta o DNA desse pai específico
                novaPopulacao[i] = paiSorteado.ClonarEMutar();
                novaPopulacao[i].ResetarPosicaoECronometro();
            }

            // Substitui a população velha pelos novos filhos
            Populacao = [.. novaPopulacao];
            GeracaoAtual++;
        }
        private float CalcularDistanciaAteComida(Minion m)
        {
            // Matemática básica de distância entre dois pontos (Pitágoras)
            float dX = m.X - ComidaX;
            float dY = m.Y - ComidaY;
            return (float)Math.Sqrt(dX * dX + dY * dY);
        }

        // Extrai o estado físico do mundo e converte em pacotes visuais cegos
        public List<SnapshotVisual> ObterCenaVisual()
        {
            List<SnapshotVisual> cena = new List<SnapshotVisual>();

            // 1. Fotografa a Comida
            // Como a Comida ainda não é uma classe herdada, montamos o pacote dela manualmente aqui
            cena.Add(new SnapshotVisual
            {
                X = this.ComidaX,
                Y = this.ComidaY,
                Tamanho = 15, // Ajuste para o seu Config.TamanhoMinion
                CorBase = Color.Blue,
                CorBorda = default, // Sem borda
                Formato = FormatoVisual.Quadrado
            });

            // 2. Fotografa a População
            foreach (var minion in Populacao)
            {
                if (minion.Vivo)
                {
                    // Como o Minion agora herda do Chassi, ele já sabe se empacotar sozinho!
                    cena.Add(minion.GerarSnapshot());
                }
            }

            return cena;
        }
    }
}