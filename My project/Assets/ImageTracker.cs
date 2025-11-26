using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.Events;
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
    public static readonly UnityEvent<string> OnPythonCodeAssemblyCompletedWithFailure = new();

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
        // Create code from recently detected block.
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

        // Update code with recent changes to detected block's visibility.
        foreach (var trackedImage in eventArgs.updated)
        {
            var trackedBlock = trackedImage.referenceImage.name;
            var code = blockToCode[trackedBlock];
            code.SetActive(trackedImage.trackingState == TrackingState.Tracking);
        }

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
        // from border sides in order to sort blocks based on their arrengement.
        var codeToBorder = await borderDetector.Detect(cameraFrame, simulationCodeBlocks);
        double maxLineBreakTol = 0.0;
        foreach (var code in simulationCodeBlocks)
        {
            if (codeToBorder.TryGetValue(code, out List<Vector2> border))
            {
                var currentLineBreakTol = code.SetLineBreakToleranceFromBorder(border);
                maxLineBreakTol = currentLineBreakTol > maxLineBreakTol ? currentLineBreakTol : maxLineBreakTol;
            }
        }
        if (maxLineBreakTol == 0.0)
        {
            // TODO: Add instructions for better detection in error message.
            OnPythonCodeAssemblyCompletedWithFailure.Invoke($"<color=red>Execution failed</color>:\nUnable to detect block borders.");
            throw new Exception("Cannot build Python code from arrangement of blocks because no block borders were detected.");
        }
        foreach (var code in simulationCodeBlocks)
        {
            if (!codeToBorder.TryGetValue(code, out List<Vector2> border))
            {
                Debug.Log($"Unable to find fitting border for code block \"{code.GetText()}\". Using other code block's fitting borders to define this code block's line break tolerance.");
                code.SetLineBreakTolerance(maxLineBreakTol);
            }
        }
        simulationCodeBlocks.Sort();

        // Finally, build Python code based on the sorted blocks.
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