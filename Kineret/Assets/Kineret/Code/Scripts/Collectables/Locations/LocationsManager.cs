using System;
using System.Collections.Generic;

public enum Destination
{
    AgmonHula,
    Shamir,
    Gilboa,
    Agre,
    Golan,
    BioCastle,
    Salmon,
    Eshkol,
    Ginosar,
    Tzemah,
    Afimilk
}
public enum InterstPoint
{
    Firewave,
    Seymour,
    Shvitz,
    Tzipori
}

public static class LocationsManager
{
    public const int SELECTABLE_DESTINATIONS_COUNT = 3;
    public static int DestinationsCount = Enum.GetValues(typeof(Destination)).Length;
    public static int InteresPointsCount = Enum.GetValues(typeof(InterstPoint)).Length;

    private static Dictionary<Destination, DestinationSO> Destinations = new Dictionary<Destination, DestinationSO>(DestinationsCount);
    private static Dictionary<InterstPoint, InterestPointSO> InteresPoints = new Dictionary<InterstPoint, InterestPointSO>(InteresPointsCount);

    public static Destination[] SelectedDestinations = new Destination[SELECTABLE_DESTINATIONS_COUNT];

    public static void AddDestination(Destination destination, DestinationSO destinationSO)
    {
        Destinations.Add(destination, destinationSO);
    }

    public static DestinationSO GetDestination(Destination destination)
    {
        return Destinations[destination];
    }

    public static void AddInterestPoint(InterstPoint interstPoint, InterestPointSO interestPointSO)
    {
        InteresPoints.Add(interstPoint, interestPointSO);
    }

    public static InterestPointSO GetInterestPoint(InterstPoint interstPoint)
    {
        return InteresPoints[interstPoint];
    }
}
