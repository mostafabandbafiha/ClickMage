using UnityEngine;

namespace FPL
{
    [ExecuteInEditMode]
    [RequireComponent (typeof (Camera))]
    public class BuiltIn_CameraDepthNormal : MonoBehaviour
    {
        [SerializeField] Camera cam;
        private void OnEnable()
        {
            //get the camera and tell it to render a depthnormals texture
            if (cam == null) cam = GetComponent<Camera> ();
            
            //cam.depthTextureMode |= DepthTextureMode.Depth;
            cam.depthTextureMode = cam.depthTextureMode | DepthTextureMode.DepthNormals;
        }

        //Old Srp didn't include the IV Matrix

        //private int IVmatrix = Shader.PropertyToID ("UNITY_MATRIX_IV");
        //private void OnRenderImage(RenderTexture source, RenderTexture destination)
        //{
        //    Shader.SetGlobalMatrix (IVmatrix, cam.cameraToWorldMatrix);
        //    //renderSource to screen
        //    Graphics.Blit (source, destination);
        //}
    }
}
