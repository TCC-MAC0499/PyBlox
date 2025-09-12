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
    public TextAsset levelFile;
    public Button simulateButton;

    private ARTrackedImageManager trackedImageManager;
    private Camera xrOriginCamera;

    private Dictionary<string, string> blockToCodeText = new();
    private Dictionary<string, GameObject> blockToCodeGameObj = new();
    
    private PythonExecutor pythonExecutor;

    private void Awake()
    {
        simulateButton.onClick.AddListener(() => OnSimulateClicked().Forget());
        
        trackedImageManager = GetComponent<ARTrackedImageManager>();
        xrOriginCamera = GetComponent<XROrigin>().Camera;
        MapBlockToCodeTextFromJson(levelFile.text);
        pythonExecutor = new PythonExecutor(Resources.Load<PythonExecutorConfig>("PythonExecutorConfig"));
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


    [Serializable]
    private class Level
    {
        [Serializable]
        public class CodeBlock
        {
            public string code;
            public string block;
        }

        public List<CodeBlock> codeBlocks;

    }
    private void MapBlockToCodeTextFromJson(string json)
    {
        var level = JsonUtility.FromJson<Level>(json);
        foreach (var codeBlock in level.codeBlocks)
        {
            blockToCodeText[codeBlock.block] = codeBlock.code;
        }
    }

    // Builds Python code from arrangement of blocks by mapping their position in the 3D world into the 2D screen.
    // Lines of code are defined by ordering code by the Y-axis.
    // Code on the same line is defined by ordering by the X-axis blocks that are vertically too close.
    public async UniTask OnSimulateClicked()
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
        var output = await pythonExecutor.Execute(pythonCode);
        print(output);
    }
}