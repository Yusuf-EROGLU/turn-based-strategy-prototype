using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace TGS {
	public class Demo1 : MonoBehaviour {

		TerrainGridSystem tgs;
		GUIStyle labelStyle, labelStyleShadow, buttonStyle, sliderStyle, sliderThumbStyle;
		float terrainSteepness = 2;
		GameObject ball, ballParent;
		int ballCount;

		void Start() {
			tgs = TerrainGridSystem.instance;

			// setup GUI resizer - only for the demo
			GUIResizer.Init(800, 500); 

			// setup GUI styles
			labelStyle = new GUIStyle();
			labelStyle.alignment = TextAnchor.MiddleLeft;
			labelStyle.normal.textColor = Color.black;
			labelStyleShadow = new GUIStyle(labelStyle);
			labelStyleShadow.normal.textColor = Color.black;
			buttonStyle = new GUIStyle(labelStyle);
			buttonStyle.alignment = TextAnchor.MiddleCenter;
			buttonStyle.normal.background = Texture2D.whiteTexture;
			buttonStyle.normal.textColor = Color.black;
			sliderStyle = new GUIStyle();
			sliderStyle.normal.background = Texture2D.whiteTexture;
			sliderStyle.fixedHeight = 4.0f;
			sliderThumbStyle = new GUIStyle();
			sliderThumbStyle.normal.background = Resources.Load<Texture2D>("thumb");
			sliderThumbStyle.overflow = new RectOffset(0, 0, 8, 0);
			sliderThumbStyle.fixedWidth = 20.0f;
			sliderThumbStyle.fixedHeight = 12.0f;

			ball = Resources.Load<GameObject>("Ball");
			ballParent = new GameObject("BallParent");
			ballParent.transform.position = tgs.terrainCenter + Vector3.up * 200.0f;

			ResetTerrain();
			SpawnBall();
		}

		void OnGUI() {
			// Do autoresizing of GUI layer	
			GUIResizer.AutoResize();

			GUI.backgroundColor = new Color(0.8f, 0.8f, 1f, 0.5f);

			GUI.Label(new Rect(10, 10, 160, 30), "Steepness", labelStyle);
			terrainSteepness = GUI.HorizontalSlider(new Rect(102, 25, 200, 30), terrainSteepness, 0, 10, sliderStyle, sliderThumbStyle);

			if (GUI.Button(new Rect(10, 70, 160, 30), "Randomize Terrain", buttonStyle)) {
				RandomizeTerrain(0.75f);
			}

		}

		void Update() {
			Camera.main.transform.RotateAround(tgs.terrainCenter, Vector3.up, Time.deltaTime * 2.0f);

			if (ballCount < 12 && Random.value > 0.98f)
				SpawnBall();

			if (Time.time > 3 && Time.time < 6) {
				tgs.cellBorderAlpha = (Time.time - 3) / 8.0f;
			}

		}

		void ResetTerrain() {
			RandomizeTerrain(0);
			while (ballParent.transform.childCount > 0) {
				Destroy(ballParent.transform.GetChild(0));
			}

			tgs.cellBorderAlpha = 0;
		}

		void RandomizeTerrain(float strength) {
			tgs.terrain.heightmapMaximumLOD = 0;	// always show maximum detail

			int w = tgs.terrain.terrainData.heightmapResolution;
			int h = tgs.terrain.terrainData.heightmapResolution;
			float[,] heights = tgs.terrain.terrainData.GetHeights(0, 0, w, h);

			float z = Time.time;
			for (int k = 0; k < w; k++) {
				for (int j = 0; j < h; j++) {
					heights[k, j] = Mathf.PerlinNoise((float)((z + k) * terrainSteepness % w) / w, (float)((z + j) * terrainSteepness % h) / h) * strength;
				}
			}

			// Add a few towers
			int maxRadius = 20;
			for (int k = 0; k < 3; k++) {
				int x = Random.Range(maxRadius, w - maxRadius);
				int y = Random.Range(maxRadius, h - maxRadius);
				for (int r = 0; r < maxRadius; r++) {
					for (int a = 0; a < 360; a++) {
						int c = (int)(x + Mathf.Cos(a * Mathf.Deg2Rad) * r);
						int l = (int)(y + Mathf.Sin(a * Mathf.Deg2Rad) * r);
						heights[c, l] = 1;
					}
				}

			}

			tgs.terrain.terrainData.SetHeights(0, 0, heights);

			TerrainCollider collider = tgs.terrain.GetComponent<TerrainCollider> ();
			collider.terrainData = null;
			collider.terrainData = tgs.terrain.terrainData;

			tgs.GenerateMap();

			// Show center of cells
//			GameObject centroidParent = GameObject.Find("Centroids");
//			if (centroidParent!=null) GameObject.Destroy(centroidParent);
//			centroidParent = new GameObject("Centroids");
//			for (int k=0;k<tgs.cells.Count;k++) {
//				Vector3 center = tgs.CellGetPosition(k);
//				GameObject obj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
//				obj.transform.position = center;
//				obj.transform.localScale = Misc.Vector3one * 5f;
//				obj.transform.SetParent(centroidParent.transform, true);
//				obj.GetComponent<Renderer>().sharedMaterial.color = Color.black;
//			}
		}

		/// <summary>
		/// Instantiates the ball - the ball is controlled by the script "BallController" which orient the ball toward current selected cell and highlight cell beneath the ball
		/// </summary>
		void SpawnBall() {
			ballCount++;
			GameObject newBall = Instantiate(ball);
			newBall.transform.SetParent(ballParent.transform, false);
			newBall.transform.localPosition = Misc.Vector3zero;
			newBall.GetComponent<Rigidbody>().AddForce(Random.onUnitSphere * 20);
		}
	}

}