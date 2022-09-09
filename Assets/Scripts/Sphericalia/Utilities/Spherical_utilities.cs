using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;


public class SphericalUtilities : SphericalCommonFunctions{

    // checks collisions between line and circle on sphere
    public bool CircleLineCollision(Vector3 center, float r, Vector3 a, Vector3 b) {
        if (SphDistance(center, a) <= r) {return true;}
        if (SphDistance(center, b) <= r) {return true;}
        Quaternion q = LerpQuaternion(a, new Vector3(1, 0, 0), 1);
        a = q * a;
        b = q * b;

        Quaternion qq;
        if (Cartesian2Spherical(b)[1] >= 0) {
            qq = Quaternion.AngleAxis(Rad2Deg * GetAngleBetween(b, a, Spherical2Cartesian(new Vector2(1, 0))), a);
        } else {
            qq = Quaternion.AngleAxis(Rad2Deg * -GetAngleBetween(b, a, Spherical2Cartesian(new Vector2(1, 0))), a);
        }

        center = qq * (q * center);
        a = qq * a;
        b = qq * b;

        Vector2 cspher = Cartesian2Spherical(center);

        if (Mathf.Abs(cspher.y) > r) {return false;}

        Vector2 circleLine = new Vector2(cspher.x, 0);
        Vector2 line = new Vector2(Cartesian2Spherical(a).x, Cartesian2Spherical(b).x);

        if (Mathf.Abs(line.x - line.y) > Mathf.PI) { // correction for ring space
            if (line.x < 0) {line.x += TAU;}
            if (line.y < 0) {line.y += TAU;}
            if (circleLine.x < 0) {circleLine.x += TAU;}
        }

        if (Mathf.Min(line.x, line.y) < circleLine.x && circleLine.x < Mathf.Max(line.x, line.y)) {return true;}
        return false;
    }

    // checks collision between ray and line
    public bool LineLineCollision(Vector3 a, Vector3 b, Vector3 v, Vector3 l) {
        Quaternion q = LerpQuaternion(v, new Vector3(1, 0, 0), 1);
        v = q * v;
        l = q * l;

        Quaternion qq;
        if (Cartesian2Spherical(l)[1] >= 0) {
            qq = Quaternion.AngleAxis(Rad2Deg * GetAngleBetween(l, v, Spherical2Cartesian(new Vector2(1, 0))), v);
        } else {
            qq = Quaternion.AngleAxis(Rad2Deg * -GetAngleBetween(l, v, Spherical2Cartesian(new Vector2(1, 0))), v);
        }

        a = qq * (q * a);
        b = qq * (q * b);
        v = qq * v;
        l = qq * l;

        if ((a.y < 0 && b.y < 0) || (a.y > 0 && b.y > 0)) {return false;}

        Vector3 dir = (b-a).normalized;
        Vector2 ispher = Cartesian2Spherical((a + (dir * -(a.y / dir.y))).normalized); // getting lines intersection to xy plane ring

        Vector2 line = new Vector2(Cartesian2Spherical(v).x, Cartesian2Spherical(l).x);

        if (Mathf.Abs(line.x - line.y) > Mathf.PI) { // correction for ring space
            if (line.x < 0) {line.x += TAU;}
            if (line.y < 0) {line.y += TAU;}
            if (ispher.x < 0) {ispher.x += TAU;}
        }

        if (Mathf.Min(line.x, line.y) < ispher.x && ispher.x < Mathf.Max(line.x, line.y)) {return true;}
        return false;
    }

    // returns distance ray has to travel to hit the line
    public float RayLineCast(Vector3 o, Vector3 d, Vector3 a, Vector3 b) {
        Quaternion q = LerpQuaternion(o, new Vector3(1, 0, 0), 1);
        o = q * o;
        d = q * d;

        Quaternion qq;
        if (Cartesian2Spherical(d)[1] >= 0) {
            qq = Quaternion.AngleAxis(Rad2Deg * GetAngleBetween(d, o, Spherical2Cartesian(new Vector2(1, 0))), o);
        } else {
            qq = Quaternion.AngleAxis(Rad2Deg * -GetAngleBetween(d, o, Spherical2Cartesian(new Vector2(1, 0))), o);
        }

        a = qq * (q * a);
        b = qq * (q * b);
        o = qq * o;
        d = qq * d;

        if ((a.y < 0 && b.y < 0) || (a.y > 0 && b.y > 0)) {return -1;}

        Vector3 dir = (b-a).normalized;
        Vector2 ispher = Cartesian2Spherical((a + (dir * -(a.y / dir.y))).normalized); // getting lines intersection to xy plane ring

        if (ispher.x < 0) {ispher.x += TAU;}

        return ispher.x;
    }

