using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
namespace CityForge.Environment {
// A modest-resolution reflection for the prepared pond scene; excludes Unity's Water layer.
[ExecuteAlways]
public sealed class PondPlanarReflection : MonoBehaviour {
public Camera sourceCamera;
public Material waterMaterial;
public float waterHeight=6.8f;
public int textureWidth=1024;
public float updatesPerSecond=8;
public bool updateOnCameraMotion;
public bool renderShadows=true;
Matrix4x4 lastView,lastProjection;
Camera reflectionCamera;RenderTexture reflection;double nextUpdate;bool rendering;
void OnEnable(){nextUpdate=0;}
void LateUpdate(){if(Time.realtimeSinceStartupAsDouble>=nextUpdate||(updateOnCameraMotion&&sourceCamera&&(lastView!=sourceCamera.worldToCameraMatrix||lastProjection!=sourceCamera.projectionMatrix))){RenderNow();nextUpdate=Time.realtimeSinceStartupAsDouble+1/Mathf.Max(1,updatesPerSecond);}}
public void RenderNow(){if(rendering||sourceCamera==null||waterMaterial==null||RenderPipelineManager.currentPipeline==null)return;rendering=true;bool previous=GL.invertCulling;try{
if(!reflectionCamera){var go=new GameObject("Pond reflection camera"){hideFlags=HideFlags.HideAndDontSave};reflectionCamera=go.AddComponent<Camera>();reflectionCamera.enabled=false;}
int height=Mathf.Max(128,Mathf.RoundToInt(textureWidth/sourceCamera.aspect));if(!reflection||reflection.width!=textureWidth||reflection.height!=height){if(reflection){reflection.Release();DestroySafe(reflection);}reflection=new RenderTexture(textureWidth,height,24,RenderTextureFormat.ARGBHalf){name="Pond live reflections",hideFlags=HideFlags.HideAndDontSave};reflection.Create();}
reflectionCamera.CopyFrom(sourceCamera);reflectionCamera.enabled=false;reflectionCamera.cameraType=CameraType.Reflection;reflectionCamera.cullingMask=sourceCamera.cullingMask&~((1<<4)|(1<<2));reflectionCamera.clearFlags=CameraClearFlags.SolidColor;reflectionCamera.backgroundColor=new Color(.12f,.25f,.42f);reflectionCamera.targetTexture=reflection;
var position=sourceCamera.transform.position;position.y=2*waterHeight-position.y;reflectionCamera.transform.position=position;var forward=sourceCamera.transform.forward;forward.y=-forward.y;var up=sourceCamera.transform.up;up.y=-up.y;reflectionCamera.transform.rotation=Quaternion.LookRotation(forward,up);
var mirror=Matrix4x4.identity;mirror.m11=-1;mirror.m13=2*waterHeight;reflectionCamera.worldToCameraMatrix=sourceCamera.worldToCameraMatrix*mirror;reflectionCamera.projectionMatrix=sourceCamera.projectionMatrix;
var point=reflectionCamera.worldToCameraMatrix.MultiplyPoint(new Vector3(0,waterHeight+.025f,0));var normal=reflectionCamera.worldToCameraMatrix.MultiplyVector(Vector3.up).normalized;reflectionCamera.projectionMatrix=reflectionCamera.CalculateObliqueMatrix(new Vector4(normal.x,normal.y,normal.z,-Vector3.Dot(point,normal)));
var data=reflectionCamera.GetUniversalAdditionalCameraData();data.renderPostProcessing=false;data.renderShadows=renderShadows;data.requiresColorTexture=false;data.requiresDepthTexture=false;data.antialiasing=AntialiasingMode.None;
GL.invertCulling=!previous;RenderPipeline.SubmitRenderRequest(reflectionCamera,new UniversalRenderPipeline.SingleCameraRequest{destination=reflection});lastView=sourceCamera.worldToCameraMatrix;lastProjection=sourceCamera.projectionMatrix;waterMaterial.SetTexture("_Reflection",reflection);waterMaterial.SetMatrix("_ReflectionVP",GL.GetGPUProjectionMatrix(reflectionCamera.projectionMatrix,true)*reflectionCamera.worldToCameraMatrix);
}finally{GL.invertCulling=previous;rendering=false;}}
void OnDisable(){if(reflectionCamera)DestroySafe(reflectionCamera.gameObject);if(reflection){reflection.Release();DestroySafe(reflection);}reflectionCamera=null;reflection=null;}
static void DestroySafe(Object obj){if(Application.isPlaying)Destroy(obj);else DestroyImmediate(obj);}
}
}
