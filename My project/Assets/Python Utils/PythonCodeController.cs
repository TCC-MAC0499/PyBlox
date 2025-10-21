using TMPro;
using UnityEngine;

[RequireComponent(typeof(TextMeshProUGUI))]
public class PythonCodeController : MonoBehaviour
{
    private void Awake()
    {
        PythonExecutor.OnPythonExecutionInitiated.AddListener(HandlePythonExecutionInitiated);
    }

    private void HandlePythonExecutionInitiated(string output)
    {
        GetComponent<TextMeshProUGUI>().text = output;
    }
}
