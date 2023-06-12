using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SliceyMesh
{
    //[CreateAssetMenu]
    public class EdtiorResources : ScriptableObject
     {
        public Texture2D CuboidHard;
        public Texture2D CuboidCylindrical;
        public Texture2D CuboidSpherical;
        public Texture2D RectHard;
        public Texture2D RectRound;

        static EdtiorResources instance;
        public static EdtiorResources Instance
        {
            get
            {
                if (instance == null)
                    instance = Resources.Load<EdtiorResources>("Slicey Mesh Editor Resources");
                return instance;
            }
        }
    }
}
