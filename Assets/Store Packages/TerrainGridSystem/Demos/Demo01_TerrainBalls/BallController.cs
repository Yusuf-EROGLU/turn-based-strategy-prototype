using UnityEngine;
using System.Collections;

namespace TGS {
	public class BallController : MonoBehaviour {

		TerrainGridSystem tgs;
		Rigidbody rb;

		void Start () {
			tgs = TerrainGridSystem.instance;
			rb = GetComponent<Rigidbody> ();
		}
	
		// Orient the ball toward current selected cell and highlight cell beneath the ball
		void Update () {
			if (transform.position.y < -10)
				Destroy (gameObject);
			
			// Move ball towards the currently selected cell
			if (tgs.cellHighlightedIndex >= 0) {
				Vector3 position = tgs.CellGetPosition (tgs.cellHighlightedIndex);
				Vector3 direction = (position - transform.position).normalized;
				rb.AddForce (direction * 100);
			}

			// Highlight cell under ball
			Cell terrainCell = tgs.CellGetAtPosition (transform.position, true);
			int cellIndex = tgs.CellGetIndex (terrainCell);
			if (cellIndex >= 0) {
				tgs.CellFadeOut (cellIndex, Color.yellow, 1f);
			}
		}
	}

}