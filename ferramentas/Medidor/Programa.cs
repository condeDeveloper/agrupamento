using Conde.Agrupamento;

namespace Conde.Agrupamento.Medidor;

/// <summary>
/// As medidas. Razao contra o agrupamento OTIMO EXATO e contagem de acertos:
/// nenhuma depende de relogio nem de maquina.
/// </summary>
public static class Programa
{
    public static int Main(string[] argumentos)
    {
        var qual = argumentos.Length > 0 ? argumentos[0] : "tudo";
        switch (qual)
        {
            case "comeco": Comeco(); break;
            case "formato": Formato(); break;
            case "criterio": Criterio(); break;
            case "parametros": Parametros(); break;
            case "espaco": Espaco(); break;
            case "tudo": Comeco(); Formato(); Criterio(); Parametros(); Espaco(); break;
            default:
                Console.Error.WriteLine("medidas: comeco, formato, criterio, parametros, espaco, tudo");
                return 1;
        }
        return 0;
    }

    /// <summary>De onde os centros comecam, contra o otimo exato.</summary>
    private static void Comeco()
    {
        Console.WriteLine("== o k-medias contra o otimo exato, em 30 nuvens de 10 pontos ==");
        Console.WriteLine($"{"comeco",-16}{"uma tentativa",16}{"dez tentativas",17}{"achou o otimo",16}");

        foreach (var comeco in new[] { KMedias.Comeco.Primeiros, KMedias.Comeco.Sorteado, KMedias.Comeco.MaisMais })
        {
            double uma = 0, dez = 0;
            var acertos = 0;

            for (ulong semente = 1; semente <= 30; semente++)
            {
                var nuvem = Nuvem.Uniforme(10, semente);
                var (_, otimo) = Otimo.Melhor(nuvem, 3);

                uma += KMedias.Rodar(nuvem, 3, semente, comeco).Custo / otimo;

                var melhor = KMedias.Melhor(nuvem, 3, 10, comeco, semente).Custo;
                dez += melhor / otimo;
                if (Math.Abs(melhor - otimo) < 1e-9) acertos++;
            }

            var nome = comeco switch
            {
                KMedias.Comeco.Primeiros => "os k primeiros",
                KMedias.Comeco.Sorteado => "sorteado",
                _ => "k-medias++",
            };

            Console.WriteLine($"{nome,-16}{uma / 30,16:P1}{dez / 30,17:P1}{acertos + "/30",16}");
        }

        Console.WriteLine();
        Console.WriteLine("a primeira linha e o unico comeco DETERMINISTICO, e as duas colunas dela sao");
        Console.WriteLine("iguais: repetir dez vezes nao muda nada, porque as dez rodadas sao a mesma");
        Console.WriteLine("rodada. Ele acha o otimo em 5 das 30 nuvens");
        Console.WriteLine();
        Console.WriteLine("o k-medias++ ganha com UMA tentativa, que e para o que ele existe: ele");
        Console.WriteLine("sorteia cada centro com peso proporcional ao quadrado da distancia ao");
        Console.WriteLine("centro mais perto ja escolhido, entao ponto longe de tudo tem muito mais");
        Console.WriteLine("chance e ponto no meio de um grupo que ja tem centro quase nao tem");
        Console.WriteLine();
        Console.WriteLine("e com dez tentativas os dois sorteados empatam. Isso me corrigiu: eu ia");
        Console.WriteLine("escrever que o k-medias++ e melhor, e o que ele e e melhor POR RODADA. Com");
        Console.WriteLine("reinicio suficiente, o sorteio simples alcanca, e o que o k-medias++ compra");
        Console.WriteLine("de verdade e nao precisar do reinicio");
        Console.WriteLine();
    }

