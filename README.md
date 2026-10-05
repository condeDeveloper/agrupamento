# agrupamento

Agrupamento do zero em C# e .NET 8: k-médias com três maneiras de começar,
DBSCAN e hierárquico com três ligações. O juiz é a busca por **todas** as
maneiras de repartir os pontos, uma por uma.

```
$ dotnet medidor.dll comeco

comeco           uma tentativa   dez tentativas   achou o otimo
os k primeiros          135.8%           135.8%            5/30
sorteado                126.1%           100.5%           28/30
k-medias++              117.9%           100.7%           27/30
```

A primeira linha é o único começo **determinístico**, e as duas colunas dela são
iguais: repetir dez vezes não muda nada, porque as dez rodadas são a mesma
rodada.

## Por que o juiz é exaustivo

As heurísticas devolvem um agrupamento plausível **sempre**, e agrupamento
plausível de custo razoável não parece errado em nada. Sem um ótimo de verdade, a
frase "o k-médias fica 18% acima do melhor possível" não tem como ser escrita.

O preço é que ele só roda em nuvem minúscula:

```
$ dotnet medidor.dll espaco

   pontos     em 2 grupos       em 3 grupos         em 4 grupos
       10             511             9,330              34,105
       15          16,383         2,375,101          42,355,950
       20         524,287       580,606,446      45,232,115,901
```

São os números de Stirling de segunda espécie, e eles explodem mais rápido que
fatorial.

A enumeração evita renomear grupos: ela fixa o primeiro ponto no grupo zero e só
permite o rótulo `g+1` depois de o rótulo `g` já ter aparecido. Sem isso cada
repartição sairia k! vezes, e a busca gastaria o tempo inteiro confirmando que
`[0,0,1]` e `[1,1,0]` são a mesma coisa.

Pelo mesmo motivo, comparar dois agrupamentos **não** é comparar rótulos item a
item: um teste que faz isso reclama de resultado certo.

## O que as medidas me corrigiram

**O k-médias++ não é melhor, é melhor por rodada.** Ele ganha com uma tentativa
só, que é para o que ele existe. Com dez tentativas, o sorteio simples alcança e
até passa na média. O que o k-médias++ compra de verdade é **não precisar do
reinício**.

**Os dois anéis com a mesma contagem de pontos não têm a mesma densidade.** Eu
pus sessenta pontos em cada anel, o que parece justo: o de fora tem quatro vezes
a circunferência do de dentro, então ficou quatro vezes mais ralo. Nenhum raio
servia para os dois ao mesmo tempo: com raio pequeno o anel de fora virava onze
pedaços, e com raio grande os dois viravam um só.

Com a contagem **proporcional ao raio**, o DBSCAN acerta com folga:

```
$ dotnet medidor.dll formato

  40 pontos no anel de dentro e 160 no de fora, com a MESMA densidade

algoritmo                        acertou                    observacao
k-medias, 20 tentativas           98/200   so sabe fazer grupo convexo
hierarquico, simples             200/200      segue cadeia de vizinhos
hierarquico, completa            145/200        quer grupo arredondado
hierarquico, media               111/200        quer grupo arredondado
DBSCAN, raio 1,5                 200/200          2 grupos, 0 de ruido
```

A lição é sobre o algoritmo e não sobre a nuvem: DBSCAN com um raio só **supõe
densidade parecida**, e essa suposição é tão forte quanto a convexidade que o
k-médias supõe.

**E o caso desbalanceado precisou de números maiores para o efeito aparecer.** A
primeira versão tinha os cem pontos espalhados numa faixa estreita, o grupo
pequeno custava caro demais para ser adotado, e o k-médias acertava. Com a faixa
larga, a conta inverte.

## Quando o critério está certo e a resposta está errada

```
$ dotnet medidor.dll criterio

  a nuvem tem 100 pontos espalhados de -50 a 50 e 10 pontos apertados em 60
  o agrupamento certo e obvio para quem olha: os 100 de um lado, os 10 do outro

  o k-medias com 30 tentativas devolve custo 29,931.3
  os 10 pontos apertados ficaram em 1 grupo(s)
  e 38 dos 100 espalhados foram junto com eles

  o custo do agrupamento que o olho escolheria: 84,646.6
```

O k-médias **não errou**: ele minimizou a soma dos quadrados, que é o que ele
promete. O agrupamento óbvio tem custo quase três vezes maior pelo critério dele.

A razão é que um grupo grande e espalhado contribui com muitos quadrados, e
cortá-lo ao meio economiza mais do que custa juntar o pequeno a um pedaço dele.
Nenhuma quantidade de reinício conserta isso: não é mínimo local, é o mínimo
global mesmo.

E vale notar de onde vem a simplicidade do método: minimizar a soma das
**distâncias** é um problema diferente, com outra resposta. O ponto que minimiza a
soma dos quadrados é a média; o que minimiza a soma das distâncias é a mediana
geométrica, que nem tem fórmula fechada.

## O DBSCAN e seus dois parâmetros

```
$ dotnet medidor.dll parametros

    raio   minimo   grupos   ruido     acertou
     0.8        3       11       8      29/200
     1.2        3        4       0     131/200
     1.5        3        2       0     200/200
     3.0        3        2       0     200/200
     7.0        3        1       0     200/200
```

Com raio pequeno demais cada anel vira vários pedaços; com raio grande demais os
dois viram um só. A faixa que funciona é larga e não é infinita.

A maneira usual de achar o raio é olhar a distância ao k-ésimo vizinho, ordenada,
e procurar o **cotovelo**: o salto no fim da lista separa os pontos densos dos
ralos. O medidor imprime essa lista em números em vez de desenhar o gráfico.

## As peças

| arquivo | o que faz |
| --- | --- |
| `Nuvem.cs` | as nuvens de teste, e por que cada uma tem os números que tem |
| `Otimo.cs` | o juiz, Stirling, e a comparação que ignora o nome do grupo |
| `KMedias.cs` | Lloyd, e os três começos |
| `Densidade.cs` | DBSCAN e o hierárquico com as três ligações |

## Como rodar

```
dotnet test testes/Agrupamento.Testes/Agrupamento.Testes.csproj -c Release
dotnet run --project ferramentas/Medidor/Medidor.csproj -c Release -- tudo
```

As medidas aceitam `comeco`, `formato`, `criterio`, `parametros`, `espaco` e
`tudo`.

## Licença

MIT.
