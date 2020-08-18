using MongoDB.Driver;
using MongoDB.Bson;
using MongoDB.Driver.Core;
using UnityEngine;
using System.Collections.Generic;

public class MongoData 
{
    private const string MONGO_URI = "mongodb+srv://nnAdmin:Something123@lobbycluster.36uie.mongodb.net/LobbyDB?retryWrites=true&w=majority";
    // private const string MONGO_URI = "mongodb://nnAdmin:Something123@lobbycluster-shard-00-00.36uie.mongodb.net:27017,lobbycluster-shard-00-01.36uie.mongodb.net:27017,lobbycluster-shard-00-02.36uie.mongodb.net:27017/LobbyDB?ssl=true&replicaSet=atlas-bshm8m-shard-0&authSource=admin&retryWrites=true&w=majority";
    // private const string MONGO_URI = "mongodb://nnAdmin:Something@lobbycluster-shard-00-00.36uie.mongodb.net:27017/LobbyDB?ssl=true&replicaSet=atlas-bshm8m-shard-0&authSource=admin&retryWrites=true&w=majority";
    private const string DATABASE_NAME="LobbyDB";

    private MongoClient client;
    // private IMongoServer server;
    private IMongoDatabase db;

    private IMongoCollection<Model_Account> accounts;
    private IMongoCollection<Model_Followers> follows;

    public void Init()
    {
        client =  new MongoClient(MONGO_URI);
        // server = client.GetServer();
        // db = server.GetDatabase(DATABASE_NAME);
        db = client.GetDatabase(DATABASE_NAME);

        //This is where we initialize Collection
        accounts = db.GetCollection<Model_Account>("account");
        follows = db.GetCollection<Model_Followers>("follow");
        
        Debug.Log("Database have been Initialized");
    }

