using System;
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;

public class GoogleStreetViewDownloader : MonoBehaviour
{
    public string apiKey = "YOUR_API_KEY"; // Your Google Street View API Key
    public string size = "600x300"; // Size of the image (width x height)
    public string identifier = "YOUR_IDENTIFIER"; // The identifier of the location

    private string imageUrl;
    private string filePath;

    void Start()
    {
        StartCoroutine(DownloadImage());
    }

    IEnumerator DownloadImage()
    {
        // Construct the URL for Google Street View API
        imageUrl = $"https://maps.googleapis.com/maps/api/streetview?size={size}&location={identifier}&key={apiKey}";

        // Start downloading the image
        UnityWebRequest www = UnityWebRequestTexture.GetTexture(imageUrl);
        yield return www.SendWebRequest();

        // Check for errors
        if (www.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError("Failed to download image: " + www.error);
            yield break;
        }

        // Get the downloaded texture
        Texture2D texture = ((DownloadHandlerTexture)www.downloadHandler).texture;

        // Save the texture as a PNG file in the Assets folder
        filePath = Application.dataPath + $"/StreetView_{identifier}.png";
        File.WriteAllBytes(filePath, texture.EncodeToPNG());

        Debug.Log("Image downloaded and saved: " + filePath);

        // Create a skybox material
        Material skyboxMaterial = new Material(Shader.Find("Skybox/Panoramic"));
        skyboxMaterial.SetTexture("_Tex", texture);

        // Apply the skybox material to the skybox
        RenderSettings.skybox = skyboxMaterial;
    }
}
