
using UnityEngine;

[CreateAssetMenu(fileName = "DestinationSO", menuName = "Assets/Scriptable Objects/Locations/Destination")]
public class DestinationSO : ScriptableObject
{
    public DestinationData DestinationData;
    public Sprite Background;
    public Sprite Logo;
    public Sprite Icon;
    public float LogoScaleModifier;
}
