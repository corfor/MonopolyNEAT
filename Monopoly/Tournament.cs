using System;
using System.Collections.Generic;
using System.Threading;
using Monopoly.Game;
using Monopoly.NeuroEvolution;

namespace Monopoly;

public class Tournament
{
    private const int TournamentSize = 256;
    private const int RoundSize = 2000; //2000

    private readonly int _workers = Environment.ProcessorCount; //20
    private const int BatchSize = 20; //20

    private Genotype _champion;

    private List<Phenotype> _contestants = new();
    private List<Genotype> _genetics = new();
    public float ChampionScore;

    public static void Initialise()
    {
        const int inputs = 126;
        const int outputs = 9;

        Population.Instance.GenerateBasePopulation(TournamentSize, inputs, outputs);
    }

    public void ExecuteTournament()
    {
        Console.WriteLine("TOURNAMENT #" + Population.Instance.Generation);

        _contestants.Clear();
        _genetics.Clear();

        for (var i = 0; i < TournamentSize; i++)
        {
            Population.Instance.Genetics[i].Bracket = 0;
            Population.Instance.Phenotypes[i].Score = 0.0f;

            _contestants.Add(Population.Instance.Phenotypes[i]);
            _genetics.Add(Population.Instance.Genetics[i]);
        }

        while (_contestants.Count > 1)
            ExecuteTournamentRound();

        for (var i = 0; i < TournamentSize; i++)
        {
            var top = 0.0f;

            if (_champion != null)
                top = _champion.Bracket;

            float diff = Population.Instance.Genetics[i].Bracket - top;
            Population.Instance.Genetics[i].Fitness = ChampionScore + diff * 5;
        }

        _champion = _genetics[0];
        ChampionScore = _genetics[0].Fitness;
    }

    private void ExecuteTournamentRound()
    {
        Console.WriteLine("\tROUND SIZE " + _contestants.Count);

        var cs = new List<Phenotype>();
        var cs_g = new List<Genotype>();

        RandomNumberGenerator.Instance.DoubleShuffle(_contestants, _genetics, ref cs, ref cs_g);

        for (var i = 0; i < TournamentSize; i++)
            Population.Instance.Phenotypes[i].Score = 0.0f;

        _contestants = cs;
        _genetics = cs_g;

        for (var i = 0; i < _contestants.Count; i += 4)
        {
            var played = 0;

            Console.WriteLine("\t\tBRACKET (" + i / 4 + ")");

            while (played < RoundSize)
            {
                Console.WriteLine($"\t\t\tInitialised {_workers} Workers");

                var workers = new Thread[_workers];

                for (var t = 0; t < _workers; t++)
                {
                    int i1 = i;
                    workers[t] = new Thread(() => PlayGameThread(this, i1));
                    workers[t].Start();
                }

                for (var t = 0; t < _workers; t++)
                    workers[t].Join();

                played += _workers * BatchSize;

                for (var c = 0; c < Constants.BoardLength; c++)
                    Console.WriteLine($"\t\t\t\tindex: {c}, {Analytics.Instance.Ratio[c]:0.000}");
            }

            var mi = 0;
            float ms = _contestants[i].Score;

            for (var j = 1; j < 4; j++)
                if (ms < _contestants[i + j].Score)
                {
                    mi = j;
                    ms = _contestants[i + j].Score;
                }

            for (var j = 0; j < 4; j++)
            {
                if (j == mi)
                {
                    _genetics[i + j].Bracket++;
                    continue;
                }

                _contestants[i + j] = null;
            }
        }

        for (var i = 0; i < _contestants.Count; i++)
            if (_contestants[i] == null)
            {
                _contestants.RemoveAt(i);
                _genetics.RemoveAt(i);
                i--;
            }
    }

    private static void PlayGameThread(Tournament instance, int i)
    {
        for (var game = 0; game < BatchSize; game++)
        {
            var adapter = new NetworkAdapter();
            var board = new Board(adapter);

            board.players[0].Network = instance._contestants[i];
            board.players[1].Network = instance._contestants[i + 1];
            board.players[2].Network = instance._contestants[i + 2];
            board.players[3].Network = instance._contestants[i + 3];

            board.players[0].Adapter = adapter;
            board.players[1].Adapter = adapter;
            board.players[2].Adapter = adapter;
            board.players[3].Adapter = adapter;

            board.players = RandomNumberGenerator.Instance.Shuffle(board.players);

            Board.OutcomeType outcomeType = Board.OutcomeType.Ongoing;

            while (outcomeType == Board.OutcomeType.Ongoing)
                outcomeType = board.Step();

            switch (outcomeType)
            {
                case Board.OutcomeType.Win1:
                {
                    MarkWinner(board, 0);
                    break;
                }
                case Board.OutcomeType.Win2:
                {
                    MarkWinner(board, 1);
                    break;
                }
                case Board.OutcomeType.Win3:
                {
                    MarkWinner(board, 2);
                    break;
                }
                case Board.OutcomeType.Win4:
                {
                    MarkWinner(board, 3);
                    break;
                }
                case Board.OutcomeType.Draw:
                {
                    lock (board.players)
                    {
                        board.players[0].Network.Score += 0.25f;
                        board.players[1].Network.Score += 0.25f;
                        board.players[2].Network.Score += 0.25f;
                        board.players[3].Network.Score += 0.25f;
                    }

                    break;
                }
                case Board.OutcomeType.Ongoing:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }

    private static void MarkWinner(Board board, int winner)
    {
        lock (board.players[winner].Network)
        {
            board.players[winner].Network.Score += 1.0f;
        }

        foreach (int t in board.players[winner].Items)
            lock (Analytics.Instance.Wins)
            {
                Analytics.Instance.MarkWin(t);
            }
    }
}