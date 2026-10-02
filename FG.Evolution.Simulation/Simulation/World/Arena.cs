using FG.Evolution.Simulation.Config;
using FG.Evolution.Simulation.Engine.Core;
using FG.Evolution.Simulation.Engine.Graphics;
using FG.Evolution.Simulation.Persistence;
using FG.Evolution.Simulation.Simulation.Entities;
using System;
using System.Collections.Generic;
using System.Linq;

namespace FG.Evolution.Simulation.Simulation.World
{
    // O mundo do ecossistema Predador vs Presa: gerencia populações, física de
    // engavetamento, alimentação, decaimento de HP e evolução por espécie.
    // Funciona tanto no modo gráfico (Program.cs + MotorGrafico) quanto no modo Headless.
    public class Arena
    {
        public Configuracao Config { get; private set; }

        public List<Presa> Presas { get; private set; }
        public List<Predador> Predadores { get; private set; }
        public List<Planta> Plantas { get; private set; }
        public List<Tijolo> Tijolos { get; private set; }

        public int GeracaoAtual { get; private set; }
        public int TickAtual { get; private set; }

        // Tick (dentro da geração atual) em que ocorreu a última morte registrada.
        // É o dado-chave usado pelo Calibrador para decidir cientificamente se um Timeout é necessário.
        public int TickUltimaMorte { get; private set; }

        private readonly Random sorteador = new Random();
        private readonly Repositorio_Genoma repositorio;
        private int ticksDesdeUltimaPlanta;

        // Agentes recém-mortos que ainda estão tocando a animação de morte da spritesheet
        // (posição/sprite/direção "congelados" no instante da morte). Removidos da lista
        // assim que o ciclo de frames de morte terminar.
        private readonly List<AgenteEvolutivo> efeitosDeMorte = new();

        public Arena(Configuracao config, Repositorio_Genoma repositorioGenoma = null)
        {
            Config = config;
            GeracaoAtual = 1;
            TickAtual = 0;
            TickUltimaMorte = 0;
            ticksDesdeUltimaPlanta = 0;

            repositorio = repositorioGenoma ?? new Repositorio_Genoma();
            repositorio.GarantirBancoCriado();

            var posicionados = new List<ElementoCenario>();

            Tijolos = new List<Tijolo>();
            Plantas = new List<Planta>();
            EspalharTijolos(posicionados); // Nascem apenas no tick zero (população fixa, sem respawn)
            EspalharPlantas(Config.PopulacaoPlantas, posicionados);

            Presas = new List<Presa>();
            Predadores = new List<Predador>();
            CriarPopulacaoInicial(posicionados);
        }

        // --------------------------------------------------------
        // CRIAÇÃO DO MUNDO (tick zero)
        // --------------------------------------------------------
        private void CriarPopulacaoInicial(List<ElementoCenario> posicionados)
        {
            var elitePresas = repositorio.ObterTopRanking(TipoEntidade.Presa, Config.TamanhoElite);
            var elitePredadores = repositorio.ObterTopRanking(TipoEntidade.Predador, Config.TamanhoElite);

            for (int i = 0; i < Config.PopulacaoPresas; i++)
            {
                Presa presa = CriarAPartirDaElite(elitePresas, dna => new Presa(dna));
                var (px, py) = EncontrarPosicaoLivre(Config.TamanhoPresa, posicionados);
                presa.ResetarPosicaoECronometro(Config, px, py);
                Presas.Add(presa);
                posicionados.Add(presa);
            }

            for (int i = 0; i < Config.PopulacaoPredadores; i++)
            {
                Predador predador = CriarAPartirDaElite(elitePredadores, dna => new Predador(dna));
                var (px, py) = EncontrarPosicaoLivre(Config.TamanhoPredador, posicionados);
                predador.ResetarPosicaoECronometro(Config, px, py);
                Predadores.Add(predador);
                posicionados.Add(predador);
            }
        }

        private T CriarAPartirDaElite<T>(List<RegistroEvolucao> elite, Func<float[], T> fabrica)
        {
            if (elite == null || elite.Count == 0)
            {
                return fabrica(null); // DNA aleatório
            }

            RegistroEvolucao paiSorteado = elite[sorteador.Next(elite.Count)];
            float[] dnaBase = Repositorio_Genoma.DeserializarGenoma(paiSorteado.Genoma_DNA);
            return fabrica(dnaBase);
        }

