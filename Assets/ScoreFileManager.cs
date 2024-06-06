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
        public string playerName;
        public int saveNumber;
        public string gameTime;

        public PlayerData(string playerName, int saveNumber, string gameTime)
        {
            this.playerName = playerName;
            this.saveNumber = saveNumber;
            this.gameTime = gameTime;
        }
    }

    public void SaveData()
    {
        scoreSum = PlayerPrefs.GetInt("ScoreSum", 0); //Score Sum of the Session
        timeSum = PlayerPrefs.GetInt("TimeSum", 0); //Time Sum of the Session

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

        PlayerData playerData = new PlayerData(playerName, scoreSum, TimeSum_Formatted);
        string json = JsonUtility.ToJson(playerData, true);

        string path = Path.Combine(Application.persistentDataPath, playerName + "_save.json");

        File.WriteAllText(path, json);

        Debug.Log("Data saved to " + path);

        // Increment save number for the next save
        saveNumber++;
    }


    // Update is called once per frame
    void Update()
    {
        
    }
}
