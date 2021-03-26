using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace TGS {
	
	public class ZoomManager : MonoBehaviour {

		public Texture2D worldTexture;
		public GameObject backgroundPlane;

		public float zoomSpeed = 10f;
		public float panSpeed = 0.001f;

		/// <summary>
		/// The maximum cell size relative to screen height
		/// </summary>
		public float maxCellSize = 0.08f;

		/* Internal fields */
		TerrainGridSystem tgs;
		bool dragging;
		Vector3 lastCameraPosition;
		Vector3 lastHitPos;
		int currentZoomLevel = -1;
		GUIStyle labelStyle;
		float cellSize;
		RaycastHit[] hits;
		Color32[] worldColors;
		float initialScale;
		Vector2 canvasWorldSize;
		Vector3 oldMousePos;

		void Start () {
			// setup GUI styles
			labelStyle = new GUIStyle ();
			labelStyle.alignment = TextAnchor.MiddleCenter;
			labelStyle.normal.textColor = Color.white;

			// Get a reference to Terrain Grid System APIs
			tgs = TerrainGridSystem.instance;

			// Load world texture
			worldColors = worldTexture.GetPixels32 ();

			// Buffer for raycasting (using RaycastNonAlloc avoids GC)
			hits = new RaycastHit[20];

			initialScale = tgs.transform.localScale.x;
			currentZoomLevel = -1;
			canvasWorldSize = tgs.GetRect ().size;
				
			UpdateGrid ();
		}


		// Update is called once per frame
		void Update () {

			Transform camTransform = Camera.main.transform;

			// Manage zoom
			float wheel = Input.GetAxis ("Mouse ScrollWheel");
			wheel *= Mathf.Sqrt (-camTransform.position.z / 500f);

			camTransform.position -= new Vector3 (0, 0, wheel * zoomSpeed);
			if (camTransform.position.z > -1) {
				camTransform.position = new Vector3 (camTransform.position.x, camTransform.position.y, -1);
			} else if (camTransform.position.z < -500) {
				camTransform.position = new Vector3 (camTransform.position.x, camTransform.position.y, -500);
			}

			// Manage panning
			if (Input.GetMouseButton (0)) {
				if (Input.mousePosition != oldMousePos) {
					oldMousePos = Input.mousePosition;
					Ray ray = Camera.main.ScreenPointToRay (Input.mousePosition);
					Vector3 worldPos;
					if (GetWorldHitFromRay (ray, out worldPos)) {
						if (!dragging) {
							lastHitPos = worldPos;
							dragging = true;
						}
						float dx = worldPos.x - lastHitPos.x;
						float dy = worldPos.y - lastHitPos.y;
						camTransform.position -= new Vector3 (dx, dy, 0);
					}
				}
			} else {
				dragging = false;
			}

			// Manage grid level
			if (lastCameraPosition != camTransform.position) {
				lastCameraPosition = camTransform.position;
				UpdateGrid ();
			}
		}

		void OnGUI () {
			GUI.Label (new Rect (0, 5, Screen.width, 30), "Distance: " + -Camera.main.transform.position.z + "   Current Zoom Level: " + currentZoomLevel, labelStyle);
			GUI.Label (new Rect (0, 25, Screen.width, 30), "Cell Size (height): " + cellSize, labelStyle);
		}

		void UpdateGrid () {
			// Flag to signal that zoom level has changed or grid has moved so we need to refresh grid scale and contents
			bool needRefreshGrid = false;

			// Computes cell sizes for all grids and enables only the grid within threshold
			Vector2 size = tgs.CellGetViewportSize (0);
			cellSize = size.y * (currentZoomLevel + 1);
			int zoomLevel = Mathf.RoundToInt (cellSize / maxCellSize);
			if (currentZoomLevel != zoomLevel) {
				currentZoomLevel = zoomLevel;
				needRefreshGrid = true;
			}

			// If current grid does not cover entire viewport then reposition it
			Transform t = tgs.transform;
			Rect gridWorldRect = tgs.GetRect ();
			Vector3 gridBL = Camera.main.WorldToViewportPoint (gridWorldRect.min);
			Vector3 gridTR = Camera.main.WorldToViewportPoint (gridWorldRect.max);
			if (gridBL.x > 0 || gridBL.y > 0 || gridTR.x < 1f || gridTR.y < 1f) {
				needRefreshGrid = true;
			}

			// Update grid contents
			if (needRefreshGrid) {
				// Adjust scale
				float scale = initialScale / (currentZoomLevel + 1);
				tgs.transform.localScale = new Vector3 (scale, scale, 1f);

				// Reposition the grid at the center of the screen
				Ray ray = Camera.main.ViewportPointToRay (new Vector3 (0.5f, 0.5f, Camera.main.nearClipPlane));
				Vector3 worldHit;
				if (GetWorldHitFromRay (ray, out worldHit)) {
					worldHit.z = 0; // keep the grid in front of the background
					tgs.SetGridCenterWorldPosition (worldHit, true);
				}

				// Refresh contents
				UpdateGridContents ();
			}

		}

		/// <summary>
		/// Ray casts to world background and returns the hit position
		/// </summary>
		/// <returns><c>true</c>, if world hit from ray was gotten, <c>false</c> otherwise.</returns>
		/// <param name="ray">Ray.</param>
		/// <param name="hitPoint">Hit point.</param>
		bool GetWorldHitFromRay (Ray ray, out Vector3 hitPoint) {
			int hitCount = Physics.RaycastNonAlloc (ray, hits);
			if (hitCount > 0) {
				for (int k = 0; k < hitCount; k++) {
					RaycastHit hit = hits [k];
					if (hit.collider.gameObject == backgroundPlane.gameObject) {
						hitPoint = hit.point;
						return true;
					}
				}
			}
			hitPoint = Vector3.zero;
			return false;
		}

		/// <summary>
		/// Updates cell contents based on their position in the world
		/// </summary>
		void UpdateGridContents () {
			int textureWidth = worldTexture.width;
			int textureHeight = worldTexture.height;
			int cellCount = tgs.cells.Count;
			for (int k = 0; k < cellCount; k++) {
				Cell cell = tgs.cells [k];
				// Get cell's center position and maps it to the world background plane 
				Vector3 cellCenter = tgs.CellGetPosition (cell);
				Vector2 worldPos = new Vector2 (cellCenter.x / canvasWorldSize.x, cellCenter.y / canvasWorldSize.y);
				if (worldPos.x > -0.5f && worldPos.y > -0.5f && worldPos.x < 0.5f && worldPos.y < 0.5f) {
					int tx = (int)((worldPos.x + 0.5f) * textureWidth);
					int ty = (int)((worldPos.y + 0.5f) * textureHeight);
					int colorIndex = ty * textureWidth + tx;
					Color32 color = worldColors [colorIndex];
					tgs.CellSetColor (k, color);
				} else {
					tgs.CellSetColor (k, Misc.ColorNull);
				}
			}
			tgs.HideHighlightedRegions ();
		}


	}
}