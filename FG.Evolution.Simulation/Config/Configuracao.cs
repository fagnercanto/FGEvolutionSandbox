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

        // DNA e População (legado - Minion/Comida, mantido para compatibilidade)
        public int TamanhoPopulacao { get; set; } = 35;
        // Limite (em ticks) de duração de uma geração. Ao ser atingido, a geração é
        // encerrada por Timeout (mesmo sem extinção total) e a evolução prossegue normalmente.
        public int TempoMaximoGeracao { get; set; } = 3600;
        public float ForcaMutacao { get; set; } = 0.1f;

        // --- NOVO ECOSSISTEMA: DNA (Rede Neural com 13 Genes) ---
        // Índices 0-2: Girar Esquerda | 3-5: Frente | 6-8: Girar Direita (Ataque) | 9-11: Fugir | 12: Inércia
        public int TamanhoDNA { get; set; } = 15;

        // --- NOVO ECOSSISTEMA: Populações Iniciais ---
        public int PopulacaoPresas { get; set; } = 20;
        public int PopulacaoPredadores { get; set; } = 20;
        public int PopulacaoPlantas { get; set; } = 20;
        public int PopulacaoTijolos { get; set; } = 20;

        // --- NOVO ECOSSISTEMA: Tamanhos (em pixels) ---
        public float TamanhoTijolo { get; set; } = 20f;
        public float TamanhoPlanta { get; set; } = 10f;
        public float TamanhoPresa { get; set; } = 10f;
        public float TamanhoPredador { get; set; } = 15f;

        // --- NOVO ECOSSISTEMA: HP e Decaimento (em percentual, 0 a 100) ---
        public float HpInicialPercentual { get; set; } = 50f;
        public float HpMaximoPercentual { get; set; } = 100f;
        public float DecaimentoHpPorTick { get; set; } = 0.055f;
        public float PenalidadeOciosidadePorTick { get; set; } = 0.11f;
        // Predador parado perde vida mais rápido que a Presa: sendo o caçador, ficar imóvel
        // não deveria ser uma estratégia viável (evita "campers" esperando a Presa vir até ele).
        public float PenalidadeOciosidadePredadorPorTick { get; set; } = 0.25f;
        public float DanoEmpurraoPercentual { get; set; } = 10f;

        // --- NOVO ECOSSISTEMA: Regras de Tijolo ---
        public float DeslocamentoTijoloPx { get; set; } = 20f;
        public int ImpactosParaQuebrarTijolo { get; set; } = 3;

        // --- NOVO ECOSSISTEMA: Física de Movimento ---
        public float VelocidadeMovimento { get; set; } = 5.0f;
        public float VelocidadeRotacao { get; set; } = 0.15f;
        // Fração da VelocidadeMovimento aplicada ao girar (0 a 1). Faz o agente avançar em
        // curva ao virar (como um carro), evitando o efeito de "girar parado no lugar".
        public float FatorVelocidadeAoGirar { get; set; } = 0.5f;

        // --- NOVO ECOSSISTEMA: Regras de Planta ---
        public int TicksParaNovaPlanta { get; set; } = 600; // 10s a 60 ticks/s

        // --- NOVO ECOSSISTEMA: Evolução ---
        public int TamanhoElite { get; set; } = 5;
        public int GeracoesCalibracao { get; set; } = 100;

        // --- O NOVO GERENCIADOR DE ARQUIVO ---
        public static Configuracao Carregar()
        {
            string caminho = "config.json";

            try
            {
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
                Configuracao configLida = JsonSerializer.Deserialize<Configuracao>(jsonLido);

                // Arquivo vazio ou "null" gera Deserialize == null. Cai para os defaults nesse caso.
                return configLida ?? new Configuracao();
            }
            catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException || ex is JsonException)
            {
                Console.WriteLine($"[Aviso] Falha ao carregar '{caminho}': {ex.Message}. Usando configuração padrão.");
                return new Configuracao();
            }
        }
    }
}