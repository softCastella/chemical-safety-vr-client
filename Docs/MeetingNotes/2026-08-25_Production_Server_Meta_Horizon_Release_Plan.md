# 실서버·Meta Horizon 입점 준비 순서와 저장소 구성

- 작성일: 2026-08-25
- 대상 프로젝트: `Final_VR_Tyche_Pivot`
- 관련 문서:
  - `Docs/MeetingNotes/2026-08-21_PPE_Training_Dashboard_Concept.md`
  - `Docs/MeetingNotes/2026-08-20_PPE_Room_Train_Test_Followup.md`

## 1. 문서 목적

처음에는 Unity 프로젝트가 저장소 루트에 있었으나 2026-08-25 현재 `Assets`, `Packages`, `ProjectSettings`가 `client` 아래로 이동했고 루트에 `server` 폴더가 생성됐다. 회사 홈페이지와 대시보드 HTML, 공통 계약, 데이터베이스와 배포 구성의 실제 재배치는 아직 끝나지 않았다. 이 상태에서 테스트 서버부터 만들면 Unity 이벤트 형식, 서버 API, 대시보드 계산 기준과 배포 위치가 뒤늦게 충돌할 수 있다.

따라서 작업 순서는 다음과 같이 바로잡는다.

```text
기존 작업 보존
→ 저장소의 client/server 모노레포 구조 확정
→ Unity와 서버가 공유할 데이터 형식 확정
→ 로컬 서버
→ 테스트 서버
→ Unity 실제 기록 연동
→ 대시보드 실제 API 연동
→ 실서버
→ Meta 내부 배포
→ 정식 심사 신청
```

**테스트 서버는 저장소 구조를 먼저 구성한 뒤 만든다.** Meta Horizon 심사 신청은 실서버와 Quest 전체 흐름 검증이 끝난 뒤 진행한다.

## 2. 현재 확인된 상태

### 2.1 Unity 앱

- Unity 프로젝트 기준 경로는 저장소 루트에서 `client`로 이동했다. 현재 기준 파일은 `client/Assets`, `client/Packages`, `client/ProjectSettings`에 있다.
- Android 앱 ID는 `com.softcastella.prototype.tyche.jinyoung`이다.
- `productName`은 `Prototype_Tyche_Jinyoung`이다.
- 앱 버전은 `0.1.0`, Android 빌드 번호는 `1`이다.
- Android ARM64와 IL2CPP는 설정되어 있다.
- Android 최소 SDK는 현재 `25`, Target SDK는 Unity 자동 선택값이다.
- 출시용 Android Keystore와 Key Alias는 아직 설정되지 않았다.
- Meta Quest용 OpenXR 기능과 Oculus Touch Controller Profile은 활성화되어 있다.
- Meta XR Platform SDK(`com.meta.xr.sdk.platform`) `205.0.0`이 `client/Packages/manifest.json`과 `client/Packages/packages-lock.json`에 등록되어 있다.
- Meta 개발자 앱은 생성됐고 DUC는 제출·검토 대기 상태다. Quest App ID는 `client/Assets/Resources/OculusPlatformSettings.asset`에 설정되어 있다. App Secret은 Unity나 Git에 넣지 않는다.
- 빌드 씬은 `0_App`에서 시작해 타이틀·인트로·로딩·PPE 룸으로 이어진다.
- 현재 PPE 씬의 `AudioManager/PPE Voice Flow.m_KeyboardPresentationRoot`는 `Modal  Keyboard Canvas`를 계속 참조하지만 `m_SkipKeyboardNameInput=true`로 `NameInput` 진입을 임시 우회한다. 재사용 패키지를 만들었더라도 참조와 이름 제출 경로를 정식 전환하기 전에는 키보드 Canvas를 삭제하지 않는다.

### 2.2 홈페이지

- 정적 HTML 홈페이지를 `server/public/site`로 이동했다.
- 기존 `tycheworks-site-final-v2-260824/tycheworks-site-final-routed` 중첩 복사본은 웹에서 제공하지 않고 `server/archive/tycheworks-site-final-routed`에 보존했다.
- 기존 사이트의 `README.txt`와 `WORKLOG_2026-08-24.md`도 공개 경로에서 제외하고 `server/archive/site-notes`에 보존했다.
- 문의 폼은 `/api/contact`로 전송하도록 작성되어 있지만 이를 처리할 백엔드가 없다.
- 공개 개인정보처리방침은 별도 공개 GitHub Pages 주소에 게시되어 있다. 회사 홈페이지와 지원 URL의 최종 운영 주소는 실서버 전환 전에 확정한다.

### 2.3 대시보드

- `server/public/dashboard/index.html`은 구조 검증용 모의 데이터를 사용한다.
- Unity의 실제 수행 이벤트, 수집 API, 데이터베이스와 집계 API는 아직 연결되지 않았다.

### 2.4 서버·배포

- 저장소 루트의 `server`를 npm 프로젝트로 초기화했고 Express `5.2.1`을 설치했다.
- 최소 실행 진입점과 `GET /api/health` 상태 확인 API, Node 내장 테스트가 있다.
- users/external identities CRUD API와 MySQL migration runner를 구현했다.
- 프로젝트용 MySQL/MariaDB 연결, Meta 인증, 교육 기록 API, 관리자 인증과 배포 구성은 아직 없다.
- `.openai/hosting.json` 같은 프로젝트 배포 설정도 없다.
- 따라서 현재는 회사 홈페이지, 분석 API와 관리자 대시보드를 한 번에 운영할 수 있는 상태가 아니다.

## 3. 목표 저장소 구조 — 비공개 모노레포

현재 Git 저장소를 제품 전체의 루트로 유지하고, 그 안에서 Unity 애플리케이션을 `client`, Express API와 웹 제공 영역을 `server`로 분리한다. `.git`을 루트에 남겨 두므로 Unity와 서버의 호환되는 변경을 하나의 커밋과 이력으로 관리할 수 있다. 이 구성을 **모노레포(monorepo)**로 정의한다.

```text
Final_VR_Tyche_Pivot/
├─ client/
│  ├─ Assets/
│  ├─ Packages/
│  └─ ProjectSettings/
├─ server/
│  ├─ src/
│  │  ├─ routes/
│  │  ├─ services/
│  │  └─ middleware/
│  ├─ db/
│  │  ├─ migrations/
│  │  └─ seeds/
│  ├─ public/
│  │  ├─ dashboard/
│  │  └─ site/
│  ├─ archive/
│  ├─ test/
│  ├─ package.json
│  └─ .env.example
├─ shared/
│  └─ contracts/
│     ├─ Unity가 보내는 이벤트 형식
│     └─ 대시보드가 받는 응답 형식
├─ infra/
│  ├─ test/
│  └─ production/
├─ Docs/
├─ Tools/
├─ .git/
├─ .gitignore
└─ AGENTS.md
```

### 3.1 각 영역의 역할

- `client`: Meta Horizon에 제출할 Unity Quest 애플리케이션이며 Unity 프로젝트 루트다.
- `server/src`: Unity·대시보드·문의 폼이 사용하는 Express HTTPS API다.
- `server/db`: 데이터베이스 구조 변경 기록을 보관한다.
- `server/public/dashboard`: 교육 관리자만 접근하는 웹 대시보드 정적 파일 영역이다. 실제 기록 조회는 관리자 인증을 거친 API만 사용한다.
- `server/public/site`: 회사 홈페이지·지원·문의 페이지를 Express와 함께 제공할 경우 사용하는 영역이다.
- `server/archive`: 공개하지 않지만 삭제하지 않고 보존해야 하는 이전 사이트와 작업 자료다.
- `shared/contracts`: Unity와 서버가 서로 다른 필드 이름을 사용하는 문제를 막는 공통 데이터 명세다.
- `infra`: 테스트 서버와 실서버의 배포 구성을 분리한다.
- 루트 `Tools`: Unity·서버·문서 제작을 함께 지원하는 저장소 공통 도구다. Unity 파일을 직접 참조하는 도구는 `client/Assets`를 명시한다.

공개 개인정보처리방침은 Meta에서 접근 가능한 URL을 안정적으로 유지해야 하므로 이 비공개 모노레포 안으로 합치지 않는다. 현재의 별도 공개 저장소 `tycheworks-safetytrainingvr-privacy`와 GitHub Pages 주소를 계속 사용한다.

Unity 앱은 데이터베이스에 직접 접속하지 않는다. Unity는 HTTPS API에 수행 기록을 전송하고, API가 검증 후 데이터베이스에 저장한다. 대시보드도 데이터베이스에 직접 접속하지 않고 관리자 인증을 거친 조회 API를 사용한다.

## 4. 저장소 재구성 작업 순서

현재 Unity 프로젝트에는 미완료·미커밋 작업이 있으므로 폴더 이동을 일반적인 파일 정리처럼 처리하지 않는다.

### 4.1 이동 전 보존

1. Unity Editor를 종료한다.
2. 현재 Git 변경 파일과 새 파일 목록을 기록한다.
3. 사용자 작업을 커밋하거나 복구 가능한 별도 백업으로 보존한다.
4. 현재 빌드 씬, 앱 ID, 버전과 Android 설정을 기록한다.
5. `Assets` 안의 모든 `.meta` 파일이 함께 이동되는지 확인한다.

### 4.2 이동 대상

- `Assets` → `client/Assets`
- `Packages` → `client/Packages`
- `ProjectSettings` → `client/ProjectSettings`
- 루트 `Tools`는 유지하고 Unity 직접 참조 경로만 `client/Assets`로 변경
- `tycheworks-site-final-v2-260824` → `server/public/site`
- 대시보드 HTML과 이후 프런트엔드 코드 → `server/public/dashboard`
- 새 Express 애플리케이션과 DB migration → `server`

### 4.3 이동하지 않거나 다시 생성할 항목

- `.git`과 루트 `.gitignore`는 저장소 루트에 유지한다.
- `Docs`와 프로젝트 전체 운영 문서는 루트에 유지한다.
- `Library`, `Temp`, `Logs`, `obj`, `.vs`와 생성된 `.csproj`, `.sln`은 기준 파일이 아니다. 새 Unity 경로에서 다시 생성한다.
- Keystore, 서버 비밀번호, API 키와 데이터베이스 접속정보는 Git에 커밋하지 않는다.

### 4.4 이동 후 검증

1. Unity Hub에 `client`를 프로젝트 경로로 다시 등록한다.
2. Unity `6000.4.8f1`로 프로젝트를 연다.
3. 패키지 복원과 스크립트 컴파일이 끝날 때까지 기다린다.
4. `client/ProjectSettings/EditorBuildSettings.asset`의 빌드 씬 순서를 확인한다.
5. 주요 씬과 프리팹의 Missing Script·Missing Reference를 확인한다.
6. 이동 전후 주요 `.meta` GUID가 변하지 않았는지 확인한다.
7. 정적 C# 빌드, Unity Play Mode와 Quest/OpenXR 실행을 각각 구분해 검증한다.
8. 루트 기준 경로를 사용하는 Editor 도구, PowerShell/Python 도구와 문서 링크를 새 경로에 맞게 수정한다.

Express 실행 기반·정적 파일 제공과 사용자 CRUD까지 구성했고, Unity 프로젝트도 새 `client` 경로에서 패키지 복원과 C# 컴파일을 완료했다. Meta 계정 조회의 1차 클라이언트 진단은 구현했지만 실제 Quest 계정 성공, 서버 Meta 증명 검증과 교육 기록 저장은 아직 연결하지 않았다.

