using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

[Serializable]
public class PythonCodeBlock : IComparable<PythonCodeBlock>
{
    private GameObject codeGameObject;
    private Vector2 blockScreenPos = Vector2.zero;

    public bool isWholeLineOfCode = true;
    public double codeLineBreakTolerance = 1.0;

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
        other.isWholeLineOfCode = false;
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
        blockScreenPos = camera.WorldToScreenPoint(codeGameObject.transform.position);
    }

    public void SetLineBreakTolerance(double newLineBreakTolerance)
    {
        codeLineBreakTolerance = newLineBreakTolerance;
    }

    public double SetLineBreakToleranceFromBorder(List<BorderDetector.BlockBorder.Coordinates> border)
    {
        var sideLengthSum = 0.0;
        for (var idx = 0; idx < border.Count; idx++)
        {
            var nextIdx = (idx + 1) % border.Count;

            var corner = new Vector2(border[idx].x, border[idx].y);
            var nextCorner = new Vector2(border[nextIdx].x, border[nextIdx].y);

            var sideLength = Math.Abs(Vector2.Distance(corner, nextCorner));
            sideLengthSum += sideLength;
        }
        // Line break tolerance is half the size length average.
        codeLineBreakTolerance = sideLengthSum / 8.0;
        return codeLineBreakTolerance;
    }

    public List<BorderDetector.BlockBorder.Coordinates> GetFittingBorder(List<BorderDetector.BlockBorder> blocks)
    {
        Debug.Log($"blockScreenPos {blockScreenPos}");
        foreach (var block in blocks)
        {
            // Unity screen is oriented so that bottom-left is (0,0) and top-right is (width-1, height-1) and
            // OpenCV image is oriented so that top-left is (0,0) bottom-right is (width-1, height-1),
            // so convertion is made to ensure orientation match.
            var topLeftCorner = new Vector2(block.border[1].x, Screen.height - block.border[1].y);
            var bottomRightCorner = new Vector2(block.border[3].x, Screen.height - block.border[3].y);
            Debug.Log($"topLeftCorner {topLeftCorner.x}, {topLeftCorner.y}");
            Debug.Log($"bottomRightCorner {bottomRightCorner.x}, {bottomRightCorner.y}");

            var topLeftFitsBlock = topLeftCorner.x <= blockScreenPos.x && topLeftCorner.y >= blockScreenPos.y;
            var bottomRightFitsBlock = bottomRightCorner.x >= blockScreenPos.x && bottomRightCorner.y <=  Screen.height - blockScreenPos.y;
            if (topLeftFitsBlock && bottomRightFitsBlock)
            {
                return block.border;
            }
        }
        return new List<BorderDetector.BlockBorder.Coordinates>();
    }
}