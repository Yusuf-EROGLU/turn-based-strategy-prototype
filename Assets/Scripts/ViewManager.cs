using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ViewManager : MonoBehaviour
{
   public static ViewManager Instance;
   
   public PresentationView PresentationView => _presentationView;
   public LevelView LevelView => _levelView;

   [SerializeField] private PresentationView _presentationView;
   [SerializeField] private LevelView _levelView;
   private void Awake()
   {
      Singleton();
   }
   
   private void Singleton()
   {
      if (Instance == null)
      {
         Instance = this;
      }
      else if (Instance != this)
      {
         Destroy(Instance);
         Instance = this;
      }
   }
   
}