### 4.5 2026-08-25 실제 진행 상태

완료된 파일 작업은 다음과 같다.

- `Assets` → `client/Assets` 이동
- `Packages` → `client/Packages` 이동
- `ProjectSettings` → `client/ProjectSettings` 이동
- 루트 `server` 폴더 생성
- `client/Packages`에 Meta XR Platform SDK `205.0.0` 등록
- `server` npm 초기화 및 Express `5.2.1` 설치
- `npm start`, `npm run dev`, `npm test` 스크립트 구성
- `GET /api/health` 구현과 실제 localhost 응답 검증
- 대시보드 → `server/public/dashboard/index.html` 이동
- 회사 사이트 → `server/public/site` 이동
- 중첩 사이트 복사본과 사이트 작업 문서 → `server/archive` 보존 이동
- Express에서 `/`, `/dashboard/` 정적 제공 연결
- `client` 생성 폴더용 `.gitignore`, Unity MCP, VS Code, README, AGENTS와 공통 도구 경로 갱신
- users/external identities migration과 사용자 CRUD 모듈 구현
- Meta 인증 전 운영 CRUD 비활성화 플래그 적용
- Unity Hub에서 `client`를 Unity `6000.4.8f1` 프로젝트로 열고 `client/Library` 및 Platform SDK package cache 재생성
- Meta Quest App ID 설정과 `OculusPlatformSettings.asset` 생성
- `MetaPlatformIdentityProbe` 구현 및 시작 씬 `0_App/AppMain` 연결
- SDK 초기화, entitlement, 앱 범위 사용자 ID와 연령대 조회 코드의 Unity C# 컴파일 확인

아직 완료로 판단하지 않는 항목은 다음과 같다.

- 프로젝트용 DB와 계정, Meta 사용자 증명 검증, 교육 기록 API는 아직 구현되지 않았다.
- 루트 `Tools`는 공통 도구 위치로 유지하고 Unity 직접 참조 스크립트는 `client/Assets` 경로로 갱신했다.
- 기존 루트 Unity 생성 폴더의 최종 정리가 남아 있다.
- 기존 루트 `Library/PackageCache`는 새 Unity 프로젝트의 영구 기준 경로가 아니며, 현재 기준은 `client/Library/PackageCache`다. 기존 루트 생성 폴더의 안전한 정리는 별도 작업으로 남긴다.
- Git 상태에서 `client`는 아직 새 경로로 추적되지 않은 상태다. 기존 변경 보존과 이동 diff 확인이 필요하다.
- 새 `client` 경로에서 패키지 복원과 C# 컴파일은 완료했다. 전체 Play Mode, Quest/OpenXR 첫 기동, 양안, 입력과 Meta 계정 실연동 검증은 아직 완료하지 않았다.

## 5. 공통 데이터 형식 확정

저장소 구조가 안정된 뒤 `shared/contracts`에서 Unity와 서버의 약속을 먼저 정한다.

최소 수행 기록은 다음 필드를 포함한다.

```text
eventId
participantId
sessionId
attemptId
courseId
mode
appVersion
eventName
checkpointId
outcome
errorCode
timestamp
elapsedFromAttempt
endReason
```

- `eventId`는 중복 전송을 제거하는 기준이다.
- `participantId`는 실명 대신 익명 ID를 사용한다.
- `attemptId`는 한 번의 교육 수행을 묶는다.
- `appVersion`은 업데이트 전후 결과를 비교하는 기준이다.
- `endReason`은 직접 종료와 추정 종료를 구분한다.

이 데이터 형식이 확정되어야 API, 데이터베이스와 Unity 기록 코드를 같은 기준으로 구현할 수 있다.

## 6. 로컬 서버와 테스트 서버

### 6.1 로컬 서버

개발 PC에서 먼저 다음 기능을 만든다.

- `POST /events/batch`: Unity 수행 기록 일괄 수집
- 중복 `eventId` 무시
- 잘못된 필드 거부
- 회차·단계·오류 기록 저장
- 대시보드용 완료율·중간 종료·단계별 집계 조회
- 홈페이지 문의 저장 또는 메일 전달
- 관리자 인증

#### 6.1.1 2026-08-25 초기화 완료 범위

- Node.js `v24.18.0`, npm `11.16.0` 환경 확인
- 서버 패키지명: `tyche-safety-training-server`
- Express `5.2.1` 설치 및 `package-lock.json` 생성
- ES module 방식의 `src/app.js`, `src/server.js` 구성
- 기본 포트 `3000`, `PORT` 환경변수 재정의 지원
- JSON 요청 본문 제한 `1mb`, `x-powered-by` 비활성화
- `GET /api/health` 응답: `{"status":"ok","service":"tyche-safety-training-server"}`
- `node --test` 기반 상태 API 테스트 1건 통과
- 실제 `npm start` 실행 후 `http://127.0.0.1:3000/api/health` HTTP 200 응답 확인
- 검증 후 테스트 서버 프로세스 종료

로컬 실행 명령은 다음과 같다.

```powershell
cd server
npm run dev
```

현재 단계는 Express 기반 서버가 실행되는지만 검증한 것이다. 데이터 CRUD 성공으로 해석하지 않는다.

#### 6.1.2 정적 사이트·대시보드 제공

- `server/public/site`를 Express `/` 경로에서 제공한다.
- `server/public/dashboard`를 Express `/dashboard/` 경로에서 제공한다.
- 자동 테스트에서 상태 API, 회사 사이트와 대시보드 3건이 모두 통과했다.
- 실제 PM2 개발 실행에서 `/api/health`, `/`, `/dashboard/`, `/styles.css`가 모두 HTTP 200을 반환했다.
- 검증 후 PM2 개발 프로세스를 종료했고 포트 `3000` listener가 남지 않았다.
- 현재 대시보드는 모의 데이터를 포함한 정적 목업이다. 실제 사용자 데이터 API와 관리자 인증은 아직 연결하지 않았다.

#### 6.1.3 PM2 단일 프로세스 관리와 데이터베이스 드라이버 확정

다음 서버 기술 구성을 채택하고 프로젝트 의존성으로 버전을 고정했다.

- 개발·운영 프로세스 관리: `PM2` `7.0.4`로 단일화
- MySQL 드라이버: `mysql2` `3.24.2`의 Promise API
- 환경변수 로딩: `dotenv` `17.4.2`
- Express: `5.2.1`
- 중복되는 개발 도구인 `nodemon`은 제거

구성한 실행 명령은 다음과 같다.

```text
npm run dev       # PM2 개발 실행, src watch, 터미널 전면 실행
npm start         # PM2 운영 실행
npm run prod      # npm start와 같은 PM2 운영 실행
npm run restart   # 환경변수를 반영한 재시작
npm run stop
npm run logs
npm run delete
```

- `ecosystem.dev.config.cjs`는 `src` 변경을 감시하고 `--no-daemon`으로 터미널에서 실행한다. `Ctrl+C`로 종료하면 개발 프로세스가 남지 않는다.
- `ecosystem.config.cjs`는 Vultr 운영용이며 watch를 끄고 백그라운드에서 프로세스를 유지한다.
- 두 설정 모두 프로세스명, 작업 경로, 환경, 포트, 자동 재시작과 `512M` 메모리 재시작 기준을 명시한다.
- `src/config/env.js`에서 HTTP와 MySQL 환경변수의 기본값·숫자 범위를 검증한다.
- `src/db/pool.js`에서 `mysql2/promise` 연결 풀을 제공한다.
- `.env.example`에는 `DB_HOST`, `DB_PORT`, `DB_USER`, `DB_PASSWORD`, `DB_NAME`, `DB_CONNECTION_LIMIT` 형식을 기록했다. 실제 `.env`는 Git에서 제외한다.
- PM2 개발 watch 실행에서 프로세스 `online`과 상태 API HTTP 200 응답을 확인했다.
- 개발 프로세스 종료 후 포트 `3000` listener와 PM2 개발 프로세스가 남지 않은 것을 확인했다.
- PM2 운영 실행에서도 프로세스 `online`, 재시작 0회, 상태 API HTTP 200 응답을 확인했다.
- 운영 검증 후 PM2 애플리케이션 등록과 로컬 PM2 데몬을 종료했다.
- 동일 명령은 `npx pm2 ...`로 직접 실행할 수 있지만 반복 작업은 `npm run` 스크립트를 기준으로 한다.
- npm 감사 결과 확인된 취약점은 0건이다.

현재 PC에는 Bitnami WAMP MariaDB `10.4.25` 서비스가 `127.0.0.1:3306`에서 실행 중이다. 그러나 이 프로젝트용 데이터베이스·계정·비밀번호는 확인되지 않았으므로 기존 DB를 임의로 변경하지 않고 연결 풀 생성까지만 검증했다. 실제 연결·migration·CRUD 성공은 확인하지 않았다. Vultr 배포 후에는 서버에서 `pm2 startup`과 `pm2 save`를 실행해 재부팅 자동 시작을 별도로 설정한다.

#### 6.1.4 사용자 CRUD 모듈

- `db/migrations/001_create_users.sql`: 내부 UUID 사용자와 soft-delete 상태
- `db/migrations/002_create_external_identities.sql`: Meta 앱 범위 사용자 ID와 연령대 연결
- `src/db/migrate.js`: 적용 migration을 `schema_migrations`에 기록하는 실행기
- `src/modules/users/user-repository.js`: `mysql2/promise` CRUD 저장소
- `src/modules/users/user-service.js`: 입력 검증, 내부 UUID와 익명 참여자 코드 생성
- `src/modules/users/user-routes.js`: REST API 라우트
- `src/middleware/error-handler.js`: 400, 404, 409와 일반 오류 응답

지원 API는 `POST /api/users`, 목록·단일 `GET`, `PATCH`, `DELETE`다. Meta 사용자 ID는 `external_identities`에 저장하고 일반 수정 대상에서 제외했다. 삭제는 사용자 행과 향후 훈련 외래키를 보존하는 soft delete다.

개발 기본값에서는 CRUD를 켜지만, PM2 `NODE_ENV=production`에서는 `ENABLE_UNAUTHENTICATED_USER_CRUD` 기본값이 false다. Meta 사용자 증명 검증이 구현되기 전에는 운영에서 이 값을 true로 바꾸지 않는다.

Node 자동 테스트 9건과 JavaScript 파일 13개의 구문 검사가 통과했고 npm 감사 취약점은 0건이다. PM2 실행과 상태 API도 확인했다. 로컬 MariaDB는 다른 WAMP 서비스이므로 자격정보 없이 사용하지 않았다. 실제 migration, SQL CRUD, 동시성, 외래키와 soft delete 결과는 아직 검증하지 않았다.

### 6.2 테스트 서버

로컬 기능이 검증된 다음 외부 HTTPS 주소를 가진 테스트 서버를 만든다.

- 실제 개인정보를 사용하지 않는 테스트 데이터만 저장한다.
- 실서버 데이터베이스와 완전히 분리한다.
- Unity Development Build는 테스트 서버 주소를 사용한다.
- 서버가 중단되어도 Unity 교육 과정은 계속 동작해야 한다.
- Quest가 오프라인이면 기기에 임시 저장하고 연결 복구 후 다시 보낸다.