        private void EspalharTijolos(List<ElementoCenario> posicionados)
        {
            for (int i = 0; i < Config.PopulacaoTijolos; i++)
            {
                var (px, py) = EncontrarPosicaoLivre(Config.TamanhoTijolo, posicionados);
                var tijolo = new Tijolo(px, py, Config.TamanhoTijolo);
                Tijolos.Add(tijolo);
                posicionados.Add(tijolo);
            }
        }

        private void EspalharPlantas(int quantidade, List<ElementoCenario> posicionados)
        {
            for (int i = 0; i < quantidade; i++)
            {
                var (px, py) = EncontrarPosicaoLivre(Config.TamanhoPlanta, posicionados);
                var planta = new Planta(px, py, Config.TamanhoPlanta);
                Plantas.Add(planta);
                posicionados.Add(planta);
            }
        }

        // Chamado durante a simulação (spawn periódico de Planta), quando não há uma lista
        // de posicionados sendo montada incrementalmente: constrói o snapshot atual do mapa
        // (Tijolos + Plantas vivas + Presas vivas + Predadores vivos) para evitar sobreposição.
        private void EspalharPlantasEmTempoReal(int quantidade)
        {
            var posicionados = ObterTodosElementosAtivos();
            EspalharPlantas(quantidade, posicionados);
        }

        private List<ElementoCenario> ObterTodosElementosAtivos()
        {
            var elementos = new List<ElementoCenario>();
            elementos.AddRange(Tijolos);
            elementos.AddRange(Plantas.Where(p => p.Viva));
            elementos.AddRange(Presas.Where(p => p.Vivo));
            elementos.AddRange(Predadores.Where(p => p.Vivo));
            return elementos;
        }

        private (float X, float Y) PosicaoAleatoriaDentroDaArena(float tamanhoEntidade)
        {
            int limiteX = Config.LarguraTela - Config.EspessuraParede - (int)tamanhoEntidade;
            int limiteY = Config.AlturaTela - Config.EspessuraParede - (int)tamanhoEntidade;

            float px = sorteador.Next(Config.EspessuraParede, Math.Max(Config.EspessuraParede + 1, limiteX));
            float py = sorteador.Next(Config.EspessuraParede, Math.Max(Config.EspessuraParede + 1, limiteY));

            return (px, py);
        }

        // Sorteia posições até encontrar uma que não sobreponha nenhuma das entidades já
        // posicionadas (com uma pequena margem de segurança). Garante que "nada ocupe o mesmo spot".
        private const float MargemSemColisao = 2f;
        private const int MaxTentativasPosicaoLivre = 300;

        private (float X, float Y) EncontrarPosicaoLivre(float tamanhoEntidade, List<ElementoCenario> posicionados)
        {
            for (int tentativa = 0; tentativa < MaxTentativasPosicaoLivre; tentativa++)
            {
                var (px, py) = PosicaoAleatoriaDentroDaArena(tamanhoEntidade);

                bool colide = false;
                foreach (var outro in posicionados)
                {
                    if (SobrepoeComMargem(px, py, tamanhoEntidade, outro, MargemSemColisao))
                    {
                        colide = true;
                        break;
                    }
                }

                if (!colide)
                {
                    return (px, py);
                }
            }

            Console.WriteLine($"[Aviso] Não foi possível encontrar uma posição livre após {MaxTentativasPosicaoLivre} tentativas (mapa muito cheio). Uma entidade pode nascer sobreposta.");
            return PosicaoAleatoriaDentroDaArena(tamanhoEntidade);
        }

        private static bool SobrepoeComMargem(float x1, float y1, float tamanho1, ElementoCenario outro, float margem)
        {
            return x1 < outro.X + outro.Tamanho + margem && x1 + tamanho1 + margem > outro.X &&
                   y1 < outro.Y + outro.Tamanho + margem && y1 + tamanho1 + margem > outro.Y;
        }

