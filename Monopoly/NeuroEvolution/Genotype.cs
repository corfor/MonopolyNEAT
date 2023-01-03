using System.Collections.Generic;

namespace Monopoly.NeuroEvolution;

public class VertexInfo
{
    public readonly int Index;

    public readonly VertexType Type;

    public VertexInfo(VertexType t, int i)
    {
        Type = t;
        Index = i;
    }
}

public class EdgeInfo
{
    public readonly int Destination;

    //structural information
    public readonly int Source;
    public bool Enabled;

    public int Innovation;

    //network information
    public float Weight;

    public EdgeInfo(int s, int d, float w, bool e)
    {
        Source = s;
        Destination = d;

        Weight = w;
        Enabled = e;
    }
}

public class Genotype
{
    public readonly List<EdgeInfo> Edges = new();

    public readonly List<VertexInfo> Vertices = new();
    public float AdjustedFitness = 0.0f;

    public int Bracket = 0;

    public float Fitness = 0.0f;

    public void AddVertex(VertexType type, int index)
    {
        var v = new VertexInfo(type, index);
        Vertices.Add(v);

        if (v.Type != VertexType.Hidden)
        {
        }

        if (v.Type == VertexType.Input)
        {
        }
    }

    public void AddEdge(int source, int destination, float weight, bool enabled)
    {
        var e = new EdgeInfo(source, destination, weight, enabled);
        Edges.Add(e);
    }

    public void AddEdge(int source, int destination, float weight, bool enabled, int innovation)
    {
        var e = new EdgeInfo(source, destination, weight, enabled)
        {
            Innovation = innovation
        };
        Edges.Add(e);
    }

    public Genotype Clone()
    {
        var copy = new Genotype();

        int vertexCount = Vertices.Count;

        for (var i = 0; i < vertexCount; i++)
            copy.AddVertex(Vertices[i].Type, Vertices[i].Index);

        int edgeCount = Edges.Count;

        for (var i = 0; i < edgeCount; i++)
            copy.AddEdge(Edges[i].Source, Edges[i].Destination, Edges[i].Weight, Edges[i].Enabled, Edges[i].Innovation);

        return copy;
    }

    public void SortTopology()
    {
        SortVertices();
        SortEdges();
    }

    public void SortVertices()
    {
        Vertices.Sort(CompareVertexByOrder);
    }

    public void SortEdges()
    {
        Edges.Sort(CompareEdgeByInnovation);
    }

    private static int CompareVertexByOrder(VertexInfo a, VertexInfo b)
    {
        if (a.Index > b.Index)
            return 1;
        if (a.Index == b.Index)
            return 0;

        return -1;
    }

    private static int CompareEdgeByInnovation(EdgeInfo a, EdgeInfo b)
    {
        if (a.Innovation > b.Innovation)
            return 1;
        if (a.Innovation == b.Innovation)
            return 0;

        return -1;
    }
}