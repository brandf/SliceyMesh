using NUnit.Framework;
using UnityEngine;

namespace SliceyMesh
{
    public class TestBase
    {
        public SliceyCache Cache;

        [SetUp]
        public virtual void SetUp()
        {
            GameObject cacheObj = new GameObject("SliceyCache");
            Cache = cacheObj.AddComponent<SliceyCache>();
            Cache.LogFlags = SliceyCache.SliceyCacheLogFlags.None;
        }

        [TearDown]
        public virtual void TearDown()
        {
            Object.DestroyImmediate(Cache.gameObject);
        }
    }
}
