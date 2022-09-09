using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(SphShape))]
public class ShapeEditor : Editor {

    SphShape shape;
    bool needsRepaint = false;
    bool editorOpen = false;
    bool isNormalized = true;

    Vector3[] ring;
    Vector3 ringOrigin = new Vector3(0, -3, 0);
    int ringEdges = 6;

    int hoverIndex = -1;
    bool pressed = false;
    Vector3 lastPos = new Vector3();
    Vector3 difference = new Vector3();

    float halfPI = Mathf.PI * 0.5f;
    float TAU = Mathf.PI * 2;
    float Deg2Rad = Mathf.PI / 180;
    float Rad2Deg = 180 / Mathf.PI;
    
    public override void OnInspectorGUI() {
        base.OnInspectorGUI();

        if (!editorOpen) {
            if(GUILayout.Button("Open editor")){
                editorOpen = true;
                needsRepaint = true;
            }
        } else {
            if(GUILayout.Button("Close editor")){
                editorOpen = false;
                needsRepaint = true;
            }

            isNormalized = true;
            for (int i = 0; i < shape.polarVertices.Length; i++) {
                if (shape.polarVertices[i][0]*shape.scale > halfPI) {
                    isNormalized = false;
                    break;
                }
            }

            if (!isNormalized) {
                GUILayout.Label("Vertices have to be normalized to edit.");
                GUILayout.Label("This will change all vertices who \nare further than PI away from center.\n(in world space)");
                if(GUILayout.Button("Normalize vertices")){
                    Undo.RecordObject(shape, "normalized vertices");
                    for (int i = 0; i < shape.polarVertices.Length; i++) {
                        if (shape.polarVertices[i][0]*shape.scale > halfPI) {
                            shape.polarVertices[i][0] = (Mathf.PI * 0.499f) / shape.scale;
                        }
                    }
                    shape.GetDefaultSetup();
                }
            } else {
                Undo.RecordObject(shape, "handles radius changed");
                shape.handlesRadius = EditorGUILayout.Slider("Handles radius", shape.handlesRadius, 0.01f, 0.1f);
            }
        }
     }

    void OnSceneGUI()
    {
        if (editorOpen && isNormalized) {
            Event guiEvent = Event.current;

            if (guiEvent.type == EventType.Repaint)
            {
                Draw();
            }
            else if (guiEvent.type == EventType.Layout)
            {
                HandleUtility.AddDefaultControl(GUIUtility.GetControlID(FocusType.Passive));
            }
            else
            {
                HandleInput(guiEvent);
                if (needsRepaint)
                {
                    HandleUtility.Repaint();
                }
            }
        }
    }

    void HandleInput(Event guiEvent)
    {
        Ray mouseRay = HandleUtility.GUIPointToWorldRay(guiEvent.mousePosition);
        float dstToDrawPlane = (ringOrigin.y - mouseRay.origin.y) / mouseRay.direction.y;
        Vector3 mousePosition = mouseRay.GetPoint(dstToDrawPlane);

        int hi = GetHoverIndex(mousePosition);
        if (hi != hoverIndex && !pressed) {needsRepaint=true; hoverIndex=hi;}

        if (guiEvent.type == EventType.MouseDown && guiEvent.button == 0 && guiEvent.modifiers == EventModifiers.None){
            pressed = true; needsRepaint=true;
        } else if (guiEvent.type == EventType.MouseUp && guiEvent.button == 0 && guiEvent.modifiers == EventModifiers.None) {
            pressed = false; needsRepaint = true;
        }

        difference = mousePosition - lastPos;

        if (pressed && hoverIndex != -1) {
            MovePoint(hoverIndex, mousePosition);
            shape.GetDefaultSetup();
            needsRepaint=true;
        }

        lastPos = mousePosition;
    }

    int GetHoverIndex(Vector3 pos) {
        for (int i = 0; i < shape.polarVertices.Length; i++)
        {
            if (Vector3.Distance(pos, ringOrigin + GetShapePoint(shape.polarVertices[i])) < shape.handlesRadius) {
                return i;
            }
        }
        return -1;
    } 

    void MovePoint(int i, Vector3 pos) {
        /* Vector3 v = GetShapePoint(shape.polarVertices[i]);
        v += movement; */
        pos.y = 0;
        SetShapePoint(i, pos);
    }

    void Draw()
    {
        // drawing black disc
        Handles.color = Color.black;
        Handles.DrawSolidDisc(ringOrigin, Vector3.up, .2f + halfPI);

        // drawing polar grid
        Handles.color = new Color(0.8f, 0.5f, 0.5f);
        for (int i = 0; i < ringEdges; i++) {
            Handles.DrawLine(ring[i], ringOrigin, 1);
        }
        Handles.DrawWireDisc(ringOrigin, Vector3.up, halfPI, 1);

        // drawing the shape
        Handles.color = shape.color * 1.5f;
        for (int i = 0; i < shape.polarVertices.Length; i++)
        {
            Handles.DrawLine(GetShapePoint(shape.polarVertices[i]) + ringOrigin, GetShapePoint(shape.polarVertices[(i+1)%shape.polarVertices.Length]) + ringOrigin, 1f);
        }
        Handles.color = shape.color;
        for (int i = 0; i < shape.polarVertices.Length; i++)
        {
            if (hoverIndex == i) {
                if (!pressed) {
                    Handles.color = shape.color * 2;
                } else {
                    Handles.color = new Color(0.9f, 0.9f, 0.9f, 1);
                }
                Handles.DrawSolidDisc(GetShapePoint(shape.polarVertices[i]) + ringOrigin, Vector3.up, shape.handlesRadius);
                Handles.color = shape.color;
            } else {
                Handles.DrawSolidDisc(GetShapePoint(shape.polarVertices[i]) + ringOrigin, Vector3.up, shape.handlesRadius);
            }
        }

        needsRepaint = false;
    }

    Vector3 GetShapePoint(Vector2 v) {
        v.x *= shape.scale;
        return new Vector3(-v.x * Mathf.Cos(Deg2Rad * v.y), 0, v.x * Mathf.Sin(Deg2Rad * v.y));
    }

    void SetShapePoint(int i, Vector3 v) {
        Vector2 vv = new Vector2((v).magnitude / shape.scale, Rad2Deg * Mathf.Atan2(v.z, -v.x));
        if (vv.y < 0) {vv.y += 360;}
        Undo.RecordObject(shape, "changing position of vertex " + i);
        shape.polarVertices[i] = vv;
    }

    void OnEnable()
    {
        shape = (SphShape)target;

        ring = new Vector3[ringEdges];
        float angle = 0;
        for (int i = 0; i < ringEdges; i++)
        {
            ring[i] = ringOrigin + halfPI * new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle));
            angle += TAU / (float)ringEdges;
        }
    }

}