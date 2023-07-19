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

        public SliceyMeshType Type = SliceyMeshType.Cube;
        public SliceyMeshRectSubType RectSubType = SliceyMeshRectSubType.Round;
        public SliceyMeshCubeSubType CubeSubType = SliceyMeshCubeSubType.RoundSidesAndZ;
        public SliceyMeshCylinderSubType CylinderSubType = SliceyMeshCylinderSubType.RoundZ;
        public SliceyFaceMode FaceMode = SliceyFaceMode.Outside;
        public SliceyOriginType OriginType = SliceyOriginType.FromAnchor;
        public SliceyAnchor Anchor = SliceyAnchor.Center;
        public Vector3 ExplicitCenter;
        public Quaternion Orientation = Quaternion.identity;
        public Vector3 Size = Vector3.one;
        public Vector3 Radii = Vector3.one * 0.25f;
        public Vector3 Quality = Vector3.one;

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

        public void SetLocalSize(Bounds bounds)
        {
            Size = bounds.size;
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

        private void OnEnable()
        {
            EnsureRenderer();
            Renderer.enabled = true;
        }

        private void OnDisable()
        {
            EnsureRenderer();
            Renderer.enabled = false;
        }

        void OnValidate()
        {
            Radii = Vector3.Min(Vector3.Max(Vector3.zero, Radii), Size * 0.5f);
            Quality = Vector3.Max(Quality, Vector3.zero);
        }

        void Update()
        {
            EnsureRenderer();
            var explicitQuality = QualityFlags.HasFlag(SliceyQualityFlags.Explicit) ? Quality : Vector3.one;
           
            var radiusQualityModifier = QualityFlags.HasFlag(SliceyQualityFlags.AdjustWithRadius) ? (0.1f + Radii.x) / 0.35f : 1f;
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
            var effectiveQuality = explicitQuality * (radiusQualityModifier * scaleQualityModifier * distanceQualityModifier);

            var (mesh, shaderParams) = Cache.Get(new SliceyConfig()
            {
                Type = Type,
                SubType = Type switch
                {
                    SliceyMeshType.Rect => (int)RectSubType,
                    SliceyMeshType.Cube => (int)CubeSubType,
                    SliceyMeshType.Cylinder => (int)CylinderSubType,
                },
                FaceMode = FaceMode,
                Size = effectiveSize,
                Pose = new Pose(Center, Orientation),
                Radii = Radii,
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

                EditType(out var meshType, out var rectSubType, out var cubeSubType, out var cylinderSubType);
                EditPosition();
                EditSize(meshType, out var sizeProp, out var size);
                var primaryRadius = EditRadii(meshType, size.x, rectSubType, cubeSubType, cylinderSubType);
                if (primaryRadius != null)
                {
                    size.x = size.y = primaryRadius.Value;
                }
                var qualityFlags = EditQuality(meshType, rectSubType, cubeSubType, cylinderSubType);
                EditMaterial();

                // Uncommon
                size = EditUncommon(meshType, size, qualityFlags);

                if (meshType == SliceyMeshType.Rect || meshType == SliceyMeshType.Cylinder)
                    sizeProp.vector3Value = size;

                serializedObject.ApplyModifiedProperties();
            }

            void EditType(out SliceyMeshType meshType, out SliceyMeshRectSubType rectSubType, out SliceyMeshCubeSubType cubeSubType, out SliceyMeshCylinderSubType cylinderSubType)
            {
                var typeProp = serializedObject.FindProperty("Type");
                EditorGUILayout.PropertyField(typeProp);
                meshType = (SliceyMeshType)typeProp.enumValueIndex;
                EditorGUI.indentLevel++;
                var rectSubTypeProp = serializedObject.FindProperty("RectSubType");
                if (meshType == SliceyMeshType.Rect)
                    EditorGUILayout.PropertyField(rectSubTypeProp, new GUIContent("Sub Type"));
                rectSubType = (SliceyMeshRectSubType)rectSubTypeProp.enumValueIndex;
                var cubeSubTypeProp = serializedObject.FindProperty("CubeSubType");
                if (meshType == SliceyMeshType.Cube)
                    EditorGUILayout.PropertyField(cubeSubTypeProp, new GUIContent("Sub Type"));
                cubeSubType = (SliceyMeshCubeSubType)cubeSubTypeProp.enumValueIndex;
                var cylinderSubTypeProp = serializedObject.FindProperty("CylinderSubType");
                if (meshType == SliceyMeshType.Cylinder)
                    EditorGUILayout.PropertyField(cylinderSubTypeProp, new GUIContent("Sub Type"));
                cylinderSubType = (SliceyMeshCylinderSubType)cylinderSubTypeProp.enumValueIndex;
                EditorGUI.indentLevel--;
            }
            
            void EditPosition()
            {
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
            }

            void EditSize(SliceyMeshType meshType, out SerializedProperty sizeProp, out Vector3 size)
            {
                sizeProp = serializedObject.FindProperty("Size");
                size = sizeProp.vector3Value;
                if (meshType == SliceyMeshType.Rect)
                {
                    var rectSize = EditorGUILayout.Vector2Field("Size", size);
                    size.x = rectSize.x;
                    size.y = rectSize.y;

                }
                else if (meshType == SliceyMeshType.Cylinder)
                {
                    size.z = EditorGUILayout.FloatField("Depth", size.z);
                }
                else
                {
                    EditorGUILayout.PropertyField(sizeProp);
                }
            }

            float? EditRadii(SliceyMeshType meshType, float primaryRadius, SliceyMeshRectSubType rectSubType, SliceyMeshCubeSubType cubeSubType, SliceyMeshCylinderSubType cylinderSubType)
            {
                var radiiProp = serializedObject.FindProperty("Radii");
                var radiiValue = radiiProp.vector3Value;
                if (meshType == SliceyMeshType.Rect && rectSubType == SliceyMeshRectSubType.Hard ||
                    meshType == SliceyMeshType.Cube && cubeSubType == SliceyMeshCubeSubType.Hard)
                {
                    return null;
                }

                float? sizeRadius = null;
                EditorGUILayout.LabelField("Radii");
                EditorGUI.indentLevel++;

                switch (meshType)
                {
                    case SliceyMeshType.Rect:
                        switch (rectSubType)
                        {
                            case SliceyMeshRectSubType.Round:
                                radiiValue = Vector3.one * EditorGUILayout.FloatField("Corner Radius", radiiValue.x);
                                break;
                        }
                        break;
                    case SliceyMeshType.Cube:
                        switch (cubeSubType)
                        {
                            case SliceyMeshCubeSubType.RoundSides:
                                radiiValue = Vector3.one * EditorGUILayout.FloatField("Side Edge Radius", radiiValue.x);
                                break;
                            case SliceyMeshCubeSubType.RoundSidesAndZ:
                                radiiValue.x = EditorGUILayout.FloatField("Side Edge Radius", radiiValue.x);
                                radiiValue.y = EditorGUILayout.FloatField("Front/Back Edge Radius", radiiValue.y);
                                radiiValue.z = radiiValue.y;
                                break;
                            case SliceyMeshCubeSubType.RoundSidesAndZAsymmetric:
                                radiiValue.x = EditorGUILayout.FloatField("Side Edge Radius", radiiValue.x);
                                radiiValue.y = EditorGUILayout.FloatField("Front Edge Radius", radiiValue.y);
                                radiiValue.z = EditorGUILayout.FloatField("Back Edge Radius", radiiValue.z);
                                break;
                        }
                        break;
                    case SliceyMeshType.Cylinder:
                        sizeRadius = EditorGUILayout.FloatField("Primary Radius", primaryRadius);
                        
                        switch (cylinderSubType)
                        {
                            case SliceyMeshCylinderSubType.RoundZ:
                                radiiValue = Vector3.one * EditorGUILayout.FloatField("Edge Radius", radiiValue.x);
                                break;
                            case SliceyMeshCylinderSubType.RoundZAsymmetric:
                                radiiValue.x = EditorGUILayout.FloatField("Front Edge Radius", radiiValue.x);
                                radiiValue.y = EditorGUILayout.FloatField("Back Edge Radius", radiiValue.y);
                                radiiValue.z = radiiValue.x;
                                break;
                        }
                        break;
                };

                EditorGUI.indentLevel--;
                radiiProp.vector3Value = radiiValue;
                return sizeRadius;
            }

            SliceyQualityFlags EditQuality(SliceyMeshType meshType, SliceyMeshRectSubType rectSubType, SliceyMeshCubeSubType cubeSubType, SliceyMeshCylinderSubType cylinderSubType)
            {
                if (meshType == SliceyMeshType.Rect && rectSubType == SliceyMeshRectSubType.Hard ||
                    meshType == SliceyMeshType.Cube && cubeSubType == SliceyMeshCubeSubType.Hard)
                {
                    return SliceyQualityFlags.None;
                }

                var qualityFlagsProp = serializedObject.FindProperty("QualityFlags");
                EditorGUILayout.PropertyField(qualityFlagsProp, new GUIContent("Quality"));

                var qualityFlags = (SliceyQualityFlags)qualityFlagsProp.enumValueFlag;
                if (!qualityFlags.HasFlag(SliceyQualityFlags.Explicit))
                    return qualityFlags;


                var qualityProp = serializedObject.FindProperty("Quality");
                var qualityValue = qualityProp.vector3Value;

                EditorGUI.indentLevel++;
                switch (meshType)
                {
                    case SliceyMeshType.Rect:
                        switch (rectSubType)
                        {
                            case SliceyMeshRectSubType.Round:
                                qualityValue = Vector3.one * EditorGUILayout.FloatField("Corner Quality", qualityValue.x);
                                break;
                        }
                        break;
                    case SliceyMeshType.Cube:
                        switch (cubeSubType)
                        {
                            case SliceyMeshCubeSubType.RoundSides:
                                qualityValue = Vector3.one * EditorGUILayout.FloatField("Side Edge Quality", qualityValue.x);
                                break;
                            case SliceyMeshCubeSubType.RoundSidesAndZ:
                                qualityValue.x = EditorGUILayout.FloatField("Side Edge Quality", qualityValue.x);
                                qualityValue.y = EditorGUILayout.FloatField("Front/Back Edge Quality", qualityValue.y);
                                qualityValue.z = qualityValue.y;
                                break;
                            case SliceyMeshCubeSubType.RoundSidesAndZAsymmetric:
                                qualityValue.x = EditorGUILayout.FloatField("Side Edge Quality", qualityValue.x);
                                qualityValue.y = EditorGUILayout.FloatField("Front Edge Quality", qualityValue.y);
                                qualityValue.z = EditorGUILayout.FloatField("Back Edge Quality", qualityValue.z);
                                break;
                        }
                        break;
                    case SliceyMeshType.Cylinder:
                        switch (cylinderSubType)
                        {
                            case SliceyMeshCylinderSubType.Hard:
                                qualityValue = Vector3.one * EditorGUILayout.FloatField("Radial Quality", qualityValue.x);
                                break;
                            case SliceyMeshCylinderSubType.RoundZ:
                                qualityValue.x = EditorGUILayout.FloatField("Radial Quality", qualityValue.x);
                                qualityValue.y = EditorGUILayout.FloatField("Edge Quality", qualityValue.y);
                                qualityValue.z = qualityValue.y;
                                break;
                            case SliceyMeshCylinderSubType.RoundZAsymmetric:
                                qualityValue.x = EditorGUILayout.FloatField("Radial Quality", qualityValue.x);
                                qualityValue.y = EditorGUILayout.FloatField("Front Edge Quality", qualityValue.y);
                                qualityValue.z = EditorGUILayout.FloatField("Back Edge Quality", qualityValue.z);
                                break;
                        }
                        break;
                };


                qualityProp.vector3Value = qualityValue;
                EditorGUI.indentLevel--;
                return qualityFlags;
            }

            void EditMaterial()
            {
                EditorGUILayout.PropertyField(serializedObject.FindProperty("MaterialFlags"));
            }

            Vector3 EditUncommon(SliceyMeshType meshType, Vector3 size, SliceyQualityFlags qualityFlags)
            {
                if (UsingUncommon = EditorGUILayout.BeginFoldoutHeaderGroup(UsingUncommon, "Uncommon Settings"))
                {
                    if (meshType == SliceyMeshType.Rect)
                        size.z = EditorGUILayout.FloatField("Depth", size.z);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("FaceMode"));
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("Orientation"));
                    if (qualityFlags.HasFlag(SliceyQualityFlags.AdjustWithViewDistance))
                        EditorGUILayout.PropertyField(serializedObject.FindProperty("_cameraOverride"));
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("_cacheOverride"));
                }

                return size;
            }
        }
#endif
    }
}