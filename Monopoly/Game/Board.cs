using System;
using System.Collections.Generic;
using System.Linq;
using Monopoly.NeuroEvolution;

namespace Monopoly.Game;

public class Board
{
    public enum CardType
    {
        Advance,
        Railroad2,
        Utility10,
        Reward,
        Card,
        Back3,
        Jail,
        Repairs,
        Street,
        Fine,
        Chairman,
        Birthday
    }

    public enum OutcomeType
    {
        Ongoing,
        Draw,
        Win1,
        Win2,
        Win3,
        Win4
    }

    private const EMode Mode = EMode.Roll;

    //penalties for landing on a property (all circumstances)
    private static readonly int[,] PropertyPenalties =
    {
        {2, 10, 30, 90, 160, 250},
        {4, 20, 60, 180, 320, 450},
        {6, 30, 90, 270, 400, 550},
        {8, 40, 100, 300, 450, 600},
        {10, 50, 150, 450, 625, 750},
        {12, 60, 180, 500, 700, 900},
        {14, 70, 200, 550, 750, 950},
        {16, 80, 220, 600, 800, 1000},
        {18, 90, 250, 700, 875, 1050},
        {20, 100, 300, 750, 925, 1100},
        {22, 110, 330, 800, 975, 1150},
        {22, 120, 360, 850, 1025, 1200},
        {26, 130, 390, 900, 1100, 1275},
        {28, 150, 450, 1000, 1200, 1400},
        {35, 175, 500, 1100, 1300, 1500},
        {50, 200, 600, 1400, 1700, 2000}
    };

    //penalties for landing on utilities (needs to be multiplied by roll)
    private static readonly int[] UtilityPosiions = {12, 28};
    private static readonly int[] UtilityPenalties = {4, 10};

    //penalties for landing on trains
    private static readonly int[] TrainPositions = {5, 15, 25, 35};
    private static readonly int[] TrainPenalties = {25, 50, 100, 200};

    private static readonly TileType[] Types =
    {
        TileType.None, TileType.Property, TileType.Chest, TileType.Property, TileType.Tax, TileType.Train, TileType.Property, TileType.Chance, TileType.Property, TileType.Property,
        TileType.None,
        TileType.Property, TileType.Utility, TileType.Property, TileType.Property, TileType.Train, TileType.Property, TileType.Chest, TileType.Property, TileType.Property,
        TileType.None,
        TileType.Property, TileType.Chance, TileType.Property, TileType.Property, TileType.Train, TileType.Property, TileType.Property, TileType.Utility, TileType.Property,
        TileType.Jail,
        TileType.Property, TileType.Property, TileType.Chest, TileType.Property, TileType.Train, TileType.Chance, TileType.Property, TileType.Tax, TileType.Property
    };

    public static readonly int[] Costs =
    {
        0, 60, 0, 60, 200, 200, 100, 0, 100, 120, 0, 140, 150, 140, 160, 200, 180, 0, 180, 200, 0, 220, 0, 220, 240, 200, 260, 260, 150, 280, 0, 300, 300, 0, 320, 200, 0, 250, 100,
        400
    };

    private static readonly int[] Build = {50, 50, 50, 50, 100, 100, 100, 100, 150, 150, 150, 150, 200, 200, 200, 200};

    private static readonly int[,] Sets =
    {
        {1, 3, -1},
        {6, 8, 9},
        {11, 13, 14},
        {16, 18, 19},
        {21, 23, 24},
        {26, 27, 29},
        {31, 32, 34},
        {37, 39, -1}
    };

    private readonly NetworkAdapter _adapter;

    //card stacks
    //--------------------
    private readonly List<CardEntry> _chance;
    private readonly List<CardEntry> _chest;

    private readonly int[] _houses = {0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0};

    //--------------------

    //board states
    //--------------------
    private readonly bool[] _mortgaged =
    {
        false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false,
        false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false
    };

    private readonly int[] _original =
        {-1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1};

    private readonly int[] _owners =
        {-1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1};

    private readonly int[] _property =
        {-1, 0, -1, 1, -1, -1, 2, -1, 2, 3, -1, 4, -1, 4, 5, -1, 6, -1, 6, 7, -1, 8, -1, 8, 9, -1, 10, 10, -1, 11, -1, 12, 12, -1, 13, -1, -1, 14, -1, 15};

    private readonly RandomNumberGenerator _randomNumberGenerator;

    private int _count;

    private int _lastRoll;
    private int _remaining;

    private int _turn;

    public NeuralPlayer[] players;
    //--------------------  

