using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.UI;
using TMPro;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using System.IO;
using System;
using static ScoreFileManager;
using System.Reflection;

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
    public TextMeshProUGUI HighScore_Value_Label;
    public GameObject GameOverPanel;
    public Transform Score_Table;
    public TMP_Dropdown CustomMap_DropDown;
    private string customMapDirectoryPath = "CustomMaps";
    List<string> customMapImageFileNames = new List<string>();
    public Image customMapPreview;

    private bool timerMode;
    private int difficultyLevel;
    private float speed;
    private int map_index;
    private int countdown_time;
    private int scoreSum = 0;
    private int timeSum = 0;
    private string TimeSum_Formatted = "";
    private bool customMap = false;

    void Start()
    {
        string directoryPath = Path.Join(Application.persistentDataPath, customMapDirectoryPath);
        if (!Directory.Exists(directoryPath)) { Directory.CreateDirectory(directoryPath);}
        DisplaySortedPlayerScores(Score_Table, HighScore_Value_Label);
        PlayerPrefs.SetInt("MapIndex", 0);
        CustomMapSelectorUpdate();
        //Game Over Handling
        if ((PlayerPrefs.GetInt("ScoreSum", 0) != 0) && (PlayerPrefs.GetInt("TimeSum", 0) != 0))
        {
            scoreSum = PlayerPrefs.GetInt("ScoreSum", 0); //Score Sum of the Session
            timeSum = PlayerPrefs.GetInt("TimeSum", 0); //Time Sum of the Session

            GameOver_Score.text = scoreSum.ToString();
            int seconds = Mathf.FloorToInt(timeSum);
            int minutes = Mathf.FloorToInt(seconds / 60);
            int remainingseconds = seconds % 60;
            TimeSum_Formatted = string.Format("{0:0}:{1:00}", minutes, remainingseconds);
            GameOver_TotalTime.text = TimeSum_Formatted;
            GameOverPanel.SetActive(true);
        }
    }


    public void UpdateScoreBoard() { DisplaySortedPlayerScores(Score_Table, HighScore_Value_Label); }

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
        //Debug.Log($"{speed} {timerMode} {difficultyLevel} {countdown_time}");
    }

    public void OpenKeyboard()
    {
        TouchScreenKeyboard.Open("");
    }

    public void TeleportToGameScene(int index)
    {
        // Load the skybox material
        Material skyboxMaterial = null;
        if (customMap)
        {
            string filepath = Path.Join(Application.persistentDataPath, customMapDirectoryPath);
            try { filepath = Path.Join(filepath, customMapImageFileNames[CustomMap_DropDown.value] + ".png"); } catch { }
            // Load the stitched texture
            Texture2D texture = new Texture2D(4096, 2048);
            texture.LoadImage(File.ReadAllBytes(filepath));
            // Create a skybox material for the 360 image
            skyboxMaterial = new Material(Shader.Find("Skybox/Panoramic"));
            skyboxMaterial.SetTexture("_MainTex", texture);
            skyboxMaterial.SetFloat("_Exposure", 1.0f);
            skyboxMaterial.SetFloat("_Rotation", 0);
        }
        else
        {
            if (index >= 0 && index < skyboxMaterials.Length)
            {
                skyboxMaterial = skyboxMaterials[index];
            }
            else
            {
                Debug.LogWarning($"Skybox material for index {index} not found.");
            }
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

    public void CustomMapSelectorPreviewUpdate()
    {
        string filepath = "";
        try { filepath = Path.Join(Application.persistentDataPath, customMapDirectoryPath, customMapImageFileNames[CustomMap_DropDown.value]+".png"); } catch { }
        // Check if the file exists
        if (File.Exists(filepath))
        {
            // Read the bytes from the file
            byte[] fileData = File.ReadAllBytes(filepath);

            // Create a new Texture2D
            Texture2D texture = new Texture2D(2, 2);
            texture.LoadImage(fileData); // Load the image data into the texture

            // Assign the texture to the RawImage
            Sprite sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), Vector2.one * 0.5f);
            customMapPreview.sprite = sprite;
        }
        else
        {
            Debug.LogError("File not found: " + filepath);
        }
    }

    public void CustomMapSelectorUpdate()
    {
        // Clear existing options
        CustomMap_DropDown.ClearOptions();

        // Get image file names without extension from the specified directory
        customMapImageFileNames = GetImageFileNames();

        // Add each image file name to the dropdown options
        CustomMap_DropDown.AddOptions(customMapImageFileNames);

        CustomMapSelectorPreviewUpdate();
    }

    public void CustomMap(bool slider)
    {
        if (slider) { customMap = true; } else { customMap = false; }
    }

    List<string> GetImageFileNames()
    {
        List<string> fileNames = new List<string>();

        // Check if the directory exists
        if (Directory.Exists(Path.Join(Application.persistentDataPath, customMapDirectoryPath)))
        {
            // Get all image files in the directory
            string[] files = Directory.GetFiles(Path.Join(Application.persistentDataPath, customMapDirectoryPath), "*.png");

            foreach (string file in files)
            {
                // Get just the file name without extension
                string fileName = Path.GetFileNameWithoutExtension(file);
                fileNames.Add(fileName);
            }
        }
        else
        {
            Debug.LogError("Directory not found: " + Path.Join(Application.persistentDataPath, customMapDirectoryPath));
        }
        return fileNames;
    }
}