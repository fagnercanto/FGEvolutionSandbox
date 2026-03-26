using System.IO;
using System.Text.Json;

namespace FG.Evolution.Simulation.Config
{
    // A maleta de regras continua burra, mas agora sabe se salvar e se ler do disco.
    public class Configuracao
    {
        // Visual
        public int LarguraTela { get; set; } = 800;
        public int AlturaTela { get; set; } = 600;
        public int FPS { get; set; } = 60;

        // Regras do Mundo
        public int EspessuraParede { get; set; } = 10;
        public int TamanhoMinion { get; set; } = 20;

        // DNA e População
        public int TamanhoPopulacao { get; set; } = 35;
        public int TempoMaximoGeracao { get; set; } = 300;
        public int TamanhoDNA { get; set; } = 12;
        public float ForcaMutacao { get; set; } = 0.1f;

        // --- O NOVO GERENCIADOR DE ARQUIVO ---
        public static Configuracao Carregar()
        {
            string caminho = "config.json";

            // 1. Se o arquivo não existir, cria um arquivo novo bonitinho
            if (!File.Exists(caminho))
            {
                Configuracao configPadrao = new Configuracao();

                // WriteIndented = true deixa o JSON formatado com quebras de linha para humanos lerem
                var opcoes = new JsonSerializerOptions { WriteIndented = true };
                string jsonNovo = JsonSerializer.Serialize(configPadrao, opcoes);

                File.WriteAllText(caminho, jsonNovo);
                return configPadrao;
            }

            // 2. Se o arquivo já existe, lê o texto e transforma na nossa maleta
            string jsonLido = File.ReadAllText(caminho);
            return JsonSerializer.Deserialize<Configuracao>(jsonLido);
        }
    }
}