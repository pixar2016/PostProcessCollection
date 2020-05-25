
using UnityEngine;
using UnityEngine.Rendering;

namespace Pixar
{
    [ExecuteInEditMode]
    [RequireComponent(typeof(Camera))]
    public class AmbientOcclusion : MonoBehaviour
    {
        
        Shader _shader;
        Material _material;
        CommandBuffer _commander1, _commander2;
        public NoiseTextures _noiseTextures;
        public Light _light;
        public float _rejectionDepth = 0.1f;
        public int _sampleCount = 16;

        void Update()
        {
            GetComponent<Camera>().depthTextureMode |= DepthTextureMode.Depth;
        }
        void OnDestroy()
        {
            if (_material != null)
            {
                if (Application.isPlaying)
                    Destroy(_material);
                else
                    DestroyImmediate(_material);
            }
            if (_commander1 != null) _commander1.Release();
        }

        void OnPreCull()
        {
            InitGlobalObjects();
            BuildCommandBuffer();
            _light.AddCommandBuffer(LightEvent.AfterScreenspaceMask, _commander1);
        }

        void OnPreRender()
        {
            if (_light != null)
            {
                //_light.RemoveCommandBuffer(LightEvent.AfterScreenspaceMask, _commander1);
                //_light.RemoveCommandBuffer(LightEvent.AfterScreenspaceMask, _commander2);
                //_commander1.Clear();
                //_commander2.Clear();
            }
        }

        void InitGlobalObjects()
        {
            if (_light == null) return;
            if (_material == null)
            {
                _shader = Shader.Find("Pixar/AmbientOcclusion");
                _material = new Material(_shader);
                _material.hideFlags = HideFlags.DontSave;
            }
            if (_commander1 == null || _commander2 == null)
            {
                _commander1 = new CommandBuffer();
                _commander2 = new CommandBuffer();
                _commander1.name = "Ray Tracing";
                _commander2.name = "Temporal Filter";
            }
            else
            {
                _commander1.Clear();
                _commander2.Clear();
            }

            _material.SetFloat("_RejectionDepth", _rejectionDepth);
            _material.SetInt("_SampleCount", _sampleCount);

            var temp = transform.InverseTransformDirection(_light.transform.forward);
            _material.SetVector("_LightVector", temp);

            var noiseTexture = _noiseTextures.GetTexture();
            _material.SetTexture("_NoiseTex", noiseTexture);

        }

        void BuildCommandBuffer()
        {
            var cam = Camera.current;
            var tempTexture = RenderTexture.GetTemporary(cam.pixelWidth, cam.pixelHeight, 0, RenderTextureFormat.R8);

            _commander1.SetGlobalTexture(Shader.PropertyToID("_ShadowMask"), BuiltinRenderTextureType.CurrentActive);
            _commander1.SetRenderTarget(tempTexture);
            _commander1.DrawProcedural(Matrix4x4.identity, _material, 0, MeshTopology.Triangles, 3);
        }
    }
}

