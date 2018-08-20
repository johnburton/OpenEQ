﻿/* Copyright (C) <2009-2011> <Thorben Linneweber, Jitter Physics>
* 
*  This software is provided 'as-is', without any express or implied
*  warranty.  In no event will the authors be held liable for any damages
*  arising from the use of this software.
*
*  Permission is granted to anyone to use this software for any purpose,
*  including commercial applications, and to alter it and redistribute it
*  freely, subject to the following restrictions:
*
*  1. The origin of this software must not be misrepresented; you must not
*      claim that you wrote the original software. If you use this software
*      in a product, an acknowledgment in the product documentation would be
*      appreciated but is not required.
*  2. Altered source versions must be plainly marked as such, and must not be
*      misrepresented as being the original software.
*  3. This notice may not be removed or altered from any source distribution. 
*/

#region Using Statements

using System.Numerics;

#endregion

namespace Jitter.LinearMath {
    /// <summary>
    ///     Bounding Box defined through min and max vectors. Member
    ///     of the math namespace, so every method has it's 'by reference'
    ///     equivalent to speed up time critical math operations.
    /// </summary>
    public struct JBBox {
        /// <summary>
        ///     Containment type used within the <see cref="JBBox" /> structure.
        /// </summary>
        public enum ContainmentType {
            /// <summary>
            ///     The objects don't intersect.
            /// </summary>
            Disjoint,

            /// <summary>
            ///     One object is within the other.
            /// </summary>
            Contains,

            /// <summary>
            ///     The two objects intersect.
            /// </summary>
            Intersects
		}

        /// <summary>
        ///     The maximum point of the box.
        /// </summary>
        public Vector3 Min;

        /// <summary>
        ///     The minimum point of the box.
        /// </summary>
        public Vector3 Max;

        /// <summary>
        ///     Returns the largest box possible.
        /// </summary>
        public static readonly JBBox LargeBox;

        /// <summary>
        ///     Returns the smalltest box possible.
        /// </summary>
        public static readonly JBBox SmallBox;

		static JBBox() {
			LargeBox.Min = new Vector3(float.MinValue);
			LargeBox.Max = new Vector3(float.MaxValue);
			SmallBox.Min = new Vector3(float.MaxValue);
			SmallBox.Max = new Vector3(float.MinValue);
		}

        /// <summary>
        ///     Constructor
        /// </summary>
        /// <param name="min">The minimum point of the box.</param>
        /// <param name="max">The maximum point of the box.</param>
        public JBBox(Vector3 min, Vector3 max) {
			Min = min;
			Max = max;
		}

        /// <summary>
        ///     Transforms the bounding box into the space given by orientation and position.
        /// </summary>
        /// <param name="position"></param>
        /// <param name="orientation"></param>
        /// <param name="result"></param>
        internal void InverseTransform(ref Vector3 position, ref JMatrix orientation) {
			Max = Max - position;
			Min = Min - position;

			Vector3 center;
			center = Max + Min;
			center.X *= 0.5f;
			center.Y *= 0.5f;
			center.Z *= 0.5f;

			Vector3 halfExtents;
			halfExtents = Max - Min;
			halfExtents.X *= 0.5f;
			halfExtents.Y *= 0.5f;
			halfExtents.Z *= 0.5f;

			center = center.TransposedTransform(ref orientation);

			JMatrix abs;
			JMath.Absolute(ref orientation, out abs);
			halfExtents = halfExtents.TransposedTransform(ref abs);

			Max = center + halfExtents;
			Min = center - halfExtents;
		}

		public void Transform(ref JMatrix orientation) {
			var halfExtents = 0.5f * (Max - Min);
			var center = 0.5f * (Max + Min);

			center = center.Transform(ref orientation);

			JMatrix abs;
			JMath.Absolute(ref orientation, out abs);
			halfExtents = halfExtents.Transform(ref abs);

			Max = center + halfExtents;
			Min = center - halfExtents;
		}

        /// <summary>
        ///     Checks whether a point is inside, outside or intersecting
        ///     a point.
        /// </summary>
        /// <returns>The ContainmentType of the point.</returns>

        #region public Ray/Segment Intersection

		bool Intersect1D(float start, float dir, float min, float max,
			ref float enter, ref float exit) {
			if(dir * dir < JMath.Epsilon * JMath.Epsilon) return start >= min && start <= max;

			var t0 = (min - start) / dir;
			var t1 = (max - start) / dir;

			if(t0 > t1) {
				var tmp = t0;
				t0 = t1;
				t1 = tmp;
			}

			if(t0 > exit || t1 < enter) return false;

			if(t0 > enter) enter = t0;
			if(t1 < exit) exit = t1;
			return true;
		}


		public bool SegmentIntersect(ref Vector3 origin, ref Vector3 direction) {
			float enter = 0.0f, exit = 1.0f;

			if(!Intersect1D(origin.X, direction.X, Min.X, Max.X, ref enter, ref exit))
				return false;

			if(!Intersect1D(origin.Y, direction.Y, Min.Y, Max.Y, ref enter, ref exit))
				return false;

			if(!Intersect1D(origin.Z, direction.Z, Min.Z, Max.Z, ref enter, ref exit))
				return false;

			return true;
		}

		public bool RayIntersect(ref Vector3 origin, ref Vector3 direction) {
			float enter = 0.0f, exit = float.MaxValue;

			if(!Intersect1D(origin.X, direction.X, Min.X, Max.X, ref enter, ref exit))
				return false;

			if(!Intersect1D(origin.Y, direction.Y, Min.Y, Max.Y, ref enter, ref exit))
				return false;

			if(!Intersect1D(origin.Z, direction.Z, Min.Z, Max.Z, ref enter, ref exit))
				return false;

			return true;
		}

