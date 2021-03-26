using UnityEngine;
using UnityEditor;
using System.Text;
using System.Collections.Generic;
using TGS;

namespace TGS_Editor {
	[CustomEditor (typeof(TerrainGridSystem))]
	public class TGSInspector : Editor {

		TerrainGridSystem tgs;
		Texture2D _headerTexture;
		string[] selectionModeOptions, topologyOptions, overlayModeOptions;
		int[] topologyOptionsValues;
		GUIStyle titleLabelStyle, infoLabelStyle;
		int cellHighlightedIndex = -1, cellTerritoryIndex, cellTextureIndex;
		List <int> cellSelectedIndices;

		Color colorSelection, cellColor;
		int textureMode, cellTag;
		static GUIStyle toggleButtonStyleNormal;
		static GUIStyle toggleButtonStyleToggled;
		SerializedProperty isDirty;
		StringBuilder sb;
		Vector2 cellSize;

		void OnEnable () {

			_headerTexture = Resources.Load<Texture2D> ("EditorHeader");

			selectionModeOptions = new string[] {
				"None",
				"Territories",
				"Cells"
			};
			overlayModeOptions = new string[] { "Overlay", "Ground" };
			topologyOptions = new string[] { "Irregular", "Box", "Hexagonal" };
			topologyOptionsValues = new int[] {
				(int)GRID_TOPOLOGY.Irregular,
				(int)GRID_TOPOLOGY.Box,
				(int)GRID_TOPOLOGY.Hexagonal
			};

			tgs = (TerrainGridSystem)target;
			if (tgs.cells == null || tgs.textures == null) {
				tgs.Init ();
			}
			sb = new StringBuilder ();
			cellSelectedIndices = new List<int> ();
			colorSelection = new Color (1, 1, 0.5f, 0.85f);
			cellColor = Color.white;
			isDirty = serializedObject.FindProperty ("isDirty");
			cellSize = tgs.cellSize;
			HideEditorMesh ();
		}

