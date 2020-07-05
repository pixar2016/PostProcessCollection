﻿
using UnityEngine;
using UnityEngine.Rendering;

namespace Pixar
{
    [ExecuteInEditMode]
    [RequireComponent(typeof(Camera))]
    public class AmbientOcclusion : MonoBehaviour
    {
        
        #region Editable attributes

        [SerializeField] Light _light;
        [SerializeField, Range(0, 5)] float _rejectionDepth = 0.5f;
        [SerializeField, Range(4, 32)] int _sampleCount = 16;
        [SerializeField, Range(0, 1)] float _temporalFilter = 0.5f;
        [SerializeField] bool _downsample = false;

        #endregion

        #region Internal resources

        [SerializeField] Shader _shader;
        [SerializeField] NoiseTextures _noiseTextures;

        #endregion

        #region Temporary objects

        public Material _material;
        public float _Convergence = 0;
        RenderTexture _prevMaskRT1, _prevMaskRT2;
        CommandBuffer _command1, _command2;

        // We track the VP matrix without using previousViewProjectionMatrix
        // because it's not available for use in OnPreCull.
        Matrix4x4 _previousVP = Matrix4x4.identity;

        #endregion

        #region MonoBehaviour implementation

        void OnDestroy()
        {
            // Release temporary objects.
            if (_material != null)
            {
                if (Application.isPlaying)
                    Destroy(_material);
                else
                    DestroyImmediate(_material);
            }

            if (_prevMaskRT1 != null) RenderTexture.ReleaseTemporary(_prevMaskRT1);
            if (_prevMaskRT2 != null) RenderTexture.ReleaseTemporary(_prevMaskRT2);

            if (_command1 != null) _command1.Release();
            if (_command2 != null) _command2.Release();
        }

        void OnPreCull()
        {
            // Update the temporary objects and build the command buffers for
            // the target light.

            UpdateTempObjects();

            if (_light != null)
            {
                BuildCommandBuffer();
                _light.AddCommandBuffer(LightEvent.AfterScreenspaceMask, _command1);
                _light.AddCommandBuffer(LightEvent.AfterScreenspaceMask, _command2);
            }
        }

        void OnPreRender()
        {
            // We can remove the command buffer before starting render in this
            // camera. Actually this should be done in OnPostRender, but it
            // crashes for some reasons. So, we do this in OnPreRender instead.

            if (_light != null)
            {
                _light.RemoveCommandBuffer(LightEvent.AfterScreenspaceMask, _command1);
                _light.RemoveCommandBuffer(LightEvent.AfterScreenspaceMask, _command2);
                _command1.Clear();
                _command2.Clear();
            }
        }

        void Update()
        {
            // We require the camera depth texture.
            GetComponent<Camera>().depthTextureMode |= DepthTextureMode.Depth;
        }

        #endregion

        #region Internal methods

        // Calculates the view-projection matrix for GPU use.
        static Matrix4x4 CalculateVPMatrix()
        {
            var cam = Camera.current;
            var p = cam.nonJitteredProjectionMatrix;
            var v = cam.worldToCameraMatrix;
            return GL.GetGPUProjectionMatrix(p, true) * v;
        }

        // Get the screen dimensions.
        Vector2Int GetScreenSize()
        {
            var cam = Camera.current;
            var div = _downsample ? 2 : 1;
            return new Vector2Int(cam.pixelWidth / div, cam.pixelHeight / div);
        }

        // Update the temporary objects for the current frame.
        void UpdateTempObjects()
        {

            if (_prevMaskRT2 != null)
            {
                RenderTexture.ReleaseTemporary(_prevMaskRT2);
                _prevMaskRT2 = null;
            }
            if (_light == null) return;
            if (_material == null)
            {
                _shader = Shader.Find("Pixar/AmbientOcclusion");
                _material = new Material(_shader);
                _material.hideFlags = HideFlags.DontSave;
            }
            if (_command1 == null || _command2 == null)
            {
                _command1 = new CommandBuffer();
                _command2 = new CommandBuffer();
                _command1.name = "Ray Tracing";
                _command2.name = "Temporal Filter";
            }
            else
            {
                _command1.Clear();
                _command2.Clear();
            }

            _material.SetFloat("_RejectionDepth", _rejectionDepth);
            _material.SetInt("_SampleCount", _sampleCount);

            _material.SetVector("_LightVector",
                transform.InverseTransformDirection(-_light.transform.forward) *
                _light.shadowBias / (_sampleCount - 1.5f)
            );

            var noiseTexture = _noiseTextures.GetTexture();
            var noiseScale = (Vector2)GetScreenSize() / noiseTexture.width;
            _material.SetTexture("_NoiseTex", noiseTexture);
            _material.SetVector("_NoiseScale", noiseScale);
            _material.SetFloat("_Convergence", _Convergence);

            _material.SetMatrix("_Reprojection", _previousVP * transform.localToWorldMatrix);
            _previousVP = CalculateVPMatrix();
        }

        // Build the command buffer for the current frame.
        void BuildCommandBuffer()
        {
            // Allocate the temporary shadow mask RT.
            var maskSize = GetScreenSize();
            var maskFormat = RenderTextureFormat.R8;
            var tempMaskRT = RenderTexture.GetTemporary(maskSize.x, maskSize.y, 0, maskFormat);

            // Command buffer 1: raytracing and temporal filter
            if (_temporalFilter == 0)
            {
                // Do raytracing and output to the temporary shadow mask RT.
                _command1.SetGlobalTexture(Shader.PropertyToID("_ShadowMask"), BuiltinRenderTextureType.CurrentActive);
                _command1.SetRenderTarget(tempMaskRT);
                _command1.DrawProcedural(Matrix4x4.identity, _material, 0, MeshTopology.Triangles, 3);
            }
            else
            {
                // Do raytracing and output to the unfiltered mask RT.
                var unfilteredMaskID = Shader.PropertyToID("_UnfilteredMask");
                _command1.SetGlobalTexture(Shader.PropertyToID("_ShadowMask"), BuiltinRenderTextureType.CurrentActive);
                _command1.GetTemporaryRT(unfilteredMaskID, maskSize.x, maskSize.y, 0, FilterMode.Point, maskFormat);
                _command1.SetRenderTarget(unfilteredMaskID);
                _command1.DrawProcedural(Matrix4x4.identity, _material, 0, MeshTopology.Triangles, 3);

                // Apply the temporal filter and output to the temporary shadow mask RT.
                _command1.SetGlobalTexture(Shader.PropertyToID("_PrevMask"), _prevMaskRT1);
                _command1.SetRenderTarget(tempMaskRT);
                _command1.DrawProcedural(Matrix4x4.identity, _material, 1, MeshTopology.Triangles, 3);
            }

            // Command buffer 2: shadow mask composition
            if (_downsample)
            {
                // Downsample enabled: Use upsampler for the composition.
                _command2.SetGlobalTexture(Shader.PropertyToID("_TempMask"), tempMaskRT);
                _command2.DrawProcedural(Matrix4x4.identity, _material, 3, MeshTopology.Triangles, 3);
            }
            else
            {
                // No downsample: Use simple blit.
                _command2.Blit(tempMaskRT, BuiltinRenderTextureType.CurrentActive);
            }

            // Update the filter history.
            _prevMaskRT2 = _prevMaskRT1;
            _prevMaskRT1 = tempMaskRT;
        }

        #endregion
    }
}

