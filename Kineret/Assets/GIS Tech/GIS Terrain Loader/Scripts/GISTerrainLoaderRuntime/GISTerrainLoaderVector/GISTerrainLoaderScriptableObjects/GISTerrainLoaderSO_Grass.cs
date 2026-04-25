/*     Unity GIS Tech 2020-2023      */

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GISTech.GISTerrainLoader
{
    public class GISTerrainLoaderSO_Grass : ScriptableObject
    {
        public GrassType grassType = GrassType.GrassTexture;
        [Space()]
        [Header("Detail Grass (3D Model)")]
        [Space()]

        public GameObject GrassModel;
        [Range(0f, 100f)]
        public float Model_AligneToTheGround = 0;
        public float Model_MinWidth = 1;
        public float Model_MaxWidth = 2;
        public float Model_MinHeight = 1;
        public float Model_MaxHeight = 2;

        public int Model_NoiseSeed = 341500000;
        public float Model_NoiseSpread = 0.1f;
        [Range(0f, 5)]
        public float Model_DetailDensity = 1;
        public bool Model_EffectedByDensityScale = true;
        public bool Model_UseGPUInstancing = false;

        [Space()]
        [Header("Grass Texture Details")]
        [Space()]

        public Texture2D DetailTexture;
        public float Texture_AligneToTheGround = 0;
        public float Texture_MinWidth = 1;
        public float Texture_MaxWidth = 2;

        public float Texture_MinHeight = 1;
        public float Texture_MaxHeight = 2;

        public int Texture_NoiseSeed = 532521046;
        public float Texture_NoiseSpread = 0.1f;
        [Range(0f, 5)]
        public float Texture_DetailDensity = 1;

        public Color32 Texture_HealthyColor = Color.green;

        public Color32 Texture_DryColor = Color.gray;
        public bool Texture_BillBoard = true;
        public bool Texture_EffectedByDensityScale = true;

    }

    public enum GrassType
    {
        DetailMesh = 0,
        GrassTexture
    }
}