		public override void OnInspectorGUI () {

			float labelWidth = EditorGUIUtility.labelWidth;

			EditorGUILayout.Separator ();
			GUI.skin.label.alignment = TextAnchor.MiddleCenter;  
			GUILayout.Label (_headerTexture, GUILayout.ExpandWidth (true));
			GUI.skin.label.alignment = TextAnchor.MiddleLeft;  

			EditorGUILayout.BeginVertical ();

			EditorGUILayout.BeginHorizontal ();
			DrawTitleLabel ("Grid Configuration");
			GUILayout.FlexibleSpace ();
			if (GUILayout.Button ("Help")) {
				EditorUtility.DisplayDialog ("Terrain Grid System", "TGS is an advanced grid generator for Unity terrain. It can also work as a standalone 2D grid.\n\nFor a complete description of the options, please refer to the documentation guide (PDF) included in the asset.\nWe also invite you to visit and sign up on our support forum on kronnect.com where you can post your questions/requests.\n\nThanks for purchasing! Please rate Terrain Grid System on the Asset Store! Thanks.", "Close");
			}
			if (GUILayout.Button ("Redraw")) {
				tgs.Redraw (false);
                GUIUtility.ExitGUI();
			}
			if (GUILayout.Button ("Clear")) {
				if (EditorUtility.DisplayDialog ("Clear All", "Remove any color/texture from cells and territories?", "Ok", "Cancel")) {
					tgs.ClearAll ();
				}
			}
			EditorGUILayout.EndHorizontal ();

			EditorGUI.BeginChangeCheck ();
			tgs.terrainObject = (GameObject)EditorGUILayout.ObjectField ("Terrain", tgs.terrainObject, typeof(GameObject), true);
			if (EditorGUI.EndChangeCheck ()) {
				GUIUtility.ExitGUI ();
				return;
			}

			if (tgs.terrain != null) {
				if (tgs.terrain.supportsMultipleObjects) {
					tgs.terrainObjectsPrefix = EditorGUILayout.TextField (new GUIContent ("Terrain Name Prefix", "Use terrain gameobjects which has this prefix in their names."), tgs.terrainObjectsPrefix);
				}
				if (tgs.terrain.supportsCustomHeightmap) {
					tgs.heightmapSize = EditorGUILayout.IntField (new GUIContent ("Heightmap Size"), tgs.heightmapSize);
				}
			}

			tgs.gridTopology = (GRID_TOPOLOGY)EditorGUILayout.IntPopup ("Topology", (int)tgs.gridTopology, topologyOptions, topologyOptionsValues);
			bool bakedVoronoi = false;
			if (tgs.gridTopology == GRID_TOPOLOGY.Irregular) {
				EditorGUILayout.BeginHorizontal ();
				GUI.enabled = !tgs.hasBakedVoronoi && !Application.isPlaying;
				if (GUILayout.Button ("Bake Voronoi")) {
					tgs.VoronoiSerializeData ();
				}
				bakedVoronoi = tgs.hasBakedVoronoi;
				GUI.enabled = bakedVoronoi && !Application.isPlaying;
				if (GUILayout.Button ("Clear Baked Data")) {
					tgs.voronoiSerializationData = null;
				}
				GUI.enabled = true;
				EditorGUILayout.EndHorizontal ();
			}

			tgs.numTerritories = EditorGUILayout.IntSlider ("Territories", tgs.numTerritories, 1, Mathf.Min (tgs.numCells, TerrainGridSystem.MAX_TERRITORIES));

			if (bakedVoronoi) {
				EditorGUILayout.LabelField ("Cells (aprox.)", tgs.numCells.ToString ());
			} else {
				if (tgs.gridTopology == GRID_TOPOLOGY.Irregular) {
					tgs.numCells = EditorGUILayout.IntField ("Cells (aprox.)", tgs.numCells);
				} else {
					tgs.columnCount = EditorGUILayout.IntField ("Columns", tgs.columnCount);
					tgs.rowCount = EditorGUILayout.IntField ("Rows", tgs.rowCount);
				}
				if (tgs.gridTopology == GRID_TOPOLOGY.Hexagonal) {
					tgs.regularHexagons = EditorGUILayout.Toggle ("Regular Hexes", tgs.regularHexagons);
					if (tgs.regularHexagons) {
						tgs.regularHexagonsWidth = EditorGUILayout.FloatField ("   Hex Width", tgs.regularHexagonsWidth);
					}
					tgs.evenLayout = EditorGUILayout.Toggle ("Even Layout", tgs.evenLayout);
				}
			}

			if (tgs.gridTopology == GRID_TOPOLOGY.Irregular) {
				if (tgs.numCells > 10000) {
					EditorGUILayout.HelpBox ("Total cell count exceeds recommended maximum of 10.000!", MessageType.Warning);
				}
			} else if (tgs.rowCount > TerrainGridSystem.MAX_ROWS_OR_COLUMNS || tgs.columnCount > TerrainGridSystem.MAX_ROWS_OR_COLUMNS) {
				EditorGUILayout.HelpBox ("Total row or column count exceeds recommended maximum of " + TerrainGridSystem.MAX_ROWS_OR_COLUMNS + "!", MessageType.Warning);
			}

			if (!bakedVoronoi) {
				if (tgs.numCells > TerrainGridSystem.MAX_CELLS_FOR_CURVATURE) {
					EditorGUILayout.LabelField ("Curvature", "Not available with >" + TerrainGridSystem.MAX_CELLS_FOR_CURVATURE + " cells");
				} else {
					tgs.gridCurvature = EditorGUILayout.Slider ("Curvature", tgs.gridCurvature, 0, 0.1f);
				}
				if (tgs.gridTopology != GRID_TOPOLOGY.Irregular) {
					EditorGUILayout.LabelField ("Relaxation", "Only available with irregular topology");
				} else if (tgs.numCells > TerrainGridSystem.MAX_CELLS_FOR_RELAXATION) {
					EditorGUILayout.LabelField ("Relaxation", "Not available with >" + TerrainGridSystem.MAX_CELLS_FOR_RELAXATION + " cells");
				} else {
					tgs.gridRelaxation = EditorGUILayout.IntSlider ("Relaxation", tgs.gridRelaxation, 1, 32);
				}
				tgs.seed = EditorGUILayout.IntSlider ("Seed", tgs.seed, 1, 10000);
			}

			if (tgs.terrain != null) {
				tgs.gridRoughness = EditorGUILayout.Slider ("Roughness", tgs.gridRoughness, 0f, 0.2f);
				tgs.cellsMaxSlope = EditorGUILayout.Slider ("Max Slope", tgs.cellsMaxSlope, 0, 1f);
			
				EditorGUILayout.BeginHorizontal ();
				tgs.cellsMinimumAltitude = EditorGUILayout.FloatField ("Minimum Altitude", tgs.cellsMinimumAltitude);
				if (tgs.cellsMinimumAltitude == 0)
					DrawInfoLabel ("(0 = not used)");
				EditorGUILayout.EndHorizontal ();
				if (tgs.cellsMinimumAltitude != 0) {
					tgs.cellsMinimumAltitudeClampVertices = EditorGUILayout.Toggle (new GUIContent ("   Clamp Vertices", "Clamp vertices altitude to the minimum altitude."), tgs.cellsMinimumAltitudeClampVertices);
				}
			}

			tgs.gridMask = (Texture2D)EditorGUILayout.ObjectField (new GUIContent ("Mask", "Alpha channel is used to determine cell visibility (0 = cell is not visible)"), tgs.gridMask, typeof(Texture2D), true);
			if (CheckTextureImportSettings (tgs.gridMask)) {
				tgs.ReloadGridMask ();
			}

			if (tgs.gridMask != null) {
				tgs.gridMaskUseScale = EditorGUILayout.Toggle (new GUIContent ("   Use Scale", "Respects offset and scale parameters when applying mask."), tgs.gridMaskUseScale);
			}

			EditorGUILayout.BeginHorizontal ();
			tgs.territoriesTexture = (Texture2D)EditorGUILayout.ObjectField (new GUIContent ("Territories Texture", "Quickly create territories assigning a color texture in which each territory corresponds to a color."), tgs.territoriesTexture, typeof(Texture2D), true);
			if (tgs.territoriesTexture != null) {
				EditorGUILayout.EndHorizontal ();
				CheckTextureImportSettings (tgs.territoriesTexture);
				#if UNITY_2018_1_OR_NEWER
				tgs.territoriesTextureNeutralColor = EditorGUILayout.ColorField (new GUIContent ("   Neutral Color", "Color to be ignored."), tgs.territoriesTextureNeutralColor, false, false, false);
				#else
				tgs.territoriesTextureNeutralColor = EditorGUILayout.ColorField (new GUIContent ("   Neutral Color", "Color to be ignored."), tgs.territoriesTextureNeutralColor, false, false, false, null);
				#endif
				EditorGUILayout.BeginHorizontal ();
				tgs.territoriesHideNeutralCells = EditorGUILayout.Toggle (new GUIContent ("   Hide Neutral Cells", "Cells belonging to neutral territories will be invisible."), tgs.territoriesHideNeutralCells);
				EditorGUILayout.Space ();
				if (GUILayout.Button ("Generate Territories", GUILayout.Width (120))) {
					tgs.CreateTerritories (tgs.territoriesTexture, tgs.territoriesTextureNeutralColor, tgs.territoriesHideNeutralCells);
				}
			}
			EditorGUILayout.EndHorizontal ();

			int cellsCreated = tgs.cells == null ? 0 : tgs.cells.Count;
			int territoriesCreated = tgs.territories == null ? 0 : tgs.territories.Count;

			EditorGUILayout.BeginHorizontal ();
			GUILayout.FlexibleSpace ();
			DrawInfoLabel ("Cells Created: " + cellsCreated + " / Territories Created: " + territoriesCreated + " / Vertex Count: " + tgs.lastVertexCount);
			GUILayout.FlexibleSpace ();
			EditorGUILayout.EndHorizontal ();

			EditorGUILayout.EndVertical ();
			EditorGUILayout.Separator ();
			EditorGUILayout.BeginVertical ();

			DrawTitleLabel ("Grid Positioning");

			EditorGUILayout.BeginHorizontal ();
			GUILayout.Label ("Hide Objects", GUILayout.Width (EditorGUIUtility.labelWidth));
			if (tgs.terrain != null && GUILayout.Button ("Toggle Terrain")) {
				tgs.terrain.enabled = !tgs.terrain.enabled;
				tgs.transparentBackground = !tgs.terrain.enabled;
				if (tgs.transparentBackground && tgs.gridSurfaceDepthOffsetTerritory < 20) {
					tgs.gridSurfaceDepthOffsetTerritory = 20;
				} else if (!tgs.transparentBackground && tgs.gridSurfaceDepthOffsetTerritory > 0) {
					tgs.gridSurfaceDepthOffsetTerritory = -1;
				}
			}
			if (GUILayout.Button ("Toggle Grid")) {
				tgs.gameObject.SetActive (!tgs.gameObject.activeSelf);
			}
			EditorGUILayout.EndHorizontal ();

            if (tgs.terrain != null && tgs.terrain.supportsPivot) {
                tgs.terrainMeshPivot = EditorGUILayout.Vector2Field(new GUIContent("Mesh Pivot", "Specify a center correction if mesh center is not at 0,0,0"), tgs.terrainMeshPivot);
            }

            tgs.gridCenter = EditorGUILayout.Vector2Field (new GUIContent("Center", "The position of the grid center."), tgs.gridCenter);

			if (tgs.gridTopology == GRID_TOPOLOGY.Hexagonal && tgs.regularHexagons) {
				GUI.enabled = false;
			}
			tgs.gridScale = EditorGUILayout.Vector2Field ("Scale", tgs.gridScale);
			GUI.enabled = true;

			if (tgs.gridTopology == GRID_TOPOLOGY.Hexagonal && tgs.regularHexagons) {
				EditorGUILayout.BeginHorizontal ();
				EditorGUILayout.Space ();
				EditorGUILayout.HelpBox ("Scale is driven by regular hexagons option.", MessageType.Info);
				EditorGUILayout.Space ();
				EditorGUILayout.EndHorizontal ();
				EditorGUILayout.Separator ();
			} else if (tgs.gridTopology != GRID_TOPOLOGY.Irregular) {
				cellSize = EditorGUILayout.Vector2Field ("Match Cell Size", cellSize);
				EditorGUILayout.BeginHorizontal ();
				GUILayout.Label ("", GUILayout.Width (EditorGUIUtility.labelWidth));
				if (GUILayout.Button ("Update Cell Size")) {
					tgs.cellSize = cellSize;
				}
				EditorGUILayout.EndHorizontal ();
			}

			tgs.gridMeshDepthOffset = EditorGUILayout.IntField("Mesh Depth Offset", tgs.gridMeshDepthOffset);
			tgs.gridSurfaceDepthOffset = EditorGUILayout.IntField("Cells Depth Offset", tgs.gridSurfaceDepthOffset);
			tgs.gridSurfaceDepthOffsetTerritory = EditorGUILayout.IntField ("Territories Depth Offset", tgs.gridSurfaceDepthOffsetTerritory);

			if (tgs.terrain != null) {
				tgs.gridElevation = EditorGUILayout.Slider ("Elevation", tgs.gridElevation, 0f, 5f);
				tgs.gridElevationBase = EditorGUILayout.FloatField ("Elevation Base", tgs.gridElevationBase);
				tgs.gridMinElevationMultiplier = EditorGUILayout.FloatField (new GUIContent ("Min Elevation Multiplier", "Grid, cells and territories meshes are rendered with a minimum gap to preserve correct order. This value is the scale for that gap."), tgs.gridMinElevationMultiplier);
				tgs.gridCameraOffset = EditorGUILayout.Slider ("Camera Offset", tgs.gridCameraOffset, 0, 0.1f);
				tgs.gridNormalOffset = EditorGUILayout.Slider ("Normal Offset", tgs.gridNormalOffset, 0.00f, 5f);
			}

			tgs.cameraMain = (Camera)EditorGUILayout.ObjectField (new GUIContent ("Camera", "The camera used for some calculations. Main camera is picked by default."), tgs.cameraMain, typeof(Camera), true);

			EditorGUILayout.EndVertical ();
			EditorGUILayout.Separator ();
			EditorGUILayout.BeginVertical ();

			DrawTitleLabel ("Grid Appearance");

			tgs.showTerritories = EditorGUILayout.Toggle ("Show Territories", tgs.showTerritories);
			EditorGUI.indentLevel++;

			tgs.territoryFrontiersColor = EditorGUILayout.ColorField ("Frontier Color", tgs.territoryFrontiersColor);
			tgs.territoryFrontiersThickness = EditorGUILayout.Slider ("Thickness", tgs.territoryFrontiersThickness, 1f, 15f);

			tgs.territoryHighlightColor = EditorGUILayout.ColorField ("Highlight Color", tgs.territoryHighlightColor);
			tgs.territoryDisputedFrontierColor = EditorGUILayout.ColorField (new GUIContent ("Disputed Frontier", "Color for common frontiers between two territories."), tgs.territoryDisputedFrontierColor);

			tgs.colorizeTerritories = EditorGUILayout.Toggle ("Colorize Territories", tgs.colorizeTerritories);
			tgs.colorizedTerritoriesAlpha = EditorGUILayout.Slider ("Alpha", tgs.colorizedTerritoriesAlpha, 0.0f, 1.0f);

			tgs.showTerritoriesOuterBorders = EditorGUILayout.Toggle ("Outer Borders", tgs.showTerritoriesOuterBorders);
			tgs.allowTerritoriesInsideTerritories = EditorGUILayout.Toggle (new GUIContent ("Internal Territories", "Allows territories to be contained by other territories."), tgs.allowTerritoriesInsideTerritories);
			EditorGUI.indentLevel--;

			tgs.showCells = EditorGUILayout.Toggle ("Show Cells", tgs.showCells);
			EditorGUI.indentLevel++;
			if (tgs.showCells) {
				tgs.cellBorderColor = EditorGUILayout.ColorField ("Border Color", tgs.cellBorderColor);
				tgs.cellBorderThickness = EditorGUILayout.Slider ("Thickness", tgs.cellBorderThickness, 1f, 10f);
			}
			tgs.cellHighlightColor = EditorGUILayout.ColorField ("Highlight Color", tgs.cellHighlightColor);
			EditorGUI.indentLevel--;
			float highlightFadeMin = tgs.highlightFadeMin;
			float highlightFadeAmount = tgs.highlightFadeAmount;
			EditorGUILayout.MinMaxSlider ("Highlight Fade", ref highlightFadeMin, ref highlightFadeAmount, 0.0f, 1.0f);
			EditorGUI.indentLevel++;

			tgs.highlightFadeMin = highlightFadeMin;
			tgs.highlightFadeAmount = highlightFadeAmount;

			tgs.highlightFadeSpeed = EditorGUILayout.Slider ("Highlight Speed", tgs.highlightFadeSpeed, 0.1f, 5.0f);
			tgs.highlightEffect = (HIGHLIGHT_EFFECT)EditorGUILayout.EnumPopup ("Highlight Effect", tgs.highlightEffect);

			if (tgs.highlightEffect == HIGHLIGHT_EFFECT.TextureScale) {
				EditorGUILayout.BeginHorizontal ();
				float highlightScaleMin = tgs.highlightScaleMin;
				float highlightScaleMax = tgs.highlightScaleMax;
				EditorGUILayout.MinMaxSlider ("      Scale Range", ref highlightScaleMin, ref highlightScaleMax, 0.0f, 2.0f);
				if (GUILayout.Button ("Default", GUILayout.Width (60))) {
					highlightScaleMin = 0.75f;
					highlightScaleMax = 1.1f;
				}
				tgs.highlightScaleMin = highlightScaleMin;
				tgs.highlightScaleMax = highlightScaleMax;
				EditorGUILayout.EndHorizontal ();
			} else if (tgs.highlightEffect == HIGHLIGHT_EFFECT.DualColors) {
				tgs.cellHighlightColor2 = EditorGUILayout.ColorField ("Cell Alternate Color", tgs.cellHighlightColor2);
				tgs.territoryHighlightColor2 = EditorGUILayout.ColorField ("Territory Alternate Color", tgs.territoryHighlightColor2);
			}
			EditorGUI.indentLevel--;

			if (tgs.terrain != null) {
				tgs.nearClipFadeEnabled = EditorGUILayout.Toggle (new GUIContent ("Near Clip Fade", "Fades out the cell and territories lines near to the camera."), tgs.nearClipFadeEnabled);
				if (tgs.nearClipFadeEnabled) {
					tgs.nearClipFade = EditorGUILayout.FloatField ("   Distance", tgs.nearClipFade);
					tgs.nearClipFadeFallOff = EditorGUILayout.FloatField ("   FallOff", tgs.nearClipFadeFallOff);
				}
				tgs.farFadeEnabled = EditorGUILayout.Toggle(new GUIContent("Far Distance Fade", "Fades out the cell and territories lines far from the camera."), tgs.farFadeEnabled);
				if (tgs.farFadeEnabled) {
					tgs.farFadeDistance = EditorGUILayout.FloatField("   Distance", tgs.farFadeDistance);
					tgs.farFadeFallOff = EditorGUILayout.FloatField("   FallOff", tgs.farFadeFallOff);
				}
			}

			tgs.useGeometryShaders = EditorGUILayout.Toggle(new GUIContent("Use Geometry Shaders", "Use geometry shaders if platform supports them."), tgs.useGeometryShaders);
			tgs.transparentBackground = EditorGUILayout.Toggle ("No Background", tgs.transparentBackground);

            if (tgs.transparentBackground) {
                tgs.sortingOrder = EditorGUILayout.IntField("Sorting Order", tgs.sortingOrder);
            }

            tgs.canvasTexture = (Texture2D)EditorGUILayout.ObjectField ("Canvas Texture", tgs.canvasTexture, typeof(Texture2D), true);

			EditorGUILayout.EndVertical ();
			EditorGUILayout.Separator ();
			EditorGUILayout.BeginVertical ();
				
			DrawTitleLabel ("Grid Behaviour");

			tgs.highlightMode = (HIGHLIGHT_MODE)EditorGUILayout.Popup ("Selection Mode", (int)tgs.highlightMode, selectionModeOptions);
			EditorGUI.indentLevel++;
			tgs.cellHighlightNonVisible = EditorGUILayout.Toggle ("Include Invisible Cells", tgs.cellHighlightNonVisible);
			tgs.highlightMinimumTerrainDistance = EditorGUILayout.FloatField (new GUIContent ("Minimum Distance", "Minimum distance of cell/territory to camera to be selectable. Useful in first person view to prevent selecting cells already under character."), tgs.highlightMinimumTerrainDistance);
			tgs.allowHighlightWhileDragging = EditorGUILayout.Toggle (new GUIContent ("Highlight While Drag", "Allows highlight while dragging."), tgs.allowHighlightWhileDragging);
			EditorGUI.indentLevel--;

			tgs.overlayMode = (OVERLAY_MODE)EditorGUILayout.Popup ("Overlay Mode", (int)tgs.overlayMode, overlayModeOptions);
			tgs.respectOtherUI = EditorGUILayout.Toggle ("Respect Other UI", tgs.respectOtherUI);

			EditorGUILayout.EndVertical ();
			EditorGUILayout.Separator ();
			EditorGUILayout.BeginVertical ();
			
			DrawTitleLabel ("Path Finding");
			
			tgs.pathFindingHeuristicFormula = (TGS.PathFinding.HeuristicFormula)EditorGUILayout.EnumPopup ("Algorithm", tgs.pathFindingHeuristicFormula);
			tgs.pathFindingMaxCost = EditorGUILayout.FloatField ("Max Search Cost", tgs.pathFindingMaxCost);
			tgs.pathFindingMaxSteps = EditorGUILayout.IntField ("Max Steps", tgs.pathFindingMaxSteps);

			if (tgs.gridTopology == GRID_TOPOLOGY.Box) {
				tgs.pathFindingUseDiagonals = EditorGUILayout.Toggle ("Use Diagonals", tgs.pathFindingUseDiagonals);
				EditorGUI.indentLevel++;
				tgs.pathFindingHeavyDiagonalsCost = EditorGUILayout.FloatField ("Diagonals Cost", tgs.pathFindingHeavyDiagonalsCost);
				EditorGUI.indentLevel--;
			}

			EditorGUILayout.EndVertical ();
			EditorGUILayout.Separator ();

			if (!Application.isPlaying) {
				EditorGUILayout.BeginVertical ();
				EditorGUILayout.BeginHorizontal ();
				DrawTitleLabel ("Grid Editor");
				GUILayout.FlexibleSpace ();
				if (GUILayout.Button ("Export Grid Config")) {
					if (EditorUtility.DisplayDialog ("Export Grid Config", "A TGS Config component will be atteched to this game object with current cell settings. You can restore this configuration just enabling this new component.\nContinue?", "Ok", "Cancel")) {
						ExportGridConfig ();
					}
				}
				if (GUILayout.Button ("Export Grid Mesh")) {
					if (EditorUtility.DisplayDialog ("Export Grid Mesh", "A copy of the grid mesh will be created and assigned to a new gameobject. This operation does not modify current grid.\nContinue?", "Ok", "Cancel")) {
						tgs.ExportTerritoryMesh (cellTerritoryIndex);
					}
				}
				if (GUILayout.Button ("Reset")) {
					if (EditorUtility.DisplayDialog ("Reset Grid", "Reset cells to their default values?", "Ok", "Cancel")) {
						ResetCells ();
						GUIUtility.ExitGUI ();
						return;
					}
				}
				EditorGUILayout.EndHorizontal ();

				tgs.enableGridEditor = EditorGUILayout.Toggle (new GUIContent ("Enable Editor", "Enables grid editing options in Scene View"), tgs.enableGridEditor);

				if (tgs.enableGridEditor) {
					int selectedCount = cellSelectedIndices.Count;
					if (selectedCount == 0) {
						GUILayout.Label ("Click on a cell in Scene View to edit its properties\n(use Control or Shift to select multiple cells)");
					} else {
						// Check that all selected cells are within range
						for (int k = 0; k < selectedCount; k++) {
							if (cellSelectedIndices [k] < 0 || cellSelectedIndices [k] >= tgs.cellCount) {
								cellSelectedIndices.Clear ();
                                GUIUtility.ExitGUI();
								return;
							}
						}
					
						int cellSelectedIndex = cellSelectedIndices [0];
						EditorGUILayout.BeginHorizontal ();
						if (selectedCount == 1) {
							EditorGUILayout.LabelField ("Selected Cell", cellSelectedIndex.ToString ());
						} else {
							sb.Length = 0;
							for (int k = 0; k < selectedCount; k++) {
								if (k > 0) {
									sb.Append (", ");
								}
								sb.Append (cellSelectedIndices [k].ToString ());
							}
							if (selectedCount > 5) {
								EditorGUILayout.LabelField ("Selected Cells");
								GUILayout.TextArea (sb.ToString (), GUILayout.ExpandHeight (true));
							} else {
								EditorGUILayout.LabelField ("Selected Cells", sb.ToString ());
							}
						}
						EditorGUILayout.EndHorizontal ();
			
						bool needsRedraw = false;

						Cell selectedCell = tgs.cells [cellSelectedIndex];
						bool cellVisible = selectedCell.visible;
						selectedCell.visible = EditorGUILayout.Toggle ("   Visible", cellVisible);
						if (selectedCell.visible != cellVisible) {
							for (int k = 0; k < selectedCount; k++) {
								tgs.cells [cellSelectedIndices [k]].visible = selectedCell.visible;
							}
							needsRedraw = true;
						}

						bool canCross = selectedCell.canCross;
						selectedCell.canCross = EditorGUILayout.Toggle (new GUIContent("   Can Cross", "This cell can be crossed when calculating a route using path finding."), selectedCell.canCross);
                        if (selectedCell.canCross != canCross) {
							for (int k = 0; k < selectedCount; k++) {
								tgs.cells[cellSelectedIndices[k]].canCross = selectedCell.canCross;
							}
						}

                        if (selectedCount == 1) {
                            EditorGUILayout.BeginHorizontal();
                            cellTag = EditorGUILayout.IntField("   Tag", cellTag);
                            if (cellTag == selectedCell.tag)
                                GUI.enabled = false;
                            if (GUILayout.Button("Set Tag", GUILayout.Width(60))) {
                                tgs.CellSetTag(cellSelectedIndex, cellTag);
                            }
                            EditorGUILayout.EndHorizontal();
                        }
						GUI.enabled = true;
						EditorGUILayout.BeginHorizontal ();
						cellTerritoryIndex = EditorGUILayout.IntField ("   Territory Index", cellTerritoryIndex);
						if (cellTerritoryIndex == selectedCell.territoryIndex)
							GUI.enabled = false;
						if (GUILayout.Button ("Set Territory", GUILayout.Width (100))) {
							for (int k = 0; k < selectedCount; k++) {
								tgs.CellSetTerritory (cellSelectedIndices [k], cellTerritoryIndex);
							}
							needsRedraw = true;
						}
						EditorGUILayout.EndHorizontal ();
						GUI.enabled = true;

						cellColor = EditorGUILayout.ColorField ("   Color", cellColor);
						cellTextureIndex = EditorGUILayout.IntField ("   Texture", cellTextureIndex);
						if (tgs.CellGetColor (cellSelectedIndex) == cellColor && tgs.CellGetTextureIndex (cellSelectedIndex) == cellTextureIndex)
							GUI.enabled = false;
						EditorGUILayout.BeginHorizontal ();
						GUILayout.Label ("", GUILayout.Width (labelWidth));
						if (GUILayout.Button ("Assign Color & Texture")) {
							for (int k = 0; k < selectedCount; k++) {
								GameObject o = tgs.CellToggleRegionSurface (cellSelectedIndices [k], true, cellColor, true, cellTextureIndex);
								o.transform.parent.gameObject.hideFlags = 0;
								o.hideFlags = 0;
							}
							needsRedraw = true;
						}
						GUI.enabled = true;
						if (GUILayout.Button ("Clear Cell")) {
							for (int k = 0; k < selectedCount; k++) {
								tgs.CellHideRegionSurface (cellSelectedIndices [k]);
							}
							needsRedraw = true;
						}
						EditorGUILayout.EndHorizontal ();


						if (needsRedraw) {
							RefreshGrid ();
							GUIUtility.ExitGUI ();
							return;
						}
					}

					GUILayout.Label ("Textures", GUILayout.Width (labelWidth));

					if (toggleButtonStyleNormal == null) {
						toggleButtonStyleNormal = "Button";
						toggleButtonStyleToggled = new GUIStyle (toggleButtonStyleNormal);
						toggleButtonStyleToggled.normal.background = toggleButtonStyleToggled.active.background;
					}

					if (tgs.textures != null) {
						int textureMax = tgs.textures.Length - 1;
						while (textureMax >= 1 && tgs.textures [textureMax] == null) {
							textureMax--;
						}
						textureMax++;
						if (textureMax >= tgs.textures.Length)
							textureMax = tgs.textures.Length - 1;

						for (int k = 1; k <= textureMax; k++) {
							EditorGUILayout.BeginHorizontal ();
							GUILayout.Label ("   " + k.ToString (), GUILayout.Width (40));
							tgs.textures [k] = (Texture2D)EditorGUILayout.ObjectField (tgs.textures [k], typeof(Texture2D), false);
							if (tgs.textures [k] != null) {
								if (GUILayout.Button (new GUIContent ("T", "Texture mode - if enabled, you can paint several cells just clicking over them."), textureMode == k ? toggleButtonStyleToggled : toggleButtonStyleNormal, GUILayout.Width (20))) {
									textureMode = textureMode == k ? 0 : k;
								}
								if (GUILayout.Button (new GUIContent ("X", "Remove texture"), GUILayout.Width (20))) {
									if (EditorUtility.DisplayDialog ("Remove texture", "Are you sure you want to remove this texture?", "Yes", "No")) {
										tgs.textures [k] = null;
										GUIUtility.ExitGUI ();
										return;
									}
								}
							}
							EditorGUILayout.EndHorizontal ();
						}
					}
				}

				EditorGUILayout.EndVertical ();
			}
			EditorGUILayout.Separator ();

			if (tgs.isDirty) {
				#if UNITY_5_6_OR_NEWER
				serializedObject.UpdateIfRequiredOrScript ();
				#else
				serializedObject.UpdateIfDirtyOrScript ();
				#endif
				if (isDirty == null)
					OnEnable ();
				isDirty.boolValue = false;
				serializedObject.ApplyModifiedProperties ();
				EditorUtility.SetDirty (target);

				// Hide mesh in Editor
				HideEditorMesh ();

				SceneView.RepaintAll ();
			}
		}

