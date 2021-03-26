//#define SHOW_DEBUG_GIZMOS
using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using TGS.Geom;
using TGS.Poly2Tri;


namespace TGS {
    [ExecuteInEditMode]
    [Serializable]
    public partial class TerrainGridSystem : MonoBehaviour {

        enum SurfaceType {
            Cell = 0,
            Territory = 1
        }

        enum RedrawType {
            None = 0,
            Full = 1,
            IncrementalTerritories = 2
        }

        // internal fields
        const double MIN_VERTEX_DISTANCE = 0.002;
        const double SQR_MIN_VERTEX_DIST = 0.0002 * 0.0002;
        const string SKW_NEAR_CLIP_FADE = "NEAR_CLIP_FADE";
        const string SKW_FAR_FADE = "FAR_FADE";
        const string SKW_TEX_HIGHLIGHT_ADDITIVE = "TGS_TEX_HIGHLIGHT_ADDITIVE";
        const string SKW_TEX_HIGHLIGHT_MULTIPLY = "TGS_TEX_HIGHLIGHT_MULTIPLY";
        const string SKW_TEX_HIGHLIGHT_COLOR = "TGS_TEX_HIGHLIGHT_COLOR";
        const string SKW_TEX_HIGHLIGHT_SCALE = "TGS_TEX_HIGHLIGHT_SCALE";
        const string SKW_TEX_DUAL_COLORS = "TGS_TEX_DUAL_COLORS";
        const string SKW_TRANSPARENT = "TGS_TRANSPARENT";
        public const int MAX_ROWS_OR_COLUMNS = 1000;

        Rect canvasRect = new Rect(-0.5f, -0.5f, 1, 1);
        int[] hexIndices = new int[] { 0, 5, 1, 1, 5, 2, 5, 4, 2, 2, 4, 3 };
        int[] quadIndices = new int[] { 0, 2, 1, 0, 3, 2 };

        // Custom inspector stuff
        public const int MAX_TERRITORIES = 512;
        public int maxCellsSqrt = 100;
        public bool isDirty;
        public const int MAX_CELLS_FOR_CURVATURE = 500;
        public const int MAX_CELLS_FOR_RELAXATION = 1000;

        // Materials and resources
        Material territoriesThinMat, territoriesGeoMat;
        Material cellsThinMat, cellsGeoMat;
        Material hudMatTerritoryOverlay, hudMatTerritoryGround, hudMatCellOverlay, hudMatCellGround;
        Material territoriesDisputedThinMat, territoriesDisputedGeoMat;
        Material coloredMatGroundCell, coloredMatOverlayCell;
        Material coloredMatGroundTerritory, coloredMatOverlayTerritory;
        Material texturizedMatGroundCell, texturizedMatOverlayCell;
        Material texturizedMatGroundTerritory, texturizedMatOverlayTerritory;
        Material cellLineMat;

        // Cell mesh data
        const string CELLS_LAYER_NAME = "Cells";
        Vector3[][] cellMeshBorders;
        int[][] cellMeshIndices;
        //		Dictionary<Segment,Region> cellNeighbourHit;
        float meshStep;
        bool recreateCells, recreateTerritories;
        Dictionary<int, Cell> cellTagged;
        bool needUpdateTerritories;

        // Territory mesh data
        const string TERRITORIES_LAYER_NAME = "Territories";
        Dictionary<Segment, Frontier> territoryNeighbourHit;
        Frontier[] frontierPool;
        List<Segment> territoryFrontiers;
        List<Territory> _sortedTerritories;
        List<TerritoryMesh> territoryMeshes;
        Connector[] territoryConnectors;

        // Common territory & cell structures
        Vector3[] frontiersPoints;
        int frontiersPointsCount;
        Dictionary<Segment, bool> segmentHit;
        List<TriangulationPoint> steinerPoints;
        PolygonPoint[] tempPolyPoints;
        PolygonPoint[] tempPolyPoints2;
        Dictionary<TriangulationPoint, int> surfaceMeshHit;
        Connector tempConnector = new Connector();
        List<Vector3> meshPoints;
        int[] triNew;
        int triNewIndex;
        int newPointsCount;
        List<Vector3> tempPoints;
        List<Vector4> tempUVs;
        List<int> tempIndices;
        bool canUseGeometryShaders;

        // Terrain data
        float[,] terrainHeights;
        float[] terrainRoughnessMap;
        float[] tempTerrainRoughnessMap;
        int terrainRoughnessMapWidth, terrainRoughnessMapHeight;
        float[] acumY;
        int heightMapWidth, heightMapHeight;
        const int TERRAIN_CHUNK_SIZE = 8;
        float maxRoughnessWorldSpace;

        // Placeholders and layers
        GameObject territoryLayer;
        GameObject _surfacesLayer;

        GameObject surfacesLayer {
            get {
                if (_surfacesLayer == null)
                    CreateSurfacesLayer();
                return _surfacesLayer;
            }
        }

        GameObject _highlightedObj;
        GameObject cellLayer;

        // Caches
        Dictionary<int, GameObject> surfaces;
        Dictionary<Territory, int> _territoryLookup;
        int lastTerritoryLookupCount = -1;
        Dictionary<Color, Material> coloredMatCacheGroundCell, coloredMatCacheGroundTerritory;
        Dictionary<Color, Material> coloredMatCacheOverlayCell, coloredMatCacheOverlayTerritory;
        Dictionary<Color, Material> frontierColorCache;
        Color[] factoryColors;
        bool refreshCellMesh, refreshTerritoriesMesh;
        List<Cell> sortedCells;
        bool needResortCells;
        bool needGenerateMap;
        RedrawType issueRedraw;
        DisposalManager disposalManager;
        List<int> tempListCells;
        int cellIteration;
        Dictionary<Point, Segment> dictSegments;
        bool gridNeedsUpdate;
        bool applyingChanges, redrawing;

        // Z-Figther & LOD
        Vector3 lastCamPos, lastPos, lastScale, lastParentScale;
        float lastGridElevation, lastGridCameraOffset, lastGridMinElevationMultiplier;
        float terrainWidth;
        float terrainHeight;
        float terrainDepth;

        // Interaction
        static TerrainGridSystem _instance;
        public bool mouseIsOver;
        Territory _territoryHighlighted;
        int _territoryHighlightedIndex = -1;
        Cell _cellHighlighted;
        int _cellHighlightedIndex = -1;
        float highlightFadeStart;
        int _territoryLastClickedIndex = -1, _cellLastClickedIndex = -1;
        int _territoryLastOverIndex = -1, _cellLastOverIndex = -1;
        Territory _territoryLastOver;
        Cell _cellLastOver;
        bool canInteract;
        List<string> highlightKeywords;
        RaycastHit[] hits;

        // Misc
        int _lastVertexCount = 0;
        Color[] mask;
        bool useEditorRay;
        Ray editorRay;
        List<SurfaceFader> tempFaders;

        [SerializeField, HideInInspector] bool newInHierarchy = true;

        public int lastVertexCount { get { return _lastVertexCount; } }


        delegate int GridDistanceFunction(int cellStartIndex, int cellEndIndex);


        bool territoriesAreUsed {
            get { return (_showTerritories || _colorizeTerritories || _highlightMode == HIGHLIGHT_MODE.Territories); }
        }

        List<Territory> sortedTerritories {
            get {
                if (_sortedTerritories.Count != territories.Count) {
                    _sortedTerritories.AddRange(territories);
                    _sortedTerritories.Sort(delegate (Territory x, Territory y) {
                        return x.region.rect2DArea.CompareTo(y.region.rect2DArea);
                    });
                }
                return _sortedTerritories;
            }
            set {
                _sortedTerritories = value;
            }
        }

        Dictionary<Territory, int> territoryLookup {
            get {
                if (_territoryLookup != null && territories.Count == lastTerritoryLookupCount)
                    return _territoryLookup;
                if (_territoryLookup == null) {
                    _territoryLookup = new Dictionary<Territory, int>();
                } else {
                    _territoryLookup.Clear();
                }
                int terrCount = territories.Count;
                for (int k = 0; k < terrCount; k++) {
                    _territoryLookup.Add(territories[k], k);
                }
                lastTerritoryLookupCount = territories.Count;
                return _territoryLookup;
            }
        }

        Material cellsMat {
            get {
                if (_cellBorderThickness > 1)
                    return cellsGeoMat;
                else
                    return cellsThinMat;
            }
        }

        Material territoriesMat {
            get {
                if (_territoryFrontiersThickness > 1)
                    return territoriesGeoMat;
                else
                    return territoriesThinMat;
            }
        }

        Material territoriesDisputedMat {
            get {
                if (_territoryFrontiersThickness > 1)
                    return territoriesDisputedGeoMat;
                else
                    return territoriesDisputedThinMat;
            }
        }

        void SetTerrain(GameObject terrain) {
            if (terrain == null) {
                terrainObject = null;
            } else {
                terrainObject = terrain;
            }
        }


        #region Gameloop events

        public void OnEnable() {

            // Use heuristic to determine if TGS should be reparented to this object automatically. Do it if we detect the object mesh is different to the basic quad TGS includes
            if (newInHierarchy) {
                newInHierarchy = false;
                bool isTerrain = GetComponent<MeshRenderer>() == null;
                if (!isTerrain) {
                    MeshFilter mf = GetComponent<MeshFilter>();
                    if (mf == null || mf.sharedMesh == null || mf.sharedMesh.vertexCount != 4) {
                        isTerrain = true;
                    }
                }
                if (isTerrain) {
                    GameObject terrain = TerrainWrapperProvider.GetSuitableTerrain(gameObject);
                    if (terrain != null) {
                        // Check if user attached TGS directly to the terrain
                        try {
                            GameObject obj = Instantiate(Resources.Load<GameObject>("Prefabs/TerrainGridSystem")) as GameObject;
                            obj.name = "TerrainGridSystem";
                            TerrainGridSystem tgs = obj.GetComponent<TerrainGridSystem>();
                            tgs.SetTerrain(gameObject);
#if UNITY_EDITOR
                            UnityEditor.Selection.activeGameObject = tgs.gameObject;
#endif
                        } catch (Exception ex) {
                            Debug.LogError("Unexpected error while attaching Terrain Grid System: " + ex.ToString());
                        }
                        DestroyImmediate(this);
                    }
                    return;
                }
            }

            // Migration from hexSize
            if (_regularHexagonsWidth == 0) {
                _regularHexagonsWidth = _hexSize * transform.lossyScale.x;
            }

            if (cameraMain == null) {
                cameraMain = Camera.main;
                if (cameraMain == null) {
                    Camera[] cams = FindObjectsOfType<Camera>();
                    for (int k = 0; k < cams.Length; k++) {
                        if (cams[k].isActiveAndEnabled) {
                            cameraMain = cams[k];
                            break;
                        }
                    }
                }
            }
            if (cameraMain == null) {
                Debug.LogError("No cameras found in scene!");
            } else {
                if ((cameraMain.cullingMask & (1 << gameObject.layer)) == 0) {
                    Debug.LogWarning("Camera is culling Terrain Grid System objects! Check the layer of Terrain Grid System and the culling mask of the camera.");
                }
            }
            if (cells == null) {
                Init();
            }
            if (hudMatTerritoryOverlay != null && hudMatTerritoryOverlay.color != _territoryHighlightColor) {
                hudMatTerritoryOverlay.color = _territoryHighlightColor;
            }
            hudMatTerritoryOverlay.SetColor("_Color2", _territoryHighlightColor2);
            if (hudMatTerritoryGround != null && hudMatTerritoryGround.color != _territoryHighlightColor) {
                hudMatTerritoryGround.color = _territoryHighlightColor;
            }
            hudMatTerritoryGround.SetColor("_Color2", _territoryHighlightColor2);
            if (hudMatCellOverlay != null && hudMatCellOverlay.color != _cellHighlightColor) {
                hudMatCellOverlay.color = _cellHighlightColor;
            }
            hudMatCellOverlay.SetColor("_Color2", _cellHighlightColor2);
            if (hudMatCellGround != null && hudMatCellGround.color != _cellHighlightColor) {
                hudMatCellGround.color = _cellHighlightColor;
            }
            hudMatCellGround.SetColor("_Color2", _cellHighlightColor2);
            if (territoriesThinMat != null && territoriesThinMat.color != _territoryFrontierColor) {
                territoriesThinMat.color = _territoryFrontierColor;
            }
            if (_territoryDisputedFrontierColor == new Color(0, 0, 0, 0)) {
                _territoryDisputedFrontierColor = _territoryFrontierColor;
            }
            if (territoriesDisputedThinMat != null && territoriesDisputedThinMat.color != _territoryDisputedFrontierColor) {
                territoriesDisputedThinMat.color = _territoryDisputedFrontierColor;
            }
            if (territoriesDisputedGeoMat != null && territoriesDisputedGeoMat.color != _territoryDisputedFrontierColor) {
                territoriesDisputedGeoMat.color = _territoryDisputedFrontierColor;
            }
            if (cellsThinMat != null && cellsThinMat.color != _cellBorderColor) {
                cellsThinMat.color = _cellBorderColor;
            }
        }


        void OnDestroy() {
            if (_terrainWrapper != null) {
                _terrainWrapper.Dispose();
            }
            if (disposalManager != null) {
                disposalManager.DisposeAll();
            }
            if (rectangleSelection != null) {
                DestroyImmediate(rectangleSelection.gameObject);
            }
        }

        void CheckChanges() {
            if (applyingChanges) return;
            applyingChanges = true;

            if (needGenerateMap) {
                GenerateMap(true);
            }

            if (needResortCells) {
                ResortCells();
            }

            FitToTerrain();         // Verify if there're changes in container and adjust the grid mesh accordingly

            if (issueRedraw != RedrawType.None) {
                Redraw(issueRedraw == RedrawType.IncrementalTerritories);
            }

            applyingChanges = false;
        }

        void LateUpdate() {

            CheckChanges();

            if (Application.isMobilePlatform && !_allowHighlightWhileDragging) {
                if (Input.touchCount != 1)
                    return;
            }
            bool startPressing = Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began;

            // Check whether the points is on an UI element, then avoid user interaction
            if (respectOtherUI) {
                if (!canInteract && Application.isMobilePlatform && !startPressing)
                    return;

                canInteract = true;
                if (UnityEngine.EventSystems.EventSystem.current != null) {
                    if (Application.isMobilePlatform && Input.touchCount > 0 && UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject(Input.GetTouch(0).fingerId)) {
                        canInteract = false;
                    } else if (UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject(-1))
                        canInteract = false;
                }
                if (!canInteract) {
                    HideTerritoryRegionHighlight();
                    HideCellRegionHighlight();
                    return;
                }
            }

            if (_allowHighlightWhileDragging || ((!Application.isMobilePlatform && !Input.GetMouseButton(0)) || startPressing)) { // on mobile only check when touch start and on desktop do not check if dragging
                CheckMousePos();        // Verify if mouse enter a territory boundary 
            }
            TriggerEvents(); // Listen to pointer events

            UpdateHighlightFade();
        }

        #endregion



        #region Initialization

        public void Init() {

#if UNITY_EDITOR
#if UNITY_2018_3_OR_NEWER
			UnityEditor.PrefabInstanceStatus prefabInstanceStatus = UnityEditor.PrefabUtility.GetPrefabInstanceStatus(gameObject);
			if (prefabInstanceStatus != UnityEditor.PrefabInstanceStatus.NotAPrefab) {
            UnityEditor.EditorApplication.delayCall += () => 
			UnityEditor.PrefabUtility.UnpackPrefabInstance(gameObject, UnityEditor.PrefabUnpackMode.Completely, UnityEditor.InteractionMode.AutomatedAction);
			}
#else
            UnityEditor.PrefabType prefabType = UnityEditor.PrefabUtility.GetPrefabType(gameObject);
            if (prefabType != UnityEditor.PrefabType.None && prefabType != UnityEditor.PrefabType.DisconnectedPrefabInstance && prefabType != UnityEditor.PrefabType.DisconnectedModelPrefabInstance && prefabType != UnityEditor.PrefabType.Prefab) {
                UnityEditor.PrefabUtility.DisconnectPrefabInstance(gameObject);
            }
#endif
#endif

            disposalManager = new DisposalManager();
            tempFaders = new List<SurfaceFader>();
            tempListCells = new List<int>();
            tempPoints = new List<Vector3>();
            tempUVs = new List<Vector4>();
            tempIndices = new List<int>();

            LoadGeometryShaders();

            if (territoriesThinMat == null) {
                territoriesThinMat = Instantiate(Resources.Load<Material>("Materials/Territory")) as Material;
                disposalManager.MarkForDisposal(territoriesThinMat);
            }
            if (territoriesDisputedThinMat == null) {
                territoriesDisputedThinMat = Instantiate(territoriesThinMat) as Material;
                disposalManager.MarkForDisposal(territoriesDisputedThinMat);
                territoriesDisputedThinMat.color = _territoryDisputedFrontierColor;
            }
            if (cellsThinMat == null) {
                cellsThinMat = Instantiate(Resources.Load<Material>("Materials/Cell"));
                disposalManager.MarkForDisposal(cellsThinMat);
            }
            if (hudMatTerritoryOverlay == null) {
                hudMatTerritoryOverlay = new Material(Shader.Find("Terrain Grid System/Unlit Highlight Ground Texture"));
                hudMatTerritoryOverlay.SetInt("_Cull", (int)UnityEngine.Rendering.CullMode.Off);
                hudMatTerritoryOverlay.SetInt("_ZTest", (int)UnityEngine.Rendering.CompareFunction.Always);
                disposalManager.MarkForDisposal(hudMatTerritoryOverlay);
            }
            if (hudMatTerritoryGround == null) {
                hudMatTerritoryGround = Instantiate(hudMatTerritoryOverlay) as Material;
                hudMatTerritoryGround.SetInt("_Cull", (int)UnityEngine.Rendering.CullMode.Back);
                hudMatTerritoryGround.SetInt("_ZTest", (int)UnityEngine.Rendering.CompareFunction.LessEqual);
                disposalManager.MarkForDisposal(hudMatTerritoryGround);
            }
            if (hudMatCellOverlay == null) {
                hudMatCellOverlay = Instantiate(hudMatTerritoryOverlay) as Material;
                hudMatCellOverlay.SetInt("_Cull", (int)UnityEngine.Rendering.CullMode.Off);
                hudMatCellOverlay.SetInt("_ZTest", (int)UnityEngine.Rendering.CompareFunction.Always);
                disposalManager.MarkForDisposal(hudMatCellOverlay);
            }
            if (hudMatCellGround == null) {
                hudMatCellGround = Instantiate(hudMatTerritoryOverlay) as Material;
                hudMatCellGround.SetInt("_Cull", (int)UnityEngine.Rendering.CullMode.Back);
                hudMatCellGround.SetInt("_ZTest", (int)UnityEngine.Rendering.CompareFunction.LessEqual);
                disposalManager.MarkForDisposal(hudMatCellGround);
            }
            // Materials for cells
            if (coloredMatGroundCell == null) {
                coloredMatGroundCell = Instantiate(Resources.Load<Material>("Materials/ColorizedRegionGround")) as Material;
                coloredMatGroundCell.renderQueue++;
                disposalManager.MarkForDisposal(coloredMatGroundCell);
            }
            if (coloredMatOverlayCell == null) {
                coloredMatOverlayCell = Instantiate(Resources.Load<Material>("Materials/ColorizedRegionOverlay")) as Material;
                coloredMatOverlayCell.renderQueue++;
                disposalManager.MarkForDisposal(coloredMatOverlayCell);
            }
            if (texturizedMatGroundCell == null) {
                texturizedMatGroundCell = Instantiate(Resources.Load<Material>("Materials/TexturizedRegionGround"));
                texturizedMatGroundCell.renderQueue++;
                disposalManager.MarkForDisposal(texturizedMatGroundCell);
            }
            if (texturizedMatOverlayCell == null) {
                texturizedMatOverlayCell = Instantiate(Resources.Load<Material>("Materials/TexturizedRegionOverlay"));
                texturizedMatOverlayCell.renderQueue++;
                disposalManager.MarkForDisposal(texturizedMatOverlayCell);
            }
            // Materials for territories
            if (coloredMatGroundTerritory == null) {
                coloredMatGroundTerritory = Instantiate(Resources.Load<Material>("Materials/ColorizedRegionGround")) as Material;
                disposalManager.MarkForDisposal(coloredMatGroundTerritory);
            }
            if (coloredMatOverlayTerritory == null) {
                coloredMatOverlayTerritory = Instantiate(Resources.Load<Material>("Materials/ColorizedRegionOverlay")) as Material;
                disposalManager.MarkForDisposal(coloredMatOverlayTerritory);
            }
            if (texturizedMatGroundTerritory == null) {
                texturizedMatGroundTerritory = Instantiate(Resources.Load<Material>("Materials/TexturizedRegionGround"));
                disposalManager.MarkForDisposal(texturizedMatGroundTerritory);
            }
            if (texturizedMatOverlayTerritory == null) {
                texturizedMatOverlayTerritory = Instantiate(Resources.Load<Material>("Materials/TexturizedRegionOverlay"));
                disposalManager.MarkForDisposal(texturizedMatOverlayTerritory);
            }

            coloredMatCacheGroundCell = new Dictionary<Color, Material>();
            coloredMatCacheOverlayCell = new Dictionary<Color, Material>();
            coloredMatCacheGroundTerritory = new Dictionary<Color, Material>();
            coloredMatCacheOverlayTerritory = new Dictionary<Color, Material>();
            frontierColorCache = new Dictionary<Color, Material>();
            if (hits == null || hits.Length == 0)
                hits = new RaycastHit[100];
            UnityEngine.Random.InitState(seed);

            if (factoryColors == null || factoryColors.Length < MAX_TERRITORIES) {
                factoryColors = new Color[MAX_TERRITORIES];
                for (int k = 0; k < factoryColors.Length; k++)
                    factoryColors[k] = new Color(UnityEngine.Random.Range(0.0f, 0.5f), UnityEngine.Random.Range(0.0f, 0.5f), UnityEngine.Random.Range(0.0f, 0.5f));
            }
            if (_sortedTerritories == null)
                _sortedTerritories = new List<Territory>(MAX_TERRITORIES);

            if (textures == null || textures.Length < 32)
                textures = new Texture2D[32];

            ReadMaskContents();
            Redraw();

            if (territoriesTexture != null) {
                CreateTerritories(territoriesTexture, territoriesTextureNeutralColor, territoriesHideNeutralCells);
            }
        }

