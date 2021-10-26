//using FFmpegOut;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Recorder;
using UnityEngine;
using System.IO;

[ExecuteInEditMode]
public class RayTracing : MonoBehaviour
{
    public ComputeShader RayTracingShader;
    public ComputeShader ReprojectionShader;
 
    public Texture SkyboxTexture;
    public Material _addMaterial;
    public Material _copyMaterial;
    public Material _tonemapper;
    public bool Integrate;
    public bool Reproject;


    [Header("Render Params")]
    [Range(0,1)] public float Specular;
    [Range(0,1)] public float Smoothness;
    [Range(0,1)] public float Threshold;
    [Range(0,2048)] public int Steps;
    [Range(0,1)] public float _StepRatio;
    public Color SkyColorA;
    public Color SkyColorB;
    public Vector3 Emission;
    [Range(0,10)] public int Palette;
    public Vector2 ColorParam;
    [Range(0,1)]public float Gamma;
    [Range(0,1)] public float Saturation;
    [Range(0,3)] public float Exposure;

    [Header("Fractal Params")]
    public Transform m_quaternionTransform;
    [Range(1,20)] public float Levels;
    public Vector4 ParamA;
    public Vector4 ParamB;
    public Vector4 ParamC;
    public Vector4 ParamD;

    [Header("Record Params")]
    public bool Record;
    public int FrameSamples;
    [Range(0,1)] public float ExposurePercent;
    public int numFrames;

    [Header("Quality")]
    [Range(0, 8)] public int SPP;
    [Range(0, 4)] public int Bounces;

    public Camera _camera;
    private float _lastFieldOfView;
    private RenderTexture _target;
    private RenderTexture _targetDepth;
    private RenderTexture _targetB;
    private RenderTexture _targetBDepth;
    public RenderTexture _converged;
    private RenderTexture _confidenceConverged;
    private RenderTexture _confidenceConvergedLastFrame;
    
    private uint _currentSample = 0;
    private List<Transform> _transformsToWatch = new List<Transform>();

    float _renderedFrameNum = 0;
    Matrix4x4 m_worldToLastFrame;
    RenderTexture _convergedLastFrame;
    public RenderTexture outputFrame;
    //CustomCameraCapture m_capture;
    Texture2D _pngTexture;
    RenderTexture _pngRenderTexture;

    public RenderTexture Image
    {
        get
        {
            return _converged;
        }
    }

    private void Awake()
    {
        Time.timeScale = 1;
        if (Record)
        {
            Time.timeScale = 1f/FrameSamples;
        }

        _camera = GetComponent<Camera>();
        _transformsToWatch.Add(transform);
    }

    public void SetDirty()
    {
        _currentSample = 0;
    }

