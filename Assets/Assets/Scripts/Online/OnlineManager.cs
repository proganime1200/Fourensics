using Firebase.Database;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

public enum LobbyState { Lobby, InGame, Voting, Finished }

public enum LobbyError { None, Unknown, TooFewPlayers, TooManyPlayers }

class OnlineManager
{
    private OnlineDatabase m_database;

    public OnlineManager()
    {
        m_database = new OnlineDatabase();
    }

    #region Async methods

    /// <summary>
    /// If the player is listed as being in a lobby and that lobby does exist, returns the lobby
    /// code. Otherwise, returns null.
    /// </summary>
    public async Task<string> GetPlayerLobby()
    {
        SignIn.Lobby = await Lobby.Fetch(SignIn.User.Lobby.Get());
        bool exists = SignIn.Lobby.State.Exists();
        if (exists) return SignIn.Lobby.Id;
        else
        {
            SignIn.User.Lobby.Set("");
            SignIn.User.Scene.Set("");
            SignIn.User.Ready.Set("");
            SignIn.User.Vote.Set("");
            foreach (var item in SignIn.User.Items)
            {
                item.Name.Set("");
                item.Description.Set("");
                item.Image.Set("");
            }
            return null;
        }
    }

    /// <summary>
    /// If the player is listed as being in a scene and that scene does exist, returns the scene
    /// number. Otherwise, returns 0.
    /// </summary>
    public int GetPlayerScene()
    {
        int scene;
        if (int.TryParse(SignIn.User.Scene.Get(), out scene)) return scene;
        else return 0;
    }

    /// <summary>
    /// If lobby exists, updates player entry to new lobby and adds player to lobby.
    /// </summary>
    public async Task<bool> JoinLobby(string code, int maxPlayers)
    {
        Lobby lobby = await Lobby.Fetch(code);
        bool exists = SignIn.Lobby.State.Exists();
        if (exists)
        {
            List<string> players = SignIn.Lobby.Users.Select(u => u.Get()).ToList();
            players.RemoveAll(s => string.IsNullOrEmpty(s));
            if (players.Count < maxPlayers && !players.Contains(SignIn.User.Id))
            {
                SignIn.Lobby = lobby;
                SignIn.User.Lobby.Set(code);
                
                players.Add(SignIn.User.Id);
                for (int i = 0; i < players.Count; i++)
                    SignIn.Lobby.Users[i].Set(players[i]);
                return true;
            }
            else return false;
        }
        else return false;
    }

    /// <summary>
    /// Creates a lobby on the server.
    /// </summary>
    public async void CreateLobby(string code)
    {
        SignIn.Lobby = await Lobby.Fetch(code);

        //SignIn.Lobby.CreatedTime.Set(DateTimeOffset.UtcNow.ToString("o"));
        SignIn.Lobby.State.Set(((int)LobbyState.Lobby).ToString());
    }

    /// <summary>
    /// Attempts to generate a lobby code which is not in use. If all codes generated are in
    /// use, returns null.
    /// </summary>
    public async Task<string> CreateLobbyCode()
    {
        for (int i = 0; i < 3; i++)
        {
            string code = GenerateRandomCode();
            string key = $"lobbies/{code}/state";

            if (!await m_database.Exists(key))
            {
                return code;
            }
        }

        return null;
    }

