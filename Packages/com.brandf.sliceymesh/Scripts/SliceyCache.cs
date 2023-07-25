using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace SliceyMesh
{
    public enum SliceyMeshType
    {
        Rect,
        Cube,
        Cylinder,
    }

    public enum SliceyMeshRectSubType
    {
        Hard,
        Round,
    }

    public enum SliceyMeshCubeSubType
    {
        Hard,
        RoundSides,
        RoundEdges,
        RoundSidesFillet,
    }

    public enum SliceyMeshCylinderSubType
    {
        Hard,
        RoundEdges,
    }

    public enum SliceyFaceMode
    { 
        Outside,
        Inside,
        DoubleSided,
    }

    public enum SliceyMeshPortion
    {
        Full,
        Half,
        Quadrant,
        Octant,
    }

    public struct SliceyConfig : IEquatable<SliceyConfig>
    {
        public SliceyMeshType Type;
        public int SubType; // cast based on value of Type
        public SliceyFaceMode FaceMode;
        public SliceyMeshPortion Portion;
        public bool PortionClosed;
        public Pose Pose;
        public Vector3 Size;
        public Vector2 Radii;
        public Vector2 Quality;

        public bool Equals(SliceyConfig other)
        {
            // note the nested tuples - if you get too many in a single tuple the cache is broken for some reason
            return ((Type, SubType), FaceMode, (Portion, PortionClosed), Pose, Size, Radii, Quality) == 
                   ((other.Type, other.SubType), other.FaceMode, (other.Portion, other.PortionClosed), other.Pose, other.Size, other.Radii, other.Quality);
        }

        public override bool Equals(object obj)
        {
            if (obj is SliceyConfig other)
                return Equals(other);
            return false;
        }

        public static bool operator==(SliceyConfig c1, SliceyConfig c2) => c1.Equals(c2);
        public static bool operator!=(SliceyConfig c1, SliceyConfig c2) => !c1.Equals(c2);

        public override int GetHashCode()
        {
            // note the nested tuples - if you get too many in a single tuple the cache is broken for some reason
            return ((Type, SubType), FaceMode, (Portion, PortionClosed), Pose, Size, Radii, Quality).GetHashCode();
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
        CylinderSlice,
        CylinderSlice2,
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
        public bool DisableCache = false;
        [Flags]
        public enum SliceyCacheLogFlags
        {
            None = 0,
            Misses = 1 << 0,
            Generates = 1 << 1,
            Hits = 1 << 2,
            Slicing = 1 << 3,
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

        struct SliceyCacheKey : IEquatable<SliceyCacheKey>
        {
            public SliceyCacheStage Stage;
            public SliceyConfig Config;

            public bool Equals(SliceyCacheKey other)
            {
                return (Stage, Config) == (Stage, Config);
            }

            public override bool Equals(object obj)
            {
                if (obj is SliceyCacheKey other)
                    return Equals(other);
                return false;
            }

            public static bool operator ==(SliceyCacheKey k1, SliceyCacheKey k2) => k1.Equals(k2);
            public static bool operator !=(SliceyCacheKey k1, SliceyCacheKey k2) => !k1.Equals(k2);

            public override int GetHashCode()
            {
                return (Stage, Config).GetHashCode();
            }
        }

        [Serializable]
        struct SliceyCacheStats
        {
            public int count;
            public int hits;
            public int misses;
            public int generates;
            public int slicesCPU;
        }

        SliceyCacheStats Stats;

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
            Clear();
            if (LogFlags != SliceyCacheLogFlags.None) Debug.Log($"{nameof(SliceyCache)} - Clearing due to Assembly Reload");
        }
#endif

        public void Collect()
        {
            List<SliceyCacheKey> keysToRemove = new();
            var currentFrame = Time.frameCount;
            foreach (var kvp in _cache)
            {
                if (kvp.Value.LastAccessedFrame < currentFrame - 3)
                    keysToRemove.Add(kvp.Key);
            }

            foreach (var key in keysToRemove)
                _cache.Remove(key);

            Stats.count = _cache.Count;
        }
        
        public void Clear()
        {
            _cache.Clear();
            Stats = new SliceyCacheStats();
        }

        public (Mesh, SliceyShaderParameters) Get(SliceyConfig config, SliceyMaterialFlags materialFlags)
        {
            // quantize the quality to points that actually change the number of segments.
            if (config.Quality.x != 0f)
                config.Quality.x = SliceyMeshBuilder.QualityForSegments(90f, SliceyMeshBuilder.SegmentsForAngle(90f, config.Quality.x));
            if (config.Quality.y != 0f)
                config.Quality.y = SliceyMeshBuilder.QualityForSegments(90f, SliceyMeshBuilder.SegmentsForAngle(90f, config.Quality.y));

            var key = new SliceyCacheKey()
            {
                Stage = SliceyCacheStage.CompleteResult,
                Config = config,
            };

            // start with the best case scenario - it's already in the cache...
            if (!DisableCache && _cache.TryGetValue(key, out var complete))
            {
                UpdateValue(ref key, ref complete);
                Stats.hits++;
                if (LogFlags.HasFlag(SliceyCacheLogFlags.Hits)) Debug.Log($"{nameof(SliceyCache)} - Cache hit");
                return (complete.Mesh, new SliceyShaderParameters() { Type = SliceyShaderType.None });
            }
            else
            {
                Stats.misses++;
                // next, try to get a canonical mesh and then slice it to the right size
                var canonicalKey = key;
                var canonicalBuilder = new SliceyMeshBuilder();
                canonicalKey.Stage = SliceyCacheStage.Canonical;
                canonicalKey.Config.FaceMode = SliceyFaceMode.Outside;
                canonicalKey.Config.Size = Vector3.one;
                canonicalKey.Config.Pose = Pose.identity;

                var effectiveDesiredFilletRadius = Mathf.Min(key.Config.Radii.y, key.Config.Radii.x); // fillet can't be bigger than primary radius
                var canonicalFilletRadius = key.Config.Radii.y;
                // cube slicing can't handle multiple different radii, so fillet radius need to be handled specially.
                // the canonical shape typically has a canonnical radius, however the fillet radius is variable so that when the primary radius
                // is streched during slicing, the fillet radius ends up being what we want.  Otherwise we would need much more complicated slicing.
                // This does mean that changing the radii of these shapes is more expensive than others, but changing the size is still cheaper.
                if (config.Type == SliceyMeshType.Cube && (SliceyMeshCubeSubType)config.SubType == SliceyMeshCubeSubType.RoundSidesFillet)
                {
                    canonicalFilletRadius = effectiveDesiredFilletRadius;
                    if (key.Config.Radii.x > 0.00001f)
                    {
                        canonicalFilletRadius *= 0.25f / key.Config.Radii.x;
                    }
                }
                canonicalKey.Config.Radii = new Vector2(0.25f, canonicalFilletRadius);
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
                        SliceyMeshType.Rect => (SliceyMeshRectSubType)config.SubType switch
                        {
                            SliceyMeshRectSubType.Hard  => SliceyCanonicalGenerator.RectHard(config.Portion),
                            SliceyMeshRectSubType.Round => SliceyCanonicalGenerator.RectRound(config.Portion, config.Quality.x),
                        },
                        SliceyMeshType.Cube => (SliceyMeshCubeSubType)config.SubType switch
                        {
                            SliceyMeshCubeSubType.Hard                => SliceyCanonicalGenerator.CubeHard(config.Portion, config.PortionClosed),
                            SliceyMeshCubeSubType.RoundSides          => SliceyCanonicalGenerator.CubeRoundSides(config.Portion, config.PortionClosed, config.Quality.x),
                            SliceyMeshCubeSubType.RoundEdges          => SliceyCanonicalGenerator.CubeRoundEdges(config.Portion, config.PortionClosed, config.Quality.x),
                            SliceyMeshCubeSubType.RoundSidesFillet    => SliceyCanonicalGenerator.CubeRoundSidesFillet(config.Portion, config.PortionClosed, canonicalFilletRadius, config.Quality.x, config.Quality.y),
                        },
                        SliceyMeshType.Cylinder => (SliceyMeshCylinderSubType)config.SubType switch
                        {
                            SliceyMeshCylinderSubType.Hard       => SliceyCanonicalGenerator.CylinderHard(config.Portion, config.PortionClosed, config.Quality.x),
                            SliceyMeshCylinderSubType.RoundEdges => SliceyCanonicalGenerator.CylinderRoundEdges(config.Portion, config.PortionClosed, config.Quality.x, config.Quality.y),
                        }
                    };

                    if (!DisableCache)
                    {
                        _cache[canonicalKey] = new SliceyCacheValue()
                        {
                            LastAccessedCount = 1,
                            LastAccessedFrame = Time.frameCount,
                            Builder = canonicalBuilder
                        };
                    }
                    Stats.generates++;
                    if (LogFlags.HasFlag(SliceyCacheLogFlags.Generates)) Debug.Log($"{nameof(SliceyCache)} - Cache miss, generating canonical mesh");
                }

                // Now that we have a canonical mesh (typically unit sized), we need to slice it to the right size, radius, etc.
                // What we need to do next is a function of:
                //  a) How the requested config differs from the canonical mesh (if at all)
                //  b) What type of shader slicing is supported by the caller (if any)
                //  c) Whatever shader vs. cpu heuristics the cache may have in terms of memory / draw call caps or even synchronous vs. async Mesh generation queuing
                var completeBuilder = CloneWithFaceMode(canonicalBuilder, config.FaceMode);

                // TODO: At this time only a small subset of the above is supported.
                var differsFromCanonical = key.Config != canonicalKey.Config;
                if (differsFromCanonical)
                {
                    var forceSynchronousMeshGeneration = true; // TODO: not always
                    if (materialFlags == SliceyMaterialFlags.ShaderSlicingNotSupported || forceSynchronousMeshGeneration)
                    {
                        Stats.slicesCPU++;
                        if (LogFlags.HasFlag(SliceyCacheLogFlags.Slicing)) Debug.Log($"{nameof(SliceyCache)} - Slicing Mesh on CPU");


                        if (config.Type == SliceyMeshType.Rect)
                        {
                            var sourceInside2d = Vector2.one * 0.25f;
                            var sourceOutside2d = Vector2.one * 0.5f;
                            var targetOutside2d = (Vector2)config.Size * 0.5f;
                            var targetInside2d = Vector2.Max(Vector2.zero, targetOutside2d - Vector2.one * config.Radii.x);
                            completeBuilder.SliceRect(sourceInside2d, sourceOutside2d, targetInside2d, targetOutside2d, new Pose(config.Pose.position + Vector3.back * (config.Size.z * 0.5f), config.Pose.rotation));
                        }
                        else if (config.Type == SliceyMeshType.Cylinder)
                        {
                            var outsideRadiusSource = 0.5f;
                            var edgeRadiusSource = 0.25f;
                            var insideRadiusSource = outsideRadiusSource - edgeRadiusSource;
                            var outsideHalfDepthSource = 0.5f;
                            var insideHalfDepthSource = outsideRadiusSource - edgeRadiusSource;

                            var outsideRadiusTarget = config.Radii.x;
                            var edgeRadiusTarget = config.Radii.y;
                            var insideRadiusTarget = Mathf.Max(0, outsideRadiusTarget - edgeRadiusTarget);
                            var outsideHalfDepthTarget = config.Size.z * 0.5f;
                            var insideHalfDepthTarget = Mathf.Max(0, outsideHalfDepthTarget - edgeRadiusTarget);

                            completeBuilder.SliceCylinder(new Vector2(insideRadiusSource, insideHalfDepthSource), new Vector2(outsideRadiusSource, outsideHalfDepthSource),
                                                          new Vector2(insideRadiusTarget, insideHalfDepthTarget), new Vector2(outsideRadiusTarget, outsideHalfDepthTarget), config.Pose);
                        }
                        else // Cube
                        {
                            var sourceInside = Vector3.one * 0.25f;
                            var sourceOutside = Vector3.one * 0.5f;
                            var targetOutside = config.Size * 0.5f;
                            var targetInside = Vector3.Max(Vector3.zero, targetOutside - Vector3.one * config.Radii.x);
                            if (config.SubType == (int)SliceyMeshCubeSubType.RoundSides)
                            {

                                sourceInside.z = sourceOutside.z - 0.001f;
                                targetInside.z = targetOutside.z - 0.001f;
                            }
                            else if (config.SubType == (int)SliceyMeshCubeSubType.RoundEdges)
                            {
                            }
                            else if (config.SubType == (int)SliceyMeshCubeSubType.RoundSidesFillet)
                            {
                            }
                            completeBuilder.SliceCube(sourceInside, sourceOutside, targetInside, targetOutside, config.Pose);
                        }
                    }
                }

                var mesh = completeBuilder.End();
                if (!DisableCache)
                {
                    var complete2 = new SliceyCacheValue()
                    {
                        LastAccessedCount = 1,
                        LastAccessedFrame = Time.frameCount,
                        Mesh = mesh
                    };
                    _cache[key] = complete2;
                }
                Stats.count++;

                return (mesh, new SliceyShaderParameters() { Type = SliceyShaderType.None });
            }
        }

        SliceyMeshBuilder CloneWithFaceMode(SliceyMeshBuilder canonicalBuilder, SliceyFaceMode faceMode)
        {
            var builder = faceMode == SliceyFaceMode.DoubleSided ? canonicalBuilder.Clone(2) : canonicalBuilder.Clone();
            if (faceMode == SliceyFaceMode.DoubleSided)
            {
                var initial = builder.Cursor;
                builder.Copy(builder.Beginning, initial);
                builder.ReverseFaces(initial, builder.Cursor);
            }
            else if (faceMode == SliceyFaceMode.Inside)
            {
                builder.ReverseFaces(builder.Beginning, builder.Cursor);
            }
            return builder;
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

#if UNITY_EDITOR
        [CustomEditor(typeof(SliceyCache))]
        public class Editor : UnityEditor.Editor
        {
            protected SliceyCache Target => target as SliceyCache;

            public override bool RequiresConstantRepaint()
            {
                return true;
            }

            public override void OnInspectorGUI()
            {
                //base.OnInspectorGUI();
                EditorGUILayout.PropertyField(serializedObject.FindProperty("DisableCache"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("LogFlags"));
                GUI.enabled = false;
                var statsJson = JsonUtility.ToJson(Target.Stats, true);
                GUILayout.Label(statsJson);
                GUI.enabled = true;

                if (GUILayout.Button("Collect"))
                {
                    Target.Collect();
                }

                if (GUILayout.Button("Clear"))
                {
                    Target.Clear();
                }

                serializedObject.ApplyModifiedProperties();
            }
        }
#endif
    }
}
