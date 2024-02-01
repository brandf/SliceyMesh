using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using static SliceyMesh.SliceyCache;

namespace SliceyMesh
{
    public struct SliceyRectOutlineConfig : IEquatable<SliceyRectOutlineConfig>
    {
        public Vector3 Size;
        public float Thickness; 
        public float Radius;
        public float Quality;

        public bool Equals(SliceyRectOutlineConfig other)
        {
            return (Size, Thickness, Radius, Quality) == (other.Size, other.Thickness, other.Radius, other.Quality);
        }

        public override bool Equals(object obj)
        {
            if (obj is SliceyRectOutlineConfig other)
                return Equals(other);
            return false;
        }

        public static bool operator ==(SliceyRectOutlineConfig c1, SliceyRectOutlineConfig c2) => c1.Equals(c2);
        public static bool operator !=(SliceyRectOutlineConfig c1, SliceyRectOutlineConfig c2) => !c1.Equals(c2);

        public override int GetHashCode()
        {
            return (Size, Thickness, Radius, Quality).GetHashCode();
        }
    }

    public class SliceyRectOutlineCache : MonoBehaviour
    {
        public bool DisableCache = false;
        public SliceyCacheLogFlags LogFlags;
        SliceyCacheStats Stats;
        Dictionary<SliceyRectOutlineConfig, SliceyCacheValue> _cache = new();

        public static SliceyRectOutlineCache DefaultCache { get; internal set; }

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
            if (LogFlags != SliceyCacheLogFlags.None) Debug.Log($"{nameof(SliceyRectOutlineCache)} - Clearing due to Assembly Reload");
        }
#endif
        public void Collect()
        {
            List<SliceyRectOutlineConfig> keysToRemove = new();
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
        public Mesh Get(SliceyRectOutlineConfig config)
        {
            // quantize the quality to points that actually change the number of segments.
            if (config.Quality != 0f)
                config.Quality = SliceyMeshBuilder.QualityForSegments(90f, SliceyMeshBuilder.SegmentsForAngle(90f, config.Quality));

            // start with the best case scenario - it's already in the cache...
            if (!DisableCache && _cache.TryGetValue(config, out var complete))
            {
                UpdateValue(ref config, ref complete);
                Stats.hits++;
                if (LogFlags.HasFlag(SliceyCacheLogFlags.Hits)) Debug.Log($"{nameof(SliceyRectOutlineCache)} - Cache hit");
                return complete.Mesh;
            }
            else
            {
                Stats.misses++;
                // next, try to get a canonical mesh and then slice it to the right size
                var canonicalConfig = config;
                var cornerSize = Vector2.one * Mathf.Max(config.Thickness, config.Radius);
                var canonicalSize = Vector2.one * Mathf.Ceil(cornerSize.x * 2f + 0.01f);
                var canonicalBuilder = new SliceyMeshBuilder();
                canonicalConfig.Size = canonicalSize;

                if (!DisableCache && _cache.TryGetValue(canonicalConfig, out var canonical))
                {
                    UpdateValue(ref canonicalConfig, ref canonical);
                    canonicalBuilder = canonical.Builder;
                    if (LogFlags.HasFlag(SliceyCacheLogFlags.Misses)) Debug.Log($"{nameof(SliceyRectOutlineCache)} - Cache miss, using cached canonical");
                }
                else // build the canonical mesh for the first time
                {
                    canonicalBuilder = SliceyCanonicalGenerator.RectOutline(canonicalSize.x, config.Radius, config.Thickness, config.Quality);

                    if (!DisableCache)
                    {
                        _cache[canonicalConfig] = new SliceyCacheValue()
                        {
                            LastAccessedCount = 1,
                            LastAccessedFrame = Time.frameCount,
                            Builder = canonicalBuilder
                        };
                    }
                    Stats.generates++;
                    if (LogFlags.HasFlag(SliceyCacheLogFlags.Generates)) Debug.Log($"{nameof(SliceyRectOutlineCache)} - Cache miss, generating canonical mesh");
                }

                var completeBuilder = canonicalBuilder.Clone(); // TODO add face mode

                var differsFromCanonical = config != canonicalConfig;
                if (differsFromCanonical)
                {
                    Stats.slicesCPU++;
                    if (LogFlags.HasFlag(SliceyCacheLogFlags.Slicing)) Debug.Log($"{nameof(SliceyRectOutlineCache)} - Slicing Mesh on CPU");

                    var sourceInside2d = canonicalSize * 0.5f - cornerSize;
                    var sourceOutside2d = canonicalSize * 0.5f;
                    var targetOutside2d = (Vector2)config.Size * 0.5f;
                    var targetInside2d = Vector2.Max(Vector2.zero, targetOutside2d - cornerSize);
                    /*
                    Debug.Log($"canonicalSize: {canonicalSize}");
                    Debug.Log($"cornerSize: {cornerSize}");
                    Debug.Log($"sourceInside2d: {sourceInside2d}");
                    Debug.Log($"sourceOutside2d: {sourceOutside2d}");
                    Debug.Log($"targetOutside2d: {targetOutside2d}");
                    Debug.Log($"targetInside2d: {targetInside2d}");
                    */
                    completeBuilder.SliceRect(sourceInside2d, sourceOutside2d, targetInside2d, targetOutside2d, Pose.identity, 1f);
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
                    _cache[config] = complete2;
                }
                Stats.count++;

                return mesh;
            }
        }

        void UpdateValue(ref SliceyRectOutlineConfig key, ref SliceyCacheValue value)
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
        [CustomEditor(typeof(SliceyRectOutlineCache))]
        public class Editor : UnityEditor.Editor
        {
            protected SliceyRectOutlineCache Target => target as SliceyRectOutlineCache;

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