    /// <summary>O formato dos grupos, que e o que cada algoritmo supoe.</summary>
    private static void Formato()
    {
        Console.WriteLine("== dois aneis concentricos: quem consegue separar ==");

        const int densidade = 20;
        var aneis = Nuvem.Aneis(densidade, 3);
        var dentro = Nuvem.PontosDoAnelDeDentro(densidade);
        var total = aneis.Quantos;

        Console.WriteLine($"  {dentro} pontos no anel de dentro e {total - dentro} no de fora, com a MESMA densidade");
        Console.WriteLine();
        Console.WriteLine($"{"algoritmo",-26}{"acertou",14}{"observacao",30}");

        var kmedias = KMedias.Melhor(aneis, 2, 20);
        Console.WriteLine($"{"k-medias, 20 tentativas",-26}{Acertos(kmedias.Rotulos, dentro, total),14}" +
                          $"{"so sabe fazer grupo convexo",30}");

        foreach (var (nome, ligacao) in Hierarquico.Todas())
        {
            var rotulos = Hierarquico.Agrupar(aneis, 2, ligacao);
            var observacao = ligacao == Hierarquico.Ligacao.Simples ? "segue cadeia de vizinhos" : "quer grupo arredondado";
            Console.WriteLine($"{"hierarquico, " + nome.Replace("ligacao ", ""),-26}" +
                              $"{Acertos(rotulos, dentro, total),14}{observacao,30}");
        }

        var dbscan = Densidade.Agrupar(aneis, 1.5, 4);
        Console.WriteLine($"{"DBSCAN, raio 1,5",-26}{Acertos(dbscan.Rotulos, dentro, total),14}" +
                          $"{dbscan.Grupos + " grupos, " + dbscan.Pontos + " de ruido",30}");

        Console.WriteLine();
        Console.WriteLine("o k-medias acerta perto da metade, que e o que o acaso daria. Nao e defeito");
        Console.WriteLine("de implementacao: cada ponto vai para o centro mais perto, e isso divide o");
        Console.WriteLine("plano em pedacos retos. Um anel em volta de outro nao e convexo, e NENHUMA");
        Console.WriteLine("escolha de dois centros separa os dois");
        Console.WriteLine();
        Console.WriteLine("a ligacao simples e o DBSCAN acertam tudo, e pelo mesmo motivo: os dois");
        Console.WriteLine("seguem cadeias de vizinhanca em vez de medir distancia a um centro");
        Console.WriteLine();

        Console.WriteLine("  e na nuvem facil, com tres grupos bem separados, todos acertam:");
        var facil = Nuvem.TresGrupos(20, 7);
        var otimoFacil = KMedias.Melhor(facil, 3, 20).Custo;
        Console.WriteLine($"    k-medias:                custo {otimoFacil,10:N2}");
        Console.WriteLine($"    hierarquico simples:     custo {Otimo.Custo(facil, Hierarquico.Agrupar(facil, 3, Hierarquico.Ligacao.Simples)),10:N2}");
        Console.WriteLine($"    hierarquico completo:    custo {Otimo.Custo(facil, Hierarquico.Agrupar(facil, 3, Hierarquico.Ligacao.Completa)),10:N2}");
        Console.WriteLine($"    DBSCAN raio 1,2:         {Densidade.Agrupar(facil, 1.2, 4)}");
        Console.WriteLine();
        Console.WriteLine("  um algoritmo que erra AQUI esta quebrado, e um que acerta so aqui nao");
        Console.WriteLine("  provou nada");
        Console.WriteLine();
    }

    private static string Acertos(IReadOnlyList<int> rotulos, int dentro, int total)
    {
        var certos = Enumerable.Range(0, total)
            .Count(i => rotulos[i] >= 0 && rotulos[i] == rotulos[i < dentro ? 0 : total - 1]);
        return $"{certos}/{total}";
    }

    /// <summary>Quando o criterio esta certo e a resposta esta errada.</summary>
    private static void Criterio()
    {
        Console.WriteLine("== quando o k-medias acerta o criterio e erra a resposta ==");

        var nuvem = Nuvem.GrandeEPequeno(5);
        var saida = KMedias.Melhor(nuvem, 2, 30);

        var noPequeno = Enumerable.Range(100, 10).Select(i => saida.Rotulos[i]).Distinct().Count();
        var grupoDoPequeno = saida.Rotulos[105];
        var juntos = Enumerable.Range(0, 100).Count(i => saida.Rotulos[i] == grupoDoPequeno);

        Console.WriteLine($"  a nuvem tem 100 pontos espalhados de -50 a 50 e 10 pontos apertados em 60");
        Console.WriteLine($"  o agrupamento certo e obvio para quem olha: os 100 de um lado, os 10 do outro");
        Console.WriteLine();
        Console.WriteLine($"  o k-medias com 30 tentativas devolve custo {saida.Custo:N1}");
        Console.WriteLine($"  os 10 pontos apertados ficaram em {noPequeno} grupo(s)");
        Console.WriteLine($"  e {juntos} dos 100 espalhados foram junto com eles");
        Console.WriteLine();

        var separado = new int[nuvem.Quantos];
        for (var i = 100; i < nuvem.Quantos; i++) separado[i] = 1;
        Console.WriteLine($"  o custo do agrupamento que o olho escolheria: {Otimo.Custo(nuvem, separado):N1}");
        Console.WriteLine();
        Console.WriteLine("o k-medias nao errou: ele minimizou a soma dos quadrados, que e o que ele");
        Console.WriteLine("promete. O agrupamento obvio tem custo MAIOR pelo criterio dele, e o");
        Console.WriteLine("criterio e que discorda do olho");
        Console.WriteLine();
        Console.WriteLine("a razao e que um grupo grande e espalhado contribui com muitos quadrados, e");
        Console.WriteLine("corta-lo ao meio economiza mais do que custa juntar o pequeno a um pedaco");
        Console.WriteLine("dele. Nenhuma quantidade de reinicio conserta isso: nao e minimo local, e o");
        Console.WriteLine("minimo global mesmo");
        Console.WriteLine();
    }