    public Board(NetworkAdapter adapter)
    {
        players = new NeuralPlayer[Constants.PlayerCount];
        _randomNumberGenerator = new RandomNumberGenerator();

        _adapter = adapter;

        for (var i = 0; i < Constants.PlayerCount; i++)
        {
            players[i] = new NeuralPlayer();

            _adapter.SetPosition(i, players[i].Position);
            _adapter.SetMoney(i, players[i].Funds);
        }

        _remaining = Constants.PlayerCount;

        _chance = new List<CardEntry>();
        _chest = new List<CardEntry>();

        //chance
        //--------------------
        _chance.Add(new CardEntry(CardType.Advance, 39));
        _chance.Add(new CardEntry(CardType.Advance, 0));
        _chance.Add(new CardEntry(CardType.Advance, 24));
        _chance.Add(new CardEntry(CardType.Advance, 11));
        _chance.Add(new CardEntry(CardType.Railroad2, 0));
        _chance.Add(new CardEntry(CardType.Railroad2, 0));
        _chance.Add(new CardEntry(CardType.Utility10, 0));
        _chance.Add(new CardEntry(CardType.Reward, 50));
        _chance.Add(new CardEntry(CardType.Card, 0));
        _chance.Add(new CardEntry(CardType.Back3, 0));
        _chance.Add(new CardEntry(CardType.Jail, 0));
        _chance.Add(new CardEntry(CardType.Repairs, 0));
        _chance.Add(new CardEntry(CardType.Fine, 15));
        _chance.Add(new CardEntry(CardType.Advance, 5));
        _chance.Add(new CardEntry(CardType.Chairman, 0));
        _chance.Add(new CardEntry(CardType.Reward, 150));
        _chance = _randomNumberGenerator.Shuffle(_chance);
        //--------------------

        //chest
        //--------------------
        _chest.Add(new CardEntry(CardType.Advance, 0));
        _chest.Add(new CardEntry(CardType.Reward, 200));
        _chest.Add(new CardEntry(CardType.Fine, 50));
        _chest.Add(new CardEntry(CardType.Reward, 50));
        _chest.Add(new CardEntry(CardType.Card, 0));
        _chest.Add(new CardEntry(CardType.Jail, 0));
        _chest.Add(new CardEntry(CardType.Reward, 100));
        _chest.Add(new CardEntry(CardType.Reward, 20));
        _chest.Add(new CardEntry(CardType.Birthday, 0));
        _chest.Add(new CardEntry(CardType.Reward, 100));
        _chest.Add(new CardEntry(CardType.Fine, 100));
        _chest.Add(new CardEntry(CardType.Fine, 50));
        _chest.Add(new CardEntry(CardType.Fine, 25));
        _chest.Add(new CardEntry(CardType.Street, 0));
        _chest.Add(new CardEntry(CardType.Reward, 10));
        _chest.Add(new CardEntry(CardType.Reward, 100));
        _chest = _randomNumberGenerator.Shuffle(_chest);
        //--------------------
    }

    public OutcomeType Step()
    {
        return Mode switch
        {
            EMode.Roll => Roll(),
            _ => throw new ArgumentOutOfRangeException()
        };
    }

