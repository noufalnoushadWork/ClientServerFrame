using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEngine;
using UnityEngine.Networking;

public class Client : MonoBehaviour
{
    public static Client Instance {private set;get;}

    private const int MAX_USER = 100;
    private const int PORT = 26000;
    private const int WEB_PORT = 26001;
    private const int BYTE_SIZE = 1024;
    private const string SERVER_IP ="127.0.0.1"; //"13.68.175.155";//"25.80.198.249"; 


    private byte reliableChannel;
    private int connectionId;
    private int hostId;
    private byte error;

    public Account self;
    private string token;
    private bool isStarted;
    

    #region Monobehaviour
    private void Start() 
    {
        Instance = this;
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
        NetworkTransport.Init();
        ConnectionConfig cc = new ConnectionConfig();
        reliableChannel = cc.AddChannel(QosType.Reliable);

        HostTopology topo = new HostTopology(cc,MAX_USER);
        
        //Client Only Server
        hostId = NetworkTransport.AddHost(topo,0);


#if UNITY_WEBGL && !UNITY_EDITOR
        // web Client
        connectionId = NetworkTransport.Connect(hostId,SERVER_IP,WEB_PORT,0, out error);   
        Debug.Log("Connecting from WebGL");
#else
        // Standalone Client
        connectionId = NetworkTransport.Connect(hostId,SERVER_IP,PORT,0, out error);
        Debug.Log("Connecting from Standalone");   
#endif        
        
        Debug.Log(string.Format("Attempting to connect on Server {0}....",SERVER_IP));
        isStarted = true;
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
                    Debug.Log("We have Connected to the server");
                    break;

            case NetworkEventType.DisconnectEvent:
                    Debug.Log("We have been disconnected");
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
        switch(msg.OP)
        {
            case NetOP.None:
                Debug.Log("Unexpected Net OP");
                break;

            case NetOP.OnCreateAccount:
                OnCreateAccount((Net_OnCreateAccount)msg);
                break;

            case NetOP.OnLoginRequest:
                OnLoginRequest((Net_OnLoginRequest)msg);
                break;
            case NetOP.OnAddFollow:
                OnAddFollow((Net_OnAddFollow)msg);
                break;
            case NetOP.OnRequestFollow:
                OnRequestFollow((Net_OnRequestFollow)msg);
                break;            
        }
    }

    private void OnCreateAccount(Net_OnCreateAccount oca)
    {
        LobbyScene.Instance.EnableInputs();
        LobbyScene.Instance.ChangeAuthenticationMessage(oca.Information);
    }

    private void OnLoginRequest(Net_OnLoginRequest olr)
    {
        LobbyScene.Instance.ChangeAuthenticationMessage(olr.Information);
        if(olr.Success != 1)
        {
            //Unable to login
            LobbyScene.Instance.EnableInputs();
        }
        else
        {
            //Successful Login
            // This is where we are going to save data about ourself
            Debug.Log(olr.Discriminator);
            self = new Account();
            self.ActiveConnection = olr.ConnectionId;
            self.Username = olr.Username;
            self.Discriminator = olr.Discriminator;

            token = olr.Token;

            UnityEngine.SceneManagement.SceneManager.LoadScene("LabHubScene");
        }
    }

    
    private void OnAddFollow(Net_OnAddFollow oaf)
    {
        if(oaf.Success == 0)
            HubSceneHandler.Instance.AddFollowToUi(oaf.Follow);
    }

    private void OnRequestFollow(Net_OnRequestFollow orf)
    {
        foreach(var follow in orf.Follows)
            HubSceneHandler.Instance.AddFollowToUi(follow);
    }

#endregion

#region Send
    public void SendServer(NetMessage msg)
    {
        // This is where we store our data
        byte[] buffer = new byte[BYTE_SIZE];

        // This is where we crush our data to a byte[]
        BinaryFormatter formatter = new BinaryFormatter();
        MemoryStream ms = new MemoryStream(buffer);
        formatter.Serialize(ms,msg);

        NetworkTransport.Send(hostId,connectionId,reliableChannel,buffer,BYTE_SIZE,out error);
    }

    public void SendCreateAccount(string username, string password,string email)
    {
        
        if(!Utility.IsUsername(username))
        {
            // Invalid Username
            LobbyScene.Instance.ChangeAuthenticationMessage("Username is Invalid");
            LobbyScene.Instance.EnableInputs();
            return;
        }

        if(!Utility.IsEmail(email))
        {
            // Invalid email
            LobbyScene.Instance.ChangeAuthenticationMessage("Email is Invalid");
            LobbyScene.Instance.EnableInputs();
            return;
        }

        if(password == null || password == "")
        {
            // Invalid Password
            LobbyScene.Instance.ChangeAuthenticationMessage("Password is empty");
            LobbyScene.Instance.EnableInputs();
            return;
        }

        Net_CreateAccount ca = new Net_CreateAccount();
        ca.Username = username;
        ca.Password = Utility.Sha256FromString(password);
        ca.Email = email;

        LobbyScene.Instance.ChangeAuthenticationMessage("Sending Request");
        SendServer(ca);
    }
    public void SendLoginRequest(string usernameOrEmail, string password)
    {
        if(!Utility.IsUsernameAndDiscriminator(usernameOrEmail) && !Utility.IsEmail(usernameOrEmail))
        {
            // Invalid Username or email
            LobbyScene.Instance.ChangeAuthenticationMessage("Email or Username#Discriminator is Invalid");
            LobbyScene.Instance.EnableInputs();
            return;
        }

        if(password == null || password == "")
        {
            // Invalid Password
            LobbyScene.Instance.ChangeAuthenticationMessage("Password is empty");
            LobbyScene.Instance.EnableInputs();
            return;
        }


        Net_LoginRequest lr = new Net_LoginRequest();
        lr.UsernameOrEmail = usernameOrEmail;
        lr.Password = Utility.Sha256FromString(password);

        LobbyScene.Instance.ChangeAuthenticationMessage("Sending Login Request");

        SendServer(lr);
    }

    public void SendAddFollow(string usernameOrEmail)
    {
        Net_AddFollow af = new Net_AddFollow();

        af.Token = token;
        af.UsernameDiscriminatorOrEmail = usernameOrEmail;

        SendServer(af);
    }

    public void SendRemoveFollow(string usernameOrEmail)
    {
        Net_RemoveFollow rf = new Net_RemoveFollow();

        rf.Token = token;
        rf.UsernameDiscriminator = usernameOrEmail;

        SendServer(rf);
    }

    public void SendRequestFollow()
    {
        Net_RequestFollow rqf = new Net_RequestFollow();

        rqf.Token = token;
        SendServer(rqf);

    }
#endregion
}
