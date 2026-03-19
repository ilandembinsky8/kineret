
using System.Collections.Generic;
using UnityEngine;


public static class LocationsManager
{
    public static int DestinationsCount;
    public static int DestinationsSelectCount;
    public static Dictionary<string, Sprite> IconsMap { get; set; } = new(6);
    public static Dictionary<int, DestinationData> Destinations { get; set; } = new(16);
    public static Dictionary<int, InterestPointData> InterestPoints { get; set; } = new(8);

    public static int[] SelectedDestinations = new int[DestinationsCount];

    public static DestinationCollectableData[] DestinationCollectables { get; set; }
    public static InfoCollectableData InfoCollectable { get; set; }
    public static InfoCollectableData InterestCollectable { get; set; }
    public static BonusCollectableData[] BonusCollectables { get; set; }
    public static ChallengeCollectableData[] Challenges{ get; set; }

    public static void AddDestination(int destination, DestinationData destinationData)
    {
        Destinations.Add(destination, destinationData);
    }
    public static void AddDestination(int destination, DestinationSO destinationData)
    {
        DestinationData data = new DestinationData
        {
            Data = destinationData.DestinationData,
            Background = destinationData.Background,
            Icon = destinationData.Icon,
            Logo = destinationData.Logo,
            LogoScaleModifier = destinationData.LogoScaleModifier
        };
        AddDestination(destination, data);
    }

    public static DestinationData GetDestination(int destination)
    {
        return Destinations[destination];
    }
    public static void AddInterestPoint(int interstPoint, InterestPointData data)
    {
        InterestPoints.Add(interstPoint, data);
    }
    public static void AddInterestPoint(int interstPoint, InterestPointSO interestPointSO)
    {
        InterestPointData data = new InterestPointData
        {
            Data = interestPointSO.InterestPointData,
            Icon = interestPointSO.Icon
        };
        AddInterestPoint(interstPoint, data);
    }

    public static InterestPointData GetInterestPoint(int interstPoint)
    {
        return InterestPoints[interstPoint];
    }

    public static InfoScreenData GetInfoScreenData(int destinationID, bool isFinal)
    {
        DestinationData destination = LocationsManager.GetDestination(destinationID);
        InfoScreenData data = new()
        {
            Title = destination.Data.UIDestinationInfoText.HebTitle,
            Subtitle = destination.Data.HebSubTitleInfoText,
            Text = destination.Data.DestinationInfoScreenText.HebDescription,
            Background = destination.Background,
            Logo = destination.Logo,
            Icon = destination.Icon,
            logoSizeMultiplier = destination.LogoScaleModifier,
            isFinal = isFinal
        };

        return data;
    }

    public static bool TryGetIconImageData(string iconName, out Sprite icon)
    {
        if (IconsMap != null &&
            IconsMap.TryGetValue(iconName, out icon)) { return true; }// Found image data for icon
        icon = default;
        return false; // No image found for icon
    }
}
