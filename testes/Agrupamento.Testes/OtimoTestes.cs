using Conde.Agrupamento;
using Xunit;

namespace Conde.Agrupamento.Testes;

/// <summary>
/// O juiz testado antes de julgar. Se a busca exaustiva estiver errada, toda
/// razao de aproximacao deste repositorio e invencao.
/// </summary>
public class OtimoTestes
{
    [Fact]
    public void OCustoDeUmGrupoSoEhAVarianciaVezesOTamanho()
    {
        var nuvem = new Nuvem([(0, 0), (2, 0), (4, 0)]);
        // A media e 2, e os quadrados sao 4, 0 e 4.
        Assert.Equal(8, Otimo.Custo(nuvem, [0, 0, 0]), 1e-9);
    }

    [Fact]
    public void CadaPontoNoProprioGrupoCustaZero()
    {
        var nuvem = Nuvem.Uniforme(8, 3);
        Assert.Equal(0, Otimo.Custo(nuvem, [.. Enumerable.Range(0, 8)]), 1e-9);
    }

    [Fact]
    public void OPontoDeRuidoNaoEntraNoCusto()
    {
        var nuvem = new Nuvem([(0, 0), (2, 0), (100, 100)]);
        Assert.Equal(2, Otimo.Custo(nuvem, [0, 0, Densidade.Ruido]), 1e-9);
    }

    /// <summary>
    /// Em quatro pontos nos cantos de um retangulo largo, o melhor par de grupos
    /// e obvio e da para conferir de cabeca.
    /// </summary>
    [Fact]
    public void OMelhorDeDoisGruposConfereDeCabeca()
    {
        var nuvem = new Nuvem([(0, 0), (0, 1), (10, 0), (10, 1)]);
        var (rotulos, custo) = Otimo.Melhor(nuvem, 2);

        Assert.Equal(1, custo, 1e-9);
        Assert.Equal(rotulos[0], rotulos[1]);
        Assert.Equal(rotulos[2], rotulos[3]);
        Assert.NotEqual(rotulos[0], rotulos[2]);
    }

    [Fact]
    public void OOtimoUsaExatamenteOsGruposPedidos()
    {
        foreach (var grupos in new[] { 2, 3, 4 })
        {
            var (rotulos, _) = Otimo.Melhor(Nuvem.Uniforme(8, 5), grupos);
            Assert.Equal(grupos, rotulos.Distinct().Count());
        }
    }

    /// <summary>
    /// Mais grupos nunca custa mais: o otimo com k+1 grupos e sempre menor ou
    /// igual ao com k. E a razao de o numero de grupos nao poder sair do proprio
    /// criterio.
    /// </summary>
    [Fact]
    public void MaisGruposNuncaCustaMais()
    {
        var nuvem = Nuvem.Uniforme(9, 11);
        double anterior = double.MaxValue;

        foreach (var grupos in new[] { 1, 2, 3, 4, 5 })
        {
            var (_, custo) = Otimo.Melhor(nuvem, grupos);
            Assert.True(custo <= anterior + 1e-9, $"com {grupos} grupos ficou pior");
            anterior = custo;
        }
    }

    [Fact]
    public void ANuvemGrandeDemaisEhRecusada()
    {
        Assert.Throws<ArgumentException>(() => Otimo.Melhor(Nuvem.Uniforme(20, 1), 3));
    }

    /// <summary>
    /// A enumeracao evita renomear grupos: cada reparticao sai UMA vez. Com tres
    /// pontos em dois grupos sao tres reparticoes, nao seis.
    /// </summary>
    [Fact]
    public void AContaDeReparticoesConfereComAEnumeracao()
    {
        Assert.Equal(3, (int)Otimo.Stirling(3, 2));
        Assert.Equal(1, (int)Otimo.Stirling(3, 1));
        Assert.Equal(1, (int)Otimo.Stirling(3, 3));
        Assert.Equal(0, (int)Otimo.Stirling(3, 4));
        Assert.Equal(9330, (int)Otimo.Stirling(10, 3));
    }

    /// <summary>
    /// Comparar rotulos item a item nao serve: o mesmo agrupamento sai com nomes
    /// diferentes dependendo da ordem em que os grupos foram criados.
    /// </summary>
    [Fact]
    public void OMesmoAgrupamentoComOutrosNomesEhOMesmo()
    {
        Assert.True(Otimo.MesmoAgrupamento([0, 0, 1], [1, 1, 0]));
        Assert.True(Otimo.MesmoAgrupamento([0, 1, 2], [2, 0, 1]));
        Assert.False(Otimo.MesmoAgrupamento([0, 0, 1], [0, 1, 1]));
        Assert.False(Otimo.MesmoAgrupamento([0, 0], [0, 0, 0]));
    }

    [Fact]
    public void UmGrupoSoEhSempreAMesmaResposta()
    {
        var nuvem = Nuvem.Uniforme(10, 7);
        var (_, custo) = Otimo.Melhor(nuvem, 1);
        Assert.Equal(Otimo.Custo(nuvem, new int[10]), custo, 1e-9);
    }
}
