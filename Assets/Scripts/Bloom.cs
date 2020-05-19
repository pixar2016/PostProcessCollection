using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
[RequireComponent(typeof(Camera))]
public class Bloom : MonoBehaviour
{
    Material _material;
    Shader _shader;
    public float Threshold = 0.3f;
    public float Intensity = 0.2f;

    private void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        if (_material == null)
        {
            _shader = Shader.Find("Pixar/Bloom");
            _material = new Material(_shader);
        }
        _material.SetFloat("_Threshold", Threshold);
        _material.SetFloat("_Intensity", Intensity);
        var rt_width = source.width;
        var rt_height = source.height;
        var rt_downsample = RenderTexture.GetTemporary((int)rt_width / 4, (int)rt_height / 4, 0, RenderTextureFormat.DefaultHDR);
        Graphics.Blit(source, rt_downsample, _material, 0);
        var rt_threshold = RenderTexture.GetTemporary(rt_width, rt_height, 0, RenderTextureFormat.DefaultHDR);
        Graphics.Blit(rt_downsample, rt_threshold, _material, 1);
        RenderTexture.ReleaseTemporary(rt_downsample);
        var rt_temp = RenderTexture.GetTemporary(rt_width, rt_height, 0, RenderTextureFormat.DefaultHDR);
        for(int i = 0; i < 4; i++)
        {
            Graphics.Blit(rt_threshold, rt_temp, _material, 2);
            Graphics.Blit(rt_temp, rt_threshold, _material, 3);
        }
        _material.SetTexture("_BlurTex", rt_threshold);
        Graphics.Blit(source, destination, _material, 4);
        RenderTexture.ReleaseTemporary(rt_threshold);
        RenderTexture.ReleaseTemporary(rt_temp);
    }
}
