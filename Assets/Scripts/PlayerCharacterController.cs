using System;
using System.Collections;
using System.Collections.Generic;
using TGS;
using UnityEngine;

public class PlayerCharacterController : CharacterControllerBase
{
    private PlayerMove _playerMove = new PlayerMove();
    
    protected override void Awake()
    {
        base.Awake();

    }
    
    private void OnCommandState()
    {
        _levelController.onLegalClick.AddListener(_playerMove.AddTargetCell);
    }
}