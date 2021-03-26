using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace TGS {
	public class Demo16 : MonoBehaviour {
		
		TerrainGridSystem grid;
		public Texture2D[] fruits;
		GUIStyle labelStyle;
		int[][] directions = new int[][] {
			new int[] { 0, 1 },
			new int[] { 1, 0 },
			new int[] {
				0,
				-1
			},
			new int[] {
				-1,
				0
			}
		};

		void Start () {
			// setup GUI - only for the demo
			GUIResizer.Init (800, 500); 
			labelStyle = new GUIStyle ();
			labelStyle.alignment = TextAnchor.MiddleLeft;
			labelStyle.normal.textColor = Color.white;

			// Get a reference to TGS system's API
			grid = TerrainGridSystem.instance;
			for (int k = 0; k < grid.numCells; k++) {
				Texture2D fruitTexture = fruits [Random.Range (0, fruits.Length)];
				DrawFruit (k, fruitTexture);
			}

			grid.OnCellClick += CheckMatchingCells;
		}

		void OnGUI () {
			// Do autoresizing of GUI layer
			GUIResizer.AutoResize ();
			GUI.Label (new Rect (10, 10, 300, 30), "Click 3 or more matching cells to remove them!", labelStyle);
		}

		void DrawFruit (int cellIndex, Texture2D fruitTexture) {
			Vector2 textureOffset = Vector2.zero; 
			Vector2 textureScale = new Vector2 (1f, 0.7f); // to keep some aspect ratio
			float rotationDegrees = 0f;
			grid.CellToggleRegionSurface (cellIndex, true, Color.white, false, fruitTexture, textureScale, textureOffset, rotationDegrees, true);
		}

		void CheckMatchingCells (TerrainGridSystem grid, int cellIndex, int buttonIndex) {
			if (cellIndex < 0)
				return;

			Texture2D selectedFruit = grid.CellGetTexture (cellIndex);
			if (selectedFruit == null)
				return;	// empty cell

			// Checks all directions until all matches are selected and no pending cells remains
			List<int> matches = new List<int> ();
			matches.Add (cellIndex);
			List<int> pending = new List<int> ();
			pending.Add (cellIndex);

			while (pending.Count > 0) {
				// extract one fruit from the queue
				int p = pending [0];
				pending.RemoveAt (0);
				// check all 4 directions of that cell for new matches
				int row = grid.CellGetRow (p);
				int col = grid.CellGetColumn (p);
				for (int k = 0; k < directions.Length; k++) {
					int row1 = row + directions [k] [0];
					int col1 = col + directions [k] [1];
					if (row1 >= 0 && row1 < grid.rowCount && col1 >= 0 && col1 < grid.columnCount) {
						int i = grid.CellGetIndex (row1, col1);
						Texture2D tex = grid.CellGetTexture (i);
						if (tex == selectedFruit && !matches.Contains (i)) {
							matches.Add (i);
							pending.Add (i);
						}
					}
				}
			}

			// If there're 3 or more matches remove them
			if (matches.Count >= 3) {
				matches.ForEach ((int matchingIndex) => {
					// Remove fruit
					grid.CellSetTexture (matchingIndex, null);
					// Produce a nice effect for matching cells
					grid.CellFlash (matchingIndex, Color.black, Color.yellow, 0.25f);
				});
				StartCoroutine (MakeFruitsFall ());
			}

		}

		IEnumerator MakeFruitsFall () {

			bool changes = true;
			while (changes) {
				changes = false;
				// Make all fruits fall to occupy empty slots
				for (int r = 0; r < grid.rowCount - 1; r++) {
					for (int c = 0; c < grid.columnCount; c++) {
						int thisCell = grid.CellGetIndex (r, c);
						Texture2D tex = grid.CellGetTexture (thisCell);
						// is this empty?
						if (tex == null) {
							// is there another fruit just above?
							int aboveCell = grid.CellGetIndex (r + 1, c);
							Texture2D texAbove = grid.CellGetTexture (aboveCell);
							if (texAbove != null) {
								grid.CellSetTexture (thisCell, texAbove);
								grid.CellSetTexture (aboveCell, null);
								changes = true;
							}
						}
					}
				}
				yield return new WaitForSeconds (0.1f);
			}
		}


	}
}
