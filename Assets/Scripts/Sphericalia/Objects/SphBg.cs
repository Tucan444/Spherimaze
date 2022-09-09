using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteAlways]
public class SphBg : MonoBehaviour
{
    [Range(0, 0.96f)]public float fillSize = 0.9f;
    public Color bgColor = new Color(0.1f, 0.1f, 0.1f, 0.9f);
    public Color orthoBg = new Color(0, 0, 0, 1);
    public Texture2D bgTexture;
    Material m;
    
    GameObject fill;
    Material fillM;

    [HideInInspector] public bool triggered = false;

    void OnEnable() {
        SphSpaceManager.sb = this;
        m = GetComponent<Renderer>().sharedMaterial;
        fill = transform.GetChild(0).gameObject;
        fillM = fill.GetComponent<Renderer>().sharedMaterial;
        fill.transform.localScale = new Vector3(fillSize, fillSize, fillSize);
        UpdateStuff();
    }

    void OnValidate() {
        UpdateStuff();
    }

    void UpdateStuff() {
        try{
        
            fillM.mainTexture = bgTexture;
            if (bgTexture) {
                fillM.SetColor("_Color", new Color(1, 1, 1, 1));
            } else {
                fillM.SetColor("_Color", new Color(0.2f, 0.2f, 0.2f, 1));
            }

            m.SetColor("_Color", bgColor);
            fill.transform.localScale = new Vector3(fillSize, fillSize, fillSize);

        } catch (Exception e) {}
    }
}
