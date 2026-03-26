using System;

namespace FG.Evolution.Simulation.Engine.Core
{
    // 'static' garante que esta classe seja um utilitário universal. Zero lixo de memória.
    public static class Sensor
    {
        // O método recebe quem está olhando (ElementoCenario) e para onde está olhando (alvoX, alvoY).
        // Retorna uma Tupla (dois valores empacotados de forma leve).
        public static (float Distancia, float DiferencaAngulo) LerAmbiente(ElementoCenario observador, float alvoX, float alvoY)
        {
            float dX = alvoX - observador.X;
            float dY = alvoY - observador.Y;

            // 1. O Cálculo de Distância (Pitágoras)
            float distancia = (float)Math.Sqrt((dX * dX) + (dY * dY));

            // 2. O Cálculo do Ângulo Relativo (Onde o alvo está em relação ao meu nariz?)
            float anguloParaAlvo = (float)Math.Atan2(dY, dX);
            float diferencaAngulo = anguloParaAlvo - observador.Angulo;

            // Normalização: Impede que a rede neural receba ângulos bizarros como 720 graus.
            // Mantém a visão sempre num limite de -180 a +180 graus (-PI a +PI).
            while (diferencaAngulo > Math.PI) diferencaAngulo -= (float)(Math.PI * 2);
            while (diferencaAngulo < -Math.PI) diferencaAngulo += (float)(Math.PI * 2);

            return (distancia, diferencaAngulo);
        }
    }
}