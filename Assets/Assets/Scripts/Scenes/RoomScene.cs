using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class StaticRoom
{
    public static bool SeenWelcome = false;

    public static void Reset()
    {
        SeenWelcome = false;
    }
}

public class RoomScene : MonoBehaviour
{
    [SerializeField] private GameObject ReadyButton = null;
    [SerializeField] private GameObject DatabaseButton = null;

    private string m_roomCode;

    private Dictionary<string, bool> m_readyPlayers = new Dictionary<string, bool>();

    public GameObject mainScreen;
    public GameObject welcomeScreen;

    void Start()
    {
        if (StaticRoom.SeenWelcome)
        {
            mainScreen.SetActive(true);
            welcomeScreen.SetActive(false);
        }
        StaticRoom.SeenWelcome = true;

        ReadyButton.SetActive(false);
        DatabaseButton.SetActive(false);

        string room = LobbyScene.Lobby.Id;
        if (!string.IsNullOrEmpty(room))
        {
            m_roomCode = room;
            foreach (var player in CloudManager.AllUsers) m_readyPlayers[player] = false;
            RegisterListeners();
            ReadyButton.SetActive(true);
            DatabaseButton.SetActive(true);
        }
        else SceneManager.LoadScene("Lobby");
    }

    private void RegisterListeners()
    {
        foreach (LobbyUserItem clue in LobbyScene.Lobby.Users.Where(u => u.UserId.Value != SignInScene.User.Id).Select(u => u.Items).SelectMany(i => i))
            clue.ValueChanged += OnSlotChanged;

        foreach (LobbyUser user in LobbyScene.Lobby.Users)
            user.Ready.ValueChanged += OnReadyChanged;
    }

    public void DatabaseButtonPressed()
    {
        if (DatabaseButton.activeSelf)
        {
            DatabaseButton.SetActive(false);
            SceneManager.LoadScene("Database");
        }
    }

    private void OnReadyChanged(CloudNode<bool> entry)
    {
        if (ReadyButton == null)
            return;

        if (entry.Value != null)
        {
            bool value = entry.Value ?? false;

            if (value == true)
            {
                string[] key = entry.Key.Split('/');
                string player = key[1];
                m_readyPlayers[player] = true;

                if (player == SignInScene.User.Id)
                {
                    ConfirmReady();
                }

                if (!m_readyPlayers.Any(p => p.Value == false))
                {
                    //NetworkController.DeregisterReadyChanged(OnReadyChanged);
                    SceneManager.LoadScene("Voting");
                }
            }
        }
    }

    public void ConfirmLeave()
    {
        CloudManager.LeaveLobby();
        SceneManager.LoadScene("Lobby");

        //NetworkController.LeaveLobby(m_roomCode, success => {
        //    if (success) SceneManager.LoadScene("Lobby");
        //});
    }

    public void ConfirmReady()
    {
        if (ReadyButton.activeSelf)
        {
            ReadyButton.SetActive(false);
            LobbyScene.Lobby.Users.First(u => u.UserId.Value == SignInScene.User.Id).Ready.Value = true;
            ReadyButton.SetActive(true);
            ReadyButton.GetComponent<Image>().color = Color.yellow;
            foreach (Transform t in ReadyButton.gameObject.transform)
            {
                var text = t.GetComponent<Text>();
                if (text != null) text.text = "Waiting...";
            }
        }
    }

    private void OnSlotChanged(CloudNode entry)
    {
        string[] keys = entry.Key.Split('/');
        if (keys.Length >= 5)
        {
            string field = keys[4];

            if (entry.Value != null && field == "name")
            {
                string value = entry.Value;

                int slot;
                if (!string.IsNullOrEmpty(value) && int.TryParse(keys[3].Replace("slot-", ""), out slot))
                {
                    int player = CloudManager.GetPlayerNumber(keys[1]);
                    if (DatabaseButton != null && !StaticClues.SeenSlots.Any(s => s.Equals(new SlotData(player.ToString(), slot.ToString(), value))))
                    {
                        foreach (Transform t in DatabaseButton.transform)
                        {
                            if (t.gameObject.name == "Alert")
                            {
                                t.gameObject.SetActive(true);
                            }
                        }
                    }
                }
            }
        }
    }
}