    private OutcomeType Roll()
    {
        BeforeTurn();

        int d1 = _randomNumberGenerator.Random.Next(1, 7);
        int d2 = _randomNumberGenerator.Random.Next(1, 7);

        _lastRoll = d1 + d2;

        bool isDouble = d1 == d2;
        var doubleInJail = false;

        if (players[_turn].State == Player.PlayerState.Jail)
        {
            _adapter.SetTurn(_turn);
            Player.JailDecision decision = players[_turn].DecideJail();

            switch (decision)
            {
                //regular jail state
                case Player.JailDecision.Roll when isDouble:
                    players[_turn].Jail = 0;
                    players[_turn].State = Player.PlayerState.Normal;

                    _adapter.SetJail(_turn, 0);

                    doubleInJail = true;
                    break;
                case Player.JailDecision.Roll:
                {
                    players[_turn].Jail++;

                    if (players[_turn].Jail >= 3)
                    {
                        Payment(_turn, Constants.JailPenalty);

                        players[_turn].Jail = 0;
                        players[_turn].State = Player.PlayerState.Normal;

                        _adapter.SetJail(_turn, 0);
                    }

                    break;
                }
                case Player.JailDecision.Pay:
                    Payment(_turn, Constants.JailPenalty);

                    players[_turn].Jail = 0;
                    players[_turn].State = Player.PlayerState.Normal;

                    _adapter.SetJail(_turn, 0);
                    break;
                case Player.JailDecision.Card when players[_turn].Card > 0:
                    players[_turn].Card--;
                    players[_turn].Jail = 0;
                    players[_turn].State = Player.PlayerState.Normal;

                    _adapter.SetJail(_turn, 0);
                    _adapter.SetCard(_turn, players[_turn].Card > 0 ? 1 : 0);
                    break;
                //run regular jail state
                case Player.JailDecision.Card when isDouble:
                    players[_turn].Jail = 0;
                    players[_turn].State = Player.PlayerState.Normal;

                    _adapter.SetJail(_turn, 0);
                    break;
                case Player.JailDecision.Card:
                {
                    players[_turn].Jail++;

                    if (players[_turn].Jail >= 3)
                    {
                        Payment(_turn, Constants.JailPenalty);

                        players[_turn].Jail = 0;
                        players[_turn].State = Player.PlayerState.Normal;

                        _adapter.SetJail(_turn, 0);
                    }

                    break;
                }
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        if (players[_turn].State == Player.PlayerState.Normal)
        {
            bool notFinalDouble = !isDouble || players[_turn].Doub <= 1;

            if (notFinalDouble)
                Movement(d1 + d2);
        }

        //start turn again (unless retired or the double was from jail)
        if (players[_turn].State != Player.PlayerState.Retired && isDouble && !doubleInJail)
        {
            players[_turn].Doub++;

            if (players[_turn].Doub >= 3)
            {
                players[_turn].Position = Constants.JailIndex;
                players[_turn].Doub = 0;
                players[_turn].State = Player.PlayerState.Jail;

                _adapter.SetJail(_turn, 1);
            }
        }

        OutcomeType outcomeType = EndTurn(!isDouble || players[_turn].State == Player.PlayerState.Retired || players[_turn].State == Player.PlayerState.Jail);
        return outcomeType;
    }

    private OutcomeType EndTurn(bool increment = true)
    {
        if (increment)
        {
            IncrementTurn();

            var count = 0;

            while (players[_turn].State == Player.PlayerState.Retired && count <= Constants.PlayerCount * 2)
            {
                IncrementTurn();
                count++;
            }

            if (_remaining <= 1)
                switch (_turn)
                {
                    case 0: return OutcomeType.Win1;
                    case 1: return OutcomeType.Win2;
                    case 2: return OutcomeType.Win3;
                    case 3: return OutcomeType.Win4;
                }
        }

        _count++;

        return _count >= Constants.StalemateTurn
            ? OutcomeType.Draw
            : OutcomeType.Ongoing;
    }

    private void IncrementTurn()
    {
        _turn++;

        if (_turn >= Constants.PlayerCount)
            _turn = 0;
    }

    private void BeforeTurn()
    {
        if (players[_turn].State == Player.PlayerState.Retired)
            return;

        int itemCount = players[_turn].Items.Count;

        for (var j = 0; j < itemCount; j++)
        {
            int index = players[_turn].Items[j];

            if (_mortgaged[index])
            {
                var advancePrice = (int) (Costs[index] * Constants.MortgageInterest);

                if (advancePrice > players[_turn].Funds)
                    continue;

                _adapter.SetTurn(_turn);

                _adapter.SetSelectionState(index, 1);

                Player.Decision decision = players[_turn].DecideAdvance(index);

                _adapter.SetSelectionState(index, 0);

                if (decision == Player.Decision.Yes)
                    Advance(index);
            }
            else
            {
                _adapter.SetTurn(_turn);

                _adapter.SetSelectionState(index, 1);

                Player.Decision decision = players[_turn].DecideMortgage(index);

                _adapter.SetSelectionState(index, 0);

                if (decision == Player.Decision.Yes)
                {
                    //Mortgage(index);
                }
            }
        }

        int[] sets = FindSets(_turn);
        int setCount = sets.GetLength(0);

        for (var j = 0; j < setCount; j++)
        {
            int houseTotal = _houses[Sets[sets[j], 0]] + _houses[Sets[sets[j], 1]];

            if (sets[j] != 0 && sets[j] != 7)
                houseTotal += _houses[Sets[sets[j], 2]];

            int sellMax = houseTotal;

            _adapter.SetTurn(_turn);

            _adapter.SetSelectionState(Sets[sets[j], 0], 1);

            int decision = players[_turn].DecideSellHouse(sets[j]);

            _adapter.SetSelectionState(Sets[sets[j], 0], 0);

            decision = Math.Min(decision, sellMax);

            if (decision <= 0)
                continue;

            SellHouses(sets[j], decision);
            players[_turn].Funds += (int) (decision * Build[_property[Sets[sets[j], 0]]] * 0.5f);
        }

        sets = FindSets(_turn);
        setCount = sets.GetLength(0);

        for (var j = 0; j < setCount; j++)
        {
            var maxHouse = 10;
            int houseTotal = _houses[Sets[sets[j], 0]] + _houses[Sets[sets[j], 1]];

            if (sets[j] != 0 && sets[j] != 7)
            {
                maxHouse = 15;
                houseTotal += _houses[Sets[sets[j], 2]];
            }

            int buildMax = maxHouse - houseTotal;
            var affordMax = (int) Math.Floor(players[_turn].Funds / (float) Build[_property[Sets[sets[j], 0]]]);

            if (affordMax < 0)
                affordMax = 0;

            buildMax = Math.Min(buildMax, affordMax);

            _adapter.SetTurn(_turn);

            _adapter.SetSelectionState(Sets[sets[j], 0], 1);

            int decision = players[_turn].DecideBuildHouse(sets[j]);

            _adapter.SetSelectionState(Sets[sets[j], 0], 0);

            decision = Math.Min(decision, buildMax);

            if (decision <= 0)
                continue;
            BuildHouses(sets[j], decision);
            Payment(_turn, decision * Build[_property[Sets[sets[j], 0]]]);
        }

        Trading();
    }

    private void Trading()
    {
        var candidates = new List<Player>();
        var candidatesIndex = new List<int>();

        for (var i = 0; i < Constants.PlayerCount; i++)
        {
            if (i == _turn)
                continue;

            if (players[i].State == Player.PlayerState.Retired)
                continue;

            candidates.Add(players[i]);
            candidatesIndex.Add(i);
        }

        if (candidates.Count == 0)
            return;

        const int tradeAttempts = 4;
        const int tradeItemMax = 5;
        const int tradeMoneyMax = 500;

        for (var t = 0; t < tradeAttempts; t++)
        {
            int give = _randomNumberGenerator.Random.Next(0, Math.Min(players[_turn].Items.Count, tradeItemMax));

            int selectedPlayer = _randomNumberGenerator.Random.Next(0, candidates.Count);

            Player other = candidates[selectedPlayer];
            int other_index = candidatesIndex[selectedPlayer];

            int recieve = _randomNumberGenerator.Random.Next(0, Math.Min(other.Items.Count, tradeItemMax));

            if (players[_turn].Funds < 0 || other.Funds < 0)
                continue;

            int moneyGive = _randomNumberGenerator.Random.Next(0, Math.Min(players[_turn].Funds, tradeMoneyMax));
            int moneyRecieve = _randomNumberGenerator.Random.Next(0, Math.Min(other.Funds, tradeMoneyMax));
            int moneyBalance = moneyGive - moneyRecieve;

            if (give == 0 || recieve == 0)
                continue;

            var gift = new List<int>();
            var possible = new List<int>(players[_turn].Items);

            for (var i = 0; i < give; i++)
            {
                int selection = _randomNumberGenerator.Random.Next(0, possible.Count);

                gift.Add(possible[selection]);
                possible.RemoveAt(selection);
            }

            var returning = new List<int>();

            possible = new List<int>(other.Items);

            for (var i = 0; i < recieve; i++)
            {
                int selection = _randomNumberGenerator.Random.Next(0, possible.Count);

                returning.Add(possible[selection]);
                possible.RemoveAt(selection);
            }

            //set neurons for trade
            foreach (int t1 in gift)
                _adapter.SetSelectionState(t1, 1);

            foreach (int t1 in returning)
                _adapter.SetSelectionState(t1, 1);

            _adapter.SetMoneyContext(moneyBalance);

            Player.Decision decision = players[_turn].DecideOfferTrade();

            if (decision == Player.Decision.No)
            {
                _adapter.ClearSelectionState();
                continue;
            }

            Player.Decision decision2 = other.DecideAcceptTrade();

            if (decision2 == Player.Decision.No)
                continue;

            foreach (int t1 in gift)
            {
                Analytics.Instance.MadeTrade(t1);

                players[_turn].Items.Remove(t1);
                other.Items.Add(t1);

                _owners[t1] = other_index;
                _adapter.SetOwner(t1, other_index);
            }

            foreach (int t1 in returning)
            {
                Analytics.Instance.MadeTrade(t1);

                other.Items.Remove(t1);
                players[_turn].Items.Add(t1);

                _owners[t1] = _turn;
                _adapter.SetOwner(t1, _turn);
            }

            _adapter.ClearSelectionState();

            players[_turn].Funds -= moneyBalance;
            other.Funds += moneyBalance;
        }
    }

    private void Auction(int index)
    {
        var participation = new bool [Constants.PlayerCount];

        for (var i = 0; i < Constants.PlayerCount; i++)
            participation[i] = players[i].State != Player.PlayerState.Retired;

        var bids = new int [Constants.PlayerCount];

        for (var i = 0; i < Constants.PlayerCount; i++)
        {
            _adapter.SetTurn(i);

            _adapter.SetSelectionState(index, 1);

            bids[i] = players[i].DecideAuctionBid(index);

            _adapter.SetSelectionState(index, 0);

            if (bids[i] > players[i].Funds)
                participation[i] = false;
        }

        var max = 0;

        for (var i = 0; i < Constants.PlayerCount; i++)
            if (participation[i])
                if (bids[i] > max)
                    max = bids[i];

        var candidates = new List<int>();
        var backup = new List<int>();

        for (var i = 0; i < Constants.PlayerCount; i++)
        {
            if (participation[i])
                if (bids[i] == max)
                    candidates.Add(i);

            if (players[i].State != Player.PlayerState.Retired)
                backup.Add(i);
        }

        if (candidates.Count > 0)
        {
            int winner = candidates[_randomNumberGenerator.Random.Next(0, candidates.Count)];

            Payment(winner, max);

            _owners[index] = winner;
            players[winner].Items.Add(index);

            if (_original[index] == -1)
                _original[index] = winner;

            _adapter.SetOwner(index, winner);
        }
        else
        {
            int winner = backup[_randomNumberGenerator.Random.Next(0, backup.Count)];

            _owners[index] = winner;
            players[winner].Items.Add(index);

            if (_original[index] == -1)
                _original[index] = winner;

            _adapter.SetOwner(index, winner);
        }
    }

    private void Movement(int roll)
    {
        players[_turn].Position += roll;

        //wrap around
        if (players[_turn].Position >= Constants.BoardLength)
        {
            players[_turn].Position -= Constants.BoardLength;

            if (players[_turn].Position == 0)
                players[_turn].Funds += Constants.GoBonus;
            else
                players[_turn].Funds += Constants.GoLandingBonus;
        }

        _adapter.SetMoney(_turn, players[_turn].Funds);
        _adapter.SetPosition(_turn, players[_turn].Position);

        ActivateTile();
    }

    private void ActivateTile()
    {
        int index = players[_turn].Position;

        TileType tileType = Types[index];

        switch (tileType)
        {
            case TileType.Property:
            {
                int owner = Owner(index);

                if (owner == Constants.BankIndex)
                {
                    _adapter.SetTurn(_turn);
                    _adapter.SetSelection(index);
                    Player.BuyDecision decision = players[_turn].DecideBuy(index);

                    switch (decision)
                    {
                        case Player.BuyDecision.Buy when players[_turn].Funds < Costs[index]:
                            Auction(index);
                            break;
                        case Player.BuyDecision.Buy:
                        {
                            Payment(_turn, Costs[index]);

                            _owners[index] = _turn;

                            if (_original[index] == -1)
                                _original[index] = _turn;

                            players[_turn].Items.Add(index);

                            _adapter.SetOwner(index, owner);
                            break;
                        }
                        case Player.BuyDecision.Auction:
                            Auction(index);
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                }
                else if (owner == _turn)
                {
                    //do nothing
                }
                else if (!_mortgaged[index])
                {
                    PaymentToPlayer(_turn, owner, PropertyPenalties[_property[index], _houses[index]]);
                }

                break;
            }
            case TileType.Train:
            {
                int owner = Owner(index);

                if (owner == Constants.BankIndex)
                {
                    _adapter.SetTurn(_turn);
                    _adapter.SetSelection(index);
                    Player.BuyDecision decision = players[_turn].DecideBuy(index);

                    if (decision == Player.BuyDecision.Buy)
                    {
                        if (players[_turn].Funds < Costs[index])
                        {
                            Auction(index);
                        }
                        else
                        {
                            Payment(_turn, Costs[index]);

                            _owners[index] = _turn;

                            if (_original[index] == -1)
                                _original[index] = _turn;

                            players[_turn].Items.Add(index);

                            _adapter.SetOwner(index, _turn);
                        }
                    }
                    else if (owner == _turn)
                    {
                        //do nothing
                    }
                    else if (decision == Player.BuyDecision.Auction)
                    {
                        Auction(index);
                    }
                }
                else if (!_mortgaged[index])
                {
                    //payment train
                    int trains = CountTrains(owner);

                    if (trains is >= 1 and <= 4)
                    {
                        int fine = TrainPenalties[trains - 1];
                        PaymentToPlayer(_turn, owner, fine);
                    }
                }

                break;
            }
            case TileType.Utility:
            {
                int owner = Owner(index);

                if (owner == Constants.BankIndex)
                {
                    _adapter.SetTurn(_turn);

                    _adapter.SetSelectionState(index, 1);

                    Player.BuyDecision decision = players[_turn].DecideBuy(index);

                    _adapter.SetSelectionState(index, 0);

                    switch (decision)
                    {
                        case Player.BuyDecision.Buy when players[_turn].Funds < Costs[index]:
                            Auction(index);
                            break;
                        case Player.BuyDecision.Buy:
                        {
                            Payment(_turn, Costs[index]);

                            _owners[index] = _turn;

                            if (_original[index] == -1)
                                _original[index] = _turn;

                            players[_turn].Items.Add(index);

                            _adapter.SetOwner(index, _turn);
                            break;
                        }
                        case Player.BuyDecision.Auction:
                            Auction(index);
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                }
                else if (owner == _turn)
                {
                    //do nothing
                }
                else if (!_mortgaged[index])
                {
                    //payment utility
                    int utilities = CountUtilities(owner);

                    if (utilities is >= 1 and <= 2)
                    {
                        int fine = UtilityPenalties[utilities - 1] * _lastRoll;
                        PaymentToPlayer(_turn, owner, fine);
                    }
                }

                break;
            }
            case TileType.Tax:
                Payment(_turn, Costs[index]);
                break;
            case TileType.Chance:
                DrawChance();
                break;
            case TileType.Chest:
                DrawChest();
                break;
            case TileType.Jail:
                players[_turn].Position = Constants.JailIndex;
                players[_turn].Doub = 0;
                players[_turn].State = Player.PlayerState.Jail;

                _adapter.SetJail(_turn, 1);
                break;
            case TileType.None:
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    private void Payment(int owner, int fine)
    {
        players[owner].Funds -= fine;
        _adapter.SetMoney(owner, players[owner].Funds);

        //prompt for selling sets
        if (players[owner].Funds < 0)
        {
            int[] sets = FindSets(_turn);
            int setCount = sets.GetLength(0);

            for (var j = 0; j < setCount; j++)
            {
                int houseTotal = _houses[Sets[sets[j], 0]] + _houses[Sets[sets[j], 1]];

                if (sets[j] != 0 && sets[j] != 7)
                    houseTotal += _houses[Sets[sets[j], 2]];

                int sellMax = houseTotal;

                _adapter.SetTurn(_turn);

                _adapter.SetSelectionState(Sets[sets[j], 0], 1);

                int decision = players[_turn].DecideSellHouse(sets[j]);

                _adapter.SetSelectionState(Sets[sets[j], 0], 0);

                decision = Math.Min(decision, sellMax);

                if (decision <= 0)
                    continue;
                SellHouses(sets[j], decision);

                players[owner].Funds += (int) (decision * Build[_property[Sets[sets[j], 0]]] * 0.5f);
                _adapter.SetMoney(owner, players[owner].Funds);
            }
        }

        //prompt for mortgages once
        if (players[owner].Funds < 0)
        {
            int itemCount = players[owner].Items.Count;

            for (var i = 0; i < itemCount; i++)
            {
                int item = players[owner].Items[i];
                _adapter.SetTurn(owner);

                _adapter.SetSelectionState(players[owner].Items[i], 1);

                Player.Decision decision = players[owner].DecideMortgage(players[owner].Items[i]);

                _adapter.SetSelectionState(players[owner].Items[i], 0);

                if (decision == Player.Decision.Yes)
                    Mortgage(item);
            }
        }

        //bankrupt
        if (players[owner].Funds >= 0)
            return;
        {
            int itemCount = players[owner].Items.Count;

            for (var i = 0; i < itemCount; i++)
            {
                int item = players[owner].Items[i];
                _owners[item] = Constants.BankIndex;
                _adapter.SetOwner(item, Constants.BankIndex);

                if (_houses[item] <= 0)
                    continue;

                _houses[item] = 0;
            }

            players[owner].Items.Clear();

            //give money to other 
            players[owner].State = Player.PlayerState.Retired;
            _remaining--;
        }
    }

    private void PaymentToPlayer(int owner, int recipient, int fine)
    {
        players[owner].Funds -= fine;
        _adapter.SetMoney(owner, players[owner].Funds);

        players[recipient].Funds += fine;
        _adapter.SetMoney(recipient, players[recipient].Funds);

        //prompt for selling sets
        if (players[owner].Funds < 0)
        {
            int[] sets = FindSets(_turn);
            int setCount = sets.GetLength(0);

            for (var j = 0; j < setCount; j++)
            {
                int houseTotal = _houses[Sets[sets[j], 0]] + _houses[Sets[sets[j], 1]];

                if (sets[j] != 0 && sets[j] != 7)
                    houseTotal += _houses[Sets[sets[j], 2]];

                int sellMax = houseTotal;

                _adapter.SetTurn(_turn);

                _adapter.SetSelectionState(Sets[sets[j], 0], 1);

                int decision = players[_turn].DecideSellHouse(sets[j]);

                _adapter.SetSelectionState(Sets[sets[j], 0], 0);

                decision = Math.Min(decision, sellMax);

                if (decision <= 0)
                    continue;
                SellHouses(sets[j], decision);
                players[owner].Funds += (int) (decision * Build[_property[Sets[sets[j], 0]]] * 0.5f);

                _adapter.SetMoney(owner, players[owner].Funds);
            }
        }

        //prompt for mortgages once
        if (players[owner].Funds < 0)
        {
            int itemCount = players[owner].Items.Count;

            for (var i = 0; i < itemCount; i++)
            {
                int item = players[owner].Items[i];
                _adapter.SetTurn(owner);

                _adapter.SetSelectionState(players[owner].Items[i], 0);

                Player.Decision decision = players[owner].DecideMortgage(players[owner].Items[i]);

                _adapter.SetSelectionState(players[owner].Items[i], 1);

                if (decision == Player.Decision.Yes)
                    Mortgage(item);
            }
        }

        //bankrupt
        if (players[owner].Funds < 0)
        {
            players[recipient].Funds += players[owner].Funds;
            _adapter.SetMoney(recipient, players[recipient].Funds);

            int itemCount = players[owner].Items.Count;

            var housemoney = 0;

            for (var i = 0; i < itemCount; i++)
            {
                //give to other player
                players[recipient].Items.Add(players[owner].Items[i]);

                _adapter.SetOwner(players[owner].Items[i], recipient);

                int item = players[owner].Items[i];
                _owners[item] = recipient;

                if (_houses[item] <= 0)
                    continue;
                int liquidated = _houses[item];
                int sell = liquidated * Build[_property[item]] / 2;
                housemoney += sell;

                _houses[item] = 0;
            }

            players[recipient].Funds += housemoney;
            _adapter.SetMoney(recipient, players[recipient].Funds);

            players[owner].Items.Clear();

            //give money to other 
            players[owner].State = Player.PlayerState.Retired;
            _remaining--;
        }
    }

    private int Owner(int index)
    {
        return _owners[index];
    }

    private void Mortgage(int index)
    {
        _mortgaged[index] = true;
        _adapter.SetMortgage(index, 1);

        players[_owners[index]].Funds += Costs[index] / 2;
        _adapter.SetMoney(_owners[index], players[_owners[index]].Funds);
    }

    private void Advance(int index)
    {
        _mortgaged[index] = false;
        _adapter.SetMortgage(index, 0);

        var cost = (int) (Costs[index] * Constants.MortgageInterest);
        Payment(_owners[index], cost);
    }

    private int CountTrains(int player)
    {
        int itemCount = players[player].Items.Count;

        var count = 0;

        for (var i = 0; i < itemCount; i++)
            if (TrainPositions.Contains(players[player].Items[i]))
                count++;

        return count;
    }

    private int CountUtilities(int player)
    {
        int itemCount = players[player].Items.Count;

        var count = 0;

        for (var i = 0; i < itemCount; i++)
            if (UtilityPosiions.Contains(players[player].Items[i]))
                count++;

        return count;
    }

    private void DrawChance()
    {
        CardEntry card = _chance[0];
        _chance.RemoveAt(0);
        _chance.Add(card);

        switch (card.CardType)
        {
            case CardType.Advance:
            {
                if (players[_turn].Position > card.Value)
                {
                    players[_turn].Funds += Constants.GoBonus;
                    _adapter.SetMoney(_turn, players[_turn].Funds);
                }

                players[_turn].Position = card.Value;
                _adapter.SetPosition(_turn, players[_turn].Position);

                ActivateTile();
                break;
            }
            case CardType.Reward:
                players[_turn].Funds += card.Value;
                _adapter.SetMoney(_turn, players[_turn].Funds);
                break;
            case CardType.Fine:
                Payment(_turn, card.Value);
                break;
            case CardType.Back3:
                players[_turn].Position -= 3;
                _adapter.SetPosition(_turn, players[_turn].Position);

                ActivateTile();
                break;
            case CardType.Card:
                players[_turn].Card++;
                _adapter.SetCard(_turn, players[_turn].Card);
                break;
            case CardType.Jail:
                players[_turn].Position = Constants.JailIndex;
                players[_turn].Doub = 0;
                players[_turn].State = Player.PlayerState.Jail;

                _adapter.SetPosition(_turn, players[_turn].Position);
                _adapter.SetJail(_turn, 1);
                break;
            case CardType.Railroad2:
                AdvanceToTrain2();
                break;
            case CardType.Utility10:
                AdvanceToUtility10();
                break;
            case CardType.Chairman:
            {
                for (var i = 0; i < Constants.PlayerCount; i++)
                {
                    if (i == _turn)
                        continue;

                    //only pay active players
                    if (players[i].State != Player.PlayerState.Retired)
                        PaymentToPlayer(_turn, i, 50);
                }

                break;
            }
            case CardType.Repairs:
            {
                var houseCount = 0;
                var hotelCount = 0;
                int itemCount = players[_turn].Items.Count;

                for (var i = 0; i < itemCount; i++)
                {
                    int index = players[_turn].Items[i];

                    if (_houses[index] <= 4)
                        houseCount += _houses[index];
                    else
                        hotelCount++;
                }

                Payment(_turn, houseCount * 25 + hotelCount * 100);
                break;
            }
            case CardType.Street:
                break;
            case CardType.Birthday:
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    private void DrawChest()
    {
        CardEntry card = _chest[0];
        _chest.RemoveAt(0);
        _chest.Add(card);

        switch (card.CardType)
        {
            case CardType.Advance:
            {
                if (players[_turn].Position > card.Value)
                {
                    players[_turn].Funds += Constants.GoBonus;
                    _adapter.SetMoney(_turn, players[_turn].Funds);
                }

                players[_turn].Position = card.Value;
                _adapter.SetPosition(_turn, players[_turn].Position);

                ActivateTile();
                break;
            }
            case CardType.Reward:
                players[_turn].Funds += card.Value;
                _adapter.SetMoney(_turn, players[_turn].Funds);
                break;
            case CardType.Fine:
                Payment(_turn, card.Value);
                break;
            case CardType.Card:
                players[_turn].Card++;
                _adapter.SetCard(_turn, players[_turn].Card);
                break;
            case CardType.Jail:
                players[_turn].Position = Constants.JailIndex;
                players[_turn].Doub = 0;
                players[_turn].State = Player.PlayerState.Jail;

                _adapter.SetPosition(_turn, players[_turn].Position);
                _adapter.SetJail(_turn, 1);
                break;
            case CardType.Birthday:
            {
                for (var i = 0; i < Constants.PlayerCount; i++)
                {
                    if (i == _turn)
                        continue;

                    //only pay active players
                    if (players[i].State != Player.PlayerState.Retired)
                        PaymentToPlayer(i, _turn, 10);
                }

                break;
            }
            case CardType.Street:
            {
                var houseCount = 0;
                var hotelCount = 0;
                int itemCount = players[_turn].Items.Count;

                for (var i = 0; i < itemCount; i++)
                {
                    int index = players[_turn].Items[i];

                    if (_houses[index] <= 4)
                        houseCount += _houses[index];
                    else
                        hotelCount++;
                }

                Payment(_turn, houseCount * Constants.BoardLength + hotelCount * 115);
                break;
            }
            case CardType.Railroad2:
                break;
            case CardType.Utility10:
                break;
            case CardType.Back3:
                break;
            case CardType.Repairs:
                break;
            case CardType.Chairman:
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    private void AdvanceToTrain2()
    {
        int index = players[_turn].Position;

        if (index < TrainPositions[0])
        {
            players[_turn].Position = TrainPositions[0];
        }
        else if (index < TrainPositions[1])
        {
            players[_turn].Position = TrainPositions[1];
        }
        else if (index < TrainPositions[2])
        {
            players[_turn].Position = TrainPositions[2];
        }
        else if (index < TrainPositions[3])
        {
            players[_turn].Position = TrainPositions[3];
        }
        else
        {
            players[_turn].Position = TrainPositions[0];
            players[_turn].Funds += Constants.GoBonus;
            _adapter.SetMoney(_turn, players[_turn].Funds);
        }

        _adapter.SetPosition(_turn, players[_turn].Position);

        index = players[_turn].Position;

        int owner = Owner(index);

        if (owner == Constants.BankIndex)
        {
            _adapter.SetTurn(_turn);

            _adapter.SetSelectionState(index, 0);

            Player.BuyDecision decision = players[_turn].DecideBuy(index);

            _adapter.SetSelectionState(index, 1);

            if (decision == Player.BuyDecision.Buy)
            {
                if (players[_turn].Funds < Costs[index])
                {
                    Auction(index);
                }
                else
                {
                    Payment(_turn, Costs[index]);

                    _owners[index] = _turn;

                    if (_original[index] == -1)
                        _original[index] = _turn;

                    players[_turn].Items.Add(index);

                    _adapter.SetOwner(index, _turn);
                }
            }
            else if (decision == Player.BuyDecision.Auction)
            {
                Auction(index);
            }
        }
        else if (owner == _turn)
        {
            //do nothing
        }
        else if (!_mortgaged[index])
        {
            //payment train
            int trains = CountTrains(owner);

            if (trains is < 1 or > 4)
                return;
            int fine = TrainPenalties[trains - 1];

            PaymentToPlayer(_turn, owner, fine * 2);
        }
    }

    private void AdvanceToUtility10()
    {
        int index = players[_turn].Position;

        if (index < UtilityPosiions[0])
        {
            players[_turn].Position = UtilityPosiions[0];
        }
        else if (index < UtilityPosiions[1])
        {
            players[_turn].Position = UtilityPosiions[1];
        }
        else
        {
            players[_turn].Position = UtilityPosiions[0];
            players[_turn].Funds += Constants.GoBonus;

            _adapter.SetMoney(_turn, players[_turn].Funds);
        }

        _adapter.SetPosition(_turn, players[_turn].Position);

        index = players[_turn].Position;

        int owner = Owner(index);

        if (owner == Constants.BankIndex)
        {
            _adapter.SetTurn(_turn);
            Player.BuyDecision decision = players[_turn].DecideBuy(index);

            switch (decision)
            {
                case Player.BuyDecision.Buy when players[_turn].Funds < Costs[index]:
                    Auction(index);
                    break;
                case Player.BuyDecision.Buy:
                {
                    Payment(_turn, Costs[index]);

                    _owners[index] = _turn;

                    if (_original[index] == -1)
                        _original[index] = _turn;

                    players[_turn].Items.Add(index);

                    _adapter.SetOwner(index, _turn);
                    break;
                }
                case Player.BuyDecision.Auction:
                    Auction(index);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
        else if (owner == _turn)
        {
            //do nothing
        }
        else if (!_mortgaged[index])
        {
            //payment utility
            int fine = 10 * _lastRoll;

            PaymentToPlayer(_turn, owner, fine);
        }
    }

    private int[] FindSets(int owner)
    {
        var sets = new List<int>();
        List<int> items = players[owner].Items;

        for (var i = 0; i < 8; i++)
        {
            //two piece sets
            if (i is 0 or 7)
            {
                if (items.Contains(Sets[i, 0]) && items.Contains(Sets[i, 1]))
                    sets.Add(i);

                continue;
            }

            //three piece sets
            if (items.Contains(Sets[i, 0]) && items.Contains(Sets[i, 1]) && items.Contains(Sets[i, 2]))
                sets.Add(i);
        }

        return sets.ToArray();
    }

    private void BuildHouses(int set, int amount)
    {
        var last = 2;

        if (set == 0 || set == 7)
            last = 1;

        for (var i = 0; i < amount; i++)
        {
            //find smallest house number from back
            int bj = last;

            for (int j = last - 1; j >= 0; j--)
                if (_houses[Sets[set, bj]] > _houses[Sets[set, j]])
                    bj = j;

            _houses[Sets[set, bj]]++;
            _adapter.SetHouse(Sets[set, bj], _houses[Sets[set, bj]]);
        }
    }

    private void SellHouses(int set, int amount)
    {
        var last = 2;

        if (set is 0 or 7)
            last = 1;

        for (var i = 0; i < amount; i++)
        {
            //find smallest house number from back
            var bj = 0;

            for (var j = 0; j <= last; j++)
                if (_houses[Sets[set, bj]] < _houses[Sets[set, j]])
                    bj = j;

            _houses[Sets[set, bj]]--;
            _adapter.SetHouse(Sets[set, bj], _houses[Sets[set, bj]]);
        }
    }

    private enum EMode
    {
        Roll
    }

    private enum TileType
    {
        None,
        Property,
        Train,
        Utility,
        Chance,
        Chest,
        Tax,
        Jail
    }

    public class CardEntry
    {
        public CardEntry(CardType cardType, int value)
        {
            CardType = cardType;
            Value = value;
        }

        public CardType CardType { get; }
        public int Value { get; }
    }
}