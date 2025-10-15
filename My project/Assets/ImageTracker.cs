using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using TMPro;
using Unity.XR.CoreUtils;
using UnityEngine;
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

    // Builds Python code from arrangement of blocks by mapping their position in the 3D world into the 2D screen.
    // Lines of code are defined by ordering code by the Y-axis.
    // Code on the same line is defined by ordering by the X-axis blocks that are vertically too close.
    public async UniTask OnSimulateClicked()
    {
        Debug.Log("Simulate clicked!");

        var borderDetectionOutput = await borderDetector.Detect();

        var simulationCodeBlocks = new List<PythonCodeBlock>();
        foreach (var (block, code) in blockToCode)
        {
            if (code.GetActive())
            {
                code.SetPositionFromCamera(xrOriginCamera);
                // TODO: Set line break tolerance for Python Code Block with Border Detector output.
                simulationCodeBlocks.Add(code);
            }
        }

        simulationCodeBlocks.Sort();

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