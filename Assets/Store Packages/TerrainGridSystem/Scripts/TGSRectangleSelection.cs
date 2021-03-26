using UnityEngine;
using System;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using TGS.Geom;
using TGS.PathFinding;

namespace TGS {
	
	/* Event definitions */
	public delegate void OnRectangleSelectionEvent (TerrainGridSystem sender, Vector2 localStartPos, Vector2 localEndPos);


	public partial class TerrainGridSystem : MonoBehaviour {

		[SerializeField]
		bool _enableRectangleSelection;

		public bool enableRectangleSelection {
			get { return _enableRectangleSelection; }
			set {
				if (_enableRectangleSelection != value) {
					_enableRectangleSelection = value; 
					CheckRectangleSelectionObject ();
				}
			}
		}


		public OnRectangleSelectionEvent OnRectangleSelection;


	
	}
}

