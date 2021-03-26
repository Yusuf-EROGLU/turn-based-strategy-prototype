using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using TGS.Geom;

namespace TGS
{
	public static class TerrainWrapperProvider
	{

		/// <summary>
		/// Returns a terrrain wrapper for the kind of terrain object with optional parameters
		/// </summary>
		/// <returns>The terrain wrapper.</returns>
		public static ITerrainWrapper GetTerrainWrapper(GameObject terrainObject, ref string prefix, int heightmapWidth, int heightmapHeight) {
			if (terrainObject == null)
				return null;

			Terrain terrain = terrainObject.GetComponent<Terrain> ();
			if (terrain != null) {
				return new UnityTerrainWrapper (terrain);
			}

			MeshRenderer renderer = terrainObject.GetComponentInChildren<MeshRenderer> ();
			if (renderer != null) {
				if (string.IsNullOrEmpty (prefix)) {
					prefix = renderer.name.Truncate (5);
				}
				return new MeshTerrainWrapper (terrainObject, prefix, heightmapWidth, heightmapHeight);
			}

			return null;
		}


		/// <summary>
		/// Returns a gameobject under a given gameobject (including it) which can be used as the terrain root for any known terrain wrapper
		/// </summary>
		/// <returns>The suitable terrain.</returns>
		/// <param name="root">Root.</param>
		public static GameObject GetSuitableTerrain(GameObject root) {
			Terrain terrain = root.GetComponent<Terrain> ();
			if (terrain != null) {
				return terrain.gameObject;
			}
			MeshRenderer renderer = root.GetComponentInChildren<MeshRenderer> ();
			if (renderer != null) {
				return renderer.gameObject;
			}
			return null;
		}

	}
}

