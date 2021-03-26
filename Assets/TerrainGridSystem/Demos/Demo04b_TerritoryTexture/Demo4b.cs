using UnityEngine;
using System.Collections;

namespace TGS {
	public class Demo4b : MonoBehaviour {

		TerrainGridSystem tgs;
		GUIStyle labelStyle;
		public Texture2D texture;

		void Start () {
			// setup GUI styles
			labelStyle = new GUIStyle ();
			labelStyle.alignment = TextAnchor.MiddleCenter;
			labelStyle.normal.textColor = Color.black;

			// Get a reference to Terrain Grid System's API
			tgs = TerrainGridSystem.instance;

			// assign a canvas (background) texture
			tgs.canvasTexture = Resources.Load<Texture2D> ("Textures/worldMap");

			// listen to click event and implement territory coloring
			tgs.OnTerritoryClick += (TerrainGridSystem grid, int territoryIndex, int buttonIndex) => {
				// Color clicked territory in white
				tgs.TerritoryToggleRegionSurface (territoryIndex, true, Color.white, false, tgs.canvasTexture);
			};

		}

		void OnGUI () {
			GUI.Label (new Rect (0, 5, Screen.width, 30), "Click on any position to reveal part of the canvas texture.", labelStyle);
		}

		void Update () {
			if (Input.GetKeyDown (KeyCode.A)) {
				tgs.CellBlink (20, Color.yellow, Color.blue, 2f);
			}
			if (Input.GetKeyDown (KeyCode.B)) {
				tgs.CellToggleRegionSurface (20, true, Color.white, false, texture);
			}
		}



	}
}
