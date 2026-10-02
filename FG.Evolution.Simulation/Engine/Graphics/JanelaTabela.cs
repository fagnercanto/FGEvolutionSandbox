using System;
using System.Linq;
using System.Windows.Forms;
using FG.Evolution.Simulation.Persistence;

namespace FG.Evolution.Simulation.Engine.Graphics
{
    // Janela simples para visualizar a tabela local (SQLite) de registros evolutivos.
    public class JanelaTabela : Form
    {
        private readonly DataGridView grade;
        private readonly Repositorio_Genoma repositorio;
        private readonly ComboBox comboTipo;
        private readonly Button botaoAtualizar;

        public JanelaTabela(Repositorio_Genoma repositorioGenoma)
        {
            repositorio = repositorioGenoma;

            Text = "Tabela Local - Historico Evolutivo (RegistroEvolucao)";
            Width = 720;
            Height = 500;
            StartPosition = FormStartPosition.Manual;
            Location = new System.Drawing.Point(420, 20);

            var painelTopo = new Panel { Dock = DockStyle.Top, Height = 40 };

            var labelFiltro = new Label { Text = "Filtrar por tipo:", Left = 10, Top = 10, Width = 90 };
            comboTipo = new ComboBox
            {
                Left = 105,
                Top = 7,
                Width = 150,
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            comboTipo.Items.Add("Todos");
            comboTipo.Items.Add(TipoEntidade.Presa.ToString());
            comboTipo.Items.Add(TipoEntidade.Predador.ToString());
            comboTipo.SelectedIndex = 0;
            comboTipo.SelectedIndexChanged += (s, e) => CarregarDados();

            botaoAtualizar = new Button { Text = "Atualizar", Left = 270, Top = 6, Width = 90 };
            botaoAtualizar.Click += (s, e) => CarregarDados();

            painelTopo.Controls.Add(labelFiltro);
            painelTopo.Controls.Add(comboTipo);
            painelTopo.Controls.Add(botaoAtualizar);

            grade = new DataGridView
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect
            };

            Controls.Add(grade);
            Controls.Add(painelTopo);

            Load += (s, e) => CarregarDados();
        }

        private void CarregarDados()
        {
            string filtro = comboTipo.SelectedItem?.ToString() ?? "Todos";

            var registros = repositorio.ObterTodosOrdenadosPorSobrevivencia();

            if (filtro != "Todos" && Enum.TryParse<TipoEntidade>(filtro, out var tipoFiltro))
            {
                registros = registros.Where(r => r.Tipo_Entidade == tipoFiltro).ToList();
            }

            grade.DataSource = registros
                .Select(r => new
                {
                    r.Id,
                    r.Geracao,
                    Tipo = r.Tipo_Entidade.ToString(),
                    r.Tempo_Sobrevivencia,
                    r.CriadoEm
                })
                .ToList();
        }
    }
}
