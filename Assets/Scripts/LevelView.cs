using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class LevelView : MonoBehaviour
{
   public Button nextTurnButton;
   public Button skillButton;

   private void Awake()
   {
      ActivateElements(false);
   }


   public void Show()
   {
      ActivateElements();
      
   }

   private void ActivateElements(bool state = true)
   {
      nextTurnButton.gameObject.SetActive(state);
      skillButton.gameObject.SetActive(state);
   }
}
