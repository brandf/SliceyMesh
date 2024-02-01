using System;
using System.Diagnostics.CodeAnalysis;
using UnityEngine;
using UnityEngine.UIElements;

namespace SliceyMesh
{
    public static class SliceyCanonicalGenerator
    {
        static public Vector3 ForwardToUp = new Vector3(0f, 1f, 1f).normalized;
        static public Vector3 ForwardToRight = new Vector3(1f, 0f, 1f).normalized;
        static public Vector3 DiagXY = new Vector3(-1f, 1f, 0f).normalized;
        static public Vector3 DiagXZ = new Vector3(1f, 0f, 1f).normalized;
        static public Vector3 DiagYZ = new Vector3(0f, 1f, 1f).normalized;

        static int PortionFactor(SliceyMeshPortion portion, bool is2D)
        {
            int PortionAxisFactor(SliceyMeshPortionAxis a) => a == SliceyMeshPortionAxis.Both ? 2 : 1;

            var factorX = PortionAxisFactor(portion.X);
            var factorY = PortionAxisFactor(portion.Y);
            var factorZ = is2D ? 1 : PortionAxisFactor(portion.Z);
            var factor = factorX * factorY * factorZ;
            return factor;
        }

        static void ApplySymmetry(ref SliceyMeshBuilder builder, SliceyMeshPortion portion, bool is2D)
        {
            // The builder intially has an Octant with X = Positive, Y = Positive, Z = Negative
            if (portion.X == SliceyMeshPortionAxis.Negative)
                builder.XReflect();
            else if (portion.X == SliceyMeshPortionAxis.Both)
                builder.XSymmetry();

            if (portion.Y == SliceyMeshPortionAxis.Negative)
                builder.YReflect();
            else if (portion.Y == SliceyMeshPortionAxis.Both)
                builder.YSymmetry();

            if (!is2D)
            {
                if (portion.Z == SliceyMeshPortionAxis.Positive)
                    builder.ZReflect();
                else if (portion.Z == SliceyMeshPortionAxis.Both)
                    builder.ZSymmetry();
            }
        }

        // this isn't really a new type, it's just an optimization for when radius = 0
        public static SliceyMeshBuilder RectHard(SliceyMeshPortion portion)
        {
            var sizeFactor = PortionFactor(portion, true);
            var builder = SliceyMeshBuilder.Begin(SliceyMeshBuilder.SizeForQuad * sizeFactor);
            AddRectHardQuadrant(ref builder);
            ApplySymmetry(ref builder, portion, true);
            return builder;
        }
        
        public static void AddRectHardQuadrant(ref SliceyMeshBuilder builder)
        {
            var h = 0.5f;
            builder.AddQuad(new Vector3(0, h, 0),
                            new Vector3(h, h, 0),
                            new Vector3(h, 0, 0),
                            new Vector3(0, 0, 0), Vector3.back); // back quarter face
        }

        public static SliceyMeshBuilder RectRound(SliceyMeshPortion portion, float quality)
        {
            var sizeFactor = PortionFactor(portion, true);
            var builder = SliceyMeshBuilder.Begin((SliceyMeshBuilder.SizeForStrip(9) + SliceyMeshBuilder.SizeForFan(90f, quality)) * sizeFactor);
            AddRectRoundQuadrant(ref builder, quality);
            ApplySymmetry(ref builder, portion, true);
            return builder;
        }


        public static void AddRectRoundQuadrant(ref SliceyMeshBuilder builder, float quality)
        {
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
        }

        public static SliceyMeshBuilder RectOutline(float size, float radius, float thickeness, float quality)
        {
            var portion = default(SliceyMeshPortion); // portions not supported on rect outline atm
            var sizeFactor = PortionFactor(portion, true);
            var builder = SliceyMeshBuilder.Begin((SliceyMeshBuilder.SizeForQuad * 2 + SliceyMeshBuilder.SizeForArcOutline(90f, quality)) * sizeFactor);
            AddRectRectOutlineQuadrant(ref builder, size, radius, thickeness, quality);
            ApplySymmetry(ref builder, portion, true);
            return builder;
        }

