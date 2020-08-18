[System.Serializable]
public class Net_AddFollow : NetMessage
{
    public Net_AddFollow()
    {
        OP = NetOP.AddFollow;
    }
    public string Token{set;get;}
    public string UsernameDiscriminatorOrEmail{set;get;}

}
