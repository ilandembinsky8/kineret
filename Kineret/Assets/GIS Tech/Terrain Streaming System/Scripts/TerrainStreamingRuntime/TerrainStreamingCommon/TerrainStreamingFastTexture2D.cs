/*     Unity GIS Tech 2020-2021      */

//CopyRights
// Brian Chasalow 2014      
// Revisions by Miha Krajnc

using UnityEngine;
using System.Collections.Generic;
using System;
using System.Runtime.InteropServices;

namespace GISTech.TerrainStreaming
{
    public class TerrainStreamingFastTexture2D : ScriptableObject
    {

        [AttributeUsage(AttributeTargets.Method)]
        public sealed class MonoPInvokeCallbackAttribute : Attribute
        {
            public MonoPInvokeCallbackAttribute(Type t)
            {
            }
        }

        [DllImport("__Internal")]
        private static extern void DeleteFastTexture2DAtTextureID(int id);

        [DllImport("__Internal")]
        private static extern void CreateFastTexture2DFromAssetPath(string assetPath, int uuid, bool resize, int resizeW, int resizeH);

        [DllImport("__Internal")]
        private static extern void RegisterFastTexture2DCallbacks(TextureLoadedCallback callback);

        public static void CreateFastTexture2D(string path, int uuid, bool resize, int resizeW, int resizeH)
        {
#if UNITY_EDITOR
#endif
        }

        public static void CleanupFastTexture2D(int texID)
        {
#if UNITY_EDITOR
#endif
        }


        private static int tex2DCount = 0;
        private static Dictionary<int, TerrainStreamingFastTexture2D> instances;

        public static Dictionary<int, TerrainStreamingFastTexture2D> Instances
        {
            get
            {
                if (instances == null)
                {
                    instances = new Dictionary<int, TerrainStreamingFastTexture2D>();
                }
                return instances;
            }
        }

        [SerializeField]
        public string url;
        [SerializeField]
        public int uuid;
        [SerializeField]
        public bool resize;
        [SerializeField]
        public int w;
        [SerializeField]
        public int h;
        [SerializeField]
        public int glTextureID;
        [SerializeField]
        private Texture2D nativeTexture;

        public Texture2D NativeTexture { get { return nativeTexture; } }

        [SerializeField]
        public bool isLoaded = false;

        public delegate void TextureLoadedCallback(int nativeTexID, int original_uuid, int w, int h);

        [MonoPInvokeCallback(typeof(TextureLoadedCallback))]
        public static void TextureLoaded(int nativeTexID, int original_uuid, int w, int h)
        {
            if (Instances.ContainsKey(original_uuid) && nativeTexID > -1)
            {
                TerrainStreamingFastTexture2D tex = Instances[original_uuid];
                tex.glTextureID = nativeTexID;
                tex.nativeTexture = Texture2D.CreateExternalTexture(w, h, TextureFormat.ARGB32, false, true, (System.IntPtr)nativeTexID);
                tex.nativeTexture.UpdateExternalTexture((System.IntPtr)nativeTexID);
                tex.isLoaded = true;
                tex.OnFastTexture2DLoaded(tex);
            }
        }

        private Action<TerrainStreamingFastTexture2D> OnFastTexture2DLoaded;

        protected void InitFastTexture2D(string _url, int _uuid, bool _resize, int _w, int _h, Action<TerrainStreamingFastTexture2D> callback)
        {
            this.url = _url;
            this.uuid = _uuid;
            this.resize = _resize;
            this.w = _w;
            this.h = _h;
            this.glTextureID = -1;
            this.OnFastTexture2DLoaded = callback;
            this.isLoaded = false;
        }

        private static bool registeredCallbacks = false;

        private static void RegisterTheCallbacks()
        {
            if (!registeredCallbacks)
            {
                registeredCallbacks = true;
#if UNITY_IOS
                if (Application.platform == RuntimePlatform.IPhonePlayer)
                    RegisterFastTexture2DCallbacks (TextureLoaded);
#endif

            }
        }

        public static TerrainStreamingFastTexture2D CreateFastTexture2D(string url, bool resize, int assetW, int assetH, Action<TerrainStreamingFastTexture2D> callback)
        {

            if (tex2DCount == 9999)
            {
                // Do nothing - to eliminate the editor warning
            }
#pragma warning disable
  
            byte[] imageBytes = System.IO.File.ReadAllBytes(url);

            Texture2D t2d = new Texture2D(
                2,
                2,
                TextureFormat.RGBA32,
                false
            );

            if (!t2d.LoadImage(imageBytes))
            {
                Destroy(t2d);
                throw new InvalidOperationException(
                    "Failed to decode terrain texture: " + url
                );
            }
            // Prevent terrain textures from sampling the opposite image edge at tile borders.
            t2d.wrapMode = TextureWrapMode.Clamp;
            t2d.filterMode = FilterMode.Bilinear;
            t2d.anisoLevel = 8;

            //t2d.alphaIsTransparency = false;

            TerrainStreamingFastTexture2D ft = ScriptableObject.CreateInstance<TerrainStreamingFastTexture2D>();
            ft.nativeTexture = t2d;
            callback(ft);
            return ft;
 
        }

        private void CleanupTexture()
        {
            isLoaded = false;

            //delete the gl texture
            if (glTextureID != -1)
                CleanupFastTexture2D(glTextureID);
            glTextureID = -1;

            //destroy the wrapper object
            if (nativeTexture)
                Destroy(nativeTexture);

            //remove it from the list so further callbacks dont try to find it
            if (Instances.ContainsKey(this.uuid))
                Instances.Remove(this.uuid);
        }

        //to destroy a FastTexture2D object, you call Destroy() on it.
        public void OnDestroy()
        {
            CleanupTexture();
        }
    }
}