    // returns distance ray has to travel to hit the circle
    public float RayCircleCast(Vector3 o, Vector3 d, Vector3 center, float r) {
        Quaternion q = LerpQuaternion(o, new Vector3(1, 0, 0), 1);
        o = q * o;
        d = q * d;

        Quaternion qq;
        if (Cartesian2Spherical(d)[1] >= 0) {
            qq = Quaternion.AngleAxis(Rad2Deg * GetAngleBetween(d, o, Spherical2Cartesian(new Vector2(1, 0))), o);
        } else {
            qq = Quaternion.AngleAxis(Rad2Deg * -GetAngleBetween(d, o, Spherical2Cartesian(new Vector2(1, 0))), o);
        }

        center = qq * (q * center);
        o = qq * o;
        d = qq * d;

        Vector2 cspher = Cartesian2Spherical(center);

        if (Mathf.Abs(cspher.y) > r) {return -1;}

        float x = Mathf.Acos(Mathf.Cos(r) / Mathf.Cos(cspher.y));
        Vector2 boundaries = CrampSpherical(new Vector2(cspher.x - x, cspher.x + x));

        if (boundaries.x < 0) {boundaries.x += TAU;} 
        if (boundaries.y < 0) {boundaries.y += TAU;}

        return Mathf.Min(boundaries.x, boundaries.y);
    }

    // returns intersection between 2 liner (returns (10, 0, 0) if there is none)
    public Vector3 LineLineIntersection(Vector3 a, Vector3 b, Vector3 c, Vector3 d) {
        float t = RayLineCast(a, b, c, d);
        if (t == -1 || t > SphDistance(a, b)) {return new Vector3(10, 0, 0);}

        return RayTravel(a, b, t);
    }

    // returns intersection between 2 liner (returns (10, 0, 0) if there is none)
    public Vector3[] LineCircleIntersection(Vector3 a, Vector3 b, Vector3 center, float r) {
        Vector3[] intersections = new Vector3[2] {new Vector3(10, 0, 0), new Vector3(10, 0, 0)};

        float t = RayCircleCast(a, b, center, r);
        float tt = TAU - RayCircleCast(a, -b, center, r);

        float length = SphDistance(a, b);
        Quaternion q;

        if (t <= length) {
            intersections[0] = RayTravel(a, b, t);
        }
        if (tt <= length) {
            intersections[1] = RayTravel(a, b, tt);
        }

        return intersections;
    }

    // calculates default normal for spherical coordinates
    public Vector3 GetDirection(Vector3 v) { // working
        return (new Vector3(0, 1.0f / Vector3.Dot(new Vector3(0, 1, 0), v), 0) - v).normalized;
    }

    // Functions made for unconvex shapes

    // return vector thats tangent to space pointing from a to b in the shortest path
    public Vector3 GetPlaneVector(Vector3 a, Vector3 b) { // working

        // I HAVE NO IDEA WHY THESE 3 LINES DONT WORK

        float angle = Mathf.Acos(Vector3.Dot(a, (a - b).normalized));
        Quaternion q = Quaternion.AngleAxis(((Mathf.PI * 0.5f) - angle) * Rad2Deg, Vector3.Cross(b, a)); // radians !!!!!!!
        return (q * (b-a)).normalized;

        // these work
        /* float angle = Mathf.Acos(Vector3.Dot(a, b));
        Vector3 v = (b * (1.0f / Mathf.Cos(angle))) - a;
        return v.normalized; */
    }

