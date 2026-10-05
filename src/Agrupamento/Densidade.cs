namespace Conde.Agrupamento;

/// <summary>
/// DBSCAN, de Ester, Kriegel, Sander e Xu, 1996: agrupar por DENSIDADE em vez de
/// por distancia a um centro.
///
/// Ele responde outra pergunta. O k-medias pergunta "qual centro esta mais
/// perto"; o DBSCAN pergunta "este ponto tem vizinhos suficientes por perto, e
/// esses vizinhos tem os deles". Dois pontos ficam no mesmo grupo quando existe
/// uma CADEIA de vizinhancas densas ligando um ao outro.
///
/// Isso muda tres coisas de uma vez:
///
///   1. o numero de grupos nao e dado: sai da conta
///   2. os grupos nao precisam ser convexos: um anel em volta de outro funciona
///   3. ha a opcao de NAO agrupar, e ponto solto fica marcado como ruido
///
/// O preco e que os dois parametros, o raio e o minimo de vizinhos, nao sao
/// obvios, e o resultado muda muito com eles. A medida mostra esse muda muito.
/// </summary>
public static class Densidade
{
    /// <summary>O rotulo dos pontos que nao entraram em grupo nenhum.</summary>
    public const int Ruido = -1;

    /// <summary>O que o DBSCAN encontrou.</summary>
    public sealed class Saida
    {
        public Saida(int[] rotulos, int grupos, int ruido, int nucleos)
        {
            Rotulos = rotulos;
            Grupos = grupos;
            Pontos = ruido;
            Nucleos = nucleos;
        }

        public int[] Rotulos { get; }

        /// <summary>Quantos grupos sairam. Nao foi pedido: foi encontrado.</summary>
        public int Grupos { get; }

        /// <summary>Quantos pontos ficaram de fora.</summary>
        public int Pontos { get; }

        /// <summary>Quantos pontos tem vizinhos suficientes para puxar grupo.</summary>
        public int Nucleos { get; }

        public override string ToString() => $"{Grupos} grupos, {Pontos} de ruido, {Nucleos} nucleos";
    }

    /// <summary>
    /// Agrupa por densidade.
    ///
    /// Um ponto e NUCLEO quando tem pelo menos <paramref name="minimo"/> vizinhos
    /// dentro do <paramref name="raio"/>, contando ele mesmo. Nucleo puxa grupo e
    /// arrasta os vizinhos; vizinho de nucleo que nao e nucleo entra no grupo e
    /// nao arrasta ninguem, que e a BORDA; quem nao e nem um nem outro e ruido.
    /// </summary>
    public static Saida Agrupar(Nuvem nuvem, double raio, int minimo)
    {
        ArgumentNullException.ThrowIfNull(nuvem);

        var n = nuvem.Quantos;
        var rotulos = new int[n];
        Array.Fill(rotulos, Ruido);

        var vizinhos = new List<int>[n];
        for (var i = 0; i < n; i++)
        {
            vizinhos[i] = [];
            for (var j = 0; j < n; j++)
                if (Nuvem.Distancia(nuvem.Pontos[i], nuvem.Pontos[j]) <= raio) vizinhos[i].Add(j);
        }

        var nucleo = new bool[n];
        for (var i = 0; i < n; i++) nucleo[i] = vizinhos[i].Count >= minimo;

        var visitado = new bool[n];
        var grupo = 0;

        for (var i = 0; i < n; i++)
        {
            if (visitado[i] || !nucleo[i]) continue;

            var fila = new Queue<int>();
            fila.Enqueue(i);
            visitado[i] = true;
            rotulos[i] = grupo;

            while (fila.Count != 0)
            {
                var atual = fila.Dequeue();

                // So NUCLEO espalha. Um ponto de borda entra no grupo e nao
                // puxa os vizinhos dele, e e essa assimetria que impede dois
                // grupos densos ligados por uma ponte rala de virarem um so.
                if (!nucleo[atual]) continue;

                foreach (var vizinho in vizinhos[atual])
                {
                    if (rotulos[vizinho] == Ruido) rotulos[vizinho] = grupo;
                    if (visitado[vizinho]) continue;
                    visitado[vizinho] = true;
                    fila.Enqueue(vizinho);
                }
            }

            grupo++;
        }

        return new Saida(rotulos, grupo, rotulos.Count(r => r == Ruido), nucleo.Count(b => b));
    }

