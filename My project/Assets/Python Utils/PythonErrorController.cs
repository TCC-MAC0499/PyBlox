using TMPro;
using UnityEngine;

[RequireComponent(typeof(TextMeshProUGUI))]
public class PythonErrorControler : MonoBehaviour
{
    [SerializeField] private GameObject simulationCanvas;
    
    private void Awake()
    {
        simulationCanvas.SetActive(false);
        PythonExecutor.OnPythonExecutionCompletedWithFailure.AddListener(HandlePythonExecutionCompletedWithFailure);
    }

    private void HandlePythonExecutionCompletedWithFailure(string output)
    {
        simulationCanvas.SetActive(true);
        GetComponent<TextMeshProUGUI>().text = output;
    }
}