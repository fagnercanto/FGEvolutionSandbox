using System;
using System.Collections.Generic;
using System.Linq;

namespace FG.Evolution.Simulation.Engine.Core
{
    // 'static' garante que esta classe seja um utilitário universal. Zero lixo de memória.
    public static class Sensor
    {
        // Encontra, dentro de uma coleção de candidatos, o mais próximo do observador.
        // Usado para que Presa/Predador enxerguem "o Tijolo/Planta/Presa/Predador mais próximo".
        public static T ObterMaisProximo<T>(ElementoCenario observador, IEnumerable<T> candidatos) where T : ElementoCenario
        {
            T maisProximo = null;
            float menorDistancia = float.MaxValue;

            foreach (var candidato in candidatos)
            {
                if (candidato == null || ReferenceEquals(candidato, observador)) continue;

                float dX = candidato.X - observador.X;
                float dY = candidato.Y - observador.Y;
                float distanciaQuadrada = (dX * dX) + (dY * dY);

                if (distanciaQuadrada < menorDistancia)
                {
                    menorDistancia = distanciaQuadrada;
                    maisProximo = candidato;
                }
            }

            return maisProximo;
        }

        // Monta as leituras (distância, ângulo) para até 3 alvos mais próximos (um por espécie
        // relevante ao observador). Alvos ausentes (null) são simplesmente ignorados/omitidos.
        public static List<(float Distancia, float DiferencaAngulo)> LerSensoresMultiplos(ElementoCenario observador, params ElementoCenario[] alvos)
        {
            var leituras = new List<(float Distancia, float DiferencaAngulo)>(alvos.Length);

            foreach (var alvo in alvos)
            {
                if (alvo == null) continue;
                leituras.Add(LerAmbiente(observador, alvo.X, alvo.Y));
            }

            return leituras;
        }

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