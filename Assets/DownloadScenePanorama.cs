using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using TMPro;
using System.Linq;
using System.Collections;

public class GoogleStreetViewDownloader : MonoBehaviour
{
    public string apiKey = "AIzaSyC7FDiMc8jqeKgQAbY4l4oXcU97OEHmrhE"; // Your Google Street View API Key
    public string size = "1920x1080"; // Size of the image (width x height)
    private string identifier; // The identifier of the location

    private string imageUrl;
    private string previewFilePath;
    private string tilesFolderPath;
    private int zoomLevel = 4; // Set zoom level for tiles

    public TextMeshProUGUI urlInputField; // Reference to the TextMeshProUGUI input field
    public Image imageObject; // The UI Image object to update

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

        // Define file paths
        string previewFolderPath = Application.dataPath + "/Scenes/PreviewImages";
        tilesFolderPath = Application.dataPath + "/Scenes/360 Images/Custom";

        // Ensure the directories exist
        if (!Directory.Exists(previewFolderPath))
        {
            Directory.CreateDirectory(previewFolderPath);
        }

        if (!Directory.Exists(tilesFolderPath))
        {
            Directory.CreateDirectory(tilesFolderPath);
        }

        // Save the preview image as a PNG file in the PreviewImages folder
        previewFilePath = previewFolderPath + $"/StreetView_{identifier.Replace(',', '_')}.png";
        File.WriteAllBytes(previewFilePath, texture.EncodeToPNG());

        Debug.Log("Image downloaded and saved: " + previewFilePath);