테스트 서버 완료 조건은 Quest에서 한 회차를 수행한 뒤 서버와 데이터베이스에 기록이 도착하고, 대시보드에서 같은 결과가 표시되는 것이다.

## 7. Unity 실제 기록 연동

서버 계약이 확정된 뒤에만 Unity 상태 소유자에 기록 호출을 연결한다.

- 과정 시작·완료
- 모드 선택
- 단계 진입·완료
- 순서 오류·일반 오류
- 힌트·재시도
- 명시적 종료
- 앱 일시정지·백그라운드 전환
- heartbeat와 마지막 정상 기록
- 중단 후 재개

네트워크 전송은 프레임과 입력을 막지 않도록 비동기로 처리한다. 서버 주소와 공개 가능한 환경 식별자는 빌드 환경별 설정으로 관리하고, 서버 비밀키를 Unity 앱 안에 넣지 않는다.

## 8. 대시보드 실제 API 연동

1. 모의 데이터와 실제 API 데이터 소스를 분리한다.
2. 테스트 서버에서 필터, 개인 진도, 중간 종료와 업데이트 비교를 검증한다.
3. 관리자 로그인 없이 참여자 기록에 접근할 수 없도록 한다.
4. 참여자 ID는 실명으로 자동 변환하지 않는다.
5. 데이터가 없을 때 0을 실제 결과처럼 표시하지 않고 `기록 없음`으로 표시한다.

## 9. 실서버 전환

테스트 서버 전체 흐름이 통과한 뒤 실서버를 만든다.

- HTTPS 도메인
- 운영 데이터베이스
- 관리자 인증
- 비밀정보 관리
- 요청 제한과 입력 검증
- 로그·오류 알림
- 정기 백업과 복구 절차
- 개인정보 보관·삭제 기준
- 공개 개인정보처리방침과 지원 URL

회사 홈페이지 문의 폼, 개인정보처리방침과 지원 페이지도 이 단계에서 실제 주소로 동작해야 한다.

## 10. Meta Horizon 준비 순서

### 10.1 출시 식별자 확정

Meta 앱 항목을 만들기 전에 다음을 확정한다.

- 정식 앱 이름
- 정식 Android 앱 ID
- 회사·개발자 표시명
- 무료·유료 여부
- 지원 기기와 국가

현재 `Prototype_Tyche_Jinyoung`과 `com.softcastella.prototype.tyche.jinyoung`을 정식 출시에도 사용할지 먼저 결정한다. 첫 업로드 뒤에는 앱 ID와 서명키를 쉽게 바꿀 수 없으므로 임시 이름 상태로 제출하지 않는다.

### 10.2 Meta 개발자 앱 상태

Meta Developer Dashboard의 앱 항목은 생성됐고 `사용자 ID`, `사용자 연령대`에 대한 DUC를 제출했다. 현재 DUC는 검토 대기 상태다. Store 정식 심사는 아직 신청하지 않고 내부 테스트용 Release Channel을 먼저 사용한다.

### 10.3 출시 빌드 준비

- Android 최소·Target SDK를 현재 Meta 요구사항과 최종 APK Manifest에서 확인
- ARM64와 IL2CPP 확인
- 출시용 Keystore 생성과 별도 백업
- 사용하지 않는 Android 권한 제거
- 인터넷 통신 권한과 개인정보 고지 확인
- 앱 버전과 빌드 번호 증가 규칙 적용
- 실제 Quest에서 최소 72FPS와 발열·메모리 확인
- 현재 smooth locomotion을 반영한 편안함 등급 선택
- 첫 실행, 권한 요청, 중단·재실행과 전체 교육 흐름 확인

### 10.4 내부 채널 검증

1. 출시 서명 APK를 내부 Release Channel에 업로드한다.
2. 심사 계정과 유사한 새 테스트 계정으로 설치한다.
3. 홈페이지·개인정보처리방침·지원 URL을 확인한다.
4. 실서버 기록과 대시보드 갱신을 확인한다.
5. 네트워크가 없는 상태에서도 앱이 멈추지 않는지 확인한다.
6. Quest 기기에서 VRC 위반, 크래시와 프레임 문제를 수정한다.

### 10.5 Store 자료와 규정

- 앱 아이콘
- 실제 Quest 화면 스크린샷
- 대표 이미지와 필요 시 영상
- 짧은 설명과 상세 설명
- 카테고리·키워드
- 개인정보처리방침 URL
- 지원 URL 또는 이메일
- 연령 등급과 Age Group Self-Certification
- 데이터 처리 항목과 Data Use Checkup
- 실제 이동 방식에 맞는 편안함 등급

### 10.6 정식 심사

내부 채널과 실서버 검증이 끝난 뒤 Store 채널로 정식 심사를 신청한다. 수정 요청을 받으면 원인을 고치고 Android 빌드 번호를 증가시킨 새 APK로 다시 제출한다.

## 11. 전체 실행 순서

```text
1. 기존 작업 커밋·백업
2. 정식 앱 이름·Android 앱 ID 결정
3. client/server/shared/infra 모노레포 구조 생성
4. Unity·홈페이지·대시보드를 목표 위치로 이동
5. 새 Unity 경로에서 컴파일·Play Mode·Quest 검증
6. shared/contracts 데이터 형식 확정
7. server 로컬 Express API·DB 구현
8. 외부 테스트 서버 배포
9. Unity 이벤트 수집 연동
10. 대시보드 실제 API 연동
11. Quest→서버→DB→대시보드 전체 테스트
12. 실서버·홈페이지·개인정보처리방침 배포
13. 기존 Meta 앱 설정 확인과 내부 Release Channel 업로드
14. VRC·성능·권한·스토어 자료 검증
15. Meta Horizon Store 정식 심사 신청
```

## 12. 현재 바로 결정해야 하는 항목

- 정식 앱 이름
- 정식 Android 앱 ID
- 현재 저장소를 위 `client/server` 모노레포 구조로 실제 이동할 시점
- 테스트·실서버 호스팅 제공자와 데이터베이스
- 관리자 로그인 방식
- 수집할 개인정보 범위와 보관 기간
- 무료 또는 유료 출시 여부

## 13. 완료 판단 기준

### 저장소 구성 완료

- `client`, `server`, `shared`, `infra`의 책임이 분리됨
- 새 Unity 경로에서 기존 씬과 GUID가 보존됨
- Unity 컴파일·Play Mode·Quest/OpenXR 결과가 구분되어 기록됨

### 서버 완료

- 오프라인·재전송·중복 제거 포함 Quest 실제 기록이 저장됨
- 관리자만 대시보드에 접근할 수 있음
- 백업·로그·개인정보 삭제 절차가 있음

### Meta 제출 준비 완료

- 출시 서명 APK와 영구 보관한 Keystore가 있음
- 내부 Release Channel 설치와 전체 과정 검증이 완료됨
- 실제 Quest에서 VRC·성능·권한·편안함 등급이 확인됨
- 개인정보처리방침·지원 URL과 Store 자료가 실제 주소에서 열림
- 실서버 장애가 Unity 교육 자체를 중단시키지 않음

## 14. 참고 자료

- Meta 관리 Store 제출 흐름: https://github.com/meta-quest/agentic-tools/blob/main/skills/hz-store-submit/SKILL.md
- Meta 앱 요구사항·배포 안내: https://communityforums.atmeta.com/discussions/GettingStarted/understanding-app-requirements--distribution-on-the-meta-horizon-store/1344935
- Meta Quest 성능 기준: https://developers.meta.com/horizon/documentation/unreal/po-perf-opt-mobile/

공식 요구사항은 Meta Developer Dashboard와 제출 시점의 VRC를 최종 기준으로 다시 확인한다.

## 15. 2026-08-25 확정 — Meta 계정 식별, 컨트롤러 안내와 키보드 전환

### 15.1 사용자 식별 기준

- 키보드로 이름을 입력받지 않고 Meta Horizon 계정으로 사용자를 식별하는 방향을 채택한다.
- 클라이언트는 Meta XR Platform SDK의 초기화와 entitlement 확인 후 로그인 사용자를 조회한다.
- 서버의 외부 계정 키는 표시명이나 변경 가능한 사용자명 대신 앱 범위의 Meta 사용자 ID를 사용한다.
- Meta 사용자 ID 원문을 대시보드 표시명으로 사용하지 않는다. 서버 내부 사용자 레코드와 연결하고 필요한 경우 해시 또는 별도 내부 ID로 변환한다.
- Meta 계정 조회 실패, 네트워크 단절 또는 Editor 실행은 정상 계정으로 오인하지 않는다. 명시적인 로컬 테스트/게스트 사용자로 분리한다.
- USB 또는 Quest Link 재연결 여부로 사용자 모드를 초기화하지 않는다. 인증 결과와 서버 사용자 레코드가 세션 상태의 기준이다.

### 15.1.1 DUC 및 공개 개인정보처리방침

- Meta DUC 요청 기능은 `사용자 ID`와 `사용자 연령대`로 제한한다.
- Horizon 사용자 이름과 프로필 사진을 사용하지 않으므로 `사용자 프로필` 요청은 제거한다.
- 개인정보 책임자는 별도 법인이 없는 개인 개발자의 법적 실명으로 등록한다.
- Meta 사용자 데이터의 클라우드 처리업체는 `The Constant Company, LLC (Vultr)`로 등록한다.
- Vultr의 서비스 범주는 클라우드 저장·처리를 포함하는 게임 서비스 또는 IT 솔루션으로 등록한다.
- 실제 서버 리전이 대한민국이면 처리 국가는 `대한민국`으로 등록하고, 리전이 바뀌면 DUC와 개인정보처리방침을 함께 갱신한다.
- Unity 비공개 저장소를 공개하지 않는다. 공개 정책은 별도 `Tyche_Privacy_Policy` 저장소에서 GitHub Pages로 임시 게시한다.
- 공개 저장소는 `https://github.com/softCastella/tycheworks-safetytrainingvr-privacy`이며, 개인정보처리방침 URL은 `https://softcastella.github.io/tycheworks-safetytrainingvr-privacy/`이다.
- 2026-08-25 GitHub Pages에서 HTTPS 200 응답, 한국어·영문 정책, 개인정보 책임자 `Jinyoung Choi`와 문의 이메일 `tycheworks0101@gmail.com` 표시를 확인했다.
- 정책 페이지는 Meta 앱 범위 사용자 ID, 사용자 연령대, 교육 기록, Vultr 위탁 처리, 보유·삭제 기간과 이용자 권리를 한국어와 영어로 고지한다.
- 임시 GitHub Pages 주소를 Vultr 정식 도메인으로 이전할 때에는 새 URL이 실제로 열린 후 Meta 앱 제출 정보의 개인정보처리방침 URL을 변경하고, 이전 주소는 변경 확인 전까지 유지한다.
- 현재 정책 기준은 교육 기록 1년, 보안 로그 90일, 삭제 전 백업 최대 30일이며 서버 구현도 같은 기준을 따라야 한다.
- 머리·손·컨트롤러 원시 자세 데이터는 기기 내 XR 상호작용에만 사용하고 서버로 전송하지 않는다. 처리 범위를 확대하려면 사전 승인, 정책 수정과 DUC 갱신이 필요하다.

### 15.2 신규·기존 사용자와 컨트롤러 안내

