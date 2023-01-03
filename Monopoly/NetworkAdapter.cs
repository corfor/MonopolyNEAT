using System;

public class NetworkAdapter
{
    private const int Card = 12;
    private const int House = 76;

    private const int Jail = 16;
    private const int Mon = 8;
    private const int Mort = 48;
    private const int Own = 20;
    private const int Pos = 4;

    private const int Select = 98;
    private const int SelectMoney = 126;

    private readonly int[] _houses =
        {-1, 0, -1, 1, -1, -1, 2, -1, 3, 4, -1, 5, -1, 6, 7, -1, 8, -1, 9, 10, -1, 11, -1, 12, 13, -1, 14, 15, -1, 16, -1, 17, 18, -1, 19, -1, -1, 20, -1, 21};

    private readonly int[] _props =
        {-1, 0, -1, 1, -1, 2, 3, -1, 4, 5, -1, 6, 7, 8, 9, 10, 11, -1, 12, 13, -1, 14, -1, 15, 16, 17, 18, 19, 20, 21, -1, 22, 23, -1, 24, 25, -1, 26, -1, 27};

    public NetworkAdapter()
    {
        Pack = new float[127];
    }

    public float[] Pack { get; private set; }

    public void Reset()
    {
        Pack = new float[127];
    }

    private static float ConvertMoney(int money)
    {
        float norm = money / 4000.0f;
        float clamp = Math.Clamp(norm, 0.0f, 1.0f);

        return clamp;
    }

    public static float ConvertMoneyValue(float value)
    {
        return value * 4000.0f;
    }

    public static float ConvertHouseValue(float value)
    {
        if (value <= 0.5f)
            value = 0.0f;

        return value * 15.0f;
    }

    private static float ConvertPosition(int position)
    {
        float norm = position / 39.0f;
        float clamp = Math.Clamp(norm, 0.0f, 1.0f);

        return clamp;
    }

    private static float ConvertCard()
    {
        float clamp = Math.Clamp(Card, 0.0f, 1.0f);
        return clamp;
    }

    private static float ConvertHouse(int houses)
    {
        float norm = houses / 5.0f;
        float clamp = Math.Clamp(norm, 0.0f, 1.0f);

        return clamp;
    }

    public void SetTurn(int index)
    {
        for (var i = 0; i < 4; i++)
            Pack[i] = 0.0f;

        Pack[index] = 1.0f;
    }

    public void SetSelection(int index)
    {
        for (int i = Select; i < Select + 29; i++)
            Pack[i] = 0.0f;

        Pack[Select + _props[index]] = 1.0f;
    }

    public void SetSelectionState(int index, int state)
    {
        Pack[Select + _props[index]] = state;
    }

    public void SetMoneyContext(int state)
    {
        Pack[SelectMoney] = state;
    }

    public void ClearSelectionState()
    {
        for (int i = Select; i < Select + 29; i++)
            Pack[i] = 0.0f;
    }

    public void SetPosition(int index, int position)
    {
        Pack[Pos + index] = ConvertPosition(position);
    }

    public void SetMoney(int index, int money)
    {
        Pack[Mon + index] = ConvertMoney(money);
    }

    public void SetCard(int index, int cards)
    {
        Pack[Card + index] = ConvertCard();
    }

    public void SetJail(int index, int state)
    {
        Pack[Jail + index] = state;
    }

    public void SetOwner(int property, int state)
    {
        float convert = (state + 1) / 4.0f;

        Pack[Own + _props[property]] = convert;
    }

    public void SetMortgage(int property, int state)
    {
        Pack[Mort + _props[property]] = state;
    }

    public void SetHouse(int property, int houses)
    {
        Pack[House + _houses[property]] = ConvertHouse(houses);
    }
}