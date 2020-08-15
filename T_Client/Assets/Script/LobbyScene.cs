using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class LobbyScene : MonoBehaviour
{
    public static LobbyScene Instance;

    private void Start() 
    {
        Instance = this;    
    }
    public void OnclickCreateAccount()
    {   
        DisableInputs();
        string username = GameObject.Find("CreateUsername").GetComponent<TMP_InputField>().text;
        string password = GameObject.Find("CreatePassword").GetComponent<TMP_InputField>().text;
        string email = GameObject.Find("CreateEmail").GetComponent<TMP_InputField>().text;

        Client.Instance.SendCreateAccount(username,password,email);
    }

    public void OnclickLoginRequest()
    {
        DisableInputs();
        string usernameOrEmail = GameObject.Find("LoginUsernameEmail").GetComponent<TMP_InputField>().text;
        string password = GameObject.Find("LoginPassword").GetComponent<TMP_InputField>().text;
        
        Client.Instance.SendLoginRequest(usernameOrEmail,password);    
    }

    public void ChangeWelcomeMessage(string msg)
    {
        GameObject.Find("WelcomeText").GetComponent<TextMeshProUGUI>().text = msg;
    }
    public void ChangeAuthenticationMessage(string msg)
    {
        GameObject.Find("AuthenticationText").GetComponent<TextMeshProUGUI>().text = msg;
    }

    public void EnableInputs()
    {
        GameObject.Find("Canvas").GetComponent<CanvasGroup>().interactable = true;
    }
    public void DisableInputs()
    {
        GameObject.Find("Canvas").GetComponent<CanvasGroup>().interactable = false;
    }

}
