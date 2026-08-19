/*     Unity GIS Tech 2020-2021      */

using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace GISTech.TerrainStreaming
{
    [CustomEditor(typeof(TerrainStreamingPlayer))]

    public class TerrainStreamingSystemPlayerGUI : Editor
    {
        public TerrainStreamingPlayer playerPrefs { get { return target as TerrainStreamingPlayer; } }

        private TabsBlock tabs;

        private void OnEnable()
        {
            tabs = new TabsBlock(new Dictionary<string, System.Action>()
            {
                {"Player", PlayerTab},
                {"Tile Intersection Mode ", TileIntersectionMode},
            });
            tabs.SetCurrentMethod(playerPrefs.lastTab);
 
        }
        public override void OnInspectorGUI()
        {
            Undo.RecordObject(playerPrefs, "TSS_Runtime_Player");
            tabs.Draw();
            if (GUI.changed)
                playerPrefs.lastTab = tabs.curMethodIndex;
             EditorUtility.SetDirty(playerPrefs);
        }
        private void PlayerTab()
        {
            using (new VerticalBlock(GUI.skin.box))
            {
                using (new VerticalBlock(GUI.skin.box))
                {
                    using (new HorizontalBlock(GUI.skin.box))
                    {
                        GUILayout.Label(new GUIContent("  Player Body", " Set the camera attached to the player "), GUILayout.MaxWidth(200));
                        playerPrefs.PlayerBody = (Transform)EditorGUILayout.ObjectField(playerPrefs.PlayerBody, typeof(UnityEngine.Transform),true, GUILayout.ExpandWidth(true));
                    }
                    using (new HorizontalBlock(GUI.skin.box))
                    {
                        GUILayout.Label(new GUIContent("  RayCast Transform", " Set the camera attached to the player "), GUILayout.MaxWidth(200));
                        playerPrefs.RayCastTransform = (Transform)EditorGUILayout.ObjectField(playerPrefs.RayCastTransform, typeof(UnityEngine.Transform), true, GUILayout.ExpandWidth(true));
                    }
                }

            }
        }
        private void TileIntersectionMode()
        {
            using (new VerticalBlock(GUI.skin.box))
            {
                using (new VerticalBlock(GUI.skin.box))
                {
                    using (new HorizontalBlock(GUI.skin.box))
                    {
                        GUILayout.Label(new GUIContent(" Intersection Mode ", " Select the method which will used to load tiles aound the player"), GUILayout.MaxWidth(200));
                        playerPrefs.intersectionMode = (IntersectionMode)EditorGUILayout.EnumPopup("", playerPrefs.intersectionMode, GUILayout.ExpandWidth(true));
                    }
                    if (playerPrefs.intersectionMode == IntersectionMode.FieldOfView)
                    {
                        using (new HorizontalBlock(GUI.skin.box))
                        {
                            GUILayout.Label(new GUIContent("  Player Camera ", " Set the camera attached to the player "), GUILayout.MaxWidth(200));
                            playerPrefs.playerCam = (Camera)EditorGUILayout.ObjectField(playerPrefs.playerCam, typeof(UnityEngine.Camera), true);

                        }

                        using (new HorizontalBlock(GUI.skin.box))
                        {
                            GUILayout.Label(new GUIContent("  Clipping Far Distance ", " The furthest distance from the camera that drawing occures"), GUILayout.MaxWidth(200));
                            playerPrefs.ClippingFarDistance = EditorGUILayout.Slider(playerPrefs.ClippingFarDistance, 10, 2000, GUILayout.ExpandWidth(true));
                            playerPrefs.OnClippingFarDistanceChanged(playerPrefs.ClippingFarDistance);
                        }

                        using (new HorizontalBlock(GUI.skin.box))
                        {
                            GUILayout.Label(new GUIContent("  Environment Distance % ", " This value represent the distance at which the Environment will be generated according to the clipping far distance "), GUILayout.MaxWidth(200));
                            playerPrefs.EnvironmentFOVDistancePercent = EditorGUILayout.Slider(playerPrefs.EnvironmentFOVDistancePercent, 1, 100, GUILayout.ExpandWidth(true));
                        }
                    }


                    if (playerPrefs.intersectionMode == IntersectionMode.Area)
                    {
                        using (new HorizontalBlock(GUI.skin.box))
                        {
                            GUILayout.Label(new GUIContent("  Area Size [Km] ", " Bounds of Intersection Tiles [Km] "), GUILayout.MaxWidth(200));
                            playerPrefs.TerrainLoadSize = EditorGUILayout.Vector2Field("",playerPrefs.TerrainLoadSize, GUILayout.ExpandWidth(true));
                        }

                        using (new HorizontalBlock(GUI.skin.box))
                        {
                            GUILayout.Label(new GUIContent("  Environment Size [Km] ", " Bounds of Intersection Environment Area [Km] "), GUILayout.MaxWidth(200));
                            playerPrefs.EnvironmentLoadSize = EditorGUILayout.Vector2Field("",playerPrefs.EnvironmentLoadSize, GUILayout.ExpandWidth(true));
                        }
                     }
                    if (playerPrefs.intersectionMode == IntersectionMode.InCircular)
                    {
                        using (new HorizontalBlock(GUI.skin.box))
                        {
                            GUILayout.Label(new GUIContent("  Raduis [Km]", "Cirlce Raduis [Km] to calculat Tiles Intersection "), GUILayout.MaxWidth(200));
                            playerPrefs.InCircularRadius = EditorGUILayout.Slider(playerPrefs.InCircularRadius, 1, 50, GUILayout.ExpandWidth(true));
                        }
                         using (new HorizontalBlock(GUI.skin.box))
                        {
                            GUILayout.Label(new GUIContent("  Environment Size [Km] ", "Environment Cirlce Raduis [Km] to calculat Tiles Intersection "), GUILayout.MaxWidth(200));
                            playerPrefs.EnvironmentInCircularRadius = EditorGUILayout.Slider(playerPrefs.EnvironmentInCircularRadius, 1, 50, GUILayout.ExpandWidth(true));
                        }
                    }
                }

            }



        }
    }

}