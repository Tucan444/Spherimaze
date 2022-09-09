using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(Lighting))]
public class LightingEditor : Editor
{
    public override void OnInspectorGUI() {
        base.OnInspectorGUI();

        Lighting lighting = (Lighting)target;

        if (lighting.useBakedLighting) {
            lighting.lightmaps = (Texture2DArray)EditorGUILayout.ObjectField("Lightmaps", lighting.lightmaps, typeof (Texture2DArray), false);
            lighting.softShadows = EditorGUILayout.Toggle("Soft shadows", lighting.softShadows);
            if(lighting.softShadows) {
                lighting.softShadowDetail = EditorGUILayout.IntSlider("Soft shadow detail", lighting.softShadowDetail, 1, 5);

                float originalWidth = EditorGUIUtility.labelWidth;
                EditorGUIUtility.labelWidth = 140;
                lighting.softShadowCorrection = EditorGUILayout.Toggle("Soft shadow correction", lighting.softShadowCorrection);
                EditorGUIUtility.labelWidth = originalWidth;
            }
            lighting.detail = EditorGUILayout.IntSlider("Detail", lighting.detail, 1, 12);
            if (lighting.baking) {
                lighting.stopBaking = EditorGUILayout.Toggle("Stop baking: ", lighting.stopBaking);
            }

            if (GUILayout.Button("Bake lighting")) {
                lighting.BakeLighting();
            }
        }

        if (GUILayout.Button("Add point light")) {
            lighting.AddPointLight();
        }
    }
}