        public static void AddRectRectOutlineQuadrant(ref SliceyMeshBuilder builder, float size, float radius, float thickeness, float quality)
        {
            var h = size * 0.5f;
            var h2 = h - radius;
            var t = h - thickeness;
            var a = h - radius;

            var initial = builder.Cursor;
            builder.AddQuad(new Vector3(h, 0f),
                            new Vector3(t, 0f),
                            new Vector3(t, h2),
                            new Vector3(h, h2), Vector3.back);
            var after = builder.Cursor;
            builder.CopyReflection(initial, after, DiagXY);
            var rI = radius - thickeness;
            if (rI > 0)
                builder.AddArcOutline(new Pose(new Vector3(a, a, 0), Quaternion.identity), radius, rI, 90f, quality); // rounded corner outline
            else
                builder.AddFan(new Pose(new Vector3(a, a, 0), Quaternion.identity), radius, 90f, quality);
        }

        public static SliceyMeshBuilder RectOutline(SliceyMeshPortion portion, float quality)
        {
            var sizeFactor = PortionFactor(portion, true);
            var builder = SliceyMeshBuilder.Begin((SliceyMeshBuilder.SizeForStrip(9) + SliceyMeshBuilder.SizeForFan(90f, quality)) * sizeFactor);
            AddRectRoundQuadrant(ref builder, quality);
            ApplySymmetry(ref builder, portion, true);
            return builder;
        }

        // this isn't really a new type, it's just an optimization for when radius = 0
        public static SliceyMeshBuilder CubeHard(SliceyMeshPortion portion, bool portionClosed)
        {
            var sizeFactor = PortionFactor(portion, false);
            var openPortionSize = SliceyMeshBuilder.SizeForQuad * 3;
            var closedPortionSize = new SliceyMeshBuilder.SliceyCursor();
            if (portionClosed)
            {
                closedPortionSize = portion.Type switch
                {
                    SliceyMeshPortionType.Full     => new SliceyMeshBuilder.SliceyCursor(),
                    SliceyMeshPortionType.Half     => SliceyMeshBuilder.SizeForQuad,
                    SliceyMeshPortionType.Quadrant => SliceyMeshBuilder.SizeForQuad * 2,
                    SliceyMeshPortionType.Octant   => SliceyMeshBuilder.SizeForQuad * 3,
                };
            }
            var builder = SliceyMeshBuilder.Begin((openPortionSize + closedPortionSize) * sizeFactor);
            AddCubeHardOctant(ref builder);
            if (portionClosed)
                CloseCubeHardOctant(ref builder, portion);
            ApplySymmetry(ref builder, portion, false);
            return builder;
        }

        static void AddCubeHardOctant(ref SliceyMeshBuilder builder)
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

        static void CloseCubeHardOctant(ref SliceyMeshBuilder builder, SliceyMeshPortion portion)
        {
            if (portion.X != SliceyMeshPortionAxis.Both)
                CloseQuadLeftOctant(ref builder);

            if (portion.Y != SliceyMeshPortionAxis.Both)
                CloseQuadBottomOctant(ref builder);

            if (portion.Z != SliceyMeshPortionAxis.Both)
                CloseQuadForwardOctant(ref builder);
        }

