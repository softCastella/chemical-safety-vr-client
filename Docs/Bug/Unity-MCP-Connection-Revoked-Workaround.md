# Unity MCP `Connection revoked` 승인 버그 및 우회

## 상태

- 상태: 프로젝트 로컬 우회 적용, MCP 호출 성공 확인
- 확인일: 2026-06-29
- Unity: 6000.4.8f1
- AI Assistant: `com.unity.ai.assistant` 2.13.0-pre.2
- Unity relay: 1.3.14 / `unity-ai-relay` 1.0.12-build.96
- MCP 클라이언트: Codex `codex-mcp-client` 0.142.4
- 플랫폼: Windows

## 증상

Unity의 **Project Settings > AI > Unity MCP Server**에서 Codex 연결을 허용해도 MCP 도구 호출이 실패했다.

```text
Connection revoked. Go to Unity Editor > Project Settings > AI > Unity MCP to change approval.
```

승인, 서버 Stop/Start, Unity 재실행, Codex 재실행을 반복해도 동일하게 재현됐다.

## 로그 근거

```text
Validation: Pending
Reason: Awaiting user approval
No pending approval found for identity Hash:<relay>|Hash:<codex>
```

Windows 실행 파일 서명 수집도 실패했다.

```text
Failed to collect Windows signature for ...\codex.exe:
Input data cannot be coded as a valid certificate.
```

승인 기록에는 `Approved by user from settings`가 저장됐지만 이미 거부된 활성 transport에는 반영되지 않았다.

## 원인

AI Assistant 2.13.0-pre.2의 MCP 연결 검증 및 승인 순서에 결함이 있다.

1. `ConnectionValidator`가 직접 연결을 `Pending`으로 반환한다.
2. `Bridge.ValidateAndApproveAsync`가 승인 정책 처리 전에 `decision.IsAccepted`를 검사한다.
3. `Pending` 결정이 거부 처리되어 transport가 `Denied` 상태가 된다.
4. Allow 버튼은 이미 사라진 pending approval을 완료하려 하므로 `No pending approval found`가 발생한다.
5. UI에 저장된 승인 상태와 실제 transport 상태가 불일치한다.

## 실패한 우회

### AI Assistant 2.6.0-pre.1로 다운그레이드

과거 연결 버그가 없다고 보고된 버전이지만 현재 Unity 백엔드에서 다음 오류가 발생한다.

```text
APINoLongerSupported
```

따라서 패키지는 2.13.0-pre.2로 복구했다.

### `requiresApproval`만 비활성화

`ConnectionValidator`의 `Pending` 판정이 정책 분기보다 먼저 거부되므로 해결되지 않았다.

### 활성 transport만 연결 해제

Bridge가 생성 시 검증 설정을 캐시하므로 transport 재연결만으로는 변경된 설정이 적용되지 않았다.

## 적용한 우회

프로젝트 전용 `Assets/Editor/UnityMcpApprovalWorkaround.cs`가 다음 작업을 수행한다.

1. `MCPSettingsManager.Settings.processValidationEnabled = false`
2. `connectionPolicies.direct.requiresApproval = false`
3. `MCPSettingsManager.SaveSettings()` 호출
4. 초기화 약 3초 후 `UnityMCPBridge.Enabled`를 `false`, `true`로 변경해 Bridge를 재생성

적용 로그:

```text
Unity MCP workaround active: broken process validation and pending-approval queue bypassed.
Unity MCP workaround: Bridge recreated with validation bypass active.
Connection validation is DISABLED
```

## 검증 결과

Codex에서 `Unity_GetConsoleLogs` 호출이 성공했다.

```text
success: true
message: Tool 'GetConsoleLogs' executed successfully
```

## 보안 영향

이 우회는 Unity MCP의 프로세스 신원 검증과 직접 연결 승인 절차를 비활성화한다. 신뢰할 수 있는 로컬 MCP 클라이언트만 실행해야 한다.

- 신뢰하지 않는 로컬 프로세스를 실행하지 않는다.
- 공식 수정 버전 적용 후 우회 스크립트를 제거하고 프로세스 검증을 다시 활성화한다.

## 제거 조건

공식 수정 버전에서 아래 조건을 모두 확인한 후 우회 스크립트를 제거한다.

1. 프로세스 검증 활성 상태에서 Codex 연결이 Pending으로 표시된다.
2. Allow 클릭 시 `No pending approval found`가 발생하지 않는다.
3. Unity와 Codex 재실행 후 자동 재연결된다.
4. `Unity_GetConsoleLogs`가 `success: true`를 반환한다.

## 관련 공식 이슈

- https://issuetracker.unity3d.com/issues/mcp-tools-return-connection-revoked-when-used-by-an-ai-client
