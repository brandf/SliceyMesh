using System;
using UnityEditor;
using UnityEngine;

namespace SliceyMesh
{
    [ExecuteAlways]
    public class SliceyMesh : MonoBehaviour
    {
        [Flags]
        public enum SliceyQualityFlags
        {
            None                   = 0,
            Explicit               = 1 << 0,
            AdjustWithRadius       = 1 << 1,
            AdjustWithScale        = 1 << 2,
            AdjustWithViewDistance = 1 << 3,
        }

        public enum SliceyOriginType
        {
            FromAnchor,
            FromExplicitCenter,
        }

        // order Z, Y, X
        public enum SliceyAnchor
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
        public SliceyFaceMode FaceMode = SliceyFaceMode.Outside;
        public SliceyOriginType OriginType = SliceyOriginType.FromAnchor;
        public SliceyAnchor Anchor = SliceyAnchor.Center;
        public Vector3 ExplicitCenter;
        public Quaternion Orientation = Quaternion.identity;
        public Vector3 Size = Vector3.one;
        public float Radius = 0.25f;
        public float Quality = 1.0f;

        public SliceyQualityFlags QualityFlags = (SliceyQualityFlags)(-1); // Everything
        public SliceyMaterialFlags MaterialFlags = SliceyMaterialFlags.ShaderSlicingNotSupported;

        public Vector3 Center
        {
            get
            {
                if (OriginType == SliceyOriginType.FromExplicitCenter)
                    return ExplicitCenter;
                var h = 0.5f;
                var unitOffset = Anchor switch
                {
                    SliceyAnchor.FrontBottomLeft    => new Vector3(-h, -h, -h),
                    SliceyAnchor.CenterBottomLeft   => new Vector3(-h, -h, 0f),
                    SliceyAnchor.BackBottomLeft     => new Vector3(-h, -h,  h),
                    SliceyAnchor.FrontCenterLeft    => new Vector3(-h, 0f, -h),
                    SliceyAnchor.CenterCenterLeft   => new Vector3(-h, 0f, 0f),
                    SliceyAnchor.BackCenterLeft     => new Vector3(-h, 0f,  h),
                    SliceyAnchor.FrontTopLeft       => new Vector3(-h,  h, -h),
                    SliceyAnchor.CenterTopLeft      => new Vector3(-h,  h, 0f),
                    SliceyAnchor.BackTopLeft        => new Vector3(-h,  h,  h),
                    SliceyAnchor.FrontBottomCenter  => new Vector3(0f, -h, -h),
                    SliceyAnchor.CenterBottomCenter => new Vector3(0f, -h, 0f),
                    SliceyAnchor.BackBottomCenter   => new Vector3(0f, -h,  h),
                    SliceyAnchor.FrontCenterCenter  => new Vector3(0f, 0f, -h),
                    SliceyAnchor.Center             => new Vector3(0f, 0f, 0f),
                    SliceyAnchor.BackCenterCenter   => new Vector3(0f, 0f,  h),
                    SliceyAnchor.FrontTopCenter     => new Vector3(0f,  h, -h),
                    SliceyAnchor.CenterTopCenter    => new Vector3(0f,  h, 0f),
                    SliceyAnchor.BackTopCenter      => new Vector3(0f,  h,  h),
                    SliceyAnchor.FrontBottomRight   => new Vector3( h, -h, -h),
                    SliceyAnchor.CenterBottomRight  => new Vector3( h, -h, 0f),
                    SliceyAnchor.BackBottomRight    => new Vector3( h, -h,  h),
                    SliceyAnchor.FrontCenterRight   => new Vector3( h, 0f, -h),
                    SliceyAnchor.CenterCenterRight  => new Vector3( h, 0f, 0f),
                    SliceyAnchor.BackCenterRight    => new Vector3( h, 0f,  h),
                    SliceyAnchor.FrontTopRight      => new Vector3( h,  h, -h),
                    SliceyAnchor.CenterTopRight     => new Vector3( h,  h, 0f),
                    SliceyAnchor.BackTopRight       => new Vector3( h,  h,  h),
                };

                return -Vector3.Scale(unitOffset, Size);
            }
        }

