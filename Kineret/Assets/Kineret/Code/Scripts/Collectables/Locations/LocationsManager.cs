
using System.Collections.Generic;


public static class LocationsManager
{
    public const int SELECTABLE_DESTINATIONS_COUNT = 3;

    public static int DestinationsCount = 11;
    public static int InterestPointsCount = 4;
    public static int BonusesCount = 4;

    private static readonly Dictionary<int, DestinationData> Destinations = new(DestinationsCount);
    private static readonly Dictionary<int, InterestPointData> InteresPoints = new(InterestPointsCount);

    public static int[] SelectedDestinations = new int[SELECTABLE_DESTINATIONS_COUNT];

    public static DestinationCollectableData[] DestinationCollectables;
    public static InfoCollectableData InfoCollectable;
    public static InfoCollectableData InterestCollectable;
    public static BonusCollectableData[] BonusCollectables;

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
        InteresPoints.Add(interstPoint, data);
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
        return InteresPoints[interstPoint];
    }

    public static InfoScreenData GetInfoScreenData(int destinationID, bool isFinal)
    {
        DestinationData destination = LocationsManager.GetDestination(destinationID);
        InfoScreenData data = new()
        {
            Title = destination.Data.Name,
            Subtitle = destination.Data.InfoScreenSubtitle,
            Text = destination.Data.InfoScreenText,
            Background = destination.Background,
            Logo = destination.Logo,
            Icon = destination.Icon,
            logoSizeMultiplier = destination.LogoScaleModifier,
            isFinal = isFinal
        };

        return data;
    }
}
