# 🎮 Simple First Person Controller for Unity - **FPController**

A lightweight, modular first-person controller built using Unity's **New Input System** and the built-in `CharacterController` component. Supports sprinting, jumping, slope and stair navigation, full air control, head bobbing, and dynamic footstep audio.

---

## 🧰 Features

- ✅ Walk, sprint, jump
- ✅ Slope and stair navigation
- ✅ Full air control
- ✅ Camera headbobbing
- ✅ Footstep sounds
- ✅ Camera shake support
- ✅ Modular architecture (plug & play)

---

## 📦 Requirements

- **Unity Version:** works up to Unity 6.1
- **Packages:**
  - Input System (`com.unity.inputsystem`)
  - SRP & URP support

---

## 🚀 Getting Started

### 1. Clone or Download

Download or clone this repository into your Unity project:

```bash
https://github.com/GTroubley/Simple-First-Person-Controller.git
```

### 2. Create Required Layer‼️

Create a layer named **Player** and assign it to the controller prefab and all its child objects.

### 3. Add Prefab to Scene

Use one of the prefabs from the GameObjects folder based on your render pipeline:

```
GameObjects/
```

- `FPController-Builtin`
- `FPController-URP`

### 4. Assign Input Actions

Input actions can be found at:

```
Input/PlayerInputControls
```

---

## 🧩 Script Components

| Script            | Description                                                                 |
|-------------------|-----------------------------------------------------------------------------|
| `FPInputManager`  | Handles all player input using Unity's New Input System.                    |
| `FPController`    | Core movement logic: walking, gravity, slopes, stairs, and camera rotation. |
| `FPJumping`       | Enables jumping behavior.                                                   |
| `FPSprint`        | Enables sprinting functionality.                                            |
| `FPHeadbobbing`   | Adds vertical camera motion while walking/running.                          |
| `FPFootsteps`     | Plays footstep audio based on movement.                                     |
| `CameraShake`     | Triggers screen shake on the camera object.                                 |

---

## 💡 Some Examples

Shake the camera using:

```csharp
CameraShake.Shake(0.5f, 0.15f);
```

---

## 🙏 Special Thanks

This controller was inspired by [passivestar](https://www.youtube.com/@passivestar).

---

## 🚧 Development Notice

The controller is still under active development, alongside my next game project.  
If you use this controller in your own game, please credit me. 😊
