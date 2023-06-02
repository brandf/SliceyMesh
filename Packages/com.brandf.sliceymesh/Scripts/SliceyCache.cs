using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace SliceyMesh
{
    public struct SliceyConfig
    {
        public SliceyMesh.SliceyMeshType Type;
        public Vector3 Size;
        public Vector4 Radii;
        public float Quality;

    }

    public struct SliceyCacheValue
    {
        public int LastAccessedFrame; // Time.frameCount the last time this was accessed
        public int LastAccessedCount; // number of times this was requested on the LastAccessedFrame
        public SliceyMeshBuilder Builder; // 
        public Mesh Mesh;
    }

    public enum SliceyShaderType
    {
        None,
        Rect9Slice,
        Rect16Slice,
        Cublic27Slice,
        Cublic256Slice,
    }

    public struct SliceyShaderParameters
    {
        public SliceyShaderType Type;
        // meaning of these depend on Type
        public Vector4 SourceInnerParam;
        public Vector4 SourceOuterParam;
        public Vector4 TargetInnerParam;
        public Vector4 TargetOuterParam;
    }

    public class SliceyCache : MonoBehaviour
    {
        public bool LogMisses;
        public bool LogHits;
        public bool LogSlicing;

        enum SliceyCacheStage
        {
            CompleteResult,
            CanonicalComplete,
            // test to see if these matter perf wise
            //CanonicalHalf, 
            //CanonicalQuadrant,
            //CanonicalOctant,
        }

        struct SliceyCacheKey
        {
            public SliceyCacheStage Stage;
            public SliceyConfig Config;

            public override int GetHashCode()
            {
                return (Stage, Config).GetHashCode();
            }
        }

        Dictionary<SliceyCacheKey, SliceyCacheValue> _cache = new();

#if UNITY_EDITOR
        void OnEnable()
        {
            AssemblyReloadEvents.afterAssemblyReload += OnAfterAssemblyReload;
        }

        void OnDisable()
        {
            AssemblyReloadEvents.afterAssemblyReload -= OnAfterAssemblyReload;
        }

        void OnAfterAssemblyReload()
        {
            _cache.Clear();
            if (LogMisses || LogHits) Debug.Log($"{nameof(SliceyCache)} - Clearing due to Assembly Reload");
        }
#endif

        public (Mesh, SliceyShaderParameters) Get(SliceyConfig config)
        {
            // quantize the quality to points that actually change the number of segments.
            config.Quality = SliceyMeshBuilder.QualityForSegments(90f, SliceyMeshBuilder.SegmentsForAngle(90f, config.Quality));
            var key = new SliceyCacheKey()
            {
                Stage = SliceyCacheStage.CompleteResult,
                Config = config,
            };

            // start with the best case scenario - it's already in the cache...
            if (_cache.TryGetValue(key, out var complete))
            {
                UpdateValue(ref key, ref complete);
                if (LogHits) Debug.Log($"{nameof(SliceyCache)} - Cache hit");
                return (complete.Mesh, new SliceyShaderParameters() { Type = SliceyShaderType.None });
            }
            else
            {
                // next, try to get a canonical mesh and then slice it to the right size
                var canonicalKey = key;
                var canonicalBuilder = new SliceyMeshBuilder();
                canonicalKey.Stage = SliceyCacheStage.CanonicalComplete;
                if (_cache.TryGetValue(canonicalKey, out var canonical))
                {
                    UpdateValue(ref canonicalKey, ref canonical);
                    canonicalBuilder = canonical.Builder;

                    if (LogMisses) Debug.Log($"{nameof(SliceyCache)} - Cache miss, using cached canonical");
                }
                else // build the canonical mesh for the first time
                {
                    canonicalBuilder = config.Type switch
                    {
                        SliceyMesh.SliceyMeshType.CuboidHard => SliceyMeshGenerator.BuildCanonicalCuboidHard(),
                        SliceyMesh.SliceyMeshType.CuboidCylindrical => SliceyMeshGenerator.BuildCanonicalCuboidCylindrical(config.Quality),
                        SliceyMesh.SliceyMeshType.CuboidSpherical => SliceyMeshGenerator.BuildCanonicalCuboidSpherical(config.Quality),
                    };
                    _cache[canonicalKey] = new SliceyCacheValue()
                    {
                        LastAccessedCount = 1,
                        LastAccessedFrame = Time.frameCount,
                        Builder = canonicalBuilder
                    };
                    if (LogMisses) Debug.Log($"{nameof(SliceyCache)} - Cache miss, generating canonical mesh");
                }

                var completeBuilder = canonicalBuilder.Clone();

                // TODO Right now we just bake to Complete synchronously.  In the future we can do this part in the background
                // while using shader based slicing while we wait.
                if (config.Type == SliceyMesh.SliceyMeshType.CuboidCylindrical ||
                    config.Type == SliceyMesh.SliceyMeshType.CuboidSpherical)
                {
                    var sourceInside = Vector3.one * 0.25f;
                    var sourceOutside = Vector3.one * 0.5f;
                    var targetOutside = config.Size * 0.5f;
                    var targetInside = Vector3.Max(Vector3.zero, targetOutside - Vector3.one * config.Radii.x);
                    if (config.Type == SliceyMesh.SliceyMeshType.CuboidCylindrical)
                    {
                        sourceInside.z = 1f;
                        targetInside.z = config.Size.z;
                    }
                    if (LogSlicing) Debug.Log($"{nameof(SliceyCache)} - Slice256");
                    completeBuilder.Slice256(sourceInside, sourceOutside, targetInside, targetOutside);
                }

                var complete2 = new SliceyCacheValue()
                {
                    LastAccessedCount = 1,
                    LastAccessedFrame = Time.frameCount,
                    Mesh = completeBuilder.End()
                };
                _cache[key] = complete2;

                return (complete2.Mesh, new SliceyShaderParameters() { Type = SliceyShaderType.None });
            }
        }

        void UpdateValue(ref SliceyCacheKey key, ref SliceyCacheValue value)
        {
            var lastFrame = value.LastAccessedFrame;
            var frame = Time.frameCount;
            if (lastFrame != frame)
            {
                value.LastAccessedFrame = frame;
                value.LastAccessedCount = 1;
            }
            else
            {
                value.LastAccessedCount++;
            }
            _cache[key] = value;
        }
    }
}
