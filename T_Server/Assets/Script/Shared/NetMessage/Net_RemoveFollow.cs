[System.Serializable]
public class Net_RemoveFollow : NetMessage
{
    public Net_RemoveFollow()
    {
        OP = NetOP.RemoveFollow;
    }
    public string Token{set;get;}
    public string UsernameDiscriminator{set;get;}
}
