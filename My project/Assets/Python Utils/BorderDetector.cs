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

            // Unity screen is oriented so that bottom-left is (0,0) and top-right is (width-1, height-1) and
            // OpenCV image is oriented so that top-left is (0,0) bottom-right is (width-1, height-1),
            // so convertion is required to ensure orientation match.
            public Vector2 ToUnityCoordinateSystem()
            {
                return new Vector2(x, Screen.height - y);
            }
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

    public async UniTask<Dictionary<PythonCodeBlock, List<Vector2>>> Detect(byte[] cameraFrameBytes, List<PythonCodeBlock> simulationCodeBlocks)
    {
        if (cameraFrameBytes.Length == 0)
        {
            throw new Exception("Cannot detect borders without a camera frame image.");
        }
        return await SendWebRequestAsync(cameraFrameBytes, simulationCodeBlocks);
    }

    public async UniTask<byte[]> GetCameraFrame(Camera camera)
    {
        var frameRender = RenderTexture.GetTemporary(Screen.width, Screen.height, 24, RenderTextureFormat.ARGB32);
        var originalCameraTarget = camera.targetTexture;
        camera.targetTexture = frameRender;
        camera.Render();

        // GPU buffers assumebottom-left image origin
        var gpuRequest = await AsyncGPUReadback.Request(frameRender, 0, TextureFormat.ARGB32);
        camera.targetTexture = originalCameraTarget;
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

            return frameTexture.EncodeToJPG();
        }
    }

    private Dictionary<PythonCodeBlock, List<Vector2>> MatchBlocksToBorders(List<BlockBorder> blockBorders, List<PythonCodeBlock> codeBlocks)
    {
        var codeToBorder = new Dictionary<PythonCodeBlock, List<Vector2>>();
        foreach (var codeBlock in codeBlocks)
        {
            var blockCenter = codeBlock.GetPositionFromCamera();
            BlockBorder matchingBorder = null;
            for (var idx = 0; idx < blockBorders.Count; idx++)
            {
                var topLeftCorner = blockBorders[idx].border[0].ToUnityCoordinateSystem();
                var bottomRightCorner = blockBorders[idx].border[2].ToUnityCoordinateSystem();

                var topLeftFitsBlock = topLeftCorner.x <= blockCenter.x && topLeftCorner.y >= blockCenter.y;
                var bottomRightFitsBlock = bottomRightCorner.x >= blockCenter.x && bottomRightCorner.y <= blockCenter.y;
                if (topLeftFitsBlock && bottomRightFitsBlock)
                {
                    var convertedBorder = new List<Vector2>();
                    foreach (var corner in blockBorders[idx].border)
                    {
                        convertedBorder.Add(corner.ToUnityCoordinateSystem());
                    }

                    codeToBorder[codeBlock] = convertedBorder;
                    matchingBorder = blockBorders[idx];
                    break;
                }
            }
            blockBorders.Remove(matchingBorder);
        }
        return codeToBorder;
    }

    private async UniTask<Dictionary<PythonCodeBlock, List<Vector2>>> SendWebRequestAsync(byte[] imageBytes, List<PythonCodeBlock> codeBlocks)
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

            Debug.Log(request.downloadHandler.text);
            var response = JsonUtility.FromJson<BorderDetectorResponse>(request.downloadHandler.text);
            if (response.success)
            {
                return MatchBlocksToBorders(response.block_borders, codeBlocks);
            }
            throw new Exception(response.error);
        }
    }
}
