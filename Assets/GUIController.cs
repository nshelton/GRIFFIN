using UnityEngine;
using System.IO;
using System.Collections.Generic;

public class GUIController : MonoBehaviour
{
    [SerializeField] private presetControl m_switcher;

    private List<string> m_presets = new List<string>();
    private List<Texture2D> m_thumbnails = new List<Texture2D>();


    public static Texture2D LoadPNG(string filePath)
    {

        Texture2D tex = null;
        byte[] fileData;

        if (File.Exists(filePath))
        {
            fileData = File.ReadAllBytes(filePath);
            tex = new Texture2D(2, 2);
            tex.LoadImage(fileData); //..this will auto-resize the texture dimensions.
        }
        return tex;
    }

    void Start()
    {
        DirectoryInfo d = new DirectoryInfo(@"D:\GRIFFIN\Assets\Presets");//Assuming Test is your Folder
        FileInfo[] Files = d.GetFiles("*.txt"); //Getting Text files
        foreach (FileInfo file in Files)
        {
            var pName = file.Name.Replace(".txt", "");
            m_presets.Add(pName);
            var pngPath = $@"D:/GRIFFIN/Assets/Presets/{pName}.png";
            m_thumbnails.Add(LoadPNG(pngPath));
        }
    }


    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Z))
        {
            int n = m_presets.Count;
            m_switcher.SavePreset(n);
            m_presets.Add(n.ToString());
            var pngPath = $@"D:/GRIFFIN/Assets/Presets/{n}.png";
            m_thumbnails.Add(LoadPNG(pngPath));
        }
    }

    void OnGUI()
    {
        int i = 0;
        foreach(var p in m_presets)
        {
            if (GUILayout.Button(m_thumbnails[i]))
            {
                m_switcher.LoadPreset(int.Parse(p));
            }
            i++;
        }
    }
}