        void LoadGeometryShaders() {
            canUseGeometryShaders = _useGeometryShaders && SystemInfo.graphicsDeviceType != UnityEngine.Rendering.GraphicsDeviceType.Metal;
            if (territoriesGeoMat == null) {
                if (canUseGeometryShaders) {
                    territoriesGeoMat = new Material(Shader.Find("Terrain Grid System/Unlit Single Color Territory Geo"));
                } else {
                    territoriesGeoMat = new Material(Shader.Find("Terrain Grid System/Unlit Single Color Territory Thick Hack"));
                }
                disposalManager.MarkForDisposal(territoriesGeoMat);
            }
            if (territoriesGeoMat != null && territoriesGeoMat.color != _territoryFrontierColor) {
                territoriesGeoMat.color = _territoryFrontierColor;
            }
            if (territoriesDisputedGeoMat == null) {
                territoriesDisputedGeoMat = Instantiate(territoriesGeoMat) as Material;
                disposalManager.MarkForDisposal(territoriesDisputedGeoMat);
            }
            if (territoriesDisputedGeoMat != null && territoriesDisputedGeoMat.color != _territoryDisputedFrontierColor) {
                territoriesDisputedGeoMat.color = _territoryDisputedFrontierColor;
            }
            if (cellsGeoMat == null) {
                if (canUseGeometryShaders) {
                    cellsGeoMat = new Material(Shader.Find("Terrain Grid System/Unlit Single Color Cell Geo"));
                } else {
                    cellsGeoMat = new Material(Shader.Find("Terrain Grid System/Unlit Single Color Cell Thick Hack"));
                }
                disposalManager.MarkForDisposal(cellsGeoMat);
            }
            if (cellsGeoMat != null && cellsGeoMat.color != _cellBorderColor) {
                cellsGeoMat.color = _cellBorderColor;
            }
            if (frontierColorCache != null) {
                frontierColorCache.Clear();
            }
        }

#if UNITY_EDITOR
        void OnDrawGizmosSelected() {
            if (_terrainWrapper != null) {
                Gizmos.DrawWireCube(_terrainWrapper.bounds.center, _terrainWrapper.bounds.size);
            }

        }


#endif


        void CreateSurfacesLayer() {
            Transform t = transform.Find("Surfaces");
            if (t != null) {
                DestroyImmediate(t.gameObject);
            }
            _surfacesLayer = new GameObject("Surfaces");
            _surfacesLayer.transform.SetParent(transform, false);
            _surfacesLayer.transform.localPosition = Misc.Vector3zero;
            _surfacesLayer.layer = gameObject.layer;
        }

        void DestroySurfaces() {
            HideTerritoryRegionHighlight();
            HideCellRegionHighlight();
            if (segmentHit != null)
                segmentHit.Clear();
            if (surfaces != null)
                surfaces.Clear();
            if (_surfacesLayer != null)
                DestroyImmediate(_surfacesLayer);
        }

        void DestroyTerritorySurfaces() {
            HideTerritoryRegionHighlight();
            if (territories != null) {
                int territoriesCount = territories.Count;
                for (int k = 0; k < territoriesCount; k++) {
                    if (territories[k].region != null) {
                        territories[k].region.DestroySurface();
                    }
                }
            }
        }



        void DestroySurfacesDirty() {
            if (cells != null) {
                int cellCount = cells.Count;
                for (int k = 0; k < cellCount; k++) {
                    Cell cell = cells[k];
                    if (cell.isDirty) {
                        int cacheIndex = GetCacheIndexForCellRegion(k);
                        if (cell.region != null) {
                            cell.region.DestroySurface();
                        }
                        surfaces[cacheIndex] = null;
                    }
                }
                int territoryCount = territories.Count;
                for (int k = 0; k < territoryCount; k++) {
                    Territory territory = territories[k];
                    if (territory.isDirty) {
                        int cacheIndex = GetCacheIndexForTerritoryRegion(k);
                        if (territory.region != null) {
                            territory.region.DestroySurface();
                        }
                        surfaces[cacheIndex] = null;
                    }
                }
            }
        }



        void ReadMaskContents() {
            if (_gridMask == null)
                return;
            try {
                mask = _gridMask.GetPixels();
            } catch {
                mask = null;
                Debug.Log("Mask texture is not readable. Check import settings.");
            }
        }


        #endregion


        #region Map generation

        [NonSerialized]
        public VoronoiCell[] lastComputedVoronoiCells;

        void SetupIrregularGrid() {

            VoronoiFortune voronoi = new VoronoiFortune();

            if (hasBakedVoronoi) {
                VoronoiDeserializeData(ref voronoi.cells);
            } else {
                int userVoronoiSitesCount = _voronoiSites != null ? _voronoiSites.Count : 0;
                Point[] centers = new Point[_numCells];
                for (int k = 0; k < centers.Length; k++) {
                    if (k < userVoronoiSitesCount) {
                        centers[k] = new Point(_voronoiSites[k].x, _voronoiSites[k].y);
                    } else {
                        centers[k] = new Point(UnityEngine.Random.Range(-0.49f, 0.49f), UnityEngine.Random.Range(-0.49f, 0.49f));
                    }
                }
                for (int k = 0; k < goodGridRelaxation; k++) {
                    voronoi.AssignData(centers);
                    voronoi.DoVoronoi();
                    if (k < goodGridRelaxation - 1) {
                        for (int j = 0; j < _numCells; j++) {
                            Point centroid = voronoi.cells[j].centroid;
                            centers[j] = (centers[j] + centroid) / 2;
                        }
                    }
                }
                if (_voronoiSerializationData != null) {
                    _voronoiSerializationData = null;
#if UNITY_EDITOR
                    if (!Application.isPlaying) {
                        UnityEditor.EditorUtility.SetDirty(this);
                        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
                    }
#endif
                }
            }
            lastComputedVoronoiCells = voronoi.cells;

            // Make cell regions: we assume cells have only 1 region but that can change in the future
            float curvature = goodGridCurvature;
            int cellsCount = 0;
            if (dictSegments == null) {
                dictSegments = new Dictionary<Point, Segment>(voronoi.cells.Length * 4);
            } else {
                dictSegments.Clear();
            }

            for (int k = 0; k < voronoi.cells.Length; k++) {
                VoronoiCell voronoiCell = voronoi.cells[k];
                Cell cell = new Cell(voronoiCell.center.vector3);
                Region cr = new Region(cell, false);
                if (curvature > 0) {
                    cr.polygon = voronoiCell.GetPolygon(3, curvature);
                } else {
                    cr.polygon = voronoiCell.GetPolygon(1, 0);
                }
                if (cr.polygon != null) {
                    // Add segments
                    int segCount = voronoiCell.segments.Count;
                    for (int i = 0; i < segCount; i++) {
                        Segment s = voronoiCell.segments[i];
                        if (!s.deleted) {
                            if (curvature > 0) {
                                cr.segments.AddRange(s.subdivisions);
                            } else {
                                cr.segments.Add(s);
                            }
                        }
                    }

                    cell.polygon = cr.polygon.Clone();
                    cell.region = cr;
                    cell.index = cellsCount++;
                    cells.Add(cell);
                }
            }
        }

        void SetupBoxGrid(bool strictQuads) {

            int qx = _cellColumnCount;
            int qy = _cellRowCount;

            double stepX = 1.0 / qx;
            double stepY = 1.0 / qy;

            double halfStepX = stepX * 0.5;
            double halfStepY = stepY * 0.5;

            Segment[,,] sides = new Segment[qx, qy, 4]; // 0 = left, 1 = top, 2 = right, 3 = bottom
            int subdivisions = goodGridCurvature > 0 ? 3 : 1;
            int cellsCount = 0;
            Connector connector = new Connector();
            Point[] quadPoints = new Point[4];

            for (int j = 0; j < qy; j++) {
                for (int k = 0; k < qx; k++) {
                    Point center = new Point((double)k / qx - 0.5 + halfStepX, (double)j / qy - 0.5 + halfStepY);
                    Cell cell = new Cell(new Vector2((float)center.x, (float)center.y));
                    cell.column = k;
                    cell.row = j;

                    Point p1 = center.Offset(-halfStepX, -halfStepY);
                    Point p2 = center.Offset(-halfStepX, halfStepY);
                    Point p3 = center.Offset(halfStepX, halfStepY);
                    Point p4 = center.Offset(halfStepX, -halfStepY);

                    Segment left = k > 0 ? sides[k - 1, j, 2] : new Segment(p1, p2, true);
                    sides[k, j, 0] = left;

                    Segment top = new Segment(p2, p3, j == qy - 1);
                    sides[k, j, 1] = top;

                    Segment right = new Segment(p3, p4, k == qx - 1);
                    sides[k, j, 2] = right;

                    Segment bottom = j > 0 ? sides[k, j - 1, 1] : new Segment(p4, p1, true);
                    sides[k, j, 3] = bottom;

                    Region cr = new Region(cell, true);
                    if (subdivisions > 1) {
                        cr.segments.AddRange(top.Subdivide(subdivisions, _gridCurvature));
                        cr.segments.AddRange(right.Subdivide(subdivisions, _gridCurvature));
                        cr.segments.AddRange(bottom.Subdivide(subdivisions, _gridCurvature));
                        cr.segments.AddRange(left.Subdivide(subdivisions, _gridCurvature));
                        connector.Clear();
                        connector.AddRange(cr.segments);
                        cr.polygon = connector.ToPolygon(); // FromLargestLineStrip();
                    } else {
                        cr.segments.Add(top);
                        cr.segments.Add(right);
                        cr.segments.Add(bottom);
                        cr.segments.Add(left);
                        quadPoints[0] = p1;
                        quadPoints[1] = p4;
                        quadPoints[2] = p3;
                        quadPoints[3] = p2;
                        cr.polygon = new Geom.Polygon(quadPoints); // left.start, bottom.start, right.start, top.start);
                    }
                    if (cr.polygon != null) {
                        cell.region = cr;
                        cell.index = cellsCount++;
                        cells.Add(cell);
                    }
                }
            }
        }

        void SetupHexagonalGrid() {

            double qx = 1.0 + (_cellColumnCount - 1.0) * 3.0 / 4.0;
            double qy = _cellRowCount + 0.5;
            int qy2 = _cellRowCount;
            int qx2 = _cellColumnCount;

            double stepX = 1.0 / qx;
            double stepY = 1.0 / qy;

            double halfStepX = stepX * 0.5;
            double halfStepY = stepY * 0.5;
            int evenLayout = _evenLayout ? 1 : 0;

            Segment[,,] sides = new Segment[qx2, qy2, 6]; // 0 = left-up, 1 = top, 2 = right-up, 3 = right-down, 4 = down, 5 = left-down
            int subdivisions = goodGridCurvature > 0 ? 3 : 1;
            int cellsCount = 0;
            for (int j = 0; j < qy2; j++) {
                for (int k = 0; k < qx2; k++) {
                    Point center = new Point((double)k / qx - 0.5 + halfStepX, (double)j / qy - 0.5 + stepY);
                    center.x -= k * halfStepX / 2;
                    Cell cell = new Cell(new Vector2((float)center.x, (float)center.y));
                    cell.row = j;
                    cell.column = k;

                    double offsetY = (k % 2 == evenLayout) ? 0 : -halfStepY;

                    Point p1 = center.Offset(-halfStepX, offsetY);
                    Point p2 = center.Offset(-halfStepX / 2, halfStepY + offsetY);
                    Point p3 = center.Offset(halfStepX / 2, halfStepY + offsetY);
                    Point p4 = center.Offset(halfStepX, offsetY);
                    Point p5 = center.Offset(halfStepX / 2, -halfStepY + offsetY);
                    Point p6 = center.Offset(-halfStepX / 2, -halfStepY + offsetY);

                    Segment leftUp = (k > 0 && offsetY < 0) ? sides[k - 1, j, 3] : new Segment(p1, p2, k == 0 || (j == qy2 - 1 && offsetY == 0));
                    sides[k, j, 0] = leftUp;

                    Segment top = new Segment(p2, p3, j == qy2 - 1);
                    sides[k, j, 1] = top;

                    Segment rightUp = new Segment(p3, p4, k == qx2 - 1 || (j == qy2 - 1 && offsetY == 0));
                    sides[k, j, 2] = rightUp;

                    Segment rightDown = (j > 0 && k < qx2 - 1 && offsetY < 0) ? sides[k + 1, j - 1, 0] : new Segment(p4, p5, (j == 0 && offsetY < 0) || k == qx2 - 1);
                    sides[k, j, 3] = rightDown;

                    Segment bottom = j > 0 ? sides[k, j - 1, 1] : new Segment(p5, p6, true);
                    sides[k, j, 4] = bottom;

                    Segment leftDown;
                    if (offsetY < 0 && j > 0 && k > 0) {
                        leftDown = sides[k - 1, j - 1, 2];
                    } else if (offsetY == 0 && k > 0) {
                        leftDown = sides[k - 1, j, 2];
                    } else {
                        leftDown = new Segment(p6, p1, true);
                    }
                    sides[k, j, 5] = leftDown;

                    cell.center.y += (float)offsetY;

                    Region cr = new Region(cell, false);
                    if (subdivisions > 1) {
                        if (!top.deleted)
                            cr.segments.AddRange(top.Subdivide(subdivisions, _gridCurvature));
                        if (!rightUp.deleted)
                            cr.segments.AddRange(rightUp.Subdivide(subdivisions, _gridCurvature));
                        if (!rightDown.deleted)
                            cr.segments.AddRange(rightDown.Subdivide(subdivisions, _gridCurvature));
                        if (!bottom.deleted)
                            cr.segments.AddRange(bottom.Subdivide(subdivisions, _gridCurvature));
                        if (!leftDown.deleted)
                            cr.segments.AddRange(leftDown.Subdivide(subdivisions, _gridCurvature));
                        if (!leftUp.deleted)
                            cr.segments.AddRange(leftUp.Subdivide(subdivisions, _gridCurvature));
                    } else {
                        if (!top.deleted)
                            cr.segments.Add(top);
                        if (!rightUp.deleted)
                            cr.segments.Add(rightUp);
                        if (!rightDown.deleted)
                            cr.segments.Add(rightDown);
                        if (!bottom.deleted)
                            cr.segments.Add(bottom);
                        if (!leftDown.deleted)
                            cr.segments.Add(leftDown);
                        if (!leftUp.deleted)
                            cr.segments.Add(leftUp);
                    }
                    Connector connector = new Connector();
                    connector.AddRange(cr.segments);
                    cr.polygon = connector.ToPolygon();
                    if (cr.polygon != null) {
                        cell.region = cr;
                        cell.index = cellsCount++;
                        cells.Add(cell);
                    }
                }
            }
        }

        void CreateCells() {
            UnityEngine.Random.InitState(seed);

            _numCells = Mathf.Max(_numTerritories, 2, cellCount);
            if (cells == null) {
                cells = new List<Cell>(_numCells);
            } else {
                cells.Clear();
            }
            if (cellTagged == null)
                cellTagged = new Dictionary<int, Cell>();
            else
                cellTagged.Clear();

            switch (_gridTopology) {
                case GRID_TOPOLOGY.Box:
                    SetupBoxGrid(true);
                    break;
                case GRID_TOPOLOGY.Hexagonal:
                    SetupHexagonalGrid();
                    break;
                default:
                    SetupIrregularGrid();
                    break;
            }

            CellsFindNeighbours();
            CellsUpdateBounds();

            // Update sorted cell list
            if (sortedCells == null) {
                sortedCells = new List<Cell>(cells);
            } else {
                sortedCells.Clear();
                sortedCells.AddRange(cells);
            }
            ResortCells();

            ClearLastOver();

            recreateCells = false;

        }

        void ResortCells() {
            needResortCells = false;
            sortedCells.Sort((cell1, cell2) => {
                float area1 = cell1.region.rect2DArea;
                float area2 = cell2.region.rect2DArea;
                if (area1 < area2) return -1;
                if (area1 > area2) return 1;
                return 0;
            });
        }

        /// <summary>
        /// Takes the center of each cell or the border points and checks the alpha component of the mask to confirm visibility
        /// </summary>
        void CellsApplyVisibilityFilters() {
            int cellsCount = cells.Count;
            if (gridMask != null && mask != null) {
                int tw = gridMask.width;
                int th = gridMask.height;
                if (tw * th == mask.Length) {
                    for (int k = 0; k < cellsCount; k++) {
                        Cell cell = cells[k];
                        int pointCount = cell.region.points.Count;
                        bool visible = false;
                        for (int v = 0; v < pointCount; v++) {
                            Vector2 p = cell.region.points[v];
                            if (_gridMaskUseScale) {
                                GetUnscaledVector(ref p);
                            }
                            float y = p.y + 0.5f;
                            float x = p.x + 0.5f;
                            int ty = (int)(y * th);
                            int tx = (int)(x * tw);
                            if (ty >= 0 && ty < th && tx >= 0 && tx < tw && mask[ty * tw + tx].a > 0) {
                                visible = true;
                                break;
                            }
                        }
                        cell.visibleByRules = visible;
                    }
                }
            } else {
                for (int k = 0; k < cellsCount; k++) {
                    Cell cell = cells[k];
                    if (cell != null) {
                        cell.visibleByRules = true;
                    }
                }
            }

            // potentially hide cells whose territories are invisible
            if (territoriesHideNeutralCells && territories != null) {
                int terrCount = territories.Count;
                for (int k = 0; k < cellsCount; k++) {
                    Cell cell = cells[k];
                    if (cell != null && cell.visible) {
                        int terr = cell.territoryIndex;
                        cell.visibleByRules = terr < 0 || (terr >= 0 && terr < terrCount && territories[terr].visible);
                    }
                }
            }


            if (_terrainWrapper != null) {
                if (_cellsMaxSlope < 1f) {
                    for (int k = 0; k < cellsCount; k++) {
                        Cell cell = cells[k];
                        if (cell == null || !cell.visible)
                            continue;
                        float x = cell.scaledCenter.x + 0.5f;
                        if (x < 0 || x > 1f)
                            continue;
                        float y = cell.scaledCenter.y + 0.5f;
                        if (y < 0 || y > 1f)
                            continue;
                        Vector3 norm = _terrainWrapper.GetInterpolatedNormal(x, y);
                        float slope = norm.y > 0 ? 1.00001f - norm.y : 1.00001f + norm.y;
                        if (slope > _cellsMaxSlope) {
                            cell.visibleByRules = false;
                        }
                    }
                }
                if (_cellsMinimumAltitude != 0f) {
                    for (int k = 0; k < cellsCount; k++) {
                        Cell cell = cells[k];
                        if (cell == null || !cell.visible)
                            continue;
                        Vector3 wpos = GetWorldSpacePosition(cell.scaledCenter);
                        if (wpos.y < _cellsMinimumAltitude) {
                            cell.visibleByRules = false;
                        }
                    }
                }
            }

            ClearLastOver();
            needRefreshRouteMatrix = true;
        }

