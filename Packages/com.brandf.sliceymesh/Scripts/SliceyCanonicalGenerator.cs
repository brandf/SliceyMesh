using System;
using UnityEngine;

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
            return portion switch
            {
                SliceyMeshPortion.Full => is2D ? 4 : 8,
                SliceyMeshPortion.Half => is2D ? 2 : 4,
                SliceyMeshPortion.Quadrant => is2D ? 1 : 2,
                SliceyMeshPortion.Octant => 1,
            };
        }

        static void ApplySymmetry(ref SliceyMeshBuilder builder, SliceyMeshPortion portion, bool is2D)
        {
            switch (portion)
            {
                case SliceyMeshPortion.Full:
                    if (is2D) builder.XYSymmetry(); else builder.XYZSymmetry();
                    break;
                case SliceyMeshPortion.Half:
                    if (is2D) builder.XSymmetry(); else builder.XYSymmetry();
                    break;
                case SliceyMeshPortion.Quadrant:
                    if (!is2D) builder.XSymmetry();
                    break;
                case SliceyMeshPortion.Octant:
                    break;
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

        // this isn't really a new type, it's just an optimization for when radius = 0
        public static SliceyMeshBuilder CubeHard(SliceyMeshPortion portion, bool portionClosed)
        {
            var sizeFactor = PortionFactor(portion, false);
            var openPortionSize = SliceyMeshBuilder.SizeForQuad * 3;
            var closedPortionSize = new SliceyMeshBuilder.SliceyCursor();
            if (portionClosed)
            {
                closedPortionSize = portion switch
                {
                    SliceyMeshPortion.Full     => new SliceyMeshBuilder.SliceyCursor(),
                    SliceyMeshPortion.Half     => SliceyMeshBuilder.SizeForQuad,
                    SliceyMeshPortion.Quadrant => SliceyMeshBuilder.SizeForQuad * 2,
                    SliceyMeshPortion.Octant   => SliceyMeshBuilder.SizeForQuad * 3,
                };
            }
            var builder = SliceyMeshBuilder.Begin((openPortionSize + closedPortionSize) * sizeFactor);
            AddCubeHardOctant(ref builder);
            if (portionClosed && portion != SliceyMeshPortion.Full)
            {
                CloseCubeHardOctant(ref builder, portion);
            }
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
            var h = 0.5f;
            builder.AddQuad(new Vector3(0, 0, 0f),
                            new Vector3(h, 0, 0f),
                            new Vector3(h, h, 0f),
                            new Vector3(0, h, 0f), Vector3.forward);                   // center quarter face
            if (portion >= SliceyMeshPortion.Quadrant)
            {
                CloseQuadBottomOctant(ref builder);
            }
            if (portion >= SliceyMeshPortion.Octant)
            {
                CloseQuadLeftOctant(ref builder);
            }
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
                if (portion >= SliceyMeshPortion.Half)
                {
                    closedPortionSize += SliceyMeshBuilder.SizeForStrip(10) +
                                         SliceyMeshBuilder.SizeForFan(90f, quality);
                }
                if (portion >= SliceyMeshPortion.Quadrant)
                {
                    closedPortionSize += SliceyMeshBuilder.SizeForQuad;
                }
                if (portion >= SliceyMeshPortion.Octant)
                {
                    closedPortionSize += SliceyMeshBuilder.SizeForQuad;
                }
            }
            var builder = SliceyMeshBuilder.Begin((openPortionSize + closedPortionSize) * sizeFactor);
            AddCubeRoundSidesOctant(ref builder, quality);
            if (portionClosed && portion != SliceyMeshPortion.Full)
            {
                CloseCubeRoundSidesOctant(ref builder, portion, quality);
            }
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
            builder.AddFan(new Pose(new Vector3(q, q, 0), Quaternion.Euler(0,180f,90f)), q, 90f, quality); // rounded corner

            if (portion >= SliceyMeshPortion.Quadrant)
            {
                CloseQuadBottomOctant(ref builder);
            }
            if (portion >= SliceyMeshPortion.Octant)
            {
                CloseQuadLeftOctant(ref builder);
            }
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
                if (portion >= SliceyMeshPortion.Half)
                {
                    closedPortionSize += roundQuadRectSize;
                }
                if (portion >= SliceyMeshPortion.Quadrant)
                {
                    closedPortionSize += roundQuadRectSize;
                }
                if (portion >= SliceyMeshPortion.Octant)
                {
                    closedPortionSize += roundQuadRectSize;
                }
            }
            var builder = SliceyMeshBuilder.Begin((openPortionSize + closedPortionSize) * sizeFactor);
            AddCubeRoundEdgesOctant(ref builder, quality);
            if (portionClosed && portion != SliceyMeshPortion.Full)
            {
                CloseCubeRoundEdgesOctant(ref builder, portion, quality);
            }
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
            var h = 0.5f;
            var q = 0.25f;
            var initial = builder.Cursor;
            builder.StripStart(new Vector3(q, h, 0), // duplicate vert for different normal
                               new Vector3(0, h, 0), Vector3.forward);
            builder.StripTo(new Vector3(q, q, 0),
                            new Vector3(0, q, 0), Vector3.forward);
            builder.StripTo(new Vector3(0, 0, 0), Vector3.forward);
            var afterStrip = builder.Cursor;
            builder.CopyReflection(initial, afterStrip, DiagXY);
            builder.AddFan(new Pose(new Vector3(q, q, 0), Quaternion.Euler(0, 180f, 90f)), q, 90f, quality); // rounded corner

            if (portion >= SliceyMeshPortion.Quadrant)
            {
                CloseRoundSidesBottomOctant(ref builder, q, quality, q);
            }
            if (portion >= SliceyMeshPortion.Octant)
            {
                CloseRoundSidesLeftOctant(ref builder, q, quality, q);
            }
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
                if (portion >= SliceyMeshPortion.Half)
                {
                    closedPortionSize += SliceyMeshBuilder.SizeForStrip(10) +
                                         SliceyMeshBuilder.SizeForFan(90f, qualitySides);
                }
                if (portion >= SliceyMeshPortion.Quadrant)
                {
                    closedPortionSize += roundQuadRectFilletSize;
                }
                if (portion >= SliceyMeshPortion.Octant)
                {
                    closedPortionSize += roundQuadRectFilletSize;
                }
            }
            var builder = SliceyMeshBuilder.Begin((openPortionSize + closedPortionSize) * sizeFactor);
            AddCubeRoundSidesFilletOctant(ref builder, filletRadius, qualitySides, qualityFillet);
            if (portionClosed && portion != SliceyMeshPortion.Full)
            {
                CloseCubeRoundSidesFilletOctant(ref builder, portion, filletRadius, qualitySides, qualityFillet);
            }
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
            var q = 0.25f;
            var f = h - filletRadius;
            var initial = builder.Cursor;
            builder.StripStart(new Vector3(q, h, 0), // duplicate vert for different normal
                               new Vector3(0, h, 0), Vector3.forward);
            builder.StripTo(new Vector3(q, q, 0),
                            new Vector3(0, q, 0), Vector3.forward);
            builder.StripTo(new Vector3(0, 0, 0), Vector3.forward);
            var afterStrip = builder.Cursor;
            builder.CopyReflection(initial, afterStrip, DiagXY);
            builder.AddFan(new Pose(new Vector3(q, q, 0), Quaternion.Euler(0, 180f, 90f)), q, 90f, qualitySides); // rounded corner

            if (portion >= SliceyMeshPortion.Quadrant)
            {
                CloseRoundSidesBottomOctant(ref builder, filletRadius, qualityFillet, f);
            }
            if (portion >= SliceyMeshPortion.Octant)
            {
                CloseRoundSidesLeftOctant(ref builder, filletRadius, qualityFillet, f);
            }
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
                if (portion >= SliceyMeshPortion.Half)
                {
                    closedPortionSize += SliceyMeshBuilder.SizeForFan(90f, qualityRadial);
                }
                if (portion >= SliceyMeshPortion.Quadrant)
                {
                    closedPortionSize += SliceyMeshBuilder.SizeForQuad;
                }
                if (portion >= SliceyMeshPortion.Octant)
                {
                    closedPortionSize += SliceyMeshBuilder.SizeForQuad;
                }
            }
            var builder = SliceyMeshBuilder.Begin((openPortionSize + closedPortionSize) * sizeFactor);
            AddCylinderHardOctant(ref builder, qualityRadial);
            if (portionClosed && portion != SliceyMeshPortion.Full)
            {
                CloseCylinderHardOctant(ref builder, portion, qualityRadial);
            }
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
            builder.AddFan(new Pose(Vector3.zero, Quaternion.Euler(0f, 180f, 90f)), r, 90f, qualityRadial);
            if (portion >= SliceyMeshPortion.Quadrant)
            {
                CloseQuadBottomOctant(ref builder);
            }
            if (portion >= SliceyMeshPortion.Octant)
            {
                CloseQuadLeftOctant(ref builder);
            }
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
                if (portion >= SliceyMeshPortion.Half)
                {
                    closedPortionSize += SliceyMeshBuilder.SizeForFan(90f, qualityRadial);
                }
                var roundQuadRectFilletSize = SliceyMeshBuilder.SizeForStrip(10) +
                                              SliceyMeshBuilder.SizeForFan(90f, qualityFillet);
                if (portion >= SliceyMeshPortion.Quadrant)
                {
                    closedPortionSize += roundQuadRectFilletSize;
                }
                if (portion >= SliceyMeshPortion.Octant)
                {
                    closedPortionSize += roundQuadRectFilletSize;
                }
            }
            var builder = SliceyMeshBuilder.Begin((openPortionSize + closedPortionSize) * sizeFactor);
            AddCylinderRoundEdgesOctant(ref builder, qualityRadial, qualityFillet);
            if (portionClosed && portion != SliceyMeshPortion.Full)
            {
                CloseCylinderRoundEdgesOctant(ref builder, portion, qualityRadial, qualityFillet);
            }
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
            builder.AddFan(new Pose(Vector3.zero, Quaternion.Euler(0f, 180f, 90f)), r, 90f, qualityRadial);
            if (portion >= SliceyMeshPortion.Quadrant)
            {
                CloseRoundSidesBottomOctant(ref builder, filletRadius, qualityFillet, f);
            }
            if (portion >= SliceyMeshPortion.Octant)
            {
                CloseRoundSidesLeftOctant(ref builder, filletRadius, qualityFillet, f);
            }
        }
    }
}