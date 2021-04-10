using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PresentationView : MonoBehaviour
{
    public Button startButton;
    private GameStateManager _gameStateManager;

    private void Awake()
    {
        ActivateElements(false);
        _gameStateManager = GameStateManager.Instance;

        startButton.onClick.AddListener(() =>
        {
            _gameStateManager.ChangeState(GameState.Command);
            Hide();
        });
    }
    


    public void Show()
    {
        ActivateElements();
    }

    private void Hide()
    {
        ActivateElements(false);
    }
    private void ActivateElements(bool state = true)
    {
        startButton.gameObject.SetActive(state);
    }
}