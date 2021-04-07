using System;
using System.Collections;
using System.Collections.Generic;
using TGS;
using UnityEngine;
using UnityEngine.Events;

public class LevelController : MonoBehaviour
{
    public static LevelController Instance;
    public UnityEvent onLevelSuccess;
    public UnityEvent onLevelFail;
    [HideInInspector] public List<EnemyCharacterController> _enemies;
    [SerializeField] private PlayerCharacterController playerCharacterController;
    
    private TerrainGridSystem _terrainGridSystem;
    private GameStateManager _gameStateManager;
    private int _selectedCellIndex;
    private int _lastClickedCell;
    private int _playerCurrentPosition;

    private void Awake()
    {
        _gameStateManager = FindObjectOfType<GameStateManager>();
        onLevelSuccess = new UnityEvent();
        //maybe this can be explicity on the inspector
        onLevelSuccess.AddListener(_gameStateManager.onSuccessView.Invoke);
        onLevelFail.AddListener(_gameStateManager.onFailView.Invoke);
        _terrainGridSystem = TerrainGridSystem.instance;
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
        if (IsCellNeighbour(cellIndex))
        {
            _lastClickedCell = cellIndex;
        }
    }

    /// <summary>
    /// controls If clicked cell is neighbour of player character cell
    /// </summary>
    /// <param name="cellIndex"></param>
    /// <returns></returns>
    private bool IsCellNeighbour(int cellIndex)
    {
        var playerCellIndex= _terrainGridSystem.CellGetAtPosition(playerCharacterController.transform.position).index;
        var cells = _terrainGridSystem.cells;
        var bottomNeighbour = cells[_terrainGridSystem.CellGetNeighbour(playerCellIndex, CELL_SIDE.Bottom)];
        var topNeighbour = cells[_terrainGridSystem.CellGetNeighbour(playerCellIndex, CELL_SIDE.Top)];
        var leftNeighbour = cells[_terrainGridSystem.CellGetNeighbour(playerCellIndex, CELL_SIDE.Left)];
        var rightNeighbour = cells[_terrainGridSystem.CellGetNeighbour(playerCellIndex, CELL_SIDE.Right)];
        var neighbourList = new List<Cell> {topNeighbour, bottomNeighbour, leftNeighbour, rightNeighbour};
        return neighbourList.Contains(_terrainGridSystem.cells[cellIndex]);
    }
}