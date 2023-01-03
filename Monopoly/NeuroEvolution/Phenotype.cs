using System;
using System.Collections.Generic;

namespace Monopoly.NeuroEvolution;

public class Vertex
{
    //structural information
    public readonly List<Edge> Incoming = new();

    public readonly VertexType Type;

    //output extraction
    public float Value;

    public Vertex(VertexType t)
    {
        Type = t;
    }
}

public class Edge
{
    public readonly int Destination;
    public readonly bool Enabled;

    //propagation information
    public readonly int Source;

    //network information
    public readonly float Weight;

    //structural information
    public EdgeType Type = EdgeType.Forward;

    public Edge(int s, int d, float w, bool e)
    {
        Source = s;
        Destination = d;

        Weight = w;
        Enabled = e;
    }
}

public class Phenotype
{
    // ReSharper disable once CollectionNeverQueried.Local
    private readonly List<Edge> _edges = new();
    private readonly List<Vertex> _vertices = new();

    private readonly List<Vertex> _verticesInputs = new();
    private readonly List<Vertex> _verticesOutputs = new();

    public float Score = 0;

    public void InscribeGenotype(Genotype code)
    {
        _vertices.Clear();
        _edges.Clear();

        int vertexCount = code.Vertices.Count;
        int edgeCount = code.Edges.Count;

        for (var i = 0; i < vertexCount; i++)
            //cast to int then to other enumerator type
            AddVertex((VertexType) (int) code.Vertices[i].Type, code.Vertices[i].Index);

        for (var i = 0; i < edgeCount; i++)
            AddEdge(code.Edges[i].Source, code.Edges[i].Destination, code.Edges[i].Weight, code.Edges[i].Enabled);
    }

    public void AddVertex(VertexType type, int index)
    {
        var v = new Vertex(type);
        _vertices.Add(v);
    }

    public void AddEdge(int source, int destination, float weight, bool enabled)
    {
        var e = new Edge(source, destination, weight, enabled);
        _edges.Add(e);

        _vertices[e.Destination].Incoming.Add(e);
    }

    public void ProcessGraph()
    {
        int verticesCount = _vertices.Count;

        //populate input and output sub-lists
        for (var i = 0; i < verticesCount; i++)
        {
            Vertex vertex = _vertices[i];

            switch (vertex.Type)
            {
                case VertexType.Input:
                    _verticesInputs.Add(vertex);
                    break;
                case VertexType.Output:
                    _verticesOutputs.Add(vertex);
                    break;
                case VertexType.Hidden:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }

    public void ResetGraph()
    {
        int verticesCount = _vertices.Count;

        for (var i = 0; i < verticesCount; i++)
        {
            Vertex vertex = _vertices[i];
            vertex.Value = 0.0f;
        }
    }

    public float[] Propagate(float[] X)
    {
        const int repeats = 10;

        for (var e = 0; e < repeats; e++)
        {
            for (var i = 0; i < _verticesInputs.Count; i++)
                _verticesInputs[i].Value = X[i];

            foreach (Vertex t in _vertices)
            {
                if (t.Type == VertexType.Output)
                    continue;

                int paths = t.Incoming.Count;

                for (var j = 0; j < paths; j++)
                    t.Value += _vertices[t.Incoming[j].Source].Value * t.Incoming[j].Weight * (t.Incoming[j].Enabled ? 1.0f : 0.0f);

                if (t.Incoming.Count > 0)
                    t.Value = Sigmoid(t.Value);
            }

            var y = new float[_verticesOutputs.Count];

            for (var i = 0; i < _verticesOutputs.Count; i++)
            {
                int paths = _verticesOutputs[i].Incoming.Count;

                for (var j = 0; j < paths; j++)
                    _verticesOutputs[i].Value += _vertices[_verticesOutputs[i].Incoming[j].Source].Value * _verticesOutputs[i].Incoming[j].Weight *
                                                 (_verticesOutputs[i].Incoming[j].Enabled ? 1.0f : 0.0f);

                if (_verticesOutputs[i].Incoming.Count <= 0)
                    continue;

                _verticesOutputs[i].Value = Sigmoid(_verticesOutputs[i].Value);
                y[i] = _verticesOutputs[i].Value;
            }

            if (e == repeats - 1)
                return y;
        }

        return Array.Empty<float>();
    }

    private static float Sigmoid(float x)
    {
        return 1.0f / (1.0f + (float) Math.Pow(Math.E, -1.0f * x));
    }
}