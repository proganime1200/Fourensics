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
    /// If lobby exists, updates player entry to new lobby and adds player to lobby.
    /// </summary>
    public static bool JoinLobby(Lobby lobby, int maxPlayers)
    {
        // Check if lobby exists
        if (lobby.State.Value == null)
            return false;

        // Get list of players in lobby
        List<string> players = lobby.Users
            .Where(u => u.Value != null)
            .Select(u => u.Value)
            .ToList();

        // If player is already in room
        if (players.Contains(SignInScene.User.Id))
            return true;

        // If too many players
        if (players.Count >= maxPlayers)
            return false;

        // Add player to lobby
        players.Add(SignInScene.User.Id);
        for (int i = 0; i < players.Count; i++)
            lobby.Users[i].Value = players[i];

        return true;
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
        SignInScene.User.Lobby.Value = null;
        SignInScene.User.Scene.Value = 0;
        SignInScene.User.Ready.Value = false;
        SignInScene.User.Vote.Value = null;
        foreach (var item in SignInScene.User.Items)
        {
            item.Name.Value = null;
            item.Description.Value = null;
            item.Image.Value = null;
        }
    }

    /// <summary>
    ///
    /// </summary>
    public async Task<int> AssignPlayerScenes(string code)
    {
        List<string> players = LobbyScene.Lobby.Users.Select(u => u.Value).ToList();
        players.RemoveAll(s => string.IsNullOrEmpty(s));
        players = players.OrderBy(_ => UnityEngine.Random.value).ToList();
        int ourScene = -1;
        for (int i = 0; i < players.Count; i++)
        {
            User player = await User.Fetch(players[i]);
            if (player.Id == SignInScene.User.Id)
            {
                ourScene = (i + 1);
            }
            player.Scene.Value = i + 1;
        }
        return ourScene;
    }

    #endregion

    #region Async database methods

    public void UploadDatabaseItem(int slot, ObjectHintData hint)
    {
        SignInScene.User.Items[slot - 1].Name.Value = hint.Name;
        SignInScene.User.Items[slot - 1].Description.Value = hint.Hint;
        SignInScene.User.Items[slot - 1].Image.Value = hint.Image;
    }

    public void RemoveDatabaseItem(int slot)
    {
        UploadDatabaseItem(slot, new ObjectHintData("", "", ""));
    }

    public async void RegisterCluesChanged(Action<CloudNode> listener)
    {
        List<string> players = LobbyScene.Lobby.Users.Select(u => u.Value).ToList();
        players.RemoveAll(s => string.IsNullOrEmpty(s));
        players.Remove(SignInScene.User.Id);
        foreach (string playerId in players)
        {
            User player = await User.Fetch(playerId);
            foreach (var clue in player.Items)
            {
                clue.Name.ValueChanged += listener;
                clue.Description.ValueChanged += listener;
                clue.Image.ValueChanged += listener;
            }
        }
    }

    public async void DeregisterCluesChanged(Action<CloudNode> listener) // TODO: make all these functions static
    {
        List<string> players = LobbyScene.Lobby.Users.Select(u => u.Value).ToList();
        players.RemoveAll(s => string.IsNullOrEmpty(s));
        players.Remove(SignInScene.User.Id);
        foreach (string playerId in players)
        {
            User player = await User.Fetch(playerId);
            foreach (var clue in player.Items)
            {
                clue.Name.ValueChanged -= listener;
                clue.Description.ValueChanged -= listener;
                clue.Image.ValueChanged -= listener;
            }
        }
    }

    public int GetPlayerNumber(string player)
    {
        List<string> players = LobbyScene.Lobby.Users.Select(u => u.Value).ToList();
        players.RemoveAll(s => string.IsNullOrEmpty(s));
        players.Remove(SignInScene.User.Id);
        players.Insert(0, SignInScene.User.Id);
        int playerNb = players.IndexOf(player);
        if (playerNb >= 0 && playerNb < players.Count)
        {
            return playerNb;
        }
        else return -1;
    }

    public async Task<User> DownloadClues(int playerNb)
    {
        List<string> players = LobbyScene.Lobby.Users.Select(u => u.Value).ToList();
        players.RemoveAll(s => string.IsNullOrEmpty(s));
        players.Remove(SignInScene.User.Id);
        players.Insert(0, SignInScene.User.Id);
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
        List<string> players = LobbyScene.Lobby.Users.Select(u => u.Value).ToList();
        players.RemoveAll(s => string.IsNullOrEmpty(s));
        return players.ToArray();
    }

    public async void RegisterReadyChanged(Action<CloudNode<bool>> listener)
    {
        List<string> players = LobbyScene.Lobby.Users.Select(u => u.Value).ToList();
        players.RemoveAll(s => string.IsNullOrEmpty(s));
        //players.Remove(m_player.Id);
        foreach (string playerId in players)
        {
            User player = await User.Fetch(playerId);
            player.Ready.ValueChanged += listener;
        }
    }

    public async void DeregisterReadyChanged(Action<CloudNode<bool>> listener) // TODO: make all these functions static
    {
        List<string> players = LobbyScene.Lobby.Users.Select(u => u.Value).ToList();
        players.RemoveAll(s => string.IsNullOrEmpty(s));
        //players.Remove(m_player.Id);
        foreach (string playerId in players)
        {
            User player = await User.Fetch(playerId);
            player.Ready.ValueChanged -= listener;
        }
    }

    public async void RegisterVoteChanged(Action<CloudNode> listener)
    {
        List<string> players = LobbyScene.Lobby.Users.Select(u => u.Value).ToList();
        players.RemoveAll(s => string.IsNullOrEmpty(s));
        //players.Remove(m_player.Id);
        foreach (string playerId in players)
        {
            User player = await User.Fetch(playerId);
            player.Vote.ValueChanged += listener;
        }
    }

    public async void DeregisterVoteChanged(Action<CloudNode> listener) // TODO: make all these functions static
    {
        List<string> players = LobbyScene.Lobby.Users.Select(u => u.Value).ToList();
        players.RemoveAll(s => string.IsNullOrEmpty(s));
        //players.Remove(m_player.Id);
        foreach (string playerId in players)
        {
            User player = await User.Fetch(playerId);
            player.Vote.ValueChanged -= listener;
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
