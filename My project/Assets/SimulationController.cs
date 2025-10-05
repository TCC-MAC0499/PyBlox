using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class SimulationController : MonoBehaviour
{
    [SerializeField] private GameObject simulationCanvas;
    [SerializeField] private GameObject pointPrefab;
    [SerializeField] private GameObject backgroundImage;
    [SerializeField] private float delayPerPoint = 0.08f;
    
    private RectTransform _backgroundRect;
    private List<float> _parsedValues = new List<float>();
    
    private const float PaddingPercentage = 0.1f; 
    
    void Awake()
    {
        simulationCanvas.SetActive(false);
        _backgroundRect = backgroundImage.GetComponent<RectTransform>();
        PythonExecutor.OnPythonExecutionComplete.AddListener(HandlePythonExecutionComplete);
    }

    private void HandlePythonExecutionComplete(string output)
    {
        _parsedValues.Clear();
        ParseOutput(output);
        StopAllCoroutines();
        StartCoroutine(CreateSimulationPoints());
    }

    private void ParseOutput(string output)
    {
        string[] lines = output.Split(
            new[] { '\r', '\n' }, 
            StringSplitOptions.RemoveEmptyEntries
        );
        
        foreach (string line in lines)
        {
            if (float.TryParse(line.Trim(), out float result))
                _parsedValues.Add(result);
            else
                Debug.LogWarning($"Could not parse line as a float: '{line.Trim()}'");
        }
    }
    
    // - Define plotting boundaries for the points (so they don't touch the edge of the background image)
    // - Normalize and map the point's coordinates in X and Y
    private IEnumerator CreateSimulationPoints()
    {
        simulationCanvas.SetActive(true);
        
        float bgWidth = _backgroundRect.rect.width;
        float bgHeight = _backgroundRect.rect.height;

        float paddedWidth = bgWidth * (1f - 2 * PaddingPercentage);
        float paddedHeight = bgHeight * (1f - 2 * PaddingPercentage);
        float xOffset = bgWidth * PaddingPercentage;
        float yOffset = bgHeight * PaddingPercentage;
        
        int numPoints = _parsedValues.Count;
        
        float dataXMax = numPoints > 0 ? numPoints - 1 : 0;
        
        float dataYMin = _parsedValues.Min();
        float dataYMax = _parsedValues.Max();
        float dataYRange = dataYMax - dataYMin;

        // If all values are the same use a default range to place all points in the middle
        if (dataYRange == 0)
            dataYRange = 1f;
        
        foreach (Transform child in backgroundImage.transform)
        {
            if (child.gameObject != backgroundImage)
                Destroy(child.gameObject);
        }

        // Calculate and instantiate points
        for (int i = 0; i < numPoints; i++)
        {
            float dataY = _parsedValues[i];

            // x-axis mapping
            float normalizedX = (float)i / dataXMax; 
            float posX = (normalizedX * paddedWidth) + xOffset - (bgWidth / 2);

            // y-axis mapping
            float normalizedY;
            if (dataYRange == 1f && dataYMax == dataYMin)
                normalizedY = 0.5f;
            else
                normalizedY = (dataY - dataYMin) / dataYRange;
            float posY = (normalizedY * paddedHeight) + yOffset - (bgHeight / 2);
            
            GameObject newPoint = Instantiate(pointPrefab, backgroundImage.transform);
            newPoint.GetComponent<RectTransform>().localPosition = new Vector3(posX, posY, 0);
            
            yield return new WaitForSeconds(delayPerPoint);
        }
    }
}