        // --------------------------------------------------------
        // O CORAÇÃO DA SIMULAÇÃO
        // --------------------------------------------------------
        public void AtualizarMundo()
        {
            TickAtual++;
            AtualizarSpawnDePlantas();

            foreach (var presa in Presas)
            {
                if (!presa.Vivo) continue;
                AtualizarAgente(presa);
            }

            foreach (var predador in Predadores)
            {
                if (!predador.Vivo) continue;
                AtualizarAgente(predador);
            }

            AtualizarTijolos();
            RemoverTijolosQuebrados();
            AtualizarEfeitosDeMorte();

            if (TodasAsEspeciesExtintas() || TickAtual >= Config.TempoMaximoGeracao)
            {
                EvoluirGeracao();
            }
        }

        private void AtualizarSpawnDePlantas()
        {
            ticksDesdeUltimaPlanta++;
            if (ticksDesdeUltimaPlanta >= Config.TicksParaNovaPlanta)
            {
                ticksDesdeUltimaPlanta = 0;
                EspalharPlantasEmTempoReal(1);
            }
        }

        private void AtualizarAgente(AgenteEvolutivo agente)
        {
            agente.TempoVivo++;

            // 1. Sensores (até 3 alvos: Tijolo, espécie oposta e Planta, todos os mais próximos)
            Tijolo tijoloProximo = Sensor.ObterMaisProximo(agente, Tijolos.Where(t => !t.Quebrado));
            Planta plantaProxima = Sensor.ObterMaisProximo(agente, Plantas.Where(p => p.Viva));
            ElementoCenario alvoEspecieOposta = agente is Presa
                ? (ElementoCenario)Sensor.ObterMaisProximo(agente, Predadores.Where(p => p.Vivo))
                : (ElementoCenario)Sensor.ObterMaisProximo(agente, Presas.Where(p => p.Vivo));

            var leituras = Sensor.LerSensoresMultiplos(agente, tijoloProximo, alvoEspecieOposta, plantaProxima);

            // 2. Decisão
            AcaoAgente decisao = agente.Cerebro.Decidir(leituras);
            bool estaParado = decisao == AcaoAgente.Fugir;

            // 3. Movimento (física de tanque: girar também avança, como um carro fazendo
            // uma curva, em vez de girar no próprio eixo. Isso evita o efeito "pombo
            // possuído" de ficar rodando no lugar sem se deslocar).
            float xAntes = agente.X;
            float yAntes = agente.Y;

            switch (decisao)
            {
                case AcaoAgente.Frente:
                    agente.X += (float)Math.Cos(agente.Angulo) * Config.VelocidadeMovimento;
                    agente.Y += (float)Math.Sin(agente.Angulo) * Config.VelocidadeMovimento;
                    break;
                case AcaoAgente.GirarEsquerda:
                    agente.Angulo -= Config.VelocidadeRotacao;
                    agente.X += (float)Math.Cos(agente.Angulo) * Config.VelocidadeMovimento * Config.FatorVelocidadeAoGirar;
                    agente.Y += (float)Math.Sin(agente.Angulo) * Config.VelocidadeMovimento * Config.FatorVelocidadeAoGirar;
                    break;
                case AcaoAgente.GirarDireita:
                    agente.Angulo += Config.VelocidadeRotacao;
                    agente.X += (float)Math.Cos(agente.Angulo) * Config.VelocidadeMovimento * Config.FatorVelocidadeAoGirar;
                    agente.Y += (float)Math.Sin(agente.Angulo) * Config.VelocidadeMovimento * Config.FatorVelocidadeAoGirar;
                    break;

                case AcaoAgente.Fugir:
                    // Freio de mão: fica parado.
                    break;
            }

            // 4. Paredes: colisores sólidos, não causam dano nem matam. Apenas bloqueiam.
            ClampNasParedes(agente);

            // Animação: direção cardinal (para escolher a linha da spritesheet) e avanço
            // do frame de caminhada, com base no deslocamento real neste tick.
            float deslocamentoX = agente.X - xAntes;
            float deslocamentoY = agente.Y - yAntes;
            bool agenteSeMoveu = Math.Abs(deslocamentoX) > 0.0001f || Math.Abs(deslocamentoY) > 0.0001f;

            if (agenteSeMoveu)
            {
                agente.DirecaoAtual = ObterDirecaoCardinal(deslocamentoX, deslocamentoY);
            }

            agente.AtualizarAnimacaoCaminhada(agenteSeMoveu);

            // 5. Colisão com Tijolo (física de engavetamento)
            Tijolo tijoloColidido = ObterTijoloEmColisao(agente);
            if (tijoloColidido != null)
            {
                float dirX = agente.X - xAntes;
                float dirY = agente.Y - yAntes;
                if (Math.Abs(dirX) < 0.0001f && Math.Abs(dirY) < 0.0001f)
                {
                    // Sem vetor de movimento definido (ex: girando parado): usa o nariz como direção do impacto.
                    dirX = (float)Math.Cos(agente.Angulo);
                    dirY = (float)Math.Sin(agente.Angulo);
                }

                // O tijolo é empurrado pelo impacto do agente, se deslocando no vetor oposto.
                float dirImpactoTijoloX = tijoloColidido.X - agente.X;
                float dirImpactoTijoloY = tijoloColidido.Y - agente.Y;
                tijoloColidido.ReceberImpacto(dirImpactoTijoloX, dirImpactoTijoloY, Config.DeslocamentoTijoloPx);

                // O agente empurrado sofre dano e é bloqueado na posição anterior à colisão.
                agente.X = xAntes;
                agente.Y = yAntes;
                agente.ReceberDano(Config.DanoEmpurraoPercentual);

                if (agente.EstaMorto)
                {
                    RegistrarMorte(agente);
                }
                else if (agente is Predador predadorEmpurrado)
                {
                    // Predador é empurrado contra a Presa (recálculo físico no fim do tick / LateUpdate).
                    predadorEmpurrado.X += dirX * Config.DeslocamentoTijoloPx;
                    predadorEmpurrado.Y += dirY * Config.DeslocamentoTijoloPx;
                    ClampNasParedes(predadorEmpurrado);
                }
            }

            // 6. Alimentação / Predação
            if (agente is Presa presaAtual)
            {
                // 6a. Colisão sólida com Predador: a Presa não morre ao simplesmente encostar
                // (a morte só ocorre quando o Predador avança ativamente sobre ela, ver seção
                // abaixo). Ao encostar, ela é bloqueada e muda de direção, fugindo do contato.
                Predador predadorBloqueador = ObterPredadorEmColisao(presaAtual);
                if (predadorBloqueador != null)
                {
                    presaAtual.X = xAntes;
                    presaAtual.Y = yAntes;

                    float dirFugaX = presaAtual.X - predadorBloqueador.X;
                    float dirFugaY = presaAtual.Y - predadorBloqueador.Y;
                    if (Math.Abs(dirFugaX) < 0.0001f && Math.Abs(dirFugaY) < 0.0001f)
                    {
                        // Sobreposição exata (mesmo centro): usa uma direção aleatória para desempatar.
                        dirFugaX = (float)Math.Cos(presaAtual.Angulo + Math.PI / 2);
                        dirFugaY = (float)Math.Sin(presaAtual.Angulo + Math.PI / 2);
                    }
                    presaAtual.Angulo = CalcularAnguloDeFuga(presaAtual, dirFugaX, dirFugaY);
                }

                Planta plantaComida = ObterPlantaEmColisao(presaAtual);
                if (plantaComida != null)
                {
                    plantaComida.Viva = false;
                    presaAtual.Alimentar(Config);
                }
            }
            else if (agente is Predador predadorAtual)
            {
                // Predação só é válida se o Predador estiver ativamente avançando (ação Frente).
                // Isso impede a estratégia degenerada de "camping" (ficar parado esperando a
                // Presa esbarrar nele): parado, ele não caça, só perde HP normalmente.
                Presa presaComida = !estaParado ? ObterPresaEmColisao(predadorAtual) : null;
                if (presaComida != null && presaComida.Vivo)
                {
                    presaComida.Vivo = false;
                    RegistrarMorte(presaComida);
                    predadorAtual.Alimentar(Config);
                }
                else
                {
                    // 6b. Colisão sólida: Predador não pode entrar em outro Predador
                    // nem atravessar uma Planta (apenas Presas comem plantas).
                    Predador outroPredadorColidido = ObterOutroPredadorEmColisao(predadorAtual);
                    Planta plantaBloqueadora = ObterPlantaEmColisao(predadorAtual);

                    if (outroPredadorColidido != null || plantaBloqueadora != null)
                    {
                        predadorAtual.X = xAntes;
                        predadorAtual.Y = yAntes;

                        // Ao colidir, muda de direção em vez de ficar empurrando o obstáculo.
                        ElementoCenario obstaculo = (ElementoCenario)plantaBloqueadora ?? outroPredadorColidido;
                        float dirFugaX = predadorAtual.X - obstaculo.X;
                        float dirFugaY = predadorAtual.Y - obstaculo.Y;
                        if (Math.Abs(dirFugaX) < 0.0001f && Math.Abs(dirFugaY) < 0.0001f)
                        {
                            // Sobreposição exata (mesmo centro): usa uma direção perpendicular para desempatar.
                            dirFugaX = (float)Math.Cos(predadorAtual.Angulo + Math.PI / 2);
                            dirFugaY = (float)Math.Sin(predadorAtual.Angulo + Math.PI / 2);
                        }
                        predadorAtual.Angulo = CalcularAnguloDeFuga(predadorAtual, dirFugaX, dirFugaY);
                    }
                }
            }

            // 7. Decaimento natural de HP (fome + penalidade de ociosidade)
            if (agente.Vivo)
            {
                agente.AplicarDecaimentoDeHp(Config, estaParado);
                if (agente.EstaMorto)
                {
                    RegistrarMorte(agente);
                }
            }
        }