        void CellsUpdateBounds() {
            // Update cells polygon
            int count = cells.Count;
            for (int k = 0; k < count; k++) {
                CellUpdateBounds(cells[k]);
            }
        }

        void CellUpdateBounds(Cell cell) {
            if (cell == null) return;

            cell.polygon = cell.region.polygon;
            if (cell.polygon.contours.Count == 0)
                return;

            List<Vector2> points = cell.region.points;
            cell.polygon.contours[0].GetVector2Points(_gridCenter, _gridScale, ref points);
            cell.region.points = points;

            // Update bounding rect
            float minx, miny, maxx, maxy;
            minx = miny = float.MaxValue;
            maxx = maxy = float.MinValue;
            int pointsCount = points.Count;
            for (int p = 0; p < pointsCount; p++) {
                Vector2 point = points[p];
                if (point.x < minx)
                    minx = point.x;
                if (point.x > maxx)
                    maxx = point.x;
                if (point.y < miny)
                    miny = point.y;
                if (point.y > maxy)
                    maxy = point.y;
            }
            float rectWidth = maxx - minx;
            float rectHeight = maxy - miny;
            cell.region.rect2D = new Rect(minx, miny, rectWidth, rectHeight);
            cell.region.rect2DArea = rectWidth * rectHeight;
            cell.scaledCenter = GetScaledVector(cell.center);
        }

        void CellsUpdateNeighbours() {
            int cellCount = cells.Count;
            for (int k = 0; k < cellCount; k++) {
                Cell cell = cells[k];
                if (cell == null) continue;
                Region region = cell.region;
                region.neighbours.Clear();
                int segCount = region.segments.Count;
                for (int j = 0; j < segCount; j++) {
                    region.segments[j].cellIndex = k;
                }
            }
            CellsFindNeighbours();
        }

        void CellsFindNeighbours() {

            int cellCount = cells.Count;
            for (int k = 0; k < cellCount; k++) {
                Cell cell = cells[k];
                if (cell == null) continue;
                Region region = cell.region;
                int numSegments = region.segments.Count;
                for (int i = 0; i < numSegments; i++) {
                    Segment seg = region.segments[i];
                    if (seg.cellIndex < 0) {
                        seg.cellIndex = cell.index;
                    } else if (seg.cellIndex != cell.index) {
                        Region neighbour = cells[seg.cellIndex].region;
                        region.neighbours.Add(neighbour);
                        neighbour.neighbours.Add(region);
                    }
                }
            }

        }

        void FindTerritoryFrontiers() {

            if (territories == null || territories.Count == 0)
                return;

            if (territoryFrontiers == null) {
                territoryFrontiers = new List<Segment>(50000);
            } else {
                territoryFrontiers.Clear();
            }
            if (territoryNeighbourHit == null) {
                territoryNeighbourHit = new Dictionary<Segment, Frontier>(50000);
            } else {
                territoryNeighbourHit.Clear();
            }
            int terrCount = territories.Count;
            if (territoryConnectors == null || territoryConnectors.Length != terrCount) {
                territoryConnectors = new Connector[terrCount];
            }
            int cellCount = cells.Count;
            if (frontierPool == null) {
                frontierPool = new Frontier[cellCount * 6];
                for (int f = 0; f < frontierPool.Length; f++) {
                    frontierPool[f] = new Frontier();
                }
            }
            int frontierPoolUsed = 0;
            for (int k = 0; k < terrCount; k++) {
                if (territoryConnectors[k] == null) {
                    territoryConnectors[k] = new Connector();
                } else {
                    territoryConnectors[k].Clear();
                }
                Territory territory = territories[k];
                territory.cells.Clear();
                if (territory.region == null) {
                    territory.region = new Region(territory, false);
                }
                territories[k].region.neighbours.Clear();
            }

            for (int k = 0; k < cellCount; k++) {
                Cell cell = cells[k];
                if (cell == null || cell.territoryIndex >= terrCount)
                    continue;
                bool validCell = cell.visible && cell.territoryIndex >= 0;
                if (validCell)
                    territories[cell.territoryIndex].cells.Add(cell);
                Region region = cell.region;
                int numSegments = region.segments.Count;
                for (int i = 0; i < numSegments; i++) {
                    Segment seg = region.segments[i];
                    if (seg.border) {
                        if (validCell) {
                            territoryFrontiers.Add(seg);
                            int territory1 = cell.territoryIndex;
                            territoryConnectors[territory1].Add(seg);
                            seg.territoryIndex = territory1;
                        }
                        continue;
                    }
                    Frontier frontier;
                    if (territoryNeighbourHit.TryGetValue(seg, out frontier)) {
                        Region neighbour = frontier.region1;
                        Cell neighbourCell = (Cell)neighbour.entity;
                        int territory1 = cell.territoryIndex;
                        int territory2 = neighbourCell.territoryIndex;
                        if (territory2 != territory1) {
                            territoryFrontiers.Add(seg);
                            if (validCell) {
                                territoryConnectors[territory1].Add(seg);
                                bool territory1IsNeutral = territories[territory1].neutral;
                                // check segment ownership
                                if (territory2 >= 0) {
                                    bool territory2IsNeutral = territories[territory2].neutral;
                                    if (territory1IsNeutral && territory2IsNeutral) {
                                        seg.territoryIndex = territory1;
                                        frontier.region2 = frontier.region1;
                                        frontier.region1 = cell.region;
                                    } else if (territory1IsNeutral && !territory2IsNeutral) {
                                        seg.territoryIndex = territory2;
                                        frontier.region2 = cell.region;
                                    } else if (!territory1IsNeutral && territory2IsNeutral) {
                                        seg.territoryIndex = territory1;
                                        frontier.region2 = frontier.region1;
                                        frontier.region1 = cell.region;
                                    } else {
                                        seg.territoryIndex = -1; // if segment belongs to a visible cell and valid territory2, mark this segment as disputed. Otherwise make it part of territory1
                                        frontier.region2 = cell.region;
                                    }
                                } else {
                                    seg.territoryIndex = territory1;
                                    frontier.region2 = cell.region;
                                }


                                if (seg.territoryIndex < 0) {
                                    // add territory neigbhours
                                    Region territory1Region = territories[territory1].region;
                                    Region territory2Region = territories[territory2].region;
                                    if (!territory1Region.neighbours.Contains(territory2Region)) {
                                        territory1Region.neighbours.Add(territory2Region);
                                    }
                                    if (!territory2Region.neighbours.Contains(territory1Region)) {
                                        territory2Region.neighbours.Add(territory1Region);
                                    }
                                }
                            }
                            if (territory2 >= 0) {
                                territoryConnectors[territory2].Add(seg);
                            }
                        }
                    } else {
                        if (frontierPoolUsed >= frontierPool.Length) {
                            // Resize frontier pool
                            int newLen = frontierPool.Length * 2;
                            Frontier[] newPool = new Frontier[newLen];
                            Array.Copy(frontierPool, newPool, frontierPool.Length);
                            for (int f = frontierPool.Length; f < newLen; f++) {
                                newPool[f] = new Frontier();
                            }
                            frontierPool = newPool;
                        }
                        // Get it from pool
                        frontier = frontierPool[frontierPoolUsed++];
                        frontier.region1 = region;
                        frontier.region2 = null;
                        territoryNeighbourHit[seg] = frontier;
                        seg.territoryIndex = cell.territoryIndex;
                    }
                }
            }

            for (int k = 0; k < terrCount; k++) {
                if (territories[k].cells.Count > 0) {
                    territories[k].polygon = territoryConnectors[k].ToPolygonFromLargestLineStrip();
                } else {
                    territories[k].region.Clear();
                    territories[k].polygon = null;
                }
            }
        }

        /// <summary>
        /// Subdivides the segment in smaller segments
        /// </summary>
        /// <returns><c>true</c>, if segment was drawn, <c>false</c> otherwise.</returns>
        void SurfaceSegmentForMesh(Vector3 p0, Vector3 p1) {

            // trace the line until roughness is exceeded
            float dist = (float)Math.Sqrt((p1.x - p0.x) * (p1.x - p0.x) + (p1.y - p0.y) * (p1.y - p0.y));
            Vector3 direction = p1 - p0;

            int numSteps = (int)(meshStep * dist);
            EnsureCapacityFrontiersPoints(numSteps * 2);

            Vector3 t0 = p0;
            float h0 = _terrainWrapper.SampleHeight(transform.TransformPoint(t0));
            if (_gridNormalOffset > 0) {
                Vector3 invNormal = transform.InverseTransformVector(_terrainWrapper.GetInterpolatedNormal(t0.x + 0.5f, t0.y + 0.5f));
                t0 += invNormal * _gridNormalOffset;
            }
            t0.z -= h0;
            Vector3 ta = t0;
            float h1, ha = h0;
            for (int i = 1; i < numSteps; i++) {
                Vector3 t1 = p0 + direction * ((float)i / numSteps);
                h1 = _terrainWrapper.SampleHeight(transform.TransformPoint(t1));
                if (h1 - h0 > maxRoughnessWorldSpace || h0 - h1 > maxRoughnessWorldSpace) {
                    frontiersPoints[frontiersPointsCount++] = t0;
                    if (t0 != ta) {
                        if (_gridNormalOffset > 0) {
                            Vector3 invNormal = transform.InverseTransformVector(_terrainWrapper.GetInterpolatedNormal(ta.x + 0.5f, ta.y + 0.5f));
                            ta += invNormal * _gridNormalOffset;
                        }
                        ta.z -= ha;
                        frontiersPoints[frontiersPointsCount++] = ta;
                        frontiersPoints[frontiersPointsCount++] = ta;
                    }
                    if (_gridNormalOffset > 0) {
                        Vector3 invNormal = transform.InverseTransformVector(_terrainWrapper.GetInterpolatedNormal(t1.x + 0.5f, t1.y + 0.5f));
                        t1 += invNormal * _gridNormalOffset;
                    }
                    t1.z -= h1;
                    frontiersPoints[frontiersPointsCount++] = t1;
                    t0 = t1;
                    h0 = h1;
                }
                ta = t1;
                ha = h1;
            }
            // Add last point
            h1 = _terrainWrapper.SampleHeight(transform.TransformPoint(p1));
            if (_gridNormalOffset > 0) {
                Vector3 invNormal = transform.InverseTransformVector(_terrainWrapper.GetInterpolatedNormal(p1.x + 0.5f, p1.y + 0.5f));
                p1 += invNormal * _gridNormalOffset;
            }
            p1.z -= h1;
            frontiersPoints[frontiersPointsCount++] = t0;
            frontiersPoints[frontiersPointsCount++] = p1;
        }

        void GenerateCellsMesh(List<Cell>cells) {

            if (segmentHit == null) {
                segmentHit = new Dictionary<Segment, bool>(50000);
            } else {
                segmentHit.Clear();
            }

            if (frontiersPoints == null || frontiersPoints.Length == 0) {
                frontiersPoints = new Vector3[100000];
            }
            frontiersPointsCount = 0;

            int cellCount = cells.Count;
            if (_terrainWrapper == null) {
                for (int k = 0; k < cellCount; k++) {
                    Cell cell = cells[k];
                    if (cell != null && cell.visible && cell.borderVisible) {
                        Region region = cell.region;
                        int numSegments = region.segments.Count;
                        EnsureCapacityFrontiersPoints(numSegments * 2);
                        for (int i = 0; i < numSegments; i++) {
                            Segment s = region.segments[i];
                            if (!segmentHit.ContainsKey(s)) {
                                segmentHit[s] = true;
                                frontiersPoints[frontiersPointsCount++] = GetScaledVector(s.start);
                                frontiersPoints[frontiersPointsCount++] = GetScaledVector(s.end);
                            }
                        }
                    }
                }
            } else {
                meshStep = (2.0f - _gridRoughness) / (float)MIN_VERTEX_DISTANCE;
                for (int k = 0; k < cellCount; k++) {
                    Cell cell = cells[k];
                    if (cell != null && cell.visible && cell.borderVisible) {
                        Region region = cell.region;
                        int numSegments = region.segments.Count;
                        for (int i = 0; i < numSegments; i++) {
                            Segment s = region.segments[i];
                            if (!segmentHit.ContainsKey(s)) {
                                segmentHit[s] = true;
                                SurfaceSegmentForMesh(GetScaledVector(s.start.vector3), GetScaledVector(s.end.vector3));
                            }
                        }
                    }
                }
            }

            int maxPointsPerMesh = 65504;
            if (isUsingVertexDispForVertexThickness) {
                maxPointsPerMesh /= 4;
            }

            int meshGroups = (frontiersPointsCount / maxPointsPerMesh) + 1;
            int meshIndex = -1;
            if (cellMeshIndices == null || cellMeshIndices.GetUpperBound(0) != meshGroups - 1) {
                cellMeshIndices = new int[meshGroups][];
                cellMeshBorders = new Vector3[meshGroups][];
            }
            if (frontiersPointsCount == 0) {
                cellMeshBorders[0] = new Vector3[0];
                cellMeshIndices[0] = new int[0];
            } else {
                // Clamp points to minimum elevation
                if (_cellsMinimumAltitudeClampVertices && _cellsMinimumAltitude != 0) {
                    float localMinima = -(_cellsMinimumAltitude - _terrainWrapper.bounds.min.y) / _terrainWrapper.transform.lossyScale.y;
                    for (int k = 0; k < frontiersPointsCount; k++) {
                        if (frontiersPoints[k].z > localMinima) {
                            frontiersPoints[k].z = localMinima;
                        }
                    }
                }

                for (int k = 0; k < frontiersPointsCount; k += maxPointsPerMesh) {
                    int max = Mathf.Min(frontiersPointsCount - k, maxPointsPerMesh);
                    ++meshIndex;
                    if (cellMeshBorders[meshIndex] == null || cellMeshBorders[0].GetUpperBound(0) != max - 1) {
                        cellMeshBorders[meshIndex] = new Vector3[max];
                        cellMeshIndices[meshIndex] = new int[max];
                    }
                    for (int j = 0; j < max; j++) {
                        cellMeshBorders[meshIndex][j] = frontiersPoints[j + k];
                        cellMeshIndices[meshIndex][j] = j;
                    }
                }
            }
        }

        void CreateTerritories() {

            _numTerritories = Mathf.Clamp(_numTerritories, 1, cellCount);

            if (!_colorizeTerritories && !_showTerritories && _highlightMode != HIGHLIGHT_MODE.Territories) {
                if (territories != null)
                    territories.Clear();
                if (territoryLayer != null)
                    DestroyImmediate(territoryLayer);
                return;
            }

            if (territories == null) {
                territories = new List<Territory>(_numTerritories);
            } else {
                territories.Clear();
            }

            CheckCells();
            // Freedom for the cells!...
            int cellsCount = cells.Count;
            for (int k = 0; k < cellsCount; k++) {
                cells[k].territoryIndex = -1;
            }
            UnityEngine.Random.InitState(seed);

            for (int c = 0; c < _numTerritories; c++) {
                Territory territory = new Territory(c.ToString());
                territory.fillColor = factoryColors[c];
                int territoryIndex = territories.Count;
                int p = UnityEngine.Random.Range(0, cellsCount);
                int z = 0;
                while ((cells[p].territoryIndex != -1 || !cells[p].visible) && z++ <= cellsCount) {
                    p++;
                    if (p >= cellsCount)
                        p = 0;
                }
                if (z > cellsCount)
                    break; // no more territories can be found - this should not happen
                Cell cell = cells[p];
                cell.territoryIndex = territoryIndex;
                territory.center = cell.center;
                territory.cells.Add(cell);
                territories.Add(territory);
            }

            // Continue conquering cells
            int[] territoryCellIndex = new int[territories.Count];

            // Iterate one cell per country (this is not efficient but ensures balanced distribution)
            bool remainingCells = true;
            while (remainingCells) {
                remainingCells = false;
                int terrCount = territories.Count;
                for (int k = 0; k < terrCount; k++) {
                    Territory territory = territories[k];
                    int territoryCellsCount = territory.cells.Count;
                    for (int p = territoryCellIndex[k]; p < territoryCellsCount; p++) {
                        Region cellRegion = territory.cells[p].region;
                        int nCount = cellRegion.neighbours.Count;
                        for (int n = 0; n < nCount; n++) {
                            Region otherRegion = cellRegion.neighbours[n];
                            Cell otherProv = (Cell)otherRegion.entity;
                            if (otherProv.territoryIndex == -1 && otherProv.visible) {
                                otherProv.territoryIndex = k;
                                territory.cells.Add(otherProv);
                                territoryCellsCount++;
                                remainingCells = true;
                                p = territoryCellsCount;
                                break;
                            }
                        }
                        if (p < territoryCellsCount) {// no free neighbours left for this cell
                            territoryCellIndex[k]++;
                        }
                    }
                }
            }

            FindTerritoryFrontiers();
            UpdateTerritoryBoundaries();

            recreateTerritories = false;

        }

        void UpdateTerritoryBoundaries() {
            if (territories == null)
                return;

            // Update territory region
            int terrCount = territories.Count;
            for (int k = 0; k < terrCount; k++) {
                Territory territory = territories[k];
                if (territory.polygon == null) {
                    continue;
                }

                Region territoryRegion = territory.region;
                List<Vector2> regionPoints = territoryRegion.points;
                territory.polygon.contours[0].GetVector2Points(_gridCenter, _gridScale, ref regionPoints);
                territoryRegion.points = regionPoints;
                regionPoints = null;
                territory.scaledCenter = GetScaledVector(territory.center);

                List<Point> points = territory.polygon.contours[0].points;
                int pointCount = points.Count;

                int segCount = territoryRegion.segments.Count;
                if (segCount < pointCount) {
                    territoryRegion.segments.Clear();
                    for (int j = 0; j < pointCount; j++) {
                        Point p0 = points[j];
                        Point p1;
                        if (j == pointCount - 1) {
                            p1 = points[0];
                        } else {
                            p1 = points[j + 1];
                        }
                        territoryRegion.segments.Add(new Segment(p0, p1));
                    }
                } else {
                    while (segCount > pointCount) {
                        territoryRegion.segments.RemoveAt(--segCount);
                    }
                    for (int j = 0; j < pointCount; j++) {
                        Point p0 = points[j];
                        Point p1;
                        if (j == pointCount - 1) {
                            p1 = points[0];
                        } else {
                            p1 = points[j + 1];
                        }
                        territoryRegion.segments[j].Init(p0, p1);
                    }
                }

                // Update bounding rect
                float minx, miny, maxx, maxy;
                minx = miny = float.MaxValue;
                maxx = maxy = float.MinValue;
                int terrPointCount = territoryRegion.points.Count;
                for (int p = 0; p < terrPointCount; p++) {
                    Vector2 point = territoryRegion.points[p];
                    if (point.x < minx)
                        minx = point.x;
                    if (point.x > maxx)
                        maxx = point.x;
                    if (point.y < miny)
                        miny = point.y;
                    if (point.y > maxy)
                        maxy = point.y;
                }
                float rectWidth = maxx - minx;
                float rectHeight = maxy - miny;
                territoryRegion.rect2D = new Rect(minx, miny, rectWidth, rectHeight);
                territoryRegion.rect2DArea = rectWidth * rectHeight;
            }

            _sortedTerritories.Clear();
        }

