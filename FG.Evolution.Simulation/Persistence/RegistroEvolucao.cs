using System;

namespace FG.Evolution.Simulation.Persistence
{
    // Tipos de entidade que podem gerar um registro evolutivo no banco.
    public enum TipoEntidade
    {
        Presa,
        Predador
    }

    // Uma "linha" da tabela de histórico evolutivo: representa o desempenho
    // de um indivíduo (Presa ou Predador) em uma geração específica.
    public class RegistroEvolucao
    {
        public int Id { get; set; }

        public int Geracao { get; set; }

        // Tempo (em ticks) que o indivíduo sobreviveu antes de morrer,
        // ou o tempo total da geração caso ele tenha sobrevivido até o fim.
        public int Tempo_Sobrevivencia { get; set; }

        public TipoEntidade Tipo_Entidade { get; set; }

        // DNA serializado em JSON (array de floats).
        public string Genoma_DNA { get; set; }

        public DateTime CriadoEm { get; set; } = DateTime.UtcNow;
    }
}
