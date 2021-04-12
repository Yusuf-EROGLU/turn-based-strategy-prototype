using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TGS;
using UnityEngine;

public class PlayerMove : Move
{
    private Stack<Cell> _targetCells;
    private Stack<SkillData> _skillsToCast;

    public Cell NextCell
    {
        get => _targetCells.Pop();
        set => AddTargetCell(value);
    }

    public void AddTargetCell(Cell value)
    {
        _targetCells ??= new Stack<Cell>();
        //!!! for just one target value
        _targetCells.Clear();
        _targetCells.Push(value);
    }

    public SkillData NextSkill
    {
        get => _skillsToCast.Pop();
        set
        {
            if (_targetCells == null)
                _targetCells = new Stack<Cell>();
            _skillsToCast.Push(value);
        }
    }
}