    // returns smaller angle on point b (returns positive value)
    public float GetAngleBetween(Vector3 a, Vector3 b, Vector3 c) { // working
        return Mathf.Acos(Vector3.Dot(GetPlaneVector(b, a), GetPlaneVector(b, c)));
    }

    // check if convexity was interupted
    public bool CheckConvexity(Vector3 a, Vector3 b, Vector3 c, Vector3 d) { // working *
        Vector3[] planeVectors = new Vector3[4] {GetPlaneVector(b, a), GetPlaneVector(b, c), GetPlaneVector(c, b), GetPlaneVector(c, d)};
        Vector3 normal = Quaternion.AngleAxis(90, b) * planeVectors[1];
        Quaternion q = Quaternion.AngleAxis(-Mathf.Acos(Vector3.Dot(b, c)) * Rad2Deg, Vector3.Cross(b, c));

        planeVectors[2] = q * planeVectors[2];
        planeVectors[3] = q * planeVectors[3];

        float det1 = Vector3.Dot((planeVectors[0] + planeVectors[1]).normalized, normal);
        float det2 = Vector3.Dot((planeVectors[2] + planeVectors[3]).normalized, normal);

        if (((det1 < 0) && (det2 < 0)) || ((det1 > 0) && (det2 > 0))) {
            return true;
        }
        return false;

    }

    // gets angles for all points
    public float[] GetAngles(Vector3[] points) {

        // filtering 180 angles and getting angles
        List<int> usefullPoints = new List<int>();
        float[] angles = new float[points.Length];

        for (int i = 0; i < points.Length; i++) {
            int[] ii = new int[3] {i, (i+1) % points.Length, (i+2) % points.Length};
            float nextAngle = GetAngleBetween(points[ii[0]], points[ii[1]], points[ii[2]]);
            angles[ii[1]] = nextAngle;
            if (Mathf.Abs(nextAngle - Mathf.PI) > 0.0001f) {
                usefullPoints.Add(ii[1]);
            }
        }

        // flagging unconvexity
        bool convexity = true;
        for (int i = 0; i < usefullPoints.Count; i++) {
            // doing prep work
            int[] ii = new int[2] {i, (i+1) % usefullPoints.Count};

            int[] ids = new int[4] {usefullPoints[ii[0]] - 1, usefullPoints[ii[0]], usefullPoints[ii[1]], (usefullPoints[ii[1]]+1) % points.Length};
            if (ids[0] == -1) {ids[0] = points.Length - 1;}

            // checking convexivity
            if (!CheckConvexity(points[ids[0]], points[ids[1]], points[ids[2]], points[ids[3]])) {
                convexity = !convexity;
            }


            if (convexity == false) {
                angles[ids[2]] = TAU - angles[ids[2]];
            }
        }

        // finding smaller area
        float[] sums = new float[2];
        for (int i = 0; i < points.Length; i++) {
            sums[0] += angles[i];
            sums[1] += TAU - angles[i];
        }

        if (sums[0] > sums[1]) {
            for (int i = 0; i < points.Length; i++) {
                angles[i] = TAU - angles[i];
            }
        }

        return angles;
    }

    // returns vector pointing from b to inside of shape
    public Vector3 GetInsetVector(Vector3 a, Vector3 b, Vector3 c, float angle) { // working
        Vector3[] planeVectors = new Vector3[2] {GetPlaneVector(b, a), GetPlaneVector(b, c)};
        Vector3 v = (planeVectors[0] + planeVectors[1]).normalized;
        if (angle > Mathf.PI) {
            v *= -1;
        }
        return v;
    }

