using System;
using UnityEngine;
using UnityEngine.Rendering;

namespace SliceyMesh
{
    [ExecuteAlways]
    public class SliceyRectOutline : MonoBehaviour
    {
        public Vector3 Size = Vector3.one;
        public float Radius = 0.25f;
        public float Thickness = 0.1f;
        public float Quality = 1f;

        void OnValidate()
        {
            Size = Vector3.Max(Size, Vector3.zero);
            Radius = Mathf.Max(Radius, 0f);
            Thickness = Mathf.Max(Thickness, 0f);
            Quality = Mathf.Max(Quality, 0f);
        }

        public void SetLocalSize(Bounds bounds)
        {
            Size = bounds.size;
        }

        [SerializeField]
        SliceyRectOutlineCache _cacheOverride;
        SliceyRectOutlineCache Cache
        {
            get
            {
                var cache = _cacheOverride;
                if (!cache)
                {
                    var defaultCache = SliceyRectOutlineCache.DefaultCache;
                    if (defaultCache)
                    {
                        cache = defaultCache;
                    }
                    else
                    {
                        cache = FindFirstObjectByType<SliceyRectOutlineCache>();
                        if (!cache)
                        {
                            var go = GameObject.Find("SliceyCache");
                            if (!go)
                                go = new GameObject("SliceyCache");
                            cache = go.AddComponent<SliceyRectOutlineCache>();
                        }
                        SliceyRectOutlineCache.DefaultCache = cache;
                    }
                }

                return cache;
            }
        }

        protected MeshRenderer Renderer;
        protected MeshFilter MeshFilter;
        protected void EnsureRenderer()
        {
            MeshFilter = gameObject.GetComponent<MeshFilter>();
            if (!MeshFilter)
                MeshFilter = gameObject.AddComponent<MeshFilter>();
            MeshFilter.hideFlags = HideFlags.HideAndDontSave | HideFlags.HideInInspector;
            Renderer = gameObject.GetComponent<MeshRenderer>();
            if (!Renderer)
            {
                Renderer = gameObject.AddComponent<MeshRenderer>();

                // lame hack to get default material
                GameObject primitive = GameObject.CreatePrimitive(PrimitiveType.Plane);
                Material defaultMaterial = primitive.GetComponent<MeshRenderer>().sharedMaterial;
                DestroyImmediate(primitive);

                Renderer.sharedMaterial = defaultMaterial;
            }
        }

#if UNITY_2019_1_OR_NEWER
        void BeginCameraRendering(ScriptableRenderContext ctx, Camera cam) => Render(cam);
#endif


        void SubscribeRender()
        {
            if (GraphicsSettings.renderPipelineAsset != null) // Using SRP
            {
#if UNITY_2019_1_OR_NEWER
                RenderPipelineManager.beginCameraRendering += BeginCameraRendering;
#else
				UnityEngine.Experimental.Rendering.RenderPipeline.beginCameraRendering += BeginCameraRendering;
#endif
            }
            else
                Camera.onPreCull += Render;
        }

        void UnsubscribeRender()
        {
            if (GraphicsSettings.renderPipelineAsset != null) // Using SRP
            {
#if UNITY_2019_1_OR_NEWER
                RenderPipelineManager.beginCameraRendering -= BeginCameraRendering;
#else
				UnityEngine.Experimental.Rendering.RenderPipeline.beginCameraRendering -= BeginCameraRendering;
#endif
            }
            else
                Camera.onPreCull -= Render;
        }


        private void OnEnable()
        {
            EnsureRenderer();
            Renderer.enabled = true;
            SubscribeRender();
        }

        private void OnDisable()
        {
            EnsureRenderer();
            Renderer.enabled = false;
            UnsubscribeRender();
        }

        public void Render(Camera camera)
        {
            EnsureRenderer();

            var mesh = Cache.Get(new SliceyRectOutlineConfig()
            {
                Size = Size,
                Radius = Radius,
                Thickness = Thickness,
                Quality = Quality,
            });
            MeshFilter.mesh = mesh;
        }
    }
}
