using System;
using System.Buffers;
using UnityEngine;

namespace SliceyMesh
{
    public struct SliceyMeshBuilder
    {
        Vector3[] vertices;
        Vector3[] normals;
        int[] indices;
        SliceyCursor _size;
        SliceyCursor _offset;

        public struct SliceyCursor
        {
            public int vertex;
            public int index;

            public static implicit operator SliceyCursor((int vertex, int index) t) => new SliceyCursor() { vertex = t.vertex, index = t.index };
            public void Deconstruct(out int vertex, out int index) { vertex = this.vertex; index = this.index; }

            public static SliceyCursor operator +(SliceyCursor a, SliceyCursor b) => (a.vertex + b.vertex, a.index + b.index);
            public static SliceyCursor operator *(SliceyCursor a, int b) => (a.vertex * b, a.index * b);
        }

        public SliceyCursor Cursor => _offset;
        public SliceyCursor Beginning => new SliceyCursor();


        public static readonly SliceyCursor SizeForTri = (3, 3);
        public static readonly SliceyCursor SizeForQuad = (4, 6);

        public static SliceyCursor SizeForStrip(int verts) => (verts, (verts - 2) * 3);
        public static SliceyCursor SizeForQuadStrip(int segments) => (2 * segments + 2, 6 * segments);

        public static int SegmentsForAngle(float angle, float quality) => Mathf.Max(1, 1 + Mathf.FloorToInt(quality * Mathf.Abs(angle) / 10f));
        public static float QualityForSegments(float angle, int segments) => (segments - 1) * 10 / Mathf.Abs(angle);
        public static SliceyCursor SizeForFan(float angle, float quality) => SizeForTri * SegmentsForAngle(angle, quality);
        public static SliceyCursor SizeForCylinder(float angle, float quality) => SizeForQuadStrip(SegmentsForAngle(angle, quality));
        public static SliceyCursor SizeForCorner3(float angle, float quality)
        {
            SliceyCursor size = (0, 0);

            var edgSegments = SegmentsForAngle(angle, quality);
            for (int segments = edgSegments; segments > 0; segments--)
            {
                size += SizeForStrip(2 * segments + 1);
            }

            return size;
        }


        internal SliceyMeshBuilder Clone()
        {
            var clone = new SliceyMeshBuilder()
            {
                vertices = ArrayPool<Vector3>.Shared.Rent(vertices.Length),
                normals = ArrayPool<Vector3>.Shared.Rent(normals.Length),
                indices = ArrayPool<int>.Shared.Rent(indices.Length),
                _size = _size,
                _offset = _offset,
            };
            vertices.CopyTo(clone.vertices.AsSpan());
            normals.CopyTo(clone.normals.AsSpan());
            indices.CopyTo(clone.indices.AsSpan());
            return clone;
        }

        internal SliceyMeshBuilder Clone(int sizeFactor)
        {
            var clone = new SliceyMeshBuilder()
            {
                vertices = ArrayPool<Vector3>.Shared.Rent(_size.vertex * sizeFactor),
                normals = ArrayPool<Vector3>.Shared.Rent(_size.vertex * sizeFactor),
                indices = ArrayPool<int>.Shared.Rent(_size.index * sizeFactor),
                _size = _size * sizeFactor,
                _offset = _offset,
            };
            Array.Copy(vertices, clone.vertices, _offset.vertex);
            Array.Copy(normals, clone.normals, _offset.vertex);
            Array.Copy(indices, clone.indices, _offset.index);
            return clone;
        }

        public static SliceyMeshBuilder Begin(SliceyCursor size) => new SliceyMeshBuilder()
        {
            vertices = ArrayPool<Vector3>.Shared.Rent(size.vertex),
            normals = ArrayPool<Vector3>.Shared.Rent(size.vertex),
            indices = ArrayPool<int>.Shared.Rent(size.index),
            _size = size
        };

        public Mesh End()
        {
            var mesh = new Mesh();
            mesh.SetVertices(vertices, 0, _offset.vertex);
            mesh.SetNormals(normals, 0, _offset.vertex);
            mesh.SetTriangles(indices, 0, _offset.index, 0, true, 0);

            ArrayPool<Vector3>.Shared.Return(vertices);
            ArrayPool<Vector3>.Shared.Return(normals);
            ArrayPool<int>.Shared.Return(indices);
            return mesh;
        }

