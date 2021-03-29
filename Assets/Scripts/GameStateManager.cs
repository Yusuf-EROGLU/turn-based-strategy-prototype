using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;


public class GameStateManager : MonoBehaviour
{
    [HideInInspector] public YsfEvents<GameState> onStateChanged = new YsfEvents<GameState>();
    
    public static GameStateManager instance;
    public YsfEvents onPresentation = new YsfEvents();
    public YsfEvents onCommand = new YsfEvents();
    public YsfEvents onPlay = new YsfEvents();
    public YsfEvents onCounterPlay = new YsfEvents();
    public YsfEvents onSuccessView = new YsfEvents();
    public YsfEvents onFailView = new YsfEvents();

    private GameState _currentState;
    
    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else if (instance != this)
        {
            Destroy(instance);
            instance = this;
        }


        ChangeState(GameState.LevelPresentation);
    }


    private void ChangeState(GameState newState)
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


///<summary>
///these classes for inspector handlers
/// </summary>
[Serializable]
public class YsfEvents : UnityEvent
{
}

[Serializable]
public class YsfEvents<T> : UnityEvent<T>
{
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