using UnityEngine;

[CreateAssetMenu(fileName = "PythonExecutorConfig", menuName = "Scriptable Objects/PythonExecutorConfig")]
public class PythonExecutorConfig : ScriptableObject
{
    [SerializeField]
    public string GoogleFunctionUrl;
}
