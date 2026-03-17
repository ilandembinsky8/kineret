using System.Collections.Generic;
using UnityEngine.Networking;
using System.Collections;
using UnityEngine;
using System.IO;
using System;

public class ImageDataManager : MonoBehaviour
{
    private Dictionary<string, DestinationImageData> _destinationImageDataDictionary;

    internal void LoadImages(List<DestinationTextData> destinationDataList, Action<Dictionary<string, DestinationImageData>> onImagesFinLoading)
    {
        StartCoroutine(LoadDestinationImageDataCoroutine(destinationDataList, onImagesFinLoading));
    }

    private IEnumerator LoadDestinationImageDataCoroutine(List<DestinationTextData> destinationDataList, Action<Dictionary<string, DestinationImageData>> onImagesFinLoading)
    {
        _destinationImageDataDictionary = new Dictionary<string, DestinationImageData>();

        foreach (DestinationTextData destination in destinationDataList)
        {
            string destinationName = destination.UIDestinationInfoText.EngTitle;

            DestinationImageData imageData = new DestinationImageData { DestinationName = destinationName };

            yield return StartCoroutine(LoadSpriteData($"{destinationName}-background.png", sprite => { imageData.backgroundImage = sprite; }));

            yield return StartCoroutine(LoadSpriteData($"{destinationName}-fluff.png", sprite => { imageData.FlufffImage = sprite; }));

            yield return StartCoroutine(LoadSpriteData($"{destinationName}-logo.png", sprite => { imageData.LogoImage = sprite; }));

            _destinationImageDataDictionary[destinationName] = imageData;
        }

        onImagesFinLoading?.Invoke(_destinationImageDataDictionary);
    }
    private IEnumerator LoadSpriteData(string fileName, Action<Sprite> onLoaded)
    {
        string fullPath = Path.Combine(Application.streamingAssetsPath, fileName);

        UnityWebRequest request = UnityWebRequestTexture.GetTexture(fullPath);
        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
        {
            Debug.LogWarning($"ImageDataManager: Failed loading image: {fileName}. Error: {request.error}");
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

public struct DestinationImageData
{
    public string DestinationName;
    public Sprite backgroundImage;
    public Sprite FlufffImage;
    public Sprite LogoImage;
}