        public static SliceyMeshBuilder CubeRoundSides(SliceyMeshPortion portion, bool portionClosed, float quality)
        {
            var sizeFactor = PortionFactor(portion, false);
            var openPortionSize = SliceyMeshBuilder.SizeForStrip(22) +
                                  SliceyMeshBuilder.SizeForFan(90f, quality) +
                                  SliceyMeshBuilder.SizeForCylinder(90f, quality) * 2;
            var closedPortionSize = new SliceyMeshBuilder.SliceyCursor();

            if (portionClosed)
            {
                if (portion.X != SliceyMeshPortionAxis.Both)
                    closedPortionSize += SliceyMeshBuilder.SizeForQuad;
                if (portion.Y != SliceyMeshPortionAxis.Both)
                    closedPortionSize += SliceyMeshBuilder.SizeForQuad;
                if (portion.Z != SliceyMeshPortionAxis.Both)
                    closedPortionSize += SliceyMeshBuilder.SizeForStrip(10) +
                                         SliceyMeshBuilder.SizeForFan(90f, quality);
            }
            var builder = SliceyMeshBuilder.Begin((openPortionSize + closedPortionSize) * sizeFactor);
            AddCubeRoundSidesOctant(ref builder, quality);
            if (portionClosed) 
                CloseCubeRoundSidesOctant(ref builder, portion, quality);
           
            ApplySymmetry(ref builder, portion, false);
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

        static void CloseCubeRoundSidesOctant(ref SliceyMeshBuilder builder, SliceyMeshPortion portion, float quality)
        {
            if (portion.X != SliceyMeshPortionAxis.Both)
                CloseRoundSidesForwardOctant(ref builder, quality);
            if (portion.X != SliceyMeshPortionAxis.Both)
                CloseQuadLeftOctant(ref builder);
            if (portion.Y != SliceyMeshPortionAxis.Both)
                CloseQuadBottomOctant(ref builder);
        }

        static void CloseQuadForwardOctant(ref SliceyMeshBuilder builder)
        {
            var h = 0.5f;
            builder.AddQuad(new Vector3(0, 0, 0f),
                            new Vector3(h, 0, 0f),
                            new Vector3(h, h, 0f),
                            new Vector3(0, h, 0f), Vector3.forward);                // forward quarter face
        }

        static void CloseQuadBottomOctant(ref SliceyMeshBuilder builder)
        {
            var h = 0.5f;
            builder.AddQuad(new Vector3(0, 0, 0f),
                            new Vector3(0, 0, -h),
                            new Vector3(h, 0, -h),
                            new Vector3(h, 0, 0f), Vector3.down);                   // bottom face
        }
        static void CloseQuadLeftOctant(ref SliceyMeshBuilder builder)
        {
            var h = 0.5f;
            builder.AddQuad(new Vector3(0, 0, 0f),
                            new Vector3(0, h, 0f),
                            new Vector3(0, h, -h),
                            new Vector3(0, 0, -h), Vector3.left);                   // left face
        }

        static void CloseRoundSidesForwardOctant(ref SliceyMeshBuilder builder, float quality)
        {
            var h = 0.5f;
            var q = 0.25f;
            var initial = builder.Cursor;

            builder.StripStart(new Vector3(q, h, 0), // duplicate vert for different normal
               new Vector3(0, h, 0), Vector3.forward);
            builder.StripTo(new Vector3(q, q, 0),
                            new Vector3(0, q, 0), Vector3.forward, true);
            builder.StripTo(new Vector3(0, 0, 0), Vector3.forward);
            var afterStrip = builder.Cursor;
            builder.CopyReflection(initial, afterStrip, DiagXY);
            builder.AddFan(new Pose(new Vector3(q, q, 0), Quaternion.Euler(0, 180f, 90f)), q, 90f, quality); // rounded corner
        }

        public static SliceyMeshBuilder CubeRoundEdges(SliceyMeshPortion portion, bool portionClosed, float quality)
        {
            var sizeFactor = PortionFactor(portion, false);

            var openPortionSize = (SliceyMeshBuilder.SizeForQuad + 
                                   SliceyMeshBuilder.SizeForCylinder(90f, quality)) * 3 +
                                  SliceyMeshBuilder.SizeForCorner3(90f, quality);

            var closedPortionSize = new SliceyMeshBuilder.SliceyCursor();
            if (portionClosed)
            {
                var roundQuadRectSize = SliceyMeshBuilder.SizeForStrip(10) +
                                        SliceyMeshBuilder.SizeForFan(90f, quality);

                if (portion.X != SliceyMeshPortionAxis.Both)
                    closedPortionSize += roundQuadRectSize;
                if (portion.Y != SliceyMeshPortionAxis.Both)
                    closedPortionSize += roundQuadRectSize;
                if (portion.Z != SliceyMeshPortionAxis.Both)
                    closedPortionSize += roundQuadRectSize;
            }
            var builder = SliceyMeshBuilder.Begin((openPortionSize + closedPortionSize) * sizeFactor);
            AddCubeRoundEdgesOctant(ref builder, quality);
            if (portionClosed)
                CloseCubeRoundEdgesOctant(ref builder, portion, quality);
            ApplySymmetry(ref builder, portion, false);
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

        static void CloseCubeRoundEdgesOctant(ref SliceyMeshBuilder builder, SliceyMeshPortion portion, float quality)
        {
            var q = 0.25f;
            if (portion.X != SliceyMeshPortionAxis.Both)
                CloseRoundSidesLeftOctant(ref builder, q, quality, q);
            if (portion.Y != SliceyMeshPortionAxis.Both)
                CloseRoundSidesBottomOctant(ref builder, q, quality, q);
            if (portion.Z != SliceyMeshPortionAxis.Both)
                CloseRoundSidesForwardOctant(ref builder, quality);
        }


        public static SliceyMeshBuilder CubeRoundSidesFillet(SliceyMeshPortion portion, bool portionClosed, float filletRadius, float qualitySides, float qualityFillet)
        {
            var sizeFactor = PortionFactor(portion, false);
            var openPortionSize = SliceyMeshBuilder.SizeForStrip(5) * 2 +
                                  SliceyMeshBuilder.SizeForQuad * 2 +
                                  SliceyMeshBuilder.SizeForFan(90f, qualitySides) +
                                  SliceyMeshBuilder.SizeForCylinder(90f, qualitySides) +
                                  SliceyMeshBuilder.SizeForCylinder(90f, qualityFillet) * 2 +
                                  SliceyMeshBuilder.SizeForRevolvedArc(90f, 90f, qualitySides, qualityFillet);

            var closedPortionSize = new SliceyMeshBuilder.SliceyCursor();
            if (portionClosed)
            {
                var roundQuadRectFilletSize = SliceyMeshBuilder.SizeForStrip(10) +
                                              SliceyMeshBuilder.SizeForFan(90f, qualityFillet);

                if (portion.X != SliceyMeshPortionAxis.Both)
                    closedPortionSize += roundQuadRectFilletSize;
                if (portion.Y != SliceyMeshPortionAxis.Both)
                    closedPortionSize += roundQuadRectFilletSize;
                if (portion.Z != SliceyMeshPortionAxis.Both)
                    closedPortionSize += SliceyMeshBuilder.SizeForStrip(10) +
                                         SliceyMeshBuilder.SizeForFan(90f, qualitySides);
            }
            var builder = SliceyMeshBuilder.Begin((openPortionSize + closedPortionSize) * sizeFactor);
            AddCubeRoundSidesFilletOctant(ref builder, filletRadius, qualitySides, qualityFillet);
            if (portionClosed)
                CloseCubeRoundSidesFilletOctant(ref builder, portion, filletRadius, qualitySides, qualityFillet);
            ApplySymmetry(ref builder, portion, false);
            return builder;
        }

        public static void AddCubeRoundSidesFilletOctant(ref SliceyMeshBuilder builder, float filletRadius, float qualitySides, float qualityFillet)
        {
            var sideRadius = 0.25f;
            var innerSize = 0.5f - sideRadius;
            var fanRadius = sideRadius - filletRadius;
            var depth = 0.5f;
            var sideDepth = depth - filletRadius;

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

            builder.AddCylinder(new Pose(new Vector3(0, sideDepth, -sideDepth), Quaternion.Euler(0, 90f, 0)), filletRadius, 90f, qualityFillet, innerSize);
            var after = builder.Cursor;
            builder.CopyReflection(initial, after, DiagXY);

            builder.AddCylinder(new Pose(new Vector3(innerSize, innerSize, -sideDepth), Quaternion.identity), sideRadius, 90f, qualitySides, sideDepth);
            builder.AddFan(new Pose(new Vector3(innerSize, innerSize, -depth), Quaternion.identity), fanRadius, 90f, qualitySides);
            builder.AddRevolvedArc(new Pose(new Vector3(innerSize, innerSize, -depth), Quaternion.identity), sideRadius, filletRadius, 90f, 90f, qualitySides, qualityFillet);

        }
        static void CloseCubeRoundSidesFilletOctant(ref SliceyMeshBuilder builder, SliceyMeshPortion portion, float filletRadius, float qualitySides, float qualityFillet)
        {
            var h = 0.5f;
            var f = h - filletRadius;

            if (portion.X != SliceyMeshPortionAxis.Both)
                CloseRoundSidesLeftOctant(ref builder, filletRadius, qualityFillet, f);
            if (portion.Y != SliceyMeshPortionAxis.Both)
                CloseRoundSidesBottomOctant(ref builder, filletRadius, qualityFillet, f);
            if (portion.Z != SliceyMeshPortionAxis.Both)
                CloseRoundSidesForwardOctant(ref builder, qualitySides);
        }

        static void CloseRoundSidesBottomOctant(ref SliceyMeshBuilder builder, float filletRadius, float qualityFillet, float f)
        {
            var h = 0.5f;
            var initial = builder.Cursor;
            builder.StripStart(new Vector3(0, 0, -h), // duplicate vert for different normal
                               new Vector3(f, 0, -h), Vector3.down);
            builder.StripTo(new Vector3(0, 0, -f),
                            new Vector3(f, 0, -f), Vector3.down, true);
            builder.StripTo(new Vector3(0, 0, 0), Vector3.down);
            var afterStrip = builder.Cursor;
            builder.CopyReflection(initial, afterStrip, DiagXZ);
            builder.AddFan(new Pose(new Vector3(f, 0, -f), Quaternion.Euler(-90, 0f, 0f)), filletRadius, 90f, qualityFillet); // rounded corner
        }
        static void CloseRoundSidesLeftOctant(ref SliceyMeshBuilder builder, float filletRadius, float qualityFillet, float f)
        {
            var h = 0.5f;
            var initial = builder.Cursor;
            builder.StripStart(new Vector3(0, h, 0), // duplicate vert for different normal
                               new Vector3(0, h, -f), Vector3.left);
            builder.StripTo(new Vector3(0, f, 0),
                            new Vector3(0, f, -f), Vector3.left, true);
            builder.StripTo(new Vector3(0, 0, 0), Vector3.left);
            var afterStrip = builder.Cursor;
            builder.CopyReflection(initial, afterStrip, DiagYZ);
            builder.AddFan(new Pose(new Vector3(0, f, -f), Quaternion.Euler(0f, 90f, 0f)), filletRadius, 90f, qualityFillet); // rounded corner
        }

        public static SliceyMeshBuilder CylinderHard(SliceyMeshPortion portion, bool portionClosed, float qualityRadial)
        {
            var sizeFactor = PortionFactor(portion, false);
            var openPortionSize = SliceyMeshBuilder.SizeForCylinder(90f, qualityRadial) + 
                                  SliceyMeshBuilder.SizeForFan(90f, qualityRadial);

            var closedPortionSize = new SliceyMeshBuilder.SliceyCursor();
            if (portionClosed)
            {
                if (portion.X != SliceyMeshPortionAxis.Both)
                    closedPortionSize += SliceyMeshBuilder.SizeForQuad;
                if (portion.Y != SliceyMeshPortionAxis.Both)
                    closedPortionSize += SliceyMeshBuilder.SizeForQuad;
                if (portion.Z != SliceyMeshPortionAxis.Both)
                    closedPortionSize += SliceyMeshBuilder.SizeForFan(90f, qualityRadial);
            }
            var builder = SliceyMeshBuilder.Begin((openPortionSize + closedPortionSize) * sizeFactor);
            AddCylinderHardOctant(ref builder, qualityRadial);
            if (portionClosed)
                CloseCylinderHardOctant(ref builder, portion, qualityRadial);
            ApplySymmetry(ref builder, portion, false);
            return builder;
        }

        public static void AddCylinderHardOctant(ref SliceyMeshBuilder builder, float qualityRadial)
        {
            var h = 0.5f;
            var r = 0.5f;
            builder.AddCylinder(new Pose(Vector3.back * h, Quaternion.identity), r, 90f, qualityRadial, h);
            builder.AddFan(new Pose(Vector3.back * h, Quaternion.identity), r, 90f, qualityRadial);
        }

        static void CloseCylinderHardOctant(ref SliceyMeshBuilder builder, SliceyMeshPortion portion, float qualityRadial)
        {
            var r = 0.5f;

            if (portion.X != SliceyMeshPortionAxis.Both)
                CloseQuadLeftOctant(ref builder);
            if (portion.Y != SliceyMeshPortionAxis.Both)
                CloseQuadBottomOctant(ref builder);
            if (portion.Z != SliceyMeshPortionAxis.Both)
                builder.AddFan(new Pose(Vector3.zero, Quaternion.Euler(0f, 180f, 90f)), r, 90f, qualityRadial);
        }

        public static SliceyMeshBuilder CylinderRoundEdges(SliceyMeshPortion portion, bool portionClosed, float qualityRadial, float qualityFillet)
        {
            var sizeFactor = PortionFactor(portion, false);
            var openPortionSize = SliceyMeshBuilder.SizeForCylinder(90f, qualityRadial) +
                                  SliceyMeshBuilder.SizeForFan(90f, qualityRadial) +
                                  SliceyMeshBuilder.SizeForRevolvedArc(90f, 90f, qualityRadial, qualityFillet);

            var closedPortionSize = new SliceyMeshBuilder.SliceyCursor();
            if (portionClosed)
            {
                var roundQuadRectFilletSize = SliceyMeshBuilder.SizeForStrip(10) +
                                              SliceyMeshBuilder.SizeForFan(90f, qualityFillet);

                if (portion.X != SliceyMeshPortionAxis.Both)
                    closedPortionSize += roundQuadRectFilletSize;
                if (portion.Y != SliceyMeshPortionAxis.Both)
                    closedPortionSize += roundQuadRectFilletSize;
                if (portion.Z != SliceyMeshPortionAxis.Both)
                    closedPortionSize += SliceyMeshBuilder.SizeForFan(90f, qualityRadial);
            }
            var builder = SliceyMeshBuilder.Begin((openPortionSize + closedPortionSize) * sizeFactor);
            AddCylinderRoundEdgesOctant(ref builder, qualityRadial, qualityFillet);
            if (portionClosed)
                CloseCylinderRoundEdgesOctant(ref builder, portion, qualityRadial, qualityFillet);
            
            ApplySymmetry(ref builder, portion, false);
            return builder;
        }


        static void AddCylinderRoundEdgesOctant(ref SliceyMeshBuilder builder, float qualityRadial, float qualityFillet)
        {
            var primaryRadius = 0.5f;
            var filletRadius = 0.25f;
            var depth = 0.5f;
            var cylinderDepth = depth - filletRadius;
            builder.AddCylinder(new Pose(Vector3.back * cylinderDepth, Quaternion.identity), primaryRadius, 90f, qualityRadial, cylinderDepth);
            builder.AddRevolvedArc(new Pose(Vector3.back * depth, Quaternion.identity), primaryRadius, filletRadius, 90f, 90f, qualityRadial, qualityFillet);
            builder.AddFan(new Pose(Vector3.back * depth, Quaternion.identity), primaryRadius - filletRadius, 90f, qualityRadial);
        }
        static void CloseCylinderRoundEdgesOctant(ref SliceyMeshBuilder builder, SliceyMeshPortion portion, float qualityRadial, float qualityFillet)
        {
            var r = 0.5f;
            var filletRadius = 0.25f;
            var f = r - filletRadius;

            if (portion.X != SliceyMeshPortionAxis.Both)
                CloseRoundSidesLeftOctant(ref builder, filletRadius, qualityFillet, f);
            if (portion.Y != SliceyMeshPortionAxis.Both)
                CloseRoundSidesBottomOctant(ref builder, filletRadius, qualityFillet, f);
            if (portion.Z != SliceyMeshPortionAxis.Both)
                builder.AddFan(new Pose(Vector3.zero, Quaternion.Euler(0f, 180f, 90f)), r, 90f, qualityRadial);
        }
    }
}