		public bool SegmentIntersect(Vector3 origin, Vector3 direction) => SegmentIntersect(ref origin, ref direction);

		public bool RayIntersect(Vector3 origin, Vector3 direction) => RayIntersect(ref origin, ref direction);

        /// <summary>
        ///     Checks wether a point is within a box or not.
        /// </summary>
        /// <param name="point"></param>
        /// <returns></returns>
        public ContainmentType Contains(Vector3 point) => Contains(ref point);

        /// <summary>
        ///     Checks whether a point is inside, outside or intersecting
        ///     a point.
        /// </summary>
        /// <param name="point">A point in space.</param>
        /// <returns>The ContainmentType of the point.</returns>
        public ContainmentType Contains(ref Vector3 point) =>
			Min.X <= point.X && point.X <= Max.X && Min.Y <= point.Y && point.Y <= Max.Y && Min.Z <= point.Z &&
			point.Z <= Max.Z
				? ContainmentType.Contains
				: ContainmentType.Disjoint;

		#endregion

        /// <summary>
        ///     Retrieves the 8 corners of the box.
        /// </summary>
        /// <returns>An array of 8 Vector3 entries.</returns>

        #region public void GetCorners(Vector3[] corners)

		public void GetCorners(Vector3[] corners) {
			corners[0].Set(Min.X, Max.Y, Max.Z);
			corners[1].Set(Max.X, Max.Y, Max.Z);
			corners[2].Set(Max.X, Min.Y, Max.Z);
			corners[3].Set(Min.X, Min.Y, Max.Z);
			corners[4].Set(Min.X, Max.Y, Min.Z);
			corners[5].Set(Max.X, Max.Y, Min.Z);
			corners[6].Set(Max.X, Min.Y, Min.Z);
			corners[7].Set(Min.X, Min.Y, Min.Z);
		}

		#endregion


		public void AddPoint(Vector3 point) {
			AddPoint(ref point);
		}

		public void AddPoint(ref Vector3 point) {
			Extensions.Max(ref Max, ref point, out Max);
			Extensions.Min(ref Min, ref point, out Min);
		}

        /// <summary>
        ///     Expands a bounding box with the volume 0 by all points
        ///     given.
        /// </summary>
        /// <param name="points">A array of Vector3.</param>
        /// <returns>The resulting bounding box containing all points.</returns>

        #region public static JBBox CreateFromPoints(Vector3[] points)

		public static JBBox CreateFromPoints(Vector3[] points) {
			var vector3 = new Vector3(float.MaxValue);
			var vector2 = new Vector3(float.MinValue);

			for(var i = 0; i < points.Length; i++) {
				Extensions.Min(ref vector3, ref points[i], out vector3);
				Extensions.Max(ref vector2, ref points[i], out vector2);
			}

			return new JBBox(vector3, vector2);
		}

		#endregion

        /// <summary>
        ///     Checks whether another bounding box is inside, outside or intersecting
        ///     this box.
        /// </summary>
        /// <param name="box">The other bounding box to check.</param>
        /// <returns>The ContainmentType of the box.</returns>

        #region public ContainmentType Contains(JBBox box)

		public ContainmentType Contains(JBBox box) {
			return Contains(ref box);
		}

        /// <summary>
        ///     Checks whether another bounding box is inside, outside or intersecting
        ///     this box.
        /// </summary>
        /// <param name="box">The other bounding box to check.</param>
        /// <returns>The ContainmentType of the box.</returns>
        public ContainmentType Contains(ref JBBox box) {
			var result = ContainmentType.Disjoint;
			if(Max.X >= box.Min.X && Min.X <= box.Max.X && Max.Y >= box.Min.Y && Min.Y <= box.Max.Y &&
			   Max.Z >= box.Min.Z && Min.Z <= box.Max.Z)
				result = Min.X <= box.Min.X && box.Max.X <= Max.X && Min.Y <= box.Min.Y && box.Max.Y <= Max.Y &&
				         Min.Z <= box.Min.Z && box.Max.Z <= Max.Z
					? ContainmentType.Contains
					: ContainmentType.Intersects;

			return result;
		}

		#endregion

        /// <summary>
        ///     Creates a new box containing the two given ones.
        /// </summary>
        /// <param name="original">First box.</param>
        /// <param name="additional">Second box.</param>
        /// <returns>A JBBox containing the two given boxes.</returns>

        #region public static JBBox CreateMerged(JBBox original, JBBox additional)

		public static JBBox CreateMerged(JBBox original, JBBox additional) {
			JBBox result;
			CreateMerged(ref original, ref additional, out result);
			return result;
		}

        /// <summary>
        ///     Creates a new box containing the two given ones.
        /// </summary>
        /// <param name="original">First box.</param>
        /// <param name="additional">Second box.</param>
        /// <param name="result">A JBBox containing the two given boxes.</param>
        public static void CreateMerged(ref JBBox original, ref JBBox additional, out JBBox result) {
			Vector3 vector;
			Vector3 vector2;
			Extensions.Min(ref original.Min, ref additional.Min, out vector2);
			Extensions.Max(ref original.Max, ref additional.Max, out vector);
			result.Min = vector2;
			result.Max = vector;
		}

		#endregion

		public Vector3 Center => (Min + Max) * (1.0f / 2.0f);

		internal float Perimeter => 2.0f * ((Max.X - Min.X) * (Max.Y - Min.Y) +
		                                    (Max.X - Min.X) * (Max.Z - Min.Z) +
		                                    (Max.Z - Min.Z) * (Max.Y - Min.Y));
	}
}