using System.Collections.Generic;

namespace Monopoly.NeuroEvolution;

public class Marking
{
    public int Destination;
    public int Order;
    public int Source;
}

public class Mutation
{
    //chances
    private const float PetrubChance = 0.9f;
    private const float ShiftStep = 0.1f;
    public static readonly Mutation Instance = new();

    public readonly List<Marking> Historical = new();
    private float _mutateDisable = 0.2f;
    private float _mutateEnable = 0.6f;

    private float _mutateLink = 0.2f;
    private float _mutateNode = 0.1f;
    private float _mutateWeight = 2.0f;

    public int RegisterMarking(EdgeInfo info)
    {
        int count = Historical.Count;

        for (var i = 0; i < count; i++)
        {
            Marking marking = Historical[i];

            if (marking.Source == info.Source && marking.Destination == info.Destination)
                return marking.Order;
        }

        var creation = new Marking
        {
            Order = Historical.Count,
            Source = info.Source,
            Destination = info.Destination
        };

        Historical.Add(creation);

        return Historical.Count - 1;
    }

    public void MutateAll(Genotype genotype)
    {
        _mutateLink = 0.2f;
        _mutateNode = 0.1f;
        _mutateEnable = 0.6f;
        _mutateDisable = 0.2f;
        _mutateWeight = 2.0f;

        float p = _mutateWeight;

        while (p > 0)
        {
            var roll = (float) RandomNumberGenerator.Instance.Random.NextDouble();

            if (roll < p)
                MutateWeight(genotype);

            p--;
        }

        p = _mutateLink;

        while (p > 0)
        {
            var roll = (float) RandomNumberGenerator.Instance.Random.NextDouble();

            if (roll < p)
                MutateLink(genotype);

            p--;
        }

        p = _mutateNode;

        while (p > 0)
        {
            var roll = (float) RandomNumberGenerator.Instance.Random.NextDouble();

            if (roll < p)
                MutateNode(genotype);

            p--;
        }

        p = _mutateDisable;

        while (p > 0)
        {
            var roll = (float) RandomNumberGenerator.Instance.Random.NextDouble();

            if (roll < p)
                MutateDisable(genotype);

            p--;
        }

        p = _mutateEnable;

        while (p > 0)
        {
            var roll = (float) RandomNumberGenerator.Instance.Random.NextDouble();

            if (roll < p)
                MutateEnable(genotype);

            p--;
        }
    }

    private void MutateLink(Genotype genotype)
    {
        int vertexCount = genotype.Vertices.Count;
        int edgeCount = genotype.Edges.Count;

        var potential = new List<EdgeInfo>();

        //gather all possible potential edges
        for (var i = 0; i < vertexCount; i++)
        for (var j = 0; j < vertexCount; j++)
        {
            int source = genotype.Vertices[i].Index;
            int destination = genotype.Vertices[j].Index;

            VertexType t1 = genotype.Vertices[i].Type;
            VertexType t2 = genotype.Vertices[j].Type;

            if (t1 == VertexType.Output || t2 == VertexType.Input)
                continue;

            if (source == destination)
                continue;

            var search = false;

            //match edge
            for (var k = 0; k < edgeCount; k++)
            {
                EdgeInfo edge = genotype.Edges[k];

                if (edge.Source == source && edge.Destination == destination)
                {
                    search = true;
                    break;
                }
            }

            if (!search)
            {
                float weight = (float) RandomNumberGenerator.Instance.Random.NextDouble() * 4.0f - 2.0f;
                var creation = new EdgeInfo(source, destination, weight, true);

                potential.Add(creation);
            }
        }

        if (potential.Count <= 0)
            return;

        int selection = RandomNumberGenerator.Instance.Random.Next(0, potential.Count);

        EdgeInfo mutation = potential[selection];
        mutation.Innovation = RegisterMarking(mutation);

        genotype.AddEdge(mutation.Source, mutation.Destination, mutation.Weight, mutation.Enabled, mutation.Innovation);
    }

    private void MutateNode(Genotype genotype)
    {
        int edgeCount = genotype.Edges.Count;

        int selection = RandomNumberGenerator.Instance.Random.Next(0, edgeCount);

        EdgeInfo edge = genotype.Edges[selection];

        if (edge.Enabled == false)
            return;

        edge.Enabled = false;

        int vertex_new = genotype.Vertices[^1].Index + 1;

        var vertex = new VertexInfo(VertexType.Hidden, vertex_new);

        var first = new EdgeInfo(edge.Source, vertex_new, 1.0f, true);
        var second = new EdgeInfo(vertex_new, edge.Destination, edge.Weight, true);

        first.Innovation = RegisterMarking(first);
        second.Innovation = RegisterMarking(second);

        genotype.AddVertex(vertex.Type, vertex.Index);

        genotype.AddEdge(first.Source, first.Destination, first.Weight, first.Enabled, first.Innovation);
        genotype.AddEdge(second.Source, second.Destination, second.Weight, second.Enabled, second.Innovation);
    }

    private static void MutateEnable(Genotype genotype)
    {
        int edgeCount = genotype.Edges.Count;

        var candidates = new List<EdgeInfo>();

        for (var i = 0; i < edgeCount; i++)
            if (!genotype.Edges[i].Enabled)
                candidates.Add(genotype.Edges[i]);

        if (candidates.Count == 0)
            return;

        int selection = RandomNumberGenerator.Instance.Random.Next(0, candidates.Count);

        EdgeInfo edge = candidates[selection];
        edge.Enabled = true;
    }

    private static void MutateDisable(Genotype genotype)
    {
        int edgeCount = genotype.Edges.Count;

        var candidates = new List<EdgeInfo>();

        for (var i = 0; i < edgeCount; i++)
            if (genotype.Edges[i].Enabled)
                candidates.Add(genotype.Edges[i]);

        if (candidates.Count == 0)
            return;

        int selection = RandomNumberGenerator.Instance.Random.Next(0, candidates.Count);

        EdgeInfo edge = candidates[selection];
        edge.Enabled = false;
    }

    private static void MutateWeight(Genotype genotype)
    {
        int selection = RandomNumberGenerator.Instance.Random.Next(0, genotype.Edges.Count);

        EdgeInfo edge = genotype.Edges[selection];

        var roll = (float) RandomNumberGenerator.Instance.Random.NextDouble();

        if (roll < PetrubChance)
            MutateWeightShift(edge, ShiftStep);
        else
            MutateWeightRandom(edge);
    }

    private static void MutateWeightShift(EdgeInfo edge, float step)
    {
        float scalar = (float) RandomNumberGenerator.Instance.Random.NextDouble() * step - step * 0.5f;
        edge.Weight += scalar;
    }

    private static void MutateWeightRandom(EdgeInfo edge)
    {
        float value = (float) RandomNumberGenerator.Instance.Random.NextDouble() * 4.0f - 2.0f;
        edge.Weight = value;
    }
}