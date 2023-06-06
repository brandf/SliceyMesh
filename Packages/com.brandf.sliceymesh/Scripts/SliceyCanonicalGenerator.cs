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
        
        // this isn't really a new type, it's just an optimization for when radius = 0
        public static SliceyMeshBuilder HardCube() 
        {
            var builder = SliceyMeshBuilder.Begin(SliceyMeshBuilder.SizeForQuad * 24);
            var h = 0.5f;
            var initial = builder.Cursor;
            builder.AddQuad(new Vector3(0, h, -h), 
                            new Vector3(h, h, -h),
                            new Vector3(h, 0, -h),
                            new Vector3(0, 0, -h), Vector3.back);                   // back quarter face
            var afterQuad = builder.Cursor;
            builder.CopyReflection(initial, afterQuad, ForwardToUp);                    // top
            builder.CopyReflection(initial, afterQuad, ForwardToRight);                 // right

            builder.XYZSymmetry();
            return builder;
        }

        public static SliceyMeshBuilder CylindricalCube(float quality)
        {
            var builder = SliceyMeshBuilder.Begin((SliceyMeshBuilder.SizeForStrip(22) + SliceyMeshBuilder.SizeForFan(90f, quality) + SliceyMeshBuilder.SizeForCylinder(90f, quality) * 2) * 8);
            var h = 0.5f;
            var q = 0.25f;
            //     __
            //    /__/
            //   /__/  /|
            //  |__|__/|/
            //  |__|__|/
            //
            builder.StripStart(new Vector3(0, h, 0),
                               new Vector3(q, h, 0), Vector3.up);
            builder.StripTo(new Vector3(0, h, -q),
                            new Vector3(q, h, -q), Vector3.up, true);
            builder.StripTo(new Vector3(0, h, -h),
                            new Vector3(q, h, -h), Vector3.up);
            builder.StripStart(new Vector3(0, h, -h), // duplicate vert for different normal
                               new Vector3(q, h, -h), Vector3.back);
            builder.StripTo(new Vector3(0, q, -h),
                            new Vector3(q, q, -h), Vector3.back, true);
            builder.StripTo(new Vector3(0, 0, -h), Vector3.back);
            builder.StripTo(new Vector3(q, 0, -h),
                            new Vector3(q, q, -h), Vector3.back);
            builder.StripTo(new Vector3(h, 0, -h),
                            new Vector3(h, q, -h), Vector3.back);
            builder.StripStart(new Vector3(h, 0, -h), // duplicate vert for different normal
                               new Vector3(h, q, -h), Vector3.right);
            builder.StripTo(new Vector3(h, 0, -q),
                            new Vector3(h, q, -q), Vector3.right);
            builder.StripTo(new Vector3(h, 0, 0),
                            new Vector3(h, q, 0), Vector3.right);

            builder.AddFan(new Pose(new Vector3(q, q, -h), Quaternion.Euler(0, 0, 90)), q, -90f, quality); // rounded corner

            var before = builder.Cursor;
            builder.AddCylinder(new Pose(new Vector3(q, q, -q), Quaternion.identity), q, 90f, quality, q); // rounded corner edge
            var after = builder.Cursor;
            builder.CopyReflection(before, after, new Plane(Vector3.back, new Vector3(0, 0, -q)));    // q -> h edge thickness
            
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