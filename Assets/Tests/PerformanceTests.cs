using UnityEngine;
using Unity.PerformanceTesting;
using NUnit.Framework;

namespace SliceyMesh
{

    public class PerformanceTests : TestBase
    {

        [Test, Performance]
        public void CacheMiss([Values(0.25f, 0.5f, 0.75f, 1.0f)] float quality)
        {
            var config = new SliceyConfig()
            {
                Type = SliceyMesh.SliceyMeshType.CuboidSpherical,
                Size = new Vector3(1, 1, 1),
                Radii = new Vector4(0.25f, 0f, 0f, 0f),
                Quality = quality
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
        public void CacheHit([Values(0.25f, 0.5f, 0.75f, 1.0f)] float quality)
        {
            var config = new SliceyConfig()
            {
                Type = SliceyMesh.SliceyMeshType.CuboidSpherical,
                Size = new Vector3(1, 1, 1),
                Radii = new Vector4(0.25f, 0f, 0f, 0f),
                Quality = quality
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
        public void CanonicalCacheHit([Values(0.25f, 0.5f, 0.75f, 1.0f)] float quality)
        {
            var configA = new SliceyConfig()
            {
                Type = SliceyMesh.SliceyMeshType.CuboidSpherical,
                Size = new Vector3(1, 1, 1),
                Radii = new Vector4(0.25f, 0f, 0f, 0f),
                Quality = quality
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
