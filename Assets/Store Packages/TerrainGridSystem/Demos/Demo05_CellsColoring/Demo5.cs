using UnityEngine;
using System.Collections;

namespace TGS {
	public class Demo5 : MonoBehaviour {

		TerrainGridSystem tgs;
		GUIStyle labelStyle;

		void Start () {
			// setup GUI styles
			labelStyle = new GUIStyle ();
			labelStyle.alignment = TextAnchor.MiddleCenter;
			labelStyle.normal.textColor = Color.black;

			// Get a reference to Terrain Grid System's API
			tgs = TerrainGridSystem.instance;
		}

		void OnGUI () {
			GUI.Label (new Rect (0, 5, Screen.width, 30), "Click to highlight random cells at same time.", labelStyle);
		}

		void Update() {
			if (Input.GetMouseButtonDown(0)) {
				Color color = new Color (Random.value, Random.value, Random.value);
				for (int k=0;k<3;k++) {
					TriggerRandomCell(color);
				}
			}
		}


		void TriggerRandomCell(Color color) {
			// We get a random vector from -0.5..0.5 on both X and Y (z is ignored)
			Vector2 localPosition = Random.onUnitSphere * 0.5f;
			Cell cell = tgs.CellGetAtPosition(localPosition);
			if (cell!=null) {
				int cellIndex = tgs.CellGetIndex(cell);
				float duration = Random.value * 2.5f + 0.5f;
				tgs.CellFadeOut(cellIndex, color, duration);
			}
		}


	}
}
