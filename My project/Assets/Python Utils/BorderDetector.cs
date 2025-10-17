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

    [Serializable]
    private class BorderDetectorResponse
    {
        public bool success;
        public string error;
        public List<BlockBorder> block_borders;
    }

    public BorderDetector(GoogleCloudConfig config)
    {
        this._config = config;
    }

    public async UniTask<List<BlockBorder>> Detect(Camera camera)
    {
        var frameBytes = await GetCameraFrame(camera);
        if (frameBytes.Length > 0)
        {
            return await SendWebRequestAsync(frameBytes);
        }
        else
        {
            throw new Exception("Error capturing camera frame.");
        }
    }

    private async UniTask<List<BlockBorder>> SendWebRequestAsync(byte[] imageBytes)
    {
        using (var request = new UnityWebRequest(_config.BorderDetectorUrl, "POST"))
        {
            request.uploadHandler = new UploadHandlerRaw(imageBytes);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "image/jpeg");

            await request.SendWebRequest();
            if (request.result != UnityWebRequest.Result.Success)
            {
                throw new Exception(request.error);
            }

            var response = JsonUtility.FromJson<BorderDetectorResponse>(request.downloadHandler.text);
            Debug.Log(request.downloadHandler.text);
            if (response.success)
            {
                return response.block_borders;
            }
            else
            {
                throw new Exception(response.error);
            }
        }
    }

    private async UniTask<byte[]> GetCameraFrame(Camera camera)
    {
        var frameRender = RenderTexture.GetTemporary(Screen.width, Screen.height, 24, RenderTextureFormat.ARGB32);
        var originalCameraTarget = camera.targetTexture;
        camera.targetTexture = frameRender;
        camera.Render();

        var gpuRequest = await AsyncGPUReadback.Request(frameRender, 0, TextureFormat.ARGB32);
        camera.targetTexture = originalCameraTarget;
        RenderTexture.ReleaseTemporary(frameRender);

        if (gpuRequest.hasError)
        {
            return Array.Empty<byte>();
        }
        else
        {
            var frameTexture = new Texture2D(Screen.width, Screen.height, TextureFormat.ARGB32, false);
            frameTexture.LoadRawTextureData(gpuRequest.GetData<uint>());
            frameTexture.Apply();

            return frameTexture.EncodeToJPG();
        }
    }
}
