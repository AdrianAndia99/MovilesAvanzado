using UnityEngine;
using TMPro;
using UnityEngine.UI;
using Unity.Netcode;


public class uiGameManager : NetworkBehaviour
{
    public TMP_InputField inputField;
    public Button ConnectButton;
    public GameObject loginPanel;

    void Start()
    {
        ConnectButton.onClick.AddListener(OnSubmitName);
        loginPanel.SetActive(false);

        gameManager2.Instance.OnClientConnected += () =>
        {
            loginPanel.SetActive(true);
            inputField.text = "";
            ConnectButton.interactable = true;
            inputField.interactable = true;
        };
    }
    
    public void OnSubmitName()
    {
        string accountID = inputField.text;
        if (!string.IsNullOrEmpty(accountID))
        {
            gameManager2.Instance.RegisterPlayerServerRpc(accountID, NetworkManager.Singleton.LocalClientId);
            ConnectButton.interactable = false;
            inputField.interactable = false;

            loginPanel.SetActive(false);
        }
    }
}