		void OnSceneGUI () {
			if (tgs == null || Application.isPlaying || !tgs.enableGridEditor)
				return;
			if (tgs.terrain != null) {
				// prevents terrain from being selected
				HandleUtility.AddDefaultControl (GUIUtility.GetControlID (FocusType.Passive));
			}
			Event e = Event.current;
			bool gridHit = tgs.CheckRay (HandleUtility.GUIPointToWorldRay (e.mousePosition));
			if (cellHighlightedIndex != tgs.cellHighlightedIndex) {
				cellHighlightedIndex = tgs.cellHighlightedIndex;
				SceneView.RepaintAll ();
			}
			int controlID = GUIUtility.GetControlID (FocusType.Passive);
			EventType eventType = e.GetTypeForControl (controlID);
			if ((eventType == EventType.MouseDown && e.button == 0) || (eventType == EventType.MouseMove && e.shift)) {
				if (gridHit) {
					e.Use ();
				}
				if (cellHighlightedIndex < 0) {
					return;
				}
				if (!e.shift && cellSelectedIndices.Contains (cellHighlightedIndex)) {
					cellSelectedIndices.Remove (cellHighlightedIndex);
				} else {
					if (!e.shift || (e.shift && !cellSelectedIndices.Contains (cellHighlightedIndex))) {
						if (!e.shift && !e.control) {
							cellSelectedIndices.Clear ();
						}
						cellSelectedIndices.Add (cellHighlightedIndex);
						if (textureMode > 0) {
							tgs.CellToggleRegionSurface (cellHighlightedIndex, true, Color.white, true, textureMode);
							SceneView.RepaintAll ();
						}
						if (cellHighlightedIndex >= 0) {
							cellTerritoryIndex = tgs.CellGetTerritoryIndex (cellHighlightedIndex);
							cellColor = tgs.CellGetColor (cellHighlightedIndex);
							if (cellColor.a == 0)
								cellColor = Color.white;
							cellTextureIndex = tgs.CellGetTextureIndex (cellHighlightedIndex);
							cellTag = tgs.CellGetTag (cellHighlightedIndex);
						}
					}
				}
				EditorUtility.SetDirty (target);
			}
			int count = cellSelectedIndices.Count;
			for (int k = 0; k < count; k++) {
				int index = cellSelectedIndices [k];
				Vector3 pos = tgs.CellGetPosition (index);
				Handles.color = colorSelection;
				// Handle size
				Rect rect = tgs.CellGetRect (index);
				Vector3 min = tgs.transform.TransformPoint (rect.min);
				Vector3 max = tgs.transform.TransformPoint (rect.max);
				float dia = Vector3.Distance (min, max);
				float handleSize = dia * 0.05f;
				Handles.DrawSolidDisc (pos, tgs.transform.forward, handleSize);
			}
		}

