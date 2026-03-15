
using UnityEngine;

[CreateAssetMenu(fileName = "CollectableData", menuName = "Assets/Scriptable Objects/Collectables/Data")]
public class CollectableData : ScriptableObject
{
    public float notificationRange;
    public float collectionRange;
    public int score;
}
