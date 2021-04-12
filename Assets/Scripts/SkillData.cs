using System.Collections;
using System.Collections.Generic;
using UnityEngine;


[CreateAssetMenu(fileName = "SkillData", menuName = "ScriptableObjects/" + "SkillData", order = 1)]
public class SkillData : ScriptableObject
{
    public SkillType type;
}

public enum SkillType
{
    Skill1,
    Skill2
    
}