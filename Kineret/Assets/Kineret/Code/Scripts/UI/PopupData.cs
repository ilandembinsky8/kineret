
using System;
using UnityEngine;

[Serializable]
public struct PopupData
{
    public PopupTextData PopupTextData;
    public Sprite IconSprite;
}
public enum PopUpType
{
    Info, TitleOnly, Full, HighFull
}