        public void SliceMesh27(Vector3 halfSizeInsideSource, Vector3 halfSizeInsideTarget, Pose pose)
        {
            SliceMesh27(Beginning, _offset, halfSizeInsideSource, halfSizeInsideTarget, pose);
        }
        public void SliceMesh27(SliceyCursor start, SliceyCursor end, Vector3 halfSizeInsideSource, Vector3 halfSizeInsideTarget, Pose pose)
        {
            var rotation = pose.rotation;
            var position = pose.position;
            var defaultRotation = rotation == Quaternion.identity;
            var defaultPosition = position == Vector3.zero;
            
            // expand all combinations outside the loop to minimize inner-loop work
            if (defaultRotation && defaultPosition)
            {
                for (var svo = start.vertex; svo < end.vertex; svo++)
                {
                    vertices[svo] = Slice27(vertices[svo], halfSizeInsideSource, halfSizeInsideTarget);
                }
            }
            else if (!defaultRotation && defaultPosition)
            {
                for (var svo = start.vertex; svo < end.vertex; svo++)
                {
                    vertices[svo] = rotation * Slice27(vertices[svo], halfSizeInsideSource, halfSizeInsideTarget);
                    normals[svo] = rotation * normals[svo];
                }
            }
            else if (defaultRotation && !defaultPosition)
            {
                for (var svo = start.vertex; svo < end.vertex; svo++)
                {
                    vertices[svo] = Slice27(vertices[svo], halfSizeInsideSource, halfSizeInsideTarget) + position;
                }
            }
            else // both not default
            {
                for (var svo = start.vertex; svo < end.vertex; svo++)
                {
                    vertices[svo] = rotation * Slice27(vertices[svo], halfSizeInsideSource, halfSizeInsideTarget) + position;
                    normals[svo] = rotation * normals[svo];
                }
            }
        }


        public void SliceMesh256(Vector3 halfSizeInsideSource, Vector3 halfSizeOutsideSource, Vector3 halfSizeInsideTarget, Vector3 halfSizeOutsideTarget, Pose pose)
        {
            SliceMesh256(Beginning, _offset, halfSizeInsideSource, halfSizeOutsideSource, halfSizeInsideTarget, halfSizeOutsideTarget, pose);
        }

        public void SliceMesh256(SliceyCursor start, SliceyCursor end, Vector3 halfSizeInsideSource, Vector3 halfSizeOutsideSource, Vector3 halfSizeInsideTarget, Vector3 halfSizeOutsideTarget, Pose pose)
        {
            var rotation = pose.rotation;
            var position = pose.position;

            var defaultRotation = rotation == Quaternion.identity;
            var defaultPosition = position == Vector3.zero;

            // expand all combinations outside the loop to minimize inner-loop work
            if (defaultRotation && defaultPosition)
            {
                for (var svo = start.vertex; svo < end.vertex; svo++)
                {
                    vertices[svo] = Slice256(vertices[svo], halfSizeInsideSource, halfSizeOutsideSource, halfSizeInsideTarget, halfSizeOutsideTarget);
                }
            }
            else if (!defaultRotation && defaultPosition)
            {
                for (var svo = start.vertex; svo < end.vertex; svo++)
                {
                    vertices[svo] = rotation * Slice256(vertices[svo], halfSizeInsideSource, halfSizeOutsideSource, halfSizeInsideTarget, halfSizeOutsideTarget);
                    normals[svo] = rotation * normals[svo];
                }
            }
            else if (defaultRotation && !defaultPosition)
            {
                for (var svo = start.vertex; svo < end.vertex; svo++)
                {
                    vertices[svo] = Slice256(vertices[svo], halfSizeInsideSource, halfSizeOutsideSource, halfSizeInsideTarget, halfSizeOutsideTarget) + position;
                }
            }
            else // both not default
            {
                for (var svo = start.vertex; svo < end.vertex; svo++)
                {
                    vertices[svo] = rotation * Slice256(vertices[svo], halfSizeInsideSource, halfSizeOutsideSource, halfSizeInsideTarget, halfSizeOutsideTarget) + position;
                    normals[svo] = rotation * normals[svo];
                }
            }
        }


