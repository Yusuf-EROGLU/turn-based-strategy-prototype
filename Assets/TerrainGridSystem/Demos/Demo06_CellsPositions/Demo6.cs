using UnityEngine;
using System.Collections;

namespace TGS {
				public class Demo6 : MonoBehaviour {

								TerrainGridSystem tgs;

								void Start () {

												// Load marker prefabs
												GameObject yellowSpherePrefab = Resources.Load<GameObject> ("YellowSphere");
												GameObject redSpherePrefab = Resources.Load<GameObject> ("RedSphere");

												// Get a reference to Terrain Grid System's API
												tgs = TerrainGridSystem.instance;

												// Iterate each cell and draws a sphere on the center of the cell as well as on the vertices
												for (int k = 0; k < tgs.cells.Count; k++) {

																// Add a yellow circle on the center of the cell
																Vector3 worldSpaceCenter = tgs.CellGetPosition (k);
																GameObject yellowSphere = Instantiate (yellowSpherePrefab);
																yellowSphere.transform.position = worldSpaceCenter + Vector3.up * 0.1f; 

																// For each vertex of the cell, add a red circle
																int vertexCount = tgs.CellGetVertexCount (k);
																for (int p = 0; p < vertexCount; p++) {
																				Vector3 worldSpacePosition = tgs.CellGetVertexPosition (k, p);
																				GameObject redSphere = Instantiate (redSpherePrefab);
																				redSphere.transform.position = worldSpacePosition + Vector3.up * 0.1f; // note: "up" is -z (grid is x,y) - displaced to avoid z-fighting
																}

												}

								}

	
	
	
				}
}
