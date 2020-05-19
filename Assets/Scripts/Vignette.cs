using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
[RequireComponent(typeof(Camera))]
public class Vignette : MonoBehaviour
{
    Material _material;
    Shader _shader;
    public float _fallOff = 0.1f;
    private void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        if(_material == null)
        {
            _shader = Shader.Find("Pixar/Vignette");
            _material = new Material(_shader);
            _material.hideFlags = HideFlags.DontSave;
        }
        var cam = GetComponent<Camera>();
        _material.SetVector("_Aspect", new Vector2(cam.aspect, 1.0f));
        _material.SetFloat("_FallOff", _fallOff);
        Graphics.Blit(source, destination, _material, 0);
    }
}
