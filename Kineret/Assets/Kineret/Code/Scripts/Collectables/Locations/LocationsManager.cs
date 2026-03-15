using System;
using System.Collections.Generic;
using UnityEditor.PackageManager;

public static class LocationsManager
{
    public const int SELECTABLE_DESTINATIONS_COUNT = 3;

    public static int DestinationsCount = 11;
    public static int InterestPointsCount = 4;
    public static int BonusesCount = 4;

    private static Dictionary<int, DestinationSO> Destinations = new Dictionary<int, DestinationSO>(DestinationsCount);
    private static Dictionary<int, InterestPointSO> InteresPoints = new Dictionary<int, InterestPointSO>(InterestPointsCount);

    public static int[] SelectedDestinations = new int[SELECTABLE_DESTINATIONS_COUNT];

    public static void AddDestination(int destination, DestinationSO destinationSO)
    {
        Destinations.Add(destination, destinationSO);
    }

    public static DestinationSO GetDestination(int destination)
    {
        return Destinations[destination];
    }

    public static void AddInterestPoint(int interstPoint, InterestPointSO interestPointSO)
    {
        InteresPoints.Add(interstPoint, interestPointSO);
    }

    public static InterestPointSO GetInterestPoint(int interstPoint)
    {
        return InteresPoints[interstPoint];
    }

    public static InfoScreenData GetInfoScreenData(int destinationID, bool isFinal)
    {
        DestinationSO destination = LocationsManager.GetDestination(destinationID);
        InfoScreenData data = new()
        {
            Title = destination.DestinationData.Name,
            Subtitle = destination.DestinationData.InfoScreenSubtitle,
            Text = destination.DestinationData.InfoScreenText,
            Background = destination.Background,
            Logo = destination.Logo,
            Icon = destination.Icon,
            logoSizeMultiplier = destination.LogoScaleModifier,
            isFinal = isFinal
        };

        return data;
    }
}
