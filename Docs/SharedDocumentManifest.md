# 교차 저장소 공유 문서 목록

## 목적

Unity XR 클라이언트와 Express 서버를 별도 저장소에서 작업하더라도 API, 데이터 계약, 대시보드, 웹사이트와 배포 관련 판단 근거를 양쪽 Codex가 확인할 수 있도록 공용 문서의 미러링 범위를 정의한다.

전체 프로젝트 문서의 기준본은 `softCastella/chemical-safety-vr-client`에 둔다. 서버 작업에 직접 필요한 문서는 `softCastella/chemical-safety-vr-server`의 같은 상대 경로에 미러링한다.

## 공유 문서

- `Docs/MeetingNotes/2026-08-21_PPE_Training_Dashboard_Concept.md`
  - 훈련 데이터 이벤트, 계산 규칙, 대시보드 해석과 제한 사항
- `Docs/MeetingNotes/2026-08-25_Production_Server_Meta_Horizon_Release_Plan.md`
  - 운영 서버, Meta Horizon 배포와 계정·인증 연동 계획
- `Docs/MeetingNotes/2026-08-26_Brand_Home_VR_Detail_Website_Layout.md`
  - 서버가 제공하는 브랜드 홈페이지와 VR 상세페이지의 현재 구조 및 검증 기록
- `Docs/MeetingNotes/2026-08-26_PPE_Training_Data_Dashboard_Followup.md`
  - Unity 계측, JSONL, Express API와 실데이터 대시보드의 후속 계약 및 검증 기록
- `Docs/MeetingNotes/2026-08-30_Client_Server_Auth_Channel_Handoff.md`
  - Meta 인증, 로컬 등록·텔레메트리 API, 채널·큐 상태와 교차 저장소 인수인계

## 동기화 규칙

1. 공용 문서를 읽거나 수정하기 전에 클라이언트와 서버 저장소의 대상 브랜치 및 커밋 SHA를 확인한다.
2. 클라이언트 저장소의 기준본과 서버 저장소의 미러는 같은 상대 경로를 사용한다.
3. 서버 구현으로 공용 사실이 바뀌면 서버 미러를 갱신하고 작업 결과에 `클라이언트 문서 동기화 필요` 여부와 서버 커밋 SHA를 기록한다.
4. 최종 보고자료를 작성하기 전에 서버의 최신 코드·테스트·원본 이벤트를 확인하고 클라이언트 기준본에 반영한다.
5. 동기화 완료를 보고할 때는 양쪽 문서의 내용이 일치하는지 확인하고 두 저장소의 기준 커밋을 함께 기록한다.
6. 이 목록에 없는 Unity 전용 버그 리포트, 대형 이미지, PPT/PDF와 제작 자료는 서버 저장소에 일괄 복사하지 않는다. 서버 작업에 꼭 필요한 경우 사용자 범위 확인 후 이 목록에 추가한다.

## 최초 분리 기준

- 기존 모노레포: `softCastella/Final_VR_Tyche_Pivot`
- 기준 브랜치: `main`
- 기준 커밋: `97e1f36`
- 분리일: 2026-08-27
