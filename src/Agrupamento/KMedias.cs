namespace Conde.Agrupamento;

/// <summary>
/// O k-MEDIAS de Lloyd, de 1957: escolhe k centros, manda cada ponto para o mais
/// perto, recalcula os centros como a media de quem foi, e repete.
///
/// Ele e o algoritmo de agrupamento mais usado do mundo e tem tres coisas que
/// quase nunca sao ditas juntas:
///
///   1. ele NAO acha o otimo, e nem tenta: ele acha um minimo local
///   2. qual minimo local depende inteiramente de onde os centros comecaram
///   3. ele sempre PARA, porque cada passo diminui o custo e ha um numero
///      finito de reparticoes
///
/// A terceira e a razao de ele ser confiavel; a segunda e a razao de quase toda
/// biblioteca rodar ele dez vezes e ficar com o melhor.
/// </summary>
public static class KMedias
{
    /// <summary>O que uma rodada encontrou.</summary>
    public sealed class Saida
    {
        public Saida(int[] rotulos, (double X, double Y)[] centros, double custo, int voltas, bool fechou)
        {
            Rotulos = rotulos;
            Centros = centros;
            Custo = custo;
            Voltas = voltas;
            Fechou = fechou;
        }

        public int[] Rotulos { get; }
        public (double X, double Y)[] Centros { get; }
        public double Custo { get; }

        /// <summary>Quantas vezes os pontos foram redistribuidos.</summary>
        public int Voltas { get; }

        /// <summary>Se o algoritmo parou sozinho, sem estourar o limite.</summary>
        public bool Fechou { get; }

        public override string ToString() => $"custo {Custo:N3} em {Voltas} voltas";
    }

    /// <summary>Como escolher os centros iniciais.</summary>
    public enum Comeco
    {
        /// <summary>k pontos sorteados. E o que o algoritmo original faz.</summary>
        Sorteado,

        /// <summary>
        /// k-medias++, de Arthur e Vassilvitskii, 2007: o primeiro centro e
        /// sorteado e cada seguinte e sorteado com peso PROPORCIONAL AO QUADRADO
        /// da distancia ao centro mais perto ja escolhido.
        ///
        /// A ideia e espalhar os centros sem fixa-los: ponto longe de tudo tem
        /// muito mais chance de ser escolhido, e ponto no meio de um grupo que ja
        /// tem centro quase nao tem. Ela compra uma garantia de verdade, de que
        /// o custo esperado fica dentro de um fator logaritmico do otimo, e a
        /// medida mostra que na pratica ela compra bem mais que isso.
        /// </summary>
        MaisMais,

        /// <summary>Os k primeiros pontos. Deterministico e geralmente ruim.</summary>
        Primeiros,
    }

    /// <summary>Roda uma vez, a partir dos centros que o comeco escolher.</summary>
    public static Saida Rodar(Nuvem nuvem, int grupos, ulong semente,
                              Comeco comeco = Comeco.MaisMais, int limite = 300)
    {
        ArgumentNullException.ThrowIfNull(nuvem);
        if (grupos <= 0 || grupos > nuvem.Quantos)
            throw new ArgumentOutOfRangeException(nameof(grupos));

        var sorte = new Aleatorio(semente);
        var centros = Escolher(nuvem, grupos, sorte, comeco);
        var rotulos = new int[nuvem.Quantos];
        Array.Fill(rotulos, -1);

        var voltas = 0;
        var fechou = false;

        for (var passo = 0; passo < limite; passo++)
        {
            voltas++;
            var mudou = false;

            for (var i = 0; i < nuvem.Quantos; i++)
            {
                var melhor = 0;
                var menor = double.MaxValue;
                for (var g = 0; g < grupos; g++)
                {
                    var d = Nuvem.DistanciaQuadrada(nuvem.Pontos[i], centros[g]);
                    if (d >= menor) continue;
                    menor = d;
                    melhor = g;
                }
                if (rotulos[i] == melhor) continue;
                rotulos[i] = melhor;
                mudou = true;
            }

            if (!mudou) { fechou = true; break; }

            for (var g = 0; g < grupos; g++)
            {
                var membros = Enumerable.Range(0, nuvem.Quantos).Where(i => rotulos[i] == g).ToList();

                // Grupo VAZIO acontece, e o que fazer com ele e uma decisao que
                // muda o resultado. Deixar o centro parado onde estava e a
                // escolha mais quieta: ela nao inventa um grupo onde nao ha
                // ponto, e permite que o centro volte a ganhar pontos numa
                // volta seguinte.
                if (membros.Count == 0) continue;

                centros[g] = (membros.Average(i => nuvem.Pontos[i].X),
                              membros.Average(i => nuvem.Pontos[i].Y));
            }
        }

        return new Saida(rotulos, centros, Otimo.Custo(nuvem, rotulos), voltas, fechou);
    }

    /// <summary>
    /// Roda varias vezes com sementes diferentes e fica com a melhor.
    ///
    /// E o que toda biblioteca faz, e a medida deste repositorio mostra quanto
    /// isso compra: com comeco sorteado, muito; com k-medias++, bem menos,
    /// porque o comeco ja e bom.
    /// </summary>
    public static Saida Melhor(Nuvem nuvem, int grupos, int tentativas,
                               Comeco comeco = Comeco.MaisMais, ulong semente = 1)
    {
        Saida? melhor = null;
        for (var i = 0; i < tentativas; i++)
        {
            var saida = Rodar(nuvem, grupos, semente + (ulong)i * 977, comeco);
            if (melhor is null || saida.Custo < melhor.Custo) melhor = saida;
        }
        return melhor!;
    }

    private static (double X, double Y)[] Escolher(Nuvem nuvem, int grupos, Aleatorio sorte, Comeco comeco)
    {
        var centros = new (double X, double Y)[grupos];

        if (comeco == Comeco.Primeiros)
        {
            for (var g = 0; g < grupos; g++) centros[g] = nuvem.Pontos[g];
            return centros;
        }

        if (comeco == Comeco.Sorteado)
        {
            var escolhidos = new HashSet<int>();
            for (var g = 0; g < grupos; g++)
            {
                int indice;
                do { indice = sorte.Entre(0, nuvem.Quantos - 1); } while (!escolhidos.Add(indice));
                centros[g] = nuvem.Pontos[indice];
            }
            return centros;
        }

        centros[0] = nuvem.Pontos[sorte.Entre(0, nuvem.Quantos - 1)];

        for (var g = 1; g < grupos; g++)
        {
            var pesos = new double[nuvem.Quantos];
            double total = 0;

            for (var i = 0; i < nuvem.Quantos; i++)
            {
                var menor = double.MaxValue;
                for (var j = 0; j < g; j++)
                    menor = Math.Min(menor, Nuvem.DistanciaQuadrada(nuvem.Pontos[i], centros[j]));
                pesos[i] = menor;
                total += menor;
            }

            if (total <= 0)
            {
                centros[g] = nuvem.Pontos[sorte.Entre(0, nuvem.Quantos - 1)];
                continue;
            }

            var alvo = sorte.Fracao() * total;
            double acumulado = 0;
            for (var i = 0; i < nuvem.Quantos; i++)
            {
                acumulado += pesos[i];
                if (acumulado < alvo) continue;
                centros[g] = nuvem.Pontos[i];
                break;
            }
        }

        return centros;
    }
}
