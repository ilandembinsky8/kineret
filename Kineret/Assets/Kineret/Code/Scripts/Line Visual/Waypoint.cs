using UnityEngine;

public class Waypoint : MonoBehaviour
{
    public void Initialize(float scale)
    {
        transform.localScale = new Vector3(scale, scale, scale);
    }
}
