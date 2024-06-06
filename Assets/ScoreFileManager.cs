using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using TMPro;
using UnityEngine;

public class ScoreFileManager : MonoBehaviour
{
    private int scoreSum = 0;
    private int timeSum = 0;
    private int saveNumber = 1;
    private string TimeSum_Formatted = string.Empty;
    public TMP_InputField PlayerName_Input;
    public TextMeshProUGUI PlayerName;

    [System.Serializable]
    public class PlayerData
    {
        public string PlayerName;
        public int HighScore;
        public string HighScoreTime;
        public int LastScore;
        public string LastScoreTime;

        public PlayerData(string playerName, int score, string time)
        {
            PlayerName = playerName;
            HighScore = score;
            HighScoreTime = time;
            LastScore = score;
            LastScoreTime = time;
        }
    }

    public void SaveData()
    {
        scoreSum = PlayerPrefs.GetInt("ScoreSum", 0); // Score Sum of the Session
        timeSum = PlayerPrefs.GetInt("TimeSum", 0); // Time Sum of the Session
        PlayerPrefs.SetInt("ScoreSum", 0); PlayerPrefs.SetInt("TimeSum", 0);

        int seconds = Mathf.FloorToInt(timeSum);
        int minutes = Mathf.FloorToInt(seconds / 60);
        int remainingseconds = seconds % 60;
        TimeSum_Formatted = string.Format("{0:0}:{1:00}", minutes, remainingseconds);

        string playerName = PlayerName_Input.text;
        PlayerName.text = playerName;

        if (string.IsNullOrEmpty(playerName))
        {
            Debug.LogWarning("Player Name is empty!");
            return;
        }

        string path = Path.Combine(Application.persistentDataPath, "PlayerScores.json");
        Dictionary<string, PlayerData> playerScores = new Dictionary<string, PlayerData>();

        // Load existing data if file exists
        if (File.Exists(path))
        {
            string json = File.ReadAllText(path);
            playerScores = JsonUtility.FromJson<Serialization<PlayerData>>(json).ToDictionary();
        }

        PlayerData playerData;
        if (playerScores.TryGetValue(playerName, out playerData))
        {
            // Update last score and time
            playerData.LastScore = scoreSum;
            playerData.LastScoreTime = TimeSum_Formatted;

            // Update high score and time if the new score is higher
            if (scoreSum > playerData.HighScore)
            {
                playerData.HighScore = scoreSum;
                playerData.HighScoreTime = TimeSum_Formatted;
            }
        }
        else
        {
            // Create new player data if player doesn't exist
            playerData = new PlayerData(playerName, scoreSum, TimeSum_Formatted);
            playerScores[playerName] = playerData;
        }

        // Serialize and save updated data
        string updatedJson = JsonUtility.ToJson(new Serialization<PlayerData>(playerScores), true);
        File.WriteAllText(path, updatedJson);

        Debug.Log("Data saved to " + path);

        // Increment save number for the next save
        saveNumber++;
    }

    [System.Serializable]
    public class Serialization<T>
    {
        public List<string> keys;
        public List<T> values;

        public Serialization(Dictionary<string, T> dictionary)
        {
            keys = new List<string>(dictionary.Keys);
            values = new List<T>(dictionary.Values);
        }

        public Dictionary<string, T> ToDictionary()
        {
            Dictionary<string, T> dict = new Dictionary<string, T>();
            for (int i = 0; i < keys.Count; i++)
            {
                dict[keys[i]] = values[i];
            }
            return dict;
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