        void GenerateTerritoriesMesh() {
            if (territories == null)
                return;

            if (frontiersPoints == null || frontiersPoints.Length == 0) {
                frontiersPoints = new Vector3[10000];
            }

            int terrCount = territories.Count;

            TerritoryMesh tm, tmDisputed = null;
            if (issueRedraw == RedrawType.IncrementalTerritories && territoryMeshes != null) {
                if (territoryFrontiers == null)
                    return;
                int territoryMeshesCount = territoryMeshes.Count;
                for (int k = 0; k < territoryMeshesCount; k++) {
                    int terrIndex = territoryMeshes[k].territoryIndex;
                    if (terrIndex >= 0) {
                        Territory territory = territories[terrIndex];
                        if (territory.isDirty) {
                            tm = territoryMeshes[k];
                            GenerateTerritoryMesh(tm);
                        }
                    } else {
                        tmDisputed = territoryMeshes[k];
                    }
                }
            } else {
                if (territoryMeshes == null) {
                    territoryMeshes = new List<TerritoryMesh>(terrCount + 1);
                } else {
                    territoryMeshes.Clear();
                }

                if (territoryFrontiers == null)
                    return;

                for (int k = 0; k < terrCount; k++) {
                    tm = new TerritoryMesh();
                    tm.territoryIndex = k;
                    if (GenerateTerritoryMesh(tm)) {
                        territoryMeshes.Add(tm);
                    }
                }
            }

            // Generate disputed frontiers
            if (tmDisputed == null) {
                tmDisputed = new TerritoryMesh();
                tmDisputed.territoryIndex = -1;
                if (GenerateTerritoryMesh(tmDisputed)) {
                    territoryMeshes.Add(tmDisputed);
                }
            } else if (!GenerateTerritoryMesh(tmDisputed)) {
                territoryMeshes.Remove(tmDisputed);
            }
        }

        /// <summary>
        /// Generates the territory mesh.
        /// </summary>
        /// <returns>True if something was produced.</returns>
        bool GenerateTerritoryMesh(TerritoryMesh tm) {

            frontiersPointsCount = 0;

            int territoryFrontiersCount = territoryFrontiers.Count;
            if (_terrainWrapper == null) {
                EnsureCapacityFrontiersPoints(territoryFrontiersCount * 2);
                for (int k = 0; k < territoryFrontiersCount; k++) {
                    Segment s = territoryFrontiers[k];
                    if (s.territoryIndex != tm.territoryIndex)
                        continue;
                    if (s.territoryIndex >= 0 && !territories[s.territoryIndex].borderVisible)
                        continue;
                    if (!s.border || _showTerritoriesOuterBorder) {
                        frontiersPoints[frontiersPointsCount++] = GetScaledVector(s.startToVector3);
                        frontiersPoints[frontiersPointsCount++] = GetScaledVector(s.endToVector3);
                    }
                }
            } else {
                meshStep = (2.0f - _gridRoughness) / (float)MIN_VERTEX_DISTANCE;
                for (int k = 0; k < territoryFrontiersCount; k++) {
                    Segment s = territoryFrontiers[k];
                    if (s.territoryIndex != tm.territoryIndex)
                        continue;
                    if (s.territoryIndex >= 0 && !territories[s.territoryIndex].borderVisible)
                        continue;
                    if (!s.border || _showTerritoriesOuterBorder) {
                        SurfaceSegmentForMesh(GetScaledVector(s.start.vector3), GetScaledVector(s.end.vector3));
                    }
                }

            }

            int maxPointsPerMesh = 65504;
            if (isUsingVertexDispForTerritoryThickness) {
                maxPointsPerMesh /= 4;
            }

            int meshGroups = (frontiersPointsCount / maxPointsPerMesh) + 1;
            int meshIndex = -1;
            if (tm.territoryMeshIndices == null || tm.territoryMeshIndices.GetUpperBound(0) != meshGroups - 1) {
                tm.territoryMeshIndices = new int[meshGroups][];
                tm.territoryMeshBorders = new Vector3[meshGroups][];
            }

            // Clamp points to minimum elevation
            if (_cellsMinimumAltitudeClampVertices && _cellsMinimumAltitude != 0) {
                float localMinima = -(_cellsMinimumAltitude - _terrainWrapper.bounds.min.y) / _terrainWrapper.transform.lossyScale.y;
                for (int k = 0; k < frontiersPointsCount; k++) {
                    if (frontiersPoints[k].z > localMinima) {
                        frontiersPoints[k].z = localMinima;
                    }
                }
            }

            for (int k = 0; k < frontiersPointsCount; k += maxPointsPerMesh) {
                int max = Mathf.Min(frontiersPointsCount - k, maxPointsPerMesh);
                ++meshIndex;
                if (tm.territoryMeshBorders[meshIndex] == null || tm.territoryMeshBorders[meshIndex].GetUpperBound(0) != max - 1) {
                    tm.territoryMeshBorders[meshIndex] = new Vector3[max];
                    tm.territoryMeshIndices[meshIndex] = new int[max];
                }
                for (int j = 0; j < max; j++) {
                    tm.territoryMeshBorders[meshIndex][j] = frontiersPoints[j + k];
                    tm.territoryMeshIndices[meshIndex][j] = j;
                }
            }

            return frontiersPointsCount > 0;
        }

        void EnsureCapacityFrontiersPoints(int payload) {
            while (frontiersPointsCount + payload >= frontiersPoints.Length) {
                Vector3[] v = new Vector3[frontiersPoints.Length * 2];
                for (int k = 0; k < frontiersPointsCount; k++) {
                    v[k] = frontiersPoints[k];
                }
                frontiersPoints = v;
            }
        }


        void FitToTerrain() {
            if (_terrainWrapper == null || cameraMain == null)
                return;

            // Fit to terrain
            Vector3 terrainSize = _terrainWrapper.size;
            terrainWidth = terrainSize.x;
            terrainHeight = terrainSize.y;
            terrainDepth = terrainSize.z;
            transform.localRotation = Quaternion.Euler(90, 0, 0);
            Vector3 lossyScale = _terrainObject.transform.lossyScale;
            transform.localScale = new Vector3(terrainWidth / lossyScale.x, terrainDepth / lossyScale.z, 1);
            maxRoughnessWorldSpace = _gridRoughness * terrainHeight;

            if (_terrainWrapper is MeshTerrainWrapper) {
                ((MeshTerrainWrapper)_terrainWrapper).pivot = _terrainMeshPivot;
            } else {
#if UNITY_EDITOR
                if (_terrainWrapper.gameObject!= null && _terrainWrapper.transform.rotation.eulerAngles != Misc.Vector3zero) {
                    Debug.LogWarning("Terrain Grid System: warning, terrain shouldn't be rotated.");
                }
#endif
            }

            Vector3 camPos = cameraMain.transform.position;
            if (transform.parent != null && transform.parent.lossyScale != lastParentScale) {
                gridNeedsUpdate = true;
            } else if (transform.position != lastPos || transform.lossyScale != lastScale) {
                gridNeedsUpdate = true;
            }
            bool refresh = gridNeedsUpdate || gridElevationCurrent != lastGridElevation || _gridCameraOffset != lastGridCameraOffset || _gridMinElevationMultiplier != lastGridMinElevationMultiplier || camPos != lastCamPos;
            if (refresh) {
                if (gridNeedsUpdate) {
                    gridNeedsUpdate = false;
                    _terrainWrapper.Refresh();
                }
                Vector3 localPosition = _terrainWrapper.localCenter;
                localPosition.y += 0.01f * _gridMinElevationMultiplier + gridElevationCurrent;
                if (_gridCameraOffset > 0) {
                    localPosition += (camPos - transform.position).normalized * (camPos - _terrainWrapper.bounds.center).sqrMagnitude * _gridCameraOffset * 0.001f;
                }
                transform.localPosition = localPosition;
                lastPos = transform.position;
                lastScale = transform.lossyScale;
                if (transform.parent != null) {
                    lastParentScale = transform.parent.lossyScale;
                }
                lastCamPos = camPos;
                lastGridElevation = gridElevationCurrent;
                lastGridCameraOffset = _gridCameraOffset;
                lastGridMinElevationMultiplier = _gridMinElevationMultiplier;
            }
        }


        void BuildTerrainWrapper() {
            // migration
            if (_terrain != null && _terrainObject == null) {
                _terrainObject = _terrain.gameObject;
                _terrain = null;
            }
            if (_terrainObject == null) {
                _terrainWrapper = null;
            } else if ((_terrainWrapper == null && _terrainObject != null) || (_terrainWrapper != null && (_terrainWrapper.gameObject != _terrainObject || _terrainWrapper.heightmapWidth != _heightmapSize || _terrainWrapper.heightmapHeight != _heightmapSize))) {
                _terrainWrapper = TerrainWrapperProvider.GetTerrainWrapper(_terrainObject, ref _terrainObjectsPrefix, _heightmapSize, _heightmapSize);
            }
        }

        bool UpdateTerrainReference(bool reuseTerrainData) {

            MeshRenderer renderer = GetComponent<MeshRenderer>();
            renderer.sortingOrder = _sortingOrder;
            if (_terrainWrapper == null) {
                if (renderer.enabled && _transparentBackground) {
                    renderer.enabled = false;
                } else if (!renderer.enabled && !_transparentBackground) {
                    renderer.enabled = true;
                }

                if (transform.parent != null && transform.GetComponentInParent<Terrain>() != null) {
                    transform.SetParent(null);
                    transform.localScale = new Vector3(100, 100, 1);
                    transform.localRotation = Quaternion.Euler(0, 0, 0);
                }
                // Check if box collider exists
                BoxCollider2D bc = GetComponent<BoxCollider2D>();
                if (bc == null) {
                    MeshCollider mc = GetComponent<MeshCollider>();
                    if (mc == null)
                        gameObject.AddComponent<MeshCollider>();
                }
            } else {
                transform.SetParent(_terrainWrapper.transform, false);
                if (renderer.enabled) {
                    renderer.enabled = false;
                }
                if (Application.isPlaying) {
                    _terrainWrapper.SetupTriggers(this);
                }
                MeshCollider mc = GetComponent<MeshCollider>();
                if (mc != null) {
                    DestroyImmediate(mc);
                }
                lastCamPos = cameraMain.transform.position - Vector3.up; // just to force update on first frame
                FitToTerrain();
                lastCamPos = cameraMain.transform.position - Vector3.up; // just to force update on first update as well
                if (CalculateTerrainRoughness(reuseTerrainData)) {
                    refreshCellMesh = true;
                    refreshTerritoriesMesh = true;
                    // Clear geometry
                    if (cellLayer != null) {
                        DestroyImmediate(cellLayer);
                    }
                    if (territoryLayer != null) {
                        DestroyImmediate(territoryLayer);
                    }
                }
            }
            return true;
        }

        /// <summary>
        /// Calculates the terrain roughness.
        /// </summary>
        /// <returns><c>true</c>, if terrain roughness has changed, <c>false</c> otherwise.</returns>
        bool CalculateTerrainRoughness(bool reuseTerrainData) {
            if (reuseTerrainData && _terrainWrapper.heightmapWidth == heightMapWidth && _terrainWrapper.heightmapHeight == heightMapHeight && terrainHeights != null && terrainRoughnessMap != null) {
                return false;
            }
            _terrainWrapper.Refresh();
            heightMapWidth = _terrainWrapper.heightmapWidth;
            heightMapHeight = _terrainWrapper.heightmapHeight;
            terrainHeights = _terrainWrapper.GetHeights(0, 0, heightMapWidth, heightMapHeight);
            terrainRoughnessMapWidth = heightMapWidth / TERRAIN_CHUNK_SIZE;
            terrainRoughnessMapHeight = heightMapHeight / TERRAIN_CHUNK_SIZE;
            int length = terrainRoughnessMapHeight * terrainRoughnessMapWidth;
            if (terrainRoughnessMap == null || terrainRoughnessMap.Length != length) {
                terrainRoughnessMap = new float[length];
                tempTerrainRoughnessMap = new float[length];
            } else {
                for (int k = 0; k < length; k++) {
                    terrainRoughnessMap[k] = 0;
                    tempTerrainRoughnessMap[k] = 0;
                }
            }

#if SHOW_DEBUG_GIZMOS
			if (GameObject.Find ("ParentDot")!=null) DestroyImmediate(GameObject.Find ("ParentDot"));
			GameObject parentDot = new GameObject("ParentDot");
            disposalManager.MarkForDisposal(parentDot);
			parentDot.transform.position = Misc.Vector3zero;
#endif

            float maxStep = (float)TERRAIN_CHUNK_SIZE / heightMapWidth;
            float minStep = 1.0f / heightMapWidth;
            float roughnessFactor = _gridRoughness * 5.0f + 0.00001f;
            for (int y = 0, l = 0; l < terrainRoughnessMapHeight; y += TERRAIN_CHUNK_SIZE, l++) {
                int linePos = l * terrainRoughnessMapWidth;
                for (int x = 0, c = 0; c < terrainRoughnessMapWidth; x += TERRAIN_CHUNK_SIZE, c++) {
                    int j0 = y == 0 ? 1 : y;
                    int j1 = y + TERRAIN_CHUNK_SIZE;
                    int k0 = x == 0 ? 1 : x;
                    int k1 = x + TERRAIN_CHUNK_SIZE;
                    float maxDiff = 0;
                    for (int j = j0; j < j1; j++) {
                        for (int k = k0; k < k1; k++) {
                            float mh = terrainHeights[j, k];
                            float diff = mh - terrainHeights[j, k - 1];
                            if (diff > maxDiff) {
                                maxDiff = diff;
                            } else if (-diff > maxDiff) {
                                maxDiff = -diff;
                            }
                            diff = mh - terrainHeights[j + 1, k - 1];
                            if (diff > maxDiff) {
                                maxDiff = diff;
                            } else if (-diff > maxDiff) {
                                maxDiff = -diff;
                            }
                            diff = mh - terrainHeights[j + 1, k];
                            if (diff > maxDiff) {
                                maxDiff = diff;
                            } else if (-diff > maxDiff) {
                                maxDiff = -diff;
                            }
                            diff = mh - terrainHeights[j + 1, k + 1];
                            if (diff > maxDiff) {
                                maxDiff = diff;
                            } else if (-diff > maxDiff) {
                                maxDiff = -diff;
                            }
                            diff = mh - terrainHeights[j, k + 1];
                            if (diff > maxDiff) {
                                maxDiff = diff;
                            } else if (-diff > maxDiff) {
                                maxDiff = -diff;
                            }
                            diff = mh - terrainHeights[j - 1, k + 1];
                            if (diff > maxDiff) {
                                maxDiff = diff;
                            } else if (-diff > maxDiff) {
                                maxDiff = -diff;
                            }
                            diff = mh - terrainHeights[j - 1, k];
                            if (diff > maxDiff) {
                                maxDiff = diff;
                            } else if (-diff > maxDiff) {
                                maxDiff = -diff;
                            }
                            diff = mh - terrainHeights[j - 1, k - 1];
                            if (diff > maxDiff) {
                                maxDiff = diff;
                            } else if (-diff > maxDiff) {
                                maxDiff = -diff;
                            }
                        }
                    }
                    maxDiff /= roughnessFactor;
                    float t = (1.0f - maxDiff) / (1.0f + maxDiff);
                    if (t <= 0)
                        maxDiff = minStep;
                    else if (t >= 1f)
                        maxDiff = maxStep;
                    else
                        maxDiff = minStep * (1f - t) + maxStep * t; // Mathf.Lerp (minStep, maxStep,t);

                    tempTerrainRoughnessMap[linePos + c] = maxDiff;
                }
            }

            // collapse chunks with low gradient
            float flatThreshold = maxStep * (1.0f - _gridRoughness * 0.1f);
            for (int j = 0; j < terrainRoughnessMapHeight; j++) {
                int jPos = j * terrainRoughnessMapWidth;
                for (int k = 0; k < terrainRoughnessMapWidth - 1; k++) {
                    if (tempTerrainRoughnessMap[jPos + k] >= flatThreshold) {
                        int i = k + 1;
                        while (i < terrainRoughnessMapWidth && tempTerrainRoughnessMap[jPos + i] >= flatThreshold)
                            i++;
                        while (k < i && k < terrainRoughnessMapWidth)
                            tempTerrainRoughnessMap[jPos + k] = maxStep * (i - k++);
                    }
                }
            }

            // spread min step
            for (int l = 0; l < terrainRoughnessMapHeight; l++) {
                int linePos = l * terrainRoughnessMapWidth;
                int prevLinePos = linePos - terrainRoughnessMapWidth;
                int postLinePos = linePos + terrainRoughnessMapWidth;
                for (int c = 0; c < terrainRoughnessMapWidth; c++) {
                    minStep = tempTerrainRoughnessMap[linePos + c];
                    if (l > 0) {
                        if (tempTerrainRoughnessMap[prevLinePos + c] < minStep)
                            minStep = tempTerrainRoughnessMap[prevLinePos + c];
                        if (c > 0)
                            if (tempTerrainRoughnessMap[prevLinePos + c - 1] < minStep)
                                minStep = tempTerrainRoughnessMap[prevLinePos + c - 1];
                        if (c < terrainRoughnessMapWidth - 1)
                            if (tempTerrainRoughnessMap[prevLinePos + c + 1] < minStep)
                                minStep = tempTerrainRoughnessMap[prevLinePos + c + 1];
                    }
                    if (c > 0 && tempTerrainRoughnessMap[linePos + c - 1] < minStep)
                        minStep = tempTerrainRoughnessMap[linePos + c - 1];
                    if (c < terrainRoughnessMapWidth - 1 && tempTerrainRoughnessMap[linePos + c + 1] < minStep)
                        minStep = tempTerrainRoughnessMap[linePos + c + 1];
                    if (l < terrainRoughnessMapHeight - 1) {
                        if (tempTerrainRoughnessMap[postLinePos + c] < minStep)
                            minStep = tempTerrainRoughnessMap[postLinePos + c];
                        if (c > 0)
                            if (tempTerrainRoughnessMap[postLinePos + c - 1] < minStep)
                                minStep = tempTerrainRoughnessMap[postLinePos + c - 1];
                        if (c < terrainRoughnessMapWidth - 1)
                            if (tempTerrainRoughnessMap[postLinePos + c + 1] < minStep)
                                minStep = tempTerrainRoughnessMap[postLinePos + c + 1];
                    }
                    terrainRoughnessMap[linePos + c] = minStep;
                }
            }


#if SHOW_DEBUG_GIZMOS
            for (int l = 0; l < terrainRoughnessMapHeight - 1; l++) {
                for (int c = 0; c < terrainRoughnessMapWidth - 1; c++) {
                    float r = terrainRoughnessMap[l * terrainRoughnessMapWidth + c];
                    GameObject marker = Instantiate(Resources.Load<GameObject>("Prefabs/Dot"));
                    marker.transform.SetParent(parentDot.transform, false);
                    disposalManager.MarkForDisposal(marker);
                    marker.transform.localPosition = new Vector3(-terrainCenter.x + terrainWidth * ((float)c / 64 + 0.5f / 64), r * terrainHeight, -terrainCenter.z + terrainDepth * ((float)l / 64 + 0.5f / 64));
                    marker.transform.localScale = Misc.Vector3one * 350 / 512.0f;

                }
            }
#endif

            return true;
        }


        void UpdateMaterialDepthOffset() {
            if (territories != null) {
                int territoriesCount = territories.Count;
                for (int c = 0; c < territoriesCount; c++) {
                    int cacheIndex = GetCacheIndexForTerritoryRegion(c);
                    GameObject surf;
                    if (surfaces.TryGetValue(cacheIndex, out surf)) {
                        if (surf != null) {
                            Material mat = surf.GetComponent<Renderer>().sharedMaterial;
                            mat.SetInt("_Offset", _gridSurfaceDepthOffsetTerritory);
                            SetBlend(mat);
                        }
                    }
                }
            }
            if (cells != null) {
                int cellsCount = cells.Count;
                for (int c = 0; c < cellsCount; c++) {
                    int cacheIndex = GetCacheIndexForCellRegion(c);
                    GameObject surf;
                    if (surfaces.TryGetValue(cacheIndex, out surf)) {
                        if (surf != null) {
                            Material mat = surf.GetComponent<Renderer>().sharedMaterial;
                            mat.SetInt("_Offset", _gridSurfaceDepthOffset);
                            SetBlend(mat);
                        }
                    }
                }
            }
            float depthOffset = _gridMeshDepthOffset / 10000.0f;

            cellsThinMat.SetFloat("_Offset", depthOffset);
            SetBlend(cellsThinMat);

            cellsGeoMat.SetFloat("_Offset", depthOffset);
            SetBlend(cellsGeoMat);

            territoriesThinMat.SetFloat("_Offset", depthOffset);
            SetBlend(territoriesThinMat);

            territoriesGeoMat.SetFloat("_Offset", depthOffset);
            SetBlend(territoriesGeoMat);

            territoriesDisputedThinMat.SetFloat("_Offset", depthOffset);
            SetBlend(territoriesDisputedThinMat);

            territoriesDisputedGeoMat.SetFloat("_Offset", depthOffset);
            SetBlend(territoriesDisputedGeoMat);

            foreach (Material mat in frontierColorCache.Values) {
                mat.SetFloat("_Offset", depthOffset);
                SetBlend(mat);
            }
            hudMatCellOverlay.SetInt("_Offset", _gridSurfaceDepthOffset);
            hudMatCellGround.SetInt("_Offset", _gridSurfaceDepthOffset - 1);
            hudMatTerritoryOverlay.SetInt("_Offset", _gridSurfaceDepthOffsetTerritory);
            hudMatTerritoryGround.SetInt("_Offset", _gridSurfaceDepthOffsetTerritory - 1);
        }

