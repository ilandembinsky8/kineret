using UnityEngine;

/// <summary>
/// Keeps whatever this sits on drawing in front of the map shape outlines.
///
/// The outlines are deliberately drawn with the depth test switched off so the terrain cannot hide
/// them - a shape hugging the ground would otherwise be cut to pieces by every ridge between it and
/// the camera. That also puts them in front of the aircraft, which is wrong the moment the player
/// switches to the third person view and the model appears in the middle of the screen.
///
/// Depth cannot separate the two cases, because "ignore the terrain but not the aircraft" is a
/// distinction between objects rather than between distances. Draw order can: the outlines go out
/// at <see cref="MapShapeRenderer.AlwaysOnTopQueue"/>, and this pushes the aircraft past them. The
/// aircraft still depth-tests against the terrain normally, so nothing else about it changes.
///
/// Put it on the aircraft's visual root; it covers every renderer underneath.
/// </summary>
[DisallowMultipleComponent]
public class DrawOverMapShapes : MonoBehaviour
{
    [Tooltip("Render queue for this object's materials. Must be above the outlines' queue (3100) and below the overlay range.")]
    [SerializeField] private int renderQueue = MapShapeRenderer.AlwaysOnTopQueue + 50;

    private void Awake()
    {
        Apply();
    }

    private void Apply()
    {
        Renderer[] renderers = GetComponentsInChildren<Renderer>(true);

        foreach (Renderer renderer in renderers)
        {
            if (renderer == null)
                continue;

            //renderer.materials hands back instances, so the shared assets in the glider package
            //are left alone and only this aircraft is reordered.
            Material[] materials = renderer.materials;

            foreach (Material material in materials)
            {
                if (material != null)
                    material.renderQueue = renderQueue;
            }

            renderer.materials = materials;
        }
    }
}
