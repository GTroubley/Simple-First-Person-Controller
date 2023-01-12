# Simple First Person Controller for Unity - FPController

A simple first person controller using Unity's **new input system** & build-in CharacterController. The controller can move, sprint, jump, handle slopes & stairs, full-no-limited air control, camera headbob and footsteps.

## Getting Started

The ready for use gameObject prefabs can be found inder the folder: ```GameObjects/...``` (FPController-Builtin & FPController-URP).

Input actions can be found at ```Input/PlayerInputControls```.

Before using the prefab, make sure to create a "Player" layer and assign it to the controller prefab and it's children.

### Script Components

* FPInputManager: Provides input.
* FPController: Is the main script responsible for the player movement, gravity, slope handling & camera rotation. 
* FPJumping: This component enables jumping.
* FPSprint: This component enables sprinting.
* FPHeadbobbing: Can move the camera holder gameObject on Y-axis.
* FPFootsteps: Responsible to play movement sounds.
* CameraShake: Script attached to the camera gameObject, can shake the camera.

## Notes

* This first person controller uses the Unity's new input system.

## Some Examples

Example to add camera shake using the static method Shake(duration,strength):

```csharp
CameraShake.Shake(0.5f, 0.15f);
```

## Special Thanks
This controller was inspired by [passivestar](https://www.youtube.com/@passivestar)
