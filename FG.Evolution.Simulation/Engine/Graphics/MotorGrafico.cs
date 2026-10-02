using Raylib_cs;
using System.Collections.Generic;

namespace FG.Evolution.Simulation.Engine.Graphics
{
    public class MotorGrafico
    {
        // Fator de escala aplicado às sprites animadas (spritesheet) para deixá-las com o
        // dobro do tamanho visual, já que a hitbox lógica (Tamanho/colisão) permanece inalterada.
        private const float FatorEscalaSpritesAnimados = 2f;

        // O MotorGraphic recebe os pacotes de dados e as informações do HUD.
        // Legenda e contadores foram movidos para o Painel de Controle (janela paralela).
        public void RenderizarCena(List<SnapshotVisual> cena)
        {
            Raylib.BeginDrawing();
            Raylib.ClearBackground(Color.DarkGray);

            // Varredura da lista de pacotes cegos
            foreach (var obj in cena)
            {
                bool temSprite = GerenciadorTexturas.TryObterTextura(obj.IdSprite, out Texture2D textura);

                switch (obj.Formato)
                {
                    case FormatoVisual.QuadradoComNariz:
                        float centroX = obj.X + obj.Tamanho / 2f;
                        float centroY = obj.Y + obj.Tamanho / 2f;
                        bool temAnimacao = temSprite && obj.FrameSprite >= 0;

                        if (temAnimacao && SpriteSheets.TryObterInfo(obj.IdSprite, out var infoAnimacao))
                        {
                            // Spritesheet com frames "achatados" (ex: roach.png): converte o
                            // índice flat em linha/coluna e recorta o frame certo da textura.
                            int linha = obj.FrameSprite / infoAnimacao.Colunas;
                            int coluna = obj.FrameSprite % infoAnimacao.Colunas;

                            var fonteFrame = new Rectangle(
                                coluna * infoAnimacao.LarguraFrame,
                                linha * infoAnimacao.AlturaFrame,
                                infoAnimacao.LarguraFrame,
                                infoAnimacao.AlturaFrame);

                            DesenharFrame(textura, fonteFrame, centroX, centroY, obj.Tamanho * FatorEscalaSpritesAnimados);
                        }
                        else if (temSprite)
                        {
                            // Sprite já representa a orientação da entidade: rotaciona conforme o Ângulo.
                            // Offset de -90° porque a arte é desenhada "olhando para cima" (0° = topo),
                            // enquanto obj.Angulo = 0 aponta para a direita (convenção matemática).
                            float anguloGraus = obj.Angulo * (180f / (float)Math.PI) + 90f;
                            DesenharSpriteRotacionado(textura, centroX, centroY, obj.Tamanho, anguloGraus);
                        }
                        else
                        {
                            // Fallback vetorial: corpo + linha indicando a direção (Nariz)
                            Raylib.DrawRectangle((int)obj.X, (int)obj.Y, (int)obj.Tamanho, (int)obj.Tamanho, obj.CorBase);
                            Raylib.DrawRectangleLines((int)obj.X, (int)obj.Y, (int)obj.Tamanho, (int)obj.Tamanho, obj.CorBorda);

                            float narizX = centroX + (float)Math.Cos(obj.Angulo) * 25f;
                            float narizY = centroY + (float)Math.Sin(obj.Angulo) * 25f;
                            Raylib.DrawLine((int)centroX, (int)centroY, (int)narizX, (int)narizY, Color.White);
                        }

                        // Nível de HP (1 a 10) exibido ao vivo no centro da entidade (com ou sem sprite).
                        // TEMPORARIAMENTE DESATIVADO para dar espaço à sprite animada (visualmente maior).
                        /*
                        if (obj.NivelHp.HasValue)
                        {
                            string texto = obj.NivelHp.Value.ToString();
                            const int tamanhoFonte = 10;
                            int larguraTexto = Raylib.MeasureText(texto, tamanhoFonte);
                            int textoX = (int)(centroX - larguraTexto / 2f);
                            int textoY = (int)(centroY - tamanhoFonte / 2f);
                            Raylib.DrawText(texto, textoX, textoY, tamanhoFonte, Color.White);
                        }
                        */
                        break;

                    case FormatoVisual.Quadrado:
                        if (temSprite && obj.FrameSprite >= 0 && SpriteSheets.TryObterInfo(obj.IdSprite, out var infoAnimacaoQuadrado))
                        {
                            // Spritesheet plana (ex: tijolo.png), sem direção/rotação: recorta o
                            // frame certo (normal/golpeado/explosão) a partir do índice flat.
                            int linhaQuad = obj.FrameSprite / infoAnimacaoQuadrado.Colunas;
                            int colunaQuad = obj.FrameSprite % infoAnimacaoQuadrado.Colunas;

                            var fonteFrameQuad = new Rectangle(
                                colunaQuad * infoAnimacaoQuadrado.LarguraFrame,
                                linhaQuad * infoAnimacaoQuadrado.AlturaFrame,
                                infoAnimacaoQuadrado.LarguraFrame,
                                infoAnimacaoQuadrado.AlturaFrame);

                            DesenharFrame(textura, fonteFrameQuad, obj.X + obj.Tamanho / 2f, obj.Y + obj.Tamanho / 2f, obj.Tamanho * FatorEscalaSpritesAnimados);
                        }
                        else if (temSprite)
                        {
                            DesenharSpriteRotacionado(textura, obj.X + obj.Tamanho / 2f, obj.Y + obj.Tamanho / 2f, obj.Tamanho, 0f);
                        }
                        else
                        {
                            // Desenha formas estáticas simples
                            Raylib.DrawRectangle((int)obj.X, (int)obj.Y, (int)obj.Tamanho, (int)obj.Tamanho, obj.CorBase);
                            if (obj.CorBorda.A > 0) // Desenha borda apenas se for visível
                            {
                                Raylib.DrawRectangleLines((int)obj.X, (int)obj.Y, (int)obj.Tamanho, (int)obj.Tamanho, obj.CorBorda);
                            }
                        }
                        break;

                    case FormatoVisual.Circulo:
                        if (temSprite)
                        {
                            DesenharSpriteRotacionado(textura, obj.X, obj.Y, obj.Tamanho, 0f);
                        }
                        else
                        {
                            // Desenha formas circulares
                            Raylib.DrawCircle((int)obj.X, (int)obj.Y, obj.Tamanho / 2f, obj.CorBase);
                        }
                        break;
                }
            }

            Raylib.EndDrawing();
        }

