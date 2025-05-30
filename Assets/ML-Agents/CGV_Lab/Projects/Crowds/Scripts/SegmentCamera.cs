using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SegmentCamera : MonoBehaviour
{
    [System.Serializable]
    public struct Segment {
        public string tag;
        public Color32 color;
    }

    public Segment[] segments;
    public Camera segmentCamera;
    public Shader segmentShader;
    public Color32 globalDefaultColor;

    private Dictionary<string, Color32> sementicDict = new Dictionary<string, Color32>();

    // Start is called before the first frame update
    void Start()
    {
        SetSegmentCamera();
    }

    void SetSegmentCamera()
    {
        foreach (var segment in segments)
        {
            sementicDict.Add(segment.tag, segment.color);
        }

        //SetTerrainColor();
        SetCrowdColor();
        SetBuildingColor();

        Shader.SetGlobalColor("_SegmentColor", globalDefaultColor);
        segmentCamera.SetReplacementShader(segmentShader, "");
    }

    void SetTerrainColor()
    {
        Terrain t = FindObjectsOfType<Terrain>()[0];
        var mpb = new MaterialPropertyBlock();

        if (sementicDict.TryGetValue(t.transform.tag, out Color32 outColor))
        {
            mpb.SetColor("_SegmentColor", outColor);
            t.SetSplatMaterialPropertyBlock(mpb);
        }
    }

    void SetCrowdColor()
    {
        var renderers = FindObjectsOfType<SkinnedMeshRenderer>();
        var mpb = new MaterialPropertyBlock();

        foreach (var r in renderers)
        {
            if (sementicDict.TryGetValue(r.transform.tag, out Color32 outColor))
            {
                mpb.SetColor("_SegmentColor", outColor);
                r.SetPropertyBlock(mpb);
            }
        }
    }

    void SetBuildingColor()
    {
        var renderers = FindObjectsOfType<MeshRenderer>();
        var mpb = new MaterialPropertyBlock();

        foreach (var r in renderers)
        {
            if (sementicDict.TryGetValue(r.transform.tag, out Color32 outColor))
            {
                mpb.SetColor("_SegmentColor", outColor);
                r.SetPropertyBlock(mpb);
            }
        }
    }
}