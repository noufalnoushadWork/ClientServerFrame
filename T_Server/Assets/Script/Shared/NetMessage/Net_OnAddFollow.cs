[System.Serializable]
public class Net_OnAddFollow : NetMessage
{
    public Net_OnAddFollow()
    {
        OP = NetOP.OnAddFollow ;
    }

    public byte Success{set;get;}
    public Account Follow {set;get;}
}
