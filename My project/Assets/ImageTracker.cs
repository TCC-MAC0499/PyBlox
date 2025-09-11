using System;
using System.Collections.Generic;
using TMPro;
using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

[
    RequireComponent(typeof(XROrigin)),
    RequireComponent(typeof(ARTrackedImageManager))
]
public class ImageTracker : MonoBehaviour
{
    public GameObject codePrefab;

    private ARTrackedImageManager trackedImageManager;
    private Camera xrOriginCamera;

    // TO-DO: Move readonly dictionary to JSON file.
    private Dictionary<string, string> blockToCodeText = new(){
            { "block-1", "print(\"Hello, world!\")" },
            { "block-2", "return true" },
        };
    private Dictionary<string, GameObject> blockToCodeGameObj = new();

    private void Awake()
    {
        trackedImageManager = GetComponent<ARTrackedImageManager>();
        xrOriginCamera = GetComponent<XROrigin>().Camera;
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
            var code = Instantiate(codePrefab, trackedImage.transform);
            var codeText = code.GetComponent<TextMeshPro>();

            blockToCodeGameObj[trackedBlock] = code;
            codeText.text = blockToCodeText[trackedBlock];
        }

        // Update code game object tracking position
        foreach (var trackedImage in eventArgs.updated)
        {
            var trackedBlock = trackedImage.referenceImage.name;
            var code = blockToCodeGameObj[trackedBlock];
            code.SetActive(trackedImage.trackingState == TrackingState.Tracking);
        }

    }

    // Builds Python code from arrangement of blocks by mapping their position in the 3D world into the 2D screen.
    // Lines of code are defined by ordering code by the Y-axis.
    // Code on the same line is defined by ordering by the X-axis blocks that are vertically too close.
    public void OnSimulateClicked()
    {
        print("Simulate!");
        var pythonCodeBlocks = new List<PythonCodeBlock>();
        foreach (var (block, code) in blockToCodeGameObj)
        {
            var worldToScreenPos = xrOriginCamera.WorldToScreenPoint(code.transform.position);
            pythonCodeBlocks.Add(new PythonCodeBlock(code, worldToScreenPos));
        }
        pythonCodeBlocks.Sort((codeA, codeB) => codeA.CompareTo(codeB));

        var pythonCode = "";
        foreach (var code in pythonCodeBlocks)
        {
            pythonCode += $"{(code.isWholeLine ? "\n" : "")}{code.GetText()}";
        }
        print(pythonCode);
    }

}