using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PythonCodeController : MonoBehaviour
{
    [SerializeField] private GameObject pythonCodeCanvas;
    [SerializeField] private Button clearButton;
    [SerializeField] private TextMeshProUGUI pythonCodeText;
    
    private void Awake()
    {
        pythonCodeCanvas.SetActive(false);
        PythonExecutor.OnPythonExecutionInitiated.AddListener(HandlePythonExecutionInitiated);
        clearButton.onClick.AddListener(PythonExecutor.OnClearPythonExecution.Invoke);
        PythonExecutor.OnClearPythonExecution.AddListener(HandleClearPythonCode);
    }

    private void HandlePythonExecutionInitiated(string output)
    {
        pythonCodeCanvas.SetActive(true);
        pythonCodeText.text = output;
    }

    private void HandleClearPythonCode()
    {
        pythonCodeCanvas.SetActive(false);
    }
}
