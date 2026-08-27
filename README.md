# Tyche Chemical Safety Training VR

Unity Quest client and Express API server managed in one private monorepo.

## Repository layout

- Unity project: `client`
- Express server: `server`
- Shared project documentation: `Docs`
- Repository-wide supporting scripts: `Tools`
- Codex and agent configuration: `.codex`, `AGENTS.md`

## Environment

- Unity 6000.4.8f1
- Universal Render Pipeline 17.4.0
- OpenXR
- XR Interaction Toolkit 3.4.1
- XR Hands 1.7.3
- Input System 1.19.0
- Targets: Meta Quest/Android and PC OpenXR

## Build scenes

The enabled build flow is configured in `client/ProjectSettings/EditorBuildSettings.asset` and currently runs from `0_App` through title, intro, `6_LoadingScene_0`, and `3_PPE_Room_3mode_loco`. Mixer scenes remain disabled follow-up content.

## Project support files

- Runtime code: `client/Assets/Scripts`
- Editor tools and validation harnesses: `client/Assets/Editor`
- Project documentation and regression reports: `Docs`
- Supporting scripts: `Tools`
- Server source and tests: `server/src`, `server/test`
- Codex and agent configuration: `.codex` and `AGENTS.md`

Unity-generated folders under `client`, such as `Library`, `Temp`, `Logs`, `obj`, and `UserSettings`, are intentionally not versioned.

## Local server

```powershell
cd server
npm install
npm run dev
```

The health endpoint is `http://localhost:3000/api/health`.
