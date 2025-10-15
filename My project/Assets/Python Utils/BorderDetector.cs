using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.Rendering;

public class BorderDetector
{
    private readonly GoogleCloudConfig _config;

    [Serializable]
    private class BorderDetectorResponse
    {
        [Serializable]
        public class BlockBorder
        {
            [Serializable]
            public class Coordinates
            {
                public int x;
                public int y;
            }

            public List<Coordinates> border;
        }

        public bool success;
        public string error;
        public List<BlockBorder> block_borders;
    }

    public BorderDetector(GoogleCloudConfig config)
    {
        this._config = config;
    }

    public async UniTask<string> Detect()
    {
        var frameBytes = await GetCameraFrame();
        if (frameBytes.Length > 0)
        {
            return await SendWebRequestAsync(frameBytes);
        }
        else
        {
            return "Error capturing camera frame.";
        }
    }

    private async UniTask<string> SendWebRequestAsync(byte[] imageBytes)
    {
        using (var request = new UnityWebRequest(_config.BorderDetectorUrl, "POST"))
        {
            request.uploadHandler = new UploadHandlerRaw(imageBytes);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "image/jpeg");

            await request.SendWebRequest();
            if (request.result != UnityWebRequest.Result.Success)
            {
                return request.error;
            }

            var response = JsonUtility.FromJson<BorderDetectorResponse>(request.downloadHandler.text);
            Debug.Log(request.downloadHandler.text);
            if (response.success)
            {
                // TODO: Interpret Border Detector output.
                // foreach (var block_border in response.block_borders)
                // {
                //     Debug.Log("border");
                //     foreach (var coordinates in block_border.border)
                //     {
                //         Debug.Log( $"{coordinates.x},{coordinates.y}");
                //     }
                // }
                return response.error;
            }
            else
            {
                return response.error;
            }
        }
    }

    private async UniTask<byte[]> GetCameraFrame()
    {
        var frameRender = RenderTexture.GetTemporary(Screen.width, Screen.height, 0, RenderTextureFormat.ARGB32);
        ScreenCapture.CaptureScreenshotIntoRenderTexture(frameRender);

        // GPU buffers assumebottom-left image origin
        var gpuRequest = await AsyncGPUReadback.Request(frameRender, 0, TextureFormat.ARGB32);
        RenderTexture.ReleaseTemporary(frameRender);

        if (gpuRequest.hasError)
        {
            return Array.Empty<byte>();
        }
        else
        {
            // Texture2D and JPG encoder assume top-left image origin
            var frameTexture = new Texture2D(Screen.width, Screen.height, TextureFormat.ARGB32, false);
            frameTexture.LoadRawTextureData(gpuRequest.GetData<uint>());
            frameTexture.Apply();

            // Consequently, frame is captured upside-down, which will be fixed in the Google Cloud function
            return frameTexture.EncodeToJPG();
        }
    }
}
