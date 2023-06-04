using UnityEngine;

namespace SliceyMesh
{
    public static class SliceyCanonicalGenerator
    {
        static public Vector3 BackToUp = Quaternion.AngleAxis(-45f, Vector3.right) * Vector3.back;
        static public Vector3 ForwardToUp = Quaternion.AngleAxis(45f, Vector3.right) * Vector3.forward;

        static public Vector3 BackToRight = Quaternion.AngleAxis(-45f, Vector3.down) * Vector3.back;
        static public Vector3 ForwardToRight = Quaternion.AngleAxis(45f, Vector3.down) * Vector3.forward;
        static public Vector3 ForwardToLeft = Quaternion.AngleAxis(45f, Vector3.up) * Vector3.forward;

        public static SliceyMeshBuilder HardCube()
        {
            var builder = SliceyMeshBuilder.Begin(SliceyMeshBuilder.SizeForQuad * 6);
            var h = 0.5f;
            var initial = builder.Cursor;
            builder.AddQuad(new Vector3(h, h, h),
                            new Vector3(-h, h, h),
                            new Vector3(-h, -h, h),
                            new Vector3(h, -h, h), Vector3.forward);                   // forward face
            var afterQuad = builder.Cursor;
            builder.CopyReflection(initial, afterQuad, ForwardToUp);                    // top face
            builder.CopyReflection(initial, afterQuad, ForwardToRight);                 // right face
            builder.CopyReflection(initial, builder.Cursor, Vector3.up, ForwardToLeft); // other 3 faces
            return builder;
        }

        public static SliceyMeshBuilder CylindricalCube(float quality)
        {
            var builder = SliceyMeshBuilder.Begin((SliceyMeshBuilder.SizeForQuad * 7 + SliceyMeshBuilder.SizeForFan(90f, quality) + SliceyMeshBuilder.SizeForCylinder(90f, quality) * 2) * 8);
            var h = 0.5f;
            var q = 0.25f;
            var initial = builder.Cursor;
            builder.AddQuad(new Vector3(0, 0, -h),
                            new Vector3(0, q, -h),
                            new Vector3(q, q, -h),
                            new Vector3(q, 0, -h), Vector3.back);                 // back face, middle center
            var afterQuad1 = builder.Cursor;
            builder.CopyReflection(initial, afterQuad1, new Plane(Vector3.down, q));    // back face, middle top
            var afterQuad2 = builder.Cursor;
            builder.CopyReflection(initial, afterQuad1, new Plane(Vector3.left, q));    // back face, middle right
            var beforeEdges = builder.Cursor;
            builder.CopyReflection(afterQuad1, afterQuad2, new Plane(BackToUp, new Vector3(0, h, -h)));    // top edge
            builder.CopyReflection(afterQuad2, beforeEdges, new Plane(BackToRight, new Vector3(h, 0, -h)));    // right edge
            var beforeCylinder = builder.Cursor;
            builder.AddCylinder(new Pose(new Vector3(q, q, -q), Quaternion.identity), q, 90f, quality, q); // rounded corner edge
            var afterEdges = builder.Cursor;
            builder.CopyReflection(beforeEdges, afterEdges, new Plane(Vector3.back, new Vector3(0, 0, -q)));    // q -> h edge thickness

            // add corner
            builder.AddFan(new Pose(new Vector3(q, q, -h), Quaternion.Euler(0, 0, 90)), q, -90f, quality); // rounded corner
            builder.XYZSymmetry();
            return builder;
        }

        public static SliceyMeshBuilder SphericalCube(float quality)
        {
            var builder = SliceyMeshBuilder.Begin(((SliceyMeshBuilder.SizeForQuad + SliceyMeshBuilder.SizeForCylinder(90f, quality)) * 3 +
                                                    SliceyMeshBuilder.SizeForCorner3(90f, quality)) * 8);
            var h = 0.5f;
            var q = 0.25f;
            var initial = builder.Cursor;
            builder.AddQuad(new Vector3(0, 0, -h),
                            new Vector3(0, q, -h),
                            new Vector3(q, q, -h),
                            new Vector3(q, 0, -h), Vector3.back);
            builder.AddCylinder(new Pose(new Vector3(0, q, -q), Quaternion.Euler(0, 90, 0)), q, 90f, quality, q);
            var after = builder.Cursor;
            builder.CopyRotation(initial, after, Quaternion.Euler(90, 90, 0));
            builder.CopyRotation(initial, after, Quaternion.Euler(0, -90, 90));
            builder.AddCorner3(new Pose(new Vector3(q, q, -h + q), Quaternion.identity), q, 90f, quality);
            builder.XYZSymmetry();
            return builder;
        }
    }
}