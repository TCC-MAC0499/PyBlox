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
        SetInterfaceElementsActive(false);
        PythonExecutor.OnPythonExecutionInitiated.AddListener(HandlePythonExecutionInitiated);
        clearButton.onClick.AddListener(PythonExecutor.OnClearPythonExecution.Invoke);
        PythonExecutor.OnClearPythonExecution.AddListener(HandleClearPythonCode);
    }

    private void SetInterfaceElementsActive(bool active)
    {
        pythonCodeCanvas.SetActive(active);
        clearButton.gameObject.SetActive(active);
    }

    private void HandlePythonExecutionInitiated(string output)
    {
        SetInterfaceElementsActive(true);
        pythonCodeText.text = output;
    }

    private void HandleClearPythonCode()
    {
        SetInterfaceElementsActive(false);
    }
}
