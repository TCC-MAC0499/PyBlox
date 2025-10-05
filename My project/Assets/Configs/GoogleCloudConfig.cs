using UnityEngine;

[CreateAssetMenu(fileName = "GoogleCloudConfig", menuName = "Scriptable Objects/GoogleCloudConfig")]
public class GoogleCloudConfig : ScriptableObject
{
    [SerializeField]
    public string PythonExecutorUrl;
    public string BorderDetectorUrl;
}
