public static class NetOP
{
    public const int None = 0;
    public const int CreateAccount = 1;
    public const int LoginRequest = 2;

    public const int OnCreateAccount = 3;
    public const int OnLoginRequest = 4;

    public const int AddFollow = 5;
    public const int RemoveFollow = 6; 
    public const int RequestFollow = 7;
    public const int OnAddFollow = 8;
    public const int OnRequestFollow = 9;
    
    public const int UpdateFollow = 10;

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
