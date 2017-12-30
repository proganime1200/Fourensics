using Firebase.Database;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[Serializable]
public class Data
{
    [SerializeField] public GameObject PlayerButton;
    [SerializeField] public GameObject CluePanel;
    [SerializeField] public List<GameObject> Slots;
}

public struct SlotData : IEquatable<SlotData>
{
    public string Player;
    public string Slot;
    public string Name;
    public GameObject Object;

    public SlotData(string player, string slot, string name, GameObject obj = null)
    {
        Player = player;
        Slot = slot;
        Name = name;
        Object = obj;
    }

    public bool Equals(SlotData other)
    {
        return Player == other.Player && Slot == other.Slot && Name == other.Name;
    }
}

public static class StaticClues
{
    public static List<SlotData> SeenSlots = new List<SlotData>();

    public static void Reset()
    {
        SeenSlots.Clear();
    }
}

public class DatabaseController : MonoBehaviour
{
    public GameObject MainScreen, WaitScreen;

    [SerializeField] private GameObject ReadyButton = null;
    [SerializeField] private GameObject ReturnButton = null;
    [SerializeField] private GameObject ButtonTemplate = null;
    [SerializeField] private GameObject[] Backgrounds = new GameObject[4];
    [SerializeField] private List<Data> Data = new List<Data>();

    private OnlineManager NetworkController;
    private string m_lobby;
    private int m_scene;

    private Dictionary<string, bool> m_readyPlayers = new Dictionary<string, bool>();

    int playerItemsLoaded = 0;

    async void Start()
    {
        NetworkController = new OnlineManager();

        MainScreen.SetActive(false);
        WaitScreen.SetActive(true);

        int scene = NetworkController.GetPlayerScene();
        if (scene > 0)
        {
            m_scene = scene;
            SetBackground();
            string lobby = await NetworkController.GetPlayerLobby();
            if (!string.IsNullOrEmpty(lobby))
            {
                string[] players = NetworkController.GetPlayers();
                m_lobby = lobby;
                foreach (var player in players) m_readyPlayers[player] = false;
                DownloadItems();
                NetworkController.RegisterCluesChanged(OnSlotChanged);
                NetworkController.RegisterReadyChanged(OnReadyChanged);
            }
            else SceneManager.LoadScene("Lobby");
        }
        else SceneManager.LoadScene("Lobby");

        for (int i = 0; i < Data.Count; i++)
        {
            Data data = Data[i];
            data.PlayerButton.GetComponent<Button>().onClick.AddListener(() => PlayerButtonPressed(data));
        }

        PlayerButtonPressed(Data[0]);
    }

    private void SetBackground()
    {
        if (m_scene <= Backgrounds.Length)
        {
            foreach (var bg in Backgrounds)
                bg.SetActive(false);

            Backgrounds[m_scene - 1].SetActive(true);
        }
    }

    public void ConfirmReady()
    {
        if (ReadyButton.activeSelf)
        {
            ReadyButton.SetActive(false);
            NetworkController.ReadyUp();
            ReadyButton.SetActive(true);
            ReadyButton.GetComponent<Image>().color = Color.yellow;
            foreach (Transform t in ReadyButton.gameObject.transform)
            {
                var text = t.GetComponent<Text>();
                if (text != null) text.text = "Waiting...";
            }
        }
    }

    public void ReturnButtonPressed()
    {
        if (ReturnButton.activeSelf)
        {
            ReturnButton.SetActive(false);
            NetworkController.DeregisterCluesChanged(OnSlotChanged);
            NetworkController.DeregisterReadyChanged(OnReadyChanged);
            SceneManager.LoadScene(m_scene);
        }
    }

    public void VotingButtonPressed()
    {
        if (!m_readyPlayers.Any(p => p.Value == false))
        {
            NetworkController.DeregisterCluesChanged(OnSlotChanged);
            NetworkController.DeregisterReadyChanged(OnReadyChanged);
            SceneManager.LoadScene("Voting");
        }
    }

    Data m_current = null;

