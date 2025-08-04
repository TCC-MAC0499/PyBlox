using System.Collections.Generic;
using TMPro;

using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

public class ImageTracker : MonoBehaviour
{
    public GameObject codePrefab;
    private ARTrackedImageManager trackedImageManager;
    private Dictionary<string, GameObject> blockToCodeGameObj = new();

    // TO-DO: Move readonly dictionary to JSON file.
    readonly Dictionary<string, string> blockToCodeText = new(){
            { "block-1", "print(\"Hello, world!\")" },
            { "block-2", "return true" },
        };

    void Awake() => trackedImageManager = GetComponent<ARTrackedImageManager>();
    void OnEnable() => trackedImageManager.trackablesChanged.AddListener(OnTrackablesChanged);
    void OnDisable() => trackedImageManager.trackablesChanged.RemoveListener(OnTrackablesChanged);

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
}