- 사용자 판정의 1순위는 Meta Platform SDK가 반환한 앱 범위 Meta 사용자 ID다. 먼저 이 ID로 서버의 내부 사용자와 수행 기록을 조회하거나 신규 사용자 레코드를 생성한다.
- 사용자 판정의 2순위는 해당 계정에 연결된 시나리오 완료 기록이다. 서버가 가동 중이면 서버 기록을 조회하고, 서버 미구축·오프라인·응답 실패 상태에서는 같은 Meta ID에 연결된 로컬 완료 기록을 사용한다. 계정이 존재한다는 사실이나 앱 실행 횟수만으로 기존 사용자로 판정하지 않는다.
- 완료된 시나리오가 0개인 사용자는 신규 사용자다. `Welcome_New`와 최초 간단 컨트롤러 안내를 적용한다.
- 종류와 관계없이 시나리오를 1개 이상 완료한 사용자는 기존 사용자다. `Welcome_Old`를 적용하고 기본 컨트롤러 가이드 단계를 생략해 교육 진입 시간을 줄인다.
- `Welcome_New` 음원을 들었거나 앱을 한 번 실행했다는 사실은 시나리오 클리어가 아니며, 신규 사용자를 기존 사용자로 전환하지 않는다.
- 자동 생략 여부와 관계없이 사용자는 Simple 안내 중 오른손 A 버튼으로 상세 컨트롤러 교육에 다시 진입할 수 있다. 미니 컨트롤러 가이드 자체는 기존 조이스틱 클릭으로 토글한다.
- 미니 가이드에는 A 버튼과 `컨트롤러 교육` 안내를 표시하며, 새 A 시각물은 별도 클릭 입력을 받지 않는다.
- 완료 여부는 실제 시나리오 완료 이벤트로 생성된 서버 기록 또는 Meta ID별 로컬 기록을 기준으로 계산한다. 서버와 로컬 중 어느 한쪽에 검증된 완료 기록이 1개 이상 있으면 기존 사용자로 판정한다. Meta ID를 확인하지 못했고 현재 설치에서 그 계정에 대해 검증해 둔 식별 캐시도 없다면 기존 사용자라고 추정해 `Welcome_Old`를 재생하거나 필수 안내를 자동 생략하지 않는다.

#### 15.2.1 현재 로컬 Intro 판정의 한계와 서버 기준 교체

현재 구현은 앱 범위 Meta 사용자 ID별 `PlayerPrefs`에 `Welcome_New` 재생 완료 여부를 저장하고 다음 실행에서 `Welcome_Old`를 선택한다. 이 방식은 실제 시나리오 클리어를 기록하지 않으므로 위에서 확정한 신규·기존 사용자 기준을 충족하지 않는다. 서버가 아직 가동되지 않은 현재 단계에서는 Welcome 청취 이력을 로컬 시나리오 완료 기록으로 교체해야 한다.

1. 기존 Inspector와 씬 작성값을 보존하며, `Welcome_New`/`Welcome_Old` 선택 외의 UI·입력·텔레포트·PPE·거울 동작은 바꾸지 않는다.
2. 계정 식별의 단일 기준은 `MetaPlatformIdentityProbe`가 조회한 앱 범위 Meta 사용자 ID다.
3. 입력 이벤트, Interactor/Caster, Raycaster, Layer와 Collider 경로는 소비하거나 변경하지 않는다.
4. `PlayerPrefs`의 New 인사 재생 이력은 신규·기존 사용자, 시나리오 완료 또는 가이드 완료의 근거로 사용하지 않는다. 로컬 판정에는 실제 시나리오 완료 시점에 기록한 완료 이벤트만 사용한다.
5. 서버 미가동 단계의 선택 경로는 `Meta 앱 범위 사용자 ID 확인 -> 해당 ID의 로컬 완료 기록 조회 -> 0개면 Welcome_New, 1개 이상이면 Welcome_Old`다.
6. 서버 가동 이후의 선택 경로는 `Meta 앱 범위 사용자 ID 확인 -> 서버 사용자 조회/upsert -> 서버 완료 기록과 미동기화 로컬 완료 기록 병합 -> 0개면 Welcome_New, 1개 이상이면 Welcome_Old`다.
7. 로컬 완료 기록은 Meta ID 해시, 시나리오 ID, 고유 attempt/event ID와 완료 시각을 포함한 오프라인 큐로 보관한다. 서버 연결 후 중복되지 않게 전송하고 성공 응답을 받은 항목만 큐에서 제거한다.
8. 서버가 응답하지 않아도 같은 Meta ID의 로컬 시나리오 완료 기록이 있으면 Old를 적용한다. Meta ID도 확인하지 못하고 연결할 로컬 계정 기록도 없으면 New로 실패 안전 처리한다. 필수 음원 참조는 런타임 자동 검색으로 수리하지 않는다.
9. 영향 소비자는 `0_App`의 Meta 식별 수명, 로컬 완료 기록 저장소, 서버 사용자·수행 기록 API와 PPE 씬의 Welcome Voice다. Simple/상세 컨트롤러 흐름, 가이드 작성값과 A 버튼 재진입은 보존한다.
10. 로컬 판정과 서버 동기화 구현 후 정적 직렬화·컴파일·Editor 하네스를 확인하고, 실제 계정·완료 기록·온라인/오프라인 조합은 Quest에서 검증한다.

서버 미가동 상태의 로컬 기록은 현재 설치 데이터에만 유효하므로 앱 삭제, 데이터 초기화 또는 다른 Quest에서는 완료 이력을 복구할 수 없다. 서버 동기화가 적용된 뒤에는 재설치나 기기 변경 여부와 관계없이 같은 Meta 앱 범위 사용자 ID의 완료 시나리오 기록을 조회하므로, 1개 이상 완료한 계정은 Old로 판정해야 한다.

### 15.3 키보드 Canvas 처리

- `Modal  Keyboard Canvas` 전체를 재생성할 수 있는 프로젝트 프리팹과 자체 포함 `.unitypackage`를 보관했다.
- 재사용 패키지는 시각 요소와 키보드 기능을 포함하지만 현재 PPE 씬 전용 `PPEVoiceKeyboardEventRelay`, `PPEControllerEducationEntry`, `PPE Voice Flow` 참조는 포함하지 않는다.
- 현재 씬에는 `PPEVoiceFlowDirector.m_KeyboardPresentationRoot -> Modal  Keyboard Canvas` 외부 참조가 남아 있으므로 바로 삭제하지 않는다.
- 기업/Meta 계정 로그인으로 전환할 때 `FlowState.NameInput`을 생략 또는 교체하고, `m_KeyboardPresentationRoot` 필수 참조와 이름 제출 릴레이를 함께 제거한 뒤 Canvas를 삭제한다.
- 미니 컨트롤러 가이드 진입 UI는 키보드 Canvas 삭제 전에 유지 위치를 별도로 정한다.

#### 15.3.1 임시 키보드 우회와 미니 가이드 A 안내

1. 이번 변경은 키보드 에셋·Canvas·이름 제출 코드를 삭제하지 않고 PPE 흐름의 `NameInput` 표시 단계만 직렬화 옵션으로 임시 우회한다.
2. 상태 소유자는 계속 `PPEVoiceFlowDirector`이며, Welcome 뒤에는 기존 Simple 컨트롤러 안내로 진입한다.
3. 입력 경로는 `오른손 primaryButton/buttonSouth -> PPEVoiceFlowDirector -> 상세 컨트롤러 안내`를 유지한다. 새 미니 A 시각물에는 별도 Ray 클릭 소비자를 추가하지 않는다.
4. 키보드 참조가 남아 있어도 Play Mode가 이를 다시 활성화하지 않도록 모든 `ApplyPresentation`·상태 전이·A 상세교육 복귀 경로를 함께 확인한다.
5. 상세교육이 끝나면 비활성 키보드로 돌아가지 않고 `CardIntro`로 진행한다. 누락 UI를 런타임에서 자동 생성하지 않는다.
6. 영향 소비자는 Welcome 이후 상태 전이, Simple 중 A 재진입과 `ControllerGuide_mini`의 작성 UI다. 기존 컨트롤러 이미지·조이스틱 토글, 카드·텔레포트·PPE·거울은 보존한다.
7. 변경 전 기준은 `Welcome -> NameInput`, A 상세교육 종료 뒤 `NameInput` 복귀다. 변경 후에는 `Welcome -> Simple`, Simple 중 A -> 상세교육 -> `CardIntro`와 키보드 비활성 상태를 정적·Editor·Play Mode로 구분해 검증한다.

적용 결과 `3_PPE_Room_3mode_loco.unity`의 `m_SkipKeyboardNameInput`을 활성화했고, `ControllerGuide_mini/Context/Controller Education Hint`를 씬 작성 UI로 추가했다. 이 안내는 기존 A 아이콘 스타일과 `컨트롤러 교육` 문구를 사용하되 `Graphic.raycastTarget`과 `Selectable`을 두지 않는 표시 전용 요소다. 런타임 코드에는 UI 위치·크기·색상 값을 추가하지 않았다.

정적 C# 빌드와 `PPELocomotionPpeRegressionValidationHarness.Validate()`는 통과했다. Editor 직렬화 확인에서 씬은 저장 완료 상태였고, 새 안내의 텍스트는 `A | 컨트롤러 교육`, Raycast 대상과 Selectable은 각각 0개였다. Play Mode 상태 전이와 Quest/OpenXR에서의 실제 배치·가독성은 아직 수동 확인이 필요하다.

### 15.4 SDK 설치 범위와 순서

계정 식별만 먼저 구현하므로 Meta XR All-in-One SDK 대신 Meta XR Platform SDK만 사용한다. 패키지 등록은 이미 완료됐다.

```text
UPM package: com.meta.xr.sdk.platform
```

설치 순서는 다음과 같다.

1. 현재 미커밋 작업과 폴더 이동을 커밋하거나 복구 가능한 기준점으로 보존한다.
2. Unity Hub에서 `client`를 Unity `6000.4.8f1` 프로젝트로 연다.
3. `com.meta.xr.sdk.platform` `205.0.0` 복원과 스크립트 컴파일이 끝날 때까지 Play Mode와 Quest Link 재시작을 피한다.
4. 기존 XRI/OpenXR 입력, 손 추적, 양안 렌더링과 Quest Link 기준 실행에 회귀가 없는지 확인한다.
5. Meta Developer Dashboard의 앱 ID를 Unity에 설정한다.
6. `Core.AsyncInitialize(appId)`를 실행한다.
7. entitlement 성공 후 `Users.GetLoggedInUser()` 결과를 받는다.
8. 성공·인증 실패·네트워크 실패·Editor 로컬 테스트를 서로 다른 상태로 검증한다.

SDK 설치만으로 키보드 Canvas를 삭제하거나 컨트롤러 가이드를 자동 생략하지 않는다. 계정 조회 어댑터, 서버 사용자 CRUD와 완료 횟수 API가 연결된 뒤 상태 전이를 변경한다.

### 15.5 현재 판단

