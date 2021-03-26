using UnityEngine;
using System;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using TGS.Geom;
using TGS.PathFinding;

namespace TGS {

    public enum CanCrossCheckType {
        Default = 0,
        IgnoreCanCrossCheckOnAllCells = 1,
        ignoreCanCrossCheckOnStartAndEndCells = 2
    }

    /* Event definitions */

    public delegate int PathFindingEvent(TerrainGridSystem sender, int cellIndex);


    public partial class TerrainGridSystem : MonoBehaviour {

        /// <summary>
        /// Fired when path finding algorithmn evaluates a cell. Return the increased cost for cell.
        /// </summary>
        public event PathFindingEvent OnPathFindingCrossCell;


        [SerializeField]
        HeuristicFormula
            _pathFindingHeuristicFormula = HeuristicFormula.EuclideanNoSQR;

        /// <summary>
        /// The path finding heuristic formula to estimate distance from current position to destination
        /// </summary>
        public PathFinding.HeuristicFormula pathFindingHeuristicFormula {
            get { return _pathFindingHeuristicFormula; }
            set {
                if (value != _pathFindingHeuristicFormula) {
                    _pathFindingHeuristicFormula = value;
                    isDirty = true;
                }
            }
        }

        [SerializeField]
        int
            _pathFindingMaxSteps = 2000;

        /// <summary>
        /// The maximum number of steps that a path can return.
        /// </summary>
        public int pathFindingMaxSteps {
            get { return _pathFindingMaxSteps; }
            set {
                if (value != _pathFindingMaxSteps) {
                    _pathFindingMaxSteps = value;
                    isDirty = true;
                }
            }
        }

        [SerializeField]
        float
            _pathFindingMaxCost = 200000;

        /// <summary>
        /// The maximum search cost of the path finding execution.
        /// </summary>
        public float pathFindingMaxCost {
            get { return _pathFindingMaxCost; }
            set {
                if (value != _pathFindingMaxCost) {
                    _pathFindingMaxCost = value;
                    isDirty = true;
                }
            }
        }

        [SerializeField]
        bool
            _pathFindingUseDiagonals = true;

        /// <summary>
        /// If path can include diagonals between cells
        /// </summary>
        public bool pathFindingUseDiagonals {
            get { return _pathFindingUseDiagonals; }
            set {
                if (value != _pathFindingUseDiagonals) {
                    _pathFindingUseDiagonals = value;
                    isDirty = true;
                }
            }
        }

        [SerializeField]
        float
            _pathFindingHeavyDiagonalsCost = 1.4f;

        /// <summary>
        /// The cost for crossing diagonals.
        /// </summary>
        public float pathFindingHeavyDiagonalsCost {
            get { return _pathFindingHeavyDiagonalsCost; }
            set {
                if (value != _pathFindingHeavyDiagonalsCost) {
                    _pathFindingHeavyDiagonalsCost = value;
                    isDirty = true;
                }
            }
        }


        #region Public Path Finding functions

        /// <summary>
        /// Returns an optimal path from startPosition to endPosition with options.
        /// </summary>
        /// <returns>The route consisting of a list of cell indexes.</returns>
        /// <param name="maxSearchCost">Maximum search cost for the path finding algorithm. A value of 0 will use the global default defined by pathFindingMaxCost</param>
        /// <param name="maxSteps">Maximum steps for the path. A value of 0 will use the global default defined by pathFindingMaxSteps</param>
        public List<int> FindPath(int cellIndexStart, int cellIndexEnd, float maxSearchCost = 0, int maxSteps = 0, int cellGroupMask = -1, CanCrossCheckType canCrossCheckType = CanCrossCheckType.Default, bool ignoreCellCosts = false) {
            float dummy;
            return FindPath(cellIndexStart, cellIndexEnd, out dummy, maxSearchCost, maxSteps, cellGroupMask, canCrossCheckType, ignoreCellCosts);
        }

