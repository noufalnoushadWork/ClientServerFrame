using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class HubSceneHandler : MonoBehaviour
{
    public static HubSceneHandler Instance {set;get;}

    [SerializeField] private TextMeshProUGUI selfInformation;
    [SerializeField] private TMP_InputField addFollowInput;
    [SerializeField] private GameObject followPrefab;
    [SerializeField] private Transform followContainer;
    void Start()
    {
        Instance = this;
        selfInformation.text = Client.Instance.self.Username+"#"+Client.Instance.self.Discriminator;
        Client.Instance.SendRequestFollow();
    }

    public void AddFollowToUi(Account follow)
    {
        GameObject followItem = Instantiate(followPrefab,followContainer);

        followItem.GetComponentInChildren<TextMeshProUGUI>().text = follow.Username+"#"+follow.Discriminator;
        followItem.transform.GetChild(1).GetComponent<Image>().color = (follow.Status!=0) ? Color.green : Color.grey;
        
        followItem.transform.GetChild(2).GetComponent<Button>().onClick.AddListener(delegate { Destroy(followItem);});
        followItem.transform.GetChild(2).GetComponent<Button>().onClick.AddListener(delegate {OnClickRemoveFollow(follow.Username,follow.Discriminator);});  
    }

    #region Button
    public void OnClickAddFollow()
    {
        string usernameDiscriminator = addFollowInput.text;

        if(!Utility.IsUsernameAndDiscriminator(usernameDiscriminator) && !Utility.IsEmail(usernameDiscriminator))
        {
            Debug.Log("Invalid Format");
            return;
        }

        //Client Instance send add follow
        Client.Instance.SendAddFollow(usernameDiscriminator);
    }

    public void OnClickRemoveFollow(string username, string discriminator)
    {
        //Client Instance remove follow
        Client.Instance.SendRemoveFollow(username +"#"+discriminator);
    }

    #endregion

}
