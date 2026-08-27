# 2026-07-07 Unity AI MCP 승인 오류 후속 회의록

- 날짜: 2026-07-07
- 대상: Unity AI MCP / 승인 플로우 / 로컬 bridge

## 논의 목적

- Unity AI MCP에서 `No pending approval found for identity`가 남는 문제를 정리한다.
- 현재 프로젝트에 적용된 임시 우회가 무엇인지 기록한다.

## 확인 내용

- Unity AI Assistant 2.13.x 계열에서 승인 큐가 불안정하다.
- 승인 완료 후에도 bridge가 이전 검증 상태를 유지해 재연결 시 오류가 반복된다.
- 프로젝트에는 `Assets/Editor/UnityMcpApprovalWorkaround.cs`가 들어가 있다.

## 반영 내용

- process validation을 끄는 임시 우회를 유지한다.
- direct connection의 approval 요구를 비활성화한다.
- 설정 저장 후 bridge를 재생성해 stale transport 상태를 끊는다.

## 수정 파일

- `Assets/Editor/UnityMcpApprovalWorkaround.cs`

## 검증 상태

- 현재 이 문제는 완전 해결이 아니라 우회 상태다.
- Unity 공식 수정 버전 확인이 필요하다.
- 우회 스크립트 제거 시점은 공식 수정 확인 후로 미룬다.

## 다음 작업

- Unity AI 쪽 승인 오류와 Codex/WebSocket 연결 실패를 서로 다른 이슈로 계속 분리한다.
- 공식 수정 여부를 확인한 뒤 우회 스크립트 제거를 검토한다.
