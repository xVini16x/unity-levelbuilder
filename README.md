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
    - You can use any 3D modeling software to create meshes. Please be aware that the prefabs need to stick to some conditions
	  - basic wall needs to have the same length as the floor
	  - wall and corner thickness needs to be the same
	  - corners and floor neeed to have a square shape
	  - length of shortened walls (left and right) need to match the length of basic wall minus corner length

---

## License

- **[MIT license](http://opensource.org/licenses/mit-license.php)**