    private void PlayerButtonPressed(Data data)
    {
        foreach (var button in Data.Select(d => d.PlayerButton))
        {
            ColorBlock colours = button.GetComponent<Button>().colors;
            colours.normalColor = colours.highlightedColor = Color.white;
            button.GetComponent<Button>().colors = colours;
        }
        ColorBlock colours2 = data.PlayerButton.GetComponent<Button>().colors;
        colours2.normalColor = colours2.highlightedColor = Color.green;
        data.PlayerButton.GetComponent<Button>().colors = colours2;

        foreach (var cluePanel in Data.Select(d => d.CluePanel))
        {
            cluePanel.SetActive(false);
        }
        data.CluePanel.SetActive(true);

        if (m_current != null)
        {
            for (int slot = 0; slot < m_current.Slots.Count; ++slot)
            {
                foreach (Transform t in m_current.Slots[slot].transform)
                {
                    foreach (Transform t2 in t)
                    {
                        if (t2.gameObject.name == "Alert")
                            t2.gameObject.SetActive(false);
                    }
                }
            }

            foreach (Transform t2 in m_current.PlayerButton.transform)
            {
                if (t2.gameObject.name == "Alert")
                    t2.gameObject.SetActive(false);
            }
        }

        for (int slot = 0; slot < data.Slots.Count; ++slot)
        {
            foreach (Transform t in data.Slots[slot].transform)
            {
                //Debug.Log("BTNPRS = player-" + Data.FindIndex(d => d == data) + "/slot-" + (slot + 1) + " = " + t.gameObject.name);
                StaticClues.SeenSlots.Add(new SlotData(Data.FindIndex(d => d == data).ToString(), (slot + 1).ToString(), t.gameObject.name, data.Slots[slot]));
            }
        }

        m_current = data;
    }

    public void PageChanged(GameObject oldPage, GameObject newPage)
    {
        /*Data oldData = Data.First(d => d.CluePanel == oldPage.transform.parent.gameObject);
        if (oldData != null)
        {
            for (int slot = 0; slot < oldData.Slots.Count; ++slot)
            {
                foreach (Transform t in oldData.Slots[slot].transform)
                {
                    foreach (Transform t2 in t)
                    {
                        if (t2.gameObject.name == "Alert")
                            t2.gameObject.SetActive(false);
                    }
                }
            }
        }*/

        Data newData = Data.First(d => d.CluePanel == newPage.transform.parent.gameObject);
        if (newData != null)
        {
            for (int slot = 0; slot < newData.Slots.Count; ++slot)
            {
                foreach (Transform t in newData.Slots[slot].transform)
                {
                    //Debug.Log("BTNPRS = player-" + Data.FindIndex(d => d == data) + "/slot-" + (slot + 1) + " = " + t.gameObject.name);
                    StaticClues.SeenSlots.Add(new SlotData(Data.FindIndex(d => d == newData).ToString(), (slot + 1).ToString(), t.gameObject.name, newData.Slots[slot]));
                }
            }
        }
    }

    public void UploadItem(int slot, ObjectHintData hint)
    {
        NetworkController.UploadDatabaseItem(slot, hint);
    }

    public void RemoveItem(int slot)
    {
        //if (!m_readyPlayers.Any(p => p.Value == false))
        //{
        NetworkController.RemoveDatabaseItem(slot);
        //}
    }

    private async void DownloadItems()
    {
        int tmp = 0;
        User player = await NetworkController.DownloadClues(tmp);
        for (int j = 0; j < player.Items.Length; j++)
        {
            int tmp2 = j;
            var clue = player.Items[tmp2];
            CheckPlayerItemsLoaded();
            if (!string.IsNullOrEmpty(clue.Name.Get()))
            {
                var slot = Data[tmp].Slots[tmp2];
                foreach (Transform t in slot.transform) if (t.gameObject.name == clue.Name.Get()) Destroy(t.gameObject);
                var newObj = Instantiate(ButtonTemplate, ButtonTemplate.transform.parent);
                newObj.SetActive(true);
                newObj.name = clue.Name.Get();
                newObj.transform.SetParent(slot.transform);
                if (!string.IsNullOrEmpty(clue.Image.Get()))
                {
                    foreach (Transform t in newObj.transform)
                    {
                        if (t.gameObject.GetComponent<Text>() != null)
                        {
                            t.gameObject.GetComponent<Text>().text = clue.Name.Get();
                        }
                        if (t.gameObject.GetComponent<Image>() != null)
                        {
                            t.gameObject.GetComponent<Image>().sprite = Resources.Load<Sprite>(clue.Image.Get());
                        }
                    }
                }
                else
                {
                    foreach (Transform t in newObj.transform)
                    {
                        if (t.gameObject.GetComponent<Text>() != null)
                        {
                            t.gameObject.GetComponent<Text>().text = clue.Name.Get();
                            t.gameObject.GetComponent<Text>().gameObject.SetActive(true);
                        }
                        if (t.gameObject.GetComponent<Image>() != null)
                        {
                            t.gameObject.GetComponent<Image>().gameObject.SetActive(false);
                        }
                    }
                }
                newObj.GetComponent<DragHandler>().enabled = false;
                newObj.GetComponent<Button>().onClick.AddListener(() => {
                    if (StaticSlot.TimesRemoved < StaticSlot.MaxRemovals)
                    {
                        slot.GetComponent<Slot>().Text.GetComponent<Text>().text = "";
                        RemoveItem(slot.GetComponent<Slot>().SlotNumber);
                        Destroy(newObj);
                        StaticSlot.TimesRemoved++;
                    }
                    else Debug.Log("YOU CANT GO THERE (EG. you have removed your maximum amount of times)");
                });
                slot.GetComponent<Slot>().Text.GetComponent<Text>().text = clue.Description.Get();
            }
        }
    }