        // Bloqueia o agente dentro dos limites da arena e, ao colidir com uma parede,
        // muda de direção (evita o agente "grudar" cheirando a parede indefinidamente).
        // Calcula o ângulo de fuga na direção oposta ao obstáculo, somando o viés de ângulo
        // (gene 14) e escalando a variação aleatória pela intensidade de mudança (gene 15) do
        // próprio DNA do agente — cada indivíduo evolui sua própria forma de esquivar.
        private const float VariacaoMaximaAnguloFugaGraus = 20f;

        private float CalcularAnguloDeFuga(AgenteEvolutivo agente, float dirFugaX, float dirFugaY)
        {
            float anguloBase = (float)Math.Atan2(dirFugaY, dirFugaX);
            float variacaoGraus = (float)(sorteador.NextDouble() * 2 - 1) * VariacaoMaximaAnguloFugaGraus;
            float variacaoRad = variacaoGraus * (float)(Math.PI / 180.0) * agente.Cerebro.IntensidadeMudancaDirecao;
            return anguloBase + variacaoRad + agente.Cerebro.ViesAnguloColisaoRad;
        }

        private void ClampNasParedes(AgenteEvolutivo agente)
        {
            float limiteEsquerdo = Config.EspessuraParede;
            float limiteSuperior = Config.EspessuraParede;
            float limiteDireito = Config.LarguraTela - Config.EspessuraParede - agente.Tamanho;
            float limiteInferior = Config.AlturaTela - Config.EspessuraParede - agente.Tamanho;

            float dirFugaX = 0f;
            float dirFugaY = 0f;
            bool colidiuComParede = false;

            if (agente.X < limiteEsquerdo) { agente.X = limiteEsquerdo; dirFugaX += 1f; colidiuComParede = true; }
            if (agente.X > limiteDireito) { agente.X = limiteDireito; dirFugaX -= 1f; colidiuComParede = true; }
            if (agente.Y < limiteSuperior) { agente.Y = limiteSuperior; dirFugaY += 1f; colidiuComParede = true; }
            if (agente.Y > limiteInferior) { agente.Y = limiteInferior; dirFugaY -= 1f; colidiuComParede = true; }

            if (colidiuComParede)
            {
                agente.Angulo = CalcularAnguloDeFuga(agente, dirFugaX, dirFugaY);
            }
        }

