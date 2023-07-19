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
            var builder = RectHardQuadrant(4);
            builder.XYSymmetry();
            return builder;
        }
        
        public static SliceyMeshBuilder RectHardQuadrant(int sizeFactor)
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
            var builder = RectRoundQuadrant(quality, 4);
            builder.XYSymmetry();
            return builder;
        }

        public static SliceyMeshBuilder RectRoundQuadrant(float quality, int sizeFactor)
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
            builder.AddFan(new Pose(new Vector3(q, q, 0), Quaternion.identity), q, 90f, quality); // rounded corner
            return builder;
        }

        // this isn't really a new type, it's just an optimization for when radius = 0
        public static SliceyMeshBuilder CubeHard()
        {
            var builder = CubeHardOctant(8);
            builder.XYZSymmetry();
            return builder;
        }



        public static SliceyMeshBuilder CubeHardOctant(int sizeFactor) 
        {
            var builder = SliceyMeshBuilder.Begin(SliceyMeshBuilder.SizeForQuad * (3 * sizeFactor));
            AddCubeHardOctant(ref builder);
            return builder;
        }

        public static void AddCubeHardOctant(ref SliceyMeshBuilder builder)
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

        public static SliceyMeshBuilder CubeRoundSides(float quality)
        {
            var builder = CubeRoundSidesOctant(quality, 8);
            builder.XYZSymmetry();
            return builder;
        }

        public static SliceyMeshBuilder CubeRoundSidesOctant(float quality, int sizeFactor)
        {
            var builder = SliceyMeshBuilder.Begin((SliceyMeshBuilder.SizeForStrip(22) + SliceyMeshBuilder.SizeForFan(90f, quality) + SliceyMeshBuilder.SizeForCylinder(90f, quality) * 2) * sizeFactor);
            AddCubeRoundSidesOctant(ref builder, quality);
            return builder;
        }

        public static void AddCubeRoundSidesOctant(ref SliceyMeshBuilder builder, float quality)
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

            builder.AddFan(new Pose(new Vector3(q, q, -h), Quaternion.identity), q, 90f, quality); // rounded corner

            var beforeCylinder = builder.Cursor;
            builder.AddCylinder(new Pose(new Vector3(q, q, -q), Quaternion.identity), q, 90f, quality, q); // rounded corner edge
            var afterCylinder = builder.Cursor;
            builder.CopyReflection(beforeCylinder, afterCylinder, new Plane(Vector3.back, new Vector3(0, 0, -q)));    // q -> h edge thickness
        }

        public static SliceyMeshBuilder CubeRoundSidesAndZ(float qualitySides, float qualityZ)
        {
            var builder = CubeRoundSidesAndZOctant(qualitySides, qualityZ, 8);
            builder.XYZSymmetry();
            return builder;
        }

        public static SliceyMeshBuilder CubeRoundSidesAndZOctant(float qualitySides, float qualityZ, int sizeFactor)
        {
            // uniform
            if (qualitySides == qualityZ)
            {
                var builder = SliceyMeshBuilder.Begin(((SliceyMeshBuilder.SizeForQuad + SliceyMeshBuilder.SizeForCylinder(90f, qualitySides)) * 3 +
                                                        SliceyMeshBuilder.SizeForCorner3(90f, qualitySides)) * sizeFactor);
                var h = 0.5f;
                var q = 0.25f;
                var initial = builder.Cursor;
                builder.AddQuad(new Vector3(0, 0, -h),
                                new Vector3(0, q, -h),
                                new Vector3(q, q, -h),
                                new Vector3(q, 0, -h), Vector3.back);
                builder.AddCylinder(new Pose(new Vector3(0, q, -q), Quaternion.Euler(0, 90, 0)), q, 90f, qualitySides, q);
                var after = builder.Cursor;
                builder.CopyRotation(initial, after, Quaternion.Euler(90, 90, 0));
                builder.CopyRotation(initial, after, Quaternion.Euler(0, -90, 90));
                builder.AddCorner3(new Pose(new Vector3(q, q, -h + q), Quaternion.identity), q, 90f, qualitySides);
                return builder;
            }
            else
            {
                return default;
            }    
        }

        public static SliceyMeshBuilder CubeRoundSidesAndZAsymmetric(float qualitySides, float qualityZ1, float qualityZ2)
        {
            // Round Z- face
            var builder = CubeRoundSidesAndZOctant(qualitySides, qualityZ1, 8); // TODO this needs to factor in both qualities

            // Hard Z+ face
            var before = builder.Cursor;
            AddCubeRoundSidesOctant(ref builder, qualitySides);
            var after = builder.Cursor;
            builder.Reflect(before, after, Vector3.forward);

            builder.XYSymmetry();
            return builder;
        }

        public static SliceyMeshBuilder CylinderHard(float qualityRadial)
        {
            var builder = CylinderHardOctant(qualityRadial, 8);
            builder.XYZSymmetry();
            return builder;
        }

        public static SliceyMeshBuilder CylinderHardOctant(float qualityRadial, int sizeFactor)
        {
            var builder = SliceyMeshBuilder.Begin((SliceyMeshBuilder.SizeForCylinder(90f, qualityRadial) + SliceyMeshBuilder.SizeForFan(90f, qualityRadial )) * sizeFactor);
            AddCylinderHardOctant(ref builder, qualityRadial);
            return builder;
        }

        public static void AddCylinderHardOctant(ref SliceyMeshBuilder builder, float qualityRadial)
        {
            builder.AddCylinder(new Pose(Vector3.back * 0.5f, Quaternion.identity), 0.5f, 90f, qualityRadial, 0.5f);
            builder.AddFan(new Pose(Vector3.back * 0.5f, Quaternion.identity), 0.5f, 90f, qualityRadial);
        }

        public static SliceyMeshBuilder CylinderRoundZ(float qualityRadial, float qualityZ)
        {
            var builder = CylinderRoundZOctant(qualityRadial, qualityZ, 8);
            builder.XYZSymmetry();
            return builder;
        }

        public static SliceyMeshBuilder CylinderRoundZOctant(float qualityRadial, float qualityZ, int sizeFactor)
        {
            var builder = SliceyMeshBuilder.Begin((SliceyMeshBuilder.SizeForCylinder(90f, qualityRadial) + 
                                                   SliceyMeshBuilder.SizeForFan(90f, qualityRadial) + 
                                                   SliceyMeshBuilder.SizeForRevolvedArc(90f, 90f, qualityRadial, qualityZ)) * sizeFactor);
            AddCylinderRoundZOctant(ref builder, qualityRadial, qualityZ);
            return builder;
        }

        public static void AddCylinderRoundZOctant(ref SliceyMeshBuilder builder, float qualityRadial, float qualityZ)
        {
            var primaryRadius = 0.5f;
            var edgeRadius = 0.1f;
            var depth = 0.5f;
            var cylinderDepth = depth - edgeRadius;
            builder.AddCylinder(new Pose(Vector3.back * cylinderDepth, Quaternion.identity), primaryRadius, 90f, qualityRadial, cylinderDepth);
            builder.AddRevolvedArc(new Pose(Vector3.back * depth, Quaternion.identity), primaryRadius, edgeRadius, 90f, 90f, qualityRadial, qualityZ);
            builder.AddFan(new Pose(Vector3.back * depth, Quaternion.identity), primaryRadius - edgeRadius, 90f, qualityRadial);
        }



        public static SliceyMeshBuilder CylinderRoundZAsymmetric(float qualityRadial, float qualityZ1, float qualityZ2)
        {
            var builder = CylinderRoundZAsymmetricOctant(qualityRadial, qualityZ1, qualityZ2, 8);
            builder.XYSymmetry();
            return builder;
        }

        public static SliceyMeshBuilder CylinderRoundZAsymmetricOctant(float qualityRadial, float qualityZ1, float qualityZ2, int sizeFactor)
        {
            var builder = SliceyMeshBuilder.Begin((SliceyMeshBuilder.SizeForCylinder(90f, qualityRadial) + SliceyMeshBuilder.SizeForFan(90f, qualityRadial)) * sizeFactor);
            AddCylinderRoundZAsymmetricOctant(ref builder, qualityRadial, qualityZ1);
            AddCylinderRoundZAsymmetricOctant(ref builder, qualityRadial, qualityZ2);
            return builder;
        }

        public static void AddCylinderRoundZAsymmetricOctant(ref SliceyMeshBuilder builder, float qualityRadial, float qualityZ)
        {
            builder.AddCylinder(new Pose(Vector3.back * 0.5f, Quaternion.identity), 0.5f, 90f, qualityRadial, 0.5f);
            builder.AddFan(new Pose(Vector3.back * 0.5f, Quaternion.identity), 0.5f, 90f, qualityRadial);
        }
    }
}