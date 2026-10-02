using FG.Evolution.Simulation.Config;
using FG.Evolution.Simulation.Persistence;
using FG.Evolution.Simulation.Simulation.World;
using System;
using System.Collections.Generic;

namespace FG.Evolution.Simulation.Simulation.Headless
{
    // Resultado de uma geração executada em modo Headless: útil para calibração
    // (média/máximo de tempo de sobrevivência) e para decidir se um Timeout é necessário.
    public class ResultadoGeracao
    {
        public int Geracao { get; set; }
        public int TickUltimaMorte { get; set; }
    }

    // Roda a simulação sem renderização gráfica (sem Raylib), processando exclusivamente
    // na CPU. Usado para validar empiricamente por quantos ticks o ecossistema sobrevive
    // antes de colapsar por inanição ou predação, sem impor um limite de tempo artificial.
    public class SimuladorHeadless
    {
        private readonly Configuracao config;
        private readonly Repositorio_Genoma repositorio;

        // Trava de segurança técnica (não é uma regra de jogo): evita loop infinito caso,
        // por alguma combinação improvável de DNA, uma geração nunca entre em extinção total.
        public long LimiteDeSegurancaTicks { get; set; } = 2_000_000;

        public SimuladorHeadless(Configuracao config, Repositorio_Genoma repositorioGenoma = null)
        {
            this.config = config;
            repositorio = repositorioGenoma ?? new Repositorio_Genoma();
        }

        // Executa 'quantidadeGeracoes' gerações consecutivas e retorna o tempo (em ticks)
        // da última morte registrada em cada uma delas.
        public List<ResultadoGeracao> RodarGeracoes(int quantidadeGeracoes)
        {
            var resultados = new List<ResultadoGeracao>(quantidadeGeracoes);
            var arena = new Arena(config, repositorio);

            arena.GeracaoConcluida += (geracao, tickUltimaMorte) =>
            {
                resultados.Add(new ResultadoGeracao { Geracao = geracao, TickUltimaMorte = tickUltimaMorte });
                Console.WriteLine($"[Headless] Geração {geracao} concluída. Última morte no tick {tickUltimaMorte}.");
            };

            long ticksProcessados = 0;
            while (resultados.Count < quantidadeGeracoes)
            {
                arena.AtualizarMundo();
                ticksProcessados++;

                if (ticksProcessados >= LimiteDeSegurancaTicks)
                {
                    Console.WriteLine($"[Aviso] Limite de segurança de {LimiteDeSegurancaTicks} ticks atingido antes da geração {resultados.Count + 1} entrar em extinção total. Interrompendo.");
                    break;
                }
            }

            return resultados;
        }
    }
}