    // gets inset version of shape
    public Vector3[] GetInsetShape(Vector3[] points, float[] angles, float d) {

        // getting inset vectors and flagging 180 angles
        Vector3[] insetVectors = new Vector3[points.Length];
        int working = 0;

        for (int i = 0; i < points.Length; i++) {
            int[] ii = new int[3] {i, (i+1) % points.Length, (i+2) % points.Length};
            if (angles[ii[1]] - Mathf.PI > 0.001f) {
                insetVectors[ii[1]] = new Vector3(10, 0, 0);
            } else {
                insetVectors[ii[1]] = GetInsetVector(points[ii[0]], points[ii[1]], points[ii[2]], angles[ii[1]]);
                working = ii[1];
            }
        }

        // getting inset vectors for 180 ones
        for (int i = 0; i < points.Length; i++) {
            int[] j = new int[3] {(i + working) % points.Length, (i+1 + working) % points.Length, (i+2 + working) % points.Length};
            if (insetVectors[j[1]].x == 10) {
                insetVectors[j[1]] = GetInsetVector(points[j[0]], points[j[1]], points[j[2]], angles[j[1]] - (0.5f * Mathf.PI));
                if (Vector3.Dot(insetVectors[j[0]], insetVectors[j[1]]) < 0) {
                    insetVectors[j[1]] *= -1;
                }
            }
        }

        // getting new points
        Vector3[] newPoints = new Vector3[points.Length];
        for (int i = 0; i < points.Length; i++) {
            newPoints[i] = points[i] + (d * insetVectors[i]);
        }
        return newPoints;
    }

    // gizmos functions
    public void GizmosDrawLine(Vector3 a, Vector3 b, int n=4, bool short_=true) {
        Vector3[] points = this.GetLinePoints(a, b, n, short_);
        for (int i = 0; i < n-1; i++) {
            Gizmos.DrawLine(points[i], points[i+1]);
        }
    }

    public void GizmosDrawPoints(Vector3[] points) {
        for (int i = 0; i < points.Length; i++) {
            Gizmos.DrawLine(points[i], points[(i+1) % points.Length]);
        }
    }
    
    public void GizmosDrawPointsNoLoop(Vector3[] points) {
        for (int i = 0; i < points.Length-1; i++) {
            Gizmos.DrawLine(points[i], points[i+1]);
        }
    }

    public void GizmosDrawNGon(Vector2[] points, Vector2 spherPos, Color color, int rings = 3, float brighten = 1.4f) {
        float multip = 1;
        for (int k = 0; k < rings; k++) {
            Vector3[] newP = new Vector3[points.Length];
            for (int i = 0; i < points.Length; i++) {
                newP[i] = Polar2Cartesian(AddPolarSpher(new Vector2(points[i][0] * multip, points[i][1]), spherPos));
            }

            Gizmos.color = brighten * color * multip * multip;

            for (int i = 0; i < points.Length; i++) {
                GizmosDrawLine(newP[i], newP[(i+1)%newP.Length]);
            }

            multip -= 0.1f;
        }
    }

    public void GizmosDrawShape(Vector3[] points, float[] angles, float d, float scale, Color color) {
        Gizmos.color = color * 1.4f;
        GizmosDrawPoints(points);
        Gizmos.color = color * 1.2f;
        GizmosDrawPoints(GetInsetShape(points, angles, d * scale));
        Gizmos.color = color;
        GizmosDrawPoints(GetInsetShape(points, angles, (2*d) * scale));
    }

    #if UNITY_EDITOR
    // handles functions
    public void HandlesDrawPoints(Vector3[] points) {
        for (int i = 0; i < points.Length; i++) {
            Handles.DrawLine(points[i], points[(i+1) % points.Length]);
        }
    }

    public void HandlesDrawLine(Vector3 a, Vector3 b, int n=4, bool short_=true) {
        Vector3[] points = this.GetLinePoints(a, b, n, short_);
        for (int i = 0; i < n-1; i++) {
            Handles.DrawLine(points[i], points[i+1]);
        }
    }

    public void HandlesDrawNGon(Vector2[] points, Vector2 spherPos, Color color, int rings = 3, float brighten = 1.4f) {
        float multip = 1;
        for (int k = 0; k < rings; k++) {
            Vector3[] newP = new Vector3[points.Length];
            for (int i = 0; i < points.Length; i++) {
                newP[i] = Polar2Cartesian(AddPolarSpher(new Vector2(points[i][0] * multip, points[i][1]), spherPos));
            }

            Handles.color = brighten * color * multip * multip;

            for (int i = 0; i < points.Length; i++) {
                HandlesDrawLine(newP[i], newP[(i+1)%newP.Length]);
            }

            multip -= 0.1f;
        }
    }
    #endif
}