- SDK 설치 상태: Meta XR Platform SDK `205.0.0` 패키지 등록 완료
- 적용 범위: Meta XR Platform SDK, Quest App ID와 1차 사용자 식별 진단 코드까지 적용됨
- 모노레포 이동 상태: Unity `client`, Express `server`, 홈페이지·대시보드 정적 경로와 사용자 CRUD 구성이 완료됐고 Git 이동 diff 정리는 남아 있음
- 현재 차단 조건: SDK 설치를 막는 DUC 조건은 없다. 실제 연령대 응답은 DUC 승인 상태에 따라 `Unknown` 또는 권한 오류가 될 수 있음
- 다음 안전 단계: 현재 변경 보존 → Android Development Build → 내부 Release Channel 설치 → Quest 첫 기동 SDK·HMD·청록색 플래시 회귀 검증 → 서버 nonce 검증 연결

### 15.6 1차 SDK 사용자 식별 진단 구현 기준

이번 변경이 대응하는 요청은 Meta Platform SDK 초기화, entitlement 확인, 앱 범위 사용자 ID 조회, 사용자 연령대 조회와 Console 진단까지다. 서버 전송·DB 저장·Meta nonce 검증·신규/기존 사용자 판정·키보드 제거·컨트롤러 가이드 생략은 이번 범위에 포함하지 않는다.

변경 전 필수 판단은 다음과 같다.

1. 기존 Inspector/씬 작성값을 보존한다. `0_App.unity`의 기존 `AppSceneBootstrap`, UI와 입력 설정은 덮어쓰지 않고, Unity Editor를 통한 명시적 작업으로 진단 컴포넌트만 추가한다.
2. 1차 진단 상태의 단일 소유자는 명시적으로 씬 오브젝트에 추가하는 `MetaPlatformIdentityProbe` 컴포넌트다.
3. 이번 변경은 컨트롤러·Interactor·Raycaster·Collider 입력 경로를 사용하거나 변경하지 않는다. 실행 진입은 컴포넌트의 `Start` 또는 Inspector Context Menu로 한정한다.
4. App ID 누락, 초기화 실패, entitlement 실패와 사용자 ID 누락은 자동 수리하거나 게스트 성공으로 바꾸지 않고 단계가 표시된 단일 오류로 종료한다. 연령대 권한이 아직 승인되지 않은 경우에는 사용자 ID 성공을 보존하고 연령대만 `Unknown`으로 완료한다.
5. UI·텔레포트·PPE Grab·거울·XR 양안 소비자는 변경하지 않는다. Meta SDK 콜백과 진단 로그만 영향을 받는다.
6. 변경 전 기준은 SDK v205 패키지 복원, `OculusPlatformSettings.asset`의 Quest App ID 저장과 Unity Console 컴파일 오류 없음이다. 변경 후에는 Unity 스크립트 컴파일과 컴포넌트 수동 추가 가능 여부를 확인한다.
7. 이번 자동 검증 범위는 정적 API 확인과 Unity Editor 컴파일까지다. Meta 계정, entitlement, 연령대와 앱 범위 ID의 실제 성공은 Release Channel 권한이 부여된 Quest에서 별도로 확인해야 하며, Quest/OpenXR 성공으로 보고하지 않는다.

`MetaPlatformIdentityProbe`는 기본적으로 앱 범위 사용자 ID를 Console에 표시하되 진단용 설정으로 분리한다. 운영 빌드 전에 원문 ID 로그를 끄거나 진단 컴포넌트를 제거해야 한다. 컴포넌트는 entitlement 실패 시 앱을 종료하지 않으며 기존 씬 진행을 차단하지 않는다.

적용한 변경은 다음과 같다.

- `Assets/Scripts/MetaPlatformIdentityProbe.cs`를 추가해 `Core.AsyncInitialize()` → `Entitlements.IsUserEntitledToApplication()` → `Users.GetLoggedInUser()` → `UserAgeCategory.Get()` 순서를 구현했다.
- 단계별 상태를 `ProbeState`로 보관하고, 중복 실행을 차단하며, 각 성공·실패 지점을 `[Meta Identity Probe]` Console 로그로 구분한다.
- 사용자 ID가 확인된 뒤 연령대만 실패하면 전체 사용자를 인증 성공으로 확정하지 않되 `CompletedWithoutAgeCategory`로 분리해 DUC 검토 상태를 진단할 수 있게 했다.
- Unity Editor API로 `Assets/Scenes/0_App.unity`의 기존 `AppMain` 오브젝트에 컴포넌트를 한 번만 추가했다. Unity가 생성한 FileID와 `.meta` GUID를 사용했으며 기존 `AppSceneBootstrap` 직렬화 값은 변경하지 않았다.

완료한 검증은 다음과 같다.

- Meta XR Platform SDK v205에 실제 존재하는 초기화, entitlement, 사용자 ID와 연령대 API 시그니처를 패키지 소스에서 확인했다.
- Unity `AssetDatabase.Refresh` 뒤 `Assembly-CSharp`가 새 스크립트를 포함해 재컴파일됐고 새 C# 컴파일 오류가 없음을 확인했다.
- 현재 열려 있던 `3_PPE_Room_3mode_loco.unity`는 dirty 상태가 아니었고, `0_App.unity`만 additive로 열어 수정·저장한 뒤 닫아 사용자의 열린 씬을 유지했다.
- Console의 기존 오류 1건은 이번 변경 전 초기 임포트에서 확인된 `construction_helmet_3d_model_Clone1.prefab`의 누락된 Variant 부모이며 이번 SDK 코드의 컴파일 오류가 아니다.

아직 필요한 수동 검증은 Release Channel 접근 권한이 있는 Meta 계정으로 설치한 Quest 빌드에서 초기화, entitlement, 동일 계정의 동일 앱 범위 ID와 `Tn`/`Ad` 연령대 결과를 확인하는 것이다. 현재 단계에서는 Meta 계정 성공, Quest/OpenXR 실행 또는 서버 저장이 검증됐다고 판단하지 않는다.

### 15.7 현재 진행 상황 통합 기록

#### 적용 완료

- Meta Quest App ID를 Unity Platform 설정에 저장했다. 숫자형 App ID는 클라이언트 설정에 포함할 수 있지만 App Secret은 서버 환경 변수로만 관리한다.
- `MetaPlatformIdentityProbe`가 앱 시작 씬에서 비동기로 Platform SDK 초기화, entitlement, 앱 범위 사용자 ID와 연령대를 순서대로 조회한다.
- 진단 실패가 기존 타이틀·인트로·PPE 진행을 강제로 종료하지 않으며 서버·DB·가이드 생략은 아직 호출하지 않는다.
- Express users/external identities CRUD와 MySQL migration runner는 구현돼 있다. 인증되지 않은 CRUD는 production 기본값에서 비활성이다.

#### 첫 기동 회귀 패치 상태

- `Docs/Bug/2026-08-25_PPE_First_Play_HMD_Simulator_Race.md`의 HMD/Simulator 경쟁 패치는 2026-08-25 적용됐다.
- `PhysicalHmdSimulatorGate`는 실제 HMD 등록보다 먼저 `XRSimulatedHMD`를 활성화하지 않도록 최대 3초 동안 실제 `XRHMD` 또는 실행 중인 `XRDisplaySubsystem`을 확인한다.
- 대상 PPE 씬의 작성값은 `Enable Simulator When No Physical HMD=true`, `Physical HMD Detection Timeout=3`, `Game View Test Mode=false`다.
- 정적 및 Editor 격리 검증은 완료했지만 실제 Quest 첫 실행에서 Simulator 비활성·정상 머리 추적을 다시 확인해야 한다.

#### 하늘색 첫 프레임 상태

- `Docs/Bug/2026-07-30_Loading_Scene_Cyan_First_Frame_Flash.md`의 전환 화면 방지 패치는 유지되고 있다.
- `0_App`, `2_Intro`, `6_LoadingScene_0` 카메라는 불투명 검정, `1_Title` 카메라는 불투명 흰색 Solid Color Clear로 확인했다.
- 로딩 Canvas는 작성 알파 `0`, 비상호작용 상태이고 `LoadingSceneController`가 2프레임 예열한 뒤 표시한다.
- 이 설정은 APK에도 포함되지만 XR compositor의 실제 첫 제출 프레임은 Quest의 완전 종료 후 cold launch에서만 확정할 수 있다.
- 태블릿 또는 PPE 씬의 실제 3D Renderer가 하늘색으로 번쩍이는 별도 현상은 `Docs/Bug/2026-07-25_PPE_Tablet_SFX_BGM_BlueFlash.md`에 미해결로 남아 있다. Emission 비활성화만으로 해결되지 않았으며, APK에서도 재현될 가능성을 배제하지 않는다.

#### 완료한 검증

- Unity `6000.4.8f1`에서 Platform SDK v205 복원 및 `Assembly-CSharp` 재컴파일 성공
- 시작 씬에 `MetaPlatformIdentityProbe`가 정확히 1개 존재하고 씬 저장 후 dirty가 아님을 확인
- 검증을 위해 씬을 additive로 열고 닫은 뒤 사용자가 열어 둔 `3_PPE_Room_3mode_loco.unity`가 유지됨을 확인
- 네 전환 씬의 불투명 카메라 Clear와 로딩 Canvas 2프레임 예열 상태 확인
- 기존 Console 오류 1건은 초기 임포트 중 확인된 `construction_helmet_3d_model_Clone1.prefab`의 누락된 Variant 부모이며 SDK 컴파일 오류와 구분함

#### 아직 필요한 수동 검증과 다음 순서

1. Android Development Build와 서명 설정을 확인한다.
2. 내부 Release Channel에 업로드하고 앱 접근 권한이 있는 Meta 테스트 계정으로 설치한다.
3. Quest에서 앱을 완전히 종료한 뒤 첫 기동과 두 번째 기동을 비교한다.
4. `[Meta Identity Probe]` 로그에서 초기화, entitlement, 같은 계정의 같은 앱 범위 ID와 연령대 결과를 확인한다.
5. 첫 기동에서 `XRSimulatedHMD`가 활성화되지 않고 화면·머리 추적·컨트롤러 입력이 정상인지 확인한다.
6. 타이틀·인트로·로딩·PPE 진입을 녹화해 화면 전체/로딩 UI의 청록색 플래시와 태블릿·3D Renderer 플래시를 구분한다.
7. 실기기 진단이 통과한 뒤 `Users.GetUserProof()` nonce를 서버가 Meta에 검증하는 인증 API와 DB upsert를 연결한다.
8. 완료 수행 횟수 API가 검증된 뒤에만 기존 사용자 컨트롤러 가이드 생략 기능을 활성화한다. A 버튼 미니 가이드는 유지한다.

현재 완료 범위는 코드·씬 설정과 Unity Editor 정적 검증까지다. APK, Release Channel, 실제 Quest 계정, HMD 양안 및 서버 저장은 아직 성공으로 보고하지 않는다.

#### 문서 정책 하네스 해결 기록

