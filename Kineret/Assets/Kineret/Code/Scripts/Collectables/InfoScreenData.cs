using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "InfoScreenData", menuName = "Assets/Scriptable Objects/InfoScreen/Data")]
public class InfoScreenData : ScriptableObject
{
    public Sprite Background;
    public Sprite Logo;
    public Sprite Icon;
    public string Title;
    public string Subtitle;
    public string Description;
}
