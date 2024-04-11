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
    public Toggle Timer_Mode_Toggle;
    public Material[] skyboxMaterials; // Assign your skybox materials in the inspector


    private bool timerMode;
    private int difficultyLevel;
    private float speed;
    private int map_index;

    public void StartButton_Click()
    {
        difficultyLevel = Difficulty_Dropdown.value;
        PlayerPrefs.SetInt("Difficulty", (int)difficultyLevel);
        speed = Velocity_Slider.value;
        PlayerPrefs.SetFloat("ObjectSpeed", (float)speed);
        timerMode = Timer_Mode_Toggle.isOn;
        PlayerPrefs.SetInt("TimerMode", timerMode ? 1 : 0);
        map_index = PlayerPrefs.GetInt("MapIndex", 0);
        TeleportToGameScene(map_index);
        Debug.Log($"{speed} {timerMode} {difficultyLevel}");
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

            Debug.Log($"Teleporting to GameScene {index + 1}...");
        };
    }
}

