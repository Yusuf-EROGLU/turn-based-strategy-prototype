using UnityEngine;
using System.Collections;

namespace TGS {
				public class Demo7 : MonoBehaviour {

								TerrainGridSystem tgs;
								int currentRow, currentCol;

								void Start () {
												// Get a reference to Terrain Grid System's API
												tgs = TerrainGridSystem.instance;
												currentRow = 0;
												currentCol = 0;
												StartCoroutine (HighlightCell ());
								}

								// Highlight cells sequentially on each frame
								IEnumerator HighlightCell () {
												currentCol++;
												// run across the grid row by row
												if (currentCol >= tgs.columnCount) {
																currentCol = 0;
																currentRow++;
																if (currentRow >= tgs.rowCount)
																				currentRow = 0;
												}
												// get cell at current grid position and color it with fade out option
												Cell cell = tgs.CellGetAtPosition (currentCol, currentRow);
												if (cell != null) {
																int cellIndex = tgs.CellGetIndex (cell);
																float duration = Random.value * 2.5f + 0.5f;
																Color color = new Color (Random.value, Random.value, Random.value);
																tgs.CellFadeOut (cellIndex, color, duration);
												}
												// trigger next iteration after this frame
												yield return new WaitForEndOfFrame ();
												StartCoroutine (HighlightCell ());
								}


				}
}
