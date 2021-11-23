//using FFmpegOut;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Recorder;
using UnityEngine;
using System.IO;
using System;
using UnityEngine.Playables;

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
    [Range(0,6)]public float Gamma;
    [Range(0,1)] public float Saturation;
    [Range(0,16)] public float Exposure;

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
    [SerializeField] PlayableDirector _timeline;
    private float _customFixedDeltaTime = 1f/30f;

    private bool _previz = false;
    private DateTime _StartTime;

    public RenderTexture Image
    {
        get
        {
            return _converged;
        }
    }

    private void Awake()
    {
        if (Record)
        {
            _customFixedDeltaTime =  1f / (30f *  (float)FrameSamples);
        }
        else {            
            _customFixedDeltaTime = 1f/30f;
            QualitySettings.vSyncCount = 0; 
            Application.targetFrameRate = 30;
        } 

        _camera = GetComponent<Camera>();
        _transformsToWatch.Add(transform);
        _StartTime = DateTime.Now;
    }

    public void SetDirty()
    {
        _currentSample = 0;
    }

    private void OnEnable()
    {
        _currentSample = 0;
    }
     
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Q))
        {
            ScreenCapture.CaptureScreenshot(Time.time + "-" + _currentSample + ".png");
        }

        if (Input.GetKeyDown(KeyCode.Z))
        {
            _previz = !_previz;
            Bounces = _previz ? 1 : 2;
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
        RayTracingShader.SetMatrix("_CameraToWorld", _camera.cameraToWorldMatrix);
        RayTracingShader.SetMatrix("_CameraInverseProjection", _camera.projectionMatrix.inverse);
        RayTracingShader.SetVector("_PixelOffset", 1024f * new Vector2(UnityEngine.Random.value, UnityEngine.Random.value));
        RayTracingShader.SetFloat("_Seed", UnityEngine.Random.value);

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
        RayTracingShader.SetFloat("_RNG", UnityEngine.Random.value);
        RayTracingShader.SetFloat("_TFAR", _camera.farClipPlane);


        RayTracingShader.SetFloat("_SPP", SPP);
        RayTracingShader.SetFloat("_Bounces", Bounces);

        _addMaterial.SetFloat("_Sample", _currentSample);

        _addMaterial.SetTexture("_Depth", _targetDepth);

        if (!_previz)
        {
            _tonemapper.SetFloat("_Exposure", Exposure);
            _tonemapper.SetFloat("_Gamma", Gamma);
        } else
        {
            _tonemapper.SetFloat("_Exposure", 1);
            _tonemapper.SetFloat("_Gamma", 0.5f);
        }
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

    private void Render(RenderTexture destination, double time)
    {
        if (Record)
        {
          _timeline.time = time;
        }
        
        // Make sure we have a current render target
        InitRenderTexture();

        int threadGroupsX = Mathf.CeilToInt(Screen.width / 8.0f);
        int threadGroupsY = Mathf.CeilToInt(Screen.height / 8.0f);

        // Set the target and dispatch the compute shader
        RayTracingShader.SetTexture(0, "Result", _target );
        RayTracingShader.Dispatch(0, threadGroupsX, threadGroupsY, 1);

       if (Reproject)
       {
            Graphics.Blit(_converged, _convergedLastFrame);
            Graphics.Blit(_confidenceConverged, _confidenceConvergedLastFrame);
            ReprojectionShader.SetTexture(0, "_LastFrameConverged", _convergedLastFrame);
            ReprojectionShader.SetTexture(0, "_ConfidenceConvergedLastFrame", _confidenceConvergedLastFrame);
            ReprojectionShader.SetTexture(0, "_ThisFrame", _target);

            ReprojectionShader.SetTexture(0, "_Result", _converged);
            ReprojectionShader.SetTexture(0, "_ResultConfidence", _confidenceConverged);
            ReprojectionShader.SetFloat("_Sample", _currentSample);

            ReprojectionShader.SetMatrix("_WorldToLastFrameProj", _camera.projectionMatrix * m_worldToLastFrame);
            ReprojectionShader.SetMatrix("_ThisFrameToWorld", _camera.cameraToWorldMatrix);
            ReprojectionShader.SetMatrix("_WorldToLastFrame", m_worldToLastFrame);
            ReprojectionShader.SetMatrix("_LastFrameToWorld", m_worldToLastFrame.inverse);
 
            ReprojectionShader.SetMatrix("_CameraInverseProjection", _camera.projectionMatrix.inverse);
            ReprojectionShader.Dispatch(0, threadGroupsX, threadGroupsY, 1);
       }
       else 
       {
            Graphics.Blit(_target, _converged, _addMaterial);
       }

      //  Graphics.Blit(_converged, _pngRenderTexture, _copyMaterial);

       Graphics.Blit(_converged, _pngRenderTexture, _tonemapper);
       Graphics.Blit(_converged, destination, _tonemapper);
    }

    bool didSave = false;
    double fakeTime  = 0;

    private void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        SetShaderParameters();

        if (!Record || _currentSample < (float)FrameSamples * ExposurePercent) {
            didSave = false;
            Render(destination, fakeTime);
        }
        fakeTime += _customFixedDeltaTime;
        
        _currentSample++;
        m_worldToLastFrame = _camera.worldToCameraMatrix;

        if ((_currentSample > FrameSamples) && Record && !didSave)
        {
            // This is where we would drop in the frame 
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
            didSave = true;
            _currentSample = 0;
            _renderedFrameNum++;
            
            float pct = 100 * ((float)_renderedFrameNum  / (float)numFrames);
            float elapsed = (float)( DateTime.Now - _StartTime).TotalSeconds;
            float timePerFrame = elapsed / (float)_renderedFrameNum;
            float remain = (float)(numFrames - _renderedFrameNum) * timePerFrame;

            Debug.Log($"{pct:0.0}% \t {_renderedFrameNum} /{numFrames}\t { timePerFrame:0}s/frame \t \t {elapsed:0}s elapsed  {remain:0}s remain ");

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
