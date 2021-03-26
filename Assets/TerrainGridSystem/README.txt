************************************
*        TERRAIN GRID SYSTEM       *
* (C) Copyright 2015-2020 Kronnect * 
*            README FILE           *
************************************


How to use this asset
---------------------
Firstly, you should run the Demo Scene provided to get an idea of the overall functionality.
Later, you should read the documentation and experiment with the tool.

Hint: to use the asset, drag the TerrainGridSystem prefab from Resources/Prefabs folder to your scene and assign your terrain to it.


Demo Scene
----------
There's one demo scene, located in "Demos" folder. Just go there from Unity, open "Demo1" scene and run it.


Documentation/API reference
---------------------------
The PDF is located in the Doc folder. It contains instructions on how to use the prefab and the API so you can control it from your code.


Support
-------
Please read the documentation PDF and browse/play with the demo scene and sample source code included before contacting us for support :-)

* Support: contact@kronnect.me
* Website-Forum: http://kronnect.me
* Twitter: @KronnectGames


Future updates
--------------

All our assets follow an incremental development process by which a few beta releases are published on our support forum (kronnect.com).
We encourage you to signup and engage our forum. The forum is the primary support and feature discussions medium.

Of course, all updates of Terrain Grid System will be eventually available on the Asset Store.


Version history
---------------

Current version
- Added "Far Distance Fade" options - makes the grid transparent at custom distance from camera
- API: added RedrawCells()
- [Fix] Fixed grid generation issue using thick borders and disabled geometry shaders

Version 10.6
- Change: all events now receive a reference to the terrain grid system object to support multi-grid configurations
- Added multi-grid and multi-terrain demo scenes. Example: https://youtu.be/40hXxxfGcJ4
- When using the grid.bounds property, the y-size if now set to a minimum value, instead of 0, so it can be used to check if it contains other positions in world space
- API: CellGetNeighbours will now correctly return all cells within range according to max search cost and max distance
- Added warning if Unity terrain gameobject has rotations
- [Fix] Fixes and improvements for surface adaptation over custom meshes

Version 10.5.3
- API: CellSetTerritory(). Performance and memory optimization when transferring cells to other territories.
- [Fix] Fixed an issue with some Voronoi cells not snapping correctly to grid corners when number of cells is two

Version 10.5.2
- Slight performance optimizations for box and hexagonal grids
- [Fix] Clicking the "Can Cross" toggle in grid editor now affects the entire selection of cells

Version 10.5.1
- [Fix] Fixed recolor issue when cell surface has +65000 vertices

Version 10.5
- Added support for thick borders on Metal platform
- Improved appearance of thick borders when using transparency
- API: added CellGetNeighbour(index, side)
- [Fix] Fixed crash then adding prefab to scene

Version 10.4.3
- API: added CellGetCanCross()
- [Fix] Fixed territory center bug when territories are created by a color texture

Version 10.4.2
- Floating cells now can be highlighted when using TGS over a group of objects parented to an empty gameobject
- [Fix] Fixed colored cells losing their colors when pointer exits their surface under certain conditions

Version 10.4.1
- [Fix] Fixed cell group mask issue with CellGetNeighbours API

Version 10.4
- Added "Sorting Order" option to control rendering order in transparent mode (No Background option set to true)
- Added demo scene 21: using a background texture to color cells instead of coloring individual cells
- [Fix] Fixed an issue with SetGridCenterWorldPosition API when used on non standard terrain surfaces

Version 10.3.1
- General fixes and improvements

Version 10.3
- API: CellGetNeighours overload now can receive a List<int> parameter to reuse memory
- API: FindPath overload now can receive a List<int> parameter to reuse memory
- API: Added "maxResultsCount" parameter to CellGetNeighbours methods
- [Fix] Reduced CPU overhead when using custom mesh terrains

Version 10.2.1
- Improvements to how grid adapts to a group of gameobjects

Version 10.2
- Added "Mesh Pivot" property to Grid Positioning section (only available when using custom terrain meshes)

