using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PresentationView : MonoBehaviour
{
    public Button startButton;

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
        startButton.gameObject.SetActive(state);
    }
    
    
    
}
