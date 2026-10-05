using Conde.Agrupamento;
using Xunit;

namespace Conde.Agrupamento.Testes;

public class DensidadeTestes
{
    private const int Densidade20 = 20;

    private static (Nuvem Nuvem, int Dentro, int Total) Aneis()
    {
        var nuvem = Nuvem.Aneis(Densidade20, 3);
        return (nuvem, Nuvem.PontosDoAnelDeDentro(Densidade20), nuvem.Quantos);
    }

    private static int Acertos(IReadOnlyList<int> rotulos, int dentro, int total) =>
        Enumerable.Range(0, total)
            .Count(i => rotulos[i] >= 0 && rotulos[i] == rotulos[i < dentro ? 0 : total - 1]);

    /// <summary>
    /// O k-medias nao consegue separar dois aneis concentricos, e nao e defeito
    /// de implementacao: ele so sabe fazer grupo convexo.
    /// </summary>
    [Fact]
    public void OKMediasNaoSeparaAneis()
    {
        var (nuvem, dentro, total) = Aneis();
        var saida = KMedias.Melhor(nuvem, 2, 20);
        Assert.True(Acertos(saida.Rotulos, dentro, total) < total * 0.8);
    }

    /// <summary>E o DBSCAN separa sem esforco, com qualquer raio da faixa boa.</summary>
    [Fact]
    public void ODbscanSeparaAneis()
    {
        var (nuvem, dentro, total) = Aneis();

        foreach (var raio in new[] { 1.5, 2.0, 3.0 })
        {
            var saida = Densidade.Agrupar(nuvem, raio, 4);
            Assert.Equal(2, saida.Grupos);
            Assert.Equal(0, saida.Pontos);
            Assert.Equal(total, Acertos(saida.Rotulos, dentro, total));
        }
    }

    [Fact]
    public void AOLigacaoSimplesTambemSeparaAneis()
    {
        var (nuvem, dentro, total) = Aneis();
        var rotulos = Hierarquico.Agrupar(nuvem, 2, Hierarquico.Ligacao.Simples);
        Assert.Equal(total, Acertos(rotulos, dentro, total));
    }

    /// <summary>
    /// E a ligacao completa nao separa: ela quer grupos arredondados, e um anel
    /// nao e arredondado no sentido dela.
    /// </summary>
    [Fact]
    public void ALigacaoCompletaNaoSeparaAneis()
    {
        var (nuvem, dentro, total) = Aneis();
        var rotulos = Hierarquico.Agrupar(nuvem, 2, Hierarquico.Ligacao.Completa);
        Assert.True(Acertos(rotulos, dentro, total) < total);
    }

    /// <summary>
    /// Com raio pequeno demais, cada anel vira varios pedacos; com raio grande
    /// demais, os dois viram um so. A faixa boa e larga e nao e infinita.
    /// </summary>
    [Fact]
    public void ORaioPequenoDemaisPicotaEOGrandeDemaisFunde()
    {
        var (nuvem, _, _) = Aneis();

        Assert.True(Densidade.Agrupar(nuvem, 0.8, 4).Grupos > 2, "raio pequeno deveria picotar");
        Assert.Equal(1, Densidade.Agrupar(nuvem, 7.0, 4).Grupos);
        Assert.Equal(2, Densidade.Agrupar(nuvem, 2.0, 4).Grupos);
    }

    /// <summary>
    /// O DBSCAN e o unico dos tres que tem a opcao de NAO agrupar um ponto, e
    /// ele acha o ruido quase exato.
    /// </summary>
    [Fact]
    public void ODbscanSeparaORuido()
    {
        var nuvem = Nuvem.ComRuido(80, 20, 5);
        var saida = Densidade.Agrupar(nuvem, 0.5, 4);

        Assert.Equal(1, saida.Grupos);
        Assert.InRange(saida.Pontos, 15, 25);

        // Os primeiros 80 sao os densos, e quase todos devem ter entrado.
        var densosAgrupados = Enumerable.Range(0, 80).Count(i => saida.Rotulos[i] >= 0);
        Assert.True(densosAgrupados > 70, $"so {densosAgrupados} dos 80 densos entraram");
    }

    [Fact]
    public void OsOutrosDoisSaoObrigadosAAgruparTudo()
    {
        var nuvem = Nuvem.ComRuido(80, 20, 5);

        Assert.All(KMedias.Rodar(nuvem, 2, 1).Rotulos, r => Assert.True(r >= 0));
        Assert.All(Hierarquico.Agrupar(nuvem, 2, Hierarquico.Ligacao.Media), r => Assert.True(r >= 0));
    }

    [Fact]
    public void ONumeroDeGruposNaoEhPedidoAoDbscan()
    {
        var facil = Nuvem.TresGrupos(20, 7);
        var saida = Densidade.Agrupar(facil, 1.0, 4);
        Assert.Equal(3, saida.Grupos);
    }

    [Fact]
    public void OHierarquicoDevolveExatamenteOsGruposPedidos()
    {
        var nuvem = Nuvem.TresGrupos(10, 3);
        foreach (var (nome, ligacao) in Hierarquico.Todas())
            foreach (var grupos in new[] { 2, 3, 5 })
                Assert.Equal(grupos, Hierarquico.Agrupar(nuvem, grupos, ligacao).Distinct().Count());
    }

    [Fact]
    public void OsRotulosDoHierarquicoComecamEmZeroESaoSeguidos()
    {
        var rotulos = Hierarquico.Agrupar(Nuvem.Uniforme(20, 3), 4, Hierarquico.Ligacao.Media);
        var usados = rotulos.Distinct().OrderBy(r => r).ToList();
        Assert.Equal(new[] { 0, 1, 2, 3 }, usados);
    }

    [Fact]
    public void NaNuvemFacilOsTresConcordam()
    {
        var nuvem = Nuvem.TresGrupos(15, 11);
        var kmedias = KMedias.Melhor(nuvem, 3, 10).Rotulos;
        var hierarquico = Hierarquico.Agrupar(nuvem, 3, Hierarquico.Ligacao.Media);
        var dbscan = Densidade.Agrupar(nuvem, 1.0, 4).Rotulos;

        Assert.True(Otimo.MesmoAgrupamento(kmedias, hierarquico));
        Assert.True(Otimo.MesmoAgrupamento(kmedias, dbscan));
    }

    /// <summary>
    /// A distancia ao k-esimo vizinho sai ordenada, e o salto no fim e o
    /// cotovelo que separa os densos dos ralos.
    /// </summary>
    [Fact]
    public void ADistanciaAoVizinhoSaiOrdenadaETemCotovelo()
    {
        var nuvem = Nuvem.ComRuido(80, 20, 5);
        var distancias = Densidade.DistanciaAoVizinho(nuvem, 4);

        for (var i = 1; i < distancias.Length; i++)
            Assert.True(distancias[i] >= distancias[i - 1]);

        // Os densos ficam colados e o ruido fica longe de tudo: a ultima parte
        // da lista tem que ser muito maior que a mediana.
        Assert.True(distancias[^1] > distancias[distancias.Length / 2] * 5);
    }
}
