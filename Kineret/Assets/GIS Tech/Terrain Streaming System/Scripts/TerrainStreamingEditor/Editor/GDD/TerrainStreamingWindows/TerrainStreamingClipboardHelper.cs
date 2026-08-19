/*     Unity GIS Tech 2020-2021      */

using UnityEngine;
using System;
using System.Reflection;

namespace GISTech.TerrainStreaming
{
    public class TerrainStreamingClipboardHelper 
    {
        public static string Clipboard
        {
            get { return GUIUtility.systemCopyBuffer; }
            set { GUIUtility.systemCopyBuffer = value; }
        }
    }
}
