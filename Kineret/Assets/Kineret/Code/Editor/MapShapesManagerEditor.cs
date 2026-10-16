using UnityEditor;
using UnityEngine;

/// <summary>
/// Show/Hide buttons for each shape. Deliberately no keyboard shortcuts.
/// </summary>
[CustomEditor(typeof(MapShapesManager))]
public class MapShapesManagerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        EditorGUILayout.Space();

        var manager = (MapShapesManager)target;

        if (!Application.isPlaying)
        {
            EditorGUILayout.HelpBox(
                "Enter Play Mode to draw the shapes. They follow the terrain surface, and the " +
                "streamed terrain only exists at runtime.",
                MessageType.Info);
            return;
        }

        if (manager.Shapes == null || manager.Shapes.Count == 0)
        {
            EditorGUILayout.HelpBox("No shapes loaded. Check MapShapes.json in StreamingAssets.", MessageType.Warning);
            return;
        }

        EditorGUILayout.LabelField("All shapes", EditorStyles.boldLabel);
        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Show All")) { manager.ShowAll(); }
            if (GUILayout.Button("Hide All")) { manager.HideAll(); }
            if (GUILayout.Button("Toggle")) { manager.ToggleAll(); }
        }

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Individual shapes", EditorStyles.boldLabel);

        foreach (MapShapeRenderer shape in manager.Shapes)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField(
                    (shape.IsVisible ? "●  " : "○  ") + shape.Id,
                    GUILayout.MinWidth(120f));

                if (GUILayout.Button("Show", GUILayout.Width(60f))) { manager.Show(shape.Id); }
                if (GUILayout.Button("Hide", GUILayout.Width(60f))) { manager.Hide(shape.Id); }
            }
        }

        //Keeps the filled/hollow visibility dots live while an animation plays.
        Repaint();
    }
}