        private static bool Sobrepoe(ElementoCenario a, ElementoCenario b)
        {
            return a.X < b.X + b.Tamanho && a.X + a.Tamanho > b.X &&
                   a.Y < b.Y + b.Tamanho && a.Y + a.Tamanho > b.Y;
        }

        private Tijolo ObterTijoloEmColisao(AgenteEvolutivo agente)
        {
            foreach (var tijolo in Tijolos)
            {
                if (!tijolo.Quebrado && Sobrepoe(agente, tijolo)) return tijolo;
            }
            return null;
        }

        private Planta ObterPlantaEmColisao(AgenteEvolutivo agente)
        {
            foreach (var planta in Plantas)
            {
                if (planta.Viva && Sobrepoe(agente, planta)) return planta;
            }
            return null;
        }

        private Presa ObterPresaEmColisao(Predador predador)
        {
            foreach (var presa in Presas)
            {
                if (presa.Vivo && Sobrepoe(predador, presa)) return presa;
            }
            return null;
        }

        // Usado pela Presa: detecta contato sólido com Predador (não causa morte por si só,
        // apenas bloqueia o avanço e força a Presa a mudar de direção).
        private Predador ObterPredadorEmColisao(Presa presa)
        {
            foreach (var predador in Predadores)
            {
                if (predador.Vivo && Sobrepoe(presa, predador)) return predador;
            }
            return null;
        }

