using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TGS
{

	public class Demo21 : MonoBehaviour
	{

		void Start()
		{
            TerrainGridSystem tgs = TerrainGridSystem.instance;
            int rows = tgs.rowCount;
            int columns = tgs.columnCount;
			Texture2D tex = new Texture2D(columns, rows);
            tex.filterMode = FilterMode.Point;
			Color[] colors = new Color[columns * rows];
            for (int k = 0; k < colors.Length; k++) {
                float c = Random.value * 0.5f + 0.5f;
                colors[k] = new Color(c, c, 1f);
            }
            tex.SetPixels(colors);
            tex.Apply();

            // Create a quad
            GameObject quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            quad.GetComponent<Renderer>().material.mainTexture = tex;

            // Adjust size to match grid also considering any cell scale
            Bounds bounds = tgs.bounds;
            quad.transform.position = bounds.center;
            quad.transform.localScale = new Vector3(bounds.size.x, bounds.size.y, 1f);

		}

	}

}