    public void Shutdown()
    {
        client = null;
        // server.Shutdown();
        db = null;
    }

#region Insert 

public bool InsertFollow(string token,string emailOrUsername)
{
    Model_Followers newfollow = new Model_Followers();
    newfollow.Sender = new MongoDBRef("account",FindAccountByToken(token)._id);

    // Startby getting reference

    if(!Utility.IsEmail(emailOrUsername))
    {
        // if its username discriminator
        string[] data = emailOrUsername.Split('#');
        if(data[1] != null)
        {
            Model_Account follow = FindAccountByUsernameAndDiscriminator(data[0],data[1]);
            if(follow != null)
            {
                newfollow.Receiver = new MongoDBRef("account", follow._id);
            }
            else
            {
                return false;
            }
        }
    }
    else
    {
        //  If its email
        Model_Account follow = FindAccountByEmail(emailOrUsername);
        if(follow != null)
        {
            newfollow.Receiver = new MongoDBRef("account", follow._id);
        }
        else
        {
            return false;
        }
    }

    if(newfollow.Receiver != newfollow.Sender)
    {
        //  Does already exist
        Model_Followers modelfollow = follows.Find(u => u.Sender.Equals(newfollow.Sender) && u.Receiver.Equals(newfollow.Receiver)).SingleOrDefault();

        //  if no create one retuen true
        if(modelfollow == null)
        {
            follows.InsertOne(newfollow);
        }
        return true;
    }

    return false;
}
public bool InsertAccount(string username,string password,string email)
{
    // Check if email is valid
    if(!Utility.IsEmail(email))
    {
        Debug.Log(email + " is not an Email");
        return false;
    }

    // Check if Username is valid
    if(!Utility.IsUsername(username))
    {
        Debug.Log(username + " is not valid Username");
        return false;
    }

    // Check if account already exists
    if(FindAccountByEmail(email) != null)
    {
        Debug.Log(email + " is already being used");
        return false;
    }


    Model_Account newAccount = new Model_Account();
    newAccount.Username = username;
    newAccount.ShaPassword = password;
    newAccount.Email = email;
    newAccount.Discriminator = "0000";

    // roll for unique discriminator
    int rollCount = 0;
    while(FindAccountByUsernameAndDiscriminator(newAccount.Username,newAccount.Discriminator) != null)
    {
        newAccount.Discriminator = Random.Range(0,9999).ToString("0000");

        rollCount++;
        if(rollCount > 1000)
        {
            Debug.Log("We rolled too many times, suggest username change!");
            return false;
        }
    }

    accounts.InsertOne(newAccount);
    return true;
}
public Model_Account LoginAccount(string usernameOrEmail,string password, int cnnId, string token)
{
    Model_Account myAccount = null;


    // find my Account
    if(Utility.IsEmail(usernameOrEmail))
    {
        // if logged in using email
        myAccount = accounts.Find(u => u.Email.Equals(usernameOrEmail) &&
                                    u.ShaPassword.Equals(password)).SingleOrDefault();
                      
    }
    else
    {
        // if logged in using username discriminator
        string[] data = usernameOrEmail.Split('#');
        if(data[1] != null)
        {
            myAccount = accounts.Find(u => u.Username.Equals(data[0]) &&
                                    u.Discriminator.Equals(data[1]) &&
                                    u.ShaPassword.Equals(password) ).SingleOrDefault();
        }
    }

    if(myAccount != null)
    {
        // We found the account lets login
        myAccount.ActiveConnection = cnnId;
        myAccount.Token = token;
        myAccount.Status = 1;
        myAccount.LastLogin = System.DateTime.Now;
        
        var filter = Builders<Model_Account>.Filter.Eq("_id", myAccount._id);
        var update = Builders<Model_Account>.Update.Set("class_id", 483);
        accounts.FindOneAndReplace(filter, myAccount);
    }
    else
    {
        // Invalid Credentials.. didn't find anything
    }

    return myAccount;
}


#endregion

#region Fetch

public Model_Account FindAccountByObjectId(ObjectId id)
{
    Model_Account modelAccount = accounts.Find(em => em._id.Equals(id)).SingleOrDefault();
    return modelAccount;
}
public Model_Account FindAccountByEmail(string email)
{
    Model_Account modelAccount = accounts.Find(em => em.Email.Equals(email)).SingleOrDefault();
    return modelAccount;
}

public Model_Account FindAccountByUsernameAndDiscriminator(string username, string discriminator)
{
    Model_Account modelAccount = accounts.Find(u => u.Username.Equals(username) && u.Discriminator.Equals(discriminator)).SingleOrDefault();
    return modelAccount;
}

public Model_Account FindAccountByToken(string token)
{
    Model_Account modelAccount = accounts.Find(em => em.Token.Equals(token)).SingleOrDefault();
    return modelAccount;
}

public Model_Followers FindFollowersByUsernameAndDiscriminator(string token, string usernameAndDiscriminator)
{
    string[] data = usernameAndDiscriminator.Split('#');
    if(data[1] != null)
    {
        // Sender
        var Sender = new MongoDBRef("account", FindAccountByToken(token)._id);
        var follow = new MongoDBRef("account", FindAccountByUsernameAndDiscriminator(data[0],data[1])._id);
        
        Model_Followers modelfollow = follows.Find(u => u.Sender.Equals(Sender) && u.Receiver.Equals(follow)).SingleOrDefault();

        return modelfollow;
    }
    return null;
}

public List<Account> FindAllFollowBy(string token)
{
    var self = new MongoDBRef("account",FindAccountByToken(token)._id);
    List<Model_Followers> modelAccountList = follows.Find(em => em.Sender.Equals(self)).ToList();

    List<Account> followResponse = new List<Account>();
    foreach(var f in modelAccountList)
    {
        followResponse.Add(FindAccountByObjectId(f.Receiver.Id.AsObjectId).GetAccount());
    }
    
    return followResponse;
}
#endregion

#region Update
#endregion

#region Delete

public void RemoveFollow(string token,string usernameDiscriminator)
{
    Model_Followers modelfollowId = FindFollowersByUsernameAndDiscriminator(token,usernameDiscriminator);
    var modelfollowFilter = Builders<Model_Followers>.Filter.Eq(e=>e._id,modelfollowId._id);
  
    follows.DeleteOne(modelfollowFilter);
}
#endregion
}
