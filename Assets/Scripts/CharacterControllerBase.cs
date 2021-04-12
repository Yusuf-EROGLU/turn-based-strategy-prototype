using System;
using System.Collections;
using System.Collections.Generic;
using TGS;
using UnityEngine;

public class CharacterControllerBase : MonoBehaviour
{
    protected Animator _animator;
    protected LevelController _levelController;
    protected ViewManager _viewManager;
    protected LevelView _levelView;
    
    protected virtual void Awake()
    {
        _levelController = LevelController.Instance;
        _viewManager = ViewManager.Instance;
        _levelView = _viewManager.LevelView;
    }

    protected void Move()
    {
    }

    protected void PerformSkill()
    {
    }

    protected void SelfDestruct()
    {
    }
}