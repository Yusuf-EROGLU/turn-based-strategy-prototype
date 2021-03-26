using UnityEngine;
using System.Collections;

namespace TGS {

	public class Demo10 : MonoBehaviour {

		TerrainGridSystem tgs;
		GUIStyle labelStyle, labelStyleShadow, buttonStyle, sliderStyle, sliderThumbStyle;
		float terrainSteepness = 2;
		int gridCenterY, gridCenterX;

		void Start () {
			tgs = TerrainGridSystem.instance;

			// setup GUI resizer - only for the demo
			GUIResizer.Init (800, 500); 

			// setup GUI styles
			labelStyle = new GUIStyle ();
			labelStyle.alignment = TextAnchor.MiddleLeft;
			labelStyle.normal.textColor = Color.black;
			labelStyleShadow = new GUIStyle (labelStyle);
			labelStyleShadow.normal.textColor = Color.black;
			buttonStyle = new GUIStyle (labelStyle);
			buttonStyle.alignment = TextAnchor.MiddleCenter;
			buttonStyle.normal.background = Texture2D.whiteTexture;
			buttonStyle.normal.textColor = Color.black;
			sliderStyle = new GUIStyle ();
			sliderStyle.normal.background = Texture2D.whiteTexture;
			sliderStyle.fixedHeight = 4.0f;
			sliderThumbStyle = new GUIStyle ();
			sliderThumbStyle.normal.background = Resources.Load<Texture2D> ("thumb");
			sliderThumbStyle.overflow = new RectOffset (0, 0, 8, 0);
			sliderThumbStyle.fixedWidth = 20.0f;
			sliderThumbStyle.fixedHeight = 12.0f;
			StartCoroutine(MoveGrid());
		}

		void OnGUI () {
			// Do autoresizing of GUI layer
			GUIResizer.AutoResize ();

			GUI.backgroundColor = new Color (0.8f, 0.8f, 1f, 0.5f);

			GUI.Label (new Rect (10, 5, 160, 30), "Example of a 10x10 grid with a grid scale of 0.1 which means the terrain can hold 100x100 cells.", labelStyle);

			GUI.Label (new Rect (10, 25, 160, 30), "Open the Demo10.cs script to learn how to assign gridCenter property using code.", labelStyle);

			GUI.Label (new Rect (10, 50, 160, 30), "Steepness", labelStyle);
			terrainSteepness = GUI.HorizontalSlider (new Rect (80, 65, 100, 30), terrainSteepness, 0, 10, sliderStyle, sliderThumbStyle);

			if (GUI.Button (new Rect (10, 90, 160, 30), "Randomize Terrain", buttonStyle)) {
				RandomizeTerrain (0.75f);
			}

		}

		void RandomizeTerrain(float strength) {
			tgs.terrain.heightmapMaximumLOD = 0;	// always show maximum detail

			int w = tgs.terrain.terrainData.heightmapResolution;
			int h = tgs.terrain.terrainData.heightmapResolution;
			float[,] heights = tgs.terrain.terrainData.GetHeights(0,0,w,h);

			float z = Time.time;
			for (int k=0;k<w;k++) {
				for (int j=0;j<h;j++) {
					heights[k,j] = Mathf.PerlinNoise( (float)((z+k)*terrainSteepness % w) /w, (float)((z+j)*terrainSteepness % h)/h)* strength;
				}
			}
			tgs.terrain.terrainData.SetHeights(0,0,heights);
			TerrainCollider collider = tgs.terrain.GetComponent<TerrainCollider> ();
			collider.terrainData = null;
			collider.terrainData = tgs.terrain.terrainData;
			tgs.GenerateMap();
		}

		IEnumerator MoveGrid() {
			// GridY and GridX specifies the position of the top/left cell of the grid in the 100x100 terrain
			// Our grid is 10x10 cells so gridX can range from 0..99 and gridY from 0..99
			gridCenterX += 10;
			if (gridCenterX>=100) {
				gridCenterX = 0;
				gridCenterY += 10;
				if (gridCenterY>=100) gridCenterY = 0;
			}

			// Moves the grid center so it matches gridY, gridX position
			tgs.gridCenter = new Vector2((gridCenterX - 45f) / 100f, (gridCenterY - 45f) / 100f);

			// Repeat
			yield return new WaitForSeconds(0.5f);
			StartCoroutine(MoveGrid ());
		}

	}

}