- 진행 내용은 중복 문서를 만들지 않고 이 기준 회의록에 통합했다.
- `DocumentationPolicyHarness.Validate()` 실행 결과, 문서 내용이나 분류가 아니라 모노레포 이동 전 프로젝트 루트 가정 때문에 실패했다.
- 하네스는 `Application.dataPath/..`인 `client`를 저장소 루트로 보고 `client/AGENTS.md`를 요구하지만, 실제 기준 `AGENTS.md`와 `Docs`는 모노레포 루트에 있다.
- 수정 전 판단은 다음과 같다.
  1. 기존 `AGENTS.md`와 문서 파일·분류·내용은 변경하지 않고 하네스의 경로 탐색만 수정한다.
  2. 저장소 루트의 단일 기준은 Unity 프로젝트 루트에서 상위로 탐색한 가장 가까운 `.git` 또는 `Docs`와 `AGENTS.md`가 함께 있는 디렉터리다.
  3. 입력·Interactor·Raycaster·Collider 경로는 사용하지 않으며 Editor 메뉴 실행만 영향을 받는다.
  4. 저장소 루트를 찾지 못하면 임의 경로를 만들거나 복사하지 않고 기존처럼 명확한 검증 오류로 멈춘다.
  5. UI·텔레포트·PPE Grab·거울·XR 양안 소비자는 영향받지 않는다. 문서 검사 경로만 영향받는다.
  6. 변경 전 기준은 `client/AGENTS.md` 누락 오류이며, 변경 후 기준은 루트 `AGENTS.md`, 루트 `Docs`, `client/Assets/Docs`를 검사하고 통과하는 것이다.
  7. 정적 코드 확인, Unity C# 컴파일과 `DocumentationPolicyHarness.Validate()` 실제 실행 결과를 구분해 기록한다. Quest/OpenXR 검증 대상은 아니다.

근본 원인은 하네스가 `Application.dataPath/..`를 저장소 루트로 고정해 모노레포의 `client`만 보던 것과, 필수 정책 문구를 이동 전 경로인 ``Assets/Docs``에 정확히 일치시키던 것이다.

적용한 변경은 다음과 같다.

- Unity 프로젝트 루트에서 상위 디렉터리를 탐색해 `AGENTS.md`와 `.git` 또는 `Docs`가 함께 있는 저장소 루트를 선택한다.
- 문서 분류 검사는 저장소 루트 `Docs`, 현재 Unity 프로젝트의 `Assets/Docs`, 호환용 저장소 루트 `Assets/Docs` 후보를 중복 없이 검사한다.
- 필수 정책 문구를 현재 기준인 ``Docs``와 ``client/Assets/Docs``에 맞췄다.
- 저장소 루트를 찾지 못할 때 파일을 복사하거나 생성하는 fallback은 추가하지 않았다.

완료한 검증은 다음과 같다.

- Unity에서 `DocumentationPolicyHarness.cs` 재임포트와 Editor C# 컴파일이 성공했다.
- 같은 Unity 세션에서 `DocumentationPolicyHarness.Validate()`를 다시 실행해 오류 없이 통과했다.
- 루트 `AGENTS.md`, 루트 `Docs`와 `client/Assets/Docs`가 현재 모노레포 기준 검사 대상에 포함된다.

이번 변경은 Editor 문서 검증 경로에만 영향을 주며 런타임, 씬, UI, 입력, Meta SDK와 Quest/OpenXR에는 영향을 주지 않는다. 저장소 구조가 다시 변경될 때에는 하네스를 재실행해 상위 루트 탐색 결과를 다시 확인해야 한다.

## 15.8 2026-08-24~25 실기 테스트 통합 체크리스트

이 절은 어제와 오늘 적용한 변경 중 정적 검사와 Unity Editor 자동 검증만으로 완료할 수 없는 항목을 실제 조작 순서로 통합한 것이다. 세부 근거는 `Docs/PPE_HandTest_Performance_Review.md`, `Docs/Bug/2026-08-24_PPE_Room_Environment_Collision.md`, `Docs/Bug/2026-08-25_PPE_First_Play_HMD_Simulator_Race.md`, `Docs/Bug/2026-07-30_PPE_Room_Card_Ray_Selection.md`, `Docs/Bug/2026-08-22_PPE_CommitCandidate_Followup_QA.md`와 `Docs/PPE_Room_Voice_Narration_Flow_Design.md`를 따른다.

테스트 기록에는 실행 방식(`Quest Android 빌드`, `Quest Link/OpenXR`, `PC Game View`), 첫 실행 여부, 사용한 Meta 계정, 왼쪽·오른쪽 눈 결과, 실제 출력 장치, 표시 주사율·앱 FPS와 실패 시점을 함께 남긴다. 서로 다른 실행 방식을 한 결과로 합쳐 판정하지 않는다.

### 15.8.1 어제(2026-08-24) 적용분

- [ ] **Quest 72Hz:** Android Quest 실행 로그에서 `[Meta Quest Refresh Rate] Requested 72 Hz`를 확인하고, 헤드셋 성능 표시에서 실제 디스플레이가 72Hz인지 확인한다. 앱 FPS와 CPU/GPU frame time이 13.89ms 예산을 넘는 구간도 기록한다.
- [ ] **전체 씬 전환:** `0_App → 1_Title → 2_Intro → 6_LoadingScene_0 → 3_PPE_Room_3mode_loco` 순서로 이동하는지 확인한다. 양안에서 로딩 UI 누락·청록색 첫 프레임·3D Renderer 플래시를 서로 구분해 기록한다.
- [ ] **환경 충돌:** 각 텔레포트 지점 도착 후 스틱 이동으로 벽, 캐비닛, 락커, 벤치, 카트와 진열 랙을 통과하지 못하는지 확인한다. 같은 위치에서 PPE Grab, 카드·모달 UI Ray와 텔레포트 Ray는 계속 동작해야 한다.
- [ ] **사용자 키와 자세:** 앉기·서기 및 가능한 서로 다른 사용자 키에서 Capsule이 HMD 높이·중심을 따라 자연스럽게 바닥과 벽에 충돌하는지 확인한다. 실제 몸만 벽 너머로 기울이는 room-scale 침범은 이번 완료 조건에 포함하지 않는다.
- [ ] **발소리 입력:** 왼손과 오른손 스틱을 각각 시작·유지·해제해 발소리가 시작되고 약 0.4초 간격으로 반복된 뒤 즉시 멈추는지 확인한다. 텔레포트와 HMD의 물리 이동만으로는 발소리가 나지 않아야 한다.
- [ ] **정상 PPE 착용 SFX:** 정상 마스크·고글·페이스실드, 안전모, 좌·우 니트릴 내부 장갑을 몸 근접 Trigger로 착용한다. 각 전용 SFX의 음질·음량과 `Correct Answer` 동시 재생이 서로 과도하게 겹치지 않는지 확인한다.
- [ ] **불량 PPE와 SCBA:** 불량 마스크는 전용 착용음 없이 `Wrong Answer`만 재생되고 장착되지 않아야 한다. SCBA를 Grab했을 때 패널 표시명이 `SCBA`인지 양안에서 확인한다.
- [ ] **몸 내부 접촉 착용:** PPE 메시가 몸 안쪽에 닿았지만 컨트롤러/손 중심은 기존 0.25m 반경 밖인 자세에서 Trigger를 누른다. 정상 PPE는 착용되고 오염·작업계획 불일치 PPE는 계속 거절돼야 한다.
- [ ] **누출 안전대와 교육 시작 음성:** 누출 작업계획에서 안전대를 사용하면 교육은 `Wrong Answer + EDU 205`, 훈련은 `Wrong Answer + TRAIN 003`, 테스트는 `Wrong Answer`만 재생하는지 확인한다. PPE 구역 진입에서는 EDU 002 뒤 EDU 003이 끊김 없이 이어져야 한다.
- [ ] **Quest 출력 장치:** Play Mode에서 기본 출력 대체 경고가 있었으므로 Quest Link 연결 상태에서 Voice·SFX·발소리가 실제 HMD 스피커 또는 선택한 출력 장치로 나오는지 확인한다.

### 15.8.2 오늘(2026-08-25) 적용분

- [ ] **첫 Play HMD 판정:** Quest를 먼저 연결한 뒤 Unity의 첫 Play를 실행한다. Simulator UI와 `XRSimulatedHMD`가 켜지지 않고 머리 포즈·양손 컨트롤러·스틱 이동·손 표시가 즉시 정상인지 확인한다. Play를 완전히 종료한 뒤 두 번째 Play와 비교한다.
- [ ] **신규 사용자 Intro:** Meta Platform 초기화·entitlement·앱 범위 사용자 ID 조회는 성공했지만 서버와 로컬 모두 완료된 시나리오가 0개인 계정에서 `VO_PPE_INTRO_001_Welcome_New`가 재생되는지 확인한다. New 음원을 끝까지 듣고 앱을 다시 실행해도 시나리오를 완료하지 않았다면 계속 New여야 한다. 원문 ID는 테스트 기록에 노출하지 않는다.
- [ ] **기존 사용자 Intro:** 같은 Meta 계정으로 시나리오를 1개 이상 완료하고 로컬 완료 기록이 생성된 것을 확인한 뒤, 서버가 없는 상태의 다음 실행에서도 `VO_PPE_INTRO_002_Welcome_Old`가 재생되는지 확인한다. 서버 연결 뒤에는 해당 로컬 기록이 중복 없이 동기화되고, 앱 데이터 삭제·재설치 또는 다른 Quest에서도 서버 완료 기록이 확인되면 Old여야 한다.
- [ ] **키보드 우회:** Welcome 뒤 공간 키보드가 나타나지 않고 Simple 컨트롤러 안내로 바로 진행하는지 확인한다. 키보드의 투명 Graphic이 PPE 클릭이나 UI 입력을 가로채지 않아야 한다.
- [ ] **미니 A 안내:** 미니 가이드에 `A` 아이콘과 `컨트롤러 교육` 문구가 잘림 없이 보이는지 양안에서 확인한다. 이 시각물을 Ray로 가리키거나 클릭해도 별도 UI 입력이 발생하지 않아야 한다.
- [ ] **A 상세교육 재진입:** Simple 안내 중 오른손 실제 A 버튼을 한 번 눌러 상세교육으로 진입한다. 중복 진입이 없어야 하며 상세교육 완료 뒤 키보드가 아니라 `CardIntro`로 진행해야 한다.
- [ ] **Simple 음성과 이미지:** Simple 001→002→교체된 003→004→005를 순서대로 듣고, Trigger는 `1_Ray`, Grip은 `2_Marker`, Joystick은 `3_Exit_Marker`와 해당 컨트롤러 이미지를 표시하는지 확인한다. 삭제된 `Panel`, `3_Ray_T`, `Ray_T_B/R`은 다시 보이면 안 된다.
- [ ] **상세 Trigger 단계:** 001→002 종료 뒤 Trigger를 기다리는지 확인한다. Grip 또는 Joystick 입력은 003을 재생하고 다시 Trigger를 기다려야 하며, Trigger 정답은 공통 008 뒤 Grip 단계로 이동해야 한다.
- [ ] **상세 Grip 단계:** 004 종료 뒤 Grip을 기다리는지 확인한다. Trigger 또는 Joystick 입력은 005 뒤 다시 Grip을 기다리고, Grip 정답은 008 뒤 Joystick 단계로 이동해야 한다.
- [ ] **상세 Joystick 단계:** 006 종료 뒤 Joystick 축 입력을 기다리는지 확인한다. Trigger 또는 Grip 입력은 007 뒤 다시 Joystick을 기다리고, 정답은 008→009 뒤 상세교육을 종료해야 한다. 설명·오답·정답 음성 재생 중 연속 입력은 중복 판정되면 안 된다.
- [ ] **컨트롤러 가이드 양안:** Trigger·Grip·Joystick 입력 대기 중 각 단계 이미지와 컨트롤러 이미지가 양쪽 눈에 누락·잘림 없이 보이고, 작성된 자식 활성 상태와 Sprite가 Play 중 바뀌지 않는지 확인한다.
- [ ] **PPE 도착 위치:** `Teleport_0/PPE_1 Arrival Anchor`로 이동했을 때 진열장과 너무 가깝지 않고 PPE 선택·이동을 시작할 여유가 있는지 확인한다. 사용자가 조정한 위치를 테스트 중 임의 저장하지 않는다.
- [ ] **교육 001·002·003·010:** 교육 모드 선택에서 교체된 EDU 001, PPE 구역 진입에서 EDU 002→003이 재생되는지 확인한다. EDU 010은 현재 재생되지 않는 것이 정상이다.
- [ ] **방호복 3종 Grab 005:** `_Clean`, `_Contam`, `_Ripped`를 새 세션 또는 상태 초기화 후 각각 Grab해 `4_VO_PPE_EDU_005_UsePPE`가 들리는지 확인한다.
- [ ] **정상 방호복 006:** `_Clean`을 몸 안쪽에 가져가 Trigger로 착용한 직후 EDU 006이 재생되고 장착 모델이 본체와 거울에 정상 표시되는지 확인한다.
- [ ] **중복 방호복 SuitAlready:** 방호복을 입은 상태에서 다른 방호복을 몸 쪽에서 사용한다. 하자 PPE 음성보다 `4_VO_PPE_EDU_204_SuitAlready`가 먼저 재생되고 두 번째 방호복은 장착되지 않아야 한다.
- [ ] **Game View 가상 손:** PC Game View에서는 정상 방호복 marker 첫 클릭 뒤 선택이 유지되고 마우스 포인터를 따라오는지 확인한다. 몸 부착 범위 밖의 두 번째 클릭은 선택을 유지하고, 몸 안쪽의 두 번째 클릭은 기존 착용 판정을 실행해야 한다.
- [ ] **Game View 보조 입력:** 이동 허용 상태에서 `↑/↓` 전후 이동, `←/→` 좌우 회전, 벽 충돌, `STICK CLICK` 미니 가이드 토글을 확인한다. PPE·태블릿·UI 클릭은 기존 상호작용을 우선하고 벽·바닥·빈 공간 클릭만 현재 음성을 건너뛰어야 한다.

