using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using Monopoly.NeuroEvolution;

namespace Monopoly;

public static class Program
{
    private const int TournamentsToPlay = 1000;

    private const char DelimMain = ';';
    private const char DelimComma = ',';

    public static void Main(string[] args)
    {
        const string path = "C:\\Git\\MonopolyNEAT\\monopoly_population 162.txt";

        var tournament = new Tournament();

        if (File.Exists(path))
            LoadState(path, ref tournament);
        else
            Tournament.Initialise();

        for (var i = 0; i < TournamentsToPlay; i++)
        {
            tournament.ExecuteTournament();
            Population.Instance.NewGeneration();
            SaveState(path, tournament);
        }
    }

    private static void SaveState(string target, Tournament tournament)
    {
        Console.WriteLine("SAVING POPULATION");
        var sb = new StringBuilder();

        sb.Append(Population.Instance.Generation);
        sb.Append(DelimMain);
        sb.Append(tournament.ChampionScore.ToString(CultureInfo.InvariantCulture));
        sb.Append(DelimMain);

        var markings = 0;

        //save markings
        for (var i = 0; i < Mutation.Instance.Historical.Count; i++)
        {
            sb.Append(Mutation.Instance.Historical[i].Order);
            sb.Append(DelimComma);

            sb.Append(Mutation.Instance.Historical[i].Source);
            sb.Append(DelimComma);

            sb.Append(Mutation.Instance.Historical[i].Destination);

            if (i != Mutation.Instance.Historical.Count - 1)
                sb.Append(DelimComma);

            markings++;
        }

        var netBuild = new List<string>();
        var geneCount = 0;

        sb.Append(DelimMain);
        var build = sb.ToString();

        //save networks, species by species
        for (var i = 0; i < Population.Instance.Species.Count; i++)
        {
            sb.Clear();

            sb.Append(Population.Instance.Species[i].TopFitness.ToString(CultureInfo.InvariantCulture));
            sb.Append(DelimComma);
            sb.Append(Population.Instance.Species[i].Staleness);

            sb.Append('&');

            netBuild.Add(sb.ToString());
            int members = Population.Instance.Species[i].Members.Count;

            for (var j = 0; j < members; j++)
            {
                sb.Clear();
                geneCount++;

                Console.WriteLine($"{geneCount}/{Population.Instance.Genetics.Count}");

                Genotype genes = Population.Instance.Species[i].Members[j];

                int vertices = genes.Vertices.Count;

                for (var k = 0; k < vertices; k++)
                {
                    sb.Append(genes.Vertices[k].Index);
                    sb.Append(DelimComma);
                    sb.Append(genes.Vertices[k].Type.ToString());
                    sb.Append(DelimComma);
                }

                sb.Append('#');

                int edges = genes.Edges.Count;

                for (var k = 0; k < edges; k++)
                {
                    sb.Append(genes.Edges[k].Source);
                    sb.Append(DelimComma);
                    sb.Append(genes.Edges[k].Destination);
                    sb.Append(DelimComma);
                    sb.Append(genes.Edges[k].Weight.ToString(CultureInfo.InvariantCulture));
                    sb.Append(DelimComma);
                    sb.Append(genes.Edges[k].Enabled);
                    sb.Append(DelimComma);
                    sb.Append(genes.Edges[k].Innovation);
                    sb.Append(DelimComma);
                }

                if (j != members - 1)
                    sb.Append('n');
            }

            if (i != Population.Instance.Species.Count - 1)
                sb.Append('&');

            netBuild.Add(sb.ToString());
        }

        using (var sw = new StreamWriter(target))
        {
            sw.Write(build);

            foreach (string b in netBuild)
                sw.Write(b);

            sw.Write(DelimMain);
        }

        Console.WriteLine($"{markings} MARKINGS");
    }

    private static void LoadState(string location, ref Tournament tournament)
    {
        string load;

        using (var sr = new StreamReader(location))
        {
            load = sr.ReadToEnd();
        }

        string[] parts = load.Split(DelimMain);

        var gen = int.Parse(parts[0]);
        var score = float.Parse(parts[1]);

        Population.Instance.Generation = gen;
        tournament.ChampionScore = score;

        string markingString = parts[2];
        string[] markingParts = markingString.Split(DelimComma);

        for (var i = 0; i < markingParts.GetLength(0); i += 3)
        {
            var order = int.Parse(markingParts[i]);
            var source = int.Parse(markingParts[i + 1]);
            var destination = int.Parse(markingParts[i + 2]);

            var recreation = new Marking
            {
                Order = order,
                Source = source,
                Destination = destination
            };

            Mutation.Instance.Historical.Add(recreation);
        }

        string networkString = parts[3];
        string[] speciesParts = networkString.Split('&');

        for (var x = 0; x < speciesParts.GetLength(0); x += 2)
        {
            string[] firstParts = speciesParts[x].Split(DelimComma);

            Population.Instance.Species.Add(new Species());
            Population.Instance.Species[^1].TopFitness = float.Parse(firstParts[0]);
            Population.Instance.Species[^1].Staleness = int.Parse(firstParts[1]);

            string[] networkParts = speciesParts[x + 1].Split('n');

            for (var i = 0; i < networkParts.GetLength(0); i++)
            {
                var genotype = new Genotype();

                string network = networkParts[i];
                string[] nparts = network.Split('#');

                string verts = nparts[0];
                string[] vparts = verts.Split(',');

                for (var j = 0; j < vparts.GetLength(0) - 1; j += 2)
                {
                    var index = int.Parse(vparts[j]);
                    string s = CultureInfo.InvariantCulture.TextInfo.ToTitleCase(vparts[j + 1].ToLowerInvariant()).Trim();
                    var type = (VertexType) Enum.Parse(typeof(VertexType), s);

                    genotype.AddVertex(type, index);
                }

                string edges = nparts[1];
                string[] eparts = edges.Split(',');

                for (var j = 0; j < eparts.GetLength(0) - 1; j += 5)
                {
                    var source = int.Parse(eparts[j]);
                    var destination = int.Parse(eparts[j + 1]);
                    var weight = float.Parse(eparts[j + 2]);
                    var enabled = bool.Parse(eparts[j + 3]);
                    var innovation = int.Parse(eparts[j + 4]);

                    genotype.AddEdge(source, destination, weight, enabled, innovation);
                }

                Population.Instance.Species[^1].Members.Add(genotype);
                Population.Instance.Genetics.Add(genotype);
            }
        }

        Population.Instance.InscribePopulation();
    }
}