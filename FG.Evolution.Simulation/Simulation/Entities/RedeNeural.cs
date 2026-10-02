using System;
using System.Collections.Generic;

namespace FG.Evolution.Simulation.Simulation.Entities
{
    // Ações possíveis para qualquer agente evolutivo (Presa ou Predador).
    public enum AcaoAgente
    {
        GirarEsquerda,
        Frente,
        GirarDireita, // Também usado como "Atacar/Investida" para o Predador
        Fugir         // Freio
    }

    // Rede neural linear compartilhada por Presa e Predador.
    // DNA: 15 genes (float, -1.0 a 1.0).
    //   Índices 0-2   -> peso Distância, peso Ângulo, viés da ação "Girar Esquerda"
    //   Índices 3-5   -> peso Distância, peso Ângulo, viés da ação "Frente"
    //   Índices 6-8   -> peso Distância, peso Ângulo, viés da ação "Girar Direita" (Ataque/Investida)
    //   Índices 9-11  -> peso Distância, peso Ângulo, viés da ação "Fugir"
    //   Índice 12     -> Inércia: multiplicador aplicado à força da última ação vencedora,
    //                    somado novamente à vontade da mesma ação neste tick. Isso quebra empates
    //                    matemáticos quando o agente está a distâncias idênticas de dois alvos
    //                    (Paradoxo de Zeno), favorecendo a continuidade da ação anterior.
    //   Índice 13     -> Viés de Ângulo de Colisão: desloca o ângulo de fuga (ao colidir com
    //                    parede/obstáculo) para a esquerda ou direita, em vez de sempre virar
    //                    exatamente para o lado oposto. Escala de -45° a +45°.
    //   Índice 14     -> Intensidade de Mudança de Direção: controla o quão brusca é a guinada
    //                    ao colidir (multiplica a variação aleatória do ângulo de fuga, de 0.2x
    //                    a 1.8x a variação padrão).
    //
    // Cada agente pode ter até 3 sensores simultâneos (ex: Tijolo, Predador, Planta para a Presa).
    // Como o genoma é compacto, os mesmos 2 pesos (Distância/Ângulo) de cada ação são
    // aplicados a TODOS os sensores e somados, e o viés é aplicado uma única vez por ação.
    public class RedeNeural
    {
        public const int QuantidadeGenes = 15;
        private const int IndiceInercia = 12;
        private const int IndiceViesAnguloColisao = 13;
        private const int IndiceIntensidadeMudancaDirecao = 14;

        private static readonly Random sorteador = new Random();

        public float[] DNA { get; private set; }

        // Viés de ângulo de colisão (gene 14), já convertido para radianos: desloca o ângulo
        // de fuga entre -45° e +45° em vez de sempre virar exatamente para o lado oposto.
        public float ViesAnguloColisaoRad => DNA[IndiceViesAnguloColisao] * (float)(Math.PI / 4.0);

        // Intensidade de mudança de direção (gene 15): escala a variação aleatória do ângulo
        // de fuga entre 0.2x (guinada suave) e 1.8x (guinada brusca) a variação padrão.
        public float IntensidadeMudancaDirecao => 1f + (DNA[IndiceIntensidadeMudancaDirecao] * 0.8f);

        // Estado de inércia: guarda a última ação vencedora e a força (vontade) com que ela venceu.
        public AcaoAgente UltimaAcao { get; private set; } = AcaoAgente.Fugir;
        public float UltimaForca { get; private set; } = 0f;

        public RedeNeural(float[] dnaHerdado = null)
        {
            if (dnaHerdado == null)
            {
                DNA = new float[QuantidadeGenes];
                for (int i = 0; i < DNA.Length; i++)
                {
                    DNA[i] = (float)(sorteador.NextDouble() * 2 - 1);
                }
            }
            else
            {
                if (dnaHerdado.Length != QuantidadeGenes)
                {
                    throw new ArgumentException($"DNA herdado deve ter {QuantidadeGenes} genes, recebeu {dnaHerdado.Length}.", nameof(dnaHerdado));
                }

                DNA = dnaHerdado;
            }
        }

