# Simple-First-Person-Controller
## A simple first person controller using Unity's **new input system** & build-in CharacterController. The controller can move, sprint, jump, handle slopes & stairs, full-no-limited air control, camera headbob and footsteps.
Note: **This first person controller uses the _Unity's new input system_.**

![1](https://user-images.githubusercontent.com/95410107/211865702-cb372800-1cb3-46e5-8b47-a0d6d54fabb9.png)

Under the folder: ```GameObjects/...``` I have included the player gameobject with it's components and scripts, ready for use.

In order to add camera shake use the static method Shake(duration,strength), example:
```csharp
CameraShake.Shake(0.5f, 0.15f);
```
