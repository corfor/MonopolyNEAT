using System;
using System.Collections.Generic;
using System.Linq;

namespace Monopoly.NeuroEvolution;

public class Species
{
    public readonly List<Genotype> Members = new();

    public Species(Genotype member = default)
    {
        if (member != default)
            Members.Add(member);
    }

    public int Staleness { get; set; }
    public float TopFitness { get; set; }
    public float FitnessSum { get; private set; }

    public Genotype Breed()
    {
        var roll = (float) RandomNumberGenerator.Instance.Random.NextDouble();

        if (roll < Crossover.CrossoverChance && Members.Count > 1)
        {
            int s1 = RandomNumberGenerator.Instance.Random.Next(0, Members.Count);
            int s2 = RandomNumberGenerator.Instance.Random.Next(0, Members.Count - 1);

            if (s2 >= s1)
                s2++;

            if (s1 > s2)
                (s1, s2) = (s2, s1);

            Genotype child = Crossover.ProduceOffspring(Members[s1], Members[s2]);
            Mutation.Instance.MutateAll(child);

            return child;
        }
        else
        {
            int selection = RandomNumberGenerator.Instance.Random.Next(0, Members.Count);

            Genotype child = Members[selection].Clone();
            Mutation.Instance.MutateAll(child);

            return child;
        }
    }

    public void SortMembers()
    {
        Members.Sort(SortGenotypeByFitness);
    }

    private static int SortGenotypeByFitness(Genotype a, Genotype b)
    {
        if (a.AdjustedFitness > b.AdjustedFitness)
            return -1;
        return Math.Abs(a.AdjustedFitness - b.AdjustedFitness) < Constants.Tolerance ? 0 : 1;
    }

    public void CullToPortion(float portion)
    {
        if (Members.Count <= 1)
            return;

        var remaining = (int) Math.Ceiling(Members.Count * portion);
        Members.RemoveRange(remaining, Members.Count - remaining);
    }

    public void CullToOne()
    {
        if (Members.Count <= 1)
            return;

        Members.RemoveRange(1, Members.Count - 1);
    }

    public void CalculateAdjustedFitnessSum()
    {
        var sum = 0.0f;
        int membersCount = Members.Count;

        for (var i = 0; i < membersCount; i++)
            sum += Members[i].AdjustedFitness;

        FitnessSum = sum;
    }
}

public class Population
{
    private const int MaxStaleness = 15;

    private const float Portion = 0.2f;
    public static readonly Population Instance = new();
    public readonly List<Genotype> Genetics = new();
    public readonly List<Phenotype> Phenotypes = new();

    public readonly List<Species> Species = new();

    private int _populationSize = 256;

    public int Generation;

    private Population()
    {
    }

    public void GenerateBasePopulation(int size, int inputs, int outputs)
    {
        _populationSize = size;

        for (var i = 0; i < _populationSize; i++)
        {
            Genotype genotype = NetworkFactory.CreateBaseGenotype(inputs, outputs);
            Genetics.Add(genotype);

            AddToSpecies(genotype);
        }

        NetworkFactory.RegisterBaseMarkings(inputs, outputs);

        for (var i = 0; i < _populationSize; i++)
            Mutation.Instance.MutateAll(Genetics[i]);

        InscribePopulation();
    }

    public void NewGeneration()
    {
        CalculateAdjustedFitness();

        for (var i = 0; i < Species.Count; i++)
        {
            Species[i].SortMembers();
            Species[i].CullToPortion(Portion);

            if (Species[i].Members.Count <= 1)
            {
                Species.RemoveAt(i);
                i--;
            }
        }

        UpdateStaleness();

        var fitnessSum = 0.0f;

        foreach (Species t in Species)
        {
            t.CalculateAdjustedFitnessSum();
            fitnessSum += t.FitnessSum;
        }

        var children = new List<Genotype>();

        foreach (Species t in Species)
        {
            int build = (int) (_populationSize * (t.FitnessSum / fitnessSum)) - 1;

            for (var j = 0; j < build; j++)
            {
                Genotype child = t.Breed();
                children.Add(child);
            }
        }

        while (_populationSize > Species.Count + children.Count)
        {
            Genotype child = Species[RandomNumberGenerator.Instance.Random.Next(0, Species.Count)].Breed();
            children.Add(child);
        }

        foreach (Species t in Species)
            t.CullToOne();

        int childrenCount = children.Count;

        for (var i = 0; i < childrenCount; i++)
            AddToSpecies(children[i]);

        Genetics.Clear();

        foreach (Genotype t in Species.SelectMany(t1 => t1.Members))
            Genetics.Add(t);

        InscribePopulation();

        Generation++;
    }

    private void CalculateAdjustedFitness()
    {
        int speciesCount = Species.Count;

        for (var i = 0; i < speciesCount; i++)
        {
            int membersCount = Species[i].Members.Count;

            for (var j = 0; j < membersCount; j++)
                Species[i].Members[j].AdjustedFitness = Species[i].Members[j].Fitness / membersCount;
        }
    }

    private void UpdateStaleness()
    {
        int speciesCount = Species.Count;

        for (var i = 0; i < speciesCount; i++)
        {
            if (speciesCount == 1)
                return;

            float top = Species[i].Members[0].Fitness;

            if (Species[i].TopFitness < top)
            {
                Species[i].TopFitness = top;
                Species[i].Staleness = 0;
            }
            else
            {
                Species[i].Staleness++;
            }

            if (Species[i].Staleness >= MaxStaleness)
            {
                Species.RemoveAt(i);
                i--;
                speciesCount--;
            }
        }
    }

    public void InscribePopulation()
    {
        Phenotypes.Clear();

        for (var i = 0; i < _populationSize; i++)
        {
            Genetics[i].Fitness = 0.0f;
            Genetics[i].AdjustedFitness = 0.0f;

            var physical = new Phenotype();
            physical.InscribeGenotype(Genetics[i]);
            physical.ProcessGraph();

            Phenotypes.Add(physical);
        }
    }

    private void AddToSpecies(Genotype genotype)
    {
        if (Species.Count == 0)
        {
            var newSpecies = new Species(genotype);

            Species.Add(newSpecies);
        }
        else
        {
            int speciesCount = Species.Count;

            var found = false;

            for (var i = 0; i < speciesCount; i++)
            {
                float distance = Crossover.SpeciationDistance(Species[i].Members[0], genotype);

                if (distance >= Crossover.Distance)
                    continue;
                Species[i].Members.Add(genotype);
                found = true;
                break;
            }

            if (found)
                return;

            var newSpecies = new Species(genotype);

            Species.Add(newSpecies);
        }
    }

    public static int SortGenotypeByFitness(Genotype a, Genotype b)
    {
        if (a.Fitness > b.Fitness)
            return -1;
        return Math.Abs(a.Fitness - b.Fitness) < Constants.Tolerance ? 0 : 1;
    }
}