        void SetBlend(Material mat) {
            if (_transparentBackground) {
                mat.SetInt("_ZWrite", 0);
                mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                if (mat.renderQueue < 3000) {
                    mat.renderQueue += 1000;
                }
            } else {
                mat.SetInt("_ZWrite", 0);
                mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                if (mat.renderQueue >= 3000) {
                    mat.renderQueue -= 1000;
                }
            }
        }

        void SetBlendHighlight(Material mat) {

            if (_transparentBackground) {
                mat.SetInt("_ZWrite", 0);
                mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                if (mat.renderQueue < 3000) {
                    mat.renderQueue += 1000;
                }
            } else {
                mat.SetInt("_ZWrite", 0);
                mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
                mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                if (mat.renderQueue >= 3000) {
                    mat.renderQueue -= 1000;
                }
            }
        }


        void UpdateMaterialNearClipFade() {
            if (_nearClipFadeEnabled && _terrainWrapper != null) {
                cellsThinMat.EnableKeyword(SKW_NEAR_CLIP_FADE);
                cellsThinMat.SetFloat("_NearClip", _nearClipFade);
                cellsThinMat.SetFloat("_FallOff", _nearClipFadeFallOff);

                cellsGeoMat.EnableKeyword(SKW_NEAR_CLIP_FADE);
                cellsGeoMat.SetFloat("_NearClip", _nearClipFade);
                cellsGeoMat.SetFloat("_FallOff", _nearClipFadeFallOff);

                territoriesThinMat.EnableKeyword(SKW_NEAR_CLIP_FADE);
                territoriesThinMat.SetFloat("_NearClip", _nearClipFade);
                territoriesThinMat.SetFloat("_FallOff", _nearClipFadeFallOff);

                territoriesGeoMat.EnableKeyword(SKW_NEAR_CLIP_FADE);
                territoriesGeoMat.SetFloat("_NearClip", _nearClipFade);
                territoriesGeoMat.SetFloat("_FallOff", _nearClipFadeFallOff);

                territoriesDisputedThinMat.EnableKeyword(SKW_NEAR_CLIP_FADE);
                territoriesDisputedThinMat.SetFloat("_NearClip", _nearClipFade);
                territoriesDisputedThinMat.SetFloat("_FallOff", _nearClipFadeFallOff);

                territoriesDisputedGeoMat.EnableKeyword(SKW_NEAR_CLIP_FADE);
                territoriesDisputedGeoMat.SetFloat("_NearClip", _nearClipFade);
                territoriesDisputedGeoMat.SetFloat("_FallOff", _nearClipFadeFallOff);

                foreach (Material mat in frontierColorCache.Values) {
                    mat.EnableKeyword(SKW_NEAR_CLIP_FADE);
                    mat.SetFloat("_NearClip", _nearClipFade);
                    mat.SetFloat("_FallOff", _nearClipFadeFallOff);
                }
            } else {
                cellsThinMat.DisableKeyword(SKW_NEAR_CLIP_FADE);
                cellsGeoMat.DisableKeyword(SKW_NEAR_CLIP_FADE);
                territoriesThinMat.DisableKeyword(SKW_NEAR_CLIP_FADE);
                territoriesGeoMat.DisableKeyword(SKW_NEAR_CLIP_FADE);
                territoriesDisputedThinMat.DisableKeyword(SKW_NEAR_CLIP_FADE);
                territoriesDisputedGeoMat.DisableKeyword(SKW_NEAR_CLIP_FADE);
                foreach (Material mat in frontierColorCache.Values) {
                    mat.DisableKeyword(SKW_NEAR_CLIP_FADE);
                }
            }
        }

        void UpdateMaterialFarFade() {
            if (_farFadeEnabled && _terrainWrapper != null) {
                cellsThinMat.EnableKeyword(SKW_FAR_FADE);
                cellsThinMat.SetFloat("_FarFadeDistance", _farFadeDistance);
                cellsThinMat.SetFloat("_FarFadeFallOff", _farFadeFallOff);

                cellsGeoMat.EnableKeyword(SKW_FAR_FADE);
                cellsGeoMat.SetFloat("_FarFadeDistance", _farFadeDistance);
                cellsGeoMat.SetFloat("_FarFadeFallOff", _farFadeFallOff);

                territoriesThinMat.EnableKeyword(SKW_FAR_FADE);
                territoriesThinMat.SetFloat("_FarFadeDistance", _farFadeDistance);
                territoriesThinMat.SetFloat("_FarFadeFallOff", _farFadeFallOff);

                territoriesGeoMat.EnableKeyword(SKW_FAR_FADE);
                territoriesGeoMat.SetFloat("_FarFadeDistance", _farFadeDistance);
                territoriesGeoMat.SetFloat("_FarFadeFallOff", _farFadeFallOff);

                territoriesDisputedThinMat.EnableKeyword(SKW_FAR_FADE);
                territoriesDisputedThinMat.SetFloat("_FarFadeDistance", _farFadeDistance);
                territoriesDisputedThinMat.SetFloat("_FarFadeFallOff", _farFadeFallOff);

                territoriesDisputedGeoMat.EnableKeyword(SKW_FAR_FADE);
                territoriesDisputedGeoMat.SetFloat("_FarFadeDistance", _farFadeDistance);
                territoriesDisputedGeoMat.SetFloat("_FarFadeFallOff", _farFadeFallOff);

                foreach (Material mat in frontierColorCache.Values) {
                    mat.EnableKeyword(SKW_FAR_FADE);
                    mat.SetFloat("_FarFadeDistance", _farFadeDistance);
                    mat.SetFloat("_FarFadeFallOff", _farFadeFallOff);
                }
            } else {
                cellsThinMat.DisableKeyword(SKW_FAR_FADE);
                cellsGeoMat.DisableKeyword(SKW_FAR_FADE);
                territoriesThinMat.DisableKeyword(SKW_FAR_FADE);
                territoriesGeoMat.DisableKeyword(SKW_FAR_FADE);
                territoriesDisputedThinMat.DisableKeyword(SKW_FAR_FADE);
                territoriesDisputedGeoMat.DisableKeyword(SKW_FAR_FADE);
                foreach (Material mat in frontierColorCache.Values) {
                    mat.DisableKeyword(SKW_FAR_FADE);
                }
            }
        }


        void UpdateHighlightEffect() {
            if (highlightKeywords == null) {
                highlightKeywords = new List<string>();
            } else {
                highlightKeywords.Clear();
            }
            switch (_highlightEffect) {
                case HIGHLIGHT_EFFECT.TextureAdditive:
                    highlightKeywords.Add(SKW_TEX_HIGHLIGHT_ADDITIVE);
                    break;
                case HIGHLIGHT_EFFECT.TextureMultiply:
                    highlightKeywords.Add(SKW_TEX_HIGHLIGHT_MULTIPLY);
                    break;
                case HIGHLIGHT_EFFECT.TextureColor:
                    highlightKeywords.Add(SKW_TEX_HIGHLIGHT_COLOR);
                    break;
                case HIGHLIGHT_EFFECT.TextureScale:
                    highlightKeywords.Add(SKW_TEX_HIGHLIGHT_SCALE);
                    break;
                case HIGHLIGHT_EFFECT.DualColors:
                    highlightKeywords.Add(SKW_TEX_DUAL_COLORS);
                    break;
            }
            if (_transparentBackground) {
                highlightKeywords.Add(SKW_TRANSPARENT);
            }
            string[] keywords = highlightKeywords.ToArray();
            hudMatCellGround.shaderKeywords = keywords;
            hudMatCellOverlay.shaderKeywords = keywords;
            hudMatTerritoryGround.shaderKeywords = keywords;
            hudMatTerritoryOverlay.shaderKeywords = keywords;
            SetBlendHighlight(hudMatCellGround);
            SetBlendHighlight(hudMatCellOverlay);
            SetBlendHighlight(hudMatTerritoryGround);
            SetBlendHighlight(hudMatTerritoryOverlay);
        }

        #endregion

        #region Drawing stuff

        int GetCacheIndexForTerritoryRegion(int territoryIndex) {
            return territoryIndex; // * 1000 + regionIndex;
        }

        Material hudMatTerritory { get { return _overlayMode == OVERLAY_MODE.Overlay ? hudMatTerritoryOverlay : hudMatTerritoryGround; } }

        Material hudMatCell { get { return _overlayMode == OVERLAY_MODE.Overlay ? hudMatCellOverlay : hudMatCellGround; } }

        Material GetColoredTexturedMaterial(SurfaceType surfaceType, Color color, Texture2D texture, bool overlay) {
            Dictionary<Color, Material> matCache;
            if (surfaceType == SurfaceType.Cell) {
                matCache = overlay ? coloredMatCacheOverlayCell : coloredMatCacheGroundCell;
            } else {
                matCache = overlay ? coloredMatCacheOverlayTerritory : coloredMatCacheGroundTerritory;
            }

            Material mat;
            if (texture == null && matCache.TryGetValue(color, out mat)) {
                return mat;
            } else {
                Material customMat;
                if (texture != null) {
                    if (surfaceType == SurfaceType.Cell) {
                        mat = overlay ? texturizedMatOverlayCell : texturizedMatGroundCell;
                    } else {
                        mat = overlay ? texturizedMatOverlayTerritory : texturizedMatGroundTerritory;
                    }
                    customMat = Instantiate(mat);
                    customMat.name = mat.name;
                    customMat.mainTexture = texture;
                } else {
                    if (surfaceType == SurfaceType.Cell) {
                        mat = overlay ? coloredMatOverlayCell : coloredMatGroundCell;
                    } else {
                        mat = overlay ? coloredMatOverlayTerritory : coloredMatGroundTerritory;
                    }
                    customMat = Instantiate(mat);
                    customMat.name = mat.name;
                    matCache[color] = customMat;
                }
                customMat.color = color;
                customMat.SetFloat("_Offset", surfaceType == SurfaceType.Cell ? _gridSurfaceDepthOffset : _gridSurfaceDepthOffsetTerritory);
                SetBlend(customMat);
                disposalManager.MarkForDisposal(customMat);
                return customMat;
            }
        }

        Material GetFrontierColorMaterial(Color color) {
            if (color == territoriesMat.color)
                return territoriesMat;

            Material mat;
            if (frontierColorCache.TryGetValue(color, out mat)) {
                return mat;
            } else {
                Material customMat = Instantiate(territoriesMat);
                customMat.name = territoriesMat.name;
                customMat.color = color;
                disposalManager.MarkForDisposal(customMat);
                frontierColorCache[color] = customMat;
                return customMat;
            }
        }

        void ApplyMaterialToSurface(Region region, Material sharedMaterial) {
            if (region.renderer != null) {
                region.renderer.sharedMaterial = sharedMaterial;
                if (region.childrenSurfaces != null) {
                    int count = region.childrenSurfaces.Count;
                    for (int k = 0; k < count; k++) {
                        Renderer r = region.childrenSurfaces[k];
                        if (r != null) {
                            r.sharedMaterial = sharedMaterial;
                        }
                    }
                }
            }
        }

        void DrawColorizedTerritories() {
            if (territories == null)
                return;
            int territoriesCount = territories.Count;
            for (int k = 0; k < territoriesCount; k++) {
                Territory territory = territories[k];
                if (issueRedraw == RedrawType.IncrementalTerritories && !territory.isDirty) continue;
                Region region = territory.region;
                if (region.customMaterial != null) {
                    TerritoryToggleRegionSurface(k, true, region.customMaterial.color, false, (Texture2D)region.customMaterial.mainTexture, region.customTextureScale, region.customTextureOffset, region.customTextureRotation, region.customRotateInLocalSpace);
                } else {
                    Color fillColor = territories[k].fillColor;
                    fillColor.a *= colorizedTerritoriesAlpha;
                    TerritoryToggleRegionSurface(k, true, fillColor);
                }
            }
        }

        public void GenerateMap(bool reuseTerrainData = false) {
            needGenerateMap = false;
            recreateCells = true;
            recreateTerritories = true;
            if (cells != null)
                cells.Clear();
            if (territories != null)
                territories.Clear();
            Redraw(reuseTerrainData);
            if (territoriesTexture != null) {
                CreateTerritories(territoriesTexture, territoriesTextureNeutralColor, territoriesHideNeutralCells);
            }
            // Reload configuration if component exists
            TGSConfig[] configs = GetComponents<TGSConfig>();
            for (int k = 0; k < configs.Length; k++) {
                if (configs[k].enabled)
                    configs[k].LoadConfiguration();
            }
        }

        void SetScaleByCellSize() {
            if (_gridTopology == GRID_TOPOLOGY.Hexagonal) {
                _gridScale.x = _cellSize.x * (1f + (_cellColumnCount - 1f) * 0.75f) / transform.lossyScale.x;
                _gridScale.y = _cellSize.y * (_cellRowCount + 0.5f) / transform.lossyScale.y;
            } else if (_gridTopology == GRID_TOPOLOGY.Box) {
                _gridScale.x = _cellSize.x * _cellColumnCount / transform.lossyScale.x;
                _gridScale.y = _cellSize.y * _cellRowCount / transform.lossyScale.y;
            }
        }

        void ComputeGridScale() {
            if (_regularHexagons && _gridTopology == GRID_TOPOLOGY.Hexagonal) {
                _gridScale = new Vector2(1f + (_cellColumnCount - 1f) * 0.75f, (_cellRowCount + 0.5f) * 0.8660254f); // cos(60), sqrt(3)/2
                float hexScale = Mathf.Max(0.00001f, _regularHexagonsWidth / transform.lossyScale.x);
                _gridScale.x *= hexScale;
                _gridScale.y *= hexScale;
                float aspectRatio = Mathf.Clamp(transform.lossyScale.x / transform.lossyScale.y, 0.01f, 10f);
                _gridScale.x = Mathf.Max(_gridScale.x, 0.0001f);
                _gridScale.y *= aspectRatio;
            }
            _gridScale.x = Mathf.Max(_gridScale.x, 0.0001f);
            _gridScale.y = Mathf.Max(_gridScale.y, 0.0001f);
            _cellSize.x = transform.lossyScale.x * _gridScale.x / _cellColumnCount;
            _cellSize.y = transform.lossyScale.y * _gridScale.y / _cellRowCount;
        }

        /// <summary>
        /// Refresh grid.
        /// </summary>
        public void Redraw() {
            BuildTerrainWrapper();

            if (issueRedraw == RedrawType.None) {
                issueRedraw = RedrawType.Full;
            }
            CheckChanges();
        }


        /// <summary>
        /// Removes any color/texture from all cells and territories
        /// </summary>
        public void ClearAll() {
            if (cells != null) {
                int cellsCount = cells.Count;
                for (int k = 0; k < cellsCount; k++) {
                    if (cells[k] != null && cells[k].region != null) {
                        cells[k].region.customMaterial = null;
                    }
                }
            }
            if (territories != null) {
                int territoriesCount = territories.Count;
                for (int k = 0; k < territoriesCount; k++) {
                    if (territories[k].region != null) {
                        territories[k].region.customMaterial = null;
                    }
                }
            }
            DestroySurfaces();
        }

        /// <summary>
        /// Refresh grid. Set reuseTerrainData to true to avoid computation of terrain heights and slope (useful if terrain is not changed).
        /// </summary>
        public void Redraw(bool reuseTerrainData) {

            if (!gameObject.activeInHierarchy || redrawing)
                return;

            redrawing = true;

            if (surfaces == null) {
                surfaces = new Dictionary<int, GameObject>();
            }
            if (issueRedraw == RedrawType.IncrementalTerritories) {
                DestroySurfacesDirty();
            } else {
                // Initialize surface cache
                List<GameObject> cached = new List<GameObject>(surfaces.Values);
                int cachedCount = cached.Count;
                for (int k = 0; k < cachedCount; k++) {
                    if (cached[k] != null) {
                        DestroyImmediate(cached[k]);
                    }
                }
                DestroySurfaces();
            }

            ClearLastOver();

            if (UpdateTerrainReference(reuseTerrainData)) {
                if (issueRedraw != RedrawType.IncrementalTerritories) {
                    refreshCellMesh = true;
                    _lastVertexCount = 0;
                    ComputeGridScale();
                    CheckCells();
                    if (_showCells) {
                        DrawCellBorders();
                    }
                    DrawColorizedCells();
                }

                refreshTerritoriesMesh = true;
                CheckTerritories();
                if (_showTerritories) {
                    DrawTerritoryFrontiers();
                }
                if (_colorizeTerritories) {
                    DrawColorizedTerritories();
                }
                UpdateMaterialDepthOffset();
                UpdateMaterialNearClipFade();
                UpdateMaterialFarFade();
                UpdateHighlightEffect();
            }

            if (issueRedraw == RedrawType.IncrementalTerritories) {
                int territoryCount = territories.Count;
                for (int k = 0; k < territoryCount; k++) {
                    Territory territory = territories[k];
                    territory.isDirty = false;
                }
            }
            issueRedraw = RedrawType.None;
            redrawing = false;
        }

        void CheckCells() {
            if (!_showCells && !_showTerritories && !_colorizeTerritories && _highlightMode == HIGHLIGHT_MODE.None)
                return;
            if (cells == null || recreateCells) {
                CreateCells();
                refreshCellMesh = true;
            }
            if (refreshCellMesh) {
                DestroyCellLayer();
                CellsApplyVisibilityFilters();
                GenerateCellsMesh(cells);
                refreshCellMesh = false;
                refreshTerritoriesMesh = true;
            }
        }

        void DestroyCellLayer() {
            if (cellLayer != null) {
                DestroyImmediate(cellLayer);
            } else {
                Transform t = transform.Find(CELLS_LAYER_NAME);
                if (t != null)
                    DestroyImmediate(t.gameObject);
            }
        }

        bool isUsingVertexDispForVertexThickness { get { return _cellBorderThickness > 1 && !canUseGeometryShaders;  } }
        bool isUsingVertexDispForTerritoryThickness { get { return _territoryFrontiersThickness > 1 && !canUseGeometryShaders; } }

        void DrawCellBorders() {

            DestroyCellLayer();
            if (cells.Count == 0)
                return;

            cellLayer = new GameObject(CELLS_LAYER_NAME);
            disposalManager.MarkForDisposal(cellLayer);
            cellLayer.transform.SetParent(transform, false);
            cellLayer.transform.localPosition = Vector3.back * 0.001f * _gridMinElevationMultiplier;

            for (int k = 0; k < cellMeshBorders.Length; k++) {
                GameObject flayer = new GameObject("flayer");
                disposalManager.MarkForDisposal(flayer);
                flayer.hideFlags |= HideFlags.HideInHierarchy;
                flayer.transform.SetParent(cellLayer.transform, false);
                flayer.transform.localPosition = Misc.Vector3zero;
                flayer.transform.localRotation = Quaternion.Euler(Misc.Vector3zero);

                Mesh mesh = new Mesh();
                if (isUsingVertexDispForVertexThickness) {
                    ComputeExplodedVertices(mesh, cellMeshBorders[k]);
                } else {
                    mesh.vertices = cellMeshBorders[k];
                    mesh.SetIndices(cellMeshIndices[k], MeshTopology.Lines, 0);
                }
                disposalManager.MarkForDisposal(mesh);

                MeshFilter mf = flayer.AddComponent<MeshFilter>();
                mf.sharedMesh = mesh;
                _lastVertexCount += mesh.vertexCount;

                MeshRenderer mr = flayer.AddComponent<MeshRenderer>();
                mr.sortingOrder = _sortingOrder;
                mr.receiveShadows = false;
                mr.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;
                mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                mr.sharedMaterial = cellsMat;
            }
            cellLayer.SetActive(_showCells);
            cellsGeoMat.SetFloat("_Thickness", (_cellBorderThickness - 0.8f) * transform.lossyScale.x / 500f);
        }