		#region Utility functions

		void HideEditorMesh () {
			Renderer[] rr = tgs.GetComponentsInChildren<Renderer> (true);
			for (int k = 0; k < rr.Length; k++) {
				#if UNITY_5_5_OR_NEWER
				EditorUtility.SetSelectedRenderState (rr [k], EditorSelectedRenderState.Hidden);
				#else
				EditorUtility.SetSelectedWireframeHidden (rr [k], true);
				#endif			
			}
		}


		void DrawTitleLabel (string s) {
			if (titleLabelStyle == null)
				titleLabelStyle = new GUIStyle (GUI.skin.label);
			titleLabelStyle.normal.textColor = EditorGUIUtility.isProSkin ? new Color (0.52f, 0.66f, 0.9f) : new Color (0.22f, 0.36f, 0.6f);
			titleLabelStyle.fontStyle = FontStyle.Bold;
			GUILayout.Label (s, titleLabelStyle);
		}

		void DrawInfoLabel (string s) {
			if (infoLabelStyle == null)
				infoLabelStyle = new GUIStyle (GUI.skin.label);
			infoLabelStyle.normal.textColor = new Color (0.76f, 0.52f, 0.52f);
			GUILayout.Label (s, infoLabelStyle);
		}

		void ResetCells () {
			TGSConfig[] cc = tgs.GetComponents<TGSConfig> ();
			for (int k = 0; k < cc.Length; k++) {
				cc [k].enabled = false;
			}
			cellSelectedIndices.Clear ();
			cellColor = Color.white;
			tgs.GenerateMap ();
			RefreshGrid ();
		}

