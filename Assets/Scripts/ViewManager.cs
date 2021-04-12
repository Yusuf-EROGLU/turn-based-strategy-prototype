using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ViewManager : MonoBehaviour
{
   private static ViewManager _instance;
   public static ViewManager Instance
   {
      get
      {
         if (_instance == null)
         {
            _instance = FindObjectOfType<ViewManager>();
            if (_instance == null)
            {
               Debug.LogWarning("ViewManager gameObject not found in the scene!");
            }
         }

         return _instance;
      }
   }
   public PresentationView PresentationView => _presentationView;
   public LevelView LevelView => _levelView;

   [SerializeField] private PresentationView _presentationView;
   [SerializeField] private LevelView _levelView;
   private void Awake()
   {
 
   }
   
 
   
}
