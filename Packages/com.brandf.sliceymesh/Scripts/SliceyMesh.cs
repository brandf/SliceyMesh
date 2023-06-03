using System;
using UnityEditor;
using UnityEngine;

namespace SliceyMesh
{
    [ExecuteAlways]
    public class SliceyMesh : MonoBehaviour
    {
        public enum SliceyMeshType
        {
            CuboidHard,
            CuboidCylindrical,
            CuboidSpherical,
        }

        [Flags]
        public enum SliceyQuailtyFlags
        {
            None                   = 0,
            AdjustWithRadius       = 1 << 0,
            AdjustWithScale        = 1 << 1,
            AdjustWithViewDistance = 1 << 2,
        }

        // order Z, Y, X
        public enum SliceyOrigin
        {
            FrontBottomLeft,
            CenterBottomLeft,
            BackBottomLeft,
            FrontCenterLeft,
            CenterCenterLeft,
            BackCenterLeft,
            FrontTopLeft,
            CenterTopLeft,
            BackTopLeft,
            FrontBottomCenter,
            CenterBottomCenter,
            BackBottomCenter,
            FrontCenterCenter,
            Center,
            BackCenterCenter,
            FrontTopCenter,
            CenterTopCenter,
            BackTopCenter,
            FrontBottomRight,
            CenterBottomRight,
            BackBottomRight,
            FrontCenterRight,
            CenterCenterRight,
            BackCenterRight,
            FrontTopRight,
            CenterTopRight,
            BackTopRight,
        }

        public SliceyMeshType Type = SliceyMeshType.CuboidSpherical;
        public SliceyOrigin Origin = SliceyOrigin.Center;
        public Vector3 Size = Vector3.one;
        [Range(0f, 4f)]
        public float Radius = 0.25f;
        [Range(0f, 2f)]
        public float Quality = 1.0f;

        public SliceyQuailtyFlags QualityFlags = (SliceyQuailtyFlags)(-1); // Everything
        public SliceyMaterialFlags MaterialFlags = SliceyMaterialFlags.ShaderSlicingNotSupported;

        public Vector3 Center
        {
            get
            {
                var h = 0.5f;
                var unitOffset = Origin switch
                {
                    SliceyOrigin.FrontBottomLeft    => new Vector3(-h, -h, -h),
                    SliceyOrigin.CenterBottomLeft   => new Vector3(-h, -h, 0f),
                    SliceyOrigin.BackBottomLeft     => new Vector3(-h, -h,  h),
                    SliceyOrigin.FrontCenterLeft    => new Vector3(-h, 0f, -h),
                    SliceyOrigin.CenterCenterLeft   => new Vector3(-h, 0f, 0f),
                    SliceyOrigin.BackCenterLeft     => new Vector3(-h, 0f,  h),
                    SliceyOrigin.FrontTopLeft       => new Vector3(-h,  h, -h),
                    SliceyOrigin.CenterTopLeft      => new Vector3(-h,  h, 0f),
                    SliceyOrigin.BackTopLeft        => new Vector3(-h,  h,  h),
                    SliceyOrigin.FrontBottomCenter  => new Vector3(0f, -h, -h),
                    SliceyOrigin.CenterBottomCenter => new Vector3(0f, -h, 0f),
                    SliceyOrigin.BackBottomCenter   => new Vector3(0f, -h,  h),
                    SliceyOrigin.FrontCenterCenter  => new Vector3(0f, 0f, -h),
                    SliceyOrigin.Center             => new Vector3(0f, 0f, 0f),
                    SliceyOrigin.BackCenterCenter   => new Vector3(0f, 0f,  h),
                    SliceyOrigin.FrontTopCenter     => new Vector3(0f,  h, -h),
                    SliceyOrigin.CenterTopCenter    => new Vector3(0f,  h, 0f),
                    SliceyOrigin.BackTopCenter      => new Vector3(0f,  h,  h),
                    SliceyOrigin.FrontBottomRight   => new Vector3( h, -h, -h),
                    SliceyOrigin.CenterBottomRight  => new Vector3( h, -h, 0f),
                    SliceyOrigin.BackBottomRight    => new Vector3( h, -h,  h),
                    SliceyOrigin.FrontCenterRight   => new Vector3( h, 0f, -h),
                    SliceyOrigin.CenterCenterRight  => new Vector3( h, 0f, 0f),
                    SliceyOrigin.BackCenterRight    => new Vector3( h, 0f,  h),
                    SliceyOrigin.FrontTopRight      => new Vector3( h,  h, -h),
                    SliceyOrigin.CenterTopRight     => new Vector3( h,  h, 0f),
                    SliceyOrigin.BackTopRight       => new Vector3( h,  h,  h),
                };

                return -Vector3.Scale(unitOffset, Size);
            }
        }

