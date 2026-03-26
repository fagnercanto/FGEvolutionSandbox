# 🧬 FG Evolution Sandbox

### 1. Visão Geral
Um simulador de vida artificial e algoritmo genético desenvolvido em **C# (.NET 8)** utilizando a biblioteca gráfica **Raylib**. O objetivo central é observar a evolução natural de redes neurais simples ("Minions") que aprendem autonomamente a buscar recursos, evitar obstáculos e otimizar rotas ao longo de sucessivas gerações.

### 2. Contexto Arquitetural (Para Contribuidores e IAs Assistentes)
Este projeto foi rigorosamente refatorado para seguir as melhores práticas da indústria de jogos, aplicando conceitos de **Data-Oriented Design** e uma variação simplificada do padrão **ECS (Entity-Component-System)**.

*   **Zero Alocações no Game Loop:** Utilitários de matemática espacial (`Sensor`) são estáticos.
*   **Separação Lógica-Visual:** A simulação roda "cega" (Headless-ready). A camada visual consome apenas estruturas de dados leves (`struct SnapshotVisual`).
*   **Injeção de Dependência:** O estado global é gerenciado por uma classe de `Configuracao` injetada no topo da aplicação.

### 3. Estrutura de Diretórios
A solução está dividida em dois domínios estritos que não se misturam:

*   **📁 Engine (O Motor Base)**
    *   `Core`: Classes abstratas fundamentais (`ElementoCenario`) e utilitários físicos globais (`Sensor`).
    *   `Graphics`: O motor de renderização isolado (`MotorGrafico`) que consome pacotes visuais (`SnapshotVisual`).
*   **📁 Simulation (O Experimento)**
    *   `World`: A `Arena` que atua como juiz físico e executor do algoritmo genético.
    *   `Entities`: Entidades biológicas (`Minion`) que herdam do Core e possuem regras próprias de evolução.
*   **📁 Config:** Parâmetros globais do sistema.

### 4. A Inteligência Artificial (Rede Neural e DNA)
O cérebro de cada entidade é definido por um array de 12 genes (pesos e vieses).
*   **Percepção:** Leitura vetorial do ambiente (distância e ângulo relativo até o alvo).
*   **Processamento:** Multiplicação de matrizes simples definindo 4 vontades de ação: `Frente`, `Girar Esquerda`, `Girar Direita` e `Fugir`.
*   **Algoritmo Genético:** Seleção por elitismo (Top 5 por proximidade). O DNA absoluto (Top 1) é persistido em disco (`melhor_dna.txt`), e a nova geração é instanciada via clonagem com taxa de mutação (`forcaMutacao = 0.1f`) aplicada aos genes da elite.

### 5. Roadmap e Progresso
- [x] Física de movimentação (Modelo Tanque).
- [x] Implementação da Rede Neural com Pesos e Vieses.
- [x] Desacoplamento da Engine Gráfica (ECS).
- [ ] Refatoração: Extrair `Comida` para herdar de `ElementoCenario`.
- [ ] Funcionalidade: Múltiplas fontes de alimento simultâneas.
- [ ] Funcionalidade: Introdução de Predadores.

### 6. Como Executar
1. Certifique-se de ter o **.NET 8 SDK** (ou superior) instalado.
2. Clone o repositório.
3. No terminal, navegue até a pasta do projeto (`FG.Evolution.Simulation`).
4. Execute o comando:
   ```bash
   dotnet run