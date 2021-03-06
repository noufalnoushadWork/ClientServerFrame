﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEngine;
using UnityEngine.Networking;

public class Server : MonoBehaviour
{
    private const int MAX_USER = 100;
    private const int PORT = 26000;
    private const int WEB_PORT = 26001;
    private const int BYTE_SIZE = 1024;

    private byte reliableChannel;
    private int hostId;
    private int webhostId;

    private bool isStarted;
    private byte error;

    private MongoData db;

    #region Monobehaviour
    private void Start() 
    {
        DontDestroyOnLoad(gameObject);
        Init();
    }
    private void Update() 
    {
        UpdateMessagePump();
    }
    #endregion

    public void Init()
    {
        db = new MongoData();
        db.Init();

        NetworkTransport.Init();
        ConnectionConfig cc = new ConnectionConfig();
        reliableChannel = cc.AddChannel(QosType.Reliable);

        HostTopology topo = new HostTopology(cc,MAX_USER);
        
        //Server Only
        hostId = NetworkTransport.AddHost(topo,PORT,null);
        webhostId = NetworkTransport.AddWebsocketHost(topo,WEB_PORT,null);
        
        Debug.Log(string.Format("Oppening connection on port {0} and webport {1}",PORT,WEB_PORT));
        isStarted = true;

        // // Test
        // db.InsertAccount("testUsername","testPassword","testEmail");
    }

    public void Shutdown()
    {
        isStarted = false;
        NetworkTransport.Shutdown();
    }

    public void  UpdateMessagePump()
    {
        if(!isStarted)
            return;

        int recHostId;      // Is this webgl or standalone
        int connectionId;   // which user is sending this 
        int channelId;      // which line is he using

        byte[] recBuffer = new byte[BYTE_SIZE];
        int dataSize;

        NetworkEventType type = NetworkTransport.Receive(out recHostId, out connectionId, out channelId, recBuffer, BYTE_SIZE,out dataSize,out error);

        switch(type)
        {
            case NetworkEventType.Nothing:
                    break;

            case NetworkEventType.ConnectEvent:
                    Debug.Log(string.Format("User {0} has connected through host {1}",connectionId,recHostId));
                    break;

            case NetworkEventType.DisconnectEvent:
                    DisconnectEvent(recHostId,connectionId);
                    break;

            case NetworkEventType.DataEvent:
                    BinaryFormatter formatter = new BinaryFormatter();
                    MemoryStream ms = new MemoryStream(recBuffer);
                    NetMessage msg = (NetMessage)formatter.Deserialize(ms);

                    OnData(connectionId,channelId,recHostId,msg);
                    break;

            default:
            case NetworkEventType.BroadcastEvent:
                    Debug.Log("Unexpected Network Event Type");
                    break;     
        }
    }

    #region OnDATA
    private void OnData(int cnnId,int channelId,int recHostId,NetMessage msg)
    {
        // Debug.Log("Received a message of type " + msg.OP);
        switch(msg.OP)
        {
            case NetOP.None:
                Debug.Log("Unexpected Net OP");
                break;

            case NetOP.CreateAccount:
                CreateAccount(cnnId,channelId,recHostId,(Net_CreateAccount)msg);
                break;
            case NetOP.LoginRequest:
                LoginRequest(cnnId,channelId,recHostId,(Net_LoginRequest)msg);
                break;
            case NetOP.AddFollow:
                AddFollow(cnnId,channelId,recHostId,(Net_AddFollow)msg);
                break;
            case NetOP.RemoveFollow:
                RemoveFollow(cnnId,channelId,recHostId,(Net_RemoveFollow)msg);
                break;
            case NetOP.RequestFollow:
                RequestFollow(cnnId,channelId,recHostId,(Net_RequestFollow)msg);
                break;        
        }
    }