        void ComputeExplodedVertices(Mesh mesh, Vector3[] vertices) {
            tempPoints.Clear();
            tempUVs.Clear();
            tempIndices.Clear();
            Vector4 uv;
            for (int k = 0; k < vertices.Length; k += 2) {
                // Triangle points
                tempPoints.Add(vertices[k]);
                tempPoints.Add(vertices[k]);
                tempPoints.Add(vertices[k + 1]);
                tempPoints.Add(vertices[k + 1]);
                uv = vertices[k + 1]; uv.w = -1; tempUVs.Add(uv);
                uv = vertices[k + 1]; uv.w = 1; tempUVs.Add(uv);
                uv = vertices[k]; uv.w = 1; tempUVs.Add(uv);
                uv = vertices[k]; uv.w = -1; tempUVs.Add(uv);
                // First triangle
                tempIndices.Add(k * 2);
                tempIndices.Add(k * 2 + 1);
                tempIndices.Add(k * 2 + 2);
                // Second triangle
                tempIndices.Add(k * 2 + 1);
                tempIndices.Add(k * 2 + 3);
                tempIndices.Add(k * 2 + 2);
            }
            mesh.SetVertices(tempPoints);
            mesh.SetUVs(0, tempUVs);
            mesh.SetTriangles(tempIndices, 0);
        }

        void DrawColorizedCells() {
            if (cells == null)
                return;
            int cellsCount = cells.Count;
            for (int k = 0; k < cellsCount; k++) {
                Cell cell = cells[k];
                if (cell == null) continue;
                Region region = cell.region;
                if (region.customMaterial != null && cell.visible) {
                    CellToggleRegionSurface(k, true, region.customMaterial.color, false, (Texture2D)region.customMaterial.mainTexture, region.customTextureScale, region.customTextureOffset, region.customTextureRotation, region.customRotateInLocalSpace);
                }
            }
        }

        void CheckTerritories() {
            if (!territoriesAreUsed)
                return;
            if (territories == null || recreateTerritories) {
                CreateTerritories();
                refreshTerritoriesMesh = true;
            } else if (needUpdateTerritories) {
                FindTerritoryFrontiers();
                UpdateTerritoryBoundaries();
                needUpdateTerritories = false;
                refreshTerritoriesMesh = true;
            }

            if (refreshTerritoriesMesh) {
                DestroyTerritoryLayer();
                GenerateTerritoriesMesh();
                refreshTerritoriesMesh = false;
            }

        }

        void DestroyTerritoryLayer() {
            if (territoryLayer != null) {
                DestroyImmediate(territoryLayer);
            } else {
                Transform t = transform.Find(TERRITORIES_LAYER_NAME);
                if (t != null)
                    DestroyImmediate(t.gameObject);
            }
        }

        void DrawTerritoryFrontiers() {
            DestroyTerritoryLayer();
            if (territories.Count == 0)
                return;

            territoryLayer = new GameObject(TERRITORIES_LAYER_NAME);
            disposalManager.MarkForDisposal(territoryLayer);
            territoryLayer.transform.SetParent(transform, false);
            territoryLayer.transform.localPosition = Vector3.back * 0.001f * _gridMinElevationMultiplier;

            for (int t = 0; t < territoryMeshes.Count; t++) {
                TerritoryMesh tm = territoryMeshes[t];

                for (int k = 0; k < tm.territoryMeshBorders.Length; k++) {
                    GameObject flayer = new GameObject("flayer");
                    disposalManager.MarkForDisposal(flayer);
                    flayer.hideFlags |= HideFlags.HideInHierarchy;
                    flayer.transform.SetParent(territoryLayer.transform, false);
                    flayer.transform.localPosition = new Vector3(0,0,-0.001f * _gridMinElevationMultiplier);
                    flayer.transform.localRotation = Quaternion.Euler(Misc.Vector3zero);

                    Mesh mesh = new Mesh();
                    if (isUsingVertexDispForTerritoryThickness) {
                        ComputeExplodedVertices(mesh, tm.territoryMeshBorders[k]);
                    } else {
                        mesh.vertices = tm.territoryMeshBorders[k];
                        mesh.SetIndices(tm.territoryMeshIndices[k], MeshTopology.Lines, 0);
                    }

                    disposalManager.MarkForDisposal(mesh);

                    MeshFilter mf = flayer.AddComponent<MeshFilter>();
                    mf.sharedMesh = mesh;
                    _lastVertexCount += mesh.vertexCount;

                    MeshRenderer mr = flayer.AddComponent<MeshRenderer>();
                    mr.sortingOrder = _sortingOrder;
                    mr.receiveShadows = false;
                    mr.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;
                    mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

                    Material mat;
                    if (tm.territoryIndex < 0) {
                        mat = territoriesDisputedMat;
                    } else {
                        Color frontierColor = territories[tm.territoryIndex].frontierColor;
                        if (frontierColor.a == 0 && frontierColor.r == 0 && frontierColor.g == 0 && frontierColor.b == 0) {
                            mat = territoriesMat;
                        } else {
                            mat = GetFrontierColorMaterial(frontierColor);
                        }
                    }
                    mr.sharedMaterial = mat;
                }
            }

            territoryLayer.SetActive(_showTerritories);
            float thick = (_territoryFrontiersThickness - 0.8f) * transform.lossyScale.x / 500f;
            territoriesGeoMat.SetFloat("_Thickness", thick);
            territoriesDisputedGeoMat.SetFloat("_Thickness", thick);
        }

        void PrepareNewSurfaceMesh(int pointCount) {
            if (meshPoints == null) {
                meshPoints = new List<Vector3>(pointCount);
            } else {
                meshPoints.Clear();
            }
            if (triNew == null || triNew.Length != pointCount) {
                triNew = new int[pointCount];
            }
            if (surfaceMeshHit == null)
                surfaceMeshHit = new Dictionary<TriangulationPoint, int>(20000);
            else {
                surfaceMeshHit.Clear();
            }

            triNewIndex = -1;
            newPointsCount = -1;
        }

        void AddPointToSurfaceMeshWithNormalOffset(TriangulationPoint p) {
            int tri;
            if (surfaceMeshHit.TryGetValue(p, out tri)) {
                triNew[++triNewIndex] = tri;
            } else {
                Vector3 np;
                np.x = p.Xf - 2;
                np.y = p.Yf - 2;
                np.z = -p.Zf;
                np += transform.InverseTransformVector(_terrainWrapper.GetInterpolatedNormal(np.x + 0.5f, np.y + 0.5f)) * _gridNormalOffset;
                meshPoints.Add(np);
                surfaceMeshHit[p] = ++newPointsCount;
                triNew[++triNewIndex] = newPointsCount;
            }
        }

        void AddPointToSurfaceMeshWithoutNormalOffset(TriangulationPoint p) {
            int tri;
            if (surfaceMeshHit.TryGetValue(p, out tri)) {
                triNew[++triNewIndex] = tri;
            } else {
                Vector3 np;
                np.x = p.Xf - 2;
                np.y = p.Yf - 2;
                np.z = -p.Zf;
                meshPoints.Add(np);
                surfaceMeshHit[p] = ++newPointsCount;
                triNew[++triNewIndex] = newPointsCount;
            }
        }


        void AddPointToSurfaceMeshWithNormalOffsetPrimitive(TriangulationPoint p) {
            Vector3 np;
            np.x = p.Xf - 2;
            np.y = p.Yf - 2;
            np.z = -p.Zf;
            np += transform.InverseTransformVector(_terrainWrapper.GetInterpolatedNormal(np.x + 0.5f, np.y + 0.5f)) * _gridNormalOffset;
            meshPoints.Add(np);
        }

        void AddPointToSurfaceMeshWithoutNormalOffsetPrimitive(TriangulationPoint p) {
            Vector3 np;
            np.x = p.Xf - 2;
            np.y = p.Yf - 2;
            np.z = -p.Zf;
            meshPoints.Add(np);
        }


        Poly2Tri.Polygon GetPolygon(Region region, ref PolygonPoint[] polyPoints, out int pointCount, bool reduce = false) {
            // Calculate region's surface points
            pointCount = 0;
            int numSegments = region.segments.Count;
            if (numSegments == 0)
                return null;

            tempConnector.Clear();
            if (_gridScale.x == 1f && _gridScale.y == 1f && _gridCenter.x == 0 && _gridCenter.y == 0) {
                // non scaling segments
                if (_terrainWrapper == null) {
                    for (int i = 0; i < numSegments; i++) {
                        Segment s = region.segments[i];
                        tempConnector.Add(s);
                    }
                } else {
                    for (int i = 0; i < numSegments; i++) {
                        Segment s = region.segments[i];
                        SurfaceSegmentForSurface(s, tempConnector);
                    }
                }
            } else {
                // scaling segments
                if (_terrainWrapper == null) {
                    for (int i = 0; i < numSegments; i++) {
                        Segment s = region.segments[i];
                        tempConnector.Add(GetScaledSegment(s));
                    }
                } else {
                    for (int i = 0; i < numSegments; i++) {
                        Segment s = region.segments[i];
                        SurfaceSegmentForSurface(GetScaledSegment(s), tempConnector);
                    }
                }
            }
            Geom.Polygon surfacedPolygon = tempConnector.ToPolygonFromLargestLineStrip();
            if (surfacedPolygon == null)
                return null;

            List<Point> surfacedPoints = surfacedPolygon.contours[0].points;

            int spCount = surfacedPoints.Count;
            double midx = 0, midy = 0;
            if (polyPoints == null || polyPoints.Length < spCount) {
                polyPoints = new PolygonPoint[spCount];
            }

            bool usesTerrain = _terrainWrapper != null;
            for (int k = 0; k < spCount; k++) {
                double x = surfacedPoints[k].x;
                double y = surfacedPoints[k].y;
                if (!IsTooNearPolygon(x, y, polyPoints, pointCount)) {
                    float h = usesTerrain ? _terrainWrapper.SampleHeight(transform.TransformPoint((float)x, (float)y, 0)) : 0;
                    // these additions are required to prevent issues with polytri library
                    x += 2 + k / 1000000.0;
                    y += 2 + k / 1000000.0;
                    polyPoints[pointCount++] = new PolygonPoint(x, y, h);
                    midx += x;
                    midy += y;
                }
            }

            if (pointCount < 3)
                return null;
            if (reduce) {
                midx /= pointCount;
                midy /= pointCount;
                for (int k = 0; k < pointCount; k++) {
                    PolygonPoint p = polyPoints[k];
                    double DX = midx - p.X;
                    double DY = midy - p.Y;
                    polyPoints[k] = new PolygonPoint(p.X + DX * 0.0001, p.Y + DY * 0.0001, p.Zf);
                }
            }
            return new Poly2Tri.Polygon(polyPoints, pointCount);
        }

        GameObject GenerateRegionSurface(Region region, int cacheIndex, Material material, Vector2 textureScale, Vector2 textureOffset, float textureRotation, bool rotateInLocalSpace) {

            try {
                // Deletes potential residual surface
                if (region.surfaceGameObject != null) {
                    DestroyImmediate(region.surfaceGameObject);
                }

                int pointCount;
                Poly2Tri.Polygon poly = GetPolygon(region, ref tempPolyPoints, out pointCount);
                if (poly == null)
                    return null;

                // Support for internal territories
                bool hasHole = false;
                if (_allowTerritoriesInsideTerritories && region.entity is Territory) {
                    int terrCount = territories.Count;
                    for (int ot = 0; ot < terrCount; ot++) {
                        Territory oter = territories[ot];
                        if (oter.region != region && region.ContainsRegion(oter.region)) {
                            int dummy;
                            Poly2Tri.Polygon oterPoly = GetPolygon(oter.region, ref tempPolyPoints2, out dummy, true);
                            if (oterPoly != null) {
                                poly.AddHole(oterPoly);
                                hasHole = true;
                            }
                        }
                    }
                }

                if (!hasHole) {
                    if (_gridTopology == GRID_TOPOLOGY.Hexagonal && pointCount == 6) {
                        return GenerateRegionSurfaceSimpleHex(poly, region, cacheIndex, material, textureScale, textureOffset, textureRotation, rotateInLocalSpace);
                    } else if (_gridTopology == GRID_TOPOLOGY.Box && pointCount == 4) {
                        return GenerateRegionSurfaceSimpleQuad(poly, region, cacheIndex, material, textureScale, textureOffset, textureRotation, rotateInLocalSpace);
                    }
                }

                if (_terrainWrapper != null) {
                    if (steinerPoints == null) {
                        steinerPoints = new List<TriangulationPoint>(6000);
                    } else {
                        steinerPoints.Clear();
                    }

                    float stepX = 1.0f / heightMapWidth;
                    float smallStep = 1.0f / heightMapWidth;
                    float y = region.rect2D.yMin + smallStep;
                    float ymax = region.rect2D.yMax - smallStep;
                    if (acumY == null || acumY.Length != terrainRoughnessMapWidth) {
                        acumY = new float[terrainRoughnessMapWidth];
                    } else {
                        Array.Clear(acumY, 0, terrainRoughnessMapWidth);
                    }
                    int steinerPointsCount = 0;
                    while (y < ymax) {
                        int j = (int)((y + 0.5f) * terrainRoughnessMapHeight);
                        if (j >= terrainRoughnessMapHeight)
                            j = terrainRoughnessMapHeight - 1;
                        else if (j < 0)
                            j = 0;
                        int jPos = j * terrainRoughnessMapWidth;
                        float sy = y + 2;
                        float xin, xout;
                        GetFirstAndLastPointInRow(sy, pointCount, out xin, out xout);
                        xin += smallStep;
                        xout -= smallStep;
                        int k0 = -1;
                        for (float x = xin; x < xout; x += stepX) {
                            int k = (int)((x + 0.5f) * terrainRoughnessMapWidth); //)) / TERRAIN_CHUNK_SIZE;
                            if (k >= terrainRoughnessMapWidth)
                                k = terrainRoughnessMapWidth - 1;
                            else if (k < 0)
                                k = 0;
                            if (k0 != k) {
                                k0 = k;
                                stepX = terrainRoughnessMap[jPos + k];
                                if (acumY[k] >= stepX)
                                    acumY[k] = 0;
                                acumY[k] += smallStep;
                            }
                            if (acumY[k] >= stepX) {
                                // Gather precision height
                                float h = _terrainWrapper.SampleHeight(transform.TransformPoint(x, y, 0));
                                float htl = _terrainWrapper.SampleHeight(transform.TransformPoint(x - smallStep, y + smallStep, 0));
                                if (htl > h)
                                    h = htl;
                                float htr = _terrainWrapper.SampleHeight(transform.TransformPoint(x + smallStep, y + smallStep, 0));
                                if (htr > h)
                                    h = htr;
                                float hbr = _terrainWrapper.SampleHeight(transform.TransformPoint(x + smallStep, y - smallStep, 0));
                                if (hbr > h)
                                    h = hbr;
                                float hbl = _terrainWrapper.SampleHeight(transform.TransformPoint(x - smallStep, y - smallStep, 0));
                                if (hbl > h)
                                    h = hbl;
                                steinerPoints.Add(new PolygonPoint(x + 2, sy, h));
                                steinerPointsCount++;
                            }
                        }
                        y += smallStep;
                        if (steinerPointsCount > 80000) {
                            Debug.LogWarning("Terrain Grid System: number of adaptation points exceeded. Try increasing the Roughness or the number of points in polygon.");
                            break;
                        }
                    }
                    poly.AddSteinerPoints(steinerPoints);
                }

                P2T.Triangulate(poly);

                Rect rect = (canvasTexture != null && material != null && material.mainTexture == canvasTexture) ? canvasRect : region.rect2D;

                // Calculate & optimize mesh data
                int triCount = poly.Triangles.Count;
                GameObject parentSurf = null;

                for (int triBase = 0; triBase < triCount; triBase += 65500) {
                    int meshTriCount = 65500;
                    if (triBase + meshTriCount > triCount) {
                        meshTriCount = triCount - triBase;
                    }

                    PrepareNewSurfaceMesh(meshTriCount * 3);

                    if (_gridNormalOffset > 0 && _terrainWrapper != null) {
                        for (int k = 0; k < meshTriCount; k++) {
                            DelaunayTriangle dt = poly.Triangles[k + triBase];
                            AddPointToSurfaceMeshWithNormalOffset(dt.Points[0]);
                            AddPointToSurfaceMeshWithNormalOffset(dt.Points[2]);
                            AddPointToSurfaceMeshWithNormalOffset(dt.Points[1]);
                        }
                    } else {
                        for (int k = 0; k < meshTriCount; k++) {
                            DelaunayTriangle dt = poly.Triangles[k + triBase];
                            AddPointToSurfaceMeshWithoutNormalOffset(dt.Points[0]);
                            AddPointToSurfaceMeshWithoutNormalOffset(dt.Points[2]);
                            AddPointToSurfaceMeshWithoutNormalOffset(dt.Points[1]);
                        }
                    }
                    if (_cellsMinimumAltitudeClampVertices && _cellsMinimumAltitude != 0 && _terrainWrapper != null) {
                        float localMinima = -(_cellsMinimumAltitude - _terrainWrapper.bounds.min.y) / _terrainWrapper.transform.lossyScale.y;
                        int meshPointsCount = meshPoints.Count;
                        for (int k = 0; k < meshPointsCount; k++) {
                            Vector3 p = meshPoints[k];
                            if (p.z > localMinima) {
                                p.z = localMinima;
                                meshPoints[k] = p;
                            }
                        }
                    }

                    string surfName;
                    if (triBase == 0) {
                        surfName = cacheIndex.ToString();
                    } else {
                        surfName = "splitMesh";
                    }
                    Renderer surfRenderer = Drawing.CreateSurface(surfName, meshPoints, triNew, material, rect, textureScale, textureOffset, textureRotation, rotateInLocalSpace, _sortingOrder, disposalManager);
                    GameObject surf = surfRenderer.gameObject;
                    _lastVertexCount += meshPoints.Count;
                    if (triBase == 0) {
                        surf.transform.SetParent(surfacesLayer.transform, false);
                        surfaces[cacheIndex] = surf;
                        if (region.childrenSurfaces != null) {
                            region.childrenSurfaces.Clear();
                        }
                        region.renderer = surfRenderer;
                    } else {
                        surf.transform.SetParent(parentSurf.transform, false);
                        if (region.childrenSurfaces == null) {
                            region.childrenSurfaces = new List<Renderer>();
                        }
                        region.childrenSurfaces.Add(surfRenderer);
                    }
                    surf.transform.localPosition = Misc.Vector3zero;
                    surf.layer = gameObject.layer;
                    parentSurf = surf;
                }
                return parentSurf;
            } catch {
                return null;
            }
        }


        GameObject GenerateRegionSurfaceSimpleHex(Poly2Tri.Polygon poly, Region region, int cacheIndex, Material material, Vector2 textureScale, Vector2 textureOffset, float textureRotation, bool rotateInLocalSpace) {

            // Calculate & optimize mesh data
            PrepareNewSurfaceMesh(6);
            IList<TriangulationPoint> points = (IList<TriangulationPoint>)poly.Points;
            if (_gridNormalOffset > 0 && _terrainWrapper != null) {
                for (int k = 0; k < 6; k++) {
                    AddPointToSurfaceMeshWithNormalOffsetPrimitive(points[k]);
                }
            } else {
                for (int k = 0; k < 6; k++) {
                    AddPointToSurfaceMeshWithoutNormalOffsetPrimitive(points[k]);
                }
            }
            Rect rect = (canvasTexture != null && material != null && material.mainTexture == canvasTexture) ? canvasRect : region.rect2D;
            Renderer surfRenderer = Drawing.CreateSurface(cacheIndex.ToString(), meshPoints, hexIndices, material, rect, textureScale, textureOffset, textureRotation, rotateInLocalSpace, _sortingOrder, disposalManager);
            GameObject surf = surfRenderer.gameObject;
            _lastVertexCount += 6;
            surf.transform.SetParent(surfacesLayer.transform, false);
            surfaces[cacheIndex] = surf;
            surf.transform.localPosition = Misc.Vector3zero;
            surf.layer = gameObject.layer;
            region.renderer = surfRenderer;
            return surf;
        }


