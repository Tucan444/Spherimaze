using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(PointLight))]
public class PointLightEditor : Editor
{
    public override void OnInspectorGUI() {
        base.OnInspectorGUI();

        PointLight pointLight = (PointLight)target;

        if (!pointLight.linear) {
            Undo.RecordObject(pointLight, "dimension changed");
            pointLight._3D = EditorGUILayout.Toggle("3D", pointLight._3D);
        } else {
            Undo.RecordObject(pointLight, "boundary changed");
            using (new GUILayout.HorizontalScope()) {
                GUILayout.Label("Boundary", GUILayout.Width(120));

                pointLight.boundary = EditorGUILayout.Slider(pointLight.boundary, 0, Mathf.PI);
            }
        }
    }
}
