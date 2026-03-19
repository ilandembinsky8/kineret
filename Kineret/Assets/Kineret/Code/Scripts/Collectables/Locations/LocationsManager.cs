
using System.Collections.Generic;
using UnityEngine;


public static class LocationsManager
{
    public const int SELECTABLE_DESTINATIONS_COUNT = 3;

    public static Dictionary<string, Sprite> IconsMap = new(6);
    public static readonly Dictionary<int, DestinationData> Destinations = new(16);
    public static readonly Dictionary<int, InterestPointData> InterestPoints = new(8);

    public static int[] SelectedDestinations = new int[SELECTABLE_DESTINATIONS_COUNT];

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
}
