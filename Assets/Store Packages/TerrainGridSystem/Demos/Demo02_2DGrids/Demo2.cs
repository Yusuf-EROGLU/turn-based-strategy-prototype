using UnityEngine;
using System.Collections;

namespace TGS {
	
	public class Demo2 : MonoBehaviour {

		TerrainGridSystem tgs;
		GUIStyle labelStyle;

		void Start () {
			tgs = TerrainGridSystem.instance;

			// setup GUI styles
			labelStyle = new GUIStyle ();
			labelStyle.alignment = TextAnchor.MiddleCenter;
			labelStyle.normal.textColor = Color.black;

			// Events
			// OnCellMouseDown occurs when user presses the mouse button on a cell
			tgs.OnCellMouseDown += OnCellMouseDown;
			// OnCellMouseUp occurs when user releases the mouse button on a cell even after a drag
            // OnCellClick occurs when user presses and releases the mouse button as in a normal click
			tgs.OnCellClick += OnCellClick;
        }

        void OnCellMouseDown (TerrainGridSystem grid, int cellIndex, int buttonIndex) {
			Debug.Log ("Mouse DOWN on cell #" + cellIndex);
		}

		void OnCellClick (TerrainGridSystem grid, int cellIndex, int buttonIndex) {
            tgs.CellSetTerritory(cellIndex, 200);
			if (buttonIndex == 0) {
				Debug.Log ("Mouse CLICK on cell #" + cellIndex + ", Merging!");
				MergeCell (tgs.cellHighlighted);
			} else if (buttonIndex == 1) {
				Debug.Log ("Right clicked on cell #" + cellIndex);
			}												
		}

		void OnGUI () {
			GUI.Label (new Rect (0, 5, Screen.width, 30), "Try changing the grid properties in Inspector. You can click a cell to merge it.", labelStyle);
		}


		/// <summary>
		/// Merge cell example. This function will make cell1 marge with a random cell from its neighbours.
		/// </summary>
		void MergeCell (Cell cell1) {
			int neighbourCount = cell1.region.neighbours.Count;
			if (neighbourCount == 0)
				return;
			Cell cell2 = (Cell)cell1.region.neighbours [Random.Range (0, neighbourCount)].entity;
			tgs.CellMerge (cell1, cell2);
			tgs.Redraw ();
		}

    }
}
