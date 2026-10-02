using Microsoft.EntityFrameworkCore;

namespace FG.Evolution.Simulation.Persistence
{
    // DbContext local em SQLite para armazenar o histórico evolutivo
    // (Geracao, Tempo_Sobrevivencia, Tipo_Entidade, Genoma_DNA) de Presas e Predadores.
    public class Contexto_Banco_Local : DbContext
    {
        private const string ArquivoBanco = "evolucao.db";

        public DbSet<RegistroEvolucao> Registros { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                optionsBuilder.UseSqlite($"Data Source={ArquivoBanco}");
            }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<RegistroEvolucao>(entidade =>
            {
                entidade.ToTable("RegistrosEvolucao");
                entidade.HasKey(r => r.Id);
                entidade.Property(r => r.Tipo_Entidade).HasConversion<string>();
                entidade.HasIndex(r => new { r.Tipo_Entidade, r.Tempo_Sobrevivencia });
            });
        }
    }
}
