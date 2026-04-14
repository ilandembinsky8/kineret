using System;
using UnityEngine;

[Serializable]
public struct InfoScreenData
{
    public string CodeName;
    public string Title;
    public string Subtitle;
    public string Text;
    public Sprite Background;
    public Sprite Logo;
    public Sprite Icon;
    public float logoSizeMultiplier;
    public bool isFinal;
}
