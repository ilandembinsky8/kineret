/*     Unity GIS Tech 2020-2023      */

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace GISTech.GISTerrainLoader
{
    public class GISTerrainLoaderGeoPoint : MonoBehaviour
    {
        [Header("Object Parameters")]
        public bool RandomScale = false;
        [Range(0.1f, 10)]
        public float RandomMaxScale = 1;
        public Transform ObjectBody;

        [Header("UI and TextMesh Parameters")]
        public Vector3 Maxscale = new Vector3(10, 10, 10);
        public float MaxDistance = 30f;// Km
        public TextMesh Unite_TextMesh;
        public Transform UIObject;
        private Camera MainCamera;
        public GISTerrainContainer container;

        [Header("TextMesh Outline Parameters")]
        public float pixelSize = 1;
        public Color outlineColor = Color.white;
        public bool resolutionDependant = false;
        public int doubleResolution = 1024;
        public int TextZoom = 12;


        private MeshRenderer meshRenderer;
        private GISTerrainLoaderVectorPoint VectorPoint;
        private float dis = 0;
        /////// Outline
        public bool EnableZoomableText = true;
        public bool EnableScalableVisibilityText = true;
        private float MaxOrthoZoom = 500;
        // Start is called before the first frame update
        void Start()
        {
            MainCamera = Camera.main;
            VectorPoint = GetComponent<GISTerrainLoaderVectorPoint>();
            container = FindObjectOfType<GISTerrainContainer>();
            meshRenderer = Unite_TextMesh.gameObject.GetComponent<MeshRenderer>();
            MaxOrthoZoom = CalculateMaxZoom(container, MainCamera.aspect);
#if GISTechAdvancedVectorGenerator
            TextZoom = VectorPoint.FeatureStyle.FeatureStyle.LabelStyle.FontSize;
#else
            Unite_TextMesh.fontSize = 14;
#endif

            if (RandomScale)
            {
                if (ObjectBody)
                {
                    var RandomScale = Random.Range(0.1f, RandomMaxScale);
                    ObjectBody.localScale = new Vector3(RandomScale, RandomScale, RandomScale);
                }
            }



            UIObject.localScale = new Vector3(1, 1, 1);
            UpdateUIIconScale();

            for (int i = 0; i < 8; i++)
            {
                GameObject outline = new GameObject("outline", typeof(TextMesh));
                outline.transform.parent = Unite_TextMesh.transform;
                outline.transform.localScale = new Vector3(1, 1, 1);
                outline.transform.rotation = Unite_TextMesh.transform.rotation;

                MeshRenderer otherMeshRenderer = outline.GetComponent<MeshRenderer>();
                otherMeshRenderer.material = new Material(meshRenderer.material);
                otherMeshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                otherMeshRenderer.receiveShadows = false;
                otherMeshRenderer.sortingLayerID = meshRenderer.sortingLayerID;
                otherMeshRenderer.sortingLayerName = meshRenderer.sortingLayerName;
            }

        }
        void FixedUpdate()
        {
            //UpdateUI();
        }
        void UpdateUIIconScale()
        {
            if (UIObject.hasChanged)
            {
                if (UIObject && MainCamera && container)
                {
                    var m_dis = MaxDistance * 1000 * container.Scale.y;
                    if (!Camera.main.orthographic)
                        dis = Vector3.Distance(MainCamera.transform.position, UIObject.position);
                    else
                        dis = MainCamera.orthographicSize * 2;

                    var y_pos = ((dis / 1000) * container.Scale.y) * 2;
                    if (y_pos < 5) y_pos = 5;

                    UIObject.transform.localPosition = new Vector3(UIObject.transform.localPosition.x, y_pos, UIObject.transform.localPosition.z);
                    if (dis > m_dis)
                        UIObject.localScale = Maxscale * container.Scale.y;
                    else
                    {

                        var value = dis * Maxscale.x * container.Scale.y / m_dis;
                        if (value > 1)
                            UIObject.localScale = new Vector3(value, value, value);
                        else if (value < 1)
                        {
                            value = 1; UIObject.localScale = new Vector3(value, value, value);
                        }

                    }
                    LookAt();
                }
            }

        }
        void LookAt()
        {
            UIObject.transform.LookAt(UIObject.transform.position + MainCamera.transform.rotation * Vector3.forward, MainCamera.transform.rotation * Vector3.up);
        }
        public void SetName(string Unite_Name)
        {
            Unite_TextMesh.text = Unite_Name;
        }
        private int GetDynamicTextSize()
        {
            if (MainCamera.orthographic)
            {
                float zoom = Camera.main.orthographicSize;
                float t = Mathf.InverseLerp(50, 2000, zoom); // zoom mapped to 0..1
                return (int)Mathf.Lerp(TextZoom, TextZoom + 8, t);
            }
            else
            {
                return TextZoom;
            }
        }
        void LateUpdate()
        {
            if (Unite_TextMesh != null)
            {
                var ZoomableTextSize = TextZoom;

                if (EnableZoomableText)
                {
                    ZoomableTextSize = GetDynamicTextSize();
                    Unite_TextMesh.fontSize = ZoomableTextSize;

#if GISTechAdvancedVectorGenerator
                                        ApplyLabel(Unite_TextMesh, VectorPoint, MainCamera.orthographicSize, MaxOrthoZoom);

#endif
                }




                //Debug.Log(zoomFactor +"  " + finalFontSize);
                Vector3 screenPoint = Camera.main.WorldToScreenPoint(Unite_TextMesh.transform.position);

                outlineColor.a = Unite_TextMesh.color.a * Unite_TextMesh.color.a;

                // copy attributes
                for (int i = 0; i < Unite_TextMesh.transform.childCount; i++)
                {

                    TextMesh other = Unite_TextMesh.transform.GetChild(i).GetComponent<TextMesh>();
                    other.color = outlineColor;
                    other.text = Unite_TextMesh.text;
                    other.alignment = Unite_TextMesh.alignment;
                    other.anchor = Unite_TextMesh.anchor;
                    other.characterSize = Unite_TextMesh.characterSize;
                    other.font = Unite_TextMesh.font;
                    other.fontSize = ZoomableTextSize;
                    other.fontStyle = Unite_TextMesh.fontStyle;
                    other.richText = Unite_TextMesh.richText;
                    other.tabSize = Unite_TextMesh.tabSize;
                    other.lineSpacing = Unite_TextMesh.lineSpacing;
                    other.offsetZ = Unite_TextMesh.offsetZ + 0.1f;

                    bool doublePixel = resolutionDependant && (Screen.width > doubleResolution || Screen.height > doubleResolution);
                    Vector3 pixelOffset = GetOffset(i) * (doublePixel ? 2.0f * pixelSize : pixelSize);
                    Vector3 worldPoint = Camera.main.ScreenToWorldPoint(screenPoint + pixelOffset * (-0.1f));
                    other.transform.position = worldPoint;

                    MeshRenderer otherMeshRenderer = Unite_TextMesh.transform.GetChild(i).GetComponent<MeshRenderer>();
                    otherMeshRenderer.sortingLayerID = meshRenderer.sortingLayerID;
                    otherMeshRenderer.sortingLayerName = meshRenderer.sortingLayerName;
                }
            }

        }
        Vector3 GetOffset(int i)
        {
            switch (i % 8)
            {
                case 0: return new Vector3(0, 1, 0);
                case 1: return new Vector3(1, 1, 0);
                case 2: return new Vector3(1, 0, 0);
                case 3: return new Vector3(1, -1, 0);
                case 4: return new Vector3(0, -1, 0);
                case 5: return new Vector3(-1, -1, 0);
                case 6: return new Vector3(-1, 0, 0);
                case 7: return new Vector3(-1, 1, 0);
                default: return Vector3.zero;
            }
        }
#if GISTechAdvancedVectorGenerator
        public bool IsLabelVisible(float zoom, float maxZoom, LabelVisibilityZoomLevel ruleZoom)
        {
            float percent = zoom / maxZoom;

            if (percent <= 0.25f && ruleZoom.HasFlag(LabelVisibilityZoomLevel.First)) return true;
            if (percent <= 0.50f && percent > 0.25f && ruleZoom.HasFlag(LabelVisibilityZoomLevel.Second)) return true;
            if (percent <= 0.75f && percent > 0.50f && ruleZoom.HasFlag(LabelVisibilityZoomLevel.Third)) return true;
            if (percent <= 1.00f && percent > 0.75f && ruleZoom.HasFlag(LabelVisibilityZoomLevel.Fourth)) return true;

            return false;
        }
        public void ApplyLabel(TextMesh label, GISTerrainLoaderVectorPoint VectorPoint, float zoom, float maxZoom)
        {
            if (VectorPoint == null)
            {
                label.gameObject.SetActive(false);
                return;
            }

            bool visible = IsLabelVisible(zoom, maxZoom, VectorPoint.FeatureStyle.FeatureStyle.LabelStyle.visibility);
            label.gameObject.SetActive(visible);

            if (!visible) return;

            float t = Mathf.InverseLerp(0, maxZoom, zoom);
            //label.fontSize = (int)Mathf.Lerp(8f,VectorPoint.FeatureStyle.FeatureStyle.FontSize, t);
            //label.font = rule.Font != null ? rule.Font : label.font;
            label.color = VectorPoint.FeatureStyle.FeatureStyle.LabelStyle.FontColor;
        }
#endif

        float CalculateMaxZoom(GISTerrainContainer container, float cameraAspect)
        {
            float Zoom = 1;

            if (container)
            {
                Bounds bounds = container.GlobalTerrainBounds;

                if (bounds.size == Vector3.zero)
                {
                    Debug.LogWarning("⚠️ Terrain bounds are zero — check if calculated.");
                    return 10f; // Fallback
                }

                float terrainWidth = bounds.size.x;
                float terrainHeight = bounds.size.z;

                // Orthographic camera sees height = 2 * orthoSize
                // and width = 2 * orthoSize * aspect
                // So to fit terrain:
                float zoomToFitWidth = (terrainWidth * 0.5f) / cameraAspect;
                float zoomToFitHeight = terrainHeight * 0.5f;

                // ✅ Take the minimum so both width and height fit in view
                float maxZoom = Mathf.Min(zoomToFitWidth, zoomToFitHeight);

                // Optional: apply margin (so labels at edges aren’t cut off)
                float paddingFactor = 0.95f; // 95% of fit to give margin
                Zoom = maxZoom * paddingFactor;
            }
            return Zoom;
        }
        void OnEnable()
        {
            Camera.onPreRender += UpdateZoomedText;
        }
        void OnDisable()
        {
            Camera.onPreRender -= UpdateZoomedText;
        }
        void UpdateZoomedText(Camera cam)
        {
            if (EnableZoomableText)
            {
                int size = GetDynamicTextSize();
                Unite_TextMesh.fontSize = size;
#if GISTechAdvancedVectorGenerator
                ApplyLabel(Unite_TextMesh, VectorPoint, cam.orthographicSize, MaxOrthoZoom);

#endif
            }
        }
    }
}