        // Predadores são corpos sólidos entre si: nenhum pode ocupar o mesmo espaço de outro.
        private Predador ObterOutroPredadorEmColisao(Predador predador)
        {
            foreach (var outro in Predadores)
            {
                if (outro != predador && outro.Vivo && Sobrepoe(predador, outro)) return outro;
            }
            return null;
        }

        private void AtualizarTijolos()
        {
            foreach (var tijolo in Tijolos)
            {
                tijolo.AtualizarAnimacao();
            }
        }

        private void RemoverTijolosQuebrados()
        {
            // Só remove de fato depois que a sequência de explosão (frames 2-5) terminou
            // de tocar; enquanto isso, o tijolo continua na lista (já sem colisão, pois
            // ObterTijoloEmColisao ignora tijolos Quebrado) só para exibir a animação.
            Tijolos.RemoveAll(t => t.Quebrado && t.AnimacaoExplosaoConcluida);
            Plantas.RemoveAll(p => !p.Viva);
        }

        private void RegistrarMorte(AgenteEvolutivo agente)
        {
            agente.Vivo = false;
            TickUltimaMorte = TickAtual;

            // Se o sprite do agente tiver animação de morte configurada, mantém o agente
            // visível (parado, tocando os frames de morte) por alguns ticks antes de sumir.
            if (SpriteSheets.TryObterInfo(agente.IdSprite, out _))
            {
                efeitosDeMorte.Add(agente);
            }
        }

        // Avança os frames de morte de cada agente recém-falecido e remove os que já
        // terminaram o ciclo de animação, para que parem de aparecer em ObterCenaVisual().
        private void AtualizarEfeitosDeMorte()
        {
            for (int i = efeitosDeMorte.Count - 1; i >= 0; i--)
            {
                if (efeitosDeMorte[i].AtualizarAnimacaoMorte())
                {
                    efeitosDeMorte.RemoveAt(i);
                }
            }
        }

        private bool TodasAsEspeciesExtintas()
        {
            return Presas.All(p => !p.Vivo) && Predadores.All(p => !p.Vivo);
        }

        // Converte um vetor de deslocamento (dx, dy) na direção cardinal predominante
        // (Direita/Baixo/Esquerda/Cima), usada para escolher a linha correta da spritesheet.
        private static DirecaoSprite ObterDirecaoCardinal(float dx, float dy)
        {
            if (Math.Abs(dx) >= Math.Abs(dy))
            {
                return dx >= 0 ? DirecaoSprite.Direita : DirecaoSprite.Esquerda;
            }

            return dy >= 0 ? DirecaoSprite.Baixo : DirecaoSprite.Cima;
        }

        // --------------------------------------------------------
        // EVOLUÇÃO (fim de geração: extinção total do ecossistema)
        // --------------------------------------------------------
        // Disparado ao final de cada geração (extinção total), antes de resetar os contadores.
        // Parâmetros: (Geracao concluída, TickUltimaMorte daquela geração).
        public event Action<int, int> GeracaoConcluida;

