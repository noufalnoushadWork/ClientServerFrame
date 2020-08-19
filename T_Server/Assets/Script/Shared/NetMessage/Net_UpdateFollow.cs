[System.Serializable]
public class Net_UpdateFollow: NetMessage
{       
   public Net_UpdateFollow ()
   {
       OP = NetOP.UpdateFollow;
   }

    public Account Follow {set;get;}
}