    private async void OnSlotChanged(CloudNode entry)
    {
        if (ReadyButton == null)
            return;

        //Debug.Log(entry.Key + " | " + (args.Snapshot.Exists ? args.Snapshot.Value.ToString() : ""));

        string[] key = entry.Path.Split('/');
        if (key.Length >= 5)
        {
            string player = key[1];
            string field = key[4];

            if (entry.Exists())
            {
                string value = entry.Get();

                int slotNb = -1;
                if (!string.IsNullOrEmpty(value) && int.TryParse(key[3].Replace("slot-", ""), out slotNb))
                {
                    int playerNb = NetworkController.GetPlayerNumber(player);
                    var slot = Data[playerNb].Slots[slotNb - 1];
                    if (field == "name")
                    {
                        foreach (Transform t in slot.transform) if (t.gameObject.name == value) Destroy(t.gameObject);
                        var newObj = Instantiate(ButtonTemplate, ButtonTemplate.transform.parent);
                        newObj.SetActive(true);
                        newObj.name = value;
                        newObj.transform.SetParent(slot.transform);
                        foreach (Transform t in newObj.transform)
                        {
                            if (t.gameObject.GetComponent<Text>() != null)
                            {
                                t.gameObject.GetComponent<Text>().text = value;
                            }
                            //Debug.Log(string.Format("LOAD = player-{0}/slot-{1} = {2}", playerNb.ToString(), slotNb.ToString(), value));
                            if (t.gameObject.name == "Alert" && !StaticClues.SeenSlots.Any(s => s.Equals(new SlotData(playerNb.ToString(), slotNb.ToString(), value))))
                            {
                                t.gameObject.SetActive(true);
                                foreach (Transform t2 in Data[playerNb].PlayerButton.transform)
                                {
                                    if (t2.gameObject.name == "Alert")
                                        t2.gameObject.SetActive(true);
                                }
                            }
                        }
                        newObj.GetComponent<DragHandler>().enabled = false;
                        //CheckItemsLoaded();
                    }
                    else if (field == "hint")
                    {
                        slot.GetComponent<Slot>().Text.GetComponent<Text>().text = value;
                    }
                    else if (field == "image")
                    {
                        if (!string.IsNullOrEmpty(value))
                        {
                            foreach (Transform t1 in slot.transform)
                            {
                                foreach (Transform t in t1)
                                {
                                    if (t.gameObject.name == "Image" && t.gameObject.GetComponent<Image>() != null)
                                    {
                                        t.gameObject.GetComponent<Image>().sprite = Resources.Load<Sprite>(value);
                                    }
                                }
                            }
                        }
                        else
                        {
                            foreach (Transform t1 in slot.transform)
                            {
                                foreach (Transform t in t1)
                                {
                                    if (t.gameObject.name == "Image" && t.gameObject.GetComponent<Image>() != null)
                                    {
                                        t.gameObject.SetActive(false);
                                    }
                                    if (t.gameObject.GetComponent<Text>() != null)
                                    {
                                        t.gameObject.SetActive(true); // TODO: REMOVE TEMP FIX
                                    }
                                }
                            }
                        }
                    }
                }
            }
            else
            {
                int slotNb = -1;
                if (int.TryParse(key[3].Replace("slot-", ""), out slotNb))
                {
                    int playerNb = NetworkController.GetPlayerNumber(player);
                    var slot = Data[playerNb].Slots[slotNb - 1];

                    slot.GetComponent<Slot>().Text.GetComponent<Text>().text = "";

                    foreach (Transform t1 in slot.transform)
                    {
                        Destroy(t1.gameObject);
                    }
                }
            }
        }
    }

    private void CheckPlayerItemsLoaded()
    {
        playerItemsLoaded++;

        //if (playerItemsLoaded >= 24)
        {
            WaitScreen.SetActive(false);
            MainScreen.SetActive(true);
        }
    }

    private void OnReadyChanged(CloudNode entry)
    {
        if (ReadyButton == null)
            return;

        if (entry.Exists())
        {
            string value = entry.Get();

            if (value == "true")
            {
                string[] key = entry.Path.Split('/');
                string player = key[1];
                m_readyPlayers[player] = true;

                if (player == SignIn.User.Id)
                {
                    ConfirmReady();
                }

                if (!m_readyPlayers.Any(p => p.Value == false))
                {
                    VotingButtonPressed();
                }
            }
        }
    }
}