        private void EvoluirGeracao()
        {
            GeracaoConcluida?.Invoke(GeracaoAtual, TickUltimaMorte);

            EvoluirEspecie(Presas, TipoEntidade.Presa, dna => new Presa(dna), Config.PopulacaoPresas, out var novasPresas);
            EvoluirEspecie(Predadores, TipoEntidade.Predador, dna => new Predador(dna), Config.PopulacaoPredadores, out var novosPredadores);

            Presas = novasPresas;
            Predadores = novosPredadores;

            Tijolos.Clear();
            Plantas.Clear();

            var posicionados = new List<ElementoCenario>();
            EspalharTijolos(posicionados);
            EspalharPlantas(Config.PopulacaoPlantas, posicionados);

            foreach (var presa in Presas)
            {
                var (px, py) = EncontrarPosicaoLivre(presa.Tamanho, posicionados);
                presa.X = px;
                presa.Y = py;
                posicionados.Add(presa);
            }

            foreach (var predador in Predadores)
            {
                var (px, py) = EncontrarPosicaoLivre(predador.Tamanho, posicionados);
                predador.X = px;
                predador.Y = py;
                posicionados.Add(predador);
            }

            ticksDesdeUltimaPlanta = 0;

            GeracaoAtual++;
            TickAtual = 0;
            TickUltimaMorte = 0;
        }

        private void EvoluirEspecie<T>(List<T> populacaoAtual, TipoEntidade tipo, Func<float[], T> fabrica, int tamanhoPopulacao, out List<T> novaPopulacao)
            where T : AgenteEvolutivo
        {
            // 1. Ranking pelo maior tempo de sobrevivência
            var ranking = populacaoAtual.OrderByDescending(a => a.TempoVivo).ToList();

            // 2. Persiste o desempenho de toda a população desta geração
            var registros = ranking.Select(a => new RegistroEvolucao
            {
                Geracao = GeracaoAtual,
                Tempo_Sobrevivencia = a.TempoVivo,
                Tipo_Entidade = tipo,
                Genoma_DNA = Repositorio_Genoma.SerializarGenoma(a.DNA)
            });
            repositorio.SalvarRegistros(registros);

            // 3. Elite: Top 5
            var elite = ranking.Take(Config.TamanhoElite).ToList();

            // 4. Roleta genética: reprodução assexuada (clone + mutação, sem crossover)
            novaPopulacao = new List<T>(tamanhoPopulacao);
            for (int i = 0; i < tamanhoPopulacao; i++)
            {
                T paiSorteado = elite.Count > 0 ? elite[sorteador.Next(elite.Count)] : populacaoAtual[sorteador.Next(populacaoAtual.Count)];
                float[] dnaFilho = paiSorteado.Cerebro.ClonarEMutarDna(Config.ForcaMutacao);
                T filho = fabrica(dnaFilho);
                filho.ResetarPosicaoECronometro(Config, 0f, 0f); // posição real é definida em EvoluirGeracao, sem sobreposição
                novaPopulacao.Add(filho);
            }
        }

        // --------------------------------------------------------
        // VISUALIZAÇÃO
        // --------------------------------------------------------
        public List<SnapshotVisual> ObterCenaVisual()
        {
            var cena = new List<SnapshotVisual>();

            foreach (var tijolo in Tijolos) cena.Add(tijolo.GerarSnapshot());
            foreach (var planta in Plantas) if (planta.Viva) cena.Add(planta.GerarSnapshot());
            foreach (var presa in Presas) if (presa.Vivo) cena.Add(presa.GerarSnapshot());
            foreach (var predador in Predadores) if (predador.Vivo) cena.Add(predador.GerarSnapshot());

            // Agentes mortos ainda tocando a animação de morte da spritesheet
            // (GerarSnapshot já usa FrameSpriteAtual, atualizado por AtualizarAnimacaoMorte).
            foreach (var agenteMorrendo in efeitosDeMorte)
            {
                cena.Add(agenteMorrendo.GerarSnapshot());
            }

            return cena;
        }
    }
}
