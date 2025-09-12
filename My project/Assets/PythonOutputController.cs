using System;
using TMPro;
using UnityEngine;

[RequireComponent(typeof(TextMeshProUGUI))]
public class PythonOutputController : MonoBehaviour
{
    private void Awake()
    {
        PythonExecutor.OnPythonExecutionComplete.AddListener(HandlePythonExecutionComplete);
    }

    private void HandlePythonExecutionComplete(string output)
    {
        GetComponent<TextMeshProUGUI>().text = output;
    }
}
