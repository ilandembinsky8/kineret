using System.Collections;
using System.Collections.Generic;
using UnityEngine;


[CreateAssetMenu(fileName = "PopUpData", menuName = "Assets/Scriptable Objects/PopUps/Data")]
public class PopUpData : ScriptableObject
{
    public Sprite IconSprite;
    public string TitleText;
    public string DescriptionText;
    public float Duration;
}
