using System;
using System.Text;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Networking;

public class PythonExecutor
{
    public static readonly UnityEvent<string> OnPythonExecutionComplete = new();

    private readonly GoogleCloudConfig _config;

    [Serializable]
    private class PythonCodeRequest
    {
        public string code;
    }

    [Serializable]
    private class PythonCodeResponse
    {
        public bool success;
        public string output;
        public string error;
    }

    public PythonExecutor(GoogleCloudConfig config)
    {
        this._config = config;
    }

    public async UniTask<string> Execute(string code)
    {
        return await SendWebRequestAsync(code);
    }

    private async UniTask<string> SendWebRequestAsync(string code)
    {
        var requestData = new PythonCodeRequest { code = code };
        var jsonBody = JsonUtility.ToJson(requestData);
        var bodyRaw = Encoding.UTF8.GetBytes(jsonBody);

        using (var request = new UnityWebRequest(_config.PythonExecutorUrl, "POST"))
        {
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");

            await request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                string formattedError = $"<color=red>Network error</color>:\n{request.error}";
                OnPythonExecutionComplete.Invoke(formattedError);
                return formattedError;
            }

            var response = JsonUtility.FromJson<PythonCodeResponse>(request.downloadHandler.text);

            if (response.success)
            {
                string formattedOutput = string.IsNullOrEmpty(response.output) ? "Python execution complete." : response.output;
                OnPythonExecutionComplete.Invoke(formattedOutput);
                return formattedOutput;
            }
            else
            {
                string formattedError = FormatPythonError(response.error);
                OnPythonExecutionComplete.Invoke(formattedError);
                return formattedError;
            }
        }
    }

    private string FormatPythonError(string error)
    {
        if (string.IsNullOrEmpty(error) || !error.Contains("Traceback (most recent call last):"))
        {
            return $"<color=red>Unknown Python error</color>:\n{error}";
        }

        string[] lines = error.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        string errorReason = lines[lines.Length - 1].Trim();
        return $"<color=red>Execution failed</color>:\n{errorReason}";
    }
}
