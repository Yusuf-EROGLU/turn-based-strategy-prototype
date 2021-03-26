using UnityEngine;
using System.Collections;
using System.Collections.Generic;


namespace TGS {
	public class Demo10b : MonoBehaviour {

		TerrainGridSystem tgs;
		GUIStyle labelStyle;
		Rigidbody character;

		void Start () {
			tgs = TerrainGridSystem.instance;

			// setup GUI resizer - only for the demo
			GUIResizer.Init (800, 500); 

			// setup GUI styles
			labelStyle = new GUIStyle ();
			labelStyle.alignment = TextAnchor.MiddleLeft;
			labelStyle.normal.textColor = Color.black;

			character = GameObject.Find ("Character").GetComponent<Rigidbody>();
		}

		void OnGUI () {
			// Do autoresizing of GUI layer
			GUIResizer.AutoResize ();
			GUI.backgroundColor = new Color (0.8f, 0.8f, 1f, 0.5f);
			GUI.Label (new Rect (10, 5, 160, 30), "Move the ball with WASD and press G to reposition grid around it.", labelStyle);
			GUI.Label (new Rect (10, 25, 160, 30), "Press N to show neighbour cells around the character position.", labelStyle);
			GUI.Label (new Rect (10, 45, 160, 30), "Press C to snap to center of cell.", labelStyle);
			GUI.Label (new Rect (10, 65, 160, 30), "Open the Demo10b.cs script to learn how to assign gridCenter property using code.", labelStyle);
		}

		void Update() {

			// Move ball
			const float strength = 10f;
			if (Input.GetKey(KeyCode.W)) {
				character.AddForce(Vector3.forward * strength);
			}
			if (Input.GetKey(KeyCode.S)) {
				character.AddForce(Vector3.back * strength);
			}
			if (Input.GetKey(KeyCode.A)) {
				character.AddForce(Vector3.left * strength);
			}
			if (Input.GetKey(KeyCode.D)) {
				character.AddForce(Vector3.right * strength);
			}
			if (Input.GetKeyDown(KeyCode.C)) {
				SnapToCellCenter();
			}

			// Reposition grid
			if (Input.GetKeyDown(KeyCode.G)) {
				RepositionGrid();
			}

			// Show neighbour cells
			if (Input.GetKeyDown(KeyCode.N)) {
				ShowNeighbours(character.transform.position);
			}

			// Position camera
			Camera.main.transform.position = character.transform.position + new Vector3(0,20,-20);
			Camera.main.transform.LookAt(character.transform.position);

		}

		// Updates grid position around newPosition
		void RepositionGrid() {
			tgs.SetGridCenterWorldPosition(character.transform.position, true);
		}

		// Moves character to center of current cell
		void SnapToCellCenter() {
			Vector3 pos = tgs.SnapToCell(character.transform.position);
			// Shift pos a bit upwards
			pos -= tgs.transform.forward;
			character.transform.position = pos;
		}


		// Highlight neighbour cells around character posiiton
		void ShowNeighbours(Vector3 position) {
			Cell characterCell = tgs.CellGetAtPosition(position, true);
			List<Cell> neighbours = tgs.CellGetNeighbours(characterCell);
			if (neighbours!=null) {
				foreach(Cell cell in neighbours) {
					tgs.CellFadeOut(cell, Color.red, 2.0f);
				}
			}
		}

	}

}