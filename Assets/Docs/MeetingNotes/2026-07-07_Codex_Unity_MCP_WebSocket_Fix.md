# 2026-07-07 Codex-Unity MCP WebSocket 실패 대응 회의록

- 날짜: 2026-07-07
- 대상: Codex / Unity MCP relay / `.codex/config.toml`

## 논의 목적

- `connection.state_chage oldState=Connecting newState=Failed error=WebSocketException` 로그의 원인을 정리한다.
- Unity와 Codex 연결이 실패하는 상황을 재발 없이 처리한다.

## 확인 내용

- `.codex/config.toml`의 relay 경로가 `C:\Users\user\...`로 잘못 잡혀 있었다.
- 실제 relay 실행 파일은 `C:\Users\lanoc\.unity\relay\relay_win.exe`에 있었다.
- 프로젝트 경로도 `C:\Users\lanoc\Documents\Workspace\3D_UI_Test_home`로 맞춰야 했다.
- Unity 콘솔의 XR `StopSubsystems` 경고와 라이선스 404 로그는 별개 이슈로 판단했다.

## 반영 내용

- `.codex/config.toml`의 Unity MCP relay 경로를 실제 사용자 경로로 수정했다.
- `--project-path`도 현재 워크스페이스 경로로 수정했다.
- 이후 WebSocket 연결 실패가 경로 문제였는지 재검증하도록 정리했다.

## 수정 파일

- `.codex/config.toml`
- `Assets/Docs/Bug/2026-07-07_Codex_Unity_MCP_WebSocket_Failed.md`

## 검증 상태

- relay 실행 파일 존재 여부를 확인했다.
- 잘못된 `C:\Users\user\...` 경로는 더 이상 사용하지 않도록 변경했다.
- Unity 재시작 후 MCP 연결과 도구 호출 성공 여부를 후속 확인해야 한다.

## 다음 작업

- Unity AI 에러를 먼저 분리해서 로그를 확인한다.
- `connection.state_chage` WebSocket 실패를 다시 재현하고 원인을 좁힌다.
- 두 문제를 같은 원인으로 묶지 말고, 각각 독립적으로 추적한다.
