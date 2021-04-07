using System;
using System.Collections;
using System.Collections.Generic;
using TGS;
using UnityEngine;

public class CharacterControllerBase : MonoBehaviour
{
    protected Animator _animator;
    protected TerrainGridSystem _terrainGridSystem;
    protected LevelController _levelController;
    protected void Awake()
    {
        _terrainGridSystem = TerrainGridSystem.instance;
        _animator = GetComponent<Animator>();
        _levelController = LevelController.Instance;
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