        Vector3 Slice27(Vector3 v, Vector3 halfSizeInsideSource, Vector3 halfSizeInsideTarget) => new Vector3(Slice3(v.x, halfSizeInsideSource.x, halfSizeInsideTarget.x),
                                                                                                              Slice3(v.y, halfSizeInsideSource.y, halfSizeInsideTarget.y),
                                                                                                              Slice3(v.z, halfSizeInsideSource.z, halfSizeInsideTarget.z));

        Vector3 Slice256(Vector3 v, Vector3 halfSizeInsideSource, Vector3 halfSizeOutsideSource, Vector3 halfSizeInsideTarget, Vector3 halfSizeOutsideTarget) => new Vector3(Slice4(v.x, halfSizeInsideSource.x, halfSizeOutsideSource.x, halfSizeInsideTarget.x, halfSizeOutsideTarget.x),
                                                                                                                                                                             Slice4(v.y, halfSizeInsideSource.y, halfSizeOutsideSource.y, halfSizeInsideTarget.y, halfSizeOutsideTarget.y),
                                                                                                                                                                             Slice4(v.z, halfSizeInsideSource.z, halfSizeOutsideSource.z, halfSizeInsideTarget.z, halfSizeOutsideTarget.z));

        float Slice3(float p, float s, float t)
        {
            var sign = Mathf.Sign(p);
            p = Mathf.Abs(p);
            if (p < s) // inside = stretch
            {
                p = p / s * t;
            }
            else // outside = offset
            {
                p = t + (p - s);
            }
            return sign * p; // re-apply sign
        }

        float Slice4(float p, float si, float so, float ti, float to)
        {
            var sign = Mathf.Sign(p);
            p = Mathf.Abs(p);
            if (p < si) // inside = stretch
            {
                p = p / si * ti;
            }
            else if (p < so) // between = lerp
            {
                p = (p - si) / (so - si) * (to - ti) + ti;
            }
            else // outside = offset
            {
                p = to + (p - so);
            }
            return sign * p; // re-apply sign
        }


        public Vector3 Reflect(Vector3 point, Vector3 normal) => point - 2 * Vector3.Dot(point, normal) * normal;
        public void CopyReflection(SliceyCursor start, SliceyCursor end, Vector3 normal)
        {
            var (vo, to) = _offset;
            var voOffset = vo - start.vertex;
            for (var svo = start.vertex; svo < end.vertex; svo++, vo++)
            {
                vertices[vo] = Reflect(vertices[svo], normal);
                normals[vo] = Reflect(normals[svo], normal);
            }

            for (var sto = start.index; sto < end.index; sto += 3, to += 3)
            {
                indices[to] = indices[sto] + voOffset;
                indices[to + 1] = indices[sto + 2] + voOffset;
                indices[to + 2] = indices[sto + 1] + voOffset;
            }
            _offset = (vo, to);
        }

        public void Reflect(SliceyCursor start, SliceyCursor end, Vector3 normal)
        {
            for (var svo = start.vertex; svo < end.vertex; svo++)
            {
                vertices[svo] = Reflect(vertices[svo], normal);
                normals[svo] = Reflect(normals[svo], normal);
            }

            for (var sto = start.index; sto < end.index; sto += 3)
            {
                var temp = indices[sto + 1];
                indices[sto + 1] = indices[sto + 2];
                indices[sto + 2] = temp;
            }
        }

        public Vector3 Reflect(Vector3 point, Plane plane) => point - 2 * plane.GetDistanceToPoint(point) * plane.normal;
        public void CopyReflection(SliceyCursor start, SliceyCursor end, Plane plane)
        {
            var (vo, to) = _offset;
            var voOffset = vo - start.vertex;
            for (var svo = start.vertex; svo < end.vertex; svo++, vo++)
            {
                vertices[vo] = Reflect(vertices[svo], plane);
                normals[vo] = Reflect(normals[svo], plane.normal);
            }

            for (var sto = start.index; sto < end.index; sto += 3, to += 3)
            {
                indices[to] = indices[sto] + voOffset;
                indices[to + 1] = indices[sto + 2] + voOffset;
                indices[to + 2] = indices[sto + 1] + voOffset;
            }
            _offset = (vo, to);
        }