    private void OnEnable()
    {
        _currentSample = 0;
        Time.timeScale = 1f / FrameSamples;
    }
     
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.F12))
        {
            ScreenCapture.CaptureScreenshot(Time.time + "-" + _currentSample + ".png");
        }

        if (Input.GetKeyDown(KeyCode.Space))
        {
            Integrate = !Integrate;
        }

        if (_camera.fieldOfView != _lastFieldOfView || !Integrate)
        {
            _currentSample = 0;
            _lastFieldOfView = _camera.fieldOfView;
        }

        if (!Record && !Reproject)
            foreach (Transform t in _transformsToWatch)
            {
                if (t.hasChanged)
                {
                    _currentSample = 0;
                    t.hasChanged = false;
                }
            }
    }

    private void SetShaderParameters()
    {
        RayTracingShader.SetTexture(0, "_SkyboxTexture", SkyboxTexture);
        RayTracingShader.SetMatrix("_CameraToWorld", _camera.transform.localToWorldMatrix);
        RayTracingShader.SetMatrix("_CameraInverseProjection", invProjection);
        RayTracingShader.SetVector("_PixelOffset", 1024f * new Vector2(Random.value, Random.value));
        RayTracingShader.SetFloat("_Seed", Random.value);

        RayTracingShader.SetVector("u_paramA", ParamA);
        RayTracingShader.SetVector("u_paramB", ParamB);
        RayTracingShader.SetVector("u_paramC", ParamC);
        RayTracingShader.SetVector("u_paramD", ParamD);

        RayTracingShader.SetVector("_SkyColorA", SkyColorA);
        RayTracingShader.SetVector("_SkyColorB", SkyColorB);
        RayTracingShader.SetVector("_EmisisonRange", Emission);
        RayTracingShader.SetVector("_ColorParam", ColorParam);
        if (m_quaternionTransform != null)
        {
            RayTracingShader.SetVector("_Quaternion", new Vector4(
            m_quaternionTransform.localRotation.x,
            m_quaternionTransform.localRotation.y,
            m_quaternionTransform.localRotation.y,
            m_quaternionTransform.localRotation.w));
        }
        
        RayTracingShader.SetFloat("_StepRatio", _StepRatio);

        RayTracingShader.SetFloat("_Palette", Palette);
        RayTracingShader.SetFloat("_Saturation", Saturation);

        RayTracingShader.SetFloat("_Specular", Specular);
        RayTracingShader.SetFloat("_Smoothness", Smoothness);
        RayTracingShader.SetFloat("_Threshold", Threshold);
        RayTracingShader.SetFloat("_Steps", Steps);
        RayTracingShader.SetFloat("_LEVELS", Levels);
        RayTracingShader.SetFloat("_Gamma", Gamma);
        RayTracingShader.SetFloat("_RNG", Random.value);
        RayTracingShader.SetFloat("_TFAR", _camera.farClipPlane);


        RayTracingShader.SetFloat("_SPP", SPP);
        RayTracingShader.SetFloat("_Bounces", Bounces);

        _addMaterial.SetFloat("_Sample", _currentSample);

        _addMaterial.SetTexture("_Depth", _targetDepth);

        _tonemapper.SetFloat("_Exposure", Exposure);
        _tonemapper.SetFloat("_Gamma", Gamma);

    }


    private void InitRenderTexture()
    {
        if (_target == null || _target.width != Screen.width || _target.height != Screen.height)
        {
            // Release render texture if we already have one
            if (_target != null)
            {
                _target.Release();
                _targetDepth.Release();
                _targetB.Release();
                _targetBDepth.Release();
                _converged.Release();
            }

            // Get a render target for Ray Tracing
            _target = new RenderTexture(Screen.width, Screen.height, 0,
                RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear);

            _target.enableRandomWrite = true;
            _target.Create();
            _targetDepth = new RenderTexture(_target);
            _targetDepth.Create();
            _targetB = new RenderTexture(_target);
            _targetB.Create();
            _targetBDepth = new RenderTexture(_target);
            _targetBDepth.Create();
            _converged = new RenderTexture(_target);
            _converged.Create();
            _convergedLastFrame = new RenderTexture(_target);
            _convergedLastFrame.Create();
            _confidenceConverged = new RenderTexture(_target);
            _confidenceConverged.Create();
            _confidenceConvergedLastFrame = new RenderTexture(_target);
            _confidenceConvergedLastFrame.Create();
            _pngTexture = new Texture2D(Screen.width, Screen.height);
            _pngRenderTexture = new RenderTexture(Screen.width, Screen.height, 0, RenderTextureFormat.ARGB32);
            _pngRenderTexture.Create();
            // Reset sampling
            _currentSample = 0;
        }
    }


    public void ClearRendertexture(RenderTexture renderTexture)
    {

        RenderTexture rt = RenderTexture.active;
        RenderTexture.active = renderTexture;
        GL.Clear(true, true, Color.clear);
        RenderTexture.active = rt;
    }

    Matrix4x4 projection;
    Matrix4x4 invProjection;

    private void Render(RenderTexture destination)
    {

        //projection = GL.GetGPUProjectionMatrix(_camera.projectionMatrix, false);
        projection = _camera.nonJitteredProjectionMatrix;
        invProjection = projection.inverse;

        // Make sure we have a current render target
        InitRenderTexture();

        int threadGroupsX = Mathf.CeilToInt(Screen.width / 8.0f);
        int threadGroupsY = Mathf.CeilToInt(Screen.height / 8.0f);

        // Set the target and dispatch the compute shader
        RayTracingShader.SetTexture(0, "Result", _target );
        RayTracingShader.SetTexture(0, "ResultDepth", _targetDepth);
        RayTracingShader.Dispatch(0, threadGroupsX, threadGroupsY, 1);

       if (Reproject)
       {
            Graphics.Blit(_converged, _convergedLastFrame);
            Graphics.Blit(_confidenceConverged, _confidenceConvergedLastFrame);
            ReprojectionShader.SetTexture(0, "_LastFrameConverged", _convergedLastFrame);
            ReprojectionShader.SetTexture(0, "_ConfidenceConvergedLastFrame", _confidenceConvergedLastFrame);
            ReprojectionShader.SetTexture(0, "_ThisFrame", _target);
            ReprojectionShader.SetTexture(0, "_ThisFrameDepth", _targetDepth);

            ReprojectionShader.SetTexture(0, "_Result", _converged);
            ReprojectionShader.SetTexture(0, "_ResultConfidence", _confidenceConverged);
            ReprojectionShader.SetFloat("_Sample", _currentSample);

            ReprojectionShader.SetMatrix("_WorldToLastFrameProj", projection * m_worldToLastFrame);
            ReprojectionShader.SetMatrix("_ThisFrameToWorld", _camera.transform.localToWorldMatrix);
            ReprojectionShader.SetMatrix("_WorldToLastFrame", m_worldToLastFrame);
            
            ReprojectionShader.SetMatrix("_CameraInverseProjection", invProjection);
            ReprojectionShader.Dispatch(0, threadGroupsX, threadGroupsY, 1);
       }
       else 
       {
            Graphics.Blit(_target, _converged, _addMaterial);
       }

      //  Graphics.Blit(_converged, _pngRenderTexture, _copyMaterial);

       Graphics.Blit(_converged, destination, _tonemapper);
    }
 
      
    private void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        SetShaderParameters();
        
        if (!Record || _currentSample < (float)FrameSamples * ExposurePercent)
            Render(destination);

        _currentSample++;
        m_worldToLastFrame = _camera.transform.worldToLocalMatrix;

        if ((_currentSample > FrameSamples) && Record)
        {
            // This is where we drop in the frame 
            //m_capture.AddFrame(_converged);
                
            //then Save To Disk as PNG
            RenderTexture.active = _pngRenderTexture;
            _pngTexture.ReadPixels(new Rect(0, 0, _pngRenderTexture.width, _pngRenderTexture.height), 0, 0);
            _pngTexture.Apply();

            byte[] bytes = _pngTexture.EncodeToPNG();
            var dirPath = Application.dataPath + "/../frames/";
            if(!Directory.Exists(dirPath)) {
                Directory.CreateDirectory(dirPath);
            }
            File.WriteAllBytes(dirPath + _renderedFrameNum + ".png", bytes);

            _currentSample = 0;
            _renderedFrameNum++;
            if (_renderedFrameNum > numFrames)
            {
                EditorApplication.ExecuteMenuItem("Edit/Play");
                Record = false;
            }
        }
    }

    private void OnValidate()
    {
        //_currentSample = 0;
    }
}
