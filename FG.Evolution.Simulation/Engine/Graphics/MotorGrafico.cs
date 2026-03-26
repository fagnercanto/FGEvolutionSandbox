using Raylib_cs;
using System.Collections.Generic;

namespace FG.Evolution.Simulation.Engine.Graphics
{
    public class MotorGrafico
    {
        // O MotorGraphic recebe os pacotes de dados e as informações do HUD
        public void RenderizarCena(List<SnapshotVisual> cena, int geracaoAtual)
        {
            Raylib.BeginDrawing();
            Raylib.ClearBackground(Color.DarkGray);

            // Varredura da lista de pacotes cegos
            foreach (var obj in cena)
            {
                switch (obj.Formato)
                {
                    case FormatoVisual.QuadradoComNariz:
                        // Desenha o corpo principal
                        Raylib.DrawRectangle((int)obj.X, (int)obj.Y, (int)obj.Tamanho, (int)obj.Tamanho, obj.CorBase);
                        Raylib.DrawRectangleLines((int)obj.X, (int)obj.Y, (int)obj.Tamanho, (int)obj.Tamanho, obj.CorBorda);

                        // Calcula e desenha o vetor de direção (Nariz)
                        float centroX = obj.X + obj.Tamanho / 2f;
                        float centroY = obj.Y + obj.Tamanho / 2f;
                        float narizX = centroX + (float)Math.Cos(obj.Angulo) * 25f;
                        float narizY = centroY + (float)Math.Sin(obj.Angulo) * 25f;
                        Raylib.DrawLine((int)centroX, (int)centroY, (int)narizX, (int)narizY, Color.White);
                        break;

                    case FormatoVisual.Quadrado:
                        // Desenha formas estáticas simples
                        Raylib.DrawRectangle((int)obj.X, (int)obj.Y, (int)obj.Tamanho, (int)obj.Tamanho, obj.CorBase);
                        if (obj.CorBorda.A > 0) // Desenha borda apenas se for visível
                        {
                            Raylib.DrawRectangleLines((int)obj.X, (int)obj.Y, (int)obj.Tamanho, (int)obj.Tamanho, obj.CorBorda);
                        }
                        break;

                    case FormatoVisual.Circulo:
                        // Desenha formas circulares
                        Raylib.DrawCircle((int)obj.X, (int)obj.Y, obj.Tamanho / 2f, obj.CorBase);
                        break;
                }
            }

            // Renderização do HUD (Informações sobrepostas)
            Raylib.DrawText($"GERACAO: {geracaoAtual}", 20, 20, 20, Color.RayWhite);

            Raylib.EndDrawing();
        }
    }
}