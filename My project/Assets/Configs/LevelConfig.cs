using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "LevelConfig", menuName = "Scriptable Objects/LevelConfig")]
public class LevelConfig : ScriptableObject
{
    [Serializable]
    public class CodeBlockConfig
    {
        public string code;
        public string block;
    }
    
    [SerializeField]
    public List<CodeBlockConfig> codeBlocks;
}
