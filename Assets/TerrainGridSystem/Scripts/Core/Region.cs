using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using TGS.Geom;

namespace TGS {
	public class Region {

		public Polygon polygon;

		public List<Vector2> points { get; set; }

		public List<Segment> segments;
		public List<Region> neighbours;
		public IAdmin entity;
		public Rect rect2D;
		public float rect2DArea;
		public Renderer renderer;
        public GameObject surfaceGameObject { get { return renderer != null ? renderer.gameObject : null; } }

		public Material customMaterial { get; set; }

		public Vector2 customTextureScale, customTextureOffset;
		public float customTextureRotation;
		public bool customRotateInLocalSpace;

		public delegate bool ContainsFunction(float x, float y);
		public ContainsFunction Contains;

		public bool isBox;

        /// <summary>
        /// If the gameobject contains one or more children surfaces with name splitSurface due to having +65000 vertices
        /// </summary>
		public List<Renderer> childrenSurfaces;

		public Region (IAdmin entity, bool isBox) {
			this.entity = entity;
			this.isBox = isBox;
			if (isBox) {
				neighbours = new List<Region> (4);
				segments = new List<Segment> (4);
				Contains = PointInBox;
			} else {
				neighbours = new List<Region> (6);
				segments = new List<Segment> (6);
				Contains = PointInPolygon;
			}
		}


		public void Clear () {
			polygon = null;
			if (points != null) {
				points.Clear ();
			}
			segments.Clear ();
			neighbours.Clear ();
			rect2D.width = rect2D.height = 0;
			rect2DArea = 0;
			if (surfaceGameObject != null) {
				GameObject.DestroyImmediate (surfaceGameObject);
			}
			customMaterial = null;
			childrenSurfaces = null;
		}

        public void DestroySurface() {
            if (renderer != null) {
				GameObject.DestroyImmediate(renderer.gameObject);
				renderer = null;
            }
		}

		public Region Clone () {
			Region c = new Region (entity, isBox);
			c.customMaterial = this.customMaterial;
			c.customTextureScale = this.customTextureScale;
			c.customTextureOffset = this.customTextureOffset;
			c.customTextureRotation = this.customTextureRotation;
			c.points = new List<Vector2> (points);
			c.polygon = polygon.Clone ();
			c.segments = new List<Segment> (segments);
			return c;
		}

		public bool Intersects (Region otherRegion) {
			return otherRegion.rect2D.Overlaps (otherRegion.rect2D);
		}


		bool PointInBox (float x, float y) { 
			return x >= rect2D.xMin && x <= rect2D.xMax && y >= rect2D.yMin && y <= rect2D.yMax;
		}
			
		bool PointInPolygon (float x, float y) { 
			if (points == null)
				return false;

			if (x > rect2D.xMax || x < rect2D.xMin || y > rect2D.yMax || y < rect2D.yMin)
				return false;

			int numPoints = points.Count;
			int j = numPoints - 1; 
			bool inside = false; 
			for (int i = 0; i < numPoints; j = i++) { 
				if (((points [i].y <= y && y < points [j].y) || (points [j].y <= y && y < points [i].y)) &&
				    (x < (points [j].x - points [i].x) * (y - points [i].y) / (points [j].y - points [i].y) + points [i].x))
					inside = !inside; 
			} 
			return inside; 
		}

		public bool ContainsRegion (Region otherRegion) {
			if (!Intersects (otherRegion))
				return false;

			if (!Contains (otherRegion.rect2D.xMin, otherRegion.rect2D.yMin))
				return false;
			if (!Contains (otherRegion.rect2D.xMin, otherRegion.rect2D.yMax))
				return false;
			if (!Contains (otherRegion.rect2D.xMax, otherRegion.rect2D.yMin))
				return false;
			if (!Contains (otherRegion.rect2D.xMax, otherRegion.rect2D.yMax))
				return false;

			int opc = otherRegion.points.Count;
			for (int k = 0; k < opc; k++) {
				if (!Contains (otherRegion.points [k].x, otherRegion.points [k].y))
					return false;
			}
			return true;
		}
	}
}