        // Update the UI image object
        Sprite sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f));
        imageObject.sprite = sprite;

        // Download and stitch the 360 tiles
        yield return StartCoroutine(DownloadAndStitchTiles());

        // Load the stitched texture
        Texture2D stitchedTexture = new Texture2D(4096, 2048);
        stitchedTexture.LoadImage(File.ReadAllBytes(tilesFolderPath + $"/StreetView_{identifier.Replace(',', '_')}_stitched.png"));
        
        // Create a skybox material for the 360 image
        Material skyboxMaterial = new Material(Shader.Find("Skybox/Panoramic"));
        skyboxMaterial.SetTexture("_MainTex", stitchedTexture);
        skyboxMaterial.SetFloat("_Exposure", 1.0f);
        skyboxMaterial.SetFloat("_Rotation", 0);

        // Apply the skybox material to the skybox
        RenderSettings.skybox = skyboxMaterial;
    }

    IEnumerator DownloadAndStitchTiles()
    {
        List<(int, int, string, string)> tiles = TilesInfo(zoomLevel);

        foreach (var (x, y, _, tileUrl) in tiles)
        {
            Debug.Log($"Downloading tile: x={x}, y={y}, URL={tileUrl}");

            using (UnityWebRequest www = UnityWebRequestTexture.GetTexture(tileUrl))
            {
                yield return www.SendWebRequest();

                if (www.result != UnityWebRequest.Result.Success)
                {
                    Debug.LogError("Failed to download tile: " + www.error);
                    yield break;
                }

                Texture2D tileTexture = ((DownloadHandlerTexture)www.downloadHandler).texture;
                string fileName = $"{tilesFolderPath}/tile_{x}-{y}.jpg";
                File.WriteAllBytes(fileName, tileTexture.EncodeToJPG());
            }
        }

        // Merge the downloaded tiles into a single image
        yield return MergeTiles(tilesFolderPath, identifier);
    }

    private List<(int, int, string, string)> TilesInfo(int zoom)
    {
        string baseUrl = "https://tile.googleapis.com/v1/streetview/tiles";
        List<(int, int, string, string)> tiles = new List<(int, int, string, string)>();

        for (int x = 0; x < Math.Pow(2, zoom); x++)
        {
            for (int y = 0; y < Math.Pow(2, zoom - 1); y++)
            {
                // Construct URL for fetching tiles
                string url = $"{baseUrl}?pano={identifier}&x={x}&y={y}&zoom={zoom}&key={apiKey}";
                tiles.Add((x, y, GetFileName(identifier, x, y), url));
            }
        }

        // Return unique tile URLs
        return tiles.Distinct().ToList();
    }

    private string GetFileName(string identifier, int x, int y)
    {
        return $"StreetView_{identifier.Replace(',', '_')}_{x}x{y}.jpg";
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
            string[] segments = uri.AbsolutePath.Split('/');

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

            string query = uri.Query;
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
        }
        catch (Exception e)
        {
            Debug.LogError("Error parsing URL:" + e.Message);
        }
        return null;
    }

    private IEnumerator MergeTiles(string folderPath, string identifier)
    {
        int tileSize = 512;
        int numTilesX = 1 << zoomLevel;
        int numTilesY = numTilesX / 2;
        Texture2D stitchedTexture = new Texture2D(tileSize * numTilesX, tileSize * numTilesY);

        for (int x = 0; x < numTilesX; x++)
        {
            for (int y = 0; y < numTilesY; y++)
            {
                string fileName = $"{folderPath}/tile_{x}-{y}.jpg";
                byte[] tileBytes = File.ReadAllBytes(fileName);
                Texture2D tileTexture = new Texture2D(2, 2); // Dummy texture size, will be replaced by actual tile texture
                tileTexture.LoadImage(tileBytes);

                for (int i = 0; i < tileSize; i++)
                {
                    for (int j = 0; j < tileSize; j++)
                    {
                        Color color = tileTexture.GetPixel(i, j);
                        stitchedTexture.SetPixel(x * tileSize + i, y * tileSize + j, color);
                    }
                }
            }
        }

        stitchedTexture.Apply();

        // Save the stitched texture as a PNG file in the 360 Images folder
        string stitchedFilePath = $"{folderPath}/StreetView_{identifier.Replace(',', '_')}_stitched.png";
        File.WriteAllBytes(stitchedFilePath, stitchedTexture.EncodeToPNG());

        Debug.Log("Stitched image saved: " + stitchedFilePath);

        yield return null;
    }


    /*using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Net;
using System.Threading.Tasks;

class Program
{
    static async Task Main(string[] args)
    {
        string apiKey = "AIzaSyC7FDiMc8jqeKgQAbY4l4oXcU97OEHmrhE"; // Replace "YOUR_API_KEY" with your actual API key
        string panoId = "XjmnNHRTP9hZwpFh56vSeQ";
        int zoomLevel = 0;

        string outputDir = "tiles";
        if (!Directory.Exists(outputDir))
        {
            Directory.CreateDirectory(outputDir);
        }

        await DownloadTiles(apiKey, panoId, zoomLevel, outputDir);

        await MergeTiles(outputDir);

        Console.WriteLine("360-degree image saved as '360_streetview.jpg'");

        // Clean up downloaded tiles
        Directory.Delete(outputDir, true);
    }

    static async Task DownloadTiles(string apiKey, string panoId, int zoomLevel, string outputDir)
    {
        string baseUrl = $"https://maps.googleapis.com/maps/api/streetview?size=1080x1080&pano={panoId}&zoom={zoomLevel}&key={apiKey}";
        using (var httpClient = new HttpClient())
        {
            int progress = 0;
            for (int x = 0; x < 12; x++)
            {
                for (int y = 0; y < 6; y++)
                {
                    string url = baseUrl + $"&heading={x * 360 / 12}&pitch={y * 360 / 6}";
                    string fileName = Path.Combine(outputDir, $"tile_{x}-{y}.jpg");

                    using (var response = await httpClient.GetAsync(url))
                    {
                        if (response.IsSuccessStatusCode)
                        {
                            progress++;
                            Console.WriteLine($"Progress: {progress}/288");
                            using (var fileStream = new FileStream(fileName, FileMode.Create))
                            {
                                // Read the response content as a stream
                                using (var imageStream = await response.Content.ReadAsStreamAsync())
                                {
                                    // Load the image from the stream
                                    using (var image = Image.FromStream(imageStream))
                                    {
                                        // Calculate the size of the cut image (0.6 * width and height)
                                        int cutWidth = (int)(image.Width * 0.6);
                                        int cutHeight = (int)(image.Height * 0.3);

                                        // Create a bitmap for the cut image
                                        using (var cutImage = new Bitmap(cutWidth, cutHeight))
                                        {
                                            // Create a graphics object from the cut image
                                            using (var graphics = Graphics.FromImage(cutImage))
                                            {
                                                // Define the portion of the original image to draw (centered)
                                                int sourceX = (image.Width - cutWidth) / 2;
                                                int sourceY = (image.Height - cutHeight) / 2;
                                                var sourceRect = new Rectangle(sourceX, sourceY, cutWidth, cutHeight);

                                                // Draw the specified portion of the original image onto the cut image
                                                graphics.DrawImage(image, 0, 0, sourceRect, GraphicsUnit.Pixel);
                                            }

                                            // Save the cut image to file
                                            cutImage.Save(fileStream, System.Drawing.Imaging.ImageFormat.Jpeg);
                                        }
                                    }
                                }
                            }
                        }
                        else
                        {
                            Console.WriteLine("Failed");
                        }
                    }
                }
            }
        }
    }

    static async Task MergeTiles(string outputDir)
    {
        int tileWidth, tileHeight;
        using (var tileImage = Image.FromFile(Path.Combine(outputDir, "tile_0-0.jpg")))
        {
            tileWidth = tileImage.Width;
            tileHeight = tileImage.Height;
        }

        int stitchedWidth = tileWidth * 12 * 10/6;
        int stitchedHeight = tileHeight * 6 * 10/6;

        using (var stitchedImage = new Bitmap(stitchedWidth, stitchedHeight))
        using (var graphics = Graphics.FromImage(stitchedImage))
        {
            for (int x = 0; x < 12; x++)
            {
                for (int y = 0; y < 6; y++)
                {
                    string fileName = Path.Combine(outputDir, $"tile_{x}-{y}.jpg");
                    using (var tileImage = Image.FromFile(fileName))
                    {
                        graphics.DrawImage(tileImage, x * tileWidth, y * tileHeight);
                    }
                }
            }

            stitchedImage.Save("360_streetview.jpg");
        }
    }
}
*/
}