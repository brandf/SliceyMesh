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

        public SliceyMeshType Type;
        public Vector3 Size = Vector3.one;
        [Range(0f, 4f)]
        public float Radius = 0.25f;
        [Range(0f, 2f)]
        public float Quality = 1.0f;

        public bool AdjustQualityWithRadius = true;
        public bool AdjustQualityWithScale = true;
        public bool AdjustQualityWithViewDistance = true;

        public Camera Camera;

        public SliceyCache Cache;

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
            var radiusQualityModifier = AdjustQualityWithRadius ? (0.1f + Radius) / 0.35f : 1f;
            // TODO: should be scale in view space
            var scaleQualityModifier = AdjustQualityWithScale ? Mathf.Max(0.1f, Mathf.Min(transform.lossyScale.x, 3.0f)) : 1f;
            var distanceQualityModifier = 1.0f;
            if (AdjustQualityWithViewDistance)
            {
                if (!Camera)
                {
                    var camera = Application.isPlaying ? Camera.main : SceneView.lastActiveSceneView.camera;
                    if (camera)
                    {
                        var distance = (camera.transform.position - transform.position).magnitude;
                        distance /= camera.transform.lossyScale.x;
                        distanceQualityModifier = Mathf.Max(0.1f, Mathf.Min((4.0f - Mathf.Pow(distance, 1.0f / 5.0f) * 1.75f), 3.0f));
                    }

                }
            }

            var effectiveSize = Vector3.Max(Vector3.zero, Size);
            var effectiveQuality = Quality * radiusQualityModifier * scaleQualityModifier * distanceQualityModifier;

            if (Cache)
            {
                var (mesh, shaderParams) = Cache.Get(new SliceyConfig()
                {
                    Type = Type,
                    Size = effectiveSize,
                    Radii = new Vector4(Radius, 0, 0, 0),
                    Quality = effectiveQuality,
                });
                MeshFilter.mesh = mesh;
                // TODO: shader stuff
            }
            else
            {
                MeshFilter.mesh = Type switch
                {
                    SliceyMeshType.CuboidHard => SliceyMeshGenerator.CubeHardEdges(effectiveSize),
                    SliceyMeshType.CuboidCylindrical => Radius == 0f ? SliceyMeshGenerator.CubeHardEdges(effectiveSize) : SliceyMeshGenerator.CubeRoundedRect(effectiveSize, Radius, effectiveQuality),
                    SliceyMeshType.CuboidSpherical => Radius == 0f ? SliceyMeshGenerator.CubeHardEdges(effectiveSize) : SliceyMeshGenerator.CubeRoundedEdges(effectiveSize, Radius, effectiveQuality),
                };
            }

        }
    }
}