        GameObject GenerateRegionSurfaceSimpleQuad(Poly2Tri.Polygon poly, Region region, int cacheIndex, Material material, Vector2 textureScale, Vector2 textureOffset, float textureRotation, bool rotateInLocalSpace) {

            // Calculate & optimize mesh data
            PrepareNewSurfaceMesh(4);
            IList<TriangulationPoint> points = poly.Points;
            if (_gridNormalOffset > 0 && _terrainWrapper != null) {
                for (int k = 0; k < 4; k++) {
                    AddPointToSurfaceMeshWithNormalOffsetPrimitive(points[k]);
                }
            } else {
                for (int k = 0; k < 4; k++) {
                    AddPointToSurfaceMeshWithoutNormalOffsetPrimitive(points[k]);
                }
            }
            Rect rect = (canvasTexture != null && material != null && material.mainTexture == canvasTexture) ? canvasRect : region.rect2D;
            Renderer surfRenderer = Drawing.CreateSurface(cacheIndex.ToString(), meshPoints, quadIndices, material, rect, textureScale, textureOffset, textureRotation, rotateInLocalSpace, _sortingOrder, disposalManager);
            GameObject surf = surfRenderer.gameObject;
            _lastVertexCount += 4;
            surf.transform.SetParent(surfacesLayer.transform, false);
            surfaces[cacheIndex] = surf;
            surf.transform.localPosition = Misc.Vector3zero;
            surf.layer = gameObject.layer;
            region.renderer = surfRenderer;
            return surf;
        }



        #endregion


        #region Internal API

        int GetFittedSizeForHeightmap(int value) {
            value = (int)Mathf.Log(value, 2);
            value = (int)Mathf.Pow(2, value);
            return value + 1;
        }


        public string GetMapData() {
            return "";
        }

        float goodGridRelaxation {
            get {
                if (_numCells >= MAX_CELLS_FOR_RELAXATION) {
                    return 1;
                } else {
                    return _gridRelaxation;
                }
            }
        }

        float goodGridCurvature {
            get {
                if (_numCells >= MAX_CELLS_FOR_CURVATURE) {
                    return 0;
                } else {
                    return _gridCurvature;
                }
            }
        }

        /// <summary>
        /// Issues a selection check based on a given ray. Used by editor to manipulate cells from Scene window.
        /// Returns true if ray hits the grid.
        /// </summary>
        public bool CheckRay(Ray ray) {
            useEditorRay = true;
            editorRay = ray;
            return CheckMousePos();
        }


        #endregion


        #region Highlighting

        void OnMouseEnter() {
            mouseIsOver = true;
            ClearLastOver();
        }

        void OnMouseExit() {
            // Make sure it's outside of grid
            Vector3 mousePos = Input.mousePosition;
            Ray ray = cameraMain.ScreenPointToRay(mousePos);
            int hitCount = Physics.RaycastNonAlloc(ray, hits);
            if (hitCount > 0) {
                for (int k = 0; k < hitCount; k++) {
                    if (hits[k].collider.gameObject == gameObject)
                        return;
                }
            }
            mouseIsOver = false;
            ClearLastOver();
        }

        void ClearLastOver() {
            NotifyExitingEntities();
            _cellLastOver = null;
            _cellLastOverIndex = -1;
            _territoryLastOver = null;
            _territoryLastOverIndex = -1;
        }

        void NotifyExitingEntities() {
            if (_territoryLastOverIndex >= 0 && OnTerritoryExit != null)
                OnTerritoryExit(this, _territoryLastOverIndex);
            if (_cellLastOverIndex >= 0 && OnCellExit != null)
                OnCellExit(this, _cellLastOverIndex);
        }


        /// <summary>
        /// When using grid on a group of gameobjects, if the parent is not a mesh, it won't hold a collider so this method check collision with the grid plane
        /// </summary>
        /// <param name="localPoint"></param>
        /// <returns></returns>
        bool GetBaseLocalHitFromMousePos(Ray ray, out Vector3 localPoint) {
            localPoint = Misc.Vector3zero;

            if (_terrainWrapper == null) return false; // returns if grid is not over custom gameobjects

            // intersection with grid plane
            Plane gridPlane = new Plane(transform.forward, transform.position);
            float enter;
            if (gridPlane.Raycast(ray, out enter)) {
                Vector3 position = ray.origin + ray.direction * enter;
                localPoint = transform.InverseTransformPoint(position);
                return (localPoint.x >= -0.5f && localPoint.x <= 0.5f && localPoint.y >= -0.5f && localPoint.y <= 0.5f);
            }
            return false;
        }

        bool GetLocalHitFromMousePos(out Vector3 localPoint) {

            Ray ray;
            localPoint = Misc.Vector3zero;

            if (useEditorRay && !Application.isPlaying) {
                ray = editorRay;
            } else {
                Vector3 mousePos = Input.mousePosition;
                if (mousePos.x < 0 || mousePos.x > Screen.width || mousePos.y < 0 || mousePos.y > Screen.height) {
                    localPoint = Misc.Vector3zero;
                    return false;
                }
                ray = cameraMain.ScreenPointToRay(mousePos);
                if (!mouseIsOver && !Application.isMobilePlatform) {
                    return GetBaseLocalHitFromMousePos(ray, out localPoint);
                }
            }
            int hitCount = Physics.RaycastNonAlloc(ray, hits);
            if (hitCount > 0) {
                if (_terrainWrapper != null) {
                    float minDistance = _highlightMinimumTerrainDistance * _highlightMinimumTerrainDistance;
                    for (int k = 0; k < hitCount; k++) {
                        GameObject o = hits[k].collider.gameObject;
                        if (_terrainWrapper.Contains(o)) {
                            if ((hits[k].point - ray.origin).sqrMagnitude > minDistance) {
                                localPoint = _terrainWrapper.GetLocalPoint(o, hits[k].point);
                                return true;
                            }
                        }
                    }
                } else {
                    for (int k = 0; k < hitCount; k++) {
                        if (hits[k].collider.gameObject == gameObject) {
                            localPoint = transform.InverseTransformPoint(hits[k].point);
                            return true;
                        }
                    }
                }
            }
            return GetBaseLocalHitFromMousePos(ray, out localPoint);
        }

        bool GetLocalHitFromWorldPosition(ref Vector3 position) {
            if (_terrainWrapper != null) {
                Ray ray = new Ray(position - transform.forward * 100, transform.forward);
                int hitCount = Physics.RaycastNonAlloc(ray, hits);
                bool goodHit = false;
                if (hitCount > 0) {
                    for (int k = 0; k < hitCount; k++) {
                        if (hits[k].collider.gameObject == _terrainWrapper.gameObject) {
                            position = _terrainWrapper.GetLocalPoint(hits[k].collider.gameObject, hits[k].point);
                            goodHit = true;
                            break;
                        }
                    }
                }
                if (!goodHit) {
                    // defaults to base grid
                    position = transform.InverseTransformPoint(position);
                    return (position.x >= -0.5f && position.x <= 0.5f && position.y >= -0.5f && position.y <= 0.5f);
                }
            } else {
                position = transform.InverseTransformPoint(position);
            }
            return true;
        }

        bool CheckMousePos() {
            if (_highlightMode == HIGHLIGHT_MODE.None || (!Application.isPlaying && !useEditorRay))
                return false;

            Vector3 localPoint;
            bool goodHit = GetLocalHitFromMousePos(out localPoint);
            if (!goodHit) {
                NotifyExitingEntities();
                HideTerritoryRegionHighlight();
                ClearLastOver();
                return false;
            }

            // verify if last highlighted territory remains active
            bool sameTerritoryHighlight = false;
            float sameTerritoryArea = float.MaxValue;
            if (_territoryLastOver != null) {
                if (_territoryLastOver.visible && _territoryLastOver.region.Contains(localPoint.x, localPoint.y)) {
                    sameTerritoryHighlight = true;
                    sameTerritoryArea = _territoryLastOver.region.rect2DArea;
                }
            }
            int newTerritoryHighlightedIndex = -1;

            // mouse if over the grid - verify if hitPos is inside any territory polygon
            if (territories != null) {
                int terrCount = sortedTerritories.Count;
                for (int c = 0; c < terrCount; c++) {
                    Region sreg = _sortedTerritories[c].region;
                    if (sreg != null && _sortedTerritories[c].visible && _sortedTerritories[c].cells != null && _sortedTerritories[c].cells.Count > 0) {
                        if (sreg.Contains(localPoint.x, localPoint.y)) {
                            newTerritoryHighlightedIndex = TerritoryGetIndex(_sortedTerritories[c]);
                            sameTerritoryHighlight = newTerritoryHighlightedIndex == _territoryLastOverIndex;
                            break;
                        }
                        if (sreg.rect2DArea > sameTerritoryArea)
                            break;
                    }
                }
            }

            if (!sameTerritoryHighlight) {
                if (_territoryLastOverIndex >= 0 && OnTerritoryExit != null)
                    OnTerritoryExit(this, _territoryLastOverIndex);
                if (newTerritoryHighlightedIndex >= 0 && OnTerritoryEnter != null)
                    OnTerritoryEnter(this, newTerritoryHighlightedIndex);
                _territoryLastOverIndex = newTerritoryHighlightedIndex;
                if (_territoryLastOverIndex >= 0)
                    _territoryLastOver = territories[_territoryLastOverIndex];
                else
                    _territoryLastOver = null;
            }

            // verify if last highlited cell remains active
            bool sameCellHighlight = false;
            if (_cellLastOver != null) {
                if (_cellLastOver.region.Contains(localPoint.x, localPoint.y)) {
                    sameCellHighlight = true;
                }
            }
            int newCellHighlightedIndex = -1;

            if (!sameCellHighlight) {
                if (_highlightMode == HIGHLIGHT_MODE.Cells || !Application.isPlaying) {
                    Cell newCellHighlighted = GetCellAtPoint(localPoint, false, _territoryLastOverIndex);
                    if (newCellHighlighted != null) {
                        newCellHighlightedIndex = CellGetIndex(newCellHighlighted);
                    }
                }
            }

            if (!sameCellHighlight) {
                if (_cellLastOverIndex >= 0 && OnCellExit != null)
                    OnCellExit(this, _cellLastOverIndex);
                if (newCellHighlightedIndex >= 0 && OnCellEnter != null)
                    OnCellEnter(this, newCellHighlightedIndex);
                _cellLastOverIndex = newCellHighlightedIndex;
                if (newCellHighlightedIndex >= 0)
                    _cellLastOver = cells[newCellHighlightedIndex];
                else
                    _cellLastOver = null;
            }

            if (_highlightMode == HIGHLIGHT_MODE.Cells || !Application.isPlaying) {
                if (!sameCellHighlight) {
                    if (newCellHighlightedIndex >= 0 && (cells[newCellHighlightedIndex].visible || _cellHighlightNonVisible)) {
                        HighlightCellRegion(newCellHighlightedIndex, false);
                    } else {
                        HideCellRegionHighlight();
                    }
                }
            } else if (_highlightMode == HIGHLIGHT_MODE.Territories) {
                if (!sameTerritoryHighlight) {
                    if (newTerritoryHighlightedIndex >= 0 && territories[newTerritoryHighlightedIndex].visible) {
                        HighlightTerritoryRegion(newTerritoryHighlightedIndex, false);
                    } else {
                        HideTerritoryRegionHighlight();
                    }
                }
            }

            return true;
        }

        void UpdateHighlightFade() {
            if (_highlightFadeAmount == 0)
                return;

            if (_highlightedObj != null) {
                float newAlpha = _highlightFadeMin + Mathf.PingPong(Time.time * _highlightFadeSpeed - highlightFadeStart, _highlightFadeAmount - _highlightFadeMin);
                Material mat = highlightMode == HIGHLIGHT_MODE.Territories ? hudMatTerritory : hudMatCell;
                mat.SetFloat("_FadeAmount", newAlpha);
                float newScale = _highlightScaleMin + Mathf.PingPong(Time.time * highlightFadeSpeed, _highlightScaleMax - _highlightScaleMin);
                mat.SetFloat("_Scale", 1f / (newScale + 0.0001f));
                _highlightedObj.GetComponent<Renderer>().sharedMaterial = mat;
            }
        }

        void TriggerEvents() {
            int buttonIndex = -1;
            bool leftButtonClick = Input.GetMouseButtonDown(0);
            bool rightButtonClick = Input.GetMouseButtonDown(1);
            bool leftButtonUp = Input.GetMouseButtonUp(0);
            bool rightButtonUp = Input.GetMouseButtonUp(1);
            if (leftButtonUp || leftButtonClick) {
                buttonIndex = 0;
            } else if (rightButtonUp || rightButtonClick) {
                buttonIndex = 1;
            }
            if (leftButtonClick || rightButtonClick) {
                // record last clicked cell/territory
                _cellLastClickedIndex = _cellLastOverIndex;
                _territoryLastClickedIndex = _territoryLastOverIndex;

                if (_territoryLastOverIndex >= 0 && OnTerritoryMouseDown != null) {
                    OnTerritoryMouseDown(this, _territoryLastOverIndex, buttonIndex);
                }
                if (_cellLastOverIndex >= 0 && OnCellMouseDown != null) {
                    OnCellMouseDown(this, _cellLastOverIndex, buttonIndex);
                }
            } else if (leftButtonUp || rightButtonUp) {
                if (_territoryLastOverIndex >= 0) {
                    if (_territoryLastOverIndex == _territoryLastClickedIndex && OnTerritoryClick != null) {
                        OnTerritoryClick(this, _territoryLastOverIndex, buttonIndex);
                    }
                    if (OnTerritoryMouseUp != null) {
                        OnTerritoryMouseUp(this, _territoryLastOverIndex, buttonIndex);
                    }
                }
                if (_cellLastOverIndex >= 0) {
                    if (_cellLastOverIndex == _cellLastClickedIndex && OnCellClick != null) {
                        OnCellClick(this, _cellLastOverIndex, buttonIndex);
                    }
                    if (OnCellMouseUp != null) {
                        OnCellMouseUp(this, _cellLastOverIndex, buttonIndex);
                    }

                }
            }
        }

        #endregion


        #region Geometric functions

        Vector3 GetWorldSpacePosition(Vector2 localPosition) {
            if (_terrainWrapper != null) {
                Vector3 wPos = transform.TransformPoint(localPosition);
                wPos.y += _terrainWrapper.SampleHeight(wPos) * _terrainWrapper.transform.lossyScale.y;
                return wPos;
            } else {
                return transform.TransformPoint(localPosition);
            }
        }

        Vector3 GetScaledVector(Vector3 p) {
            p.x *= _gridScale.x;
            p.x += _gridCenter.x;
            p.y *= _gridScale.y;
            p.y += _gridCenter.y;
            return p;
        }

        Vector3 GetScaledVector(Point q) {
            Vector3 p;
            p.x = (float)q.x * _gridScale.x;
            p.x += _gridCenter.x;
            p.y = (float)q.y * _gridScale.y;
            p.y += _gridCenter.y;
            p.z = 0;
            return p;
        }


        void GetUnscaledVector(ref Vector2 p) {
            p.x -= _gridCenter.x;
            p.x /= _gridScale.x;
            p.y -= _gridCenter.y;
            p.y /= _gridScale.y;
        }

        Point GetScaledPoint(Point p) {
            p.x *= _gridScale.x;
            p.x += _gridCenter.x;
            p.y *= _gridScale.y;
            p.y += _gridCenter.y;
            return p;
        }

        Segment GetScaledSegment(Segment s) {
            Segment ss = new Segment(s.start, s.end, s.border);
            ss.start = GetScaledPoint(ss.start);
            ss.end = GetScaledPoint(ss.end);
            return ss;
        }


        #endregion



        #region Territory stuff

        void HideTerritoryRegionHighlight() {
            HideCellRegionHighlight();
            if (_territoryHighlighted == null)
                return;
            if (_highlightedObj != null) {
                if (_territoryHighlighted.region.customMaterial != null) {
                    ApplyMaterialToSurface(_territoryHighlighted.region, _territoryHighlighted.region.customMaterial);
                } else if (_highlightedObj.GetComponent<SurfaceFader>() == null) {
                    _highlightedObj.SetActive(false);
                }
                if (!_territoryHighlighted.visible) {
                    _highlightedObj.SetActive(false);
                }
                _highlightedObj = null;
            }
            _territoryHighlighted = null;
            _territoryHighlightedIndex = -1;
        }

        /// <summary>
        /// Highlights the territory region specified. Returns the generated highlight surface gameObject.
        /// Internally used by the Map UI and the Editor component, but you can use it as well to temporarily mark a territory region.
        /// </summary>
        /// <param name="refreshGeometry">Pass true only if you're sure you want to force refresh the geometry of the highlight (for instance, if the frontiers data has changed). If you're unsure, pass false.</param>
        GameObject HighlightTerritoryRegion(int territoryIndex, bool refreshGeometry) {
            if (_territoryHighlighted != null)
                HideTerritoryRegionHighlight();
            if (territoryIndex < 0 || territoryIndex >= territories.Count)
                return null;

            if (_highlightEffect != HIGHLIGHT_EFFECT.None) {
                if (OnTerritoryHighlight != null) {
                    bool cancelHighlight = false;
                    OnTerritoryHighlight(this, territoryIndex, ref cancelHighlight);
                    if (cancelHighlight)
                        return null;
                }

                int cacheIndex = GetCacheIndexForTerritoryRegion(territoryIndex);
                bool existsInCache = surfaces.ContainsKey(cacheIndex);
                if (refreshGeometry && existsInCache) {
                    GameObject obj = surfaces[cacheIndex];
                    surfaces.Remove(cacheIndex);
                    DestroyImmediate(obj);
                    existsInCache = false;
                }
                if (existsInCache) {
                    _highlightedObj = surfaces[cacheIndex];
                    if (_highlightedObj == null) {
                        surfaces.Remove(cacheIndex);
                    } else {
                        if (!_highlightedObj.activeSelf)
                            _highlightedObj.SetActive(true);
                        Renderer rr = _highlightedObj.GetComponent<Renderer>();
                        if (rr.sharedMaterial != hudMatTerritory)
                            rr.sharedMaterial = hudMatTerritory;
                    }
                } else {
                    _highlightedObj = GenerateTerritoryRegionSurface(territoryIndex, hudMatTerritory, Misc.Vector2one, Misc.Vector2zero, 0, false);
                }
                // Reuse territory texture
                Territory territory = territories[territoryIndex];
                if (territory.region.customMaterial != null) {
                    if (hudMatTerritory.HasProperty("_MainTex")) {
                        hudMatTerritory.mainTexture = territory.region.customMaterial.mainTexture;
                    } else {
                        hudMatTerritory.mainTexture = null;
                    }
                }
                highlightFadeStart = Time.time;
            }

            _territoryHighlightedIndex = territoryIndex;
            _territoryHighlighted = territories[territoryIndex];

            return _highlightedObj;
        }

        GameObject GenerateTerritoryRegionSurface(int territoryIndex, Material material, Vector2 textureScale, Vector2 textureOffset, float textureRotation, bool rotateInLocalSpace) {
            if (territoryIndex < 0 || territoryIndex >= territories.Count)
                return null;
            Region region = territories[territoryIndex].region;
            int cacheIndex = GetCacheIndexForTerritoryRegion(territoryIndex);
            return GenerateRegionSurface(region, cacheIndex, material, textureScale, textureOffset, textureRotation, rotateInLocalSpace);
        }

        void UpdateColorizedTerritoriesAlpha() {
            if (territories == null)
                return;
            int territoriesCount = territories.Count;
            for (int c = 0; c < territoriesCount; c++) {
                Territory territory = territories[c];
                int cacheIndex = GetCacheIndexForTerritoryRegion(c);
                GameObject surf;
                if (surfaces.TryGetValue(cacheIndex, out surf)) {
                    if (surf != null) {
                        Material mat = surf.GetComponent<Renderer>().sharedMaterial;
                        Color newColor = mat.color;
                        newColor.a = territory.fillColor.a * _colorizedTerritoriesAlpha;
                        mat.color = newColor;
                    }
                }
            }
        }

        Territory GetTerritoryAtPoint(Vector3 position, bool worldSpace) {
            if (territories == null)
                return null;
            if (worldSpace) {
                if (!GetLocalHitFromWorldPosition(ref position))
                    return null;
            }
            int territoriesCount = territories.Count;
            for (int p = 0; p < territoriesCount; p++) {
                Territory territory = territories[p];
                if (territory.region.Contains(position.x, position.y)) {
                    return territory;
                }
            }
            return null;
        }