    /// <summary>
    /// Deletes the player entry and removes the player from the lobby. If no players are left in the
    /// lobby, deletes the lobby.
    /// </summary>
    public void LeaveLobby()
    {
        SignIn.User.Lobby.Set("");
        SignIn.User.Scene.Set("");
        SignIn.User.Ready.Set("");
        SignIn.User.Vote.Set("");
        foreach (var item in SignIn.User.Items)
        {
            item.Name.Set("");
            item.Description.Set("");
            item.Image.Set("");
        }

        // Delete 'players/{0}', pull 'lobbies/{1}/players', remove the player from list and push
        // 'lobbies/{1}/players' back up (unless there are no players left, then delete the lobby).
        /*m_player.Delete(success1 => {
            if (success1) {
                //m_lobby = new Lobby(m_database, code); // TODO
                m_lobby.Players.Pull(success2 => {
                    if (success2) {
                        //List<string> layers = m_lobby.Players.Value.Split(',').ToList();
                        //layers.Remove(m_player.Id);
                        //layers.RemoveAll(s => string.IsNullOrEmpty(s));
                        //if (layers.Count > 0) {
                        //    m_lobby.Players.Value = string.Join(",", layers.ToArray());
                        //    m_lobby.Players.Push(returnSuccess);
                        //} else {
                            m_lobby.Delete(returnSuccess);
                        //}
                    }
                    else returnSuccess(false);
                });
            }
            else returnSuccess(false);
        });*/
    }

    /*
    /// <summary>
    /// Checks if the lobby has the required number of players.
    /// </summary>
    public LobbyError CanStartGame()
    {
        // TODO: remove this in final build
        return LobbyError.None;
        
        bool success = await m_lobby.Players.Pull();
        if (success)
        {
            List<string> players = SignIn.Lobby.Users.Select(u => u.Get()).ToList();
            players.RemoveAll(s => string.IsNullOrEmpty(s));
            if (players.Count < requiredPlayers) return LobbyError.TooFewPlayers;
            else if (players.Count > requiredPlayers) return LobbyError.TooManyPlayers;
            else return LobbyError.None;
        }
        else return LobbyError.Unknown;
    }
    */

    /// <summary>
    /// Pushes a new lobby state to the server.
    /// </summary>
    public void SetLobbyState(LobbyState state)
    {
        SignIn.Lobby.State.Set(((int)state).ToString());
    }

    /// <summary>
    ///
    /// </summary>
    public async Task<int> AssignPlayerScenes(string code)
    {
        List<string> players = SignIn.Lobby.Users.Select(u => u.Get()).ToList();
        players.RemoveAll(s => string.IsNullOrEmpty(s));
        players = players.OrderBy(_ => UnityEngine.Random.value).ToList();
        int ourScene = -1;
        for (int i = 0; i < players.Count; i++)
        {
            User player = await User.Fetch(players[i]);
            if (player.Id == SignIn.User.Id)
            {
                ourScene = (i + 1);
            }
            player.Scene.Set((i + 1).ToString());
        }
        return ourScene;
    }

    #endregion

    #region Async database methods

    public void UploadDatabaseItem(int slot, ObjectHintData hint)
    {
        SignIn.User.Items[slot - 1].Name.Set(hint.Name);
        SignIn.User.Items[slot - 1].Description.Set(hint.Hint);
        SignIn.User.Items[slot - 1].Image.Set(hint.Image);
    }

    public void RemoveDatabaseItem(int slot)
    {
        UploadDatabaseItem(slot, new ObjectHintData("", "", ""));
    }

    public async void RegisterCluesChanged(Action<CloudNode> listener)
    {
        List<string> players = SignIn.Lobby.Users.Select(u => u.Get()).ToList();
        players.RemoveAll(s => string.IsNullOrEmpty(s));
        players.Remove(SignIn.User.Id);
        foreach (string playerId in players)
        {
            User player = await User.Fetch(playerId);
            foreach (var clue in player.Items)
            {
                clue.Name.OnValueChanged += listener;
                clue.Description.OnValueChanged += listener;
                clue.Image.OnValueChanged += listener;
            }
        }
    }

    public async void DeregisterCluesChanged(Action<CloudNode> listener) // TODO: make all these functions static
    {
        List<string> players = SignIn.Lobby.Users.Select(u => u.Get()).ToList();
        players.RemoveAll(s => string.IsNullOrEmpty(s));
        players.Remove(SignIn.User.Id);
        foreach (string playerId in players)
        {
            User player = await User.Fetch(playerId);
            foreach (var clue in player.Items)
            {
                clue.Name.OnValueChanged -= listener;
                clue.Description.OnValueChanged -= listener;
                clue.Image.OnValueChanged -= listener;
            }
        }
    }

