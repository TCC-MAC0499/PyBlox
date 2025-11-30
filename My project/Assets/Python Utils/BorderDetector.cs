using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.Rendering;

public class BorderDetector
{
    private readonly GoogleCloudConfig _config;

    public class BlockDelimiter
    {
        public List<Vector2> border;
        public Vector2 topLeftCorner;
        public Vector2 bottomRightCorner;
    }

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

    private List<BlockDelimiter> ExtractDelimitersFromBorders(List<BlockBorder> blockBorders)
    {
        var blockDelimiters = new List<BlockDelimiter>();
        foreach (var block in blockBorders)
        {
            Debug.Log("Start of border delimeters");
            var convertedBorder = new List<Vector2>();
            foreach (var corner in block.border)
            {
                var convertedCorner = corner.ToUnityCoordinateSystem();
                convertedBorder.Add(convertedCorner);
                Debug.Log($"Converted corner: {convertedCorner.x}, {convertedCorner.y})");
            }
            var borderByAscendingY = convertedBorder.OrderBy(corner => corner.y).ToList();

            var topLeftCorner = borderByAscendingY[3].x < borderByAscendingY[2].x ? borderByAscendingY[3] : borderByAscendingY[2];
            var bottomRightCorner = borderByAscendingY[0].x > borderByAscendingY[1].x ? borderByAscendingY[0] : borderByAscendingY[1];
            Debug.Log($"Top left corner: {topLeftCorner.x}, {topLeftCorner.y})");
            Debug.Log($"Bottom right corner: {bottomRightCorner.x}, {bottomRightCorner.y})");

            blockDelimiters.Add(new BlockDelimiter
            {
                border = convertedBorder,
                topLeftCorner = topLeftCorner,
                bottomRightCorner = bottomRightCorner
            });
        }
        return blockDelimiters;
    }

    private Dictionary<PythonCodeBlock, List<Vector2>> MatchBlocksToBorders(List<BlockBorder> blockBorders, List<PythonCodeBlock> codeBlocks)
    {
        var codeToBorder = new Dictionary<PythonCodeBlock, List<Vector2>>();
        var blockDelimiters = ExtractDelimitersFromBorders(blockBorders);
        foreach (var codeBlock in codeBlocks)
        {
            var blockCenter = codeBlock.GetPositionFromCamera();
            BlockDelimiter matchingDelimiter = null;
            foreach (var blockDelimiter in blockDelimiters)
            {
                var topLeftFitsBlock = blockDelimiter.topLeftCorner.x <= blockCenter.x && blockDelimiter.topLeftCorner.y >= blockCenter.y;
                var bottomRightFitsBlock = blockDelimiter.bottomRightCorner.x >= blockCenter.x && blockDelimiter.bottomRightCorner.y <= blockCenter.y;
                if (topLeftFitsBlock && bottomRightFitsBlock)
                {
                    Debug.Log($"Matched top left corner: {blockDelimiter.topLeftCorner.x}, {blockDelimiter.topLeftCorner.y})");
                    Debug.Log($"Matched bottom right corner: {blockDelimiter.bottomRightCorner.x}, {blockDelimiter.bottomRightCorner.y})");
                    codeToBorder[codeBlock] = blockDelimiter.border;
                    matchingDelimiter = blockDelimiter;
                    break;
                }
            }
            blockDelimiters.Remove(matchingDelimiter);
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
