[System.Serializable]
public class Net_RequestFollow : NetMessage
{
    public Net_RequestFollow()
    {
        OP = NetOP.RequestFollow;
    }
    public string Token{set;get;}
}
