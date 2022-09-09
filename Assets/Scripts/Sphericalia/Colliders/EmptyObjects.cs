using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EmptyObjects
{
    SphericalUtilities su = new SphericalUtilities();
    public CircleS GetEmptyCircle() {
        CircleS circle = new CircleS();
        circle.center = new Vector3(1, 0, 0);
        circle.r = 0;
        circle.color = Color.gray;
        return circle;
    }

    public TriangleS GetEmptyTriangle() {
        Vector3[] points = su.GetCirclePoints(new Vector3(1, 0, 0), 0.00001f, 3);
        ConvexCollider c = new ConvexCollider(points, Color.gray);
        TriangleS t = c.triangles[0];
        return t;
    }

    public QuadS GetEmptyQuad() {
        Vector3[] points = su.GetCirclePoints(new Vector3(1, 0, 0), 0.00001f, 4);
        ConvexCollider c = new ConvexCollider(points, Color.gray);
        QuadS q = c.quads[0];
        return q;
    }
}
