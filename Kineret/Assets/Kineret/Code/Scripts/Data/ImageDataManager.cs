using System.Collections.Generic;
using UnityEngine.Networking;
using System.Collections;
using UnityEngine;
using System.IO;
using System;

public class ImageDataManager : MonoBehaviour
{
    private string _imagePathFolder = "Destinations";
    private string _iconPathFolder = "Icons";

    internal void LoadImages(List<DestinationTextData> destinationDataList, Action<Dictionary<string, DestinationImageData>> onImagesFinLoading)
    {
        StartCoroutine(CollectDestinationImageDataCoroutine(destinationDataList, onImagesFinLoading));
    }
    internal void LoadIcons(IconData iconsList, Action<Dictionary<string, Sprite>> onIconsFinLoading)
    {
        StartCoroutine(CollectIconsCoroutine(iconsList, onIconsFinLoading));
    }

    private IEnumerator CollectDestinationImageDataCoroutine(List<DestinationTextData> destinationDataList, Action<Dictionary<string, DestinationImageData>> onImagesFinLoading)
    {
        Dictionary<string, DestinationImageData> destinationImageDataDictionary = new Dictionary<string, DestinationImageData>();

        foreach (DestinationTextData destination in destinationDataList)
        {
            string destinationName = destination.CodeName;

            DestinationImageData imageData = new DestinationImageData { DestinationName = destinationName };

            yield return StartCoroutine(LoadSpriteData($"{destinationName}-background.png", _imagePathFolder, sprite => { imageData.backgroundImage = sprite; }));

            //yield return StartCoroutine(LoadSpriteData($"{destinationName}-icon.png", _imagePathFolder, sprite => { imageData.IconImage = sprite; }));

            yield return StartCoroutine(LoadSpriteData($"{destinationName}-logo.png", _imagePathFolder, sprite => { imageData.LogoImage = sprite; }));

            destinationImageDataDictionary[destinationName] = imageData;
        }

        onImagesFinLoading?.Invoke(destinationImageDataDictionary);
    }
    private IEnumerator CollectIconsCoroutine(IconData iconsList, Action<Dictionary<string, Sprite>> onIconsFinLoading)
    {
        Dictionary<string, Sprite> iconImageDataDictionary = new Dictionary<string, Sprite>(iconsList.IconFileNames.Count);

        foreach (string iconImageName in iconsList.IconFileNames)
        {
            Sprite tempSpriteVar = null;

            yield return StartCoroutine(LoadSpriteData($"{iconImageName}.png", _iconPathFolder, sprite => { tempSpriteVar = sprite; }));

            if (tempSpriteVar == null)
            {
                Debug.LogError($"ImageDataManager: icon is null for: {iconImageName}");
                continue;
            }

            iconImageDataDictionary[iconImageName] = tempSpriteVar;
        }

        onIconsFinLoading?.Invoke(iconImageDataDictionary);
    }

    /// <summary>
    /// Loads a SINGLE sprite from file in StreamingAssets
    /// </summary>
    private IEnumerator LoadSpriteData(string fileName, string fileFolderName, Action<Sprite> onLoaded)
    {
        string fullPath = Path.Combine(Application.streamingAssetsPath, fileFolderName, fileName);

        UnityWebRequest request = UnityWebRequestTexture.GetTexture(fullPath);
        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError($"ImageDataManager: Failed loading image: {fileName}. Error: {request.error}");
            onLoaded?.Invoke(null);
            yield break;
        }

        Texture2D texture = DownloadHandlerTexture.GetContent(request);

        Sprite sprite = Sprite.Create(
            texture,
            new Rect(0, 0, texture.width, texture.height),
            new Vector2(0.5f, 0.5f)
        );

        onLoaded?.Invoke(sprite);
    }

}

public struct IconData { public List<string> IconFileNames; }

public struct DestinationImageData
{
    public string DestinationName;
    public Sprite backgroundImage;
    public Sprite IconImage;
    public Sprite LogoImage;
}