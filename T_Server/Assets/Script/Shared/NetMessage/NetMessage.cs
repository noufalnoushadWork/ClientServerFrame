public static class NetOP
{
    public const int None = 0;
    public const int CreateAccount = 1;
    public const int LoginRequest = 2;

    public const int OnCreateAccount = 3;
    public const int OnLoginRequest = 4;

}

[System.Serializable]
public abstract class NetMessage 
{
    public byte OP {set;get;}

    public NetMessage()
    {
        OP = NetOP.None;
    }
}
