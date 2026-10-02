using FG.Evolution.Simulation.Config;
using FG.Evolution.Simulation.Persistence;
using FG.Evolution.Simulation.Simulation.Headless;
using System;
using System.Linq;

namespace FG.Evolution.Simulation
{
    // Script de referência para calibração científica: roda N gerações em modo Headless
    // (sem renderização) e calcula estatísticas de sobrevivência (Tempo_Sobrevivencia),
    // usadas para decidir se um Timeout é necessário na arena final.
    // Pode ser executado via pipeline de CI ou chamado a partir de um teste unitário.
    public static class Calibrador_Geracao_Headless
    {
        public static void Executar(int quantidadeGeracoes = 100)
        {
            Configuracao config = Configuracao.Carregar();
            var repositorio = new Repositorio_Genoma();
            var simulador = new SimuladorHeadless(config, repositorio);

            Console.WriteLine($"[Calibrador] Iniciando {quantidadeGeracoes} gerações em modo Headless...");

            var resultados = simulador.RodarGeracoes(quantidadeGeracoes);

            if (resultados.Count == 0)
            {
                Console.WriteLine("[Calibrador] Nenhuma geração foi concluída (limite de segurança atingido antes da extinção total).");
                return;
            }

            double media = resultados.Average(r => r.TickUltimaMorte);
            int maximo = resultados.Max(r => r.TickUltimaMorte);
            int minimo = resultados.Min(r => r.TickUltimaMorte);

            Console.WriteLine();
            Console.WriteLine("========== RESULTADO DA CALIBRAÇÃO ==========");
            Console.WriteLine($"Gerações concluídas: {resultados.Count} de {quantidadeGeracoes}");
            Console.WriteLine($"Tempo de última morte - Média:  {media:F2} ticks");
            Console.WriteLine($"Tempo de última morte - Máximo: {maximo} ticks");
            Console.WriteLine($"Tempo de última morte - Mínimo: {minimo} ticks");
            Console.WriteLine("==============================================");
            Console.WriteLine("Use o valor Máximo (com margem de segurança) como referência para um eventual Timeout na arena final.");
        }
    }
}
