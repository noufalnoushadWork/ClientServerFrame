using System.Collections.Generic;

[System.Serializable]
public class Net_OnRequestFollow : NetMessage
{
    public Net_OnRequestFollow()
    {
        OP = NetOP.OnRequestFollow ;
    }
    public List<Account> Follows {set;get;}
}