    public int GetPlayerNumber(string player)
    {
        List<string> players = SignIn.Lobby.Users.Select(u => u.Get()).ToList();
        players.RemoveAll(s => string.IsNullOrEmpty(s));
        players.Remove(SignIn.User.Id);
        players.Insert(0, SignIn.User.Id);
        int playerNb = players.IndexOf(player);
        if (playerNb >= 0 && playerNb < players.Count)
        {
            return playerNb;
        }
        else return -1;
    }

    public async Task<User> DownloadClues(int playerNb)
    {
        List<string> players = SignIn.Lobby.Users.Select(u => u.Get()).ToList();
        players.RemoveAll(s => string.IsNullOrEmpty(s));
        players.Remove(SignIn.User.Id);
        players.Insert(0, SignIn.User.Id);
        if (playerNb < players.Count)
        {
            return await User.Fetch(players[playerNb]);
        }
        else return null;
    }

    #endregion

    #region Async voting methods

    public string[] GetPlayers()
    {
        List<string> players = SignIn.Lobby.Users.Select(u => u.Get()).ToList();
        players.RemoveAll(s => string.IsNullOrEmpty(s));
        return players.ToArray();
    }

    public void ReadyUp()
    {
        SignIn.User.Ready.Set("true");
    }

    public void SubmitVote(string suspect)
    {
        SignIn.User.Vote.Set(suspect);
    }

    public async void RegisterReadyChanged(Action<CloudNode> listener)
    {
        List<string> players = SignIn.Lobby.Users.Select(u => u.Get()).ToList();
        players.RemoveAll(s => string.IsNullOrEmpty(s));
        //players.Remove(m_player.Id);
        foreach (string playerId in players)
        {
            User player = await User.Fetch(playerId);
            player.Ready.OnValueChanged += listener;
        }
    }

    public async void DeregisterReadyChanged(Action<CloudNode> listener) // TODO: make all these functions static
    {
        List<string> players = SignIn.Lobby.Users.Select(u => u.Get()).ToList();
        players.RemoveAll(s => string.IsNullOrEmpty(s));
        //players.Remove(m_player.Id);
        foreach (string playerId in players)
        {
            User player = await User.Fetch(playerId);
            player.Ready.OnValueChanged -= listener;
        }
    }

    public async void RegisterVoteChanged(Action<CloudNode> listener)
    {
        List<string> players = SignIn.Lobby.Users.Select(u => u.Get()).ToList();
        players.RemoveAll(s => string.IsNullOrEmpty(s));
        //players.Remove(m_player.Id);
        foreach (string playerId in players)
        {
            User player = await User.Fetch(playerId);
            player.Vote.OnValueChanged += listener;
        }
    }

    public async void DeregisterVoteChanged(Action<CloudNode> listener) // TODO: make all these functions static
    {
        List<string> players = SignIn.Lobby.Users.Select(u => u.Get()).ToList();
        players.RemoveAll(s => string.IsNullOrEmpty(s));
        //players.Remove(m_player.Id);
        foreach (string playerId in players)
        {
            User player = await User.Fetch(playerId);
            player.Vote.OnValueChanged -= listener;
        }
    }

    #endregion

    #region Listeners

    public void RegisterListener(string path, EventHandler<ValueChangedEventArgs> listener)
    {
        m_database.RegisterListener(path, listener);
    }

    public void DeregisterListener(string path, EventHandler<ValueChangedEventArgs> listener)
    {
        m_database.DeregisterListener(path, listener);
    }

    #endregion

    #region Utility methods

    /// <summary>
    /// Generates a random five-character room code.
    /// </summary>
    private static string GenerateRandomCode()
    {
        const string validChars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";

        string roomCode = "";

        for (int i = 0; i < 5; i++)
        {
            // Gets a random valid character and adds it to the room code string.
            int randomIndex = UnityEngine.Random.Range(0, validChars.Length - 1);
            char randomChar = validChars[randomIndex];
            roomCode += randomChar;
        }

        return roomCode;
    }

    #endregion
}
