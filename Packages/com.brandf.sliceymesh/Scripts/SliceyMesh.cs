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
            Default   = 0,
            Explicit  = 1 << 0,
            Automatic = 1 << 1,
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
        public SliceyMeshCubeSubType CubeSubType = SliceyMeshCubeSubType.RoundEdges;
        public SliceyMeshCylinderSubType CylinderSubType = SliceyMeshCylinderSubType.RoundEdges;
        public SliceyFaceMode FaceMode = SliceyFaceMode.Outside;
        public SliceyMeshPortion Portion = SliceyMeshPortion.Full;
        public bool PortionClosed = false;
        public SliceyOriginType OriginType = SliceyOriginType.FromAnchor;
        public SliceyAnchor Anchor = SliceyAnchor.Center;
        public Vector3 ExplicitCenter;
        public Quaternion Orientation = Quaternion.identity;
        public Vector3 Size = Vector3.one;
        public Vector2 Radii = Vector2.one * 0.25f;
        public Vector2 Quality = Vector2.one;

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
            Size = Vector3.Max(Size, Vector3.zero);
            Radii = Vector2.Max(Radii, Vector2.zero);
            Quality = Vector2.Max(Quality, Vector2.zero);
        }

        void LateUpdate()
        {
            EnsureRenderer();
            var explicitQuality = QualityFlags.HasFlag(SliceyQualityFlags.Explicit) ? Quality : Vector2.one;
            var autoLOD = QualityFlags.HasFlag(SliceyQualityFlags.Automatic);


            var radiusQualityModifier = autoLOD ? Vector2.Min(Vector2.one * 0.1f + Radii * 2.857f, Vector2.one * 4f) : Vector2.one;
            // TODO: should be scale in view space
            var scaleQualityModifier = autoLOD ? Mathf.Max(0.1f, Mathf.Min(Mathf.Abs(transform.lossyScale.x), 3.0f)) : 1f;
            var distanceQualityModifier = 1.0f;
            if (autoLOD)
            {
                var camera = Camera;
                if (camera)
                { 
                    var distance = (camera.transform.position - transform.position).magnitude;
                    distance /= camera.transform.lossyScale.x;
                    distanceQualityModifier = Mathf.Max(0.1f, Mathf.Min((4.0f - Mathf.Pow(distance, 1.0f / 5.0f) * 1.75f), 3.0f));
                }
            }

            var effectiveQuality = explicitQuality  * (scaleQualityModifier * distanceQualityModifier);
            effectiveQuality.Scale(radiusQualityModifier);
            //Debug.Log($"distanceQualityModifier = {distanceQualityModifier}");
            //Debug.Log($"effectiveQuality = {effectiveQuality}");

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
                Portion = Portion,
                PortionClosed = PortionClosed,
                Size = Size,
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

            public override void OnInspectorGUI()
            {
                //base.OnInspectorGUI();
                //EditorGUILayout.Space();

                EditType(out var meshType, out var rectSubType, out var cubeSubType, out var cylinderSubType);
                EditPosition();
                EditSize(meshType, out var sizeProp, out var size);
                var radii = EditRadii(meshType, size, rectSubType, cubeSubType, cylinderSubType);

                if (meshType == SliceyMeshType.Cylinder)
                {
                    size.x = size.y = radii.x * 2f;
                }
                var qualityFlags = EditQuality(meshType, rectSubType, cubeSubType, cylinderSubType);
                EditMaterial();

                // Uncommon
                EditUncommon(meshType, qualityFlags);

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
                    size.z = EditorGUILayout.FloatField("Z Offset", size.z / 2.0f) * 2.0f;

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

            Vector2 EditRadii(SliceyMeshType meshType, Vector3 size, SliceyMeshRectSubType rectSubType, SliceyMeshCubeSubType cubeSubType, SliceyMeshCylinderSubType cylinderSubType)
            {
                var radiiProp = serializedObject.FindProperty("Radii");
                var radiiValue = radiiProp.vector2Value;
                if (meshType == SliceyMeshType.Rect && rectSubType == SliceyMeshRectSubType.Hard ||
                    meshType == SliceyMeshType.Cube && cubeSubType == SliceyMeshCubeSubType.Hard)
                {
                    return Vector2.zero;
                }

                EditorGUILayout.LabelField("Radii");
                EditorGUI.indentLevel++;

                switch (meshType)
                {
                    case SliceyMeshType.Rect:
                        switch (rectSubType)
                        {
                            case SliceyMeshRectSubType.Round:
                                radiiValue = Vector2.one * EditorGUILayout.FloatField("Corner Radius", radiiValue.x);
                                break;
                        }
                        break;
                    case SliceyMeshType.Cube:
                        switch (cubeSubType)
                        {
                            case SliceyMeshCubeSubType.RoundSides:
                                radiiValue = Vector2.one * EditorGUILayout.FloatField("Side Edge Radius", radiiValue.x);
                                break;
                            case SliceyMeshCubeSubType.RoundEdges:
                                radiiValue = Vector2.one * EditorGUILayout.FloatField("Edge Radius", radiiValue.x);
                                break;
                            case SliceyMeshCubeSubType.RoundSidesFillet:
                                radiiValue.x = EditorGUILayout.FloatField("Side Edge Radius", radiiValue.x);
                                radiiValue.y = EditorGUILayout.FloatField("Fillet Radius", radiiValue.y);
                                break;
                        }
                        break;
                    case SliceyMeshType.Cylinder:
                        radiiValue.x = EditorGUILayout.FloatField("Primary Radius", size.x / 2f); // note we read this from size bc it's a better experience if you switch between mesh types

                        switch (cylinderSubType)
                        {
                            case SliceyMeshCylinderSubType.Hard:
                                radiiValue.y = 0f;
                                break;
                            case SliceyMeshCylinderSubType.RoundEdges:
                                radiiValue.y = EditorGUILayout.FloatField("Edge Radius", radiiValue.y);
                                break;
                        }
                        break;
                };

                EditorGUI.indentLevel--;
                radiiProp.vector2Value = radiiValue;
                return radiiValue;
            }

            SliceyQualityFlags EditQuality(SliceyMeshType meshType, SliceyMeshRectSubType rectSubType, SliceyMeshCubeSubType cubeSubType, SliceyMeshCylinderSubType cylinderSubType)
            {
                if (meshType == SliceyMeshType.Rect && rectSubType == SliceyMeshRectSubType.Hard ||
                    meshType == SliceyMeshType.Cube && cubeSubType == SliceyMeshCubeSubType.Hard)
                {
                    return SliceyQualityFlags.Default;
                }

                var qualityFlagsProp = serializedObject.FindProperty("QualityFlags");
                EditorGUILayout.PropertyField(qualityFlagsProp, new GUIContent("Quality"));

                var qualityFlags = (SliceyQualityFlags)qualityFlagsProp.enumValueFlag;
                if (!qualityFlags.HasFlag(SliceyQualityFlags.Explicit))
                    return qualityFlags;


                var qualityProp = serializedObject.FindProperty("Quality");
                var qualityValue = qualityProp.vector2Value;

                EditorGUI.indentLevel++;
                switch (meshType)
                {
                    case SliceyMeshType.Rect:
                        switch (rectSubType)
                        {
                            case SliceyMeshRectSubType.Round:
                                qualityValue = Vector2.one * EditorGUILayout.FloatField("Corner Quality", qualityValue.x);
                                break;
                        }
                        break;
                    case SliceyMeshType.Cube:
                        switch (cubeSubType)
                        {
                            case SliceyMeshCubeSubType.RoundSides:
                                qualityValue = Vector2.one * EditorGUILayout.FloatField("Side Edge Quality", qualityValue.x);
                                break;
                            case SliceyMeshCubeSubType.RoundEdges:
                                qualityValue = Vector2.one * EditorGUILayout.FloatField("Edge Quality", qualityValue.x);
                                break;
                            case SliceyMeshCubeSubType.RoundSidesFillet:
                                qualityValue.x = EditorGUILayout.FloatField("Side Edge Quality", qualityValue.x);
                                qualityValue.y = EditorGUILayout.FloatField("Fillet Quality", qualityValue.y);
                                break;
                        }
                        break;
                    case SliceyMeshType.Cylinder:
                        switch (cylinderSubType)
                        {
                            case SliceyMeshCylinderSubType.Hard:
                                qualityValue = Vector2.one * EditorGUILayout.FloatField("Radial Quality", qualityValue.x);
                                break;
                            case SliceyMeshCylinderSubType.RoundEdges:
                                qualityValue.x = EditorGUILayout.FloatField("Radial Quality", qualityValue.x);
                                qualityValue.y = EditorGUILayout.FloatField("Edge Quality", qualityValue.y);
                                break;
                        }
                        break;
                };


                qualityProp.vector2Value = qualityValue;
                EditorGUI.indentLevel--;
                return qualityFlags;
            }

            void EditMaterial()
            {
                EditorGUILayout.PropertyField(serializedObject.FindProperty("MaterialFlags"));
            }

            void EditUncommon(SliceyMeshType meshType, SliceyQualityFlags qualityFlags)
            {
                var uncommonProp = serializedObject.FindProperty("FaceMode");
                if (uncommonProp.isExpanded = EditorGUILayout.BeginFoldoutHeaderGroup(uncommonProp.isExpanded, "Uncommon Settings"))
                {
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("FaceMode"));
                    var portionProp = serializedObject.FindProperty("Portion");
                    EditorGUILayout.PropertyField(portionProp);
                    if ((SliceyMeshPortion)portionProp.enumValueIndex != SliceyMeshPortion.Full)
                    {
                        EditorGUI.indentLevel++;
                        EditorGUILayout.PropertyField(serializedObject.FindProperty("PortionClosed"));
                        EditorGUI.indentLevel--;
                    }
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("Orientation"));
                    if (qualityFlags.HasFlag(SliceyQualityFlags.Automatic))
                        EditorGUILayout.PropertyField(serializedObject.FindProperty("_cameraOverride"));
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("_cacheOverride"));
                }
            }
        }
#endif
    }
}