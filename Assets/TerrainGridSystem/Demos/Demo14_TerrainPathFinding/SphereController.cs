using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace TGS {
	public class SphereController : MonoBehaviour {

		enum State {
			IDLE,
			MOVING,
			MOVESELECT
		}

		State state;
		TerrainGridSystem tgs;
		List<int> moveList;
		short moveCounter;
		int lastTerrIndex;

		// Use this for initialization
		void Start () {
			tgs = TerrainGridSystem.instance;
			state = State.MOVESELECT;
	
		}
	
		// Update is called once per frame
		void Update () {

			// Blinks the new territory under the sphere
			Territory terr = tgs.TerritoryGetAtPosition (transform.position, true);
			int terrIndex = tgs.TerritoryGetIndex (terr);
			if (terrIndex != lastTerrIndex) {
				lastTerrIndex = terrIndex;
				tgs.TerritoryBlink (terrIndex, Color.red, 0.5f);
			}

			// Check ball state
			switch (state) {
			case State.IDLE:
				break;
				
			case State.MOVING:
				if (moveCounter < moveList.Count) { 
					Move (tgs.CellGetPosition (moveList [moveCounter]));
				} else {
					moveCounter = 0;
					state = State.MOVESELECT;
				}
				break;
				
			case State.MOVESELECT:
				if (Input.GetMouseButtonUp (0)) {                   //gets path when left mouse is released and over terrain
					int t_cell = tgs.cellHighlightedIndex;
					tgs.CellFadeOut (t_cell, Color.red, 50);
					if (t_cell != -1) {                           //checks if we selected a cell
						int startCell = tgs.CellGetIndex (tgs.CellGetAtPosition (transform.position, true));
						float totalCost;
						moveList = tgs.FindPath (startCell, t_cell, out totalCost);
						if (moveList == null)
							return;
						Debug.Log ("Cell Clicked: " + t_cell + ", Total move cost: " + totalCost);
						tgs.CellFadeOut (moveList, Color.green, 5f);
						moveCounter = 0;
						state = State.MOVING;
					} else {
						Debug.Log ("No Cell");
					}
				}
				break;
			}
		}

		void Move (Vector3 in_vec) {
			float speed = moveList.Count * 5f;
			float step = speed * Time.deltaTime;

			// target position must account the sphere height since the cellGetPosition will return the center of the cell which is at floor.
			in_vec.y += transform.localScale.y * 0.5f; 
			transform.position = Vector3.MoveTowards (transform.position, in_vec, step);

			// Check if character has reached next cell (we use a small threshold to avoid floating point comparison; also we check only xz plane since the character y position could be adjusted or limited
			// by the slope of the terrain).
			float dist = Vector2.Distance (new Vector2 (transform.position.x, transform.position.z), new Vector2 (in_vec.x, in_vec.z));
			if (dist <= 0.1f) {
				moveCounter++;
			}
		}

	}
}




