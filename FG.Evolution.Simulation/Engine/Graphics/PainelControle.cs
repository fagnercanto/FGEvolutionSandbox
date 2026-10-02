using System;
using System.Drawing;
using System.Windows.Forms;

namespace FG.Evolution.Simulation.Engine.Graphics
{
    // Janela paralela (WinForms) que roda em sua própria thread com STA, permitindo
    // ao usuário "brincar" com os parâmetros da simulação em tempo real, além de
    // acompanhar geração atual, legenda e contagem de presas/predadores vivos.
    // Layout responsivo: usa TableLayoutPanel/FlowLayoutPanel com Dock/Anchor, então
    // a janela pode ser redimensionada arrastando as bordas sem cortar texto.
    public class PainelControle : Form
    {
        private readonly NumericUpDown campoLargura;
        private readonly NumericUpDown campoAltura;
        private readonly NumericUpDown campoTijolos;
        private readonly NumericUpDown campoPlantas;
        private readonly NumericUpDown campoPredadores;
        private readonly NumericUpDown campoPresas;
        private readonly NumericUpDown campoFPS;
        private readonly Button botaoAtualizar;

        private readonly Label labelGeracao;
        private readonly Label labelPresasVivas;
        private readonly Label labelPredadoresVivos;
        private readonly Label labelElite;
        private readonly Label labelTempoEstimado;
        private readonly Button botaoVerTabela;

        private readonly int tempoMaximoGeracaoTicks;

        // Evento disparado quando o usuário clica em Atualizar/Enter, entregando os
        // novos parâmetros desejados para a Arena ser reconstruída.
        public event Action<ParametrosArena> AtualizarSolicitado;

        // Evento disparado quando o usuário clica em "Ver Tabela do Banco".
        public event Action VerTabelaSolicitado;

        public PainelControle(int larguraInicial, int alturaInicial, int tijolosInicial, int plantasInicial, int predadoresInicial, int presasInicial, int tamanhoElite, int fpsInicial, int tempoMaximoGeracaoTicks)
        {
            this.tempoMaximoGeracaoTicks = tempoMaximoGeracaoTicks;

            Text = "Painel de Controle - FG Evolution Simulation";
            MinimumSize = new Size(380, 480);
            Size = new Size(420, 700);
            FormBorderStyle = FormBorderStyle.Sizable; // Permite arrastar as bordas para redimensionar
            MaximizeBox = true;
            StartPosition = FormStartPosition.Manual;
            Location = new Point(20, 20);
            AutoScaleMode = AutoScaleMode.Font;

            var layoutRaiz = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                AutoScroll = true,
                Padding = new Padding(12)
            };
            layoutRaiz.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            Controls.Add(layoutRaiz);

