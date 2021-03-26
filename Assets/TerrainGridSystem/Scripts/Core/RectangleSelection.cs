using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TGS {

	public class RectangleSelection : MonoBehaviour {

		[NonSerialized]
		public TerrainGridSystem tgs;

		public RectTransform selectionSquareImage;

		Vector3 screenStartPos, screenEndPos;

		void Start () {
			if (tgs == null) {
				tgs = TerrainGridSystem.instance;
			}
			selectionSquareImage.gameObject.SetActive (false);
		}

		void Update () {
			if (Input.GetMouseButtonDown (0)) {
				screenStartPos = screenEndPos = Input.mousePosition;
				selectionSquareImage.gameObject.SetActive (true);
			}

			if (selectionSquareImage.gameObject.activeSelf) {
				if (Input.GetMouseButtonUp (0)) {
					selectionSquareImage.gameObject.SetActive (false);
					// Only trigger selection if it has dragged
					Vector3 diff = screenEndPos - screenStartPos;
					if (Mathf.Abs (diff.x) > 5 || Mathf.Abs (diff.y) > 5) {
						if (tgs.OnRectangleSelection != null) {
							tgs.OnRectangleSelection (tgs, TransformScreenPointToLocalPosition (screenStartPos), TransformScreenPointToLocalPosition (screenEndPos));
						}
					}
				} else if (Input.GetMouseButton (0)) {
					screenEndPos = Input.mousePosition;
					Vector3 center = (screenStartPos + screenEndPos) * 0.5f;
					selectionSquareImage.position = center;

					float sizeX = Mathf.Abs (screenStartPos.x - screenEndPos.x);
					float sizeY = Mathf.Abs (screenStartPos.y - screenEndPos.y);
					selectionSquareImage.sizeDelta = new Vector2 (sizeX, sizeY);
				}
			}

			if (Input.GetKeyDown (KeyCode.Escape)) {
				selectionSquareImage.gameObject.SetActive (false);
			}

		}

		Vector2 TransformScreenPointToLocalPosition (Vector3 screenPos) {
			Plane plane = new Plane (-tgs.transform.forward, tgs.transform.position);
			Ray ray = tgs.cameraMain.ScreenPointToRay (screenPos);
			float enter = 0;
			if (plane.Raycast (ray, out enter)) {
				Vector3 worldPos = ray.GetPoint (enter);
				return tgs.transform.InverseTransformPoint (worldPos);
			} else {
				return Misc.Vector2zero;
			}
		}


	}

}