    /// <summary>O quanto o resultado do DBSCAN depende dos dois parametros.</summary>
    private static void Parametros()
    {
        Console.WriteLine("== o DBSCAN e seus dois parametros ==");

        const int densidade = 20;
        var aneis = Nuvem.Aneis(densidade, 3);
        var dentro = Nuvem.PontosDoAnelDeDentro(densidade);
        var total = aneis.Quantos;

        Console.WriteLine($"{"raio",8}{"minimo",9}{"grupos",9}{"ruido",8}{"acertou",12}");

        foreach (var raio in new[] { 0.8, 1.0, 1.2, 1.5, 2.0, 3.0, 7.0 })
            foreach (var minimo in new[] { 3, 5 })
            {
                var saida = Densidade.Agrupar(aneis, raio, minimo);
                Console.WriteLine($"{raio,8:N1}{minimo,9}{saida.Grupos,9}{saida.Pontos,8}" +
                                  $"{Acertos(saida.Rotulos, dentro, total),12}");
            }

        Console.WriteLine();
        Console.WriteLine("com raio pequeno demais cada anel vira varios pedacos e metade dos pontos");
        Console.WriteLine("vira ruido; com raio grande demais os dois aneis viram um so. A faixa que");
        Console.WriteLine("funciona e larga neste caso e nao e infinita");
        Console.WriteLine();

        var distancias = Densidade.DistanciaAoVizinho(aneis, 4);
        Console.WriteLine("  a maneira usual de achar o raio e olhar a distancia ao k-esimo vizinho,");
        Console.WriteLine("  ordenada, e procurar o cotovelo:");
        Console.WriteLine();
        Console.Write("  ");
        foreach (var posicao in new[] { 0, total / 4, total / 2, 3 * total / 4, total - 10, total - 1 })
            Console.Write($"{distancias[posicao],8:N2}");
        Console.WriteLine();
        Console.WriteLine($"  {"(minimo)",8}{"",8}{"(mediana)",8}{"",8}{"",8}{"(maximo)",8}");
        Console.WriteLine();
        Console.WriteLine("  o salto no fim da lista e o cotovelo, e ele e quem separa os pontos");
        Console.WriteLine("  densos dos ralos");
        Console.WriteLine();
    }

    /// <summary>O tamanho do espaco de agrupamentos.</summary>
    private static void Espaco()
    {
        Console.WriteLine("== quantas maneiras de repartir existem ==");
        Console.WriteLine($"{"pontos",9}{"em 2 grupos",16}{"em 3 grupos",18}{"em 4 grupos",20}");

        foreach (var pontos in new[] { 5, 8, 10, 12, 15, 20 })
            Console.WriteLine($"{pontos,9}{Otimo.Stirling(pontos, 2),16:N0}" +
                              $"{Otimo.Stirling(pontos, 3),18:N0}{Otimo.Stirling(pontos, 4),20:N0}");

        Console.WriteLine();
        Console.WriteLine("sao os numeros de Stirling de segunda especie, e eles explodem mais rapido");
        Console.WriteLine("que fatorial. Com vinte pontos em quatro grupos ja sao 45 bilhoes, e e por");
        Console.WriteLine("isso que o juiz exato deste repositorio so roda em nuvem minuscula");
        Console.WriteLine();
        Console.WriteLine("e e exatamente por isso que ele e indispensavel. As heuristicas devolvem um");
        Console.WriteLine("agrupamento plausivel SEMPRE, e agrupamento plausivel de custo razoavel nao");
        Console.WriteLine("parece errado em nada. Sem um otimo de verdade, 'o k-medias fica 18% acima");
        Console.WriteLine("do melhor possivel' nao tem como ser escrito");
        Console.WriteLine();
    }
}
