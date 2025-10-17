using System;
using System.Collections.Generic;
using System.IO;
using Cysharp.Threading.Tasks;
using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

[
    RequireComponent(typeof(XROrigin)),
    RequireComponent(typeof(ARTrackedImageManager))
]
public class ImageTracker : MonoBehaviour
{
    public GameObject codePrefab;
    public LevelConfig levelConfig;
    public Button simulateButton;

    private ARTrackedImageManager trackedImageManager;
    private Camera xrOriginCamera;

    private Dictionary<string, PythonCodeBlock> blockToCode = new();
    private PythonExecutor pythonExecutor;
    private BorderDetector borderDetector;

    private void Awake()
    {
        simulateButton.onClick.AddListener(() => OnSimulateClicked().Forget());

        trackedImageManager = GetComponent<ARTrackedImageManager>();
        xrOriginCamera = GetComponent<XROrigin>().Camera;

        var googleCloudConfig = Resources.Load<GoogleCloudConfig>("GoogleCloudConfig");
        pythonExecutor = new PythonExecutor(googleCloudConfig);
        borderDetector = new BorderDetector(googleCloudConfig);
    }
    void OnEnable()
    {
        trackedImageManager.trackablesChanged.AddListener(OnTrackablesChanged);
    }

    void OnDisable()
    {
        trackedImageManager.trackablesChanged.RemoveListener(OnTrackablesChanged);
    }

    private void OnTrackablesChanged(ARTrackablesChangedEventArgs<ARTrackedImage> eventArgs)
    {
        // Create code from prefab and tracked block
        foreach (var trackedImage in eventArgs.added)
        {
            var trackedBlock = trackedImage.referenceImage.name;
            var codeBlockData = levelConfig.codeBlocks.Find(codeBlock => codeBlock.block == trackedBlock);
            if (codeBlockData != null)
            {
                var code = new PythonCodeBlock(codePrefab, codeBlockData.code, trackedImage.transform);
                blockToCode[trackedBlock] = code;
            }
        }

        // Update code game object tracking position
        foreach (var trackedImage in eventArgs.updated)
        {
            var trackedBlock = trackedImage.referenceImage.name;
            var code = blockToCode[trackedBlock];
            code.SetActive(trackedImage.trackingState == TrackingState.Tracking);
        }

    }

    private double GetBlockSideLengthAvg(List<BorderDetector.BlockBorder> blocks)
    {
        Debug.Log($"Detected {blocks.Count} blocks in current frame.");
        if (blocks.Count > 5)
        {
            Debug.Log("More blocks than available in PyBlox have been mistakenly detected, so the average length of block sides will probably be miscalculated.");
        } else if (blocks.Count < 5)
        {
            Debug.Log($"Less blocks than available in PyBlox have been detected. If only {blocks.Count} blocks are in frame, this is expected. If not, the average length of block sides will probably be miscalculated.");
        }

        var sideLengthAvg = 0.0;
        foreach (var block in blocks)
        {
            var sideLengthSum = 0.0;
            for (var idx = 0; idx < block.border.Count; idx++)
            {
                var nextIdx = (idx + 1) % block.border.Count;

                var cornerCoords = block.border[idx];
                var nextCornerCoords = block.border[nextIdx];

                var corner = new Vector2(cornerCoords.x, cornerCoords.y);
                var nextCorner = new Vector2(nextCornerCoords.x, nextCornerCoords.y);

                var sideLength = Math.Abs(Vector2.Distance(corner, nextCorner));
                sideLengthSum += sideLength;
            }
            sideLengthAvg += sideLengthSum / 4.0;
        }
        sideLengthAvg /= blocks.Count;
    
        return sideLengthAvg;
    }

    // Builds Python code from arrangement of blocks by mapping their position in the 3D world into the 2D screen.
    // Lines of code are defined by ordering code by the Y-axis.
    // Code on the same line is defined by ordering by the X-axis blocks that are vertically too close.
    public async UniTask OnSimulateClicked()
    {
        Debug.Log("Simulate clicked!");
    
        // First, capture camera frame and calculate block position from camera
        // in order to ensure maximum accuracy of position values.
        var cameraFrame = await borderDetector.GetCameraFrame(xrOriginCamera);
        var simulationCodeBlocks = new List<PythonCodeBlock>();
        foreach (var (block, code) in blockToCode)
        {
            if (code.GetActive())
            {
                code.SetPositionFromCamera(xrOriginCamera);
                simulationCodeBlocks.Add(code);
            }
        }


        // Second, detect block borders in camera frame and calculate line break tolerance
        // from border sides in order to sort blocks from their arrengement.
        var blockBorders = await borderDetector.Detect(cameraFrame);
        var defaultLineBreakTolerance = GetBlockSideLengthAvg(blockBorders) / 2.0;
        foreach (var code in simulationCodeBlocks)
        {
            var blockBorder = code.GetFittingBorder(blockBorders);
            if (blockBorder.Count == 0)
            {
                Debug.Log($"Unable to find fitting border for code block \"{code.GetText()}\"");
                code.SetLineBreakTolerance(defaultLineBreakTolerance);
            }
            else
            {
                code.SetLineBreakToleranceFromBorder(blockBorder);
            }
        }
        simulationCodeBlocks.Sort();

        // Finally, build Python code from sorted block order.
        var simulationCode = "";
        foreach (var code in simulationCodeBlocks)
        {
            simulationCode += $"{(code.isWholeLineOfCode ? "\n" : "")}{code.GetText()}";
        }
        Debug.Log(simulationCode);

        var pythonExecutionOutput = await pythonExecutor.Execute(simulationCode);
        Debug.Log(pythonExecutionOutput);
    }
}