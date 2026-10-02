using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;

namespace FG.Evolution.Simulation.Persistence
{
    // Ponto único de acesso ao histórico evolutivo salvo em SQLite.
    // Responsável por: garantir a criação do banco, salvar registros de desempenho
    // por geração e ranquear/recuperar genomas para reprodução ou visualização.
    public class Repositorio_Genoma
    {
        // Garante que o arquivo/esquema do banco exista antes do primeiro uso.
        // Chame uma vez no início da aplicação (headless ou gráfica).
        public void GarantirBancoCriado()
        {
            try
            {
                using var contexto = new Contexto_Banco_Local();
                contexto.Database.EnsureCreated();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Aviso] Falha ao inicializar o banco de dados: {ex.Message}. Persistência ficará indisponível nesta execução.");
            }
        }

        // Serializa um array de DNA para JSON, pronto para ser salvo em Genoma_DNA.
        public static string SerializarGenoma(float[] dna) => JsonSerializer.Serialize(dna);

        // Desserializa o JSON salvo de volta para um array de DNA.
        public static float[] DeserializarGenoma(string genomaJson) => JsonSerializer.Deserialize<float[]>(genomaJson);

        // Salva um lote de registros (ex: toda a população de uma geração) de uma só vez.
        public void SalvarRegistros(IEnumerable<RegistroEvolucao> registros)
        {
            try
            {
                using var contexto = new Contexto_Banco_Local();
                contexto.Registros.AddRange(registros);
                contexto.SaveChanges();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Aviso] Falha ao salvar registros evolutivos: {ex.Message}. Progresso desta geração não foi persistido no banco.");
            }
        }

        public void SalvarRegistro(RegistroEvolucao registro)
        {
            SalvarRegistros(new[] { registro });
        }

        // Retorna o Top N (por padrão 5) registros de uma espécie, ranqueados pelo
        // maior Tempo_Sobrevivencia já registrado no histórico (considerando todas as gerações).
        public List<RegistroEvolucao> ObterTopRanking(TipoEntidade tipo, int quantidade = 5)
        {
            try
            {
                using var contexto = new Contexto_Banco_Local();
                return contexto.Registros
                    .Where(r => r.Tipo_Entidade == tipo)
                    .OrderByDescending(r => r.Tempo_Sobrevivencia)
                    .Take(quantidade)
                    .ToList();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Aviso] Falha ao ler ranking de '{tipo}': {ex.Message}. Retornando lista vazia.");
                return new List<RegistroEvolucao>();
            }
        }

        // Recupera um registro específico pelo Id, para uso no Modo Visualização
        // (carregar um genoma específico na arena para análise assistida pelo usuário).
        public RegistroEvolucao ObterPorId(int id)
        {
            try
            {
                using var contexto = new Contexto_Banco_Local();
                return contexto.Registros.FirstOrDefault(r => r.Id == id);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Aviso] Falha ao buscar registro Id={id}: {ex.Message}.");
                return null;
            }
        }

        // Lista os melhores registros de todas as gerações, úteis para popular
        // uma tela de seleção no Modo Visualização.
        public List<RegistroEvolucao> ListarMelhores(TipoEntidade tipo, int quantidade = 20)
        {
            return ObterTopRanking(tipo, quantidade);
        }

        // Retorna toda a tabela de histórico evolutivo, ordenada pelo maior tempo de
        // sobrevivência. Usado pela janela de visualização da tabela local (SQLite).
        public List<RegistroEvolucao> ObterTodosOrdenadosPorSobrevivencia()
        {
            try
            {
                using var contexto = new Contexto_Banco_Local();
                return contexto.Registros
                    .OrderByDescending(r => r.Tempo_Sobrevivencia)
                    .ToList();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Aviso] Falha ao ler a tabela de histórico evolutivo: {ex.Message}. Retornando lista vazia.");
                return new List<RegistroEvolucao>();
            }
        }
    }
}
