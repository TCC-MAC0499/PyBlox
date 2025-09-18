using System;
using System.Text;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Networking;

public class PythonExecutor
{
    public static readonly UnityEvent<string> OnPythonExecutionComplete = new();
    
    private readonly PythonExecutorConfig _config;

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
    
    public PythonExecutor(PythonExecutorConfig config)
    {
        this._config = config;
    }

    public async UniTask<string> Execute(string code)
    {
        return await SendWebRequestAsync(code);
    }
    
    private async UniTask<string> SendWebRequestAsync(string code)
    {
        var requestData = new PythonCodeRequest();
        requestData.code = code;

        var jsonBody = JsonUtility.ToJson(requestData);
        var bodyRaw = Encoding.UTF8.GetBytes(jsonBody);

        using (var request = new UnityWebRequest(_config.GoogleFunctionUrl, "POST"))
        {
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");

            try
            {
                await request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.ConnectionError || request.result == UnityWebRequest.Result.ProtocolError)
                {
                    Debug.LogError("Request error: " + request.error);
                    return request.error;
                }
                else
                {
                    Debug.Log("Server's response:\n" + request.downloadHandler.text);

                    var response = JsonUtility.FromJson<PythonCodeResponse>(request.downloadHandler.text);
                    OnPythonExecutionComplete.Invoke(response.output);
                    
                    return response.output;
                }
            }
            catch (Exception ex)
            {
                Debug.LogError("Request threw an exception: " + ex.Message);
                OnPythonExecutionComplete.Invoke(ex.Message);
                return ex.Message;
            }
        }
    }
}
