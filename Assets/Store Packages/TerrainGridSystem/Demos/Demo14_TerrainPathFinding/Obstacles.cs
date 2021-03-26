using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace TGS {

	/// <summary>
	/// Marks random cells as unpassable
	/// </summary>
	public class Obstacles : MonoBehaviour {

		TerrainGridSystem tgs;

		// Use this for initialization
		void Start () {
			tgs = TerrainGridSystem.instance;
			Color blockedColor = new Color (0.2f, 0.2f, 0.2f, 1f);
			for (int k = 0; k < 1500; k++) {
				int cellIndex = Random.Range (0, tgs.cellCount);
				tgs.CellToggleRegionSurface (cellIndex, true, blockedColor);
				tgs.CellSetCanCross (cellIndex, false);
			}
		}

	}
}




