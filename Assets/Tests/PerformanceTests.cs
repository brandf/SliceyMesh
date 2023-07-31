using UnityEngine;
using Unity.PerformanceTesting;
using NUnit.Framework;

namespace SliceyMesh
{

    public class PerformanceTests : TestBase
    {
        [Test, Performance]
        public void MeshTypeNoCache([Values(SliceyMeshType.Rect,
                                            SliceyMeshType.Cube, 
                                            SliceyMeshType.Cylinder)] SliceyMeshType type)
        {
            var config = new SliceyConfig()
            {
                Type = type,
                Size = new Vector3(1, 1, 1),
                Radii = new Vector2(0.25f, 0.25f),
                Quality = new Vector2(1, 1),
            };

            Measure.Method(() =>
            {
                Cache.Get(config, SliceyMaterialFlags.ShaderSlicingNotSupported);
            }).
            SetUp(() =>
            {
                Cache.Clear();
            }).
            WarmupCount(5).
            IterationsPerMeasurement(1).
            MeasurementCount(100).
            Run();
        }

        [Test, Performance]
        public void CacheMiss([Values(0.0f, 0.5f, 1.0f, 2.0f, 3.0f)] float quality)
        {
            var config = new SliceyConfig()
            {
                Type = SliceyMeshType.Cube,
                SubType = (int)SliceyMeshCubeSubType.RoundSidesFillet,
                Size = new Vector3(1, 1, 1),
                Radii = new Vector2(0.25f, 0.25f),
                Quality = Vector2.one * quality
            };

            Measure.Method(() =>
            {
                Cache.Get(config, SliceyMaterialFlags.ShaderSlicingNotSupported);
            }).
            SetUp(() =>
            {
                Cache.Clear();
            }).
            WarmupCount(5).
            IterationsPerMeasurement(1).
            MeasurementCount(100).
            Run();
        }

        [Test, Performance]
        public void CacheHit([Values(0.0f, 0.5f, 1.0f, 2.0f, 3.0f)] float quality)
        {
            var config = new SliceyConfig()
            {
                Type = SliceyMeshType.Cube,
                SubType = (int)SliceyMeshCubeSubType.RoundSidesFillet,
                Size = new Vector3(1, 1, 1),
                Radii = new Vector2(0.25f, 0.25f),
                Quality = Vector2.one * quality
            };
            Cache.Get(config, SliceyMaterialFlags.ShaderSlicingNotSupported);

            Measure.Method(() =>
            {
                Cache.Get(config, SliceyMaterialFlags.ShaderSlicingNotSupported);
            }).
            WarmupCount(5).
            IterationsPerMeasurement(1).
            MeasurementCount(100).
            Run();
        }

        [Test, Performance]
        public void CanonicalCacheHit([Values(0.0f, 0.5f, 1.0f, 2.0f, 3.0f)] float quality)
        {
            var configA = new SliceyConfig()
            {
                Type = SliceyMeshType.Cube,
                SubType = (int)SliceyMeshCubeSubType.RoundSidesFillet,
                Size = new Vector3(1, 1, 1),
                Radii = new Vector2(0.25f, 0.25f),
                Quality = Vector2.one * quality
            };

            //Different size but should share canonical with configA
            var configB = configA;
            configB.Size = new Vector3(2, 2, 2);

            Measure.Method(() =>
            {
                Cache.Get(configB, SliceyMaterialFlags.ShaderSlicingNotSupported);
            }).
            SetUp(() =>
            {
                Cache.Clear();
                Cache.Get(configA, SliceyMaterialFlags.ShaderSlicingNotSupported);
            }).
            WarmupCount(5).
            IterationsPerMeasurement(1).
            MeasurementCount(100).
            Run();
        }
    }
}