### 15.8.3 권장 실행 순서와 합격 기준

1. Quest 성능 표시와 로그 수집을 켠 뒤 앱 완전 종료 상태에서 첫 실행한다.
2. 첫 Play/HMD 판정, 72Hz, New Intro, 로딩 전환과 청록색 플래시를 먼저 기록한다.
3. Simple·A 상세교육·가이드 양안·PPE 도착 위치를 확인한다.
4. 교육 모드에서 EDU 001~003, 방호복 005·006·SuitAlready와 정상·불량 PPE SFX를 확인한다.
5. 훈련·테스트 모드의 누출 안전대 피드백을 각각 분리해 확인한다.
6. 텔레포트별 환경 충돌, 발소리와 기존 Ray 입력 회귀를 확인한다.
7. 앱을 재시작해 Old Intro와 두 번째 Play HMD 상태를 확인한다.
8. PC Game View 전용 가상 손·화살키·배경 클릭은 Quest 결과와 분리해 마지막에 확인한다.

합격은 위 항목의 기대 결과와 일치하고 Unity Console의 새 Error/Exception/Assert가 0건이며 Quest 양안·실제 출력 장치에서 같은 결과가 확인된 상태다. 실패 시에는 체크를 지우지 말고 실행 방식, 단계, 사용 장비, 들린 Clip 이름, 왼쪽·오른쪽 눈 차이, FPS/frame time과 Console 첫 오류를 함께 기록한다.

## 15.9 에셋 경로·RPG FPS 복구 폴더·Quest MSAA 정리

### 15.9.1 긴 에셋 경로 진단과 송풍기 폴더명 단축

Git이 추적하는 파일만 대상으로 경로 길이를 조사했다. 생성 폴더인 `Library`, `Temp`, `Logs`, `UserSettings`는 조사 대상에서 제외했다. 저장소에는 절대경로 260자 이상인 추적 파일은 없었지만, 다음 송풍기 텍스처와 `.meta`가 각각 251자와 256자로 확인돼 Unity 임포트, 압축 해제, 빌드 도구 또는 더 긴 위치의 클론에서 문제가 될 여유가 거의 없었다.

`client/Assets/TripoModels/yellow_industrial_blower_3d_model_Clone1_Clone1`

근본 원인은 동일한 긴 이름이 외부 폴더, FBX, `.fbm` 폴더와 텍스처에 반복된 구조다. FBX 바이너리에는 `.fbm`과 텍스처의 상대경로가 들어 있으므로 내부 FBX·Material·Texture 이름은 바꾸지 않았다. Unity `AssetDatabase.MoveAsset`으로 외부 폴더만 다음과 같이 이동했다.

`yellow_industrial_blower_3d_model_Clone1_Clone1` → `yellow_industrial_blower`

적용 후 저장소의 최장 절대경로는 256자에서 233자로 줄었다. 폴더와 하위 에셋의 `.meta` GUID를 유지했고 이동된 추적 파일 9개의 내용이 변경 전 Git blob과 동일함을 확인했다. `Assets/Scenes/5_MixerRoom_Unlit_scale.unity`의 FBX 의존성, 원본 Material과 Mixer Room Unlit Material의 BaseColor Texture 의존성, Unity의 GUID→신규 경로 해석과 에셋 로드를 확인했다. 과거 감사 문서에 남은 이전 경로 문자열은 실행 의존성이 아니므로 역사 기록으로 유지했다.

이 변경은 외부 폴더 경로만 줄이며 FBX 내부 상대경로, 메시, 재질, 텍스처, 씬 오브젝트와 Transform에는 영향을 주지 않는다. Unity Editor 의존성 검증은 완료했지만 해당 송풍기가 표시되는 Mixer Room을 실제 Play Mode와 Quest 양안에서 보는 수동 검증은 아직 필요하다.

### 15.9.2 `RPG_FPS_Unused_Recovery_20260804` 판정

루트의 `RPG_FPS_Unused_Recovery_20260804`는 696개 파일, 약 445.82MB의 복구 묶음이다. 2026-08-05 커밋에서 `Assets/RPG_FPS_game_assets_industrial`의 에셋 695개를 Unity `Assets` 밖으로 이동하면서 만들어졌고, 당시 문서에는 대량 삭제·재임포트와 누락된 `Hangar_v2_6 Variant` 부모 참조가 기록돼 있다. 사용자가 당시 폴더를 제거한 뒤 혼합기동 건물 또는 바닥이 선홍색으로 보였다고 확인했으므로, `Unused`라는 폴더명만으로 전체를 삭제 가능한 상태로 판단하지 않는다.

현재 `5_MixerRoom_Unlit_scale.unity`가 실제로 참조하는 RPG FPS 사용본은 다음 `client/Assets` 경로에 존재한다.

- `Assets/RPG_FPS_game_assets_industrial/Buildings/Industrial/Hangars/Hangar_v2/Source/Hangar_v2_6.FBX`
- `Assets/RPG_FPS_game_assets_industrial/Buildings/Industrial/Hangars/Hangar_v2/Source/Hangar_v2.mat`
- `Assets/RPG_FPS_game_assets_industrial/Buildings/Industrial/Hangars/Hangars_interior/Source/Hangars_interior_v1.mat`
- `Assets/RPG_FPS_game_assets_industrial/Other_props/Support_set/Support_set_v1/Source/Support_set_v1.mat`
- `Assets/UIs/Facilities/mixer_Romm_A/mixer_room_A.fbx`

복구 폴더의 `Extracted_Prefabs/Hangar_v2_6.FBX`는 현재 사용본과 GUID `d38c6eb5eac83874ca4e1361ff5ff63c`가 중복된다. 따라서 복구 폴더 전체를 `client/Assets/UIs/Facilities/mixer_Romm_A` 또는 다른 `Assets` 하위로 옮기면 Unity가 446MB에 가까운 복구 묶음을 다시 임포트하고 중복 GUID 충돌을 일으킬 수 있다. 복구 폴더는 현재 위치에서 보존하며 전체 이동·삭제하지 않는다.

현재 Mixer Room Preview Scene의 Renderer 357개를 조사한 결과 누락 Material, 누락 Shader와 미지원 Shader는 각각 0개였다. 위 Hangar·Interior·Support Material은 `Universal Render Pipeline/Simple Lit`을 사용하고 `shader.isSupported=true`이며 각 TGA Base Texture가 로드되는 것을 Unity에서 확인했다. 이는 정적·Editor 검증이며, 과거 선홍색 회귀가 실제 Quest 빌드에서 재발하지 않는지는 Mixer Room 양안 확인이 필요하다.

### 15.9.3 Android/Quest MSAA 4x 적용

변경 전 Android 기본 품질은 `Mobile`이지만 `QualitySettings.antiAliasing=0`, `Mobile_RPAsset.m_MSAA=1`이어서 Quest 빌드에서는 MSAA가 사실상 꺼져 있었다. 반면 PC 품질과 `PC_RPAsset`은 MSAA 4x였다. Mixer Room XR 카메라는 `allowMSAA=true`, URP 후처리 Anti-aliasing은 `None`이었다.

Unity Editor API로 다음 두 값만 변경했다.

- `ProjectSettings/QualitySettings.asset`: `Mobile`의 `antiAliasing`을 `0`에서 `4`로 변경
- `Assets/Settings/Mobile_RPAsset.asset`: `m_MSAA`를 `1`에서 `4`로 변경

에디터 현재 품질은 기존 `PC(1)`로 복원해 저장했으며 PC MSAA 4x, Mobile Render Scale `0.8`, 카메라와 씬 작성값은 변경하지 않았다. Unity 실행 결과 Mobile 품질과 `Mobile_RPAsset`이 모두 4x로 읽히는 것을 확인했고, 최종 YAML diff도 위 두 값만 변경된 것을 확인했다.

PPE룸 바닥 자체는 사용자가 정상이라고 확인했으므로 `PPE_Room_Floor.png`, `PPE_Room_Floor.mat`, 바닥 밝기와 Texture Import 설정은 이번 변경 범위에서 제외했다. Automatic Viewport Dynamic Resolution, Foveated Rendering과 Application SpaceWarp는 Android OpenXR 설정에서 비활성이다. `Mobile_RPAsset`의 Adaptive Performance 허용은 켜져 있지만 실제 런타임 해상도 변경이 발생하는지는 아직 확정하지 않았다.

### 15.9.4 남은 Quest 수동 검증

1. Quest Android 빌드에서 양안의 PPE룸 직선·UI 외곽선·가는 균열 선을 같은 위치와 머리 움직임으로 비교한다.
2. MSAA 4x 적용 뒤 표시 주사율 72Hz와 앱 FPS 72가 유지되는지 확인하고 CPU/GPU frame time이 13.89ms 예산을 넘는 구간을 기록한다.
3. FPS가 낮아지면서 화면 전체가 아지랑이처럼 일렁이면 단순 앨리어싱과 재투영을 분리한다.
4. 특정 선만 떨리면 1픽셀보다 얇은 `LineRenderer`·월드 스페이스 UI 외곽선의 실제 폭과 동일 깊이 면의 z-fighting을 먼저 확인한다.
5. Mobile Render Scale은 GPU 부하를 고려해 `0.8`로 유지한다. 성능 여유가 확인될 때만 단일 변수로 `0.9`를 비교하며, `1.0`은 `0.8`보다 렌더 픽셀 수가 약 56% 증가하므로 첫 대응으로 적용하지 않는다.
6. SMAA/FXAA는 월드 스페이스 UI와 투명 경계에 도움이 될 수 있지만 글자 흐림과 추가 비용이 있으므로 마지막 비교 항목으로 둔다.