        // Desenha um frame recortado (sourceRect) de uma spritesheet, centralizado em
        // (centroX, centroY) e escalado para caber em um quadrado de lado 'tamanho'.
        // Não rotaciona: a spritesheet já tem uma imagem desenhada para cada direção.
        private static void DesenharFrame(Texture2D textura, Rectangle fonteFrame, float centroX, float centroY, float tamanho)
        {
            var origem = new System.Numerics.Vector2(tamanho / 2f, tamanho / 2f);
            var destino = new Rectangle(centroX, centroY, tamanho, tamanho);

            Raylib.DrawTexturePro(textura, fonteFrame, destino, origem, 0f, Color.White);
        }

        // Desenha uma textura centralizada em (centroX, centroY), escalada para caber em
        // um quadrado de lado 'tamanho' e rotacionada por 'anguloGraus' em torno do próprio centro.
        private static void DesenharSpriteRotacionado(Texture2D textura, float centroX, float centroY, float tamanho, float anguloGraus)
        {
            var origem = new System.Numerics.Vector2(tamanho / 2f, tamanho / 2f);
            var destino = new Rectangle(centroX, centroY, tamanho, tamanho);
            var fonte = new Rectangle(0, 0, textura.Width, textura.Height);

            Raylib.DrawTexturePro(textura, fonte, destino, origem, anguloGraus, Color.White);
        }
    }
}
