using UnityEngine;
using System.Collections;

namespace TGS {
	public class Demo3 : MonoBehaviour {

		TerrainGridSystem tgs;
		GUIStyle labelStyle;

		void Start () {
			// setup GUI styles
			labelStyle = new GUIStyle ();
			labelStyle.alignment = TextAnchor.MiddleCenter;
			labelStyle.normal.textColor = Color.black;

			tgs = TerrainGridSystem.instance;

			// hide all cells
			for (int k = 0; k < tgs.cells.Count; k++) {
				tgs.CellSetBorderVisible (k, false);
			}
			tgs.Redraw(); // forces redraw immediately

			// listen to events
			tgs.OnCellClick += (grid, cellIndex, buttonIndex) => toggleCellVisible (cellIndex);

		}


		void OnGUI () {
			GUI.Label (new Rect (0, 5, Screen.width, 30), "Click on any position to toggle cell visibility.", labelStyle);
		}

		void toggleCellVisible (int cellIndex) {
			// CellSetBorderVisible controls the visibility of the cell's border
			// CellSetVisible controls the visibility of both cell's border and surface
			tgs.CellSetBorderVisible (cellIndex, !tgs.CellHasBorderVisible (cellIndex));
		}


	}
}
