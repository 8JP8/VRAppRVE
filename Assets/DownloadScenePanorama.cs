using System;
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;
using TMPro;

public class GoogleStreetViewDownloader : MonoBehaviour
{
    public string apiKey = "YOUR_API_KEY"; // Your Google Street View API Key
    public string size = "600x300"; // Size of the image (width x height)
    private string identifier = "YOUR_IDENTIFIER"; // The identifier of the location

    private string imageUrl;
    private string filePath;

    public TextMeshProUGUI urlInputField; // Reference to the TextMeshProUGUI input field

    void Start()
    {
        // Optional: You can start downloading an initial image if needed
        // StartCoroutine(DownloadImage());
    }

    IEnumerator DownloadImage()
{
    // Construct the URL for Google Street View API
    imageUrl = $"https://maps.googleapis.com/maps/api/streetview?size={size}&location={identifier}&key={apiKey}";

    // Log the constructed URL
    Debug.Log("Constructed URL: " + imageUrl);

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

    public void SetLocationFromUrl()
    {
        string url = urlInputField.text;

        // Extract the location identifier from the URL
        identifier = ExtractLocationIdentifier(url);

        if (!string.IsNullOrEmpty(identifier))
        {
            Debug.Log("Location identifier extracted: " + identifier);
            StartCoroutine(DownloadImage());
        }
        else
        {
            Debug.LogError("Failed to extract location identifier from the URL.");
        }
    }

    private string ExtractLocationIdentifier(string url)
    {
        try
        {
            Uri uri = new Uri(url);
            string query = uri.Query;

            // Check if the query contains latitude and longitude
            if (query.Contains("3d") && query.Contains("4d"))
            {
                string[] queryParts = query.Split(new[] { '!', '?' }, StringSplitOptions.RemoveEmptyEntries);

                foreach (var part in queryParts)
                {
                    if (part.StartsWith("3d"))
                    {
                        string[] coordinates = part.Substring(2).Split(',');

                        if (coordinates.Length >= 2)
                        {
                            string lat = coordinates[0];
                            string lng = coordinates[1];

                            if (!string.IsNullOrEmpty(lat) && !string.IsNullOrEmpty(lng))
                            {
                                return $"{lat},{lng}";
                            }
                        }
                    }
                }
            }

            // Extract latitude and longitude from URL path if not found in query
            string[] segments = uri.Segments;
            foreach (var segment in segments)
            {
                if (segment.StartsWith("@"))
                {
                    string coordsPart = segment.TrimStart('@');
                    string[] coordinates = coordsPart.Split(',');

                    if (coordinates.Length >= 2)
                    {
                        string lat = coordinates[0];
                        string lng = coordinates[1];

                        if (!string.IsNullOrEmpty(lat) && !string.IsNullOrEmpty(lng))
                        {
                            return $"{lat},{lng}";
                        }
                    }
                }
            }
        }
        catch (Exception e)
        {
            Debug.LogError("Error parsing URL: " + e.Message);
        }

        return null;
    }
}