        /// <summary>
		/// Returns an optimal path from startPosition to endPosition with options.
		/// </summary>
		/// <returns>The route consisting of a list of cell indexes.</returns>
		/// <param name="totalCost">The total accumulated cost for the path</param>
		/// <param name="maxSearchCost">Maximum search cost for the path finding algorithm. A value of 0 will use the global default defined by pathFindingMaxCost</param>
		/// <param name="maxSteps">Maximum steps for the path. A value of 0 will use the global default defined by pathFindingMaxSteps</param>
		public List<int> FindPath(int cellIndexStart, int cellIndexEnd, out float totalCost, float maxSearchCost = 0, int maxSteps = 0, int cellGroupMask = -1, CanCrossCheckType canCrossCheckType = CanCrossCheckType.Default, bool ignoreCellCosts = false) {
            List<int> results = new List<int>();
            FindPath(cellIndexStart, cellIndexEnd, results, out totalCost, maxSearchCost, maxSteps, cellGroupMask, canCrossCheckType, ignoreCellCosts);
            return results;
        }


        /// <summary>
        /// Returns an optimal path from startPosition to endPosition with options.
        /// </summary>
        /// <returns>The route consisting of a list of cell indexes.</returns>
        /// <param name="cellIndices">User provided list to fill with path indices</param>
        /// <param name="totalCost">The total accumulated cost for the path</param>
        /// <param name="maxSearchCost">Maximum search cost for the path finding algorithm. A value of 0 will use the global default defined by pathFindingMaxCost</param>
        /// <param name="maxSteps">Maximum steps for the path. A value of 0 will use the global default defined by pathFindingMaxSteps</param>
        public int FindPath(int cellIndexStart, int cellIndexEnd, List<int> cellIndices, out float totalCost, float maxSearchCost = 0, int maxSteps = 0, int cellGroupMask = -1, CanCrossCheckType canCrossCheckType = CanCrossCheckType.Default, bool ignoreCellCosts = false) {
            totalCost = 0;
            if (cellIndices == null) return 0;
            cellIndices.Clear();
            if (cellIndexStart == cellIndexEnd) return 0;
            Cell startCell = cells[cellIndexStart];
            Cell endCell = cells[cellIndexEnd];
            if (startCell == null || endCell == null) return 0;
            bool startCellCanCross = startCell.canCross;
            bool endCellCanCross = endCell.canCross;
            if (canCrossCheckType == CanCrossCheckType.ignoreCanCrossCheckOnStartAndEndCells) {
                startCell.canCross = endCell.canCross = true;
            } else if (!startCell.canCross || !endCell.canCross)
                return 0;
            ComputeRouteMatrix();
            finder.Formula = _pathFindingHeuristicFormula;
            finder.MaxSteps = maxSteps > 0 ? maxSteps : _pathFindingMaxSteps;
            finder.Diagonals = _pathFindingUseDiagonals;
            finder.HeavyDiagonalsCost = _pathFindingHeavyDiagonalsCost;
            finder.HexagonalGrid = _gridTopology == GRID_TOPOLOGY.Hexagonal;
            finder.MaxSearchCost = maxSearchCost > 0 ? maxSearchCost : _pathFindingMaxCost;
            finder.CellGroupMask = cellGroupMask;
            finder.IgnoreCanCrossCheck = canCrossCheckType == CanCrossCheckType.IgnoreCanCrossCheckOnAllCells;
            finder.IgnoreCellCost = ignoreCellCosts;
            if (OnPathFindingCrossCell != null) {
                finder.OnCellCross = FindRoutePositionValidator;
            } else {
                finder.OnCellCross = null;
            }
            List<PathFinderNode> route = finder.FindPath(this, startCell, endCell, out totalCost, _evenLayout);
            startCell.canCross = startCellCanCross;
            endCell.canCross = endCellCanCross;
            if (route != null) {
                int routeCount = route.Count;
                if (_gridTopology == GRID_TOPOLOGY.Irregular) {
                    for (int r = routeCount - 2; r >= 0; r--) {
                        cellIndices.Add(route[r].PX);
                    }
                } else {
                    for (int r = routeCount - 2; r >= 0; r--) {
                        int cellIndex = route[r].PY * _cellColumnCount + route[r].PX;
                        cellIndices.Add(cellIndex);
                    }
                }
                cellIndices.Add(cellIndexEnd);
            } else {
                return 0;    // no route available
            }
            return cellIndices.Count;
        }

        #endregion



    }
}

