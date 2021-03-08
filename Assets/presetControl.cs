using UnityEngine;
using UnityEditor;
using System;
using System.IO;

public static class ExtensionMethod
{
    public static Texture2D toTexture2D(this RenderTexture rTex)
    {
        Texture2D tex = new Texture2D(rTex.height, rTex.height, TextureFormat.RGB24, false);

        tex.Resize(128, 128);

        RenderTexture.active = rTex;
        tex.ReadPixels(new Rect(0, 0, rTex.width, rTex.height), 0, 0);
        tex.Apply();
        return tex;
    }
}

public class presetControl : MonoBehaviour
{
    public RayTracing m_fractal;

    public static string Path(int n)
    {
        return $"Assets/Presets/{n}.txt";
    }

    public static string ImagePath(int n)
    {
        return $"Assets/Presets/{n}.png";
    }

    public void SavePreset(int index)
    {
        StreamWriter writer = new StreamWriter(Path(index), true);

        string json = JsonUtility.ToJson(m_fractal);
        json = StripInstanceIDs(json);
        string transform = SerializeTransform();
        writer.WriteLine(json);
        writer.WriteLine(transform);
        writer.Close();
        SaveTextureAsPNG(m_fractal.Image, ImagePath(index));
    }

    private string StripInstanceIDs(string json)
    {
        // bs instanceID workaround

        var fields = json.Split(',');

        string result = string.Empty;

        for(int i = 0; i < fields.Length; i++)
        {
            if (!fields[i].Contains("instanceID"))
            {
                result += fields[i] + ",";
            }
        }

        if (result[0] != '{')
        {
            result = '{' + result;
        }
        if (result[result.Length-1] != '}')
        {
            result = result + "}";
        }

        return result;
    }

    public static void SaveTextureAsPNG(RenderTexture texture, string fullPath)
    {
        byte[] _bytes = texture.toTexture2D().EncodeToPNG();
        File.WriteAllBytes(fullPath, _bytes);
    }

    private string SerializeTransform()
    {
        var t = m_fractal.transform;

        string json = String.Empty;
        json += t.position.x + ",";
        json += t.position.y + ",";
        json += t.position.z + ",";

        json += t.rotation.x + ",";
        json += t.rotation.y + ",";
        json += t.rotation.z + ",";
        json += t.rotation.w ;

        Debug.Log(json);
        return json;
    }

    private void DeserializeTransform(string csv)
    {
        var fields = csv.Split(',');

        m_fractal.transform.position = new Vector3(
            float.Parse(fields[0]),
            float.Parse(fields[1]),
            float.Parse(fields[2]));

        m_fractal.transform.rotation = new Quaternion(
           float.Parse(fields[3]),
           float.Parse(fields[4]),
           float.Parse(fields[5]),
           float.Parse(fields[6]));
    }

    public Transform m_quaternionTranfrorm;
    public Texture2D m_skyboxTexture;
    public Material m_addMaterial;
    public ComputeShader ReprojectionShader;
    public ComputeShader RayTracingShader;

    public void LoadPreset(int index)
    {
        string[] lines = File.ReadAllLines(Path(index));
        JsonUtility.FromJsonOverwrite(lines[0], m_fractal);

        // bs instanceID workaround
        m_fractal._camera = Camera.main;
        m_fractal.m_quaternionTransform = m_quaternionTranfrorm;
        m_fractal.SkyboxTexture = m_skyboxTexture;
        m_fractal.ReprojectionShader = ReprojectionShader;
        m_fractal.RayTracingShader = RayTracingShader;
            
        DeserializeTransform(lines[1]);
        m_fractal.SetDirty();
    }

}