Version 10.1
- Added canCross field to Grid Editor (it's also stored when grid settings are exported into TGS Config component)
- Added option to hide cells of neutral territories
- Minor fixes and improvements

Version 10.0.1
- [Fix] Fixed rendering order issue with Microsplat in forward rendering path

Version 10.0
- Custom territory thickness option
- Option to bake Voronoi data into the scene. Improves runtime loading time when using many cells.
- Alpha blending support when "No Background" option is enabled
- API: added "fadeOutDuration" parameter to CancelAnimation methods
- [Fix] Fixed null exception error in inspector when assigning a mask textured generated procedurally
- [Fix] Grid now will appear correctly when changing rows or columns if showCells is set to false
- [Fix] Fixed normal interpolation on mesh-based terrain

Version 9.2.4
- API: added CancelAnimations to stop all current animations on grid (flash, blink, colortemp, etc.)
- [Fix] CellColorTemp finishes with a black cell when transparent background option is enabled

Version 9.2.3
- Added Min Elevation Multiplier to control minimum elevation shift between layers
- [Fix] CellSetBorderVisible now issue a refresh at end of frame

Version 9.2.2
- [Fix] Fixed issue when building for Android

Version 9.2.1
- [Fix] API: fixed GetCellAtPosition issue with box topology

Version 9.2
- Added "Dual Colors" option to highlight effects
- Added "bounds" property which returns the bounds of the grid in world space
- [Fix] Fixed highlight effect when No Background option is enabled

Version 9.1
- Added ability to render without background geometry ("No Background" property)
- Added Terrain Surface Depth Offset parameter to inspector
- Optimized performance of flat 2D grids using box topology
- API: added SetDimensions which changes rows/columns in one step
- [Fix] API: fixed CellSetTexture not texturing certain cells
- [Fix] Fixed rectangle selection issue when grid is in oblique angle
- [Fix] Fixed grid not appearing when added dynamically to a terrain instance in a build

Version 9.0 2019-MAY-1
- Support for mesh-based terrains
- Added "Clamp Vertices" option when Minimum Elevation is used
- Minimum Unity version required is now 5.6, will be 2017.4 LTS by July 2019
- Performance improvements
- [Fix] Fixed near clip fade issue with territory borders

Version 8.0 2019-APR-5
- Rectangle selection support. Demo scene 20.
- Snippets demo scene 19. New collection of TGS* scripts that use Terrain Grid System API interactively. Add them to any gameobject to make it interact with grid without coding. See TerrainGridSystem/Scripts/Snippets folder.
- Added path-finding support for irregular topology
- Reduction of vertex count in irregular topology by removing collinear segments
- Improved performance of A* methods
- API: added CellGetSideBlocksLOS / CellSetSideBlocksLOS to specify which cell sides block line of sight
- API: added CellGetNeighboursWithinRange
- API: added CellTestLineOfSight to filter a list of cells not visible from a given cell
- API: added ignoreCanCrossCheck argument to FindPath and CellGetNeighbours methods

Version 7.7 2019-MAR-19
- Calling Redraw manually now forces immediaty refresh instead of refreshing at end of frame
- Internal improvements to inspector
- [Fix] Fixed class conflict with WPM Globe Edition
- [Fix] Fixed Microsplat issue in forward rendering path

Version 7.6.3 2019-MAR-11
- [Fix] Added compatibility with Microsplat in Forward Rendering path

Version 7.6.2 2019-MAR-1
- [Fix] Fixed highlight issue when highlight effect is set to None and cell is colored using scripting
- [Fix] Fixed cells not being updated when some settings changed and cells mesh was hidden

Version 7.6.1 2019-JAN-7
- [Fix] Fix for terrain color artifacts when roughness is zero

Version 7.6 2018-DEC-24
- API: added CellSetBorderVisible, TerritorySetBorderVisible
- [Fix] Fixed issue when setting diagonal costs in box topology

Version 7.5 2018-DEC-17
- Exported mesh from Grid Editor now ensures UV coordinates are generated
- Added support for per-side cross cost to grids of box topology
- Refactored pathfinding feature to support floating point values
- Support for Unity 2018.3

Version 7.4 2018-DEC-08
- Added Allow Highlight on Drag option
- Inspector: added "Clear" button next to Redraw
- Grid Editor: added Export Territory Mesh option
- Exported configurations: convenient Clear Grid and Reload Config buttons
- Replaced Hex Scale with Hex Width when using regular hexagons
- Added Match Cell Size option in inspector for box topology
- API: added CellGetInArea(bounds, list, padding)
- [Fix] Regression: exported cell settings do not apply over grid
- [Fix] Fixed issue with ignoreStartEndCellCanCrossCheck in FindPath method

Version 7.3 2018-NOV-06
- API: added ignoreStartEndCellCanCrossCheck optional parameter to FindRoute method
- Improved performance and accuracy of grid's mesh/surface generation
- [Fix] Reduced internal minimum vertex distance constant to allow really small cells

Version 7.2 2018-SEP-18
- Added camera property just in case main camera is not set
- API: added CellGetFromGroup: retrives all cells from a group
- API: added CellScaleSurface, TerritoryScaleSurface
- Memory and performance optimizations

Version 7.1 2018-AUG-27
- API: added CellGetCrossCost; CellGetSideCrossCost now accepts direction argument.
- API: added CellSetCrossCost(cell1, cell2) / CellGetCrossCost(cell1, cell2)

Version 7.0 2018-AUG-21
- Added repetition option to Cell and Territory flash/blink/... effects
- Added CellCancelAnimations / TerritoryCancelAnimations
- Added option to respect offset & scale when applying grid mask
- Added texture read enabled check to grid mask
- API: Added TerritoryGetFrontierCells
- API: Added CellGetGameObject / TerritoryGetGameObject
- API: Added CellSetSideCrossCost, CellSetCrossCost, CellGetSideCrossCost
- [Fix] Fixed cells Max Slope issue

Version 6.8 2018-JUL-24
- New demo scene 18: progressive zoom
- Added Transparent Background option (requires any other background geometry that draws to zbuffer)
- API: Added WarmCells which pregenerate cell geometry for faster performance in gameplay
- API: Added GetRect which returns the world space rectangle enclosing the grid
- API: Added HideHighlightedRegions which cancels current highlighted cell or territory
- [Fix] Fixed territory boundary update issue when it does not contain any cell

Version 6.7 2018-JUL-15
- API: added CellGetNormal
- [Fix] Fixed calculation bug with CellGetAtPosition when grid center/scale parameters are used

Version 6.6 2018-JUL-10
- Improved CellGetAtPosition performance (now O(1) speed)
- Grid Editor: shift now allows quick selection of cells as mouse move over them
- Grid Editor: selection marker now scales according to cell size
- API: Added optional territoryIndex to CellGetAtPosition which restricts the search making it even faster
- API: ability to provide a list of Voronoi site points using voronoiSites property

Version 6.5 2018-JUN-07
- Added Regular Hexagons option to inspector with Hexagon Scale parameter
- API: Added CellSetVisibility(obj, status) where obj can be a Rect, Collider, GameObject, Renderer or Bounds
- API: Added CellSetColor (simplified shortcut for CellToggleRegionSurface)

Version 6.4.2 2018-MAY-23
- [Fix] Fixed null exception when toggling certain cells with a texture

Version 6.4.1 2018-MAY-21
- [Fix] Fixed harmless inspector error message when TGS is selected and clicking outside the grid in SceneView

Version 6.4 2018-MAY-14
- Grid Editor: added multiple selection with control key (hold Control key to select multiple cells)
- [Fix] Grid Editor: prevents terrain selection when clicking on grid cells

Version 6.3.2 2018-MAY-11
- API: SetGridCenterWorldPosition now repositions the grid at any position with snapping option. Demo scene 10b updated.

Version 6.3.1 2018-APR-30
- [Fix] Fixed issue with disposal manager and multiple terrain grid instantiations

Version 6.3 2018-APR-20
- New memory manager avoids memory leaks when playing the scene several times inside Unity Editor
- [Fix] Fixed issue with some cells not being cleared when calling CellClear() method
- [Fix] Fixed issue with CellFadeOut no reseting the fade amount value
- [Fix] Fixed border cells not triggering OnCellExit event
- [Fix] Fixed error in TGS inspector on Unity 2018.1
- [Fix] Workaround for bug in Unity 2018.1 with colors and styles in the inspector on Metal 
- [Fix] Fixed rare pathfinding bug which prevented some cells to be included in the GetCellNeighbours result
- [Fix] Fixed bug with right button click event
- [Fix] Fixed textured cells not accepting transparency

Version 6.2 2018-JAN-29
- Added highlight effect none (allows selection of cells and territories but does not show highlight effect)
- Added neutral territories (API: TerritorySetNeutral / TerritoryIsNeutral). Neutral territories do not dispute frontiers.
- [Fix] Fixed rare issue with territories surfaces when changing cell ownership
- [Fix] Removed harmless error message related to material lacking a mainTexture property

Version 6.1 2018-JAN-9
- Added new highlight effect options (texture additive, texture multiply, texture color, texture scale)

Version 6.0 2018-DEC-28
- Editor: added toggle to enable/disable grid editing options in Scene View
- API: Ability to assign cells to groups with CellSetGroup/CellGetGroup
- API: FindPath method now accepts cell group mask as argument to consider only certain cell groups
- API: Added CellGetLineOfSight() for Line of Sight computation. Example in demo scene 12.
- API: CellGetNeighbours now accepts a range/distance. Range example in demo scene 12.
- API: CellColorTemp, temporarily colors a cell or list of cells
- API: Added CellGetHexagonDistance
- Highlight Fade now accepts a range for increased effect flexibility
- Added Hightlight Speed option
- Max number of territories increased to 512
- Performance improvements

Version 5.2 2017-NOV-21
- New cell border thickness option (uses geometry shader, SM 4.0 required)
- Minor usability and performance improvements
- [Fix] Fixed issue with mesh vertex limit on very large colored surfaces
- [Fix] Fixed issue when clicking on cells in SceneView deselecting TGS

Version 5.1 2017-NOV-13
- New demo scene 17: using a texture to color cells
- Improved event system with new events: OnCellMouseDown, OnCellMouseUp, OnCellClick, OnTerritoryMouseDown, OnTerritoryMouseUp, OnTerritoryClick
- [Fix] Fixed "Respect Other UI" regression issues on mobile

Version 5.0.1 2017-NOV-6
- Added "Near Clip Fade" toggle to inspector
- API: Added TerritoryGetPosition, TerritoryGetRectWorldSpace, TerritoryGetVertexCount, TerritoryGetVertexPosition
- [Fix] Fixed highlighting issue on WebGL platform
- [Fix] Fixed highlighting issue when using CellFadeOut

Version 5.0 2017-OCT-4
- TGS now requires Unity 5.5 or later
- New demo scene 16: simple matching game
- API: new variants for CellBlink and CellFlash
- API: added CellGetRect to obtain the rectangle enclosing any given cell in local or worldspace

Version 4.9.2 2017-SEP-1
- New demo scene 15: create grid dynamically at runtime
- Cell tag field can now be modified in the Grid Editor
- [Fix] Fixed LoadConfiguration issue with cell colors

Version 4.9.1 2017-JUL-16
- [Fix] Fixed TerritogyGetNeighbours returning 0 elements

Version 4.9 2017-JUN-27
- Added EvenLayout option to hexagonal grid
- Added CellFlash, CellBlink, TerritoryFlash and TerritoryBlink new effects

Version 4.8 2017-MAY-31
- Added Max Movement Cost to path finding functions
- Minor optimizations (less GC when creating cells)
- [Fix] Fixed grid scale regression bug introduced in 4.7
- [Fix] Fixed territory disputed frontiers color issue

Version 4.7 2017-MAY-23
- Added new parameter to CellToggleRegionSurface and TerritoryToggleRegionSurface to specify if colored surface is shown on top of objects or on the ground
- [Fix] Fixed path finding missing some cells under heavy usage
- [Fix] Fixed OnCellEnter/OnCellExit firing null errors

Version 4.6.1 2017-MAR-14
- [Fix] Fixed cell visible property being ignored during redraw call
- [Fix] Fixed RespectOtherUI on mobile

Version 4.6 2017-MAR-07
- New demo scene 14 "PathFinding over terrain"
- Updated demo scene 10b with option to show neighbour cells
- Added Max Slope parameter to hide cells hanging over terrain edges
- Added Minimum Altitude parameter to hide cells under certain altitude
- Added gridMeshDepthOffset and gridSurfaceDepthOffset for improved control of zfighting issues
- Added gridCenterWorldPosition to simplify grid centering (also simplified gridCenter concept to be in the range of -0.5 to 0.5 irrespective of grid scale)
- [Fix] Fixed some issues when coloring certain hexagonal cells
- [Fix] Fixed/handled compatibility warnings with Unity 5.6

Version 4.5 2017-JAN-13
- New demo scene 10b: Grid around character
- Upped max cell count
- [Fix] Fixed internal territories issues with overlapping edges

Version 4.4 16-DEC-2016
- Added RespectOtherUI to prevent pointer interactions when it's over an UI element in front of the grid
- [Fix] Fixed depth offset parameter not being applied correctly
- [Fix] Fixed texture list empty in Editor
- [Fix] Fixed Unity 5.5 compatibility issues

Version 4.3 16-NOV-2016
- New events: OnTerritoryHighlight/OnCellHighlight with option to cancel highlight
- New territoryDisputedFrontiersColor property.
- Ability to set individual territory frontier color using TerritorySetFrontierColor
- Editor changes are now registered so Unity asks for changes to be saved

Version 4.2 26-SEP-2016
- Ability to define territories by using a color texture
- Support for enclaves (territories surrounding other territories)
- Public API reorganization

Version 4.1 - 20-SEP-2016
- Ability to add color textures to define territories
- New demo scene #13

Version 4.0 - 10-SEP-2016
  - A* Path Finding system. New demo scene 12.
  - Faster cells and territory line shaders
  - Added new properties to the inspector to control near clip fade amount and falloff
  - PathFinding now works with POT and non POT grid sizes
  - [Fix] Fixed near clip fade effect with orthographic camera

Version 3.2 - 30-AUG-2016
  - Ability to add two or more grids to same terrain
  - New Elevation Base property to allow set higher heights to grid over terrain
  - Updated demo scene 6b showing how to get the row/column of the cell beneath a gameobject and fade neigbhours/range of cells
  - [Fix] Minor fix to CellGetAtPoint method which returned null when positions passed crossed the terrain

Version 3.1 - 24-JUL-2016
  - New option in inspector to specify the minimum distance to camera for a cell to be selectable
  - [Fix] Changed hexagonal topology so all rows contains same number of cells

Version 3.0 - 02-JUL-2016
  - New grid editor section with option to load configurations

Version 2.2 - 23-JUN-2016
  - New demo scene #11 showing how to transfer cells to another territory
  - Ability to hide territories outer borders
  - New API: CellSetTerritory.
  - Redraw() method now accepts a reuseTerrainData parameter to speed up updates if terrain has not changed
  - [Fix] Fixed lower boundary of territory in hexagonal grid

Version 2.1 - 03-JUN-2016
  - New demo scene #10 showing how to position the grid inside the terrain using gridCenter and gridScale properties.
  - Cells will be visible if at least one vertex if visible when applying mask.
  - Canvas texture now works with territories also

Version 2.0 - 24-MAY-2016
  - Added mask property to define cells visibility.
  - CellGetAtPosition now can accept world space coordinates.
  - Option to prevent highlighting of invisible cells
  - [Fix] Fixed bug in territory frontiers line shader

Version 1.4.0 - 12-MAY-2016
  - Added grid center and scale properties.
  - [Fix] CellGetPosition and CellGetVertexPosition now takes into account terrain height

Version 1.3.2 - 15-APR-2016
  - [Fix] Fixed compatibility with Orthographic Camera
  - [Fix] Fixed CellMerge out of bounds error

Version 1.3.1 - 04-APR-2016
  - Grid configuration now supports specifying exact column and row number for box and hexagonal topologies
  - Added new Demo7 scene.
  - Added cellRowCount, cellColumnCount
  - Added CelGetAtPosition(column, row)
  - Added CellGetVertexCount, CellGetVertexPosition
  - [Fix] Fixed CellGetCenterPosition in stand-alone mode

Version 1.2 - 04-JAN-2016
  - Added new Demo5 scene with cell fading example.
  - Can fade out cells and territories with a single function call.
  - Added new Demo6 scene with cell position and vertices locating example.
  - Some internal performance optimizations

Version 1.1 - 26-NOV-2015
  - Added new Demo3 and Demo4 scenes.
  - Can assign a canvas texture for all cells
  - Added new events: OnTerrainEnter, OnTerrainExit, OnTerrainClick, OnCellEnter, OnCellExit, OnCellClick.
  - Added cell visibility field.
  - Added CellSetTag and CellGetWithTag methods.
  - Cells can be customized with individual or canvas textures.

Version 1.0 - Initial launch 7/10/2015







