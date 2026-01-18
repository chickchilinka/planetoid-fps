# Hot Planet (Work‑in‑Progress)

A small Unity project demonstrating custom surface gravity, 3rd‑person movement & camera, and foundational infrastructure. 3rd-person shooter in future.

![Gameplay Preview](docs/content/preview.gif)

## 🎮 What’s Here

- **Core Mechanics**
    - Custom surface gravity (mesh‑based normals)
    - Smooth 3rd‑person platformer a-like controller (variable‑height jumps, air control)

- **Infrastructure**
    - Bootstrap & loading flow
    - Window system
    - DI via Zenject, reactive programming via UniRx
    - Module with the basis of client-server interaction and implementation in Fishnet. See demo for basic client-server message exchange and RPC in `Demos/NetworkingDemo`.

## 🚀 Getting Started

1. Clone this repo
2. Open in Unity 6+
3. Hit **Game->Play** in the Toolbar

## 🛠 Tech Stack

- Unity 6 (LTS)
- Zenject
- UniRx
- Addressables (in future)
- FishNet

## 🏗 Architecture Overview

The project follows a modular, layered architecture focused on scalability, replaceability, and clear separation of responsibilities.

### Base Layer

`Base` contains isolated, reusable core modules and abstractions that are independent from concrete gameplay implementations.  
This layer defines:

- Core domain contracts and service interfaces
- Infrastructure primitives (lifecycle, messaging, composition helpers)
- Extension points for feature implementations
- Reactive and async orchestration

Base modules do not reference any concrete gameplay or SDK-specific code.  
They form a stable foundation that can be reused across different projects or replaced incrementally.

### Features Layer

`Features` contains concrete implementations of the extension points defined in `Base`.

This includes:

- Gameplay systems and integrations
- SDK adapters and infrastructure bindings
- Composition roots and dependency wiring
- UI and presentation logic

Features can evolve independently without impacting the core architecture.

## 📈 Main Goals Roadmap

- [ ] Networked multiplayer with authoritative server
- [ ] Player spawn system
- [ ] Damage and Weapon systems, HUD
- [ ] Backend for authentication, player profiles, lobbies
- [ ] Character & weapon animations
- [ ] UI, Settings
- [ ] Audio & SFX
- [ ] Art & VFX
- [ ] Input remapping & accessibility options
- [ ] Performance profiling & optimization
- [ ] Automated tests & CI/CD pipeline
- [ ] Analytics / telemetry framework
- [ ] Localization support
- [ ] Tutorial / onboarding flow
- [ ] Release a minimal MVP build

## 🗣 About Me

This is an early, in‑development prototype - my personal rewrite of Hot Planet game (https://youtu.be/uvDixcYVbT8) I've made long time ago. 
I’m actively iterating on it to shape a standalone MVP I can release.


