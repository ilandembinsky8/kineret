using System.Collections;
using System.Collections.Generic;
using UnityEngine;


[CreateAssetMenu(fileName = "PopUpData", menuName = "Assets/Scriptable Objects/PopUps/Data")]
public class PopupData : ScriptableObject
{
    public PopUpType Type;
    public Sprite IconSprite;
    public string Title;
    public string Description;
    public float Duration;
}
public enum PopUpType
{
    Info, TitleOnly, Full, HighFull
}