        public Bounds LocalBounds
        {
            get => new Bounds(Center, Size);
            set
            {
                OriginType = SliceyOriginType.FromExplicitCenter;
                ExplicitCenter = value.center;
                Size = value.size;
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
#if UNITY_EDITOR
                        camera = _lastCamera = Application.isPlaying ? Camera.main : SceneView.lastActiveSceneView.camera;
#else
                        camera = _lastCamera = Camera.main;
#endif
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

        void OnValidate()
        {
            Radius = Mathf.Min(Mathf.Max(0f, Radius), Size.x / 2f);
            Quality = Mathf.Max(Quality, 0f);
        }

        void Update()
        {
            EnsureRenderer();
            var explicitQualityModifier = QualityFlags.HasFlag(SliceyQualityFlags.Explicit) ? Quality : 1f;
            var radiusQualityModifier = QualityFlags.HasFlag(SliceyQualityFlags.AdjustWithRadius) ? (0.1f + Radius) / 0.35f : 1f;
            // TODO: should be scale in view space
            var scaleQualityModifier = QualityFlags.HasFlag(SliceyQualityFlags.AdjustWithScale) ? Mathf.Max(0.1f, Mathf.Min(transform.lossyScale.x, 3.0f)) : 1f;
            var distanceQualityModifier = 1.0f;
            if (QualityFlags.HasFlag(SliceyQualityFlags.AdjustWithViewDistance))
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
            var effectiveQuality = explicitQualityModifier * radiusQualityModifier * scaleQualityModifier * distanceQualityModifier;

            var (mesh, shaderParams) = Cache.Get(new SliceyConfig()
            {
                Type = Type,
                FaceMode = FaceMode,
                Size = effectiveSize,
                Pose = new Pose(Center, Orientation),
                Radii = new Vector4(Radius, 0, 0, 0),
                Quality = effectiveQuality,
            }, MaterialFlags);
            MeshFilter.mesh = mesh;
            // TODO: shader stuff
        }

#if UNITY_EDITOR
        [CustomEditor(typeof(SliceyMesh))]
        public class Editor : UnityEditor.Editor
        {
            protected SliceyMesh Target => target as SliceyMesh;
            bool UsingUncommon = false;

            public override void OnInspectorGUI()
            {
                //base.OnInspectorGUI();
                //EditorGUILayout.Space();

                EditorGUILayout.BeginHorizontal();
                var typeProp = serializedObject.FindProperty("Type");
                EditorGUILayout.PropertyField(typeProp);
                var typeTexture = ((SliceyMeshType)typeProp.enumValueIndex) switch
                {
                    SliceyMeshType.CuboidHard        => SliceyMeshEdtiorResources.Instance.CuboidHard,
                    SliceyMeshType.CuboidCylindrical => SliceyMeshEdtiorResources.Instance.CuboidCylindrical,
                    SliceyMeshType.CuboidSpherical   => SliceyMeshEdtiorResources.Instance.CuboidSpherical,
                    SliceyMeshType.RectHard          => SliceyMeshEdtiorResources.Instance.RectHard,
                    SliceyMeshType.RectRound         => SliceyMeshEdtiorResources.Instance.RectRound,
                    SliceyMeshType.CuboidSphericalCylindrical => SliceyMeshEdtiorResources.Instance.CuboidSpherical,
                };
                GUILayout.Label(typeTexture);
                EditorGUILayout.EndHorizontal();
                var originType = serializedObject.FindProperty("OriginType");
                
                EditorGUILayout.PropertyField(originType);
                EditorGUI.indentLevel++;
                var originTypeValue = (SliceyOriginType)originType.enumValueIndex;
                if (originTypeValue == SliceyOriginType.FromAnchor)
                {
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("Anchor"));
                }
                else
                {
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("ExplicitCenter"));
                }
            
                EditorGUI.indentLevel--;

                EditorGUILayout.PropertyField(serializedObject.FindProperty("Size"));

                EditorGUILayout.PropertyField(serializedObject.FindProperty("Radius"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("MaterialFlags"));
                
                var qualityProp = serializedObject.FindProperty("QualityFlags");
                EditorGUILayout.PropertyField(qualityProp);

                var qualityFlags = (SliceyQualityFlags)qualityProp.enumValueFlag;
                EditorGUI.indentLevel++;
                if (qualityFlags.HasFlag(SliceyQualityFlags.Explicit))
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("Quality"));
                EditorGUI.indentLevel--;

                // Uncommon
                if (UsingUncommon = EditorGUILayout.BeginFoldoutHeaderGroup(UsingUncommon, "Uncommon Settings"))
                { 
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("FaceMode"));
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("Orientation"));
                    if (qualityFlags.HasFlag(SliceyQualityFlags.AdjustWithViewDistance))
                        EditorGUILayout.PropertyField(serializedObject.FindProperty("_cameraOverride"));
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("_cacheOverride"));
                }


            serializedObject.ApplyModifiedProperties();
            }
        }
#endif
    }
}