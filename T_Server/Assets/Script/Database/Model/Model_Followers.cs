using MongoDB.Bson;
using MongoDB.Driver;
using UnityEngine;
public class Model_Followers 
{
   public ObjectId _id;

   public MongoDBRef Sender;
   public MongoDBRef Receiver;
   
}