        Camera _lastCamera; // non-serialized cache so we don't do Camera.main every frame
        [SerializeField]
        Camera _cameraOverride;
        public Camera Camera
        {
            get
            {
                
                var camera = _cameraOverride;
                if (!camera)
                {
                    camera = _lastCamera;
                    if (!camera)
                    {
                        camera = _lastCamera = Application.isPlaying ? Camera.main : SceneView.lastActiveSceneView.camera;
                    }
                }

                return camera;
            }
        }

        [SerializeField]
        SliceyCache _cacheOverride;
        SliceyCache Cache
        {
            get
            {
                var cache = _cacheOverride;
                if (!cache)
                {
                    var defaultCache = SliceyCache.DefaultCache;
                    if (defaultCache)
                    {
                        cache = defaultCache;
                    }
                    else
                    {
                        cache = FindFirstObjectByType<SliceyCache>();
                        if (!cache)
                        {
                            cache = new GameObject("SliceyCache").AddComponent<SliceyCache>();
                            cache.gameObject.hideFlags = HideFlags.None; //HideFlags.HideAndDontSave;
                        }
                        SliceyCache.DefaultCache = cache;
                    }
                }

                return cache;
            }
        }

        protected MeshRenderer Renderer;
        protected MeshFilter MeshFilter;
        protected void EnsureRenderer()
        {
            MeshFilter = gameObject.GetComponent<MeshFilter>() ?? gameObject.AddComponent<MeshFilter>();
            MeshFilter.hideFlags = HideFlags.None;// HideFlags.HideAndDontSave | HideFlags.HideInInspector;
            Renderer = gameObject.GetComponent<MeshRenderer>() ?? gameObject.AddComponent<MeshRenderer>();
        }

        void Update()
        {
            EnsureRenderer();
            var radiusQualityModifier = QualityFlags.HasFlag(SliceyQuailtyFlags.AdjustWithRadius) ? (0.1f + Radius) / 0.35f : 1f;
            // TODO: should be scale in view space
            var scaleQualityModifier = QualityFlags.HasFlag(SliceyQuailtyFlags.AdjustWithScale) ? Mathf.Max(0.1f, Mathf.Min(transform.lossyScale.x, 3.0f)) : 1f;
            var distanceQualityModifier = 1.0f;
            if (QualityFlags.HasFlag(SliceyQuailtyFlags.AdjustWithViewDistance))
            {
                var camera = Camera;
                if (camera)
                { 
                    var distance = (camera.transform.position - transform.position).magnitude;
                    distance /= camera.transform.lossyScale.x;
                    distanceQualityModifier = Mathf.Max(0.1f, Mathf.Min((4.0f - Mathf.Pow(distance, 1.0f / 5.0f) * 1.75f), 3.0f));
                }
            }

            var effectiveSize = Vector3.Max(Vector3.zero, Size);
            var effectiveQuality = Quality * radiusQualityModifier * scaleQualityModifier * distanceQualityModifier;

            var (mesh, shaderParams) = Cache.Get(new SliceyConfig()
            {
                Type = Type,
                Size = effectiveSize,
                Offset = Center,
                Radii = new Vector4(Radius, 0, 0, 0),
                Quality = effectiveQuality,
            }, MaterialFlags);
            MeshFilter.mesh = mesh;
            // TODO: shader stuff
        }
    }
}