using System.Collections;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;

public class UnconvexCollider
{
    Vector3[] points;

    List<Vector3[]> currentShapes;
    float[] currentAngles;
    List<Vector3[]> shapesToDo;
    List<ConvexCollider> finalShapes;

    public TriangleS[] triangles;
    Color color;

    bool invisible = false;
    bool empty = false;

    SphericalUtilities su = new SphericalUtilities();
    EmptyObjects eo = new EmptyObjects();

    public UnconvexCollider(Vector3[] points_, Color c_, bool invisible_=false, bool empty_ = false) {
        points = (Vector3[])points_.Clone();
        color = c_;
        
        currentShapes = new List<Vector3[]>();
        shapesToDo = new List<Vector3[]>();
        finalShapes = new List<ConvexCollider>();

        currentShapes.Add(points);
        GetTriangles();

        invisible = invisible_;
        empty = empty_;
        if (empty || invisible) {
            for (int i = 0; i < triangles.Length; i++) {
                triangles[i] = eo.GetEmptyTriangle();
            }
        }
    }

    public void Update(Vector3[] points_, Color c_, bool invisible_=false, bool empty_ = false) {
        points = (Vector3[])points_.Clone();
        color = c_;
        
        currentShapes = new List<Vector3[]>();
        shapesToDo = new List<Vector3[]>();

        currentShapes.Add(points);
        UpdateTriangles();

        invisible = invisible_;
        empty = empty_;
        if (empty || invisible) {
            for (int i = 0; i < triangles.Length; i++) {
                triangles[i] = eo.GetEmptyTriangle();
            }
        }
    }

    public void MoveRotate(Quaternion q) {
        for (int i = 0; i < points.Length; i++) {
            points[i] = q * points[i];
        }

        for (int i = 0; i < triangles.Length; i++) {
            finalShapes[i].MoveRotate(q);
            if (!empty && !invisible) {
                triangles[i].a = q * triangles[i].a; triangles[i].b = q * triangles[i].b; triangles[i].c = q * triangles[i].c;
                triangles[i].midAB = q * triangles[i].midAB; triangles[i].midBC = q * triangles[i].midBC; triangles[i].midCA = q * triangles[i].midCA;
            }
        }
    }

    public void ChangeColor(Color c_) {
        color = c_;
        for (int i = 0; i < triangles.Length; i++)
        {
            finalShapes[i].c = color;
            triangles[i].color = color;
        }
    }

    void GetTriangles() {
        GetFinalShapes();

        triangles = new TriangleS[finalShapes.Count];

        for (int i = 0; i < finalShapes.Count; i++) {
            triangles[i] = finalShapes[i].triangles[0];
        }
    }

    void UpdateTriangles() {
        UpdateFinalShapes();

        triangles = new TriangleS[finalShapes.Count];

        for (int i = 0; i < finalShapes.Count; i++) {
            triangles[i] = finalShapes[i].triangles[0];
        }
    }
    
    void GetFinalShapes() {

        while (currentShapes.Count > 0) { // checking if there are shapes left to do 
            for (int i = 0; i < currentShapes.Count; i++) { // iterating over shapes
                if (currentShapes[i].Length == 3) {
                    ConvexCollider c = new ConvexCollider(currentShapes[i], color);
                    finalShapes.Add(c);
                } else {
                    currentAngles = su.GetAngles(currentShapes[i]); // getting angles for current shape
                    int j = FindConvexAngle(currentAngles); // convex angle

                    int[] triangle = new int[3] {(j - 1 + currentAngles.Length) % currentAngles.Length, j, (j+1)%currentAngles.Length}; // indexes for triangle
                    Vector3[] triangleP = new Vector3[3] {
                        currentShapes[i][triangle[0]], currentShapes[i][triangle[1]], currentShapes[i][triangle[2]]
                    }; // points for triangle collider

                    ConvexCollider c = new ConvexCollider(triangleP, color);

                    Vector3 closestP = new Vector3(); // getting closest point in triangle
                    int closestPindex = -1;
                    float dist = 10000000;
                    for (int k = 0; k < currentShapes[i].Length; k++) {
                        if (!triangle.Contains(k)) {
                            if (c.CollidePoint(currentShapes[i][k])) {
                                float newDist = Vector3.Distance(currentShapes[i][j], currentShapes[i][k]);
                                if (newDist < dist) {
                                    dist = newDist;
                                    closestP = currentShapes[i][k];
                                    closestPindex = k;
                                }
                            }
                        }
                    }

                    if (dist == 10000000) { // no point was found, putting triangle in finalShapes and creating new shape
                        finalShapes.Add(c);
                        Vector3[] newP = GetArrayWithSkip(currentShapes[i], j);
                        shapesToDo.Add(newP);
                    } else {
                        int k = closestPindex;
                        if (j > k) { // swapping j k so k is lower
                            int ooooo = j;
                            j = k;
                            k = ooooo;
                        }

                        int sizeA = k-j+1;
                        Vector3[] newA = new Vector3[sizeA];
                        int iter=0;
                        for (int ii = j; ii < k+1; ii++) {
                            newA[iter] = currentShapes[i][ii];
                            iter++;
                        }

                        int sizeB = currentShapes[i].Length - sizeA + 2;
                        Vector3[] newB = new Vector3[sizeB];
                        iter=0;
                        for (int ii = k; ii < k+sizeB; ii++) {
                            newB[iter] = currentShapes[i][ii % currentShapes[i].Length];
                            iter++;
                        }

                        shapesToDo.Add(newA);
                        shapesToDo.Add(newB);
                    }
                }
            }

            currentShapes = new List<Vector3[]>();

            for (int i = 0; i < shapesToDo.Count; i++) {
                currentShapes.Add(shapesToDo[i]);
            }

            shapesToDo = new List<Vector3[]>();
        }
    }

