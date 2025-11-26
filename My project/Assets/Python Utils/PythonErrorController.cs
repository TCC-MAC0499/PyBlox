using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class PythonErrorControler : MonoBehaviour
{
    [SerializeField] private GameObject errorCanvas;
    [SerializeField] private Button clearButton;
    [SerializeField] private TextMeshProUGUI errorText;
    
    private void Awake()
    {
        SetInterfaceElementsActive(false);
        ImageTracker.OnPythonCodeAssemblyCompletedWithFailure.AddListener(HandlePythonFailure);
        PythonExecutor.OnPythonExecutionCompletedWithFailure.AddListener(HandlePythonFailure);
        clearButton.onClick.AddListener(HandleClearPythonError);
        PythonExecutor.OnClearPythonExecution.AddListener(HandleClearPythonError);
    }

    private void SetInterfaceElementsActive(bool active)
    {
        errorCanvas.SetActive(active);
        clearButton.gameObject.SetActive(active);
    }

    private void HandlePythonFailure(string output)
    {
        SetInterfaceElementsActive(true);
        errorText.text = output;
    }

    private void HandleClearPythonError()
    {
        SetInterfaceElementsActive(false);
    }
}