            // --- Seção: Parâmetros editáveis ---
            var grupoParametros = new GroupBox { Text = "Parâmetros da Simulação", Dock = DockStyle.Top, AutoSize = true };
            var tabelaParametros = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                ColumnCount = 2,
                AutoSize = true,
                Padding = new Padding(8)
            };
            tabelaParametros.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 65f));
            tabelaParametros.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 35f));

            campoLargura = AdicionarCampoNumerico(tabelaParametros, "Tamanho da Arena - Largura:", larguraInicial, 200, 4000);
            campoAltura = AdicionarCampoNumerico(tabelaParametros, "Tamanho da Arena - Altura:", alturaInicial, 200, 4000);
            campoTijolos = AdicionarCampoNumerico(tabelaParametros, "Quantidade de Tijolos:", tijolosInicial, 0, 200);
            campoPlantas = AdicionarCampoNumerico(tabelaParametros, "Quantidade de Plantas iniciais:", plantasInicial, 0, 200);
            campoPredadores = AdicionarCampoNumerico(tabelaParametros, "Quantidade de Predadores:", predadoresInicial, 1, 200);
            campoPresas = AdicionarCampoNumerico(tabelaParametros, "Quantidade de Presas:", presasInicial, 1, 200);
            campoFPS = AdicionarCampoNumerico(tabelaParametros, "FPS (velocidade de exibição):", fpsInicial, 1, 240);
            campoFPS.ValueChanged += (s, e) => AtualizarLabelTempoEstimado();

            grupoParametros.Controls.Add(tabelaParametros);
            layoutRaiz.Controls.Add(grupoParametros);

            botaoAtualizar = new Button
            {
                Text = "Atualizar (Enter)",
                Dock = DockStyle.Top,
                Height = 34,
                Margin = new Padding(0, 8, 0, 8)
            };
            botaoAtualizar.Click += (s, e) => DispararAtualizacao();
            layoutRaiz.Controls.Add(botaoAtualizar);
            AcceptButton = botaoAtualizar;

            // --- Seção: Status ao vivo ---
            var grupoStatus = new GroupBox { Text = "Status ao Vivo", Dock = DockStyle.Top, AutoSize = true };
            var painelStatus = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                FlowDirection = FlowDirection.TopDown,
                AutoSize = true,
                WrapContents = false,
                Padding = new Padding(8)
            };

            labelGeracao = CriarLabelInfo(painelStatus, "GERACAO: 1");
            labelPresasVivas = CriarLabelInfo(painelStatus, "Presas vivas: 0");
            labelPredadoresVivos = CriarLabelInfo(painelStatus, "Predadores vivos: 0");
            labelElite = CriarLabelInfo(painelStatus, $"Genomas guardados (elite) por espécie: {tamanhoElite}");
            labelTempoEstimado = CriarLabelInfo(painelStatus, "Tempo estimado por geração: --");

            grupoStatus.Controls.Add(painelStatus);
            layoutRaiz.Controls.Add(grupoStatus);
            AtualizarLabelTempoEstimado();

            // --- Seção: Legenda ---
            var grupoLegenda = new GroupBox { Text = "Legenda", Dock = DockStyle.Top, AutoSize = true };
            var painelLegenda = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                FlowDirection = FlowDirection.TopDown,
                AutoSize = true,
                WrapContents = false,
                Padding = new Padding(8)
            };

            CriarLabelLegenda(painelLegenda, "Quadrados azuis com \"nariz\" = Presas");
            CriarLabelLegenda(painelLegenda, "Quadrados vermelhos = Predadores");
            CriarLabelLegenda(painelLegenda, "Número amarelo acima da entidade = nível de HP (1 a 10)");
            CriarLabelLegenda(painelLegenda, "Quadrados marrons = Tijolos (obstáculos empurráveis)");
            CriarLabelLegenda(painelLegenda, "Círculos verdes = Plantas (comida das presas)");

            grupoLegenda.Controls.Add(painelLegenda);
            layoutRaiz.Controls.Add(grupoLegenda);

            // --- Ação: Ver tabela do banco ---
            botaoVerTabela = new Button
            {
                Text = "Ver Tabela do Banco (SQLite)",
                Dock = DockStyle.Top,
                Height = 34,
                Margin = new Padding(0, 8, 0, 0)
            };
            botaoVerTabela.Click += (s, e) => VerTabelaSolicitado?.Invoke();
            layoutRaiz.Controls.Add(botaoVerTabela);
        }

        private NumericUpDown AdicionarCampoNumerico(TableLayoutPanel tabela, string rotulo, int valorInicial, int minimo, int maximo)
        {
            var label = new Label
            {
                Text = rotulo,
                AutoSize = true,
                Anchor = AnchorStyles.Left,
                Margin = new Padding(0, 6, 6, 0)
            };

            var campo = new NumericUpDown
            {
                Minimum = minimo,
                Maximum = maximo,
                Value = Math.Clamp(valorInicial, minimo, maximo),
                Anchor = AnchorStyles.Left | AnchorStyles.Right,
                Width = 90,
                Margin = new Padding(0, 3, 0, 3)
            };

            int linha = tabela.RowCount;
            tabela.RowCount = linha + 1;
            tabela.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            tabela.Controls.Add(label, 0, linha);
            tabela.Controls.Add(campo, 1, linha);

            return campo;
        }

        private Label CriarLabelInfo(Control painel, string textoInicial)
        {
            var label = new Label
            {
                Text = textoInicial,
                AutoSize = true,
                Font = new Font(Font, FontStyle.Bold),
                Margin = new Padding(0, 2, 0, 2)
            };
            painel.Controls.Add(label);
            return label;
        }

        private void CriarLabelLegenda(Control painel, string texto)
        {
            var label = new Label
            {
                Text = texto,
                AutoSize = true,
                Margin = new Padding(0, 2, 0, 2)
            };
            painel.Controls.Add(label);
        }

        private void DispararAtualizacao()
        {
            var parametros = new ParametrosArena
            {
                LarguraTela = (int)campoLargura.Value,
                AlturaTela = (int)campoAltura.Value,
                PopulacaoTijolos = (int)campoTijolos.Value,
                PopulacaoPlantas = (int)campoPlantas.Value,
                PopulacaoPredadores = (int)campoPredadores.Value,
                PopulacaoPresas = (int)campoPresas.Value,
                FPS = (int)campoFPS.Value
            };

            AtualizarSolicitado?.Invoke(parametros);
        }

        // Recalcula o tempo estimado (em segundos) de uma geração com base no FPS
        // configurado no campo, sem esperar o botão Atualizar ser clicado.
        private void AtualizarLabelTempoEstimado()
        {
            int fps = (int)campoFPS.Value;
            double segundos = fps > 0 ? (double)tempoMaximoGeracaoTicks / fps : 0;
            labelTempoEstimado.Text = $"Tempo estimado por geração: ~{segundos:0.0}s ({tempoMaximoGeracaoTicks} ticks a {fps} FPS)";
        }

        // Chamado pelo loop principal (via Invoke, thread-safe) para atualizar o HUD.
        public void AtualizarStatus(int geracaoAtual, int presasVivas, int predadoresVivos)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new Action(() => AtualizarStatus(geracaoAtual, presasVivas, predadoresVivos)));
                return;
            }

            labelGeracao.Text = $"GERACAO: {geracaoAtual}";
            labelPresasVivas.Text = $"Presas vivas: {presasVivas}";
            labelPredadoresVivos.Text = $"Predadores vivos: {predadoresVivos}";
        }
    }
}
