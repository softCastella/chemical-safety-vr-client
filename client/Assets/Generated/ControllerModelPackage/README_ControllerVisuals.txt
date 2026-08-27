PPE Controller Visuals - Left / Right
=====================================

Contents
- Prefabs/LeftControllerVisual.prefab
- Prefabs/RightControllerVisual.prefab
- ControllerVisualDependencies.txt

Usage
1. Import the unitypackage into the target Unity project.
2. Place LeftControllerVisual under the target rig's left tracked controller Transform.
3. Place RightControllerVisual under the target rig's right tracked controller Transform.
4. Keep each visual prefab's local position/rotation at zero and local scale at one.

Asset dependencies
All dependencies located under Assets, including referenced FBX models, materials, animation assets, and project-owned scripts, are embedded in the unitypackage.

Package Manager prerequisites for full behaviour (install separately)
- Universal Render Pipeline 17.4.x
- Input System 1.19.x
- XR Interaction Toolkit 3.4.x
- XR Hands 1.7.x
Package Manager source files are intentionally not embedded in the unitypackage. The visual meshes can still be reused without the tracking rig, but any imported component whose package is absent will require that package to be installed.