        public void Copy(SliceyCursor start, SliceyCursor end)
        {
            var (vo, to) = _offset;
            var voOffset = vo - start.vertex;
            for (var svo = start.vertex; svo < end.vertex; svo++, vo++)
            {
                vertices[vo] = vertices[svo];
                normals[vo] = normals[svo];
            }

            for (var sto = start.index; sto < end.index; sto += 3, to += 3)
            {
                indices[to] = indices[sto] + voOffset;
                indices[to + 1] = indices[sto + 1] + voOffset;
                indices[to + 2] = indices[sto + 2] + voOffset;
            }
            _offset = (vo, to);
        }

        public void ReverseFaces(SliceyCursor start, SliceyCursor end)
        {
            // reverse normals
            for (var svo = start.vertex; svo < end.vertex; svo++)
            {
                normals[svo] = -normals[svo];
            }

            // reverse winding direction
            for (var sto = start.index; sto < end.index; sto += 3)
            {
                indices[sto] = indices[sto];
                var temp = indices[sto + 1];
                indices[sto + 1] = indices[sto + 2];
                indices[sto + 2] = temp;
            }
        }

        public void XSymmetry() => CopyReflection(Beginning, _offset, Vector3.right);
        public void YSymmetry() => CopyReflection(Beginning, _offset, Vector3.up);
        public void ZSymmetry() => CopyReflection(Beginning, _offset, Vector3.forward);

        public void XYSymmetry()
        {
            XSymmetry();
            YSymmetry();
        }
        public void XYZSymmetry()
        {
            XSymmetry();
            YSymmetry();
            ZSymmetry();
        }

        public void CopyRotation(SliceyCursor start, SliceyCursor end, Quaternion rotation)
        {
            var (vo, to) = _offset;
            var initialVO = vo;
            for (var svo = start.vertex; svo < end.vertex; svo++, vo++)
            {
                vertices[vo] = rotation * vertices[svo];
                normals[vo] = rotation * normals[svo];
            }

            for (var sto = start.index; sto < end.index; sto++, to++)
            {
                indices[to] = indices[sto] + initialVO;
            }
            _offset = (vo, to);
        }

        public void AddTri(Vector3 v1, Vector3 v2, Vector3 v3, Vector3 n)
        {
            var (vo, to) = _offset;
            vertices[vo] = v1;
            vertices[vo + 1] = v2;
            vertices[vo + 2] = v3;
            normals[vo] = n;
            normals[vo + 1] = n;
            normals[vo + 2] = n;
            indices[to] = vo;
            indices[to + 1] = vo + 1;
            indices[to + 2] = vo + 2;
            _offset += SizeForTri;
        }

        public void AddTri(Vector3 v1, Vector3 v2, Vector3 v3, Vector3 n1, Vector3 n2, Vector3 n3)
        {
            var (vo, to) = _offset;
            vertices[vo] = v1;
            vertices[vo + 1] = v2;
            vertices[vo + 2] = v3;
            normals[vo] = n1;
            normals[vo + 1] = n2;
            normals[vo + 2] = n3;
            indices[to] = vo;
            indices[to + 1] = vo + 1;
            indices[to + 2] = vo + 2;
            _offset += SizeForTri;
        }

        public void AddFan(Pose p, float radius, float spreadAngle, float quality)
        {
            var segments = SegmentsForAngle(spreadAngle, quality);
            var deltaAngle = spreadAngle / segments;
            var center = p.position;
            var normal = p.rotation * Vector3.back;

            var deltaRot = p.rotation * Quaternion.AngleAxis(deltaAngle, Vector3.forward) * Quaternion.Inverse(p.rotation);
            var cprev = p.rotation * Vector3.right * radius;
            var prev = center + cprev;
            for (var i = 0; i < segments; ++i)
            {
                var ccur = deltaRot * cprev;
                var cur = center + ccur;
                AddTri(center, prev, cur, normal);
                cprev = ccur;
                prev = cur;
            }
        }

