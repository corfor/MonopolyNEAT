using System.Collections.Generic;

namespace Monopoly.Game;

public class Player
{
    public enum BuyDecision
    {
        Buy,
        Auction
    }

    public enum Decision
    {
        Yes,
        No
    }

    public enum JailDecision
    {
        Roll,
        Pay,
        Card
    }

    public enum PlayerState
    {
        Normal,
        Jail,
        Retired
    }

    public readonly List<int> Items = new();

    public int Card = 0;
    public int Doub = 0;
    public int Funds = 1500;

    public int Jail = 0;

    public int Position = 0;

    public PlayerState State = PlayerState.Normal;

    public virtual BuyDecision DecideBuy(int index)
    {
        return BuyDecision.Buy;
    }

    public virtual JailDecision DecideJail()
    {
        return JailDecision.Roll;
    }

    public virtual Decision DecideMortgage(int index)
    {
        return Funds < 0 ? Decision.Yes : Decision.No;
    }

    public virtual Decision DecideAdvance(int index)
    {
        return Decision.Yes;
    }

    public virtual int DecideAuctionBid(int index)
    {
        return Board.Costs[index];
    }

    public virtual int DecideBuildHouse(int set)
    {
        return 15;
    }

    public virtual int DecideSellHouse(int set)
    {
        return Funds < 0 ? 15 : 0;
    }

    public virtual Decision DecideOfferTrade()
    {
        return Decision.No;
    }

    public virtual Decision DecideAcceptTrade()
    {
        return Decision.No;
    }
}