현재 완료 범위는 외부 송풍기 폴더 이동과 의존성 검증, RPG FPS 사용 경로·복구 중복 GUID 조사, Android/Quest MSAA 4x 저장 및 정적·Unity Editor 검증까지다. Quest 양안 화질, 72fps 유지, 재투영과 실제 선홍색 미재현은 완료로 보고하지 않는다.

## 15.10 2026-08-26 브랜드 홈페이지 대안 시안·이미지·상호작용 정리

### 15.10.1 이번 변경이 대응하는 요청과 보존 범위

이번 작업은 기존 회사 홈페이지의 브랜드 페이지를 검토하고 별도 디자인 시안을 만든 뒤, 사용자 피드백에 따라 홈과의 톤, 이미지 화질, 풋터, 라인 페이지 네비게이션과 IMMERSA 대표 프로젝트 CTA를 조정한 기록이다.

- 기존 `server/public/site/brand/index.html`은 비교 기준으로 보존했다.
- 새 시안은 `server/public/site/brand-v2/index.html`과 `brand-v2.css`에 분리했다.
- Unity 클라이언트, PPE 상태, 서버 API와 데이터베이스 동작은 변경하지 않았다.
- VR 상세 페이지의 `metahorizon_title_2560x1440.png` 히어로는 사용자 요청에 따라 교체 대상에서 제외했다.
- 기존 저해상도 이미지도 삭제하거나 덮어쓰지 않고 보존했다.

### 15.10.2 새 브랜드 페이지

별도 `/brand-v2/` 경로에 브랜드 구조와 세 라인을 설명하는 대안 페이지를 추가했다. 스킵 링크, 키보드 포커스, 모바일 `details` 메뉴, `aria-current`, 모션 감소 설정을 포함하고 IMMERSA·SPARK·LOOP의 운영 상태를 `ACTIVE`와 `COMING SOON`으로 구분했다.

초기 시안에는 브랜드 설명의 실제 사례로 화학물질 안전훈련 VR 섹션을 넣었으나, 이는 브랜드 페이지 범위를 넘어선 구성이라는 사용자 피드백에 따라 완전히 제거했다. 히어로의 `대표 작업 보기`도 `설계 기준 보기`로 바꾸고 브랜드 페이지 내부 `#principles`로 연결했다.

홈과 톤이 달랐던 근본 원인은 새 시안이 독립된 베이지 배경과 어두운 히어로·CTA를 사용한 것이다. 다음처럼 홈의 시각 기준에 맞췄다.

- 전체를 흰색 배경, 얇은 회색 선과 연한 그리드, 오렌지 포인트 중심으로 변경
- 어두운 히어로와 어두운 하단 CTA를 밝은 카드형 구성으로 변경
- 타이포그래피와 브랜드 시스템·라인 패널 구조는 유지
- 원칙 카드 최소 높이를 `270px`에서 `220px`, 번호와 제목 사이를 `64px`에서 `34px`로 축소
- 모바일 원칙 카드의 고정 최소 높이를 제거하고 제목 위 간격을 `28px`로 축소
- 풋터 배경을 다른 페이지와 동일한 `#f8f9fb`, `34px × 34px` 연회색 그리드 패턴으로 통일

### 15.10.3 홈 캐러셀 2·3번 배너 잘림

홈 HTML에는 화학물질 안전훈련 VR, SPARK, LOOP 배너 세 개가 모두 존재했지만 `.release-track` 자체의 `overflow:hidden` 때문에 첫 슬라이드 밖에 배치된 두 번째와 세 번째 슬라이드가 잘렸다. 이동 트랙의 클리핑만 제거하고 외부 `.release-carousel`과 각 슬라이드의 둥근 모서리 클리핑은 유지했다.

세 배너 존재와 이동 트랙의 자체 클리핑 금지를 `server/test/site-carousel.test.js`에서 검사한다. 세부 원인과 수동 확인 항목은 `Docs/Bug/2026-08-26_Brand_Home_Carousel_Track_Clipping.md`에 분리해 기록했다.

### 15.10.4 고해상도 웹 이미지

기존 사진은 다음처럼 큰 카드와 고해상도 화면에 사용하기에는 원본 크기가 작았다.

| 기존 파일 | 기존 크기 |
|---|---:|
| `game.jpg` | `388 × 244` |
| `app.jpg` | `398 × 244` |
| `hazmat.jpg` | `662 × 466` |
| `industry.jpg` | `758 × 334` |
| `vr-hero.jpg` | `684 × 660` |

기존 이미지를 구도 참고로 사용해 OpenAI 이미지 생성 도구로 새 고해상도 이미지를 만들었다. 외부 웹사이트나 스톡 이미지에서 내려받은 파일이 아니며, 단순 업스케일이 아니라 같은 역할과 구도를 기반으로 새로 생성한 AI 이미지다. 웹 전송량을 줄이기 위해 JPEG 품질 92로 저장했다.

| 추가 파일 | 크기 | 약 파일 크기 |
|---|---:|---:|
| `game-hd.jpg` | `1536 × 1024` | `84KB` |
| `app-hd.jpg` | `1536 × 1024` | `97KB` |
| `hazmat-hd.jpg` | `1536 × 1024` | `283KB` |
| `industry-hd.jpg` | `1672 × 941` | `297KB` |
| `vr-hero-hd.jpg` | `1122 × 1402` | `163KB` |

홈 캐러셀은 `hazmat-hd.jpg`, `game-hd.jpg`, `app-hd.jpg`를 사용한다. IMMERSA 개요 페이지는 `vr-hero-hd.jpg`, `hazmat-hd.jpg`, `industry-hd.jpg`를 사용한다. 새 브랜드 시안의 화학물질 프로젝트 섹션은 삭제됐으므로 현재 `brand-v2`에서는 `hazmat-hd.jpg`를 소비하지 않는다.

라인 로고의 `IMMERSA`, `SPARK`, `LOOP` 문자는 CSS 텍스트가 아니라 각 PNG 안에 래스터화돼 있다. 저장소에는 SVG·AI·PSD 원본이나 사용 폰트 메타데이터가 없어 정확한 서체를 확정할 수 없다. 시각적으로는 `Montserrat SemiBold` 계열과 가깝지만, `IMMERSA`의 I, `SPARK`의 A와 `LOOP`의 무한대 기호는 커스텀 심볼이다. 일반 웹 본문은 `Inter`, `Pretendard`, `Noto Sans KR` 순서의 폰트 스택을 사용한다.

### 15.10.5 IMMERSA CTA 애니메이션

IMMERSA 개요 페이지의 화학물질 안전훈련 VR `자세히 보기 →` 버튼만 `featured-cta`로 지정했다. 다른 `.blue-btn`에는 애니메이션을 적용하지 않았다.

현재 작성값은 다음과 같다.

- 전체 주기: `0.78초`
- 최고 높이: `14px`
- 정점 유지: 키프레임 `36%~50%`, 약 `0.11초`
- 정점 그림자: `0 22px 40px rgba(32,135,255,.09)`로 멀고 옅게 표시
- 하강: `56% -13px → 70% -9px → 82% -4px → 90% -1px`
- 착지: `94%`에서 `scaleX(1.038) scaleY(.968)`
- 착지 그림자: `0 7px 18px rgba(32,135,255,.27)`
- hover와 keyboard focus에서는 애니메이션을 멈추고 버튼을 약간 확대
- `prefers-reduced-motion: reduce`에서는 애니메이션을 사용하지 않음

이 값은 사용자의 반복 비교에 따라 정점에서는 가볍게 잠시 머물고, 하강과 착지는 정점보다 무게가 있으나 과도하게 찍히지 않는 중간값으로 조정한 상태다.

### 15.10.6 SPARK·LOOP 네비게이션 hover

SPARK와 LOOP 준비중 페이지의 공통 `.line-nav a:hover`가 `#111`로 고정돼 라인 색으로 바뀌지 않았다. IMMERSA의 키컬러 hover 방식과 동일하게 다음 규칙을 적용했다.

- SPARK hover·focus: `#ff5f00`
- LOOP hover·focus: `#f4c430`
- 색상 전환 시간: `0.2초`

마우스 hover뿐 아니라 키보드 `focus-visible`도 같은 라인 키컬러를 사용한다.

### 15.10.7 완료한 검증

- `brand-v2` HTML 태그 열기·닫기 균형과 중복 ID 없음 확인
- `brand-v2` 내부 fragment 링크가 실제 ID를 가리키는지 확인
- 제거한 프로젝트 섹션의 `featured`, `project-status`, `hazmat` 참조가 `brand-v2`에 남지 않았는지 확인
- 새 이미지의 실제 픽셀 크기와 파일 크기 확인
- 로컬 Express에서 `/brand-v2/`, CSS, 이미지와 연결 페이지가 HTTP `200`을 반환함을 확인
- `git diff --check` 통과
- `npm test`: 신규 캐러셀 회귀 검사를 포함한 10개 테스트 통과

### 15.10.8 아직 필요한 수동 검증과 남은 결정

1. 데스크톱 브라우저에서 홈 캐러셀 `01 / 03 → 02 / 03 → 03 / 03` 버튼 전환, 5초 자동 전환과 마지막에서 첫 슬라이드로 이어지는 동작을 확인한다.
2. `/brand-v2/`의 데스크톱·태블릿·모바일 레이아웃, 원칙 카드 간격과 모든 페이지 풋터 배경이 실제로 같은지 비교한다.
3. IMMERSA CTA의 `0.78초`, `14px`, 정점 대기와 착지 변형이 최종 의도와 맞는지 실제 브라우저에서 확인한다.
4. SPARK·LOOP 네비게이션 hover와 keyboard focus 색을 확인한다. LOOP 노란색은 흰 배경에서 대비가 낮을 수 있으므로 가독성 문제가 있으면 키컬러를 유지한 더 어두운 파생색을 별도 승인 후 사용한다.
5. 생성 이미지의 인물·PPE·산업 설비와 기기 표현을 최종 브랜드 자산으로 승인할지 결정한다. 승인 전 기존 저해상도 파일은 삭제하지 않는다.
6. 기존 `/brand/`와 새 `/brand-v2/` 중 실제 `BRAND` 네비게이션의 최종 목적지를 결정한다. 현재는 기존 페이지를 자동 교체하지 않았다.
7. 홈페이지 문의 폼의 `/api/contact`는 현재 서버 라우트가 없어 POST 시 `404`가 발생한다. 문의 저장 또는 메일 전달 정책을 확정한 뒤 별도 구현·검증해야 한다.

정적 파일과 자동 테스트 결과를 실제 브라우저의 시각적 승인으로 확대 해석하지 않는다. 현재 완료 범위는 별도 시안과 자산·CSS 적용, 정적 검사와 서버 테스트까지이며 최종 라우팅 교체와 실브라우저 반응형 승인은 남아 있다.
