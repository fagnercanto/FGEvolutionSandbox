# Sprites da Simulação

Coloque aqui arquivos PNG (com fundo transparente) usando exatamente estes nomes
para que o `MotorGrafico` os carregue automaticamente no lugar dos desenhos
vetoriais atuais (retângulos/círculos coloridos):

| Arquivo         | Entidade   | Sugestão de arte                         |
|-----------------|------------|-------------------------------------------|
| `presa.png`     | Presa      | Joaninha (vista de cima, "olhando" para cima na imagem) |
| `predador.png`  | Predador   | Lagartixa (vista de cima, "olhando" para cima na imagem) |
| `planta.png`    | Planta     | Folha/planta simples (vista de cima)      |
| `tijolo.png`    | Tijolo     | Textura de tijolo/pedra (quadrada)        |

## Convenções

- A arte de `presa.png` e `predador.png` deve estar desenhada "olhando para cima"
  (0° no topo da imagem), pois o motor gráfico rotaciona a textura de acordo com
  o ângulo de movimento do agente.
- Recomenda-se imagens quadradas (ex: 64x64 ou 128x128) para evitar distorção,
  já que são escaladas para o tamanho atual da entidade na simulação.
- Se um arquivo não existir, a simulação continua funcionando normalmente e usa
  o desenho vetorial (retângulo/círculo colorido) como no visual atual — não é
  necessário ter todos os sprites prontos ao mesmo tempo.

### Spritesheet animado da Presa (`presa.png`, ex-roach.png)

`presa.png` é tratado como uma spritesheet animada (grid de 4 colunas x 5 linhas,
frames de 32x32), configurada em `Engine/Graphics/SpriteSheets.cs`. O layout
confirmado, lido da esquerda para a direita e de cima para baixo (índice
"achatado" 0-based), é:

| Índice | 0        | 1       | 2         | 3             |
|--------|----------|---------|-----------|---------------|
| 0-3    | direita  | direita | direita   | direita       |
| 4-7    | cima     | cima    | cima      | baixo         |
| 8-11   | baixo    | baixo   | baixo     | esquerda      |
| 12-15  | esquerda | esquerda| esquerda  | baixo-morta   |
| 16-19  | direita-morta | cima-morta | esquerda-morta | (vazio) |

Ou seja: caminhada de Direita usa os frames 0-3 (4 frames); Cima usa 4-6 (3
frames, pois o frame 7 pertence à caminhada de Baixo); Baixo usa 7-10 (4
frames); Esquerda usa 11-14 (4 frames). A animação de morte é um único frame
fixo por direção: Baixo=15, Direita=16, Cima=17, Esquerda=18. O índice 19 fica
vazio/não utilizado.

Se o layout de uma nova spritesheet mudar, basta ajustar os arrays de índices
em `SpriteSheets.Animacoes` — nenhum outro arquivo precisa mudar.

## Como adicionar

1. Baixe/produza o PNG com o nome exato da tabela acima.
2. Copie o arquivo para esta pasta (`FG.Evolution.Simulation/Assets/Sprites/`).
3. Rode o projeto novamente — o `GerenciadorTexturas` carrega o arquivo
   automaticamente na primeira vez que a entidade correspondente aparece em tela.
