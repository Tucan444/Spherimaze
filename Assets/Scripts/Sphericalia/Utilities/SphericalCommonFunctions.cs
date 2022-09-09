using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SphericalCommonFunctions : SphericalAdder
{
    // returns spherical distance
    public float SphDistance(Vector3 a, Vector3 b) {
        return Mathf.Acos(Vector3.Dot(a, b));
    }
    
    // lerps between a b in spherical space
    public Vector3 SphLerp(Vector3 a, Vector3 b, float t) {
        float d = Mathf.Acos(Vector3.Dot(a, b)) * t;
        Quaternion q = Quaternion.AngleAxis(Rad2Deg * d, Vector3.Cross(a, b));
        return q * a;
    }

    // returns quaternion that does lerping
    public Quaternion LerpQuaternion(Vector3 a, Vector3 b, float t) {
        float d = Mathf.Acos(Vector3.Dot(a, b)) * t;
        Quaternion q = Quaternion.AngleAxis(Rad2Deg * d, Vector3.Cross(a, b));
        return q;
    }

    // calculates position where ray is after distance t
    public Vector3 RayTravel(Vector3 o, Vector3 d, float t) {
        return Quaternion.AngleAxis(Rad2Deg * t, Vector3.Cross(o, d)) * o;
    }

    // returns points on line on sphere
    public Vector3[] GetLinePoints(Vector3 v0, Vector3 v1, int points_n=20, bool shortest_path=true) {
        float length = Mathf.Acos(Vector3.Dot(v0, v1));
        if (shortest_path == false) {
            length = -(Mathf.PI * 2) + length;
        }
        
        Quaternion q = Quaternion.AngleAxis(-(length * (180.0f / Mathf.PI)) / ((float)points_n - 1),
                                             Vector3.Cross(v1, v0));
        Quaternion newQ = q;
        Vector3[] points = new Vector3[points_n];

        points[0] = v0;
        for (int n = 0; n < points_n-1; n++) {
            points[n+1] = newQ * v0;
            newQ = newQ * q;
        }

        return points;
    }

    // returns points on circle
    public Vector3[] GetCirclePoints(Vector2 center, float r, int points_n=20) {
        Vector3 v1 = Spherical2Cartesian(center);
        Vector3 v2 = Spherical2Cartesian(new Vector2(center.x, center.y + 0.1f));

        Quaternion q = Quaternion.AngleAxis(r * Rad2Deg, Vector3.Cross(v1, v2));
        
        Vector3 sample = q * v1;

        q = Quaternion.AngleAxis(360.0f / points_n, v1);
        Quaternion iter = Quaternion.identity;

        Vector3[] points = new Vector3[points_n];

        for (int i = 0; i < points_n; i++) {
            points[i] = iter * sample;
            iter = q * iter;
        }

        return points;
    }
}
