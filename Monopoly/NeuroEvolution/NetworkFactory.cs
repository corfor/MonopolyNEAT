namespace Monopoly.NeuroEvolution;

public class NetworkFactory
{
    public static readonly NetworkFactory Instance = new();

    private NetworkFactory()
    {
    }

    public static Genotype CreateBaseGenotype(int inputs, int outputs)
    {
        var network = new Genotype();

        for (var i = 0; i < inputs; i++)
            network.AddVertex(VertexType.Input, i);

        for (var i = 0; i < outputs; i++)
            network.AddVertex(VertexType.Output, i + inputs);

        network.AddEdge(0, inputs, 0.0f, true, 0);

        //int innovation = 0;
        //
        //for (int i = 0; i < inputs; i++)
        //{
        //    for (int j = 0; j < outputs; j++)
        //    {
        //        int input = i;
        //        int output = j + inputs;
        //
        //        network.AddEdge(input, output, 0.0f, true, innovation);
        //
        //        innovation++;
        //    }
        //}

        return network;
    }

    public static void RegisterBaseMarkings(int inputs, int outputs)
    {
        for (var i = 0; i < inputs; i++)
        for (var j = 0; j < outputs; j++)
        {
            int output = j + inputs;

            var info = new EdgeInfo(i, output, 0.0f, true);

            Mutation.Instance.RegisterMarking(info);
        }
    }

    public Genotype CreateBaseRecurrent()
    {
        var network = new Genotype();

        var nodeNum = 0;

        for (var i = 0; i < 1; i++)
        {
            network.AddVertex(VertexType.Input, nodeNum);
            nodeNum++;
        }

        for (var i = 0; i < 1; i++)
        {
            network.AddVertex(VertexType.Output, nodeNum);
            nodeNum++;
        }

        network.AddEdge(0, 1, 0.0f, true, 0);
        network.AddEdge(1, 0, 0.0f, true, 1);

        var physicals = new Phenotype();
        physicals.InscribeGenotype(network);
        physicals.ProcessGraph();

        return network;
    }

    public Genotype CreateBuggyNetwork()
    {
        var network = new Genotype();

        var nodeNum = 0;

        for (var i = 0; i < 2; i++)
        {
            network.AddVertex(VertexType.Input, nodeNum);
            nodeNum++;
        }

        for (var i = 0; i < 1; i++)
        {
            network.AddVertex(VertexType.Output, nodeNum);
            nodeNum++;
        }

        for (var i = 0; i < 2; i++)
        {
            network.AddVertex(VertexType.Hidden, nodeNum);
            nodeNum++;
        }

        network.AddEdge(0, 2, 0.0f, true, 0);
        network.AddEdge(1, 2, 0.0f, true, 1);
        network.AddEdge(1, 3, 0.0f, true, 2);
        network.AddEdge(3, 2, 0.0f, true, 3);

        var physicals = new Phenotype();
        physicals.InscribeGenotype(network);
        physicals.ProcessGraph();

        return network;
    }

    public Phenotype CreateBasePhenotype(int inputs, int outputs)
    {
        var network = new Phenotype();

        for (var i = 0; i < inputs; i++)
            network.AddVertex(VertexType.Input, i);

        for (var i = 0; i < outputs; i++)
            network.AddVertex(VertexType.Output, i + inputs);

        for (var i = 0; i < inputs; i++)
        for (var j = 0; j < outputs; j++)
        {
            int input = i;
            int output = j + inputs;

            network.AddEdge(input, output, 0.0f, true);
        }

        return network;
    }
}