using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace Pixar
{
	[ExecuteInEditMode]
    [RequireComponent(typeof(Camera))]
	public class ScanEffect : MonoBehaviour
	{
		public Material _material;

		public float ScanSpeed = 20;

		private float scanTimer = 0;

		private Vector3 scanPoint = Vector3.zero;

		Shader _shader;
		Camera _camera;

		void Awake()
		{
			_camera = GetComponent<Camera>();
			_camera.depthTextureMode = DepthTextureMode.Depth;
			_camera.depthTextureMode |= DepthTextureMode.DepthNormals;
			if (_material == null)
			{
				_shader = Shader.Find("Pixar/ScanEffectShader");
				_material = new Material(_shader);
				_material.hideFlags = HideFlags.DontSave;
			}
			_material.SetFloat("_ScanWidth", 20);
		}

		Matrix4x4 getFrustumCorner(){
			float aspect = _camera.aspect;
			float farPlaneDistance = _camera.farClipPlane;
			float _farDistanceUp = Mathf.Tan(_camera.fieldOfView / 2 * Mathf.Deg2Rad) * farPlaneDistance;
			Vector3 midup = _farDistanceUp * _camera.transform.up;
			Vector3 midright = _farDistanceUp * _camera.transform.right * aspect;
			Vector3 farPlaneMid = _camera.transform.forward * farPlaneDistance;

			Vector3 bottomLeft = farPlaneMid - midup - midright;
			Vector3 bottomRight = farPlaneMid - midup + midright;
			Vector3 upLeft = farPlaneMid + midup - midright;
			Vector3 upRight = farPlaneMid + midup + midright;
			
			Matrix4x4 frustumCorner = new Matrix4x4();
			frustumCorner.SetRow(0, bottomLeft);
			frustumCorner.SetRow(1, bottomRight);
			frustumCorner.SetRow(2, upRight);
			frustumCorner.SetRow(3, upLeft);

			return frustumCorner;
		}

		void Update(){
			RaycastHit hit;
			Ray ray = _camera.ScreenPointToRay(Input.mousePosition);
			if(Input.GetMouseButton(0) && Physics.Raycast(ray, out hit)){
				scanTimer = 0;
				scanPoint = hit.point;
			}
			scanTimer += Time.deltaTime * ScanSpeed/_camera.farClipPlane;
			_material.SetFloat("_ScanDepth", scanTimer);
			_material.SetMatrix("_FrustumCorner", getFrustumCorner());
			_material.SetVector("_ScanCenter", scanPoint);
			_material.SetMatrix("_CamToWorld", _camera.cameraToWorldMatrix);
		}

		private void OnRenderImage(RenderTexture source, RenderTexture destionation)
		{
			_material.SetFloat("_CamFar", _camera.farClipPlane);
			Graphics.Blit(source, destionation, _material);
		}
	}

}

