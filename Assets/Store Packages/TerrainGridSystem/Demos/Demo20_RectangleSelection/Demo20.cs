using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TGS;

public class Demo20 : MonoBehaviour {

	TerrainGridSystem tgs;

	void Start () {
		tgs = TerrainGridSystem.instance;
		tgs.enableRectangleSelection = true;
		tgs.OnRectangleSelection += SelectCells;

		tgs.OnCellClick += (grid, cellIndex, buttonIndex) => PaintCell(cellIndex);
	}

	void PaintCell(int cellIndex)
	{
		tgs.CellSetColor(cellIndex, Color.red);
	}

	void SelectCells (TerrainGridSystem grid, Vector2 localPosStart, Vector2 localPosEnd) {
		List<int> cells = new List<int> ();
		tgs.CellGetInArea (localPosStart, localPosEnd, cells);
		tgs.ClearAll ();
		tgs.CellSetColor (cells, Color.yellow);

	}
}