    /// <summary>
    /// A distancia ate o k-esimo vizinho de cada ponto, ordenada.
    ///
    /// E a maneira usual de escolher o raio: o grafico dessa lista tem um
    /// COTOVELO, e o raio do cotovelo separa os pontos densos dos ralos. Aqui ela
    /// existe para a medida mostrar o cotovelo em numero em vez de em desenho.
    /// </summary>
    public static double[] DistanciaAoVizinho(Nuvem nuvem, int k)
    {
        ArgumentNullException.ThrowIfNull(nuvem);

        var saida = new double[nuvem.Quantos];
        for (var i = 0; i < nuvem.Quantos; i++)
        {
            var distancias = Enumerable.Range(0, nuvem.Quantos)
                .Where(j => j != i)
                .Select(j => Nuvem.Distancia(nuvem.Pontos[i], nuvem.Pontos[j]))
                .OrderBy(d => d)
                .ToList();
            saida[i] = distancias[Math.Min(k - 1, distancias.Count - 1)];
        }

        Array.Sort(saida);
        return saida;
    }
}

/// <summary>
/// O agrupamento HIERARQUICO por aglomeracao: comeca com cada ponto no proprio
/// grupo e junta os dois mais proximos, repetidamente.
///
/// O que muda tudo e o criterio de "mais proximos", porque dois grupos tem muitas
/// distancias entre si e e preciso escolher qual delas conta. As tres escolhas
/// classicas dao resultados completamente diferentes na mesma nuvem, e a medida
/// mostra isso.
/// </summary>
public static class Hierarquico
{
    /// <summary>Qual distancia entre dois grupos conta.</summary>
    public enum Ligacao
    {
        /// <summary>
        /// A MENOR distancia entre um ponto de um e um do outro.
        ///
        /// Ela acha grupos alongados e segue cadeias, o que e otimo para anel e
        /// pessimo quando dois grupos densos tem uma ponte rala entre eles: um
        /// unico par de pontos proximos cola os dois.
        /// </summary>
        Simples,

        /// <summary>
        /// A MAIOR distancia. Ela e o oposto: produz grupos compactos e
        /// arredondados, e parte grupo alongado ao meio.
        /// </summary>
        Completa,

        /// <summary>A media de todas as distancias entre os dois grupos.</summary>
        Media,
    }

    /// <summary>
    /// Junta ate sobrarem <paramref name="grupos"/> grupos.
    /// </summary>
    public static int[] Agrupar(Nuvem nuvem, int grupos, Ligacao ligacao)
    {
        ArgumentNullException.ThrowIfNull(nuvem);

        var n = nuvem.Quantos;
        var rotulos = Enumerable.Range(0, n).ToArray();
        var vivos = Enumerable.Range(0, n).ToHashSet();

        while (vivos.Count > grupos)
        {
            var melhorA = -1;
            var melhorB = -1;
            var menor = double.MaxValue;

            foreach (var a in vivos)
                foreach (var b in vivos)
                {
                    if (a >= b) continue;
                    var d = Entre(nuvem, rotulos, a, b, ligacao);
                    if (d >= menor) continue;
                    menor = d;
                    melhorA = a;
                    melhorB = b;
                }

            if (melhorA < 0) break;

            for (var i = 0; i < n; i++) if (rotulos[i] == melhorB) rotulos[i] = melhorA;
            vivos.Remove(melhorB);
        }

        // Renumerar para zero, um, dois... O numero do grupo e um nome, e
        // devolver nomes esburacados faz todo teste e toda tabela precisarem
        // lidar com isso.
        var mapa = new Dictionary<int, int>();
        for (var i = 0; i < n; i++)
        {
            if (!mapa.TryGetValue(rotulos[i], out var novo))
            {
                novo = mapa.Count;
                mapa[rotulos[i]] = novo;
            }
            rotulos[i] = novo;
        }

        return rotulos;
    }

    private static double Entre(Nuvem nuvem, int[] rotulos, int a, int b, Ligacao ligacao)
    {
        var deA = Enumerable.Range(0, nuvem.Quantos).Where(i => rotulos[i] == a).ToList();
        var deB = Enumerable.Range(0, nuvem.Quantos).Where(i => rotulos[i] == b).ToList();

        var todas = deA.SelectMany(i => deB.Select(j => Nuvem.Distancia(nuvem.Pontos[i], nuvem.Pontos[j])));

        return ligacao switch
        {
            Ligacao.Simples => todas.Min(),
            Ligacao.Completa => todas.Max(),
            _ => todas.Average(),
        };
    }

    /// <summary>As tres ligacoes, com nome, para as varreduras.</summary>
    public static IEnumerable<(string Nome, Ligacao Qual)> Todas()
    {
        yield return ("ligacao simples", Ligacao.Simples);
        yield return ("ligacao completa", Ligacao.Completa);
        yield return ("ligacao media", Ligacao.Media);
    }
}
