namespace Conde.Agrupamento;

/// <summary>
/// O sorteio com semente, escrito por extenso: o Random do .NET nao promete a
/// mesma sequencia entre versoes nem entre sistemas, e a CI roda em tres.
/// </summary>
public sealed class Aleatorio
{
    private ulong estado;

    public Aleatorio(ulong semente) => estado = semente * 2 + 1;

    public ulong Proximo()
    {
        estado = unchecked(estado * 6364136223846793005UL + 1442695040888963407UL);
        return estado >> 17;
    }

    public int Entre(int baixo, int alto) => baixo + (int)(Proximo() % (ulong)(alto - baixo + 1));

    /// <summary>Um real entre zero e um.</summary>
    public double Fracao() => Entre(0, 1_000_000) / 1_000_000.0;
}

/// <summary>
/// Uma NUVEM de pontos no plano.
///
/// Duas dimensoes de proposito. O assunto deste repositorio e o criterio e o
/// algoritmo, nao a dimensao, e com dois eixos o resultado da para desenhar
/// numa folha e conferir com o olho, que e uma conferencia que mais dimensoes
/// tiram.
/// </summary>
public sealed class Nuvem
{
    public Nuvem(IEnumerable<(double X, double Y)> pontos)
    {
        ArgumentNullException.ThrowIfNull(pontos);
        Pontos = [.. pontos];
    }

    public IReadOnlyList<(double X, double Y)> Pontos { get; }

    public int Quantos => Pontos.Count;

    public static double Distancia((double X, double Y) a, (double X, double Y) b)
    {
        var dx = a.X - b.X;
        var dy = a.Y - b.Y;
        return Math.Sqrt(dx * dx + dy * dy);
    }

    /// <summary>
    /// O quadrado da distancia, que e o que o k-medias realmente minimiza.
    ///
    /// Essa diferenca nao e detalhe. Minimizar a soma das distancias e um
    /// problema diferente, com outra resposta: o ponto que minimiza a soma dos
    /// QUADRADOS e a media, e o que minimiza a soma das distancias e a mediana
    /// geometrica, que nem tem formula fechada. O k-medias so e simples porque
    /// usa o quadrado.
    /// </summary>
    public static double DistanciaQuadrada((double X, double Y) a, (double X, double Y) b)
    {
        var dx = a.X - b.X;
        var dy = a.Y - b.Y;
        return dx * dx + dy * dy;
    }

    /// <summary>
    /// Tres grupos bem separados, que e o caso facil.
    ///
    /// Ele existe para ser a referencia de cima: um algoritmo que erra AQUI esta
    /// quebrado, e um que acerta so aqui nao provou nada.
    /// </summary>
    public static Nuvem TresGrupos(int porGrupo, ulong semente)
    {
        var sorte = new Aleatorio(semente);
        var pontos = new List<(double, double)>();

        foreach (var (cx, cy) in new[] { (0.0, 0.0), (10.0, 0.0), (5.0, 9.0) })
            for (var i = 0; i < porGrupo; i++)
                pontos.Add((cx + sorte.Fracao() * 2 - 1, cy + sorte.Fracao() * 2 - 1));

        return new Nuvem(pontos);
    }

    /// <summary>
    /// Dois GRUPOS DE TAMANHOS MUITO DIFERENTES, que e onde o k-medias erra por
    /// construcao.
    ///
    /// Ele minimiza a soma dos quadrados, e um grupo grande contribui com muitos
    /// quadrados. A resposta que minimiza a conta corta o grupo grande ao meio e
    /// junta o pequeno com um pedaco dele, o que esta CERTO pelo criterio e
    /// errado para quem olha.
    /// </summary>
    public static Nuvem GrandeEPequeno(ulong semente)
    {
        var sorte = new Aleatorio(semente);
        var pontos = new List<(double, double)>();

        // Os numeros aqui foram ajustados ate o efeito aparecer, e a conta diz
        // por que. Com os 100 pontos espalhados por uma faixa de 100 de largura,
        // o agrupamento obvio custa uns 83 mil; cortar a faixa ao meio e deixar
        // o grupo pequeno junto com uma das metades custa uns 31 mil. A primeira
        // versao tinha a faixa com 20 de largura, o grupo pequeno custava caro
        // demais para ser adotado, e o k-medias acertava.
        for (var i = 0; i < 100; i++) pontos.Add((sorte.Fracao() * 100 - 50, sorte.Fracao() * 2 - 1));
        for (var i = 0; i < 10; i++) pontos.Add((60 + sorte.Fracao() * 2 - 1, sorte.Fracao() * 2 - 1));

        return new Nuvem(pontos);
    }

