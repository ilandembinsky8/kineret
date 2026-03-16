
using UnityEngine;

[CreateAssetMenu(fileName = "InterestPointSO", menuName = "Assets/Scriptable Objects/Locations/InterestPoint")]
public class InterestPointSO : ScriptableObject
{
    public InterestPointTextData InterestPointData;
    public Sprite Icon;
}

public struct InterestPointData
{
    public InterestPointTextData Data;
    public Sprite Icon;
}


