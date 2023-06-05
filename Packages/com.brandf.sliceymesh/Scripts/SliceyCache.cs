using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace SliceyMesh
{
    public struct SliceyConfig : IEquatable<SliceyConfig>
    {
        public SliceyMesh.SliceyMeshType Type;
        public Vector3 Size;
        public Vector3 Offset;
        public Vector4 Radii;
        public float Quality;

        public bool Equals(SliceyConfig other)
        {
            return (Type, Size, Offset, Radii, Quality) == (other.Type, other.Size, other.Offset, other.Radii, other.Quality);
        }

        public static bool operator==(SliceyConfig c1, SliceyConfig c2) => c1.Equals(c2);
        public static bool operator!=(SliceyConfig c1, SliceyConfig c2) => !c1.Equals(c2);

        public override int GetHashCode()
        {
            return (Type, Size, Offset, Radii, Quality).GetHashCode();
        }

    }

    public struct SliceyCacheValue
    {
        public int LastAccessedFrame; // Time.frameCount the last time this was accessed
        public int LastAccessedCount; // number of times this was requested on the LastAccessedFrame
        public SliceyMeshBuilder Builder; // for non-complete values
        public Mesh Mesh; // for complete results
    }

    public enum SliceyShaderType
    {
        None,
        Rect9Slice,
        Rect16Slice,
        Cublic27Slice,
        Cublic256Slice,
    }

    
    [Flags]
    public enum SliceyMaterialFlags
    {
        ShaderSlicingNotSupported = 0,
        SupportsRect9Slice        = 1 << 0,
        SupportsRect16Slice       = 1 << 1,
        SupportsCublic27Slice     = 1 << 2,
        SupportsCublic256Slice    = 1 << 3,
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
        [Flags]
        public enum SliceyCacheLogFlags
        { 
            None    = 0,
            Misses  = 1 << 0,
            Hits    = 1 << 1,
            Slicing = 1 << 2,
        }

        public SliceyCacheLogFlags LogFlags;

        enum SliceyCacheStage
        {
            CompleteResult, // includes slicing to specific size, offset, radii
            Canonical,
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

        public static SliceyCache DefaultCache { get; internal set; }

        public int Count => _cache.Count;

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
            if (LogFlags != SliceyCacheLogFlags.None) Debug.Log($"{nameof(SliceyCache)} - Clearing due to Assembly Reload");
        }
#endif
        
        public void Clear()
        {
            _cache.Clear();
        }

        public (Mesh, SliceyShaderParameters) Get(SliceyConfig config, SliceyMaterialFlags materialFlags)
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
                if (LogFlags.HasFlag(SliceyCacheLogFlags.Hits)) Debug.Log($"{nameof(SliceyCache)} - Cache hit");
                return (complete.Mesh, new SliceyShaderParameters() { Type = SliceyShaderType.None });
            }
            else
            {
                // next, try to get a canonical mesh and then slice it to the right size
                var canonicalKey = key;
                var canonicalBuilder = new SliceyMeshBuilder();
                canonicalKey.Stage = SliceyCacheStage.Canonical;
                canonicalKey.Config.Size = Vector3.one;
                canonicalKey.Config.Offset = Vector3.zero;
                canonicalKey.Config.Radii = new Vector4(0.25f, 0f ,0f);
                if (_cache.TryGetValue(canonicalKey, out var canonical))
                {
                    UpdateValue(ref canonicalKey, ref canonical);
                    canonicalBuilder = canonical.Builder;

                    if (LogFlags.HasFlag(SliceyCacheLogFlags.Misses)) Debug.Log($"{nameof(SliceyCache)} - Cache miss, using cached canonical");
                }
                else // build the canonical mesh for the first time
                {
                    canonicalBuilder = config.Type switch
                    {
                        SliceyMesh.SliceyMeshType.CuboidHard        => SliceyCanonicalGenerator.HardCube(),
                        SliceyMesh.SliceyMeshType.CuboidCylindrical => SliceyCanonicalGenerator.CylindricalCube(config.Quality),
                        SliceyMesh.SliceyMeshType.CuboidSpherical   => SliceyCanonicalGenerator.SphericalCube(config.Quality),
                    };
                    _cache[canonicalKey] = new SliceyCacheValue()
                    {
                        LastAccessedCount = 1,
                        LastAccessedFrame = Time.frameCount,
                        Builder = canonicalBuilder
                    };
                    if (LogFlags.HasFlag(SliceyCacheLogFlags.Misses)) Debug.Log($"{nameof(SliceyCache)} - Cache miss, generating canonical mesh");
                }

                // Now that we have a canonical mesh (typically unit sized), we need to slice it to the right size, radius, etc.
                // What we need to do next is a function of:
                //  a) How the requested config differs from the canonical mesh (if at all)
                //  b) What type of shader slicing is supported by the caller (if any)
                //  c) Whatever shader vs. cpu heuristics the cache may have in terms of memory / draw call caps or even synchronous vs. async Mesh generation queuing
                var completeBuilder = canonicalBuilder.Clone();

                // TODO: At this time only a small subset of the above is supported.
                var differsFromCanonical = key.Config != canonicalKey.Config;
                if (differsFromCanonical)
                {
                    var forceSynchronousMeshGeneration = true; // TODO: not always
                    if (materialFlags == SliceyMaterialFlags.ShaderSlicingNotSupported || forceSynchronousMeshGeneration)
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
                        if (LogFlags.HasFlag(SliceyCacheLogFlags.Slicing)) Debug.Log($"{nameof(SliceyCache)} - SliceMesh256");
                        // TODO, sometimes we can use the cheaper SliceMesh27, SliceMesh16, or SliceMesh9
                        completeBuilder.SliceMesh256(sourceInside, sourceOutside, targetInside, targetOutside, config.Offset);
                    }
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
