using UnityEngine;
using NUnit.Framework;

namespace SliceyMesh
{
    public class CacheTests : TestBase
    {

        [Test]
        public void CanonicalIsSharedBetweenMatchingConfigs()
        {
            var configBase = new SliceyConfig()
            {
                Type = SliceyMesh.SliceyMeshType.CuboidSpherical,
                Size = new Vector3(1, 1, 1),
                Radii = new Vector4(0.25f, 0f, 0f, 0f),
                Quality = 1f
            };

            var configA = configBase;
            var configB = configBase;
            var configC = configBase;

            configA.Size = new Vector3(2, 1, 1);
            configB.Size = new Vector3(3, 1, 1);
            configC.Size = new Vector3(4, 1, 1);

            Cache.Get(configA);
            Cache.Get(configB);
            Cache.Get(configC);

            //1 per config plus 1 for the shared canonical version
            Assert.That(Cache.Count, Is.EqualTo(4));
        }
    }
}
