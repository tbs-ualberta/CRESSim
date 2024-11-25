using System.Collections;
using System.Collections.Generic;
using PhysX5ForUnity;
using UnityEngine;

public class SaveFrame : MonoBehaviour
{
    private void Update()
    {
        if (Input.GetKeyDown("x"))
        {
            SaveCameraFrame();
        }
    }
    private void SaveCameraFrame()
    {
        int width = m_cameraToCapture.pixelWidth;
        int height = m_cameraToCapture.pixelHeight;
        // Create a RenderTexture with desired dimensions
        RenderTexture renderTexture = new RenderTexture(width, height, 24);
        m_cameraToCapture.targetTexture = renderTexture;
        m_cameraToCapture.Render();

        // Set up a new Texture2D with the same dimensions
        RenderTexture.active = renderTexture;
        Texture2D screenshot = new Texture2D(width, height, TextureFormat.RGB24, false);
        screenshot.ReadPixels(new Rect(0, 0, width, height), 0, 0);
        screenshot.Apply();

        // Reset target texture and RenderTexture
        m_cameraToCapture.targetTexture = null;
        RenderTexture.active = null;
        Destroy(renderTexture);

        // Encode texture to PNG format
        byte[] bytes = screenshot.EncodeToPNG();
        
        // Define the file path
        string filePath = System.IO.Path.Combine(Application.dataPath, "CameraCapture_" + System.DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss") + ".png");

        // Write the file to disk
        System.IO.File.WriteAllBytes(filePath, bytes);
        Debug.Log("Saved camera frame to: " + filePath);

        // Clean up
        Destroy(screenshot);
    }


    [SerializeField]
    private Camera m_cameraToCapture;

    private Material m_processDepthMaterial;
}