		void RefreshGrid () {
			tgs.Redraw ();
			HideEditorMesh ();
			EditorUtility.SetDirty (target);
			SceneView.RepaintAll ();
		}

		void ExportGridConfig () {
			TGSConfig configComponent = tgs.gameObject.AddComponent<TGSConfig> ();
			configComponent.SaveConfiguration (tgs);
			configComponent.enabled = false;
		}

		bool CheckTextureImportSettings (Texture2D tex) {
			if (tex == null)
				return false;
			string path = AssetDatabase.GetAssetPath (tex);
			if (string.IsNullOrEmpty (path))
				return false;
			TextureImporter imp = (TextureImporter)AssetImporter.GetAtPath (path);
			if (imp != null && !imp.isReadable) {
				EditorGUILayout.HelpBox ("Texture is not readable. Fix it?", MessageType.Warning);
				if (GUILayout.Button ("Fix texture import setting")) {
					imp.isReadable = true;
					imp.SaveAndReimport ();
					return true;
				}
			}
			return false;
		}

		#endregion

		#region Editor integration

		[MenuItem ("GameObject/3D Object/Terrain Grid System", false)]
		static void CreateTGSMenuOption (MenuCommand menuCommand) {
			GameObject go = Instantiate<GameObject> (Resources.Load<GameObject> ("Prefabs/TerrainGridSystem"));
			go.name = "Terrain Grid System";
			Undo.RegisterCreatedObjectUndo (go, "Create " + go.name);
			Selection.activeObject = go;

			if (Terrain.activeTerrain != null) {
				TerrainGridSystem tgs = go.GetComponent<TerrainGridSystem> ();
				if (tgs != null && Terrain.activeTerrain != null) {
					tgs.terrainObject = Terrain.activeTerrain.gameObject;
				}
			} else {
				go.transform.rotation = Quaternion.Euler (90, 0, 0);
			}

		}


		#endregion
	}

}