        public void AddCylinder(Pose p, float radius, float spreadAngle, float quality, float depth)
        {
            var segments = SegmentsForAngle(spreadAngle, quality);
            var deltaAngle = spreadAngle / segments;
            var center = p.position;
            var depthOffset = p.rotation * Vector3.forward * depth;

            var deltaRot = p.rotation * Quaternion.AngleAxis(deltaAngle, Vector3.forward) * Quaternion.Inverse(p.rotation);
            var invRadius = 1f / radius;
            var offset = p.rotation * Vector3.right * radius;
            var normal = offset * invRadius;
            var point = center + offset;
            StripStart(point + depthOffset, point, normal);
            for (var i = 0; i < segments; ++i)
            {
                offset = deltaRot * offset;
                normal = offset * invRadius;
                point = center + offset;
                StripTo(point + depthOffset, point, normal);
            }
        }

        public void AddCorner3(Pose p, float radius, float spreadAngle, float quality)
        {
            var edgSegments = SegmentsForAngle(spreadAngle, quality);
            var center = p.position;
            var invRadius = 1f / radius;

            var initialLocalPoint = Vector3.right * radius;
            var localDeltaAngleUp = spreadAngle / edgSegments;
            var localDeltaUp = Quaternion.AngleAxis(localDeltaAngleUp, Vector3.up);

            for (int segments = edgSegments; segments > 0; segments--)
            {
                var nextSegments = segments - 1;
                var localDeltaAngleForward = spreadAngle / segments;
                var localDeltaAngleForwardNext = spreadAngle / nextSegments;
                var localDeltaForward = Quaternion.AngleAxis(localDeltaAngleForward, Vector3.forward);
                var localDeltaForwardNext = Quaternion.AngleAxis(localDeltaAngleForwardNext, Vector3.forward);

                var localPoint = initialLocalPoint;
                var initialLocalPointNext = localDeltaUp * initialLocalPoint;
                var localPointNext = initialLocalPointNext;

                var offset1 = p.rotation * localPoint;
                var normal1 = offset1 * invRadius;
                var point1 = center + offset1;

                var offset2 = p.rotation * localPointNext;
                var normal2 = offset2 * invRadius;
                var point2 = center + offset2;

                StripStart(point1, point2, normal1, normal2);

                for (var i = 0; i < nextSegments; ++i)
                {
                    localPoint = localDeltaForward * localPoint;
                    localPointNext = localDeltaForwardNext * localPointNext;

                    offset1 = p.rotation * localPoint;
                    normal1 = offset1 * invRadius;
                    point1 = center + offset1;

                    offset2 = p.rotation * localPointNext;
                    normal2 = offset2 * invRadius;
                    point2 = center + offset2;

                    StripTo(point1, point2, normal1, normal2);
                }


                localPoint = localDeltaForward * localPoint;
                offset1 = p.rotation * localPoint;
                normal1 = offset1 * invRadius;
                point1 = center + offset1;
                StripTo(point1, normal1);

                initialLocalPoint = initialLocalPointNext;
            }
        }

        public void AddQuad(Pose p, Vector2 halfSize)
        {
            var center = p.position;
            var up = p.rotation * Vector3.up;
            var right = p.rotation * Vector3.right;
            var v1 = center + right * halfSize.x + up * halfSize.y;
            var v2 = center + right * halfSize.x - up * halfSize.y;
            var v3 = center - right * halfSize.x - up * halfSize.y;
            var v4 = center - right * halfSize.x + up * halfSize.y;
            var n = Vector3.Cross(up, right);
            AddQuad(v1, v2, v3, v4, n);
        }

        public void StripStart(Vector3 v1, Vector3 v2, Vector3 n) => StripStart(v1, v2, n, n);
        public void StripStart(Vector3 v1, Vector3 v2, Vector3 n1, Vector3 n2)
        {
            var vo = _offset.vertex;
            vertices[vo] = v1;
            vertices[vo + 1] = v2;
            normals[vo] = n1;
            normals[vo + 1] = n2;
            _offset.vertex = vo + 2;
        }

        public void StripTo(Vector3 v1, Vector3 v2, Vector3 n, bool odd = false) => StripTo(v1, v2, n, n, odd);