        // Calcula a ação vencedora a partir de até 3 leituras de sensores (distância, ângulo).
        // Sensores ausentes (ex: nenhum Predador visível) devem ser omitidos da lista.
        public AcaoAgente Decidir(IReadOnlyList<(float Distancia, float DiferencaAngulo)> leituras)
        {
            float vontadeGirarEsquerda = DNA[2];
            float vontadeFrente = DNA[5];
            float vontadeGirarDireita = DNA[8];
            float vontadeFugir = DNA[11];

            foreach (var leitura in leituras)
            {
                vontadeGirarEsquerda += (leitura.Distancia * DNA[0]) + (leitura.DiferencaAngulo * DNA[1]);
                vontadeFrente += (leitura.Distancia * DNA[3]) + (leitura.DiferencaAngulo * DNA[4]);
                vontadeGirarDireita += (leitura.Distancia * DNA[6]) + (leitura.DiferencaAngulo * DNA[7]);
                vontadeFugir += (leitura.Distancia * DNA[9]) + (leitura.DiferencaAngulo * DNA[10]);
            }

            // Gene 13 (Inércia): reforça a vontade da ação que já estava vencendo no tick anterior.
            // Importante: a inércia é calculada sobre a força "crua" do tick anterior (antes do
            // próprio bônus de inércia ser somado), nunca sobre o valor já amplificado. Caso
            // contrário, o bônus realimentaria a si mesmo a cada tick (loop de feedback positivo),
            // crescendo exponencialmente e travando o agente numa única ação para sempre, surdo a
            // qualquer estímulo dos sensores.
            float inercia = DNA[IndiceInercia] * UltimaForca;
            float vontadeGirarEsquerdaComInercia = vontadeGirarEsquerda;
            float vontadeFrenteComInercia = vontadeFrente;
            float vontadeGirarDireitaComInercia = vontadeGirarDireita;
            float vontadeFugirComInercia = vontadeFugir;

            switch (UltimaAcao)
            {
                case AcaoAgente.GirarEsquerda: vontadeGirarEsquerdaComInercia += inercia; break;
                case AcaoAgente.Frente: vontadeFrenteComInercia += inercia; break;
                case AcaoAgente.GirarDireita: vontadeGirarDireitaComInercia += inercia; break;
                case AcaoAgente.Fugir: vontadeFugirComInercia += inercia; break;
            }

            float maiorVontade = vontadeGirarEsquerdaComInercia;
            AcaoAgente acaoVencedora = AcaoAgente.GirarEsquerda;
            float forcaCrua = vontadeGirarEsquerda;

            if (vontadeFrenteComInercia > maiorVontade) { maiorVontade = vontadeFrenteComInercia; acaoVencedora = AcaoAgente.Frente; forcaCrua = vontadeFrente; }
            if (vontadeGirarDireitaComInercia > maiorVontade) { maiorVontade = vontadeGirarDireitaComInercia; acaoVencedora = AcaoAgente.GirarDireita; forcaCrua = vontadeGirarDireita; }
            if (vontadeFugirComInercia > maiorVontade) { maiorVontade = vontadeFugirComInercia; acaoVencedora = AcaoAgente.Fugir; forcaCrua = vontadeFugir; }

            UltimaAcao = acaoVencedora;
            // Guarda a força SEM o bônus de inércia, evitando o loop de realimenta\u00e7\u00e3o exponencial.
            UltimaForca = forcaCrua;

            return acaoVencedora;
        }

        // Clona este DNA aplicando uma mutação simples (sem crossover), dentro da força informada.
        public float[] ClonarEMutarDna(float forcaMutacao)
        {
            float[] dnaFilho = new float[QuantidadeGenes];

            for (int i = 0; i < DNA.Length; i++)
            {
                float mutacao = (float)(sorteador.NextDouble() * 2 - 1) * forcaMutacao;
                dnaFilho[i] = DNA[i] + mutacao;
            }

            return dnaFilho;
        }
    }
}
