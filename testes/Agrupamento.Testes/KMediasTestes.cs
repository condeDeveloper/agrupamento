using Conde.Agrupamento;
using Xunit;

namespace Conde.Agrupamento.Testes;

public class KMediasTestes
{
    [Fact]
    public void OKMediasSempreParaSozinho()
    {
        for (ulong semente = 1; semente <= 20; semente++)
        {
            var saida = KMedias.Rodar(Nuvem.Uniforme(40, semente), 4, semente);
            Assert.True(saida.Fechou, $"semente {semente} estourou o limite");
        }
    }

    /// <summary>
    /// Cada volta DIMINUI o custo, e e isso que garante que ele para: o custo
    /// nao pode cair para sempre num conjunto finito de reparticoes.
    /// </summary>
    [Fact]
    public void CadaVoltaDiminuiOCusto()
    {
        var nuvem = Nuvem.Uniforme(30, 3);
        double anterior = double.MaxValue;

        for (var limite = 1; limite <= 10; limite++)
        {
            var custo = KMedias.Rodar(nuvem, 3, 7, KMedias.Comeco.Sorteado, limite).Custo;
            Assert.True(custo <= anterior + 1e-9, $"com limite {limite} o custo subiu");
            anterior = custo;
        }
    }

    [Fact]
    public void TodoPontoRecebeUmGrupo()
    {
        var saida = KMedias.Rodar(Nuvem.Uniforme(25, 9), 4, 1);
        Assert.All(saida.Rotulos, r => Assert.InRange(r, 0, 3));
    }

    [Fact]
    public void AMesmaSementeDaOMesmoResultado()
    {
        var nuvem = Nuvem.Uniforme(30, 5);
        var a = KMedias.Rodar(nuvem, 3, 42);
        var b = KMedias.Rodar(nuvem, 3, 42);

        Assert.Equal(a.Custo, b.Custo, 1e-12);
        Assert.Equal(a.Rotulos, b.Rotulos);
    }

    /// <summary>
    /// O comeco pelos k primeiros pontos e DETERMINISTICO, entao repetir nao
    /// muda nada. E a linha da tabela em que as duas colunas sao iguais.
    /// </summary>
    [Fact]
    public void OComecoPelosPrimeirosNaoMelhoraComRepeticao()
    {
        var nuvem = Nuvem.Uniforme(20, 11);
        var uma = KMedias.Rodar(nuvem, 3, 1, KMedias.Comeco.Primeiros).Custo;
        var dez = KMedias.Melhor(nuvem, 3, 10, KMedias.Comeco.Primeiros).Custo;
        Assert.Equal(uma, dez, 1e-12);
    }

    [Fact]
    public void OComecoSorteadoMelhoraComRepeticao()
    {
        var nuvem = Nuvem.Uniforme(20, 11);
        var uma = KMedias.Rodar(nuvem, 3, 1, KMedias.Comeco.Sorteado).Custo;
        var dez = KMedias.Melhor(nuvem, 3, 10, KMedias.Comeco.Sorteado).Custo;
        Assert.True(dez <= uma + 1e-9);
    }

    /// <summary>
    /// O k-medias++ ganha POR RODADA, que e para o que ele existe.
    /// </summary>
    [Fact]
    public void OKMediasMaisMaisGanhaComUmaTentativaSo()
    {
        double sorteado = 0, maisMais = 0;

        for (ulong semente = 1; semente <= 30; semente++)
        {
            var nuvem = Nuvem.Uniforme(10, semente);
            var (_, otimo) = Otimo.Melhor(nuvem, 3);
            sorteado += KMedias.Rodar(nuvem, 3, semente, KMedias.Comeco.Sorteado).Custo / otimo;
            maisMais += KMedias.Rodar(nuvem, 3, semente, KMedias.Comeco.MaisMais).Custo / otimo;
        }

        Assert.True(maisMais < sorteado, $"++ deu {maisMais / 30:P1} e sorteado deu {sorteado / 30:P1}");
    }

