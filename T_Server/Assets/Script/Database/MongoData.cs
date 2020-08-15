using MongoDB.Driver;
using MongoDB.Bson;
using MongoDB.Driver.Core;
using UnityEngine;

public class MongoData 
{
    private const string MONGO_URI = "mongodb+srv://nnAdmin:Something123@lobbycluster.36uie.mongodb.net/LobbyDB?retryWrites=true&w=majority";
    private const string DATABASE_NAME="LobbyDB";

    private MongoClient client;
    // private IMongoServer server;
    private IMongoDatabase db;

    private IMongoCollection<Model_Account> accounts;

    public void Init()
    {
        client =  new MongoClient(MONGO_URI);
        // server = client.GetServer();
        // db = server.GetDatabase(DATABASE_NAME);
        db = client.GetDatabase(DATABASE_NAME);

        //This is where we initialize Collection
        accounts = db.GetCollection<Model_Account>("account");
        
        Debug.Log("Database have been Initialized");
    }

    public void Shutdown()
    {
        client = null;
        // server.Shutdown();
        db = null;
    }

#region Insert
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

#endregion

#region Update
#endregion

#region Delete
#endregion
}
