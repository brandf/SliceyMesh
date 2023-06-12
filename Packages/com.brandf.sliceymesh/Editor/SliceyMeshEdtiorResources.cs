using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SliceyMesh
{
    [CreateAssetMenu(fileName = "Slicey Mesh Editor Resources", menuName = "Slicey Mesh/Editor Resources")]
    public class SliceyMeshEdtiorResources : ScriptableObject
     {
        public Texture2D CuboidHard;
        public Texture2D CuboidCylindrical;
        public Texture2D CuboidSpherical;
        public Texture2D RectHard;
        public Texture2D RectRound;

        static SliceyMeshEdtiorResources instance;
        public static SliceyMeshEdtiorResources Instance
        {
            get
            {
                if (instance == null)
                    instance = Resources.Load<SliceyMeshEdtiorResources>("Slicey Mesh Editor Resources");
                return instance;
            }
        }
    }
}
