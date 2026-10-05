namespace Conde.Agrupamento;

/// <summary>
/// O JUIZ: todas as maneiras de repartir os pontos em k grupos, uma por uma.
///
/// Sao os NUMEROS DE STIRLING de segunda especie, e eles explodem mais rapido
/// que fatorial: repartir doze pontos em tres grupos ja da 86 mil maneiras, e
/// quinze pontos em quatro da 42 milhoes.
///
/// Por isso ele so roda em nuvem minuscula, e e exatamente por isso que ele e
/// indispensavel. As heuristicas deste repositorio devolvem um agrupamento
/// plausivel SEMPRE, e agrupamento plausivel de custo razoavel nao parece errado
/// em nada. Sem um otimo de verdade, a frase "o k-medias fica 8% acima do melhor
/// possivel" nao tem como ser escrita.
/// </summary>
public static class Otimo
{
    /// <summary>
    /// A SOMA DOS QUADRADOS DENTRO DOS GRUPOS, que e o criterio que o k-medias
    /// minimiza.
    ///
    /// Repare que ela nao e "a qualidade do agrupamento": ela e UM criterio, e os
    /// casos deste repositorio mostram que ele discorda do olho humano em
    /// situacoes bem comuns.
    /// </summary>
    public static double Custo(Nuvem nuvem, IReadOnlyList<int> rotulos)
    {
        ArgumentNullException.ThrowIfNull(nuvem);
        ArgumentNullException.ThrowIfNull(rotulos);

        double soma = 0;
        foreach (var grupo in rotulos.Where(r => r >= 0).Distinct())
        {
            var membros = Enumerable.Range(0, nuvem.Quantos).Where(i => rotulos[i] == grupo).ToList();
            if (membros.Count == 0) continue;

            var centro = (X: membros.Average(i => nuvem.Pontos[i].X),
                          Y: membros.Average(i => nuvem.Pontos[i].Y));
            foreach (var i in membros) soma += Nuvem.DistanciaQuadrada(nuvem.Pontos[i], centro);
        }
        return soma;
    }

    /// <summary>
    /// O melhor agrupamento em exatamente k grupos, procurado um por um.
    ///
    /// A enumeracao fixa o primeiro ponto no grupo zero e so permite o rotulo
    /// g+1 depois de o rotulo g ja ter aparecido. Sem isso, cada reparticao
    /// sairia k! vezes, uma por renomeacao dos grupos, e a busca gastaria o
    /// tempo inteiro confirmando que [0,0,1] e [1,1,0] sao a mesma coisa.
    /// </summary>
    public static (int[] Rotulos, double Custo) Melhor(Nuvem nuvem, int grupos, int limite = 13)
    {
        ArgumentNullException.ThrowIfNull(nuvem);
        if (nuvem.Quantos > limite)
            throw new ArgumentException($"a busca exaustiva so vai ate {limite} pontos", nameof(nuvem));

        int[] melhor = [];
        var menor = double.MaxValue;
        var atual = new int[nuvem.Quantos];

        void Repartir(int indice, int usados)
        {
            if (indice == nuvem.Quantos)
            {
                if (usados != grupos) return;
                var custo = Custo(nuvem, atual);
                if (custo >= menor) return;
                menor = custo;
                melhor = (int[])atual.Clone();
                return;
            }

            for (var g = 0; g <= Math.Min(usados, grupos - 1); g++)
            {
                atual[indice] = g;
                Repartir(indice + 1, Math.Max(usados, g + 1));
            }
        }

        Repartir(0, 0);
        return (melhor, menor);
    }

    /// <summary>
    /// Quantas reparticoes existem: o numero de Stirling de segunda especie.
    /// </summary>
    public static System.Numerics.BigInteger Stirling(int pontos, int grupos)
    {
        if (grupos == 0) return pontos == 0 ? 1 : 0;
        if (grupos > pontos) return 0;

        var tabela = new System.Numerics.BigInteger[pontos + 1, grupos + 1];
        tabela[0, 0] = 1;

        for (var n = 1; n <= pontos; n++)
            for (var k = 1; k <= Math.Min(n, grupos); k++)
                tabela[n, k] = k * tabela[n - 1, k] + tabela[n - 1, k - 1];

        return tabela[pontos, grupos];
    }

    /// <summary>
    /// Se dois agrupamentos sao o MESMO, ignorando o nome dos grupos.
    ///
    /// Comparar rotulos item a item nao serve: o mesmo agrupamento sai com
    /// nomes diferentes dependendo da ordem em que os grupos foram criados, e um
    /// teste que compara rotulos reclama de resultado certo.
    /// </summary>
    public static bool MesmoAgrupamento(IReadOnlyList<int> a, IReadOnlyList<int> b)
    {
        ArgumentNullException.ThrowIfNull(a);
        ArgumentNullException.ThrowIfNull(b);
        if (a.Count != b.Count) return false;

        var deParaA = new Dictionary<int, int>();
        var deParaB = new Dictionary<int, int>();

        for (var i = 0; i < a.Count; i++)
        {
            if (deParaA.TryGetValue(a[i], out var esperadoB))
            {
                if (esperadoB != b[i]) return false;
            }
            else
            {
                if (deParaB.ContainsKey(b[i])) return false;
                deParaA[a[i]] = b[i];
                deParaB[b[i]] = a[i];
            }
        }

        return true;
    }
}
