using Monopoly.Game;
using Monopoly.NeuroEvolution;

namespace Monopoly;

public class Analytics
{
    public static readonly Analytics Instance = new();
    private readonly float[] _average;

    private readonly int[] _bids;
    private readonly int[] _money;
    private readonly float[] _price;

    private readonly int[] _trades;
    public readonly float[] Ratio;

    public readonly int[] Wins;
    private int _max;

    private Analytics()
    {
        _bids = new int[Constants.BoardLength];
        _money = new int[Constants.BoardLength];
        _average = new float[Constants.BoardLength];
        _price = new float[Constants.BoardLength];

        _trades = new int[Constants.BoardLength];

        Wins = new int[Constants.BoardLength];
        Ratio = new float[Constants.BoardLength];
    }

    public void MakeBid(int index, int bid)
    {
        _bids[index]++;
        _money[index] += bid;
        // ReSharper disable once PossibleLossOfFraction
        _average[index] = _money[index] / _bids[index];
        _price[index] = _average[index] / Board.Costs[index];
    }

    public void MadeTrade(int index)
    {
        _trades[index]++;
    }

    public void MarkWin(int index)
    {
        Wins[index]++;

        if (_max < Wins[index])
            _max = Wins[index];

        int tempMin = int.MaxValue;

        for (var i = 0; i < Constants.BoardLength; i++)
            if (Wins[i] != 0 && Wins[i] < tempMin)
                tempMin = Wins[i];

        Ratio[index] = (Wins[index] - tempMin) / (float) (_max - tempMin);
    }
}