        public void StripTo(Vector3 v1, Vector3 v2, Vector3 n1, Vector3 n2, bool odd = false)
        {
            var (vo, to) = _offset;
            vertices[vo] = v1;
            vertices[vo + 1] = v2;
            normals[vo] = n1;
            normals[vo + 1] = n2;

            if (odd)
            {
                indices[to] = vo - 2;
                indices[to + 1] = vo - 1;
                indices[to + 2] = vo + 1;

                indices[to + 3] = vo - 2;
                indices[to + 4] = vo + 1;
                indices[to + 5] = vo;
            }
            else
            {
                indices[to] = vo - 2;
                indices[to + 1] = vo - 1;
                indices[to + 2] = vo;

                indices[to + 3] = vo - 1;
                indices[to + 4] = vo + 1;
                indices[to + 5] = vo;
            }
            _offset = (vo + 2, to + 6);
        }

        public void StripTo(Vector3 v, Vector3 n, bool odd = false)
        {
            var (vo, to) = _offset;
            vertices[vo] = v;
            normals[vo] = n;

            if (odd)
            {
                indices[to] = vo - 1;
                indices[to + 1] = vo - 2;
                indices[to + 2] = vo;
            }
            else
            {
                indices[to] = vo - 2;
                indices[to + 1] = vo - 1;
                indices[to + 2] = vo;
            }

            _offset = (vo + 1, to + 3);
        }

        public SliceyCursor FanStart(Vector3 v1, Vector3 v2, Vector3 n) => FanStart(v1, v2, n, n);
        public SliceyCursor FanStart(Vector3 v1, Vector3 v2, Vector3 n1, Vector3 n2)
        {
            var fanCursor = _offset;
            var vo = fanCursor.vertex;
            vertices[vo] = v1;
            vertices[vo + 1] = v2;
            normals[vo] = n1;
            normals[vo + 1] = n2;
            _offset.vertex = vo + 2;
            return fanCursor;
        }

        public void FanTo(SliceyCursor fanCursor, Vector3 v, Vector3 n)
        {
            var (vo, io) = _offset;
            vertices[vo] = v;
            normals[vo] = n;
            indices[io] = fanCursor.vertex;
            indices[io + 1] = vo - 1;
            indices[io + 2] = vo;
            _offset = (vo + 1, io + 3);
        }

        /// <summary>
        /// Clockwise winding while looking at the front face
        /// </summary>
        /// <param name="v1"></param>
        /// <param name="v2"></param>
        /// <param name="v3"></param>
        /// <param name="v4"></param>
        /// <param name="n"></param>
        public void AddQuad(Vector3 v1, Vector3 v2, Vector3 v3, Vector3 v4, Vector3 n)
        {
            var (vo, to) = _offset;
            vertices[vo] = v1;
            vertices[vo + 1] = v2;
            vertices[vo + 2] = v3;
            vertices[vo + 3] = v4;
            normals[vo] = n;
            normals[vo + 1] = n;
            normals[vo + 2] = n;
            normals[vo + 3] = n;
            indices[to] = vo;
            indices[to + 1] = vo + 1;
            indices[to + 2] = vo + 2;
            indices[to + 3] = vo;
            indices[to + 4] = vo + 2;
            indices[to + 5] = vo + 3;
            _offset += SizeForQuad;
        }

        public void AddQuad(Vector3 v1, Vector3 v2, Vector3 v3, Vector3 v4, Vector3 n1, Vector3 n2, Vector3 n3, Vector3 n4)
        {
            var (vo, to) = _offset;
            vertices[vo] = v1;
            vertices[vo + 1] = v2;
            vertices[vo + 2] = v3;
            vertices[vo + 3] = v4;
            normals[vo] = n1;
            normals[vo + 1] = n1;
            normals[vo + 2] = n1;
            normals[vo + 3] = n1;
            indices[to] = vo;
            indices[to + 1] = vo + 1;
            indices[to + 2] = vo + 2;
            indices[to + 3] = vo;
            indices[to + 4] = vo + 2;
            indices[to + 5] = vo + 3;
            _offset += SizeForQuad;
        }
    }
}