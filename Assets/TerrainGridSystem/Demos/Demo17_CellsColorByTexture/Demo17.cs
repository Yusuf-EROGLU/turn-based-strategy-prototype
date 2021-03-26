using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace TGS {
	public class Demo17 : MonoBehaviour {
		
		public Texture2D textureForCells;
		TerrainGridSystem tgs;

		void Start () {
			// Get a reference to TGS system's API
			tgs = TerrainGridSystem.instance;

			// Read texture colors
			Color32[] colors = textureForCells.GetPixels32 ();

			// Iterate cells and picks the corresponding color in the texture
			int cellCount = tgs.cells.Count;
			for (int k = 0; k < cellCount; k++) {
				Vector2 cellCenter = tgs.cells [k].center;

				// Convert the center to texture coordinates
				// The center is in the range of -0.5..0.5, so we add 0.5 and multiply by the texture width in pixels to get the X texture coordinate
				int px = (int)((cellCenter.x + 0.5f) * textureForCells.width);
				// Same for Y
				int py = (int)((cellCenter.y + 0.5f) * textureForCells.height);

				// Now get the color
				Color32 color = colors [py * textureForCells.width + px];

				// And assign it to the cell
				tgs.CellToggleRegionSurface (k, true, color);
			}
		}

	}
}