        void TerritoryAnimate(FADER_STYLE style, int territoryIndex, Color color, float duration, int repetitions) {
            if (territoryIndex < 0 || territoryIndex >= territories.Count)
                return;
            int cacheIndex = GetCacheIndexForTerritoryRegion(territoryIndex);
            GameObject territorySurface = null;
            surfaces.TryGetValue(cacheIndex, out territorySurface);
            if (territorySurface == null) {
                territorySurface = TerritoryToggleRegionSurface(territoryIndex, true, color);
                territories[territoryIndex].region.customMaterial = null;
            } else {
                territorySurface.SetActive(true);
            }
            Renderer renderer = territorySurface.GetComponent<Renderer>();
            Material oldMaterial = renderer.sharedMaterial;
            Material fadeMaterial = Instantiate(hudMatTerritory);
            Region region = territories[territoryIndex].region;
            fadeMaterial.color = region.customMaterial != null ? region.customMaterial.color : oldMaterial.color;
            if (fadeMaterial.HasProperty("_MainTex") && oldMaterial.HasProperty("_MainTex")) {
                fadeMaterial.mainTexture = oldMaterial.mainTexture;
            }
            disposalManager.MarkForDisposal(fadeMaterial);
            fadeMaterial.name = oldMaterial.name;
            renderer.sharedMaterial = fadeMaterial;
            SurfaceFader.Animate(style, this, territorySurface, region, fadeMaterial, color, duration, repetitions);
        }


        void TerritoryCancelAnimation(int territoryIndex, float fadeOutDuration) {
            if (territoryIndex < 0 || territoryIndex >= territories.Count)
                return;
            int cacheIndex = GetCacheIndexForTerritoryRegion(territoryIndex);
            GameObject territorySurface = null;
            surfaces.TryGetValue(cacheIndex, out territorySurface);
            if (territorySurface == null)
                return;
            territorySurface.GetComponents<SurfaceFader>(tempFaders);
            int count = tempFaders.Count;
            for (int k = 0; k < count; k++) {
                tempFaders[k].Finish(fadeOutDuration);
            }
        }

        #endregion


        #region Cell stuff

        int GetCacheIndexForCellRegion(int cellIndex) {
            return 1000000 + cellIndex; // * 1000 + regionIndex;
        }

        /// <summary>
        /// Highlights the cell region specified. Returns the generated highlight surface gameObject.
        /// Internally used by the Map UI and the Editor component, but you can use it as well to temporarily mark a territory region.
        /// </summary>
        /// <param name="refreshGeometry">Pass true only if you're sure you want to force refresh the geometry of the highlight (for instance, if the frontiers data has changed). If you're unsure, pass false.</param>
        GameObject HighlightCellRegion(int cellIndex, bool refreshGeometry) {
            if (_cellHighlighted != null)
                HideCellRegionHighlight();
            if (cellIndex < 0 || cellIndex >= cells.Count)
                return null;

            if (_highlightEffect != HIGHLIGHT_EFFECT.None) {

                if (OnCellHighlight != null) {
                    bool cancelHighlight = false;
                    OnCellHighlight(this, cellIndex, ref cancelHighlight);
                    if (cancelHighlight)
                        return null;
                }

                int cacheIndex = GetCacheIndexForCellRegion(cellIndex);
                GameObject obj;
                bool existsInCache = surfaces.TryGetValue(cacheIndex, out obj);
                if (refreshGeometry && existsInCache) {
                    surfaces.Remove(cacheIndex);
                    DestroyImmediate(obj);
                    existsInCache = false;
                }
                if (existsInCache) {
                    _highlightedObj = surfaces[cacheIndex];
                    if (_highlightedObj != null) {
                        _highlightedObj.SetActive(true);
                        _highlightedObj.GetComponent<Renderer>().sharedMaterial = hudMatCell;
                    } else {
                        surfaces.Remove(cacheIndex);
                    }
                } else {
                    _highlightedObj = GenerateCellRegionSurface(cellIndex, hudMatCell, Misc.Vector2one, Misc.Vector2zero, 0, false);
                }
                // Reuse cell texture
                Cell cell = cells[cellIndex];
                if (cell.region.customMaterial != null && cell.region.customMaterial.HasProperty("_MainTex")) {
                    hudMatCell.mainTexture = cell.region.customMaterial.mainTexture;
                } else {
                    hudMatCell.mainTexture = null;
                }
                highlightFadeStart = Time.time;
            }

            _cellHighlighted = cells[cellIndex];
            _cellHighlightedIndex = cellIndex;
            return _highlightedObj;
        }

        void HideCellRegionHighlight() {
            if (_cellHighlighted == null)
                return;
            if (_highlightedObj != null) {
                if (cellHighlighted.region.customMaterial != null) {
                    ApplyMaterialToSurface(cellHighlighted.region, _cellHighlighted.region.customMaterial);
                } else if (_highlightedObj.GetComponent<SurfaceFader>() == null) {
                    _highlightedObj.SetActive(false);
                }
                if (!cellHighlighted.visible) {
                    _highlightedObj.SetActive(false);
                }
                _highlightedObj = null;
            }
            _cellHighlighted = null;
            _cellHighlightedIndex = -1;
        }

        void SurfaceSegmentForSurface(Segment s, Connector connector) {

            // trace the line until roughness is exceeded
            double dist = s.magnitude; // (float)Math.Sqrt ( (p1.x-p0.x)*(p1.x-p0.x) + (p1.y-p0.y)*(p1.y-p0.y));
            Point direction = s.end - s.start;
            int numSteps = (int)(dist / MIN_VERTEX_DISTANCE);
            if (numSteps <= 1) {
                connector.Add(s);
                return;
            }

            Point t0 = s.start;
            Vector3 wp_start = transform.TransformPoint(t0.vector3);
            float h0 = _terrainWrapper.SampleHeight(wp_start);
            Point ta = t0;
            float h1;
            Vector3 wp_end = transform.TransformPoint(s.end.vector3);
            Vector3 wp_direction = wp_end - wp_start;
            for (int i = 1; i < numSteps; i++) {
                Point t1 = s.start + direction * ((double)i / numSteps);
                Vector3 v1 = wp_start + wp_direction * ((float)i / numSteps);
                h1 = _terrainWrapper.SampleHeight(v1);
                if (h1 - h0 > maxRoughnessWorldSpace || h0 - h1 > maxRoughnessWorldSpace) {
                    if (t0 != ta) {
                        Segment s0 = new Segment(t0, ta, s.border);
                        connector.Add(s0);
                        Segment s1 = new Segment(ta, t1, s.border);
                        connector.Add(s1);
                    } else {
                        Segment s0 = new Segment(t0, t1, s.border);
                        connector.Add(s0);
                    }
                    t0 = t1;
                    h0 = h1;
                }
                ta = t1;
            }
            // Add last point
            Segment finalSeg = new Segment(t0, s.end, s.border);
            connector.Add(finalSeg);
        }


        void GetFirstAndLastPointInRow(float y, int pointCount, out float first, out float last) {
            float minx = float.MaxValue;
            float maxx = float.MinValue;
            PolygonPoint p0 = tempPolyPoints[0], p1;
            for (int k = 1; k <= pointCount; k++) {
                if (k == pointCount) {
                    p1 = tempPolyPoints[0];
                } else {
                    p1 = tempPolyPoints[k];
                }
                // if line crosses the horizontal line
                if (p0.Yf >= y && p1.Yf <= y || p0.Yf <= y && p1.Yf >= y) {
                    float x;
                    if (p1.Xf == p0.Xf) {
                        x = p0.Xf;
                    } else {
                        float a = (p1.Xf - p0.Xf) / (p1.Yf - p0.Yf);
                        x = p0.Xf + a * (y - p0.Yf);
                    }
                    if (x > maxx)
                        maxx = x;
                    if (x < minx)
                        minx = x;
                }
                p0 = p1;
            }
            first = minx - 2f;
            last = maxx - 2f;
        }

        bool IsTooNearPolygon(double x, double y, PolygonPoint[] tempPolyPoints, int pointsCount) {
            for (int j = pointsCount - 1; j >= 0; j--) {
                PolygonPoint p1 = tempPolyPoints[j];
                double dx = x - p1.X;
                double dy = y - p1.Y;
                if (dx * dx + dy * dy < SQR_MIN_VERTEX_DIST) {
                    return true;
                }
            }
            return false;
        }

        GameObject GenerateCellRegionSurface(int cellIndex, Material material, Vector2 textureScale, Vector2 textureOffset, float textureRotation, bool rotateInLocalSpace) {
            if (cellIndex < 0 || cellIndex >= cells.Count)
                return null;
            Region region = cells[cellIndex].region;
            int cacheIndex = GetCacheIndexForCellRegion(cellIndex);
            return GenerateRegionSurface(region, cacheIndex, material, textureScale, textureOffset, textureRotation, rotateInLocalSpace);
        }

        Cell GetCellAtPoint(Vector3 position, bool worldSpace, int territoryIndex = -1) {
            // Compute local point
            if (worldSpace) {
                if (!GetLocalHitFromWorldPosition(ref position))
                    return null;
            }

            int cellsCount = cells.Count;
            if ((_gridTopology == GRID_TOPOLOGY.Hexagonal || _gridTopology == GRID_TOPOLOGY.Box) && cellsCount == _cellRowCount * _cellColumnCount) {
                float px = (position.x - _gridCenter.x) / _gridScale.x + 0.5f;
                float py = (position.y - _gridCenter.y) / _gridScale.y + 0.5f;
                int xc = (int)(_cellColumnCount * px);
                int yc = (int)(_cellRowCount * py);
                for (int y = yc - 1; y < yc + 2; y++) {
                    for (int x = xc - 1; x < xc + 2; x++) {
                        int index = y * _cellColumnCount + x;
                        if (index < 0 || index >= cellCount)
                            continue;
                        Cell cell = cells[index];
                        if (cell == null || cell.region == null || cell.region.points == null)
                            continue;
                        if (cell.region.Contains(position.x, position.y)) {
                            return cell;
                        }
                    }
                }
            } else {
                if (territoryIndex >= 0 && (territories == null || territoryIndex >= territories.Count))
                    return null;
                List<Cell> cells = territoryIndex >= 0 ? territories[territoryIndex].cells : sortedCells;
                cellsCount = cells.Count;
                for (int p = 0; p < cellsCount; p++) {
                    Cell cell = cells[p];
                    if (cell == null || cell.region == null || cell.region.points == null)
                        continue;
                    if (cell.region.Contains(position.x, position.y)) {
                        return cell;
                    }
                }
            }
            return null;
        }


        void Encapsulate(ref Rect r, Vector3 position) {
            if (position.x < r.xMin)
                r.xMin = position.x;
            if (position.x > r.xMax)
                r.xMax = position.x;
            if (position.y < r.yMin)
                r.yMin = position.y;
            if (position.y > r.yMax)
                r.yMax = position.y;
        }



        int GetCellInArea(Bounds bounds, List<int> cellIndices, float padding = 0) {
            Vector3 min = bounds.min;
            Vector3 max = bounds.max;
            Rect rect = new Rect(transform.InverseTransformPoint(min), Misc.Vector2zero);
            Encapsulate(ref rect, transform.InverseTransformPoint(new Vector3(min.x, min.y, max.z)));
            Encapsulate(ref rect, transform.InverseTransformPoint(new Vector3(max.x, min.y, min.z)));
            Encapsulate(ref rect, transform.InverseTransformPoint(new Vector3(max.x, min.y, max.z)));
            Encapsulate(ref rect, transform.InverseTransformPoint(new Vector3(min.x, max.y, min.z)));
            Encapsulate(ref rect, transform.InverseTransformPoint(new Vector3(min.x, max.y, max.z)));
            Encapsulate(ref rect, transform.InverseTransformPoint(new Vector3(max.x, max.y, min.z)));
            Encapsulate(ref rect, transform.InverseTransformPoint(new Vector3(max.x, max.y, max.z)));
            return GetCellInArea(rect, cellIndices, padding);
        }



        int GetCellInArea(Rect rect, List<int> cellIndices, float padding = 0) {
            rect.xMin -= padding;
            rect.xMax += padding;
            rect.yMin -= padding;
            rect.yMax += padding;
            cellIndices.Clear();
            int cellCount = cells.Count;
            for (int k = 0; k < cellCount; k++) {
                Cell cell = cells[k];
                if (cell != null && cell.center.x >= rect.xMin && cell.center.x <= rect.xMax && cell.center.y >= rect.yMin && cell.center.y <= rect.yMax) {
                    cellIndices.Add(k);
                }
            }
            return cellIndices.Count;
        }




        void CellAnimate(FADER_STYLE style, int cellIndex, Color initialColor, Color color, float duration, int repetitions) {
            if (cellIndex < 0 || cellIndex >= cells.Count)
                return;
            int cacheIndex = GetCacheIndexForCellRegion(cellIndex);
            GameObject cellSurface;
            surfaces.TryGetValue(cacheIndex, out cellSurface);
            if (cellSurface == null) {
                cellSurface = CellToggleRegionSurface(cellIndex, true, color, false);
                cells[cellIndex].region.customMaterial = null;
            } else {
                cellSurface.SetActive(true);
            }
            if (cellSurface == null)
                return;
            Renderer renderer = cellSurface.GetComponent<Renderer>();
            if (renderer == null)
                return;
            Material oldMaterial = renderer.sharedMaterial;
            Material fadeMaterial = Instantiate(hudMatCell);
            fadeMaterial.SetFloat("_FadeAmount", 0);
            Region region = cells[cellIndex].region;
            fadeMaterial.color = (initialColor.a == 0 && region.customMaterial != null) ? region.customMaterial.color : initialColor;
            fadeMaterial.mainTexture = oldMaterial.mainTexture;
            disposalManager.MarkForDisposal(fadeMaterial);
            renderer.sharedMaterial = fadeMaterial;

            SurfaceFader.Animate(style, this, cellSurface, region, fadeMaterial, color, duration, repetitions);
        }


        void CellCancelAnimation(int cellIndex, float fadeOutDuration) {
            if (cellIndex < 0 || cellIndex >= cells.Count)
                return;
            int cacheIndex = GetCacheIndexForCellRegion(cellIndex);
            GameObject cellSurface = null;
            surfaces.TryGetValue(cacheIndex, out cellSurface);
            if (cellSurface == null)
                return;
            cellSurface.GetComponents<SurfaceFader>(tempFaders);
            int count = tempFaders.Count;
            for (int k = 0; k < count; k++) {
                tempFaders[k].Finish(fadeOutDuration);
            }
        }

        void CancelAnimationAll(float fadeOutDuration) {
            GetComponentsInChildren<SurfaceFader>(tempFaders);
            int count = tempFaders.Count;
            for (int k = 0; k < count; k++) {
                tempFaders[k].Finish(fadeOutDuration);
            }
        }

        bool ToggleCellsVisibility(Rect rect, bool visible, bool worldSpace = false) {
            if (cells == null)
                return false;
            int count = cells.Count;
            if (worldSpace) {
                Vector3 pos = rect.min;
                if (!GetLocalHitFromWorldPosition(ref pos))
                    return false;
                rect.min = pos;
                pos = rect.max;
                if (!GetLocalHitFromWorldPosition(ref pos))
                    return false;
                rect.max = pos;
            }
            if (rect.xMin < -0.4999f)
                rect.xMin = -0.4999f;
            if (rect.yMin < -0.4999f)
                rect.yMin = -0.4999f;
            if (rect.xMax > 0.4999f)
                rect.xMax = 0.4999f;
            if (rect.yMax > 0.4999f)
                rect.yMax = 0.4999f;
            if (_gridTopology == GRID_TOPOLOGY.Irregular) {
                float xmin = rect.xMin;
                float xmax = rect.xMax;
                float ymin = rect.yMin;
                float ymax = rect.yMax;
                for (int k = 0; k < count; k++) {
                    Cell cell = cells[k];
                    if (cell.center.x >= xmin && cell.center.x <= xmax && cell.center.y >= ymin && cell.center.y <= ymax) {
                        cell.visible = visible;
                    }
                }
            } else {
                Cell topLeft = CellGetAtPosition(rect.min, worldSpace);
                Cell bottomRight = CellGetAtPosition(rect.max, worldSpace);
                if (topLeft == null || bottomRight == null)
                    return false;
                int row0 = topLeft.row;
                int col0 = topLeft.column;
                int row1 = bottomRight.row;
                int col1 = bottomRight.column;
                if (row1 < row0) {
                    int tmp = row0;
                    row0 = row1;
                    row1 = tmp;
                }
                if (col1 < col0) {
                    int tmp = col0;
                    col0 = row1;
                    col1 = tmp;
                }
                for (int k = 0; k < count; k++) {
                    Cell cell = cells[k];
                    if (cell.row >= row0 && cell.row <= row1 && cell.column >= col0 && cell.column <= col1) {
                        cell.visible = visible;
                    }
                }
            }
            ClearLastOver();
            needRefreshRouteMatrix = true;
            refreshCellMesh = true;
            issueRedraw = RedrawType.Full;
            return true;
        }


        bool GetAdjacentCellCoordinates(int cellIndex, CELL_SIDE side, out int or, out int oc, out CELL_SIDE os) {
            Cell cell = cells[cellIndex];
            int r = cell.row;
            int c = cell.column;
            or = r;
            oc = c;
            os = side;
            if (_gridTopology == GRID_TOPOLOGY.Hexagonal) {
                switch (side) {
                    case CELL_SIDE.Bottom:
                        or--;
                        os = CELL_SIDE.Top;
                        break;
                    case CELL_SIDE.Top:
                        or++;
                        os = CELL_SIDE.Bottom;
                        break;
                    case CELL_SIDE.BottomRight:
                        if (oc % 2 != 0) {
                            or--;
                        }
                        oc++;
                        os = CELL_SIDE.TopLeft;
                        break;
                    case CELL_SIDE.TopRight:
                        if (oc % 2 == 0) {
                            or++;
                        }
                        oc++;
                        os = CELL_SIDE.BottomLeft;
                        break;
                    case CELL_SIDE.TopLeft:
                        if (oc % 2 == 0) {
                            or++;
                        }
                        oc--;
                        os = CELL_SIDE.BottomRight;
                        break;
                    case CELL_SIDE.BottomLeft:
                        if (oc % 2 != 0) {
                            or--;
                        }
                        oc--;
                        os = CELL_SIDE.TopRight;
                        break;
                }
            } else if (_gridTopology == GRID_TOPOLOGY.Box) {
                switch (side) {
                    case CELL_SIDE.Bottom:
                        or--;
                        os = CELL_SIDE.Top;
                        break;
                    case CELL_SIDE.Top:
                        or++;
                        os = CELL_SIDE.Bottom;
                        break;
                    case CELL_SIDE.BottomRight:
                        or--;
                        oc++;
                        os = CELL_SIDE.TopLeft;
                        break;
                    case CELL_SIDE.TopRight:
                        or++;
                        oc++;
                        os = CELL_SIDE.BottomLeft;
                        break;
                    case CELL_SIDE.TopLeft:
                        or++;
                        oc--;
                        os = CELL_SIDE.BottomRight;
                        break;
                    case CELL_SIDE.BottomLeft:
                        or--;
                        oc--;
                        os = CELL_SIDE.TopRight;
                        break;
                    case CELL_SIDE.Left:
                        oc--;
                        os = CELL_SIDE.Right;
                        break;
                    case CELL_SIDE.Right:
                        oc++;
                        os = CELL_SIDE.Left;
                        break;
                }
            } else {
                return false;
            }
            return or >= 0 && or < _cellRowCount && oc >= 0 && oc < _cellColumnCount;
        }

        #endregion

        #region Rectangle selection

        RectangleSelection rectangleSelection;

        void CheckRectangleSelectionObject() {
            if (rectangleSelection == null) {
                rectangleSelection = FindObjectOfType<RectangleSelection>();
            }
            if (rectangleSelection == null) {
                GameObject rectangleSelectionObj = Instantiate<GameObject>(Resources.Load<GameObject>("Prefabs/CanvasSelectionRectangle"));
                rectangleSelectionObj.name = "TGS Rectangle Selector";
                rectangleSelection = rectangleSelectionObj.GetComponentInChildren<RectangleSelection>();
            }
        }


        #endregion

    }

    public static class TGSHelper {

        public static TerrainGridSystem AddTerrainGridSystem(this GameObject o) {
            if (o == null)
                return null;
            o.AddComponent<TerrainGridSystem>();
            return o.GetComponentInChildren<TerrainGridSystem>();
        }
    }
}