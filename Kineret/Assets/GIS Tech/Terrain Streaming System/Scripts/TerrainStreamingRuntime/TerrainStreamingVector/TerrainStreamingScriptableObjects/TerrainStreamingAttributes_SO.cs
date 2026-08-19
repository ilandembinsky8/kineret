/*     Unity GIS Tech 2020-2021      */

using System;
using System.Collections.Generic;
using UnityEngine;

namespace GISTech.TerrainStreaming
{
    public class TerrainStreamingAttributes_SO : ScriptableObject
    {
        public List<TerrainStreamingVectorTag> Content = new List<TerrainStreamingVectorTag>();
        public bool Contains (TerrainStreamingVectorTag tag)
        {
            bool contains = false;

            foreach(var element in Content)
            {
                if (element.Attribute == tag.Attribute && element.Value == tag.Value)
                {
                    contains = true;
                    break;
                }
        
            }

            return contains;
        }
        public bool ContainsAndTagEnabled(TerrainStreamingVectorTag tag)
        {
            bool contains = false;

            foreach (var element in Content)
            {
                if (element.Attribute == tag.Attribute && element.Value == tag.Value)
                {
                    if(element.EnableTag) contains = true;
                    break;
                }
            }
            return contains;
        }
    }

    [Serializable]
    public class TerrainStreamingVectorTag
    {
        public bool EnableTag = true;
        public string Attribute = "";
        public string Value = "";

        public TerrainStreamingVectorTag(string m_Attribute, string m_Value)
        {
            Attribute = m_Attribute;
            Value = m_Value;
        }

        public bool Equal(TerrainStreamingVectorTag m_tag)
        {
            bool equal = false;

            if (m_tag.Attribute == Attribute && m_tag.Value == Value)
                return true;

            return equal;
        }
    }
}