    /// <summary>
    /// E com reinicio suficiente os dois empatam. Isso me corrigiu: o k-medias++
    /// nao e melhor, ele e melhor POR RODADA, e o que ele compra de verdade e
    /// nao precisar do reinicio.
    /// </summary>
    [Fact]
    public void ComDezTentativasOsDoisSorteadosEmpatam()
    {
        double sorteado = 0, maisMais = 0;

        for (ulong semente = 1; semente <= 30; semente++)
        {
            var nuvem = Nuvem.Uniforme(10, semente);
            var (_, otimo) = Otimo.Melhor(nuvem, 3);
            sorteado += KMedias.Melhor(nuvem, 3, 10, KMedias.Comeco.Sorteado, semente).Custo / otimo;
            maisMais += KMedias.Melhor(nuvem, 3, 10, KMedias.Comeco.MaisMais, semente).Custo / otimo;
        }

        Assert.True(Math.Abs(maisMais - sorteado) / 30 < 0.02, "a diferenca deveria quase sumir");
    }

    [Fact]
    public void ComDezTentativasEleQuaseSempreAchaOOtimo()
    {
        var acertos = 0;
        for (ulong semente = 1; semente <= 30; semente++)
        {
            var nuvem = Nuvem.Uniforme(10, semente);
            var (_, otimo) = Otimo.Melhor(nuvem, 3);
            if (Math.Abs(KMedias.Melhor(nuvem, 3, 10, KMedias.Comeco.MaisMais, semente).Custo - otimo) < 1e-9)
                acertos++;
        }
        Assert.True(acertos >= 25, $"achou so {acertos} de 30");
    }

    [Fact]
    public void ONumeroDeGruposPrecisaCaberNaNuvem()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => KMedias.Rodar(Nuvem.Uniforme(3, 1), 5, 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => KMedias.Rodar(Nuvem.Uniforme(3, 1), 0, 1));
    }

    [Fact]
    public void ComUmGrupoSoOCentroEhAMedia()
    {
        var nuvem = new Nuvem([(0, 0), (2, 0), (4, 0)]);
        var saida = KMedias.Rodar(nuvem, 1, 1);
        Assert.Equal(2, saida.Centros[0].X, 1e-9);
        Assert.Equal(0, saida.Centros[0].Y, 1e-9);
    }

    /// <summary>
    /// Na nuvem facil, com tres grupos bem separados, ele acha o agrupamento
    /// certo. Um algoritmo que erra aqui esta quebrado.
    /// </summary>
    [Fact]
    public void NaNuvemFacilEleAcerta()
    {
        var nuvem = Nuvem.TresGrupos(15, 7);
        var saida = KMedias.Melhor(nuvem, 3, 10);

        for (var grupo = 0; grupo < 3; grupo++)
        {
            var inicio = grupo * 15;
            var rotulo = saida.Rotulos[inicio];
            for (var i = inicio; i < inicio + 15; i++) Assert.Equal(rotulo, saida.Rotulos[i]);
        }
    }

    /// <summary>
    /// O caso em que ele acerta o CRITERIO e erra a resposta: o agrupamento que
    /// o olho escolheria tem custo maior pela soma dos quadrados.
    /// </summary>
    [Fact]
    public void OAgrupamentoObvioTemCustoMaiorNoCasoDesbalanceado()
    {
        var nuvem = Nuvem.GrandeEPequeno(5);

        var obvio = new int[nuvem.Quantos];
        for (var i = 100; i < nuvem.Quantos; i++) obvio[i] = 1;

        var achado = KMedias.Melhor(nuvem, 2, 30).Custo;
        Assert.True(achado < Otimo.Custo(nuvem, obvio),
                    "o k-medias deveria achar algo com custo MENOR que o agrupamento obvio");
    }

    [Fact]
    public void ENesseCasoEleMisturaOsDoisGrupos()
    {
        var nuvem = Nuvem.GrandeEPequeno(5);
        var saida = KMedias.Melhor(nuvem, 2, 30);

        var grupoDoPequeno = saida.Rotulos[105];
        var espalhadosJunto = Enumerable.Range(0, 100).Count(i => saida.Rotulos[i] == grupoDoPequeno);

        Assert.True(espalhadosJunto > 10, $"so {espalhadosJunto} dos espalhados foram junto");
    }
}
