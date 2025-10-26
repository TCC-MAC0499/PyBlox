using TMPro;
using UnityEngine;

[RequireComponent(typeof(TextMeshProUGUI))]
public class PythonCodeController : MonoBehaviour
{
    [SerializeField] private GameObject pythonCodeCanvas;
    
    private void Awake()
    {
        pythonCodeCanvas.SetActive(false);
        PythonExecutor.OnPythonExecutionInitiated.AddListener(HandlePythonExecutionInitiated);
    }

    private void HandlePythonExecutionInitiated(string output)
    {
        pythonCodeCanvas.SetActive(true);
        GetComponent<TextMeshProUGUI>().text = output;
    }
}
