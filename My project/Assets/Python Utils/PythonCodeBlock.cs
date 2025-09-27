using System;
using TMPro;
using UnityEngine;

[Serializable]
public class PythonCodeBlock : IComparable<PythonCodeBlock>
{
    public GameObject gameObj;
    public Vector2 screenPos;
    public bool isWholeLine;

    public PythonCodeBlock(GameObject codeGameObj, Vector3 worldToScreenPos)
    {
        gameObj = codeGameObj;
        screenPos = (Vector2)worldToScreenPos;
        isWholeLine = true;
    }

    public int CompareTo(PythonCodeBlock other)
    {
        // Should be sorted descending for lines (Y-axis) as we read from top to bottom,
        // so Y-axis comparisons are made from other to this.
        var deltaY = other.screenPos.y - screenPos.y;
        if (Math.Abs(deltaY) > /*lineBreakTolerance*/ 0.2)
        {
            return (int)deltaY;
        }

        // Should be sorted ascending for columns (X-axis) as we read from left to right,
        // so X-axis comparisons are made from this to other.
        var deltaX = screenPos.x - other.screenPos.x;
        isWholeLine = false;
        return (int)deltaX;
    }

    public string GetText()
    {
        var codeText = gameObj.GetComponent<TextMeshPro>().text;
        return codeText.Equals("[TAB]") ? "\t" : codeText;
    }
}