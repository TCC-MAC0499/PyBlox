using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "LevelConfig", menuName = "Scriptable Objects/LevelConfig")]
public class LevelConfig : ScriptableObject
{
    [Serializable]
    public class CodeBlock
    {
        public string code;
        public string block;
    }
    
    [SerializeField]
    public List<CodeBlock> codeBlocks;
}
