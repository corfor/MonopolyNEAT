using Monopoly.NeuroEvolution;

namespace Monopoly.Game;

public class NeuralPlayer : Player
{
    public NetworkAdapter Adapter;
    public Phenotype Network;

    public override BuyDecision DecideBuy(int index)
    {
        float[] y = Network.Propagate(Adapter.Pack);

        return y[0] > 0.5f ? BuyDecision.Buy : BuyDecision.Auction;
    }

    public override JailDecision DecideJail()
    {
        float[] Y = Network.Propagate(Adapter.Pack);

        return Y[1] switch
        {
            < 0.333f => JailDecision.Card,
            < 0.666f => JailDecision.Roll,
            _ => JailDecision.Pay
        };
    }

    public override Decision DecideMortgage(int index)
    {
        float[] y = Network.Propagate(Adapter.Pack);

        return y[2] > 0.5f ? Decision.Yes : Decision.No;
    }

    public override Decision DecideAdvance(int index)
    {
        float[] y = Network.Propagate(Adapter.Pack);

        return y[3] > 0.5f ? Decision.Yes : Decision.No;
    }

    public override int DecideAuctionBid(int index)
    {
        float[] y = Network.Propagate(Adapter.Pack);

        float result = y[4];
        float money = NetworkAdapter.ConvertMoneyValue(result);

        Analytics.Instance.MakeBid(index, (int) money);

        return (int) money;
    }

    public override int DecideBuildHouse(int set)
    {
        float[] y = Network.Propagate(Adapter.Pack);

        float result = y[5];
        float money = NetworkAdapter.ConvertHouseValue(result);

        return (int) money;
    }

    public override int DecideSellHouse(int set)
    {
        float[] y = Network.Propagate(Adapter.Pack);

        float result = y[6];
        float money = NetworkAdapter.ConvertHouseValue(result);

        return (int) money;
    }

    public override Decision DecideOfferTrade()
    {
        float[] y = Network.Propagate(Adapter.Pack);

        return y[7] > 0.5f ? Decision.Yes : Decision.No;
    }

    public override Decision DecideAcceptTrade()
    {
        float[] y = Network.Propagate(Adapter.Pack);

        return y[8] > 0.5f ? Decision.Yes : Decision.No;
    }
}