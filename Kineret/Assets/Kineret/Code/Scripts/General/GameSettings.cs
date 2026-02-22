
using UnityEngine;


[CreateAssetMenu(fileName = "GameSettings", menuName = "Assets/Scriptable Objects/GameSettings")]
public class GameSettings : ScriptableObject
{
    [field: SerializeField] public int SelectionDestinationCount { get; private set; }
    [field: SerializeField] public int GameDestinationCount { get; private set; }
}
