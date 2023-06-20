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

        static public Vector3 DiagXY = new Vector3(1f, 1f, 0).normalized;

        // this isn't really a new type, it's just an optimization for when radius = 0
        public static SliceyMeshBuilder RectHard()
        {
            var builder = HardRectQuadrant(4);
            builder.XYSymmetry();
            return builder;
        }
        
        public static SliceyMeshBuilder HardRectQuadrant(int sizeFactor)
        {
            var builder = SliceyMeshBuilder.Begin(SliceyMeshBuilder.SizeForQuad * sizeFactor);
            var h = 0.5f;
            builder.AddQuad(new Vector3(0, h, 0),
                            new Vector3(h, h, 0),
                            new Vector3(h, 0, 0),
                            new Vector3(0, 0, 0), Vector3.back);                   // back quarter face

            return builder;
        }
        
        public static SliceyMeshBuilder RectRound(float quality)
        {
            var builder = RoundRectQuadrant(quality, 4);
            builder.XYSymmetry();
            return builder;
        }

        public static SliceyMeshBuilder RoundRectQuadrant(float quality, int sizeFactor)
        {
            var builder = SliceyMeshBuilder.Begin((SliceyMeshBuilder.SizeForStrip(9) + SliceyMeshBuilder.SizeForFan(90f, quality)) * sizeFactor);
            var h = 0.5f;
            var q = 0.25f;
            //   __
            //  |__|
            //  | /
            //

            var initial = builder.Cursor;
            builder.StripStart(new Vector3(0, h, 0), // duplicate vert for different normal
                               new Vector3(q, h, 0), Vector3.back);
            builder.StripTo(new Vector3(0, q, 0),
                            new Vector3(q, q, 0), Vector3.back, true);
            builder.StripTo(new Vector3(0, 0, 0), Vector3.back);
            var after = builder.Cursor;
            builder.CopyReflection(initial, after, DiagXY);
            builder.AddFan(new Pose(new Vector3(q, q, 0), Quaternion.Euler(0, 0, 90)), q, -90f, quality); // rounded corner

            return builder;
        }

        // this isn't really a new type, it's just an optimization for when radius = 0
        public static SliceyMeshBuilder CuboidHard()
        {
            var builder = HardCubeOctant(8);
            builder.XYZSymmetry();
            return builder;
        }



        public static SliceyMeshBuilder HardCubeOctant(int sizeFactor) 
        {
            var builder = SliceyMeshBuilder.Begin(SliceyMeshBuilder.SizeForQuad * (3 * sizeFactor));
            AddHardCubeOctant(ref builder);
            return builder;
        }

        public static void AddHardCubeOctant(ref SliceyMeshBuilder builder)
        {
            var h = 0.5f;
            var initial = builder.Cursor;
            builder.AddQuad(new Vector3(0, h, -h),
                            new Vector3(h, h, -h),
                            new Vector3(h, 0, -h),
                            new Vector3(0, 0, -h), Vector3.back);                   // back quarter face
            var afterQuad = builder.Cursor;
            builder.CopyReflection(initial, afterQuad, ForwardToUp);                    // top
            builder.CopyReflection(initial, afterQuad, ForwardToRight);                 // right
        }

        public static SliceyMeshBuilder CuboidCylindrical(float quality)
        {
            var builder = CylindricalCubeOctant(quality, 8);
            builder.XYZSymmetry();
            return builder;
        }

        public static SliceyMeshBuilder CylindricalCubeOctant(float quality, int sizeFactor)
        {
            var builder = SliceyMeshBuilder.Begin((SliceyMeshBuilder.SizeForStrip(22) + SliceyMeshBuilder.SizeForFan(90f, quality) + SliceyMeshBuilder.SizeForCylinder(90f, quality) * 2) * sizeFactor);
            AddCylindricalCubeOctant(ref builder, quality);
            return builder;
        }

        public static void AddCylindricalCubeOctant(ref SliceyMeshBuilder builder, float quality)
        {
            var h = 0.5f;
            var q = 0.25f;
            //     __
            //    /__/
            //   /__/
            //  |__|
            //  | /
            //
            var initial = builder.Cursor;
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
            var afteStrip = builder.Cursor;
            builder.CopyReflection(initial, afteStrip, DiagXY);

            builder.AddFan(new Pose(new Vector3(q, q, -h), Quaternion.Euler(0, 0, 90)), q, -90f, quality); // rounded corner

            var beforeCylinder = builder.Cursor;
            builder.AddCylinder(new Pose(new Vector3(q, q, -q), Quaternion.identity), q, 90f, quality, q); // rounded corner edge
            var afterCylinder = builder.Cursor;
            builder.CopyReflection(beforeCylinder, afterCylinder, new Plane(Vector3.back, new Vector3(0, 0, -q)));    // q -> h edge thickness
        }

        public static SliceyMeshBuilder CuboidSpherical(float quality)
        {
            var builder = SphericalCubeOctant(quality, 8);
            builder.XYZSymmetry();
            return builder;
        }

        public static SliceyMeshBuilder CuboidSphericalCylindrical(float quality)
        {
            // Round Z- face
            var builder = SphericalCubeOctant(quality, 8);
            
            // Hard Z+ face
            var before = builder.Cursor;
            AddCylindricalCubeOctant(ref builder, quality);
            var after = builder.Cursor;
            builder.Reflect(before, after, Vector3.forward);

            builder.XYSymmetry();
            return builder;
        }

        public static SliceyMeshBuilder SphericalCubeOctant(float quality, int sizeFactor)
        {
            var builder = SliceyMeshBuilder.Begin(((SliceyMeshBuilder.SizeForQuad + SliceyMeshBuilder.SizeForCylinder(90f, quality)) * 3 +
                                                    SliceyMeshBuilder.SizeForCorner3(90f, quality)) * sizeFactor);
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
            return builder;
        }
    }
}