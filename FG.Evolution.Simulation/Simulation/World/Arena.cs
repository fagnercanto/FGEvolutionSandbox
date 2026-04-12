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

        // NOVO: Coleção de recursos
        public List<Comida> Alimentos { get; private set; }
        private Random sorteador = new Random();

        //// NOVO: A comida agora é um objeto real
        //public Comida Alimento { get; private set; }

        // --- NOVO: AS COORDENADAS DO ALVO ---
        //public int Alimento.X { get; private set; }
        //public int Alimento.Y { get; private set; }


        // O Construtor agora só pede a maleta de configuração
        public Arena(Configuracao config)
        {
            Config = config;
            GeracaoAtual = 1;
            Alimentos = new List<Comida>();
            EspalharComida(); // O Semeador inicial
            //// Instancia o objeto Comida
            //Alimento = new Comida(config.LarguraTela - 100, config.AlturaTela - 100, 15); // Assumindo tamanho 15
            //// Coloca a comida fixa no canto inferior direito
            //Alimento.X = config.LarguraTela - 100;
            //Alimento.Y = config.AlturaTela - 100;
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

        // NOVO MÉTOD0: Semeia 10 alvos pelo mapa
        private void EspalharComida()
        {
            Alimentos.Clear();
            int quantidadeAlvos = 10;

            for (int i = 0; i < quantidadeAlvos; i++)
            {
                // Garante que a comida nasça dentro dos limites da tela
                int limiteX = Config.LarguraTela - Config.EspessuraParede - 15;
                int limiteY = Config.AlturaTela - Config.EspessuraParede - 15;

                float px = sorteador.Next(Config.EspessuraParede, limiteX);
                float py = sorteador.Next(Config.EspessuraParede, limiteY);

                Alimentos.Add(new Comida(px, py, 15));
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

                // 1. Sensores e Decisão (Foco no alvo mais próximo)
                Comida alvo = ObterComidaMaisProxima(m);
                string decisao = "Fugir"; // Ação padrão se não houver comida

                if (alvo != null)
                {
                    var leitura = Sensor.LerAmbiente(m, alvo.X, alvo.Y);
                    decisao = m.Pensar(leitura.Distancia, leitura.DiferencaAngulo);
                }

                // --- O BLOCO QUE FALTAVA: TRADUZIR PENSAMENTO EM MOVIMENTO ---
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

                // ... (código de movimento Frente/Girar continua igual)

                // 3. Checagem de Morte (Parede)
                bool bateuNaParede = m.X <= limiteEsquerdo || m.X >= limiteDireito || m.Y <= limiteSuperior || m.Y >= limiteInferior;
                bool morreuDeVelho = m.TempoVivo >= Config.TempoMaximoGeracao;

                // 3.1 Checagem de Sucesso e Consumo Real
                bool achouComida = false;
                for (int i = Alimentos.Count - 1; i >= 0; i--)
                {
                    var c = Alimentos[i];
                    if (m.X < c.X + Config.TamanhoMinion && m.X + Config.TamanhoMinion > c.X &&
                        m.Y < c.Y + Config.TamanhoMinion && m.Y + Config.TamanhoMinion > c.Y)
                    {
                        achouComida = true;
                        Alimentos.RemoveAt(i); // A comida é devorada e some do mapa
                        break;
                    }
                }

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
            Comida alvo = ObterComidaMaisProxima(m);
            if (alvo == null) return 0; // Se comeram tudo, distância é 0 (sucesso absoluto)

            float dX = m.X - alvo.X;
            float dY = m.Y - alvo.Y;
            return (float)Math.Sqrt(dX * dX + dY * dY);
        }

        // Extrai o estado físico do mundo e converte em pacotes visuais cegos
        public List<SnapshotVisual> ObterCenaVisual()
        {
            List<SnapshotVisual> cena = new List<SnapshotVisual>();

            // 1. Fotografa todas as Comidas no mapa
            foreach (var comida in Alimentos)
            {
                cena.Add(comida.GerarSnapshot());
            }

            // 2. Fotografa a População
            foreach (var minion in Populacao)
            {
                if (minion.Vivo)
                {
                    cena.Add(minion.GerarSnapshot());
                }
            }

            return cena;
        }

        // NOVO MÉTODO: Descobre qual é a comida mais próxima do minion
        private Comida ObterComidaMaisProxima(Minion m)
        {
            if (Alimentos.Count == 0) return null;

            return Alimentos.OrderBy(c =>
            {
                float dX = m.X - c.X;
                float dY = m.Y - c.Y;
                return Math.Sqrt(dX * dX + dY * dY);
            }).First();
        }
    }
}