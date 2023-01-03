using System;
using System.Collections.Generic;
using System.Linq;

namespace Monopoly.NeuroEvolution;

public class Crossover
{
    private const float C1 = 1.0f;
    private const float C2 = 1.0f;
    private const float C3 = 0.4f;

    public const float CrossoverChance = 0.75f;
    public const float Distance = 1.0f;
    public static readonly Crossover Instance = new();

    private Crossover()
    {
    }

    public static Genotype ProduceOffspring(Genotype first, Genotype second)
    {
        var copyFirst = new List<EdgeInfo>();
        var copySecond = new List<EdgeInfo>();

        copyFirst.AddRange(first.Edges);
        copySecond.AddRange(second.Edges);

        var match_first = new List<EdgeInfo>();
        var match_second = new List<EdgeInfo>();

        var disjoint_first = new List<EdgeInfo>();
        // ReSharper disable once CollectionNeverQueried.Local
        var disjointSecond = new List<EdgeInfo>();

        var excessFirst = new List<EdgeInfo>();
        // ReSharper disable once CollectionNeverQueried.Local
        var excessSecond = new List<EdgeInfo>();

        int genesFirst = first.Edges.Count;
        int genesSecond = second.Edges.Count;

        int innovationMaxFirst = first.Edges[^1].Innovation;
        int innovationMaxSecond = second.Edges[^1].Innovation;

        int innovationMin = innovationMaxFirst > innovationMaxSecond ? innovationMaxSecond : innovationMaxFirst;

        for (var i = 0; i < genesFirst; i++)
        for (var j = 0; j < genesSecond; j++)
        {
            EdgeInfo infoFirst = copyFirst[i];
            EdgeInfo infoSecond = copySecond[j];

            //matching genes
            if (infoFirst.Innovation != infoSecond.Innovation)
                continue;
            match_first.Add(infoFirst);
            match_second.Add(infoSecond);

            copyFirst.Remove(infoFirst);
            copySecond.Remove(infoSecond);

            i--;
            genesFirst--;
            genesSecond--;
            break;
        }

        foreach (EdgeInfo t in copyFirst)
            if (t.Innovation > innovationMin)
                excessFirst.Add(t);
            else
                disjoint_first.Add(t);

        foreach (EdgeInfo t in copySecond)
            if (t.Innovation > innovationMin)
                excessSecond.Add(t);
            else
                disjointSecond.Add(t);

        var child = new Genotype();

        int matching = match_first.Count;

        for (var i = 0; i < matching; i++)
        {
            int roll = RandomNumberGenerator.Instance.Random.Next(0, 2);

            if (roll == 0 || !match_second[i].Enabled)
                child.AddEdge(match_first[i].Source, match_first[i].Destination, match_first[i].Weight, match_first[i].Enabled, match_first[i].Innovation);
            else
                child.AddEdge(match_second[i].Source, match_second[i].Destination, match_second[i].Weight, match_second[i].Enabled, match_second[i].Innovation);
        }

        foreach (EdgeInfo t in disjoint_first)
            child.AddEdge(t.Source, t.Destination, t.Weight, t.Enabled, t.Innovation);

        foreach (EdgeInfo t in excessFirst)
            child.AddEdge(t.Source, t.Destination, t.Weight, t.Enabled, t.Innovation);

        child.SortEdges();

        var ends = new List<int>();

        foreach (VertexInfo vertex in first.Vertices.TakeWhile(vertex => vertex.Type != VertexType.Hidden))
        {
            ends.Add(vertex.Index);
            child.AddVertex(vertex.Type, vertex.Index);
        }

        AddUniqueVertices(child, ends);

        child.SortVertices();

        return child;
    }

    private static void AddUniqueVertices(Genotype genotype, ICollection<int> ends)
    {
        var unique = new List<int>();

        int edgeCount = genotype.Edges.Count;

        for (var i = 0; i < edgeCount; i++)
        {
            EdgeInfo info = genotype.Edges[i];

            if (!ends.Contains(info.Source) && !unique.Contains(info.Source))
                unique.Add(info.Source);

            if (!ends.Contains(info.Destination) && !unique.Contains(info.Destination))
                unique.Add(info.Destination);
        }

        int uniques = unique.Count;

        for (var i = 0; i < uniques; i++)
            genotype.AddVertex(VertexType.Hidden, unique[i]);
    }

    public static float SpeciationDistance(Genotype first, Genotype second)
    {
        var copyFirst = new List<EdgeInfo>();
        var copySecond = new List<EdgeInfo>();

        copyFirst.AddRange(first.Edges);
        copySecond.AddRange(second.Edges);

        var matchFirst = new List<EdgeInfo>();
        // ReSharper disable once CollectionNeverQueried.Local
        var matchSecond = new List<EdgeInfo>();

        var disjointFirst = new List<EdgeInfo>();
        var disjointSecond = new List<EdgeInfo>();

        var excessFirst = new List<EdgeInfo>();
        var excessSecond = new List<EdgeInfo>();

        int genesFirst = first.Edges.Count;
        int genesSecond = second.Edges.Count;

        int innovationMaxFirst = first.Edges[^1].Innovation;
        int innovationMaxSecond = second.Edges[^1].Innovation;

        int invmin = innovationMaxFirst > innovationMaxSecond ? innovationMaxSecond : innovationMaxFirst;

        var diff = 0.0f;

        for (var i = 0; i < genesFirst; i++)
        for (var j = 0; j < genesSecond; j++)
        {
            EdgeInfo infoFirst = copyFirst[i];
            EdgeInfo infoSecond = copySecond[j];

            //matching genes
            if (infoFirst.Innovation != infoSecond.Innovation)
                continue;
            float weightDiff = Math.Abs(infoFirst.Weight - infoSecond.Weight);
            diff += weightDiff;

            matchFirst.Add(infoFirst);
            matchSecond.Add(infoSecond);

            copyFirst.Remove(infoFirst);
            copySecond.Remove(infoSecond);

            i--;
            genesFirst--;
            genesSecond--;
            break;
        }

        foreach (EdgeInfo t in copyFirst)
            if (t.Innovation > invmin)
                excessFirst.Add(t);
            else
                disjointFirst.Add(t);

        foreach (EdgeInfo t in copySecond)
            if (t.Innovation > invmin)
                excessSecond.Add(t);
            else
                disjointSecond.Add(t);

        int match = matchFirst.Count;
        int disjoint = disjointFirst.Count + disjointSecond.Count;
        int excess = excessFirst.Count + excessSecond.Count;

        int n = Math.Max(first.Edges.Count, second.Edges.Count);

        float e = excess / (float) n;
        float d = disjoint / (float) n;
        float w = diff / match;

        return e * C1 + d * C2 + w * C3;
    }
}