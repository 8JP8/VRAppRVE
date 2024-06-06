using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.UI;
using TMPro;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using System.IO;
using System;

public class ConfigurationPicker : MonoBehaviour
{

    //public Button StartGame_Button;
    public TMP_Dropdown Difficulty_Dropdown;
    public Slider Velocity_Slider;
    public Slider Countdown_Time_Slider;
    public Toggle Timer_Mode_Toggle;
    public Material[] skyboxMaterials; // Assign your skybox materials in the inspector
    public TextMeshProUGUI GameOver_Score;
    public TextMeshProUGUI GameOver_TotalTime;
    public GameObject GameOverPanel;


    private bool timerMode;
    private int difficultyLevel;
    private float speed;
    private int map_index;
    private int countdown_time;
    private int scoreSum = 0;
    private int timeSum = 0;
    private string TimeSum_Formatted = "";

    void Start()
    {
        //Game Over Handling
        if ((PlayerPrefs.GetInt("ScoreSum", 0) != 0) && (PlayerPrefs.GetInt("TimeSum", 0) != 0))
        {
            scoreSum = PlayerPrefs.GetInt("ScoreSum", 0); //Score Sum of the Session
            timeSum = PlayerPrefs.GetInt("TimeSum", 0); //Time Sum of the Session
            PlayerPrefs.SetInt("ScoreSum", 0); PlayerPrefs.SetInt("TimeSum", 0);

            GameOver_Score.text = scoreSum.ToString();
            int seconds = Mathf.FloorToInt(timeSum);
            int minutes = Mathf.FloorToInt(seconds / 60);
            int remainingseconds = seconds % 60;
            TimeSum_Formatted = string.Format("{0:0}:{1:00}", minutes, remainingseconds);
            GameOver_TotalTime.text = TimeSum_Formatted;
            GameOverPanel.SetActive(true);
        }
    }

    public void StartButton_Click()
    {
        PlayerPrefs.SetInt("ScoreSum", 0); PlayerPrefs.SetInt("TimeSum", 0);
        difficultyLevel = Difficulty_Dropdown.value;
        PlayerPrefs.SetInt("Difficulty", difficultyLevel);
        speed = Velocity_Slider.value;
        PlayerPrefs.SetFloat("ObjectSpeed", speed);
        timerMode = Timer_Mode_Toggle.isOn;
        PlayerPrefs.SetInt("TimerMode", timerMode ? 1 : 0);
        countdown_time = (int)Countdown_Time_Slider.value;
        PlayerPrefs.SetInt("CountdownTime", countdown_time);
        map_index = PlayerPrefs.GetInt("MapIndex", 0);
        if (difficultyLevel == 1) PlayerPrefs.SetInt("WinScore", 3); else if (difficultyLevel == 2) PlayerPrefs.SetInt("WinScore", 5); else PlayerPrefs.SetInt("WinScore", 2); //Set Initial WinScore
        TeleportToGameScene(map_index);
        Debug.Log($"{speed} {timerMode} {difficultyLevel} {countdown_time}");
    }

    public void TeleportToGameScene(int index)
    {
        // Load the skybox material
        Material skyboxMaterial = null;
        if (index >= 0 && index < skyboxMaterials.Length)
        {
            skyboxMaterial = skyboxMaterials[index];
        }
        else
        {
            Debug.LogWarning($"Skybox material for index {index} not found.");
        }

        // Teleport logic here
        SceneManager.LoadSceneAsync($"GameScene{/*index*/1}").completed += (operation) =>
        {
            // Apply the skybox material after the scene has finished loading
            if (skyboxMaterial != null)
            {
                RenderSettings.skybox = skyboxMaterial;
                Debug.Log($"Skybox changed to {skyboxMaterial.name}.");
            }

            Debug.Log($"GameScene{/*index + */1} Loaded...");
        };               
    }
}

