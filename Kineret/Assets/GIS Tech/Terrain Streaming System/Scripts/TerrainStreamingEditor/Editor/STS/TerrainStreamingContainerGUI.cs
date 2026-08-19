/*     Unity GIS Tech 2020-2021      */

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

namespace GISTech.TerrainStreaming
{
    [CustomEditor(typeof(TerrainStreamingContainer))]
    public class TerrainStreamingContainerGUI : Editor
    {
        private TerrainStreamingContainer ContainerObjectInfo { get { return target as TerrainStreamingContainer; } }

        private TabsBlock tabs;

        private Texture2D m_resetPrefs;

        private void OnEnable()
        {
            tabs = new TabsBlock(new Dictionary<string, System.Action>()
            {
                {"Terrain Metadata", TerrainMetadata}
            });

            tabs.SetCurrentMethod(ContainerObjectInfo.lastTab);
        }
        private void TerrainMetadata()
        {
            using (new HorizontalBlock())
            {
                CoordinatesBarGUI();
            }
        }
        private void CoordinatesBarGUI()
        {
            using (new VerticalBlock(GUI.skin.box))
            {
                GUILayout.Label("Terrain Coordinates [Geographic Lat/Lon] ");

                using (new HorizontalBlock(GUI.skin.button))
                {
                    GUILayout.Label(" Upper-Left :    ");
                    GUILayout.Label("");
                    GUILayout.Label("  Latitude : ");
                    GUI.SetNextControlName("UpperLeftCoordianteLat");
                    GUILayout.Label(Math.Round(ContainerObjectInfo.UpperLeftCoordinate.y, 10).ToString());
                    GUI.SetNextControlName("UpperLeftCoordianteLon");

                    GUILayout.Label("  Longitude : ");
                    GUI.SetNextControlName("UpperLeftCoordianteLon");
                    GUILayout.Label(Math.Round(ContainerObjectInfo.UpperLeftCoordinate.x, 10).ToString());
                    GUI.SetNextControlName("UpperLeftCoordianteLon");

                }


                using (new HorizontalBlock(GUI.skin.button))
                {
                    GUILayout.Label(" Bottom-Right : ");
                    GUILayout.Label("", GUILayout.ExpandWidth(true));
                    GUILayout.Label("  Latitude : ");
                    GUI.SetNextControlName("UpperLeftCoordianteLat");
                    GUILayout.Label(Math.Round(ContainerObjectInfo.BottomRightCoordiante.y, 10).ToString());
                    GUI.SetNextControlName("UpperLeftCoordianteLon");

                    GUILayout.Label("  Longitude : ");
                    GUI.SetNextControlName("UpperLeftCoordianteLon");
                    GUILayout.Label(Math.Round(ContainerObjectInfo.BottomRightCoordiante.x, 10).ToString());
                    GUI.SetNextControlName("UpperLeftCoordianteLon");

                }

                GUILayout.Label("Terrain Dimension [Km] ");

                using (new HorizontalBlock(GUI.skin.button))
                {
                    GUILayout.Label("  Width :  ");
                    GUILayout.Label(Math.Round(ContainerObjectInfo.Dimensions.x, 2).ToString());

                    GUILayout.Label("  Lenght : ");
                    GUILayout.Label(Math.Round(ContainerObjectInfo.Dimensions.y, 2).ToString());

                }
                GUILayout.Label("Min Max Elevation [m] ");

                using (new HorizontalBlock(GUI.skin.button))
                {
                    GUILayout.Label("  Min :  ");
                    GUILayout.Label(Math.Round(ContainerObjectInfo.MinMaxElevation.x, 2).ToString());

                    GUILayout.Label("  Max :  ");
                    GUILayout.Label(Math.Round(ContainerObjectInfo.MinMaxElevation.y, 2).ToString());

                }

                GUILayout.Label("Terrain Scale Factor ");

                using (new HorizontalBlock(GUI.skin.button))
                {
                    GUILayout.Label("  X :  ");
                    GUILayout.Label(ContainerObjectInfo.Scale.x.ToString());

                    GUILayout.Label("  Y : ");
                    GUILayout.Label(ContainerObjectInfo.Scale.y.ToString());

                    GUILayout.Label("  Z : ");
                    GUILayout.Label(ContainerObjectInfo.Scale.z.ToString());

                }

                GUILayout.Label("Terrain Total Size [Terrain Unite] ");

                using (new HorizontalBlock(GUI.skin.button))
                {
                    GUILayout.Label("  X :  ");
                    GUILayout.Label(ContainerObjectInfo.ContainerSize.x.ToString());

                    GUILayout.Label("  Y : ");
                    GUILayout.Label(ContainerObjectInfo.ContainerSize.y.ToString());

                    GUILayout.Label("  Z : ");
                    GUILayout.Label(ContainerObjectInfo.ContainerSize.z.ToString());

                }

                GUILayout.Label("Terrains Count ");

                using (new HorizontalBlock(GUI.skin.button))
                {
                    GUILayout.Label("  X :  ");
                    GUILayout.Label(ContainerObjectInfo.TilesCount.x.ToString());

                    GUILayout.Label("  Y : ");
                    GUILayout.Label(ContainerObjectInfo.TilesCount.y.ToString());

                    GUILayout.Label("      ");
                    GUILayout.Label(" ".ToString());

                }

                using (new HorizontalBlock(GUI.skin.button))
                {
                    GUILayout.Label(new GUIContent(" Draw Sectors ", " Use this option to enable/disable Sectors in Editor Mode "), GUILayout.MaxWidth(200));
                    ContainerObjectInfo.EnableDrawSectors = (OptionEnabDisab)EditorGUILayout.EnumPopup("", ContainerObjectInfo.EnableDrawSectors);
                }
 
            }
        }
        public override void OnInspectorGUI()
        {
            //base.OnInspectorGUI();
            Undo.RecordObject(ContainerObjectInfo, "TSS_ContainerInfo");
            tabs.Draw();
            if (GUI.changed)
                ContainerObjectInfo.lastTab = tabs.curMethodIndex;
            EditorUtility.SetDirty(ContainerObjectInfo);
        }

    }
}