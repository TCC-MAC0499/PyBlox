using System;
using TMPro;
using UnityEngine;
using UnityEngine.XR.ARFoundation;

[Serializable]
public class PythonCodeBlock : IComparable<PythonCodeBlock>
{
    private GameObject codeGameObject;
    private Vector2 blockScreenPos = Vector2.zero;

    public bool isWholeLineOfCode = true;
    public double codeLineBreakTolerance = 0.2;

    public PythonCodeBlock(GameObject codePrefab, string codeText, Transform codePosition)
    {
        codeGameObject = UnityEngine.Object.Instantiate(codePrefab, codePosition);
        codeGameObject.GetComponent<TextMeshPro>().text = codeText;
    }


    public int CompareTo(PythonCodeBlock other)
    {
        // Should be sorted descending for lines (Y-axis) as we read from top to bottom,
        // so Y-axis comparisons are made from other to this.
        var deltaY = other.blockScreenPos.y - blockScreenPos.y;
        if (Math.Abs(deltaY) > codeLineBreakTolerance)
        {
            return (int)deltaY;
        }

        // Should be sorted ascending for columns (X-axis) as we read from left to right,
        // so X-axis comparisons are made from this to other.
        var deltaX = blockScreenPos.x - other.blockScreenPos.x;
        isWholeLineOfCode = false;
        return (int)deltaX;
    }

    public bool GetActive()
    {
        return codeGameObject.activeSelf;
    }

    public void SetActive(bool newState)
    {
        codeGameObject.SetActive(newState);
    }
    public string GetText()
    {
        var codeText = codeGameObject.GetComponent<TextMeshPro>().text;
        return codeText.Equals("[TAB]") ? "\t" : codeText;
    }

    public void SetPositionFromCamera(Camera camera)
    {
        camera.WorldToScreenPoint(codeGameObject.transform.position);
    }
}