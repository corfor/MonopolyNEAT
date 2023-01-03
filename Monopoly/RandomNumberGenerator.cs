using System;
using System.Collections.Generic;
using Monopoly.Game;
using Monopoly.NeuroEvolution;

namespace Monopoly;

public class RandomNumberGenerator
{
    public static readonly RandomNumberGenerator Instance = new();
    public readonly Random Random = new();

    public List<Board.CardEntry> Shuffle(List<Board.CardEntry> cards)
    {
        var shuffle = new List<Board.CardEntry>();

        for (var i = 0; i < cards.Count;)
        {
            int r = Random.Next(0, cards.Count);
            shuffle.Add(cards[r]);
            cards.RemoveAt(r);
        }

        return shuffle;
    }

    public NeuralPlayer[] Shuffle(IEnumerable<NeuralPlayer> list)
    {
        var container = new List<NeuralPlayer>(list);
        var shuffle = new List<NeuralPlayer>();

        for (var i = 0; i < container.Count;)
        {
            int r = Random.Next(0, container.Count);
            shuffle.Add(container[r]);
            container.RemoveAt(r);
        }

        return shuffle.ToArray();
    }

    public void DoubleShuffle(List<Phenotype> phen, List<Genotype> gene, ref List<Phenotype> op, ref List<Genotype> og)
    {
        for (var i = 0; i < phen.Count;)
        {
            int r = Random.Next(0, phen.Count);
            op.Add(phen[r]);
            og.Add(gene[r]);
            phen.RemoveAt(r);
            gene.RemoveAt(r);
        }
    }
}