# 버그 리포트: Unity AI MCP 승인 큐/승인 상태 불일치

| 항목 | 내용 |
|---|---|
| 날짜 | 2026-07-07 |
| 대상 | Unity AI MCP / `UnityMcpApprovalWorkaround.cs` |
| 환경 | Unity 6000.4.8f1 / Unity AI Assistant 2.13.x / Windows |
| 상태 | 원인 확인, 공식 수정 전까지 우회 유지 |

## BUG-012: 승인 후에도 `No pending approval found for identity`가 발생함

### 증상

- Unity AI MCP 연결 승인 과정을 진행해도 연결이 유지되지 않는다.
- 에디터 쪽에서 다음 계열 메시지가 나타난다.

```text
No pending approval found for identity
```

- 기존 승인 토큰이나 승인 상태가 UI와 실제 transport 상태를 일치시키지 못한다.

### 원인

- Unity AI Assistant 2.13.x 계열에서 MCP 승인 큐가 정상적으로 유지되지 않는다.
- 승인 플로우보다 먼저 process validation 또는 direct connection policy가 개입하면서 승인 상태가 꼬인다.
- 승인 완료 후에도 bridge가 이전 상태를 들고 있어 재연결 시 승인 정보가 사라진 것처럼 보인다.

### 조치

- 프로젝트에 `Assets/Editor/UnityMcpApprovalWorkaround.cs`를 두어 임시 우회를 적용했다.
- 우회 내용:
  - `processValidationEnabled = false`
  - `requiresApproval = false`
  - 설정 저장 후 bridge 재생성

### 주의 사항

- 이 우회는 로컬 MCP 연결 검증을 약하게 만든다.
- 신뢰할 수 있는 로컬 클라이언트만 사용해야 한다.
- Unity가 공식 수정본을 제공하면 이 우회 스크립트는 제거해야 한다.

### 남은 검증

- [ ] Unity AI 공식 수정 버전 확인
- [ ] 우회 제거 후 승인 절차 정상 동작 여부 확인
- [ ] 재실행 후 자동 재연결 상태 확인
