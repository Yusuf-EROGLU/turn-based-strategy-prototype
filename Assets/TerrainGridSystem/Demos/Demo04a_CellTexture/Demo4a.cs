using UnityEngine;
using System.Collections;

namespace TGS {
	public class Demo4a : MonoBehaviour {

		TerrainGridSystem tgs;
		GUIStyle labelStyle;

		void Start () {
			// setup GUI styles
			labelStyle = new GUIStyle ();
			labelStyle.alignment = TextAnchor.MiddleCenter;
			labelStyle.normal.textColor = Color.black;

			// Get a reference to Terrain Grid System's API
			tgs = TerrainGridSystem.instance;

			// listen to events
			tgs.OnCellClick += (grid, cellIndex, buttonIndex) => TextureCell (cellIndex);

			// assign a canvas texture
			tgs.canvasTexture = Resources.Load<Texture2D> ("Textures/worldMap");
		}

		void OnGUI () {
			GUI.Label (new Rect (0, 5, Screen.width, 30), "Click on any position to reveal part of the canvas texture.", labelStyle);
		}

		void TextureCell (int cellIndex) {
			tgs.CellToggleRegionSurface (cellIndex, true, Color.white, false, tgs.canvasTexture);
		}


	}
}
