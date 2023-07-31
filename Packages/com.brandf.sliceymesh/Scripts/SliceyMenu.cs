
using UnityEditor;
using UnityEngine;

namespace SliceyMesh
{
#if UNITY_EDITOR
    public class SliceyMenu : ScriptableObject
    {
        [MenuItem("GameObject / SliceyMesh / Rect / Hard")]
        static void CreateRectHard()
        {
            var sm = CreateSliceyMesh();
            sm.Type = SliceyMeshType.Rect;
            sm.RectSubType = SliceyMeshRectSubType.Hard;
            Selection.activeObject = sm;
        }

        [MenuItem("GameObject / SliceyMesh / Rect / Round")]
        static void CreateRectRoundEdges()
        {
            var sm = CreateSliceyMesh();
            sm.Type = SliceyMeshType.Rect;
            sm.RectSubType = SliceyMeshRectSubType.Round;
            Selection.activeObject = sm;
        }

        [MenuItem("GameObject / SliceyMesh / Cube / Hard")]
        static void CreateCubeHard()
        {
            var sm = CreateSliceyMesh();
            sm.Type = SliceyMeshType.Cube;
            sm.CubeSubType = SliceyMeshCubeSubType.Hard;
            Selection.activeObject = sm;
        }

        [MenuItem("GameObject / SliceyMesh / Cube / RoundEdges")]
        static void CreateCubeRoundEdges()
        {
            var sm = CreateSliceyMesh();
            sm.Type = SliceyMeshType.Cube;
            sm.CubeSubType = SliceyMeshCubeSubType.RoundEdges;
            Selection.activeObject = sm;
        }

        [MenuItem("GameObject / SliceyMesh / Cube / RoundSides")]
        static void CreateCubeRoundSides()
        {
            var sm = CreateSliceyMesh();
            sm.Type = SliceyMeshType.Cube;
            sm.CubeSubType = SliceyMeshCubeSubType.RoundSides;
            Selection.activeObject = sm;
        }

        [MenuItem("GameObject / SliceyMesh / Cube / RoundSidesFillet")]
        static void CreateCubeRoundSidesFillet()
        {
            var sm = CreateSliceyMesh();
            sm.Type = SliceyMeshType.Cube;
            sm.CubeSubType = SliceyMeshCubeSubType.RoundSidesFillet;
            Selection.activeObject = sm;
        }

        [MenuItem("GameObject / SliceyMesh / Cylinder / Hard")]
        static void CreateCylinderHard()
        {
            var sm = CreateSliceyMesh();
            sm.Type = SliceyMeshType.Cylinder;
            sm.CylinderSubType = SliceyMeshCylinderSubType.Hard;
            Selection.activeObject = sm;
        }

        [MenuItem("GameObject / SliceyMesh / Cylinder / RoundEdges")]
        static void CreateCylinderRoundEdges()
        {
            var sm = CreateSliceyMesh();
            sm.Type = SliceyMeshType.Cylinder;
            sm.CylinderSubType = SliceyMeshCylinderSubType.RoundEdges;
            Selection.activeObject = sm;
        }

        static SliceyMesh CreateSliceyMesh()
        {
            var go = new GameObject("Slicey Mesh");
            return go.AddComponent<SliceyMesh>();
        }
    }
#endif
}
