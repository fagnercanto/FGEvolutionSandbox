using System;
using System.Collections.Generic;
using System.IO;
using Raylib_cs;

namespace FG.Evolution.Simulation.Engine.Graphics
{
    // Carrega e mantém em cache texturas (sprites) usadas pelo MotorGrafico.
    // Caso o arquivo do sprite não exista, o pedido falha silenciosamente
    // (TryObterTextura retorna false) e o MotorGrafico recorre ao desenho vetorial.
    public static class GerenciadorTexturas
    {
        private static readonly Dictionary<string, Texture2D> cache = new();
        private static readonly HashSet<string> falhasConhecidas = new();

        // Pasta onde os sprites devem ficar: Assets/Sprites/{idSprite}.png
        private static readonly string pastaSprites = Path.Combine(AppContext.BaseDirectory, "Assets", "Sprites");

        // Tenta obter (carregando e cacheando na primeira vez) a textura associada ao id.
        // Retorna false se o id for nulo/vazio ou o arquivo não existir/falhar ao carregar.
        public static bool TryObterTextura(string idSprite, out Texture2D textura)
        {
            textura = default;

            if (string.IsNullOrWhiteSpace(idSprite) || falhasConhecidas.Contains(idSprite))
            {
                return false;
            }

            if (cache.TryGetValue(idSprite, out textura))
            {
                return true;
            }

            string caminho = Path.Combine(pastaSprites, idSprite + ".png");

            if (!File.Exists(caminho))
            {
                falhasConhecidas.Add(idSprite);
                return false;
            }

            try
            {
                textura = Raylib.LoadTexture(caminho);

                if (textura.Id == 0)
                {
                    falhasConhecidas.Add(idSprite);
                    return false;
                }

                cache[idSprite] = textura;
                return true;
            }
            catch
            {
                falhasConhecidas.Add(idSprite);
                return false;
            }
        }

        // Libera todas as texturas carregadas. Deve ser chamado antes de Raylib.CloseWindow().
        public static void DescarregarTudo()
        {
            foreach (var textura in cache.Values)
            {
                Raylib.UnloadTexture(textura);
            }

            cache.Clear();
            falhasConhecidas.Clear();
        }
    }
}
