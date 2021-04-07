using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;


public class GameStateManager : MonoBehaviour
{
    [HideInInspector] public UnityEvent<GameState> onStateChanged = new UnityEvent<GameState>();

    public UnityEvent onPresentation = new UnityEvent();
    public UnityEvent onCommand = new UnityEvent();
    public UnityEvent onPlay = new UnityEvent();
    public UnityEvent onCounterPlay = new UnityEvent();
    public UnityEvent onSuccessView = new UnityEvent();
    public UnityEvent onFailView = new UnityEvent();

    private GameState _currentState;
    private static GameStateManager _instance;

    public static GameStateManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<GameStateManager>();
                if (_instance == null)
                {
                    Debug.LogWarning("GameStateManager gameObject not found in the scene!");
                }
            }

            return _instance;
        }
    }

    private void Awake()
    {
        ChangeState(GameState.LevelPresentation);
    }

    public void ChangeState(GameState newState)
    {
        _currentState = newState;
        onStateChanged.Invoke(newState);
        switch (newState)
        {
            case GameState.LevelPresentation:
                onPresentation.Invoke();
                break;
            case GameState.Command:
                onCommand.Invoke();
                break;
            case GameState.Play:
                onPlay.Invoke();
                break;
            case GameState.CounterPlay:
                onCounterPlay.Invoke();
                break;
            case GameState.SuccessView:
                onSuccessView.Invoke();
                break;
            case GameState.FailView:
                onFailView.Invoke();
                break;
        }
    }
}

public enum GameState
{
    LevelPresentation = 1,
    Command = 2,
    Play = 3,
    CounterPlay = 4,
    FailView = 5,
    SuccessView = 6
}