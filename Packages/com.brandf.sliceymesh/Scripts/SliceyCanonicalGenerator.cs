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

        static public Vector3 DiagXY = new Vector3(-1f, 1f, 0).normalized;

        static int PortionFactor(SliceyMeshPortion portion, bool isQuadrant)
        {
            return portion switch
            {
                SliceyMeshPortion.Full => isQuadrant ? 4 : 8,
                SliceyMeshPortion.Half => isQuadrant ? 2 : 4,
                SliceyMeshPortion.Partial => 1,
            };
        }

        static void ApplySymmetry(ref SliceyMeshBuilder builder, SliceyMeshPortion portion, bool isQuadrant)
        {
            switch (portion)
            {
                case SliceyMeshPortion.Full:
                    if (isQuadrant) builder.XYSymmetry(); else builder.XYZSymmetry();
                    break;
                case SliceyMeshPortion.Half:
                    if (isQuadrant) builder.YSymmetry(); else builder.XYSymmetry();
                    break;
                case SliceyMeshPortion.Partial:
                    break;
            }
        }

        // this isn't really a new type, it's just an optimization for when radius = 0
        public static SliceyMeshBuilder RectHard(SliceyMeshPortion portion)
        {
            var builder = RectHardQuadrant(PortionFactor(portion, true));
            ApplySymmetry(ref builder, portion, true);
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

        public static SliceyMeshBuilder RectRound(SliceyMeshPortion portion, float quality)
        {
            var builder = RectRoundQuadrant(quality, PortionFactor(portion, true));
            ApplySymmetry(ref builder, portion, true);
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
        public static SliceyMeshBuilder CubeHard(SliceyMeshPortion portion)
        {
            var builder = CubeHardOctant(PortionFactor(portion, false));
            ApplySymmetry(ref builder, portion, false);
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

        public static SliceyMeshBuilder CubeRoundSides(SliceyMeshPortion portion, float quality)
        {
            var builder = CubeRoundSidesOctant(quality, PortionFactor(portion, false));
            ApplySymmetry(ref builder, portion, false);
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
            var afterStrip = builder.Cursor;
            builder.CopyReflection(initial, afterStrip, DiagXY);

            builder.AddFan(new Pose(new Vector3(q, q, -h), Quaternion.identity), q, 90f, quality); // rounded corner

            var beforeCylinder = builder.Cursor;
            builder.AddCylinder(new Pose(new Vector3(q, q, -q), Quaternion.identity), q, 90f, quality, q); // rounded corner edge
            var afterCylinder = builder.Cursor;
            builder.CopyReflection(beforeCylinder, afterCylinder, new Plane(Vector3.back, new Vector3(0, 0, -q)));    // q -> h edge thickness
        }


        public static SliceyMeshBuilder CubeRoundEdges(SliceyMeshPortion portion, float quality)
        {
            var builder = CubeRoundEdgesOctant(quality, PortionFactor(portion, false));
            ApplySymmetry(ref builder, portion, false);
            return builder;
        }

        public static SliceyMeshBuilder CubeRoundEdgesOctant(float quality, int sizeFactor)
        {

            var builder = SliceyMeshBuilder.Begin(((SliceyMeshBuilder.SizeForQuad + SliceyMeshBuilder.SizeForCylinder(90f, quality)) * 3 +
                                                    SliceyMeshBuilder.SizeForCorner3(90f, quality)) * sizeFactor);
            AddCubeRoundEdgesOctant(ref builder, quality);
            return builder;
        }

        public static void AddCubeRoundEdgesOctant(ref SliceyMeshBuilder builder, float quality)
        {
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
        }

        public static SliceyMeshBuilder CubeRoundSidesFillet(SliceyMeshPortion portion, float qualitySides, float qualityFillet)
        {
            var builder = CubeRoundSidesFilletOctant(qualitySides, qualityFillet, PortionFactor(portion, false));
            ApplySymmetry(ref builder, portion, false);
            return builder;
        }

        public static SliceyMeshBuilder CubeRoundSidesFilletOctant(float qualitySides, float qualityFillet, int sizeFactor)
        {

            var builder = SliceyMeshBuilder.Begin((SliceyMeshBuilder.SizeForStrip(5)*2 +
                                                   SliceyMeshBuilder.SizeForQuad * 2 +
                                                   SliceyMeshBuilder.SizeForFan(90f, qualitySides) + 
                                                   SliceyMeshBuilder.SizeForCylinder(90f, qualitySides) +
                                                   SliceyMeshBuilder.SizeForCylinder(90f, qualityFillet) * 2 +
                                                   SliceyMeshBuilder.SizeForRevolvedArc(90f, 90f, qualitySides, qualityFillet)) * sizeFactor);
            AddCubeRoundSidesFilletOctant(ref builder, qualitySides, qualityFillet);
            return builder;  
        }

        public static void AddCubeRoundSidesFilletOctant(ref SliceyMeshBuilder builder, float qualitySides, float qualityFillet)
        {
            var sideRadius = 0.25f;
            var edgeRadius = 0.125f;
            var innerSize = 0.5f - sideRadius;
            var fanRadius = sideRadius - edgeRadius;
            var depth = 0.5f;
            var sideDepth = depth - edgeRadius;

            var initial = builder.Cursor;
            builder.StripStart(new Vector3(0, sideDepth, -depth),
                               new Vector3(innerSize, sideDepth, -depth), Vector3.back);
            builder.StripTo(new Vector3(0, innerSize, -depth),
                            new Vector3(innerSize, innerSize, -depth), Vector3.back, true);

            builder.StripTo(new Vector3(0, 0, -depth), Vector3.back);

            builder.AddQuad(new Vector3(0, depth, 0),
                            new Vector3(innerSize, depth, 0),
                            new Vector3(innerSize, depth, -sideDepth),
                            new Vector3(0, depth, -sideDepth), Vector3.up);

            builder.AddCylinder(new Pose(new Vector3(0, sideDepth, -sideDepth), Quaternion.Euler(0, 90f, 0)), edgeRadius, 90f, qualityFillet, innerSize);
            var after = builder.Cursor;
            builder.CopyReflection(initial, after, DiagXY);

            builder.AddCylinder(new Pose(new Vector3(innerSize, innerSize, -sideDepth), Quaternion.identity), sideRadius, 90f, qualitySides, sideDepth);
            builder.AddFan(new Pose(new Vector3(innerSize, innerSize, -depth), Quaternion.identity), fanRadius, 90f, qualitySides);
            builder.AddRevolvedArc(new Pose(new Vector3(innerSize, innerSize, -depth), Quaternion.identity), sideRadius, edgeRadius, 90f, 90f, qualitySides, qualityFillet);

        }

        public static SliceyMeshBuilder CylinderHard(SliceyMeshPortion portion, float qualityRadial)
        {
            var builder = CylinderHardOctant(qualityRadial, PortionFactor(portion, false));
            ApplySymmetry(ref builder, portion, false);
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

        public static SliceyMeshBuilder CylinderRoundEdges(SliceyMeshPortion portion, float qualityRadial, float qualityFillet)
        {
            var builder = CylinderRoundEdgesOctant(qualityRadial, qualityFillet, PortionFactor(portion, false));
            ApplySymmetry(ref builder, portion, false);
            return builder;
        }

        public static SliceyMeshBuilder CylinderRoundEdgesOctant(float qualityRadial, float qualityFillet, int sizeFactor)
        {
            var builder = SliceyMeshBuilder.Begin((SliceyMeshBuilder.SizeForCylinder(90f, qualityRadial) + 
                                                   SliceyMeshBuilder.SizeForFan(90f, qualityRadial) + 
                                                   SliceyMeshBuilder.SizeForRevolvedArc(90f, 90f, qualityRadial, qualityFillet)) * sizeFactor);
            AddCylinderRoundEdgesOctant(ref builder, qualityRadial, qualityFillet);
            return builder;
        }

        public static void AddCylinderRoundEdgesOctant(ref SliceyMeshBuilder builder, float qualityRadial, float qualityFillet)
        {
            var primaryRadius = 0.5f;
            var edgeRadius = 0.25f;
            var depth = 0.5f;
            var cylinderDepth = depth - edgeRadius;
            builder.AddCylinder(new Pose(Vector3.back * cylinderDepth, Quaternion.identity), primaryRadius, 90f, qualityRadial, cylinderDepth);
            builder.AddRevolvedArc(new Pose(Vector3.back * depth, Quaternion.identity), primaryRadius, edgeRadius, 90f, 90f, qualityRadial, qualityFillet);
            builder.AddFan(new Pose(Vector3.back * depth, Quaternion.identity), primaryRadius - edgeRadius, 90f, qualityRadial);
        }
    }
}