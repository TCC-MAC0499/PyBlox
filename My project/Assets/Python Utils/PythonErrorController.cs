using TMPro;
using UnityEngine;
using UnityEngine.Serialization;

public class PythonErrorControler : MonoBehaviour
{
    [SerializeField] private GameObject errorCanvas;
    [SerializeField] private TextMeshProUGUI errorText;
    
    private void Awake()
    {
        errorCanvas.SetActive(false);
        PythonExecutor.OnPythonExecutionCompletedWithFailure.AddListener(HandlePythonExecutionCompletedWithFailure);
        PythonExecutor.OnClearPythonExecution.AddListener(HandleClearPythonError);
    }

    private void HandlePythonExecutionCompletedWithFailure(string output)
    {
        errorCanvas.SetActive(true);
        errorText.text = output;
    }

    private void HandleClearPythonError()
    {
        errorCanvas.SetActive(false);
    }
}