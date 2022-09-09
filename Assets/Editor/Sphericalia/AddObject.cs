using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class AddObject : EditorWindow
{
    GameObject space;

    public enum SphericalObjects {
        Circle, NGon, GeneralShape, UVTiles
    }
    SphericalObjects objects;

    // generally used variables
    bool draw = true;
    bool batchGenerate = false;

    int layer = 0;
    bool Static = true;
    bool isCollider = false;
    bool isTrigger = false;
    bool invisible = false;
    bool empty = false;
    string namE = "duck";
    int nOfObjects = 6;
    Vector2 sphericalPosition = new Vector2();
    Color color = new Color(0.69f, 0.48f, 0.41f, 1);

    // for circle
    float radius = 0.1f;

    // for ngon  
    int ngon = 5;
    float rotation = 0;
    float scale = .5f; 

    // for general shape
    int nshape = 20;

    // other
    SphericalUtilities su = new SphericalUtilities();
    
    [MenuItem("Spherical/AddObject")]
    public static void OpenAddObjectWindow() => GetWindow<AddObject>("Object adder");

    void OnGUI() {

        using (new GUILayout.HorizontalScope()) {
            GUILayout.Label("Object : ");
            objects = (SphericalObjects)EditorGUILayout.EnumPopup(objects);
        }

        GUILayout.BeginHorizontal();
        draw = EditorGUILayout.Toggle("Draw: ", draw);
        batchGenerate = EditorGUILayout.Toggle("       Batch Generate: ", batchGenerate);
        GUILayout.EndHorizontal();
        layer = EditorGUILayout.IntField("Layer: ", layer);
        GUILayout.BeginHorizontal();
        Static = EditorGUILayout.Toggle("Static: ", Static);
        isCollider = EditorGUILayout.Toggle("       Is Collider: ", isCollider);
        GUILayout.EndHorizontal();
        GUILayout.BeginHorizontal();
        isTrigger = EditorGUILayout.Toggle("Is Trigger: ", isTrigger);
        invisible = EditorGUILayout.Toggle("       Invisible: ", invisible);
        GUILayout.EndHorizontal();
        empty = EditorGUILayout.Toggle("Empty: ", empty);
        namE = EditorGUILayout.TextField("Name: ", namE);
        if (batchGenerate) {
            float originalWidth = EditorGUIUtility.labelWidth;
            EditorGUIUtility.labelWidth = 180;
            nOfObjects = EditorGUILayout.IntField("Number of objects to generate: ", nOfObjects);
            if (nOfObjects < 0) {nOfObjects = 0;}
            EditorGUIUtility.labelWidth = originalWidth;
        }
        sphericalPosition = EditorGUILayout.Vector2Field("SphPosition: ", sphericalPosition);
        color = EditorGUILayout.ColorField("Color: ", color);

        // dealing with circle
        if (objects == SphericalObjects.Circle) {
            using (new GUILayout.HorizontalScope()) {
                GUILayout.Label("Radius : " + radius, GUILayout.Width(120));

                radius = GUILayout.HorizontalSlider(radius, 0.01f, -0.01f + Mathf.PI);
            }
            
            if (!batchGenerate) {
                if (GUILayout.Button("Create circle")) {
                    CreateCircle(namE);
                }
            } else {
                if (GUILayout.Button("Create circles")) {
                    if (nOfObjects > 0) {
                        GameObject obj = new GameObject(namE);
                        obj.transform.parent = GameObject.Find("___SphericalSpace___").transform;

                        for (int i = 0; i < nOfObjects; i++)
                        {
                            GameObject o = CreateCircle(i.ToString());
                            o.transform.parent = obj.transform;
                        }
                    } else {
                        Debug.Log("nOfObjects = 0, not creating anything");
                    }
                }
            }
            
        } // dealing with ngon
        else if (objects == SphericalObjects.NGon) {
            using (new GUILayout.HorizontalScope()) {
                GUILayout.Label("N : " + ngon, GUILayout.Width(60));

                ngon = EditorGUILayout.IntSlider(ngon, 3, 20);
            }
            using (new GUILayout.HorizontalScope()) {
                GUILayout.Label("Rotation : " + rotation, GUILayout.Width(140));

                rotation = GUILayout.HorizontalSlider(rotation, -180, 180);
            }
            using (new GUILayout.HorizontalScope()) {
                GUILayout.Label("Scale : " + scale, GUILayout.Width(140));

                scale = GUILayout.HorizontalSlider(scale, -0.01f, -0.001f + Mathf.PI*0.5f);
            }

            if (!batchGenerate) {
                if (GUILayout.Button("Create ngon")) {
                    CreateNGon(namE);
                }
            } else {
                if (GUILayout.Button("Create ngons")) {
                    if (nOfObjects > 0) {
                        GameObject obj = new GameObject(namE);
                        obj.transform.parent = GameObject.Find("___SphericalSpace___").transform;

                        for (int i = 0; i < nOfObjects; i++)
                        {
                            GameObject o = CreateNGon(i.ToString());
                            o.transform.parent = obj.transform;
                        }
                    } else {
                        Debug.Log("nOfObjects = 0, not creating anything");
                    }
                }
            }
        }// dealing with general shapes
        else if (objects == SphericalObjects.GeneralShape) {
            using (new GUILayout.HorizontalScope()) {
                GUILayout.Label("N : " + nshape, GUILayout.Width(60));

                nshape = EditorGUILayout.IntSlider(nshape, 3, 60);
            }
            using (new GUILayout.HorizontalScope()) {
                GUILayout.Label("Rotation : " + rotation, GUILayout.Width(140));

                rotation = GUILayout.HorizontalSlider(rotation, -180, 180);
            }
            using (new GUILayout.HorizontalScope()) {
                GUILayout.Label("Scale : " + scale, GUILayout.Width(140));

                scale = GUILayout.HorizontalSlider(scale, -0.01f, 2);
            }

            if (!batchGenerate) {
                if (GUILayout.Button("Create general shape")) {
                    CreateShape(namE);
                }
            } else {
                if (GUILayout.Button("Create general shapes")) {
                    if (nOfObjects > 0) {
                        GameObject obj = new GameObject(namE);
                        obj.transform.parent = GameObject.Find("___SphericalSpace___").transform;

                        for (int i = 0; i < nOfObjects; i++)
                        {
                            GameObject o = CreateShape(i.ToString());
                            o.transform.parent = obj.transform;
                        }
                    } else {
                        Debug.Log("nOfObjects = 0, not creating anything");
                    }
                }
            }
        } // dealing with uv tiles
        else if (objects == SphericalObjects.UVTiles) {
            using (new GUILayout.HorizontalScope()) {
                GUILayout.Label("Rotation : " + rotation, GUILayout.Width(140));

                rotation = GUILayout.HorizontalSlider(rotation, -180, 180);
            }

            if (!batchGenerate) {
                if (GUILayout.Button("Create UVTiles")) {
                    CreateUVTiles(namE);
                }
            } else {
                if (GUILayout.Button("Create multiple UVTiles")) {
                    if (nOfObjects > 0) {
                        GameObject obj = new GameObject(namE);
                        obj.transform.parent = GameObject.Find("___SphericalSpace___").transform;

                        for (int i = 0; i < nOfObjects; i++)
                        {
                            GameObject o = CreateUVTiles(i.ToString());
                            o.transform.parent = obj.transform;
                        }
                    } else {
                        Debug.Log("nOfObjects = 0, not creating anything");
                    }
                }
            }
        }
    }

    GameObject CreateCircle(string name_) {
        GameObject obj = new GameObject(name_);
        SphCircle sc = obj.AddComponent(typeof(SphCircle)) as SphCircle;
        Undo.RecordObject(sc, "Configured Circle");
        sc.layer = layer;
        sc.Static = Static;
        sc.isCollider = isCollider;
        sc.isTrigger = isTrigger;
        sc.sphPosition = sphericalPosition;
        sc.radius = radius;
        sc.color = color;
        sc.invisible = invisible;
        sc.empty = empty;
        sc.GetDefaultSetup();

        return obj;
    }

    GameObject CreateNGon(string name_) {
        GameObject obj = new GameObject(name_);
        SphGon sg = obj.AddComponent(typeof(SphGon)) as SphGon;
        Undo.RecordObject(sg, "Configured NGon");
        sg.layer = layer;
        sg.Static = Static;
        sg.isCollider = isCollider;
        sg.isTrigger = isTrigger;
        sg.n = ngon;
        sg.sphPosition = sphericalPosition;
        sg.rotation = rotation;
        sg.scale = scale;
        sg.color = color;
        sg.invisible = invisible;
        sg.empty = empty;
        sg.GetDefaultSetup();

        return obj;
    }

    GameObject CreateShape(string name_) {
        GameObject obj = new GameObject(name_);
        SphShape ss = obj.AddComponent(typeof(SphShape)) as SphShape;
        Undo.RecordObject(ss, "Configured General Shape");
        ss.layer = layer;
        ss.Static = Static;
        ss.isCollider = isCollider;
        ss.isTrigger = isTrigger;
        ss.sphPosition = sphericalPosition;
        ss.rotation = rotation;
        ss.scale = scale;
        ss.color = color;
        ss.invisible = invisible;
        ss.empty = empty;
        ss.polarVertices = new Vector2[nshape];
        ss.SetToNGon();
        ss.GetDefaultSetup();

        return obj;
    }

    GameObject CreateUVTiles(string name_) {
        GameObject obj = new GameObject(name_);
        UVTiles uvt = obj.AddComponent(typeof(UVTiles)) as UVTiles;
        Undo.RecordObject(uvt, "Configured UVTiles");
        uvt.layer = layer;
        uvt.Static = Static;
        uvt.isCollider = isCollider;
        uvt.isTrigger = isTrigger;
        uvt.sphPosition = sphericalPosition;
        uvt.rotation = rotation;
        uvt.color = color;
        uvt.invisible = invisible;
        uvt.empty = empty;
        uvt.OnEnable();

        return obj;
    }

    void DuringSceneGUI(SceneView view) {
        if (draw) {
            if (objects == SphericalObjects.Circle) {
                Handles.color = color * 1.4f;
                su.HandlesDrawPoints(su.GetCirclePoints(sphericalPosition, radius));
                Handles.color = color * 1.2f;
                su.HandlesDrawPoints(su.GetCirclePoints(sphericalPosition, radius * 0.9f));
                Handles.color = color;
                su.HandlesDrawPoints(su.GetCirclePoints(sphericalPosition, radius * 0.8f));
            } else if (objects == SphericalObjects.NGon) {
                Handles.color = color * 1.4f;
                su.HandlesDrawPoints(ProcessVertices(su.GetCirclePoints(sphericalPosition, scale, ngon)));
                Handles.color = color * 1.2f;
                su.HandlesDrawPoints(ProcessVertices(su.GetCirclePoints(sphericalPosition, scale * 0.9f, ngon)));
                Handles.color = color;
                su.HandlesDrawPoints(ProcessVertices(su.GetCirclePoints(sphericalPosition, scale * 0.8f, ngon)));
            } else if (objects == SphericalObjects.GeneralShape) {
                Handles.color = color * 1.4f;
                su.HandlesDrawPoints(ProcessVertices(su.GetCirclePoints(sphericalPosition, scale, nshape)));
                Handles.color = color * 1.2f;
                su.HandlesDrawPoints(ProcessVertices(su.GetCirclePoints(sphericalPosition, scale * 0.9f, nshape)));
                Handles.color = color;
                su.HandlesDrawPoints(ProcessVertices(su.GetCirclePoints(sphericalPosition, scale * 0.8f, nshape)));
            } else if (objects == SphericalObjects.UVTiles) {
                Handles.color = color * 1.4f;
                su.HandlesDrawPoints(ProcessVertices(su.GetCirclePoints(sphericalPosition, 0.5f, 4), 45));
                Handles.color = color * 1.2f;
                su.HandlesDrawPoints(su.GetCirclePoints(sphericalPosition, 0.45f, 10));
                Handles.color = color;
                su.HandlesDrawPoints(su.GetCirclePoints(sphericalPosition, 0.4f, 10));
            }
        }
        
    }

    void OnEnable() {
        space = GameObject.Find("___SphericalSpace___");
        SceneView.duringSceneGui += DuringSceneGUI;
    }

    void OnDisable() {
        SceneView.duringSceneGui -= DuringSceneGUI;
    }

    Vector3[] ProcessVertices(Vector3[] verts, float add=0) {
        Quaternion q = Quaternion.AngleAxis(rotation + add, su.Spherical2Cartesian(sphericalPosition));
        for (int i = 0; i < verts.Length; i++) {
            verts[i] = q * verts[i];
        }
        return verts;
    }
}
