using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace TGS
{
	public class Demo6b : MonoBehaviour
	{
	
		TerrainGridSystem tgs;
		public int index = 0;
		public Vector2 location = Vector2.zero;

		float timer = 0.2f;
	
		// Use this for initialization
		void Start ()
		{
			Debug.Log ("Select the sphere in the hierarchy and watch script values.");
			Debug.Log ("Or click the sphere gameobject to print them to console.");

			tgs = TerrainGridSystem.instance;
			location.x = tgs.columnCount / 2;
			location.y = tgs.rowCount / 2;
		}
	
		// Update is called once per frame
		void Update ()
		{
			timer -= Time.deltaTime;
			if (timer <= 0) {
			
				//Use these lines to trace by column and row.
				location.x++;
				if (location.x >= tgs.columnCount) {
					location.x = 0;
					location.y++;
					if (location.y>=tgs.rowCount) {
						location.y = 0;
					}
				}
				Cell cell = tgs.CellGetAtPosition ((int)location.x, (int)location.y);
				int cellIndex = tgs.CellGetIndex (cell);
				if (cellIndex >= 0)
					transform.position = tgs.CellGetPosition (cellIndex);
			
			
				timer = 2.0f;
			}
		}

		void OnMouseDown() {

			// Gets the cell beneath the sphere
			Cell sphereCell = tgs.CellGetAtPosition(transform.position, true);
			Debug.Log ("Sphere Cell Row = " + sphereCell.row + ", Col = " + sphereCell.column);

			// Fade cells around sphere position by row and column
			const int size = 3;
			for (int j=sphereCell.row - size; j<=sphereCell.row + size;j++) {
				for (int k=sphereCell.column - size; k<=sphereCell.column + size; k++) {
					int cellIndex = tgs.CellGetIndex(j,k, true);
					tgs.CellFadeOut(cellIndex, Color.blue, 1.0f);
				}
			}

			// Get cell neighbours
			List<Cell>neighbours = tgs.CellGetNeighbours(sphereCell);
			foreach(Cell cell in neighbours) {
				tgs.CellFadeOut(cell, Color.red, 2.0f);
			}

		}
	}
}