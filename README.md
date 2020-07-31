# Unity Levelbuilder Tool

> Package for Unity (2019.4) to prototype levels efficiently based on 3D rooms and hallways.<br>
> Main features are room spawning and extension while using your own prefabs for walls, corners and floors.

Room Spawning             |  Room Extension
:-------------------------:|:-------------------------:
<img src="/Documentation/img/RoomSpawnerWithText.gif" width="600" />  |  <img src="/Documentation/img/RoomExtensionWithText.gif" width="600" />

---

## Table of Contents

- [Installation](#installation)
- [Documentation](#documentation)
- [Team](#team)
- [FAQ](#faq)
- [License](#license)

---

## Installation

- We provide a working demo here: `https://github.com/xVini16x/unity-levelbuilder-testproject`
- There are different ways to install unity packages
  - We propose installing from a Git URL `https://docs.unity3d.com/Manual/upm-ui-giturl.html`
  - For other installation options please refer to `https://docs.unity3d.com/Manual/PackagesList.html`

---

## Purpose

The tool is developed for usage in applications that have a fixed camera that is looking in the positive z-axis in Unity as it was developed with 2.5D Games in mind. Therefore that are at the back side of the room (the one closest to the camera) will be made completly transparent while others have their top still visible. If working with a perspective camera working with dynamic visible / invisible side walls through a shader is recommended.

--

## Documentation 

### Room Spawning

1. Open the Room Spawner window:
   - <img src="/Documentation/img/OpenSpawner.png" width="600" />

2. Assign your prefabs accordingly and choose the room size:
   - <img src="/Documentation/img/SpawnerGUI.png" width="600" />

3. Click the "Create room" button to create your room. 
   - Note that the smallest possible room contains out of one floor, four walls and four corners.

Small Room | Medium Room
:-------------------------:|:-------------------------:
<img src="/Documentation/img/spawnSmallRoom.png" width="300" /> | <img src="/Documentation/img/spawnMediumRoom.png" width="500" />

### Room Extension

1. Select the custom tool for room extension:
   - <img src="/Documentation/img/SelectCustomToolForExtending.png" width="600" />
2. Select a wall game object:
   - <img src="/Documentation/img/Extend_SelectWall.png" width="600" />
3. Drag the handle to extend the room:
   - <img src="/Documentation/img/Extend_WallBy1.png" width="600" />
4. Drag more to create hallways:
   - <img src="/Documentation/img/Extend_MultipleWalls.png" width="600" />
     
### Creating own 3D models 

All 3D models that can be rendered by the Unity MeshRenderer Component can be used for the room creation if they follow the following rules:

  - For the tool to work correctly the 3D Models need to have their origin in the center of the 3D-Model for all axis and they need to have their correct orientation without having any rotation applied. (Front sides are oriented towards negative z-direction)
  - Model for full wall needs to have the same length as the floor (x-size of wall needs to be same as x/z of floor)
  - Wall and corner thickness needs to be the same (z-size of wall needs to be the same as x/z of corner)
  - Corners and floor neeed to have a square shape (in x/z-direction)
  - Length (x-direction) of wall shortened on one side (left or right) need to match the length of full wall minus corner length (x-direction)
  - Length (x-direction) of wall shortened on both side needs to match the length of full wall minus two corner lenghts (x-direction)
  - All models have one material slot for each side (front, back, left, right, top, bottom)
 
<img src="/Documentation/img/All_Sizes_with_Arrows.jpg" width="600" /> 
Elements from left to right: Corner, Wall Shortened Both Sides, Wall Shortened Left, Wall Shortened Right, Full Wall.
<br>
 
To use the models they need to be included in a prefab that obeys to the following rules:
  - The prefab needs to include the MeshRenderer somewhere in it's hierarchy (doesn't need to be top element)
  - The prefab needs to have a MaterialSlotMapper Component attached in which all indices are correctly assigned.
  - All wall prefabs have the correct material attached to their front- and top-side and are from all other sides transparent (materials for the left and right side will be dynamically assigned when the wall is spawned
  - Corner prefabs have the correct material assigned to their front-, right- and top-side all other sides are transparent.
  - The floor prefabs have the correct material assigned to all sides.

After you created the prefabs you can add them as default values in the Project Settings or can directly add them in the RoomSpawner Window. 

---

## Team

 <a href="https://github.com/xVini16x" target="_blank">Vincenzo Angrisano</a> | <a href="https://github.com/kichriwa" target="_blank">**Kira Wanjek**</a> 
 :---: |:---:
 <img src="/Documentation/img/photoVincenzo.jpg" width="220" /> | <img src="/Documentation/img/photoKira.jpg" width="390" />
 `vincenzo.angrisano(@)haw-hamburg.de` | `Kira.Wanjek(@)haw-hamburg.de` 

Students of HAW Hamburg<br>
Faculty of Design, Media and Information<br>
Department of Media Technology<br>
Master of Arts (M.A.): SOUND – VISION – GAMES<br>
Programming 1 - Summer Term 2020

---

## FAQ

- **I have a lot of exceptions and the tool does not behave as expected. What did I do wrong?**
	- Rooms spawnned and extended with this tool rely on an internal data structure. If you modify the room manually for example by deleting or moving parts, the data structure can get broken and the tool features will not work anymore.
- **How do I create my own prefabs for *room spawning*?**
    - You can use any 3D modeling software to create meshes. Please be aware that the prefabs need to stick to some conditions. See [here](#creating-own-3d-models) for more information.

---

## License

- **[MIT license](http://opensource.org/licenses/mit-license.php)**
