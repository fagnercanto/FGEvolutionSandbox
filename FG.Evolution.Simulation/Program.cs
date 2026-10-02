using FG.Evolution.Simulation.Config;
using FG.Evolution.Simulation.Engine.Graphics;
using FG.Evolution.Simulation.Persistence;
using FG.Evolution.Simulation.Simulation.World;
using Raylib_cs;
using System.Linq;
using System.Threading;
using System.Windows.Forms;

namespace FG.Evolution.Simulation
{
    class Program
    {
        static void Main(string[] args)
        {
            // Seleção de modo via argumento de linha de comando:
            //   (sem argumentos)      -> Modo Gráfico (visualização, Raylib)
            //   headless [geracoes]   -> Modo Headless (calibração, sem renderização)
            string modo = args.Length > 0 ? args[0].ToLowerInvariant() : "grafico";

            if (modo == "headless")
            {
                int quantidadeGeracoes = 100;
                if (args.Length > 1 && int.TryParse(args[1], out int valorInformado))
                {
                    quantidadeGeracoes = valorInformado;
                }

                Calibrador_Geracao_Headless.Executar(quantidadeGeracoes);
                return;
            }

            RodarModoGrafico();
        }

        // Modo Gráfico / Modo Visualização: renderiza a simulação em tempo real via Raylib.
        // A população inicial é carregada automaticamente a partir do Top 5 de cada espécie
        // salvo no banco (Repositorio_Genoma), permitindo observar o comportamento dos
        // melhores genomas já evoluídos. Em paralelo, uma segunda janela (WinForms) permite
        // editar parâmetros da simulação em tempo real e acompanha geração/legenda/contagens.
        private static void RodarModoGrafico()
        {
            Configuracao maletaConfig = Configuracao.Carregar();
            Repositorio_Genoma repositorio = new Repositorio_Genoma();
            repositorio.GarantirBancoCriado();

            // Fila simples (com lock) para receber pedidos de atualização vindos da thread do WinForms.
            ParametrosArena pedidoAtualizacao = null;
            object trava = new object();

            PainelControle painel = null;
            var threadPainel = new Thread(() =>
            {
                painel = new PainelControle(
                    maletaConfig.LarguraTela,
                    maletaConfig.AlturaTela,
                    maletaConfig.PopulacaoTijolos,
                    maletaConfig.PopulacaoPlantas,
                    maletaConfig.PopulacaoPredadores,
                    maletaConfig.PopulacaoPresas,
                    maletaConfig.TamanhoElite,
                    maletaConfig.FPS,
                    maletaConfig.TempoMaximoGeracao);

                painel.AtualizarSolicitado += novosParametros =>
                {
                    lock (trava)
                    {
                        pedidoAtualizacao = novosParametros;
                    }
                };

                painel.VerTabelaSolicitado += () =>
                {
                    var janelaTabela = new JanelaTabela(repositorio);
                    janelaTabela.Show();
                };

                Application.EnableVisualStyles();
                Application.Run(painel);
            });
            threadPainel.SetApartmentState(ApartmentState.STA);
            threadPainel.IsBackground = true;
            threadPainel.Start();

            Raylib.InitWindow(maletaConfig.LarguraTela, maletaConfig.AlturaTela, "FG Sandbox Engine - Predador vs Presa");
            Raylib.SetTargetFPS(maletaConfig.FPS);

            Arena arena = new Arena(maletaConfig, repositorio);
            MotorGrafico motor = new MotorGrafico();

            while (!Raylib.WindowShouldClose())
            {
                ParametrosArena parametrosRecebidos = null;
                lock (trava)
                {
                    if (pedidoAtualizacao != null)
                    {
                        parametrosRecebidos = pedidoAtualizacao;
                        pedidoAtualizacao = null;
                    }
                }

                if (parametrosRecebidos != null)
                {
                    maletaConfig.LarguraTela = parametrosRecebidos.LarguraTela;
                    maletaConfig.AlturaTela = parametrosRecebidos.AlturaTela;
                    maletaConfig.PopulacaoTijolos = parametrosRecebidos.PopulacaoTijolos;
                    maletaConfig.PopulacaoPlantas = parametrosRecebidos.PopulacaoPlantas;
                    maletaConfig.PopulacaoPredadores = parametrosRecebidos.PopulacaoPredadores;
                    maletaConfig.PopulacaoPresas = parametrosRecebidos.PopulacaoPresas;
                    maletaConfig.FPS = parametrosRecebidos.FPS;

                    Raylib.SetWindowSize(maletaConfig.LarguraTela, maletaConfig.AlturaTela);
                    Raylib.SetTargetFPS(maletaConfig.FPS);
                    arena = new Arena(maletaConfig, repositorio);
                }

                arena.AtualizarMundo();
                var cenaVisual = arena.ObterCenaVisual();
                motor.RenderizarCena(cenaVisual);

                int totalPresas = arena.Presas.Count(p => p.Vivo);
                int totalPredadores = arena.Predadores.Count(p => p.Vivo);
                painel?.AtualizarStatus(arena.GeracaoAtual, totalPresas, totalPredadores);
            }

            GerenciadorTexturas.DescarregarTudo();
            Raylib.CloseWindow();

            if (painel != null)
            {
                painel.BeginInvoke(new System.Action(() => painel.Close()));
            }
        }
    }
}

