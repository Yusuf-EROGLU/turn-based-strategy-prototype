using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace TGS
{
	public class Demo15 : MonoBehaviour
	{

		TerrainGridSystem tgs;

		void Start ()
		{
			// Create a TGS instance from scratch

			// Step 1: instantiate TGS prefab and position it in world space
			GameObject tgsGO = Instantiate<GameObject>(Resources.Load<GameObject>("Prefabs/TerrainGridSystem"));
			tgsGO.transform.position = new Vector3(0,0,500);

			// Step 2: get a reference to the TGS main script (this is the API)
			tgs = tgsGO.GetComponent<TerrainGridSystem>();

			// Step 3: customize TGS look & feel (check the documentation for the complete property list)
			tgs.gridTopology = GRID_TOPOLOGY.Hexagonal;
			tgs.rowCount = 48;
			tgs.columnCount = 64;

			tgs.cellBorderColor = Color.yellow;	// color of cells
			tgs.GetComponent<MeshRenderer>().sharedMaterial.color = Color.black; // color of grid background

			tgs.showTerritories = true; // territories are optional
			tgs.numTerritories = 8;
			tgs.territoryDisputedFrontierColor = Color.red;
			tgs.highlightMode = HIGHLIGHT_MODE.Territories;

			// Step 4: configure some event trigger
			tgs.OnCellClick += OnCellClickHandler;
		
		}

		void OnCellClickHandler(TerrainGridSystem grid, int cellIndex, int buttonIndex) {
			Debug.Log ("Clicked on cell #" + cellIndex + " with button " + buttonIndex);
		}
	
	}
}