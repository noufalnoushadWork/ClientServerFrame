﻿[System.Serializable]
public class Account 
{
    public int ActiveConnection {set;get;}
    public string Username {set;get;}
    public string Discriminator {set;get;}
    public byte Status{set;get;}
}
