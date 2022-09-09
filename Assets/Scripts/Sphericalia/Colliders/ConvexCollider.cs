using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ConvexCollider
{
    Vector3 center;
    Vector3[] points;
    Vector3[] normals;
    Vector3[] mids;

    public Color c;
    public TriangleS[] triangles;
    public QuadS[] quads;

    bool invisible = false;
    bool empty = false;

    SphericalUtilities su = new SphericalUtilities();
    EmptyObjects eo = new EmptyObjects();

    public ConvexCollider(Vector3[] points_, Color c_, bool invisible_=false, bool empty_=false) {
        points = (Vector3[])points_.Clone();
        mids = new Vector3[points.Length];
        normals = new Vector3[points.Length];
        center = ComputeCenter(points_);
        c = c_;
        ComputeNormalsAndMids();

        ComputeObjects();

        invisible = invisible_;
        empty = empty_;
        if (empty || invisible) {
            for (int i = 0; i < triangles.Length; i++) {
                triangles[i] = eo.GetEmptyTriangle();
            }
            for (int i = 0; i < quads.Length; i++)
            {
                quads[i] = eo.GetEmptyQuad();
            }   
        }
    }

    public void Update(Vector3[] points_, Color c_, bool invisible_=false, bool empty_=false) {
        points = (Vector3[])points_.Clone();
        mids = new Vector3[points.Length];
        normals = new Vector3[points.Length];
        center = ComputeCenter(points_);
        c = c_;
        ComputeNormalsAndMids();

        ComputeObjects();

        invisible = invisible_;
        empty = empty_;
        if (empty || invisible) {
            for (int i = 0; i < triangles.Length; i++) {
                triangles[i] = eo.GetEmptyTriangle();
            }
            for (int i = 0; i < quads.Length; i++)
            {
                quads[i] = eo.GetEmptyQuad();
            }   
        }
    }

    // clones passed collider to self
    public void BeClone(ConvexCollider c_) {
        center = c_.center;
        points = c_.points;
        normals = c_.normals;
        mids = c_.mids;

        invisible = c_.invisible;
        empty = c_.empty;

        c = c_.c;
        triangles = c_.triangles;
        quads = c_.quads;
    }

    public void MoveRotate(Quaternion q) {
        for (int i = 0; i < points.Length; i++) {
            points[i] = q * points[i];
            normals[i] = q * normals[i];
            mids[i] = q * mids[i];
        }

        if (!empty && !invisible) {
            for (int i = 0; i < triangles.Length; i++) {
                triangles[i].a = q * triangles[i].a; triangles[i].b = q * triangles[i].b; triangles[i].c = q * triangles[i].c;
                triangles[i].midAB = q * triangles[i].midAB; triangles[i].midBC = q * triangles[i].midBC; triangles[i].midCA = q * triangles[i].midCA;
            }

            for (int i = 0; i < quads.Length; i++) {
                quads[i].a = q * quads[i].a; quads[i].b = q * quads[i].b; quads[i].c = q * quads[i].c; quads[i].d = q * quads[i].d;
                quads[i].midAB = q * quads[i].midAB; quads[i].midBC = q * quads[i].midBC; quads[i].midCD = q * quads[i].midCD; quads[i].midDA = q * quads[i].midDA;
            }
        }
    }

    public void ChangeColor(Color c_) {
        c = c_;
        if (!empty && !invisible) {
            for (int i = 0; i < triangles.Length; i++)
            {
                triangles[i].color = c;
            }
            for (int i = 0; i < quads.Length; i++)
            {
                quads[i].color = c;
            }
        }
    }

    Vector3 ComputeCenter(Vector3[] points_) {
        Vector3 center_ = new Vector3();
        for (int i = 0; i < points_.Length; i++) {
            center_ += points_[i];
        }
        center_ = center_.normalized;
        return center_;
    }
    
    void ComputeNormalsAndMids() {
        for (int i = 0; i < points.Length; i++) {
            int ii = (i+1) % points.Length;

            mids[i] = (points[i] + points[ii]).normalized;

            normals[i] = Vector3.Cross(mids[i], (points[ii] - points[i]).normalized);

            if (Vector3.Dot(normals[i], (mids[i] - center)) < 0) {
                normals[i] *= -1;
            }
        }
    }

    void ComputeObjects() {
        triangles = new TriangleS[0];
        if (points.Length % 2 != 0) {
            triangles = new TriangleS[1];
            Vector3[] trianglePoints = new Vector3[3] {points[2], points[1], points[0]};
            Vector3[] triangleMids = new Vector3[3];
            Vector3[] triangleNormals = new Vector3[3];
            TriangleS t = new TriangleS();

            Vector3 tcenter = ComputeCenter(trianglePoints);

            for (int j = 0; j < 3; j++) {
                int jj = (j+1) % 3;

                triangleMids[j] = (trianglePoints[j] + trianglePoints[jj]).normalized;

                triangleNormals[j] = Vector3.Cross(triangleMids[j], (trianglePoints[jj] - trianglePoints[j]).normalized);

                if (Vector3.Dot(triangleNormals[j], (triangleMids[j] - tcenter)) < 0) {
                    triangleNormals[j] *= -1;
                }
            }

            t.a = triangleNormals[0]; t.b = triangleNormals[1]; t.c = triangleNormals[2];
            t.midAB = triangleMids[0]; t.midBC = triangleMids[1]; t.midCA = triangleMids[2];

            t.color = c;

            triangles[0] = t;
        }
        
        quads = new QuadS[(int)((points.Length - 2 - triangles.Length) / 2)];
        int quadsCount = 0;
        for (int i = 1+triangles.Length; i < points.Length-1; i+=2) 
        {
            Vector3[] quadPoints = new Vector3[4] {points[(i+2) % points.Length], points[(i+1) % points.Length], points[i], points[0]};
            Vector3[] quadMids = new Vector3[4];
            Vector3[] quadNormals = new Vector3[4];
            QuadS q = new QuadS();

            Vector3 tcenter = ComputeCenter(quadPoints);

            for (int j = 0; j < 4; j++) {
                int jj = (j+1) % 4;

                quadMids[j] = (quadPoints[j] + quadPoints[jj]).normalized;

                quadNormals[j] = Vector3.Cross(quadMids[j], (quadPoints[jj] - quadPoints[j]).normalized);

                if (Vector3.Dot(quadNormals[j], (quadMids[j] - tcenter)) < 0) {
                    quadNormals[j] *= -1;
                }
            }

            q.a = quadNormals[0]; q.b = quadNormals[1]; q.c = quadNormals[2]; q.d = quadNormals[3];
            q.midAB = quadMids[0]; q.midBC = quadMids[1]; q.midCD = quadMids[2]; q.midDA = quadMids[3];

            q.color = c;

            quads[quadsCount] = q;
            quadsCount++;
        }
    }

    public bool CollidePoint(Vector3 p) {
        if (empty) {return false;}

        for (int i = 0; i < points.Length; i++) {
            if (Vector3.Dot(normals[i], p - mids[i]) > 0) {
                return false;
            }
        }

        return true;
    }

    public bool CollideCircle(Vector3 center, float r) {
        if (empty) {return false;}
        if (CollidePoint(center)) {return true;}

        for (int i = 0; i < points.Length; i++) {
            if(su.CircleLineCollision(center, r, points[i], points[(i+1) % points.Length])) {return true;}
        }

        return false;
    }

    public float RayCast(Vector3 o, Vector3 d) {
        float minT = 10;
        for (int i = 0; i < points.Length; i++)
        {
            float t = su.RayLineCast(o, d, points[i], points[(i+1)% points.Length]);
            if (t != -1) {
                minT = Mathf.Min(minT, t);
            }
        }
        if (minT == 10) {
            return -1;
        } else {
            return minT;
        }
    }
}

public struct TriangleS {
    public Vector3 a;
    public Vector3 b;
    public Vector3 c;

    public Vector3 midAB;
    public Vector3 midBC;
    public Vector3 midCA;

    public Color color;
}