    /// <summary>
    /// Dois ANEIS concentricos: o k-medias nao tem como acertar, e o DBSCAN
    /// acerta sem esforco.
    ///
    /// A razao e que o k-medias so sabe fazer grupos CONVEXOS, porque cada ponto
    /// vai para o centro mais perto e isso divide o plano em pedacos retos. Um
    /// anel em volta de outro nao e convexo, e nenhuma escolha de dois centros
    /// separa os dois.
    ///
    /// O numero de pontos de cada anel e PROPORCIONAL AO RAIO, e isso me
    /// corrigiu. A primeira versao punha a mesma quantidade nos dois, o que
    /// parece justo e nao e: o anel de fora tem quatro vezes a circunferencia do
    /// de dentro, entao com a mesma contagem ele fica quatro vezes mais ralo. O
    /// DBSCAN agrupa por DENSIDADE, e nenhum raio servia para os dois ao mesmo
    /// tempo: com raio pequeno o anel de fora virava onze pedacos, com raio
    /// grande os dois viravam um so.
    ///
    /// Com a densidade igual nos dois, ele acerta com folga. A licao e sobre o
    /// algoritmo: DBSCAN com um raio so supoe densidade parecida, e essa
    /// suposicao e tao forte quanto a convexidade que o k-medias supoe.
    /// </summary>
    public static Nuvem Aneis(int porUnidadeDeRaio, ulong semente)
    {
        var sorte = new Aleatorio(semente);
        var pontos = new List<(double, double)>();

        foreach (var raio in new[] { 2.0, 8.0 })
        {
            var quantos = (int)(porUnidadeDeRaio * raio);
            for (var i = 0; i < quantos; i++)
            {
                var angulo = sorte.Fracao() * 2 * Math.PI;
                var r = raio + sorte.Fracao() * 0.6 - 0.3;
                pontos.Add((r * Math.Cos(angulo), r * Math.Sin(angulo)));
            }
        }

        return new Nuvem(pontos);
    }

    /// <summary>Quantos pontos o anel de dentro tem, para os testes conferirem.</summary>
    public static int PontosDoAnelDeDentro(int porUnidadeDeRaio) => (int)(porUnidadeDeRaio * 2.0);

    /// <summary>
    /// Um grupo denso com RUIDO espalhado em volta.
    ///
    /// E o caso que separa o DBSCAN dos outros dois: ele e o unico que tem a
    /// opcao de nao agrupar um ponto. O k-medias e o hierarquico sao obrigados a
    /// dar um grupo a cada ponto, inclusive aos que claramente nao pertencem a
    /// nenhum.
    /// </summary>
    public static Nuvem ComRuido(int densos, int ruido, ulong semente)
    {
        var sorte = new Aleatorio(semente);
        var pontos = new List<(double, double)>();

        for (var i = 0; i < densos; i++) pontos.Add((sorte.Fracao() * 2 - 1, sorte.Fracao() * 2 - 1));
        for (var i = 0; i < ruido; i++) pontos.Add((sorte.Fracao() * 20 - 10, sorte.Fracao() * 20 - 10));

        return new Nuvem(pontos);
    }

    /// <summary>Pontos sorteados sem estrutura nenhuma.</summary>
    public static Nuvem Uniforme(int quantos, ulong semente)
    {
        var sorte = new Aleatorio(semente);
        var pontos = new List<(double, double)>();
        for (var i = 0; i < quantos; i++) pontos.Add((sorte.Fracao() * 10, sorte.Fracao() * 10));
        return new Nuvem(pontos);
    }

    public override string ToString() => $"nuvem de {Quantos} pontos";
}
