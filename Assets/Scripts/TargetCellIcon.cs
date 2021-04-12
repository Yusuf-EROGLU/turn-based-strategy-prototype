using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

public class TargetCellIcon : MonoBehaviour
{
  private SpriteRenderer _spriteRenderer;

  private void Awake()
  {
    _spriteRenderer = GetComponent<SpriteRenderer>();
  }
  
  public void HighLight(Vector3 position)
  {
    var transformRef = transform;
    transformRef.position = position;
    transformRef.localScale = Vector3.zero;
    transformRef.DOScale(new Vector3(1.5f, 1.5f, 1f), 0.4f).SetEase(Ease.InSine);
  }
}
