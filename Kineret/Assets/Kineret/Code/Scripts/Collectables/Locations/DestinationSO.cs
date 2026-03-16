
using UnityEngine;

[CreateAssetMenu(fileName = "DestinationSO", menuName = "Assets/Scriptable Objects/Locations/Destination")]
public class DestinationSO : ScriptableObject
{
    public DestinationTextData DestinationData;
    public Sprite Background;
    public Sprite Logo;
    public Sprite Icon;
    public float LogoScaleModifier;
}

public struct DestinationData
{
    public DestinationTextData Data;
    public Sprite Background;
    public Sprite Logo;
    public Sprite Icon;
    public float LogoScaleModifier;
}
