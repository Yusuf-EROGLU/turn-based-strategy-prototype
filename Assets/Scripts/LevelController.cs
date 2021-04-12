using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using TGS;
using UnityEngine;
using UnityEngine.Events;

public class LevelController : MonoBehaviour
{
    private static LevelController _instance;
    public UnityEvent onLevelSuccess;
    public UnityEvent onLevelFail;
    public UnityEvent<Cell> onLegalClick;
    [HideInInspector] public List<EnemyCharacterController> _enemies;
    [SerializeField] private PlayerCharacterController playerCharacterController;
    [SerializeField] private TargetCellIcon targetCellIcon;

    private TerrainGridSystem _terrainGridSystem;
    private GameStateManager _gameStateManager;
    private int _selectedCellIndex;
    private int _lastClickedCell;
    private int _playerCurrentPosition;

    public static LevelController Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<LevelController>();
                if (_instance == null)
                {
                    Debug.LogWarning("LevelController gameObject not found in the scene!");
                }
            }

            return _instance;
        }
    }

    private void Awake()
    {
        _gameStateManager = FindObjectOfType<GameStateManager>();
        onLevelSuccess = new UnityEvent();
        //maybe this can be explicity on the inspector
        onLevelSuccess.AddListener(_gameStateManager.onSuccessView.Invoke);
        onLevelFail.AddListener(_gameStateManager.onFailView.Invoke);
        _terrainGridSystem = TerrainGridSystem.instance;
    }


    public void StartCommand()
    {
        _terrainGridSystem.OnCellClick += EvaluateCellClick;
    }


    public int GetLastClickedLegalCellIndex()
    {
        return _lastClickedCell;
    }

    public Cell GetPlayerCell()
    {
        return _terrainGridSystem.CellGetAtPosition(playerCharacterController.transform.position);
    }

    public void ControlSuccessEnd()
    {
        if (_enemies.Count == 0)
        {
            onLevelSuccess.Invoke();
        }
    }

    private void EvaluateCellClick(TerrainGridSystem sender, int cellIndex, int buttonIndex)
    {
        if (IsCellNeighbourOfPlayerCell(cellIndex))
        {
            HighLightSelectedCell(cellIndex);
            onLegalClick.Invoke(_terrainGridSystem.cells[_lastClickedCell]);
        }
    }

    private void HighLightSelectedCell(int cellIndex)
    {
        _terrainGridSystem.CellClear(_lastClickedCell);
        _lastClickedCell = cellIndex;
        _terrainGridSystem.CellSetColor(_lastClickedCell, new Color(0.27f, 0.54f, 0.62f));
        var pos = _terrainGridSystem.CellGetPosition(_lastClickedCell) + new Vector3(0, 0.1f, 0);
        targetCellIcon.HighLight(pos);
    }

    /// <summary>
    /// controls If clicked cell is neighbour of player character cell
    /// </summary>
    /// <param name="cellIndex"></param>
    /// <returns></returns>
    private bool IsCellNeighbourOfPlayerCell(int cellIndex)
    {
        var playerCellIndex = _terrainGridSystem.CellGetAtPosition(playerCharacterController.transform.position, true)
            .index;
        var cells = _terrainGridSystem.cells;
        var bottomNeighbour = cells[_terrainGridSystem.CellGetNeighbour(playerCellIndex, CELL_SIDE.Bottom)];
        var topNeighbour = cells[_terrainGridSystem.CellGetNeighbour(playerCellIndex, CELL_SIDE.Top)];
        var leftNeighbour = cells[_terrainGridSystem.CellGetNeighbour(playerCellIndex, CELL_SIDE.Left)];
        var rightNeighbour = cells[_terrainGridSystem.CellGetNeighbour(playerCellIndex, CELL_SIDE.Right)];
        var neighbourList = new List<Cell> {topNeighbour, bottomNeighbour, leftNeighbour, rightNeighbour};
        return neighbourList.Contains(_terrainGridSystem.cells[cellIndex]);
    }
}