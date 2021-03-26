using UnityEngine;
using System.Collections;

namespace TGS {
	public class DemoOrtho : MonoBehaviour {

        public SpriteRenderer sprite;

		TerrainGridSystem tgs;
		GUIStyle labelStyle;

		void Start () {
			tgs = TerrainGridSystem.instance;

			// setup GUI styles
			labelStyle = new GUIStyle ();
			labelStyle.alignment = TextAnchor.MiddleCenter;
			labelStyle.normal.textColor = Color.white;

			tgs.OnCellHighlight += (TerrainGridSystem grid, int cellIndex, ref bool cancelHighlight) => { 
				cancelHighlight = true; 
				tgs.CellFadeOut (cellIndex, Color.yellow, 2f); 
			};
			tgs.OnCellClick += (TerrainGridSystem grid, int cellIndex, int buttonIndex) => MergeCell (cellIndex);

            // Compute sprite scale
            //float tileSize = Vector3.Distance(tgs.CellGetVertexPosition(0, 0), tgs.CellGetVertexPosition(0, 3));
            //float spriteSize = Vector3.Distance(sprite.bounds.max, sprite.bounds.min);
            //float scale = tileSize / spriteSize;
            //for (int k=0;k<100;k++) {
            //    GameObject o = Instantiate<GameObject>(sprite.gameObject);
            //    o.transform.position = tgs.CellGetPosition(Random.Range(0, tgs.cellCount));
            //    o.transform.localScale = new Vector3(scale, scale, 1f);
            //}

            for (int k=0;k<tgs.cellCount;k++) {
                tgs.CellSetColor(k, Color.yellow);

            }
        }

		void OnGUI () {
			GUI.Label (new Rect (0, 5, Screen.width, 30), "Try changing the grid properties in Inspector. You can click a cell to merge it.", labelStyle);
		}

		/// <summary>
		/// Merge cell example. This function will make cell1 marge with a random cell from its neighbours.
		/// </summary>
		void MergeCell (int cellIndex) {
			Cell cell1 = tgs.cells [cellIndex];
			int neighbourCount = cell1.region.neighbours.Count;
			if (neighbourCount == 0)
				return;
			Cell cell2 = (Cell)cell1.region.neighbours [Random.Range (0, neighbourCount)].entity;
			tgs.CellMerge (cell1, cell2);
			tgs.Redraw ();
		}

	}
}