    void UpdateFinalShapes() {
        int fs = 0;

        while (currentShapes.Count > 0) { // checking if there are shapes left to do 
            for (int i = 0; i < currentShapes.Count; i++) { // iterating over shapes
                if (currentShapes[i].Length == 3) {
                    ConvexCollider c = new ConvexCollider(currentShapes[i], color);
                    finalShapes[fs].BeClone(c);
                    fs++;
                } else {
                    currentAngles = su.GetAngles(currentShapes[i]); // getting angles for current shape
                    int j = FindConvexAngle(currentAngles); // convex angle

                    int[] triangle = new int[3] {(j - 1 + currentAngles.Length) % currentAngles.Length, j, (j+1)%currentAngles.Length}; // indexes for triangle
                    Vector3[] triangleP = new Vector3[3] {
                        currentShapes[i][triangle[0]], currentShapes[i][triangle[1]], currentShapes[i][triangle[2]]
                    }; // points for triangle collider

                    ConvexCollider c = new ConvexCollider(triangleP, color);

                    Vector3 closestP = new Vector3(); // getting closest point in triangle
                    int closestPindex = -1;
                    float dist = 10000000;
                    for (int k = 0; k < currentShapes[i].Length; k++) {
                        if (!triangle.Contains(k)) {
                            if (c.CollidePoint(currentShapes[i][k])) {
                                float newDist = Vector3.Distance(currentShapes[i][j], currentShapes[i][k]);
                                if (newDist < dist) {
                                    dist = newDist;
                                    closestP = currentShapes[i][k];
                                    closestPindex = k;
                                }
                            }
                        }
                    }

                    if (dist == 10000000) { // no point was found, putting triangle in finalShapes and creating new shape
                        finalShapes[fs].BeClone(c);
                        fs++;

                        Vector3[] newP = GetArrayWithSkip(currentShapes[i], j);
                        shapesToDo.Add(newP);
                    } else {
                        int k = closestPindex;
                        if (j > k) { // swapping j k so k is lower
                            int ooooo = j;
                            j = k;
                            k = ooooo;
                        }

                        int sizeA = k-j+1;
                        Vector3[] newA = new Vector3[sizeA];
                        int iter=0;
                        for (int ii = j; ii < k+1; ii++) {
                            newA[iter] = currentShapes[i][ii];
                            iter++;
                        }

                        int sizeB = currentShapes[i].Length - sizeA + 2;
                        Vector3[] newB = new Vector3[sizeB];
                        iter=0;
                        for (int ii = k; ii < k+sizeB; ii++) {
                            newB[iter] = currentShapes[i][ii % currentShapes[i].Length];
                            iter++;
                        }

                        shapesToDo.Add(newA);
                        shapesToDo.Add(newB);
                    }
                }
            }

            currentShapes = new List<Vector3[]>();

            for (int i = 0; i < shapesToDo.Count; i++) {
                currentShapes.Add(shapesToDo[i]);
            }

            shapesToDo = new List<Vector3[]>();
        }
    }

    Vector3[] GetArrayWithSkip(Vector3[] arr, int skip) {
        Vector3[] newP = new Vector3[arr.Length - 1];
        for (int k = 0; k < arr.Length; k++) { 
            if (k != skip) {
                int jj = k;
                if (k > skip) {
                    jj -= 1;
                }
                newP[jj] = arr[k];
            }
        }
        return newP;
    }

    int FindConvexAngle(float[] angles_) {
        for (int i = 0; i < angles_.Length; i++) {
            if (angles_[i] < Mathf.PI) {
                return i;
            }
        }
        Debug.Log("no convex angle");
        return -1;
    }

    public bool CollidePoint(Vector3 p) {
        if (empty) {return false;}

        for (int i = 0; i < finalShapes.Count; i++) {
            if (finalShapes[i].CollidePoint(p)) {
                return true;
            }
        }
        return false;
    }

    public bool CollideCircle(Vector3 center, float r) {
        if (empty) {return false;}
        for (int i = 0; i < finalShapes.Count; i++) {
            if (finalShapes[i].CollideCircle(center, r)) {return true;}
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
