# 버그 리포트: Codex-Unity MCP WebSocket 연결 실패

| 항목 | 내용 |
|---|---|
| 날짜 | 2026-07-07 |
| 대상 | `.codex/config.toml` / Unity MCP relay |
| 환경 | Windows / Unity 6000.4.8f1 / Codex MCP relay |
| 상태 | 원인 확인 및 설정 수정 완료 |

## BUG-011: `connection.state_chage` 가 `Failed`로 떨어지며 WebSocketException 발생

### 증상

- Unity 콘솔 또는 연동 로그에서 다음 형태의 메시지가 남았다.

```text
connection.state_chage oldState=Connecting newState=Failed error=WebSocketException
```

- Codex와 Unity MCP 사이의 연결이 성립되지 않아 도구 호출이 실패했다.

### 원인

- `.codex/config.toml`의 Unity relay 경로가 실제 사용자 계정 경로와 달랐다.
- 설정은 `C:\Users\user\...`를 가리키고 있었고, 실제 설치 경로는 `C:\Users\lanoc\...`였다.
- relay 실행 파일을 찾지 못한 상태에서 MCP 연결을 시도하면서 WebSocket 연결이 실패했다.

### 조치

- `.codex/config.toml`의 `command`와 `--project-path`를 실제 경로로 수정했다.
- 수정 후 경로는 아래와 같다.

```toml
command = "C:\\Users\\lanoc\\.unity\\relay\\relay_win.exe"
args = [
  "--mcp",
  "--project-path",
  "C:\\Users\\lanoc\\Documents\\Workspace\\3D_UI_Test_home",
]
```

### 확인한 점

- `C:\Users\lanoc\.unity\relay\relay_win.exe`는 실제로 존재한다.
- `C:\Users\user\.unity\relay\relay_win.exe`는 존재하지 않는다.
- Unity 로그에 보이던 라이선스 404, XR `StopSubsystems` 경고는 별도 문제로 보인다.

### 남은 검증

- [ ] Unity Editor 재시작 후 MCP 연결 상태 재확인
- [ ] Codex 도구 호출 성공 여부 확인
- [ ] `connection.state_chage` 실패 로그 재발 여부 확인

## 미해결 항목

- Unity AI 관련 에러는 아직 별도로 남아 있다.
- `connection.state_chage` / WebSocket 연결 실패도 아직 완전히 해결되지 않았다.
- 다음 작업에서는 Unity AI 에러와 연결 실패를 분리해서 각각 원인을 확인해야 한다.