    private void  DisconnectEvent(int recHostId, int cnnId)
    {
         Debug.Log(string.Format("User {0} has Disconnected..",cnnId));

        //  get a reference to the connected account
        Model_Account account = db.FindAccountByConnectionId(cnnId);
        if(account == null)
        {
            // making sure athenticated
            return;
        } 
        db.UpdateAccountAfterDisconnection(account.Email);

        // Prepare and send update message

        Net_UpdateFollow fu = new Net_UpdateFollow();
        Model_Account updatedAccount = db.FindAccountByEmail(account.Email);
        fu.Follow = updatedAccount.GetAccount();

        foreach(var f in db.FindAllFollowBy(account.Email))
        {
            if(f.ActiveConnection == 0)
                continue;
            
            SendClient(recHostId,f.ActiveConnection,fu);
        }

    }
    private void RequestFollow(int cnnId, int channelId, int recHostId, Net_RequestFollow msg)
    {
       Net_OnRequestFollow orf = new Net_OnRequestFollow();

       orf.Follows = db.FindAllFollowFrom(msg.Token);

       SendClient(recHostId,cnnId,orf); 
    }

    private void RemoveFollow(int cnnId, int channelId, int recHostId, Net_RemoveFollow msg)
    {
        db.RemoveFollow(msg.Token,msg.UsernameDiscriminator);
    }

    private void AddFollow(int cnnId, int channelId, int recHostId, Net_AddFollow msg)
    {
        Net_OnAddFollow oaf = new Net_OnAddFollow();

        if(db.InsertFollow(msg.Token, msg.UsernameDiscriminatorOrEmail))
        {
            oaf.Success = 0;
            if(Utility.IsEmail(msg.UsernameDiscriminatorOrEmail))
            {
                // email
                oaf.Follow = db.FindAccountByEmail(msg.UsernameDiscriminatorOrEmail).GetAccount();
            }
            else
            {
                string[] data = msg.UsernameDiscriminatorOrEmail.Split('#');
                if(data[1] == null)
                {
                    return;
                }
                else
                {
                    oaf.Follow = db.FindAccountByUsernameAndDiscriminator(data[0],data[1]).GetAccount();
                }
            }
        }

        SendClient(recHostId,cnnId,oaf);

    }

    private void CreateAccount(int cnnId,int channelId,int recHostId,Net_CreateAccount ca)
    {
        // Debug.Log(string.Format("{0},{1},{2}",ca.Username,ca.Password,ca.Email));

        Net_OnCreateAccount oca = new Net_OnCreateAccount();

        if(db.InsertAccount(ca.Username,ca.Password,ca.Email))
        {
            oca.Success = 1;
            oca.Information = "Account was Created";
        }
        else
        {
            oca.Success = 0;
            oca.Information = "There was an error creating account!";
        }
       

        SendClient(recHostId,cnnId,oca);
    }
     private void LoginRequest(int cnnId,int channelId,int recHostId,Net_LoginRequest lr)
    {
        // Debug.Log(string.Format("{0},{1}",lr.UsernameOrEmail,lr.Password));

        string randomToken = Utility.GenerateRandom(4); //Change to 256 on production
        Model_Account account = db.LoginAccount(lr.UsernameOrEmail,lr.Password,cnnId,randomToken);
        Net_OnLoginRequest olr = new Net_OnLoginRequest();

        if(account != null)
        {
            olr.Success = 1;
            olr.Information = "You have been logged in as" + account.Username;

            olr.Username = account.Username;
            olr.Discriminator = account.Discriminator;
            olr.Token = account.Token;
            olr.ConnectionId = cnnId;

            // Here

            // Prepare and send update message
            Net_UpdateFollow fu = new Net_UpdateFollow();
            fu.Follow = account.GetAccount();

            foreach(var f in db.FindAllFollowBy(account.Email))
            {
                if(f.ActiveConnection == 0)
                    continue;
                
                SendClient(recHostId,f.ActiveConnection,fu);
            }
        }
        else
        {
            olr.Success = 0;
        }
       

        SendClient(recHostId,cnnId,olr);
    }
    #endregion

    #region Send
    public void SendClient(int recHostId,int cnnId,NetMessage msg)
    {
        // This is where we store our data
        byte[] buffer = new byte[BYTE_SIZE];

        // This is where we crush our data to a byte[]
        BinaryFormatter formatter = new BinaryFormatter();
        MemoryStream ms = new MemoryStream(buffer);
        formatter.Serialize(ms,msg);
        if(recHostId == 0)
            NetworkTransport.Send(hostId,cnnId,reliableChannel,buffer,BYTE_SIZE,out error);
        else
            NetworkTransport.Send(webhostId,cnnId,reliableChannel,buffer,BYTE_SIZE,out error);    
    }
    #endregion
}
