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

- Unity 프로젝트 기준 경로는 저장소 루트에서 `client`로 이동했다. 현재 기준 파일은 `Assets`, `Packages`, `ProjectSettings`에 있다.
- Android 앱 ID는 `com.softcastella.prototype.tyche.jinyoung`이다.
- `productName`은 `Prototype_Tyche_Jinyoung`이다.
- 앱 버전은 `0.1.0`, Android 빌드 번호는 `1`이다.
- Android ARM64와 IL2CPP는 설정되어 있다.
- Android 최소 SDK는 현재 `25`, Target SDK는 Unity 자동 선택값이다.
- 출시용 Android Keystore와 Key Alias는 아직 설정되지 않았다.
- Meta Quest용 OpenXR 기능과 Oculus Touch Controller Profile은 활성화되어 있다.
- Meta XR Platform SDK(`com.meta.xr.sdk.platform`) `205.0.0`이 `Packages/manifest.json`과 `Packages/packages-lock.json`에 등록되어 있다.
- Meta 개발자 앱은 생성됐고 DUC는 제출·검토 대기 상태다. Quest App ID는 `Assets/Resources/OculusPlatformSettings.asset`에 설정되어 있다. App Secret은 Unity나 Git에 넣지 않는다.
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
- 루트 `Tools`: Unity·서버·문서 제작을 함께 지원하는 저장소 공통 도구다. Unity 파일을 직접 참조하는 도구는 `Assets`를 명시한다.

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

- `Assets` → `Assets`
- `Packages` → `Packages`
- `ProjectSettings` → `ProjectSettings`
- 루트 `Tools`는 유지하고 Unity 직접 참조 경로만 `Assets`로 변경
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
4. `ProjectSettings/EditorBuildSettings.asset`의 빌드 씬 순서를 확인한다.
5. 주요 씬과 프리팹의 Missing Script·Missing Reference를 확인한다.
6. 이동 전후 주요 `.meta` GUID가 변하지 않았는지 확인한다.
7. 정적 C# 빌드, Unity Play Mode와 Quest/OpenXR 실행을 각각 구분해 검증한다.
8. 루트 기준 경로를 사용하는 Editor 도구, PowerShell/Python 도구와 문서 링크를 새 경로에 맞게 수정한다.

Express 실행 기반·정적 파일 제공과 사용자 CRUD까지 구성했고, Unity 프로젝트도 새 `client` 경로에서 패키지 복원과 C# 컴파일을 완료했다. Meta 계정 조회의 1차 클라이언트 진단은 구현했지만 실제 Quest 계정 성공, 서버 Meta 증명 검증과 교육 기록 저장은 아직 연결하지 않았다.

### 4.5 2026-08-25 실제 진행 상태

완료된 파일 작업은 다음과 같다.

- `Assets` → `Assets` 이동
- `Packages` → `Packages` 이동
- `ProjectSettings` → `ProjectSettings` 이동
- 루트 `server` 폴더 생성
- `Packages`에 Meta XR Platform SDK `205.0.0` 등록
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
- Unity Hub에서 `client`를 Unity `6000.4.8f1` 프로젝트로 열고 `Library` 및 Platform SDK package cache 재생성
- Meta Quest App ID 설정과 `OculusPlatformSettings.asset` 생성
- `MetaPlatformIdentityProbe` 구현 및 시작 씬 `0_App/AppMain` 연결
- SDK 초기화, entitlement, 앱 범위 사용자 ID와 연령대 조회 코드의 Unity C# 컴파일 확인

아직 완료로 판단하지 않는 항목은 다음과 같다.

- 프로젝트용 DB와 계정, Meta 사용자 증명 검증, 교육 기록 API는 아직 구현되지 않았다.
- 루트 `Tools`는 공통 도구 위치로 유지하고 Unity 직접 참조 스크립트는 `Assets` 경로로 갱신했다.
- 기존 루트 Unity 생성 폴더의 최종 정리가 남아 있다.
- 기존 루트 `Library/PackageCache`는 새 Unity 프로젝트의 영구 기준 경로가 아니며, 현재 기준은 `Library/PackageCache`다. 기존 루트 생성 폴더의 안전한 정리는 별도 작업으로 남긴다.
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
  6. 변경 전 기준은 `client/AGENTS.md` 누락 오류이며, 변경 후 기준은 루트 `AGENTS.md`, 루트 `Docs`, `Assets/Docs`를 검사하고 통과하는 것이다.
  7. 정적 코드 확인, Unity C# 컴파일과 `DocumentationPolicyHarness.Validate()` 실제 실행 결과를 구분해 기록한다. Quest/OpenXR 검증 대상은 아니다.

근본 원인은 하네스가 `Application.dataPath/..`를 저장소 루트로 고정해 모노레포의 `client`만 보던 것과, 필수 정책 문구를 이동 전 경로인 ``Assets/Docs``에 정확히 일치시키던 것이다.

적용한 변경은 다음과 같다.

- Unity 프로젝트 루트에서 상위 디렉터리를 탐색해 `AGENTS.md`와 `.git` 또는 `Docs`가 함께 있는 저장소 루트를 선택한다.
- 문서 분류 검사는 저장소 루트 `Docs`, 현재 Unity 프로젝트의 `Assets/Docs`, 호환용 저장소 루트 `Assets/Docs` 후보를 중복 없이 검사한다.
- 필수 정책 문구를 현재 기준인 ``Docs``와 ``Assets/Docs``에 맞췄다.
- 저장소 루트를 찾지 못할 때 파일을 복사하거나 생성하는 fallback은 추가하지 않았다.

완료한 검증은 다음과 같다.

- Unity에서 `DocumentationPolicyHarness.cs` 재임포트와 Editor C# 컴파일이 성공했다.
- 같은 Unity 세션에서 `DocumentationPolicyHarness.Validate()`를 다시 실행해 오류 없이 통과했다.
- 루트 `AGENTS.md`, 루트 `Docs`와 `Assets/Docs`가 현재 모노레포 기준 검사 대상에 포함된다.

이번 변경은 Editor 문서 검증 경로에만 영향을 주며 런타임, 씬, UI, 입력, Meta SDK와 Quest/OpenXR에는 영향을 주지 않는다. 저장소 구조가 다시 변경될 때에는 하네스를 재실행해 상위 루트 탐색 결과를 다시 확인해야 한다.

## 15.8 2026-08-24~25 실기 테스트 통합 체크리스트

이 절은 어제와 오늘 적용한 변경 중 정적 검사와 Unity Editor 자동 검증만으로 완료할 수 없는 항목을 실제 조작 순서로 통합한 것이다. 세부 근거는 `Docs/PPE_HandTest_Performance_Review.md`, `Docs/Bug/2026-08-24_PPE_Room_Environment_Collision.md`, `Docs/Bug/2026-08-25_PPE_First_Play_HMD_Simulator_Race.md`, `Docs/Bug/2026-07-30_PPE_Room_Card_Ray_Selection.md`, `Docs/Bug/2026-08-22_PPE_CommitCandidate_Followup_QA.md`와 `Docs/PPE_Room_Voice_Narration_Flow_Design.md`를 따른다.

테스트 기록에는 실행 방식(`Quest Android 빌드`, `Quest Link/OpenXR`, `PC Game View`), 첫 실행 여부, 사용한 Meta 계정, 왼쪽·오른쪽 눈 결과, 실제 출력 장치, 표시 주사율·앱 FPS와 실패 시점을 함께 남긴다. 서로 다른 실행 방식을 한 결과로 합쳐 판정하지 않는다.

### 15.8.1 어제(2026-08-24) 적용분

- [ ] **Quest 72Hz:** Android Quest 실행 로그에서 `[Meta Quest Refresh Rate] Requested 72 Hz`를 확인하고, 헤드셋 성능 표시에서 실제 디스플레이가 72Hz인지 확인한다. 앱 FPS와 CPU/GPU frame time이 13.89ms 예산을 넘는 구간도 기록한다.
- [ ] **전체 씬 전환:** `0_App → 1_Title → 2_Intro → 3_Loading → 4_PPE_Room` 순서로 이동하는지 확인한다. 양안에서 로딩 UI 누락·청록색 첫 프레임·3D Renderer 플래시를 서로 구분해 기록한다.
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

`Assets/TripoModels/yellow_industrial_blower_3d_model_Clone1_Clone1`

근본 원인은 동일한 긴 이름이 외부 폴더, FBX, `.fbm` 폴더와 텍스처에 반복된 구조다. FBX 바이너리에는 `.fbm`과 텍스처의 상대경로가 들어 있으므로 내부 FBX·Material·Texture 이름은 바꾸지 않았다. Unity `AssetDatabase.MoveAsset`으로 외부 폴더만 다음과 같이 이동했다.

`yellow_industrial_blower_3d_model_Clone1_Clone1` → `yellow_industrial_blower`

적용 후 저장소의 최장 절대경로는 256자에서 233자로 줄었다. 폴더와 하위 에셋의 `.meta` GUID를 유지했고 이동된 추적 파일 9개의 내용이 변경 전 Git blob과 동일함을 확인했다. `Assets/Scenes/5_MixerRoom_Unlit_scale.unity`의 FBX 의존성, 원본 Material과 Mixer Room Unlit Material의 BaseColor Texture 의존성, Unity의 GUID→신규 경로 해석과 에셋 로드를 확인했다. 과거 감사 문서에 남은 이전 경로 문자열은 실행 의존성이 아니므로 역사 기록으로 유지했다.

이 변경은 외부 폴더 경로만 줄이며 FBX 내부 상대경로, 메시, 재질, 텍스처, 씬 오브젝트와 Transform에는 영향을 주지 않는다. Unity Editor 의존성 검증은 완료했지만 해당 송풍기가 표시되는 Mixer Room을 실제 Play Mode와 Quest 양안에서 보는 수동 검증은 아직 필요하다.

### 15.9.2 `RPG_FPS_Unused_Recovery_20260804` 판정

루트의 `RPG_FPS_Unused_Recovery_20260804`는 696개 파일, 약 445.82MB의 복구 묶음이다. 2026-08-05 커밋에서 `Assets/RPG_FPS_game_assets_industrial`의 에셋 695개를 Unity `Assets` 밖으로 이동하면서 만들어졌고, 당시 문서에는 대량 삭제·재임포트와 누락된 `Hangar_v2_6 Variant` 부모 참조가 기록돼 있다. 사용자가 당시 폴더를 제거한 뒤 혼합기동 건물 또는 바닥이 선홍색으로 보였다고 확인했으므로, `Unused`라는 폴더명만으로 전체를 삭제 가능한 상태로 판단하지 않는다.

현재 `5_MixerRoom_Unlit_scale.unity`가 실제로 참조하는 RPG FPS 사용본은 다음 `Assets` 경로에 존재한다.

- `Assets/RPG_FPS_game_assets_industrial/Buildings/Industrial/Hangars/Hangar_v2/Source/Hangar_v2_6.FBX`
- `Assets/RPG_FPS_game_assets_industrial/Buildings/Industrial/Hangars/Hangar_v2/Source/Hangar_v2.mat`
- `Assets/RPG_FPS_game_assets_industrial/Buildings/Industrial/Hangars/Hangars_interior/Source/Hangars_interior_v1.mat`
- `Assets/RPG_FPS_game_assets_industrial/Other_props/Support_set/Support_set_v1/Source/Support_set_v1.mat`
- `Assets/UIs/Facilities/mixer_Romm_A/mixer_room_A.fbx`

복구 폴더의 `Extracted_Prefabs/Hangar_v2_6.FBX`는 현재 사용본과 GUID `d38c6eb5eac83874ca4e1361ff5ff63c`가 중복된다. 따라서 복구 폴더 전체를 `Assets/UIs/Facilities/mixer_Romm_A` 또는 다른 `Assets` 하위로 옮기면 Unity가 446MB에 가까운 복구 묶음을 다시 임포트하고 중복 GUID 충돌을 일으킬 수 있다. 복구 폴더는 현재 위치에서 보존하며 전체 이동·삭제하지 않는다.

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

## 15.11 2026-08-27 Windows에서 Vultr 서버 Codex 바로 접속

### 15.11.1 목적과 적용 범위

Windows PC에서 Vultr의 Chemical Safety VR 서버 작업 공간으로 빠르게 접속할 수 있도록 PowerShell 명령과 바탕화면 바로가기를 구성했다. 이 설정은 로컬 실행 편의만 추가하며 Vultr 서버 파일, Git 저장소, DB, PM2 및 배포 설정은 변경하지 않는다.

접속 대상은 다음과 같다.

- SSH 호스트 별칭: `tycheworks`
- 서버 프로젝트 경로: `/home/linuxuser/workspace/chemical-safety-vr`
- 원격 Codex 절대 경로: `/home/linuxuser/.local/bin/codex`

SSH 서버 주소, 개인 키 경로와 인증 정보는 이 문서에 기록하지 않는다. 해당 값은 사용자 PC의 기존 SSH 설정에서 관리한다.

### 15.11.2 생성한 로컬 항목

| 용도 | 경로 |
|---|---|
| PowerShell `cvr` 명령 | `C:\Users\user\AppData\Roaming\npm\cvr.cmd` |
| 바탕화면 바로가기 | `C:\Users\user\Desktop\Chemical Safety VR - Vultr Codex.lnk` |

기존 `C:\Users\user\Desktop\cvr.cmd`는 내용을 먼저 확인했으며 삭제하거나 덮어쓰지 않고 그대로 보존했다.

바탕화면 바로가기는 고정된 Windows PowerShell 실행 파일을 `-NoProfile`로 시작하고 PATH에 설치한 `cvr.cmd`를 호출한다. `cvr.cmd`는 Windows OpenSSH의 절대 경로를 사용해 `tycheworks`에 TTY로 접속한 뒤, 프로젝트 경로로 이동하고 원격 Codex의 절대 경로를 실행한다. 따라서 로컬 PowerShell 프로필과 원격 비대화형 셸의 PATH에 의존하지 않는다.

SSH 연결 또는 Codex 실행이 실패하면 종료 코드를 표시하고 키 입력을 기다린다. 바로가기에도 `-NoExit`를 적용해 오류가 발생해도 창이 즉시 닫히지 않도록 했다.

### 15.11.3 사용 방법

현재 PowerShell 창에서 다음 명령만 실행하면 같은 창에서 서버 Codex 세션이 열린다.

```powershell
cvr
```

새 창이 필요하면 바탕화면의 `Chemical Safety VR - Vultr Codex` 바로가기를 더블클릭한다. 실행 흐름은 `Windows PowerShell → ssh tycheworks → /home/linuxuser/workspace/chemical-safety-vr → Codex` 순서다.

### 15.11.4 확인한 환경과 근본 원인

- Windows PowerShell `5.1`과 Windows OpenSSH `9.5p2`를 확인했다.
- Windows Terminal이 설치되어 있으나 바로가기 대상은 경로가 고정된 Windows PowerShell로 구성했다.
- 기존 바탕화면 `cvr.cmd`는 Desktop 폴더가 PATH에 포함되지 않아 PowerShell에서 `cvr`만으로 발견되지 않았다.
- `ssh tycheworks` 연결과 서버 프로젝트 디렉터리 존재는 정상으로 확인했다.
- 원격 비대화형 SSH에서는 `codex`가 PATH에서 발견되지 않았다.
- 원격 로그인 셸에서 Codex가 `/home/linuxuser/.local/bin/codex`에 있고 `codex-cli 0.150.1`로 실행됨을 확인했다.
- 위 두 PATH 문제를 피하기 위해 로컬 PATH 폴더에는 `cvr.cmd`를 별도로 두고, 원격 Codex는 절대 경로로 실행하도록 구성했다.

### 15.11.5 완료한 검증

- 새 `-NoProfile` PowerShell에서 `Get-Command cvr`가 `C:\Users\user\AppData\Roaming\npm\cvr.cmd`를 찾는지 확인했다.
- 바로가기의 대상, 실행 인수, 작업 폴더와 설명을 다시 읽어 따옴표와 경로가 보존됐는지 확인했다.
- 바탕화면 바로가기를 실제 실행해 별도 PowerShell 프로세스와 `ssh.exe -tt tycheworks` 프로세스가 생성되는지 확인했다.
- Vultr에서 `/home/linuxuser/.local/bin/codex`와 Codex code-mode host 프로세스가 실행 중인지 확인했다.
- 사용자가 실제 열린 세션에서 접속 성공을 확인했다.
- 원본 모노레포 작업 트리가 변경 전에는 깨끗했으며, 서버 코드·설정에는 변경을 가하지 않았다.

### 15.11.6 다른 PC에서 재설정할 때의 주의사항

`C:\Users\user`가 포함된 로컬 경로는 현재 PC 사용자 계정에만 해당한다. 다른 Windows 계정이나 PC에서는 Desktop, `%APPDATA%\npm`, PowerShell 및 OpenSSH 실제 경로를 다시 확인해야 한다. 또한 `tycheworks` SSH 별칭이 먼저 정상 연결되고, 원격 프로젝트와 Codex 절대 경로가 유지되는지 읽기 전용으로 검증한 뒤 바로가기를 만든다.

## 2026-08-27 클라이언트 저장소 Unity 프로젝트 루트 평탄화

### 적용한 변경

- 별도 클라이언트 저장소 안의 `client/Assets`, `client/Packages`, `client/ProjectSettings`를 각각 저장소 루트의 `Assets`, `Packages`, `ProjectSettings`로 이동했다.
- 모든 `.meta` 파일과 GUID를 그대로 보존했고, 동일했던 `client/.vsconfig` 중복본은 제거했다.
- `.gitignore`, VS Code 설정, Codex 하네스 경로, 제작 스크립트, 저장소 안내와 현재 경로를 설명하는 문서를 새 Unity 프로젝트 루트에 맞췄다.
- Unity Hub에서 잘못 저장소 루트를 열며 생성됐던 빈 프로젝트 설정과 이전 위치의 `Library`, `Logs`, `Temp`, `UserSettings` 캐시는 이동하지 않고 제거했다.
- 서버 소스는 별도 저장소에 유지하며 이 저장소에 `server/` 트리를 추가하지 않았다.

### 근본 원인과 영향 범위

- 클라이언트와 서버를 별도 저장소로 분리한 뒤에도 기존 모노레포의 `client/` 하위 Unity 프로젝트 구조가 남아 있어, 저장소 루트와 Unity Hub에서 열 프로젝트 경로를 혼동할 수 있었다.
- 평탄화 후 Unity 프로젝트 루트는 클라이언트 저장소 루트와 동일하다. Unity 내부의 `Assets/...` 경로와 직렬화 GUID는 바뀌지 않으므로 씬·프리팹·머티리얼 참조에는 의도된 변경이 없다.
- 서버 API, 인증, 텔레메트리, 대시보드 및 서버 저장소 구조에는 변경이 없다.

### 완료한 검증

- Git이 이동된 8,602개 추적 항목을 내용이 동일한 `R100` rename으로 인식하는 것을 확인했다.
- 루트 `Assets` 8,574개, `Packages` 2개, `ProjectSettings` 26개 추적 파일과 Unity `6000.4.8f1` 버전 파일을 확인했다.
- 실행 설정과 도구에서 이전 `client/` 프로젝트 경로 참조가 0개인 것을 확인했다.
- `Packages/manifest.json`, 활성 Build Settings 씬 경로, PowerShell 도구 구문과 `git diff --check`를 정적으로 확인했다.
- Unity 배치 실행이 새 루트의 `Assets`와 `Library/` 재구성을 인식하고 초기 도메인 로드까지 진행한 것을 확인했다.

### 아직 필요한 수동 검증

- Unity 배치 검증은 Licensing Client 재연결 실패로 C# 전체 컴파일과 `DocumentationPolicyHarness.Validate()` 실행 전에 중단했다. 구조나 C# 컴파일 오류로 완료된 것으로 해석하지 않는다.
- Unity Hub에서 저장소 루트를 Unity `6000.4.8f1`로 열고 최초 재임포트가 끝난 뒤 Console 컴파일 오류가 없는지 확인한다.
- `Tools > Documentation > Validate Authoring Policy`를 실행하고 `Assets/Scenes/0_App.unity`의 Build Settings 및 정상 로드를 확인한다.
- Quest/OpenXR Play Mode와 양안 렌더링은 이번 경로 이전의 정적 검증 범위에 포함하지 않았으며 별도 실기 확인이 필요하다.

## 2026-08-28 결정: Meta Horizon APK용 클라이언트 텔레메트리 전송 구조

### 목적과 현재 기준

- 목적은 Meta Horizon 릴리스 채널에 제출한 Quest APK에서 실제 Meta 테스트 계정으로 훈련을 실행하고,
  해당 세션의 이벤트를 기존 Vultr 서버 프로젝트로 안전하게 전송해 MySQL과 관리자 대시보드에서 같은
  `sessionId`로 확인할 수 있게 하는 것이다.
- Vultr에는 Express·Nginx·HTTPS·MySQL과 관리자 인증 기반이 이미 배포되어 있다. 새 서버를 만드는 작업이
  아니라 기존 서버 프로젝트에 훈련 텔레메트리 수신·저장·조회 기능을 추가한다.
- 클라이언트 `main` 기준 커밋은 `2f7250e6c3ecdeaf9f712676d7d01074a6120b94`, 서버 저장소 로컬
  `main` 기준 커밋은 `e5add7cdd3f0462a287bc9e193b95da41dafa5bd`다. Vultr에 실제 배포된 현재
  커밋 SHA는 구현 시작 전에 서버에서 별도로 확인해야 한다.
- 현재 Unity는 `PPETrainingTelemetryCapture`가 `Application.persistentDataPath/tyche-training-telemetry`에
  세션 JSONL을 기록한다. 전체 JSONL의 운영 HTTPS 업로더와 서버 ACK 기반 재전송 상태는 아직 없다.
- 이번 기록은 구현 결정만 확정한다. 사용자가 우선 진행할 버그 수정과 분리하며 클라이언트 코드·씬,
  서버 코드·DB·배포 상태는 변경하지 않는다.

### 확정한 클라이언트 흐름

```text
PPE 이벤트 발생
→ 로컬 JSONL 원본 기록
→ Meta 앱 범위 사용자 ID와 일회용 User Proof 획득
→ Vultr가 Meta 사용자 증명을 검증하고 단기 업로드 토큰 발급
→ 세션 시작·이벤트 배치·세션 완료 전송
→ 서버 ACK의 마지막 sequence를 로컬에 저장
→ 실패하거나 앱이 종료되면 다음 실행에서 미확인 이벤트 재전송
```

- 로컬 JSONL 기록은 네트워크·인증·서버 상태와 관계없이 계속한다. 업로드 실패가 PPE 입력, 음성, UI,
  씬 전환이나 훈련 완료를 막아서는 안 된다.
- 원본 JSONL은 서버가 저장 성공을 확인하기 전에 삭제하지 않는다. 서버 ACK 이후에도 직렬화 설정으로
  정한 보존 기간까지 유지한 뒤 정리한다.
- 직접 PPE 씬 시작과 SDKless Editor는 익명 로컬 JSONL만 기록한다. Meta 인증과 서버 업로드는
  `0_App`에서 시작한 실제 Meta SDK 실행만 소유한다.
- Editor에서 Meta 테스트 계정을 명시적으로 사용하는 경우에는 테스트 서버만 사용한다. 제출 APK는
  `0_App`에서 시작하며 운영 HTTPS 설정을 사용한다.

### 클라이언트 구성요소와 작성 기준

| 구성요소 | 책임 |
|---|---|
| `PPETrainingTelemetryCapture` | 기존 PPE 이벤트와 로컬 JSONL 원본 생성 유지 |
| `TycheTelemetryApiConfig` | 개발·테스트·운영 기본 주소, 배치·재시도·보존 설정 소유 |
| `TycheMetaSessionAuthenticator` | 앱 범위 Meta ID와 `Users.GetUserProof()` 일회용 nonce 획득 |
| `TycheTrainingTelemetryUploader` | 미완료 세션 탐색, 인증, 배치 전송과 재시도 |
| `TycheTelemetryUploadState` | 세션별 서버 ACK와 마지막 확인 sequence의 로컬 상태 |

- 업로더와 환경 설정은 `0_App`의 Inspector 직렬화 참조를 기준으로 작성하고 씬 전환 후 유지한다.
  누락된 설정·프리팹·참조를 런타임에서 자동 생성하거나 운영 주소를 C#에 하드코딩하지 않는다.
- 기존 직접 씬 테스트를 보존하기 위해 업로더를 `RuntimeInitializeOnLoadMethod`로 모든 씬에 자동 설치하지 않는다.
- Meta App Secret, App Access Token, MySQL 자격 증명과 관리자 비밀정보는 APK, `PlayerPrefs`, JSONL에
  저장하지 않는다. Meta 사용자 증명 검증에 필요한 비밀정보는 Vultr 서버만 소유한다.
- `metaAgeCategory`는 서버 업무 요구와 개인정보 처리 근거가 별도로 승인되기 전에는 운영 텔레메트리로
  업로드하지 않는다. 서버 계정 연결의 기준은 검증된 앱 범위 Meta 사용자 ID다.

### 이벤트 식별과 배치 계약

현재 이벤트에 다음 필드를 추가하는 방향으로 계약을 확정한다.

```json
{
  "schemaVersion": 1,
  "sessionId": "abc123",
  "eventId": "abc123:00000042",
  "sequence": 42,
  "timestampUtc": "2026-08-28T00:00:00.0000000Z",
  "eventType": "ppe_choice_resolved"
}
```

- `sequence`는 세션 안에서 1부터 단조 증가한다.
- `eventId`는 `sessionId + sequence`로 결정적으로 생성한다.
- 서버는 `eventId`에 고유 제약을 적용해 같은 배치를 재전송해도 원본 이벤트가 중복 저장되지 않게 한다.
- 기존 PPE 이벤트 필드와 JSONL 원본 형식은 필요한 범위에서 확장하며 기존 의미를 바꾸지 않는다.
- 초기 기본 배치 크기는 25건으로 하되 Inspector/설정 자산에서 조정 가능하게 한다.

클라이언트가 요구하는 서버 응답은 최소한 다음 상태를 포함한다.

```json
{
  "accepted": 23,
  "duplicates": 2,
  "rejected": 0,
  "acceptedThroughSequence": 75
}
```

클라이언트는 HTTP 성공 여부만으로 파일 전체를 완료 처리하지 않고 `acceptedThroughSequence`까지만
서버 저장 확인 상태로 기록한다. `rejected`가 있으면 해당 이벤트와 서버 오류 코드를 보존해 진단할 수 있어야 한다.

### Meta 인증과 예상 서버 계약

- 앱 범위 Meta 사용자 ID 문자열만 전송하는 기존 개발용 등록은 운영 사용자 검증으로 사용하지 않는다.
- `MetaPlatformIdentityProbe`의 entitlement와 `Users.GetLoggedInUser()` 성공 후 `Users.GetUserProof()`를 호출해
  한 번만 검증 가능한 nonce를 얻는다.
- 클라이언트는 Meta ID와 nonce를 Vultr 인증 API로 보내고, 서버는 Meta API와 서버에만 보관된 자격 증명으로
  사용자를 검증한 뒤 짧은 수명의 Tyche 업로드 토큰을 발급한다.
- 일회용 nonce를 재사용하거나 Meta 테스트 사용자 access token을 APK에 포함하지 않는다.

서버가 최종 계약 검토에서 확정할 예상 경로는 다음과 같다.

```text
POST /api/training-telemetry/auth/meta
POST /api/training-telemetry/sessions
POST /api/training-telemetry/sessions/{sessionId}/events
POST /api/training-telemetry/sessions/{sessionId}/complete
```

### 실패·재전송 정책

- 연결 실패 시 기본 재시도 간격은 `2초 → 5초 → 10초 → 30초 → 최대 60초`이며 작은 무작위 지연을 더한다.
- 앱 실행 중 네트워크가 끊기면 원본 기록만 계속하고 연결 복구 후 다시 전송한다.
- 앱 재시작 시 완료 ACK가 없는 세션 파일부터 탐색한다.
- 서버의 수락·중복·거부 수와 마지막 sequence를 별도 상태 파일에 원자적으로 저장한다.
- 운영 업로드 오류는 Console `Error Pause`로 훈련을 정지시키는 반복 `Debug.LogError`가 아니라 제한된 경고와
  로컬 진단 상태로 남긴다. 데이터 손상이나 계약 위반은 한 번의 명확한 오류로 구분한다.
- 배치 크기, 요청 제한 시간, 최대 재시도 간격, 보존 기간과 저장 한도는 직렬화 설정으로 관리한다.

### 구현 순서와 완료 조건

현재 버그 수정을 먼저 완료한 뒤 다음 순서로 별도 작업한다.

1. 서버 저장소에서 Meta 증명 검증, 이벤트 스키마, ACK 응답과 오류 코드를 확정한다.
2. 서버 API·신규 migration·중복 제거 자동 테스트를 구현하고 테스트 DB에서 검증한다.
3. 서버 대상 브랜치와 커밋 SHA, 환경별 HTTPS 주소와 요청·응답 예시를 클라이언트 작업에 전달한다.
4. 클라이언트 이벤트에 `schemaVersion`, `eventId`, `sequence`를 추가한다.
5. `0_App` 작성형 설정·인증·업로더·ACK 상태와 오프라인 재전송을 구현한다.
6. Unity Editor 테스트 계정으로 테스트 API·DB·대시보드의 같은 `sessionId`와 이벤트 수를 확인한다.
7. Quest 개발 빌드에서 네트워크 차단·복구, 강제 종료·재실행과 중복 전송을 검증한다.
8. 운영 HTTPS 주소를 사용하는 서명 APK를 Meta Horizon Alpha 채널에 제출하고 실제 테스트 계정으로 재검증한다.

한 회차의 완료 조건은 다음과 같다.

- Meta 테스트 사용자의 User Proof가 서버에서 검증된다.
- `Unity 전송 수 = API 수락 수 = DB 고유 이벤트 수 = 대시보드 원본 수`가 일치한다.
- 같은 배치를 두 번 보내도 DB 고유 이벤트 수가 증가하지 않는다.
- 네트워크 차단과 앱 강제 종료 뒤에도 미확인 이벤트가 복구된다.
- SDKless Editor와 직접 PPE 씬의 로컬 익명 기록 동작이 유지된다.
- Quest/OpenXR 양안, PPE 입력, 음성, UI와 훈련 상태 전이가 업로더 때문에 정지하거나 변경되지 않는다.

정적 계약이나 자동 테스트만 통과한 상태를 Meta Horizon 실기 수집 완료로 보고하지 않는다. 최종 완료는
Meta Alpha APK의 실제 Quest 세션이 Vultr·MySQL·대시보드까지 같은 식별자와 수량으로 확인된 경우다.

### 새 클라이언트 로컬 Unity 업로드 로직 구현 상태

이번 후속 작업은 이전 모노리포가 아니라 분리된 `chemical-safety-vr-client` 저장소의 로컬 Unity Editor 테스트 경로만 대상으로 한다.

- 새 JSONL 이벤트에 `schemaVersion=1`, `sourceProject=chemical-safety-vr-client`, 세션별 단조 증가 `sequence`와 결정적 `eventId`를 기록한다.
- 기존 `Application.persistentDataPath/tyche-training-telemetry` 폴더는 유지하지만, 업로더는 위 출처·스키마 표식이 없는 이전 모노리포 JSONL을 무시한다.
- `TycheTrainingTelemetryUploader`는 최대 25건 기본 배치로 세션 시작·원본 이벤트·세션 완료 API를 호출하고 서버의 `acceptedThroughSequence`를 별도 상태 파일에 보존한다.
- 서버 연결 또는 앱 종료 뒤에도 로컬 JSONL을 먼저 보존하며, 다음 Editor 실행에서 미확인 sequence부터 재전송한다.
- 현재 로컬 통합 시험은 `TYCHE_TELEMETRY_UPLOAD_TOKEN` 환경 변수와 loopback 서버 주소만 허용한다. 토큰은 저장소, `PlayerPrefs`, JSONL과 씬에 기록하지 않는다.
- 새 클라이언트는 `client-instance-id.txt`에 32자리 익명 설치 ID를 보존한다. Meta 테스트 ID가 수집되면 세션 시작 요청에만 이를 선택적으로 포함하고, 없으면 설치 ID만 보낸다.
- 서버는 두 경우 모두 숫자형 자체 `participantId`를 발급한다. 같은 Meta ID는 기기가 달라도 같은 자체 ID로, Meta ID가 없는 경우에는 같은 설치 ID가 같은 자체 ID로 조회된다.
- 공유 기기에서 서로 다른 사용자를 잘못 합치지 않기 위해 익명 설치 ID와 이후 확인된 Meta ID는 자동 병합하지 않는다. 원본 Meta ID와 설치 ID 값은 개발 조회 화면에 노출하지 않는다.
- 업로드 DTO에서 `metaAppScopedUserId`와 `metaAgeCategory`를 제외하고 로컬 파일 경로가 포함된 note는 전송 전에 대체한다.
- Player와 출시 APK에서는 현재 테스트 토큰 업로드를 시작하지 않는다. Meta User Proof와 서버 단기 토큰 인증은 출시 전 별도 구현·검증한다.

`Tools > PPE > Configure Local Telemetry DB Upload`은 사용자가 `0_App` 씬을 명시적으로 연 상태에서만 `AppMain`에 업로더를 추가한다. 기존 컴포넌트가 있으면 Inspector 값을 덮어쓰지 않는다. Unity가 생성한 컴파일 응답 설정으로 런타임 업로더와 Editor 설정 도구의 최신 소스를 별도 임시 DLL에 컴파일해 오류가 없음을 확인했다. 테스트 DB migration과 Express → MySQL 합성 데이터 왕복은 완료했으며, 열린 Unity의 자동 재컴파일, 설정 메뉴 실행, 씬 저장과 실제 Unity → Express → MySQL 전송은 아직 남아 있다.

서버 텔레메트리 구현은 서버 `main`의 `a036897453724f5ab9bc9f0aecbfb3c3b36d4a23`에 커밋·푸시했다. 클라이언트 연동 구현은 `260828_ppe_client_integration_followup` 브랜치의 `ae997d68970deeeacf34af27137425047ffaff72`에 커밋했고, 저장소 이력상 2026-08-28 18:24:28 KST의 `e4c34177bdb2e9cd962592f863edb661c53d52a1`에서 클라이언트 `main`에 병합됐다. 아래 로컬 MySQL·실기 결과는 이 구현의 검증 근거지만 운영 DB migration, 운영 수집과 Meta Alpha APK 통합 완료를 의미하지 않는다.

### 2026-08-28 로컬 MySQL·인코딩 통합 검증

서버 저장소 `main`의 기준 커밋 `0d47517faa2daa01e3fb337681a3b0b9bd7ea9a4`와 클라이언트 저장소 `main`의 기준 커밋 `2f7250e6c3ecdeaf9f712676d7d01074a6120b94` 위 작업 트리를 대상으로 검증했다. 두 저장소의 텔레메트리 변경은 아직 별도 커밋 SHA가 없으므로 기준 커밋과 작업 트리 변경을 구분한다.

- 로컬 전용 `tyche_training_test`에 migration `001`~`012`를 적용했다. 기존 실행기가 migration 파일 하나를 SQL 문 하나로 실행하므로 텔레메트리 테이블은 `009`~`012` 네 파일로 분리했다.
- MySQL `8.0.17`, 데이터베이스 문자 집합 `utf8mb4`, collation `utf8mb4_unicode_ci`를 확인했다.
- `/telemetry-ingest-test/`는 HTTP `200`으로 응답했고 서버 자동 테스트는 33개 통과, 실패 0개다.
- 실제 MySQL 합성 데이터 결과는 참여자 2명, 식별자 2개, 세션 4개, 원본 이벤트 8개다. 실제 사용자 정보와 이전 모노리포 JSONL은 사용하지 않았다.
- Meta ID가 없는 동일 설치 ID는 두 세션 모두 자체 `participantId=1`, 같은 Meta 테스트 ID는 서로 다른 설치 ID의 두 세션 모두 `participantId=2`로 유지됐다.
- 각 최초 세션의 3개 이벤트는 `accepted=3`, `acceptedThroughSequence=3`, 완료 상태 `completed`로 확인했다.
- 합성 이벤트의 `itemName=안전모`, `note=한글 행동 데이터 정상 수신`을 UTF-8 JSON으로 전송하고 MySQL 저장 뒤 상세 조회 API에서 같은 문자열로 확인했다.
- Bearer 토큰은 HTTP 헤더 인코딩 문제를 막기 위해 16~512자의 공백 없는 ASCII만 허용한다. HTML과 Unity 업로더도 전송 전에 같은 조건을 검사한다. 행동 데이터 JSON과 HTML 문서는 UTF-8, DB는 `utf8mb4`를 사용한다.
- 현재 로컬 검증은 로컬 관리자 계정을 사용했으며 운영 자격 증명, 운영 DB, PM2, Nginx와 배포 상태는 변경하지 않았다. 운영 적용 전에는 최소 권한 애플리케이션 계정으로 전환해야 한다.
- 브라우저 자동 제어 백엔드를 사용할 수 없어 화면의 시각·클릭 검증은 수행하지 못했다. HTML 제공, 스크립트 문법, 샘플과 조회 API 왕복은 자동·통합 검사로 확인했다.

이 검증은 Express → 로컬 MySQL → 조회 API 경로의 완료를 의미한다. 새 Unity 클라이언트의 런타임·Editor 업로더 최신 소스는 Unity 컴파일 설정으로 오류 없이 컴파일했지만, `0_App` 설정 메뉴 실행과 실제 Unity Play Mode 행동 이벤트 전송은 아직 남아 있다. Meta User Proof, Quest Player, 운영 HTTPS와 Vultr 배포 성공으로 확대 해석하지 않는다.

### 2026-08-28 첫 Unity 실기 수집 재현 결과

사용자가 `0_App`부터 시작해 HMD를 착용하고 Education·Training·Test 모드와 작업 시나리오를 순회한 뒤 DB 확인 화면에서 익명 세션과 Education만 보이는 문제를 재현했다.

- 당시 DB에는 실제 Unity 세션이 0건이었다. 표시된 참여자·세션은 이전 Express → MySQL 합성 검증 데이터뿐이었고, 익명 자체 ID `1`의 합성 세션 2개가 실제 익명 사용자 2명처럼 오해될 수 있었다.
- 새 클라이언트 원본 `session-20260828-050205-32bf72164cbf4e19a655679b17721522.jsonl`은 `sourceProject=chemical-safety-vr-client` 표식을 가진 실제 Unity 파일이며 원본 이벤트 1,294건을 정상 기록했다.
- 원본의 모드 분포는 Education 507건, Training 419건, Test 360건이다. 작업계획 분포는 ConfinedSpace 631건, LeakResponse 612건, None 43건으로 전체 실행 사실이 로컬 원본에 남아 있다.
- Meta 상태는 Idle 2건 뒤 `SkippedForEditorTesting` 1,292건이었다. `0_App`의 `useMetaPlatformSdkInEditor=0` 때문에 HMD 착용 여부와 무관하게 Platform SDK, entitlement와 앱 범위 Meta ID 조회를 의도적으로 건너뛴 것이 원인이다.
- Unity Editor 로그에는 `TYCHE_TELEMETRY_UPLOAD_TOKEN가 없어 로컬 DB 업로드를 시작하지 않습니다`가 기록됐다. 따라서 JSONL 수집은 성공했지만 Unity → Express → MySQL 전송은 시작되지 않았다.

재현 뒤 `0_App`의 `useMetaPlatformSdkInEditor`를 켜고, Windows Editor 업로더가 프로세스 환경 변수에 값이 없을 때 사용자 범위 `TYCHE_TELEMETRY_UPLOAD_TOKEN`도 읽도록 보완했다. 확인 HTML에는 `앱 버전/출처` 열을 추가해 `local-db-test`, `identity-repeat-test` 합성 세션과 Unity `0.1.0` 원본 세션을 구분한다. 서버 자동 테스트 33개와 Unity 런타임·Editor 컴파일을 다시 통과했다.

다음 실기에서는 Meta probe가 `Completed` 또는 `CompletedWithoutAgeCategory`인지, 앱 범위 Meta ID가 세션 시작 요청에 포함되는지, Unity 원본 세션이 MySQL에서 새 자체 ID와 전체 이벤트 수로 조회되는지를 확인한다. `Failed`나 `SkippedForEditorTesting`이면 Meta 사용자 수집 성공으로 표시하지 않는다.

### 2026-08-28 두 번째 Unity 실기 수집 재현 결과

`0_App`에서 다시 시작한 새 클라이언트 세션 `2e3a04b3f9be40b68d729e5b554650c6`은 로컬 원본 JSONL에 67개 이벤트를 기록했다. Education 22건과 Training 39건, 작업계획 ConfinedSpace 35건과 None 26건이 포함됐으며 이 짧은 실행에는 Test와 LeakResponse 이벤트가 없다. 따라서 클라이언트 행동 수집 자체는 확인했지만 전체 모드·시나리오 회차로 보지는 않는다.

이 실행 직후 서버 조회에는 여전히 합성 검증 세션 4건만 있었고 실제 Unity 세션은 없었다. Unity 로그에는 다음 두 원인이 각각 확인됐다.

- 사용자 범위 테스트 토큰은 32자의 유효한 ASCII 값으로 존재했지만, 기존 Unity/Hub 프로세스는 계속 환경 변수가 없다고 판단해 업로더를 시작하지 않았다.
- Meta XR Platform SDK v203 이상과 현재 PCLink/Skyline 런타임이 호환되지 않아 Platform 초기화 뒤 요청을 처리하지 못했다. 원본의 Meta 상태는 `Initializing`에서 종료됐고 앱 범위 Meta ID는 기록되지 않았다.

Windows Editor 업로더는 프로세스·사용자 범위 환경 변수에 이어 현재 사용자 레지스트리 값을 직접 읽도록 보완했다. Meta 조회가 응답하지 않아도 `session_ended`가 기록된 세션은 더 이상 무기한 대기하지 않고 Meta ID 없는 익명 사용자로 적재한다. Meta ID가 이미 기록된 경우에는 기존처럼 Meta 식별을 우선한다. 런타임과 Editor 소스를 Unity 컴파일 응답 설정으로 다시 컴파일해 새 오류가 없음을 확인했다.

첫 실기의 1,294개 이벤트와 두 번째 실기의 67개 이벤트 원본은 삭제되지 않았다. 다음 Play에서 Unity가 최신 소스를 실제 재컴파일하고 업로더가 시작되면 두 파일 모두 새 클라이언트 출처 검사 뒤 재전송 대상이 된다. 다만 과거 파일에 없던 Meta ID를 소급 생성할 수는 없으므로 두 과거 세션은 익명 자체 ID로 적재된다. Meta 사용자 수집 성공은 PCLink/Platform SDK 호환을 해결한 뒤 새 세션에서 `Completed` 또는 `CompletedWithoutAgeCategory`, 앱 범위 Meta ID와 DB의 새 `participantId`를 함께 확인해야 한다.

Meta Horizon 입점용 출시 경계에서는 이 익명 보완을 사용하지 않는다. Meta ID가 없는 사용자 수용은 로컬 QA·비 Meta 테스트 채널과 장애 진단을 위한 요구사항이다. 출시 APK는 entitlement, 앱 범위 Meta 사용자 ID와 User Proof의 서버 검증이 끝나기 전에는 해당 회차를 정상 사용자 수집 성공으로 표시하거나 익명 `participantId`로 확정하지 않는다. 일시적인 인증·네트워크 실패 때에는 원본을 기기에 보존하고 재시도하며, 반복 실패는 별도 진단 상태로 남긴다. 운영 서버에는 검증된 Meta 인증이 없는 세션을 거부하는 환경별 강제 설정을 적용하고, 실제 Quest 새 세션에서 `Meta ID → 서버 자체 participantId → 전체 이벤트` 연결을 확인해야 입점용 통합 완료로 판단한다.

### 2026-08-28 Unity → Express → MySQL 첫 실제 적재 결과

열려 있던 Unity 프로세스가 프로세스·사용자 환경 변수와 현재 사용자 레지스트리 값을 모두 읽지 못하는 현상이 반복됐다. 저장소 밖 `Application.persistentDataPath/tyche-training-telemetry/.editor-upload-token`에 로컬 Editor 전용 토큰을 두고 업로더가 마지막 보완 경로로 읽도록 변경했다. 이 파일과 값은 Git, 씬, JSONL과 출시 Player에 포함하지 않는다.

보완 소스의 Unity 실제 재컴파일 뒤 Play Mode를 다시 시작해 다음 결과를 확인했다.

- 과거 새 클라이언트 실제 세션 2건이 같은 익명 자체 ID `participantId=3`으로 MySQL에 적재됐다.
- 세션 `32bf72164cbf4e19a655679b17721522`는 원본 이벤트 1,294건, 세션 `2e3a04b3f9be40b68d729e5b554650c6`은 67건이다.
- 두 세션은 `appVersion=0.1.0`, 상태 `completed`, 합계 1,361개 고유 이벤트로 조회됐다.
- 각 JSONL 옆에 완료 ACK를 보존한 `.upload-state.json`이 생성됐다.
- 합성 검증 세션과 실제 Unity 세션은 확인 화면의 앱 버전·출처에서 구분된다.

이는 로컬 Editor에서 `Unity JSONL → Express API → 로컬 MySQL → 조회 화면` 경로가 실제 행동 원본으로 동작함을 의미한다. 두 과거 세션에는 수집 당시 Meta ID가 없었으므로 익명 사용자로 적재됐으며 Meta 입점 연동 성공으로 보지 않는다. 사용자는 이번 적재 확인 Play를 HMD 없이 Game View로 진행했다. 같은 시점의 Meta probe에 `Invalid OAuth 2.0 Access Token` 코드 190과 빈 앱 범위 Meta ID가 남았지만, 이 무HMD 실행만으로 Meta 앱·테스트 계정 설정 결함을 확정하지 않는다. 다음 완료 조건은 HMD를 착용하고 Meta 테스트 계정이 활성화된 별도 새 세션에서 `Meta 연결 성공`, 자체 `participantId`와 원본 이벤트를 함께 확인하는 것이다.

### 2026-08-28 클라이언트 수정 중 통합 테스트 보류 결정

클라이언트의 별도 오류 수정이 완료될 때까지 추가 Game View, HMD, Meta ID와 Unity → DB 통합 테스트를 보류한다. 현재 확인한 익명 실제 세션 2건과 원본 이벤트 1,361건, 완료 ACK와 MySQL 조회 결과는 삭제하거나 성공 범위를 확대하지 않고 기준 증거로 보존한다. 테스트 재개 전에는 클라이언트 컴파일 오류가 없고 `0_App` 시작 흐름과 HMD 실행 준비가 끝났는지 먼저 확인한다.

보류 기간에는 Linux 서버 작업 환경에서 홈페이지와 대시보드 정적 화면을 병렬 수정할 수 있다. 충돌을 피하기 위한 범위는 다음과 같다.

- 홈페이지는 `public/site/**`, 대시보드는 `public/dashboard/**`를 별도 Git 브랜치에서 수정한다.
- 현재 Windows 작업 트리의 `src/app.js`, `src/modules/training-telemetry/**`, `public/telemetry-ingest-test/**`, migration `009`~`012`, 환경 설정, README와 이 공용 문서는 Linux 병렬 작업에서 수정하지 않는다.
- 병렬 작업 브랜치는 커밋과 push까지만 수행하고 현재 작업이 정리되기 전에 `main`에 병합하지 않는다.
- 운영 배포, 운영 DB migration, PM2 재시작과 Nginx 공개 변경은 별도 승인 전까지 수행하지 않는다.
- 작업 완료 뒤 브랜치명과 커밋 SHA를 기준으로 겹치는 파일과 계약 변경을 확인한 후 병합한다.

보류 결정 시점의 기준 커밋은 서버 `main`/`origin/main` `0d47517faa2daa01e3fb337681a3b0b9bd7ea9a4`, 클라이언트 `main`/`origin/main` `2f7250e6c3ecdeaf9f712676d7d01074a6120b94`다. 텔레메트리와 문서 변경은 양쪽 작업 트리에 아직 커밋되지 않았으므로 기준 커밋과 구분한다. 이번 결정은 문서화만 수행하며 실행 코드, DB 데이터와 운영 상태를 추가로 변경하지 않는다.


### 2026-08-28 HMD 실기 재개 결과와 출시 APK 후속작업

클라이언트 오류 수정 중 보류했던 실기 테스트를 재개했다. 사용자는 HMD를 착용하고 `0_App`부터 시작해 Education·Training·Test와 두 작업계획을 연속 수행했다. 원본 씬 순서는 `0_App → 1_Title → 2_Intro → 6_LoadingScene_0 → 3_PPE_Room_3mode_loco`로 확인됐으므로 이번 Meta ID 누락은 직접 PPE 씬 시작 문제로 보지 않는다.

#### 완료한 수집·DB 검증

- 대상 세션은 `2ac1c70158554c658c7b14f291ff235c`, 앱 버전은 `0.1.0`, 원본 이벤트는 1,429개다.
- 첫 이벤트 `session_started`, 마지막 이벤트 `session_ended`, 종료 사유 `application_quitting`을 확인했다.
- 서버에서 같은 세션이 `completed`, 이벤트 1,429개로 조회됐고 로컬 ACK 상태도 `acceptedThroughSequence=1429`, `completed=true`로 일치했다.
- 모드는 Education 583건, Training 448건, Test 392건과 초기 미선택 6건으로 구분됐다.
- 작업계획은 ConfinedSpace 718건, LeakResponse 664건, None 41건과 초기 미선택 6건으로 구분됐다.
- 각 모드와 두 작업계획의 조합이 모두 원본 및 DB 상세 조회에 존재했다. 이전 화면에서 Education만 보인 현상은 이번 원본·DB 기준으로 재현되지 않았다.
- 직접 PPE 씬에서 시작한 확인용 세션은 로컬 원본은 생성하지만 `0_App`에만 있는 Editor 업로더가 실행되지 않았다. DB 재전송 확인은 반드시 `0_App`에서 Play를 시작해야 한다.
- 재전송 과정에서 들어온 과거·확인용 세션과 대상 실기 세션을 구분했다. 이번 실기 판정의 기준은 1,429개 이벤트를 가진 위 세션 하나다.

#### Meta ID 누락 원인

대상 세션의 Meta 상태는 Idle 2건 뒤 Initializing 1,427건이었고 앱 범위 Meta 사용자 ID는 비어 있었다. 서버는 같은 설치의 익명 식별자로 `participantId=3`을 부여했다. 이는 로컬 QA 적재 성공일 뿐 Meta 입점 인증 성공이 아니다.

클라이언트는 Meta XR Platform SDK `205.0.0`을 사용한다. 같은 실행의 Unity Editor 로그에는 현재 PC Meta Horizon Link LIVE 런타임이 Meta XR Platform SDK v203 이상과 호환되지 않아 초기화 뒤 Platform 요청을 처리할 수 없다는 오류가 기록됐다. 따라서 HMD 미착용이나 시작 씬 누락이 아니라 Unity Play-In-Editor의 PC Link 런타임 호환 문제가 직접 원인이다.

Meta Horizon Link의 Public Test Channel 전환은 Editor에서 SDK 205를 진단하기 위한 임시 개발 경로다. 출시 사용자가 PTC를 사용해야 한다는 의미가 아니며 Quest 단독 실행 APK는 PC Link를 거치지 않는다.

- Meta XR Platform SDK v205 공식 알려진 문제: https://developers.meta.com/horizon/downloads/package/meta-xr-platform-sdk/
- Meta Horizon Alpha·Beta·RC·Production 릴리스 채널: https://developers.meta.com/horizon/resources/publish-release-channels/

#### 현재 APK 동작 경계

- `MetaPlatformIdentityProbe`는 Player에서 Platform SDK 초기화, entitlement, 앱 범위 사용자 ID 조회를 시도한다.
- `PPETrainingTelemetryCapture`는 Quest의 `Application.persistentDataPath`에 JSONL 원본을 기록할 수 있다.
- 현재 `TycheTrainingTelemetryUploader`는 `Application.isEditor`가 아니면 출시 Player 인증 미연결을 보고하고 업로드를 시작하지 않는다.
- 따라서 현재 APK는 Meta ID와 로컬 JSONL 진단은 가능하지만 `Quest APK → 서버 → MySQL → 확인 HTML` 통합 검증은 불가능하다.
- 단순 sideload만으로 entitlement 성공을 확정하지 않는다. Meta 테스트 계정과 Alpha 릴리스 채널을 사용한 실제 Quest 회차가 최종 기준이다.

#### 재개 시 후속작업

1. Editor 진단이 필요하면 PC Meta Horizon Link를 SDK 205 호환 Public Test Channel로 전환하고 Unity를 재시작한다.
2. HMD와 Meta 테스트 계정으로 `0_App`부터 짧은 새 세션을 실행해 probe가 `Completed` 또는 `CompletedWithoutAgeCategory`인지 먼저 확인한다.
3. 출시 경로에는 앱 범위 Meta ID와 `Users.GetUserProof()`를 획득하는 Quest 인증기를 구현한다.
4. 서버는 User Proof를 Meta 측에 검증하고 검증 성공 세션에만 단기 업로드 토큰과 숫자형 `participantId`를 발급한다.
5. Quest용 HTTPS 업로더에 로컬 원본 우선 저장, ACK, 중복 제거, 네트워크 복구와 앱 재실행 재전송을 연결한다. Editor Bearer 토큰을 APK에 포함하지 않는다.
6. Meta ID가 없거나 검증되지 않은 출시 회차는 익명 정상 사용자로 확정하지 않고 기기에 보존해 재시도·진단 상태로 남긴다.
7. Alpha 채널 APK에서 `Meta ID → User Proof 검증 → participantId → 전체 이벤트 → MySQL → 확인 화면`을 같은 `sessionId`와 이벤트 수로 대조한다.
8. 네트워크 차단·복구, 중복 전송, 강제 종료·재실행 뒤에도 고유 이벤트 수와 완료 상태가 유지되는지 검증한다.

현재 확정된 완료 범위는 `HMD 실기 원본 수집 → 로컬 Editor 업로드 → Express → 로컬 MySQL → 상세 조회`다. Meta ID, Quest Player 업로드, User Proof 서버 검증, 운영 HTTPS, 운영 DB와 Meta Alpha APK 통합은 후속작업으로 보류한다. 다음 재개 때 정적 하네스나 과거 익명 세션을 Meta 인증 성공으로 확대 해석하지 않는다.

### 2026-08-29 교차 저장소 병렬 DB 실테스트 기록 해석

2026-08-28에는 서버 저장소 작업과 클라이언트 저장소 작업이 별도 작업 트리에서 동시에 진행됐고, 그 과정에서 Unity 원본 수집, Editor 업로드, Express 수신과 로컬 MySQL 적재를 실제로 시험했다. Git 커밋 시각은 각 작업 트리의 스냅샷이 저장된 시점이지 Unity 실행, HTTP 전송 또는 DB 적재의 시작·종료 시각이 아니다. 따라서 당시 시험 순서를 커밋 시각만으로 재구성하거나, 한 저장소가 다른 저장소의 최신 병합 상태를 즉시 알고 있었다고 가정하지 않는다.

위에 기록된 세션 `32bf72164cbf4e19a655679b17721522`, `2e3a04b3f9be40b68d729e5b554650c6`, `2ac1c70158554c658c7b14f291ff235c`와 이벤트 수·ACK 결과는 당시 실테스트 기록으로 보존한다. 이번 문서 재검토에서는 클라이언트 `main`이 `ae997d68970deeeacf34af27137425047ffaff72`의 코드와 씬을 포함하고 현재 작업 트리의 핵심 파일 blob도 같은 것을 확인했다. 서버 구현 커밋은 `a036897453724f5ab9bc9f0aecbfb3c3b36d4a23`으로 확인했다.

현재 PC의 `Application.persistentDataPath/tyche-training-telemetry`에는 2026-08-26 세션 파일만 남아 있고 위 2026-08-28 세션 JSONL과 `.upload-state.json`은 다시 찾지 못했다. 이번 재검토에서는 당시 MySQL 행과 Express 요청 로그도 재조회하지 않았다. 이는 기존 실테스트가 수행되지 않았다는 반증이 아니라, 현재 환경에서 원본을 독립적으로 재검증할 수 없는 상태라는 뜻이다. 최종 제출자료나 운영 전환 판단에 이 수치를 다시 사용할 때는 원본 JSONL·ACK, Express 로그와 MySQL 세션·이벤트 행을 같은 `sessionId`로 재대조한다.

현재 상태는 다음과 같이 구분한다.

- 서버 수신·로컬 DB 기반 구현과 클라이언트 코드 병합: Git 코드와 씬 기준 확인 완료
- 2026-08-28 Unity Editor/HMD → Express → 로컬 MySQL 실테스트: 당시 기록 유지, 이번 재검토에서 원본 재조회는 미완료
- Meta ID, Quest Player 업로드, User Proof 검증, 운영 HTTPS·DB와 Meta Alpha APK 통합: 후속작업

2026-08-29 재연동 전 로컬 실행 상태를 확인한 결과 MariaDB `wampstackMariaDB-1`은 TCP `3306`에서 실행 중이었지만 Express의 TCP `3000` 리스너, 서버 `.env`, 서버 업로드 토큰과 Unity Editor용 `TYCHE_TELEMETRY_UPLOAD_TOKEN`은 없었다. 이 시점에는 `Unity → Express → 로컬 DB` 업로드 경로를 실행할 수 없었고 `PPETrainingTelemetryCapture`의 로컬 JSONL 기록만 별도로 계속 가능한 상태였다.

같은 날 사용자 승인 후 로컬 서버 경로를 복구했다. `npm ci`로 서버 의존성을 복원하고, 기존 MariaDB에 `tyche_training` 데이터베이스와 `tyche_app` 계정이 존재하지 않는 것을 확인한 뒤 로컬 전용 계정·데이터베이스를 새로 만들었다. 저장소의 기존 migration `001`~`012`를 적용했으며, Git에서 제외되는 서버 `.env`와 Windows 사용자 범위 Unity Editor 토큰에 같은 로컬 전용 업로드 토큰을 설정했다. 자격 값은 문서·Git·씬·JSONL에 기록하지 않았다.

복구 뒤 Express는 `127.0.0.1:3000`, MariaDB는 `127.0.0.1:3306`에서 실행된다. health, 대시보드와 `/telemetry-ingest-test/`는 HTTP `200`, 무토큰 DB API는 `401`, 토큰을 사용한 세션·참여자 조회는 `200`으로 확인했다. migration 12개는 재실행 시 모두 적용 완료로 판정됐고 서버 자동 테스트 34개가 통과했다. Unity 로컬 JSONL 조회에는 13개 세션이 있지만 새 DB의 참여자·세션·이벤트 행은 모두 0개다. 따라서 2026-08-28 MySQL 행을 현재 DB에서 복구 또는 재검증한 것은 아니며, 새 `0_App` Play에서 실제 Unity 이벤트가 DB에 적재되는지는 별도 수동 검증으로 남는다. Meta PTC·앱 범위 사용자 ID 진단은 DB 재연동과 구분한다.

병렬 작업 중 생성된 서버·클라이언트 고유 기록은 한쪽 파일로 덮어쓰지 않는다. 공용 문서 기준본을 통합할 때 각 절의 서버 코드·DB 근거와 클라이언트 코드·씬·Unity 근거를 각각 확인한 뒤 시간순으로 합친다.

### 2026-09-03 Meta Alpha APK Manifest 사전 검사 보완

Meta 업로드 검사가 첫 출시 서명 APK에서 Android Target SDK 36, 자동 회전 화면 방향, `preferExternal` 설치 위치를 거부했다. 조직 관리자의 개발자 배포 계약 서명은 사용자가 완료했으며, Quest 1 미지원 표시는 Quest 2 이상 대상 앱의 경고로 구분한다.

이번 변경이 대응하는 사용자 요청은 Meta 업로드의 세 Manifest 오류 수정이다. 기존 Inspector·씬 작성값, UI·음성·입력·훈련 상태 전이, 텔레메트리 계약은 보존한다. 단일 기준은 `ProjectSettings/ProjectSettings.asset`의 Android Player Settings이고 입력 이벤트 경로는 변경하지 않는다. 잘못된 값은 런타임에서 자동 수리하지 않고 `MetaQuestAndroidBuildValidationHarness`가 빌드 전에 명확한 오류로 중단한다. 영향 소비자는 Android Manifest와 Meta Alpha 업로드 검사이며, Unity Editor·Standalone 흐름에는 영향을 주지 않는다.

변경 전 기준 실행은 빌드 번호 1 APK에 대한 Meta 업로드 거부와 `aapt2`의 `targetSdkVersion='36'`, 비가로 방향, `install-location:'preferExternal'` 확인이다. 변경 후 Target SDK 34, Landscape, Automatic 설치 위치와 Android 빌드 번호 2를 적용했다. Unity Editor의 `Tools > XR > Validate Meta Quest Android Build` PASS를 확인했고, 재빌드한 `ChemicalSafetyVR_Alpha_0.1.0_2.apk`는 APK Signature Scheme v2 서명, `versionCode='2'`, `targetSdkVersion='34'`, `install-location:'auto'`, `screenOrientation='landscape'`, ARM64, 필수 VR 헤드트래킹과 Meta VR 카테고리를 확인했다. APK SHA-256은 `C04BF625CD0A0FFCBA8EE94CAB193E62963A61BF09FA13B580F31BC29E5F08E1`이다. 2026-09-03 Meta Developer Dashboard에서 해당 빌드가 Quest 2·Quest Pro·Quest 3 패밀리·향후 기기 대상으로 Alpha 채널에 업로드되고 채널 1개에 할당된 것을 확인했다. 정적 설정, 재빌드 APK 검사와 Alpha 업로드는 완료했으며 Quest 2 설치·독립 실행, 양안 화질과 운영 데이터 전송은 아직 완료로 기록하지 않는다.

한국 거주 계정은 최초 Alpha 초대 URL에서 정책상 참여 불가로 표시됐다. Meta 대한민국 배포 정책에 따라 IARC를 다시 작성하되, 시나리오 선택·컨트롤러 과제·교육/훈련/테스트·퀴즈·완료 판정을 포함한 실제 인터랙티브 훈련 시뮬레이션을 `Game`으로 분류하고 현재 빌드에 없는 폭력·공포·성적 내용·도박·약물·사용자 공유·구매·정확한 위치 공유 등의 항목은 `No`로 답했다. 그 결과 GRAC `전체 이용가`가 발급됐고 Meta 앱 메타데이터에 IARC 인증서를 저장했다. 이후 같은 한국 계정에서 Alpha 초대 페이지가 정상 표시되고 릴리스 채널 참여 완료 상태를 확인했다. 남은 검증은 같은 계정의 Quest 2 라이브러리 설치와 Alpha APK 독립 실행이다.

### 2026-09-03 집에서 재개할 작업 인수인계

#### 현재까지 완료한 사실

- 기존 BGM을 제거하고 Suno Pro 계정으로 리마스터한 `Assets/Audio/BGM/XR Horizon Interface (Remastered).mp3`를 타이틀 BGM으로 연결했다. 타이틀의 기존 1초 페이드인 동작을 복구했고 사용자가 실제 재생을 확인했다. Suno Pro 사용 근거는 사용자가 별도로 보관했으며 곡 ID와 생성일 메타데이터 위치는 아직 확인하지 않았다.
- 출시 키스토어를 Android Player Settings에 연결했다. 비밀번호는 저장소에 기록하지 않는다.
- Meta 업로드용 Target SDK 34, 가로 방향, Automatic 설치 위치를 적용했고 빌드 번호 2 APK가 Alpha 채널에 업로드됐다. IARC/GRAC 전체 이용가 저장과 한국 테스트 계정의 Alpha 참여도 완료했다.
- 로딩 씬의 `XROrigin`에 다른 XR 씬과 같은 `XRSessionForwardAlignment`를 연결한 빌드 번호 3 APK를 Alpha에 업로드하고 Meta 배포본으로 Quest 2에 설치했다. 설치 패키지는 `versionCode=3`, `targetSdkVersion=34`, installer `com.oculus.ocms`로 확인했다.
- 빌드 번호 3의 Quest JSONL에서 `3_Loading` 진입은 `07:11:01.647754Z`, `4_PPE_Room` 진입은 `07:11:07.924301Z`로 약 6.28초 간격이었고, 사용자는 로딩 화면 수정 반영을 시각적으로 확인했다.
- 데이터 기준의 시작은 사용자가 Education·Training·Test 모드를 선택해 실행이 확정된 순간으로 유지했다. 완료는 마지막 퀴즈 선택 직후가 아니라 종료 음성, Test 결과 확인 또는 자동 복귀, 암전·페이드인 이후 모드 선택 모달이 다시 표시된 순간에 기록하도록 코드를 정정했다. EXIT Point 중도 복귀는 완료 이벤트를 기록하지 않는다.
- 런타임과 Editor 보조 C# 빌드는 오류 0개로 통과했다. Unity의 `Tools > PPE > Validate Training Data Contract`는 `4_PPE_Room`의 25개 binding과 종료 경로를 PASS했다. 실수로 실행한 `PPE Marker Selection`도 PASS했으며 읽기 전용 검사라 씬을 변경하지 않았다.

#### 현재 정확한 중단 지점

- `ProjectSettings/ProjectSettings.asset`의 `AndroidBundleVersionCode`는 **4**로 올렸지만 code 4 APK는 아직 만들거나 업로드하지 않았다.
- `Tools > PPE > Validate Train Test Modes`는 과거 씬 경로와 과거 상세 컨트롤러 음원 번호를 기대하던 하네스 오류를 순서대로 드러냈다. 기본 대상은 현재 `Assets/Scenes/4_PPE_Room.unity`로, 상세 설명은 `001·002 → 004 → 006`으로 정정했다.
- 마지막 실행에서는 상세 Joystick 단계와 간단 안내 단계의 시각 배열 길이가 같아야 한다는 오래된 조건 때문에 FAIL했다. 실제 작성 상태는 상세 설명 Clip 1개에 시각 1개, 간단 안내 Clip 2개에 같은 시각 2개로 정상이다. 하네스를 `상세 설명 Clip마다 같은 순번의 간단 안내 시각을 재사용`하도록 수정했지만 **수정 후 Unity Refresh와 PASS 재실행은 아직 하지 않았다.**
- code 4에는 완료 경계 수정이 들어가므로 code 3에서 정상 수행시간 데이터를 수집하지 않는다. 현재 Quest의 code 3 앱도 새 기준 표본용으로 실행하지 않는다.

#### 집에서 재개하는 정확한 순서

1. `release/2026-09-01-meta-horizon-submission`은 이미 `main`에 병합됐으므로 저장소의 최신 `main`을 pull하고, `main`에서 별도 후속작업 브랜치를 만든 뒤 Unity `6000.4.8f1`로 연다. 과거 release 브랜치로 다시 전환하지 않는다.
2. Unity에서 `Assets > Refresh`를 누르고 컴파일이 끝날 때까지 기다린다. Play Mode에는 들어가지 않는다.
3. `Tools > PPE > Validate Train Test Modes`를 실행한다. PASS가 아니면 code 4 빌드를 진행하지 않고 오류 원문을 기록한다.
4. `Tools > PPE > Validate Training Data Contract`와 `Tools > XR > Validate Meta Quest Android Build`를 다시 실행해 모두 PASS인지 확인한다. `Import Error Code:(4)` 수정 시간 경고가 다시 나타나면 OpenXR 설정의 디스크 값과 Unity AssetDB가 안정적으로 재임포트됐는지 확인한 뒤 빌드한다.
5. Android Build Profiles의 활성 씬이 `0_App`, `1_Title`, `2_Intro`, `3_Loading`, `4_PPE_Room` 순서인지 확인한다. `5_MixerRoom`, `6_InsideMixer`와 삭제된 Confined Space 항목은 활성화하지 않는다.
6. 기존 출시 키스토어와 alias를 유지하고 `Builds/MetaHorizonAlpha/ChemicalSafetyVR_Alpha_0.1.0_4.apk`로 빌드한다. 키스토어 비밀번호는 저장소가 아니라 Unity 보안 입력에서 제공한다.
7. 생성 APK에서 서명, `versionCode=4`, Target SDK 34, landscape, install location auto, ARM64, `android.hardware.vr.headtracking`, `com.oculus.intent.category.VR`을 검사한다.
8. Meta Horizon Developer Dashboard의 Alpha 채널에서 **새 빌드 업로드**로 code 4 APK를 올린다. 기존 code 3을 삭제할 필요는 없으며 code 4가 Alpha 현재 빌드가 됐는지 확인한다.
9. Quest 2 라이브러리에서 업데이트를 설치한 뒤 `dumpsys package`로 `versionCode=4`와 installer `com.oculus.ocms`를 확인한다. sideload APK를 정상 Alpha 배포본으로 오해하지 않는다.
10. 새 기준 데이터 시작 전에 code 3 진단 JSONL은 이미 백업됐는지 확인하고 code 4 앱 데이터만 비운다. 앱 데이터 삭제는 사용자 이름 입력과 로컬 JSONL도 함께 지우므로 백업 확인 뒤 한 번만 수행한다.
11. 앱을 `0_App`부터 실행하고 한 작업계획에서 **Test → Training → Education**을 한 판씩 연속 수행한다. 각 판은 해당 모드의 설계 음성을 생략하지 않고 첫 시도 정상 진행하며, 종료 음성·복귀가 끝나 **모드 선택 모달이 다시 보인 순간**을 그 판의 종료로 본다. 세 판 사이에 로비나 앱까지 나갈 필요는 없다.
12. 세 판 뒤 앱을 정상 종료하고 Quest JSONL을 별도 폴더로 pull한다. 서로 다른 `modeSessionId`의 `mode_session_started` 3건과 `mode_session_completed` 3건, mode/workPlan, 퀴즈 수량, Test 결과와 이벤트 순서를 대조한다.
13. 중도 EXIT 미완료 검증은 정상 기준 표본과 섞지 않는다. 기준 JSONL을 먼저 백업한 뒤 별도 회차에서 한 모드를 시작하고 EXIT Point로 복귀해 같은 ID의 `mode_session_completed`가 없는지 확인한다.
14. 현재 Alpha 출시 빌드는 로컬 JSONL을 생성하지만 Quest Player의 서버 자동 업로드는 아직 출시 인증 경계상 활성화하지 않았다. 따라서 JSONL 생성만으로 Express·MySQL·대시보드 반영 완료라고 기록하지 않는다.

#### 보존 및 커밋 제외 대상

- `.baseline-preservation/**`, `Backups/Quest2TelemetryBeforeCode3_2026-09-03/**`, `Backups/Quest2TelemetryCode3LoadingFix_2026-09-03/**`는 로컬 증거 백업이며 Git 커밋에 포함하지 않는다.
- Unity의 `Library`, `Temp`, `Logs`, 사용자별 비밀번호와 토큰은 커밋하지 않는다.

## 2026-09-04 code 4 Quest 실기 피드백과 code 5 후속 계획

### 확인된 배포·실행 기준

- 클라이언트 `main@389a37a27164c988c517b7e3377006668a2da20b`, 서버
  `main@92b3f23bbd9c5f4e4f24f8db531d8279a141784d`를 기준으로 code 4를 만들었다.
- `ChemicalSafetyVR_Alpha_0.1.0_4.apk`는 Unity 빌드 성공, `versionCode=4`, Target SDK 34,
  `install-location=auto`, landscape, ARM64, Meta VR headtracking/category와 APK Signature Scheme v2를
  확인했다. code 3과 인증서 SHA-256이 같으며 APK SHA-256은
  `B52F5B7A394C0F5072F43C1B75400204E476D518CA1750FE90D4B4FCEF659EAB`이다.
- Meta Alpha 채널에서 code 4 업로드 검사가 완료됐고 Quest 2에 `installerPackageName=com.oculus.ocms`,
  `versionCode=4`로 설치된 것을 확인했다.
- Quest code 4 원본 JSONL 3개는
  `C:\Users\user\Documents\Backups\chemical-safety-vr-client\2026-09-04-code4-smoke`에 백업했다.
  전체 파일의 JSON 파싱 오류, sequence 중복과 eventId 중복은 0개다. 가장 긴 세션은 Education·Training·Test와
  두 작업계획의 6개 `mode_session_started`가 각각 같은 ID의 6개 `mode_session_completed`로 닫혔다.
  나머지 두 세션은 모드 시작 없이 0_App부터 PPE Room까지 진입하고 `session_ended`로 끝난 스모크 실행이다.
- Quest logcat에는 해당 실행 시간대의 `FATAL EXCEPTION`과 ANR이 없었다. 프로세스 종료 기록은 각 JSONL의
  `session_ended` 뒤에 발생했다.

### 선생님 화질 피드백과 사용자 수정 요청

다음은 code 4 Quest 2 실기에서 관찰된 사실이다. 아직 코드·씬·Inspector의 근본 원인은 확정하지 않았으며,
이번 기록 단계에서는 런타임·씬·프로젝트 설정을 수정하지 않는다.

1. 전체 화질을 높일 필요가 있고 특히 텍스트가 뭉개져 보인다.
2. 태블릿 문서는 얼굴 가까이 가져와야만 글자가 읽힌다.
3. 진열장 가운데 가로 선반과 오른쪽 기둥 일부에 손이 관통한다.
4. 거울의 플레이어 표시는 괜찮지만 방의 좌우가 기대와 반대로 보인다. 상하는 정상이다.
5. 거울에 EXIT는 보이지만 위치 마커는 보이지 않는다.
6. 복귀 위치에 도착했을 때 퀴즈 모달이 남아 보인다. 요청 동작은 복귀 전에 퀴즈 모달을 닫은 뒤 이동하는 것이다.
7. 최초 Simple 컨트롤러 안내 이후 미니 컨트롤러 구역에서 오른손 A 버튼을 눌러도 상세 컨트롤러 가이드의
   음원과 UI가 재생되지 않는다.
8. 재진입 사용자는 `Welcome_Old` 뒤 Simple 컨트롤러 단계를 생략하고 다음 단계로 진행해야 한다.

### 변경 전 필수 질문 답변

1. **Inspector·씬 작성값 보존:** 태블릿 Canvas/TMP/Material, 진열장 Collider, 거울 Camera/Layer/Culling Mask와
   퀴즈·가이드 참조는 씬 작성값을 기준으로 조사한다. 런타임 코드에서 위치·크기·색·폰트·해상도 값을 새로
   하드코딩하지 않고 `Awake`, `OnEnable`, `Start`, 상태 전환과 생성기에서 덮어쓰는 경로를 함께 확인한다.
2. **단일 기준과 상태 소유자:** 전역 Quest 화질은 Mobile URP/Quality 설정, 태블릿 가독성은 태블릿의 씬 작성
   Canvas·TMP·Material, 충돌은 진열장 Collider, 반사는 거울 카메라와 반사 레이어, 퀴즈·복귀와 컨트롤러
   안내 상태는 `PPEVoiceFlowDirector`·`PPEQuizController`·`PPEFinaleController`, 계정 상태는
   `MetaPlatformIdentityProbe`를 우선 소유자 후보로 둔다. 실제 참조를 확인하기 전에는 소유자를 확정하지 않는다.
3. **입력 전체 경로:** A 상세교육 경로는 `오른손 primaryButton/buttonSouth → Input System/XRI action →
   PPEVoiceFlowDirector의 상태별 입력 판정 → 상세 ControllerRay/Marker/RayT 상태 → 씬 작성 UI와 음원 →
   CardIntro 복귀`로 추적한다. 미니 컨트롤러 UI 자체에 새 Ray 클릭이나 별도 입력 소비자를 추가하지 않는다.
4. **실패 처리:** 누락 Collider·참조·Layer를 런타임에서 자동 생성·자동 선택하지 않는다. 진단 하네스가 정확한
   오브젝트 경로와 누락값으로 실패하게 하고 수리는 명시적 Editor 작업 또는 Inspector 작성값으로 한정한다.
5. **영향 소비자:** Quest 프레임 예산·양안 텍스트, 태블릿 UI, 손·컨트롤러 충돌, 거울 반사와 마커,
   퀴즈 완료·텔레포트 순서, A 입력·가이드 UI/음원, 신규·기존 사용자 진입이 서로 다른 소비자다. 한 패치에서
   한 소비자만 변경하고 각 단계의 회귀 검증 뒤 다음 항목으로 이동한다.
6. **변경 전후 비교:** 변경 전 기준은 Alpha code 4와 백업 JSONL·Quest 관찰이다. 변경 후에는 같은 Quest 2,
   같은 거리·장면·IPD 조건에서 캡처하고, 태블릿 가독 거리, 72Hz 프레임 예산, Collider 관통, 거울 좌우·마커,
   모달 종료 순서와 FirstVisit/Returning 입력 상태표를 항목별로 비교한다.
7. **검증 범위:** 정적 YAML/코드와 C# 빌드, Unity Preview/하네스, Game View/Play Mode, Quest code 5 양안·입력·오디오,
   Alpha 설치와 JSONL을 각각 구분한다. Game View 또는 하네스 PASS만으로 Quest 화질과 물리 A 입력을 완료 처리하지 않는다.

### 결정한 수정 순서

1. **현재 상태 보존:** Unity를 정상 종료한 뒤 새 터미널에서 브랜치와 작업 트리를 다시 확인한다. 현재 클라이언트는
   `followup/2026-09-04-quest-quality-and-ppe-fixes`이며, code 4 빌드 뒤 `ProjectSettings/ProjectSettings.asset`에
   `preloadedAssets` 2건이 미커밋으로 추가돼 있다. 이 값은 사용자 작업으로 보존하고, 생성 원인을 확인하기 전
   커밋하거나 되돌리지 않는다.
2. **화질과 태블릿을 분리 진단:** Mobile Render Scale `0.8`과 MSAA 4x의 code 4 기준 성능을 먼저 기록한다.
   전체 화면이 흐린지 태블릿만 흐린지 분리하고 태블릿 Canvas 픽셀 밀도, TMP 폰트 atlas/material, 스케일과
   런타임 덮어쓰기를 조사한다. 전역 Render Scale 상향은 72Hz GPU 예산을 확인한 뒤에만 후보로 적용한다.
3. **진열장 충돌:** 문제 선반·오른쪽 기둥의 전체 계층과 Renderer Bounds, 기존 Collider Bounds 및 Layer를
   진단한다. 시각 메시를 바꾸지 않고 씬 작성 Collider의 빈 구간만 보완하며 손과 컨트롤러 양쪽을 확인한다.
4. **거울 반사:** 플레이어·방·EXIT·위치 마커의 Layer와 반사 카메라 Culling Mask를 대조한다. 좌우 문제는
   반사 카메라 Transform·projection·출력 UV 중 하나만 변경해 비교하고, 마커는 EXIT와 같은 반사 정책인지
   확인한 뒤 필요한 Layer만 포함한다. 상하와 플레이어 정합은 보존한다.
5. **퀴즈 복귀 순서:** `퀴즈 종료 → 퀴즈 모달 비활성 → 종료 음성/결과 처리 → 암전 → 시작 위치 복귀 →
   페이드인 → 모드 선택 표시 → 완료 이벤트` 상태표와 코드를 맞춘다. 텔레포트 뒤 모달을 숨기는 보정은 하지 않는다.
6. **A 상세 가이드:** 현재 A 입력이 허용되는 FlowState와 미니 컨트롤러 구간의 상태를 로그로 확인한다. 요청된
   구간을 명시적으로 허용하되 중복 실행, Simple 음원과의 동시 재생, 모달·텔레포트 입력 소비는 추가하지 않는다.
7. **재진입 흐름:** 최신 요청 목표는 `Welcome_New → Simple → CardIntro`,
   `Welcome_Old → Simple 생략 → CardIntro`다. Old 판정이 단순 Welcome 청취 이력인지 실제 완료 기록인지 먼저
   확인하고, 기존 문서의 “시나리오 1개 이상 완료” 계약과 충돌하면 사용자 승인 없이 판정 기준을 바꾸지 않는다.
   Returning 상태에서도 미니 컨트롤러 구역의 A 상세교육 재진입 가능 여부를 별도 상태표로 검증한다.
8. **code 5 출시 게이트:** 관련 하네스를 항목별로 확장하고 C# 오류 0, Train/Test·데이터 계약·이동/PPE·Android
   빌드 검증 PASS 뒤 code 5를 만든다. Quest 2에서 양안·72Hz·입력·오디오·두 사용자 경로·두 작업계획을 확인하고
   새 JSONL을 별도 백업한 뒤에만 Alpha code 5와 선생님 재검토본으로 분류한다.

### 다음 터미널 시작점

- 이번 세션에서는 위 계획만 문서화하고 코드·씬·UI·Collider·Layer·Render Scale을 수정하지 않는다.
- Unity가 열려 있으므로 정상 종료한 뒤 다음 터미널을 시작한다. 새 세션은 먼저 `git status -sb`, Unity 프로세스,
  `ProjectSettings.asset`의 `preloadedAssets` diff와 현재 씬을 확인한다.
- 8개 관찰 중 첫 구현 항목은 화질·태블릿이 아니라 **원인 분리를 위한 읽기 전용 기준 조사**다. 조사 결과를
  항목별로 기록한 뒤 한 소비자씩 수정한다.

## 2026-09-04 code 5 읽기 전용 원인 조사와 첫 격리 수정

### 재개 기준과 보존 상태

- 클라이언트는 `followup/2026-09-04-quest-quality-and-ppe-fixes`, `3bcd496`에서 재개했다. 작업 시작 시
  tracked 변경은 없었고 Unity Editor 프로세스도 없었다.
- 서버 미러 저장소는 같은 이름의 브랜치, `b38ab9e`였으며 이 문서의 시작 SHA-256과 줄 수는 클라이언트와
  일치했다. 이번 조사는 클라이언트 런타임 계약만 대상으로 하며 서버 API 완료로 확대하지 않는다.
- `ProjectSettings/ProjectSettings.asset`의 작업 트리와 staged diff는 모두 0이다. 문서의 이전 시작점에 적힌
  `preloadedAssets` 2건은 `9355cb8`에서 추가됐다가 `49be0d3`에서 제거되어 이미 커밋된 이력이다. 현재 파일에는
  기존 1건만 있으므로 보존할 미커밋 PlayerSettings 변경은 없다.
- 이번 변경이 대응하는 사용자 요청은 code 4 Quest 실기에서 확인된 8개 현상의 원인 분리와, 원인이 정적으로
  확정된 거울 위치 마커 누락의 최소 수정이다. 기존 Render Scale, 태블릿 Transform·문서 자산, 진열장 Collider,
  거울 projection·Transform, 퀴즈·가이드 상태 전이와 사용자 판정은 보존한다.

### 항목별 정적 조사 결과

1. **전체 화질:** 현재 `Mobile_RPAsset`은 MSAA 4x와 Render Scale `0.9`다. 이전 계획의 `0.8` 기록은
   `db88050` 이전 값으로 현재 code 4 기준과 다르다. Android OpenXR의 Foveated Rendering과 동적 viewport
   해상도 기능도 꺼져 있다. 따라서 `0.8` 하나를 전체 흐림의 원인으로 확정할 수 없으며, 같은 Quest 위치에서
   GPU frame time·실제 eye buffer와 캡처를 확보하기 전에는 전역 Render Scale을 바꾸지 않는다.
2. **태블릿 문서:** 현재 표시 경로는 `PPE/PPE_C_Tablet/ConfinedSpace`와 `Leak`의 Unlit MeshRenderer이며,
   작업계획별 `1055 × 1491` PNG를 사용한다. `PPETabletChecklistController.ApplyWorkPlan()`은 두 작성 오브젝트의
   활성 상태만 전환하고 Transform·머티리얼을 덮어쓰지 않는다. `201 × 294` `work_confirm.png`를 쓰는 Canvas는
   비활성 레거시 자식이므로 관찰된 문제의 직접 표시 경로가 아니다. 현재 문서는 작은 글자가 이미지에 구워져 있고
   약 태블릿 크기의 월드 면에 표시되므로, 고정 거리 Quest 캡처로 물리 크기·글자 획·샘플링과 전역 해상도를
   분리해야 한다. 원인 확정 전 이미지 확대, TMP 변경 또는 Canvas 활성화는 하지 않는다.
3. **진열장 관통:** `PPE_B_MetalShelving`의 렌더 자식은 33개지만 BoxCollider는 루트와
   `tripo_part_4`, `_5`, `_15`, `_6`의 5개뿐이다. 기존 `PPERoomEnvironmentCollisionSetup`도 세 가로판과
   `_6` 소유의 묶음 기둥만 정상 범위로 검사하고 `_0`, `_2`, `_3`에는 Collider가 없어야 한다고 강제한다.
   따라서 현재 하네스가 PASS해도 선생님이 지목한 가운데 선반·오른쪽 기둥의 실제 Renderer Bounds 빈 구간을
   놓칠 수 있다. Unity에서 해당 메시의 월드 Bounds를 선택 진단한 뒤 기존 Collider만 보완해야 한다.
4. **거울 좌우:** `PlanarMirrorRenderer`는 반사 위치와 off-axis projection을 직접 계산하지만 출력 UV의 명시적
   좌우 반전은 없다. 플레이어 정합은 정상이라는 관찰과 물리 거울의 정상적인 좌우 반전 가능성이 함께 있어,
   정적 코드만으로 결함을 확정할 수 없다. 방의 비대칭 기준물과 원본/거울 캡처를 같은 위치에서 비교하기 전에는
   projection, 카메라 Transform 또는 material UV를 변경하지 않는다.
5. **거울 위치 마커:** 씬의 반사 마스크 `0x40000033`은 EXIT가 속한 Default(0)는 포함하지만 위치 마커가
   속한 Teleport Target(31)은 제외한다. 런타임도 이 직렬화 마스크를 반사 카메라에 다시 적용하므로 원인이
   정적으로 확정됐다.
6. **퀴즈 복귀:** Education/Training의 `quizRoot`는 마지막 답 처리에서 먼저 닫힌다. 남는 것은 Test 결과
   `resultRoot` 경로다. Test의 뒤로 버튼은 `ReturnToModeChoices(true)`를 시작하지만 결과 UI는 텔레포트와
   페이드인이 끝난 뒤 `ResetModeSessionForNextSelection()`에서 닫힌다. 따라서 요청한 “모달 비활성 후 복귀”와
   실제 순서가 반대다. 다음 격리 수정은 Test 결과 UI만 복귀 시작 전에 닫고 통계·완료 이벤트 순서는 보존해야 한다.
7. **미니 가이드 A 입력:** 오른손 A의 `primaryButton/buttonSouth` 액션은 생성·활성화돼 있다. 그러나
   `NotifyControllerEducationRequested()`는 `NameInput` 또는 Simple 컨트롤러 단계에서만 요청을 허용한다.
   `ControllerGuide_mini`는 별도 `ControllerGuideMiniActivator`가 thumbstick으로 표시할 뿐 director 상태를
   전달하지 않으므로 CardIntro/PpeArea에서 A 입력이 상태 가드에 의해 조용히 거부된다. 별도 입력 소비자를
   추가하지 말고 “미니 가이드가 실제 활성인 구간”만 기존 상태 소유자에 명시적으로 연결해야 한다.
8. **재진입 사용자:** `MetaPlatformIdentityProbe`의 Returning은 시나리오 완료가 아니라 앱 범위 Meta ID별
   `Welcome_New` 재생 완료 `PlayerPrefs`다. 시작 때 `m_ControllerEducationCompleted`도 다시 false가 되어
   Returning도 Simple로 진입한다. JSONL에는 `mode_session_completed`와 Meta ID가 기록되지만 이를 사용자
   진입 판정으로 읽는 클라이언트 경로는 없다. 따라서 “완료 시나리오 1개 이상” 계약을 먼저 구현하지 않은 채
   현재 Returning 플래그만으로 Simple을 생략하면 신규 사용자를 오분류하므로 적용하지 않는다.

### 첫 격리 수정: 거울 위치 마커 반사

- `Assets/Scenes/4_PPE_Room.unity`의 기존 `PlanarMirrorRenderer.reflectedLayers`에
  `Teleport Target(31)` 한 비트만 추가했다. 새 오브젝트·FileID·Transform·카메라·projection·RenderTexture와
  EXIT/플레이어 반사 정책은 변경하지 않았다.
- `PPELocomotionPpeRegressionValidationHarness`에 반사 마스크가 프로젝트의 `Teleport Target` 레이어를
  포함하는지 검사하는 회귀 조건을 추가했다.

### 완료한 검증과 남은 게이트

- 정적 diff는 씬 1줄과 하네스 24줄뿐이며 `git diff --check`를 통과했다. 잘못 적용된 대규모 씬 diff는 즉시
  폐기했고, 다시 적용하기 전 작업 파일의 blob SHA가 `HEAD`와 동일함을 확인했다.
- `dotnet build Assembly-CSharp-Editor.csproj`는 오류 0개, 기존 경고 68개로 통과했다.
- Unity 6000.4.8f1 배치 하네스는 Licensing IPC 재연결에서 정지해 실행 본문에 도달하지 못했다. 해당 배치
  Editor와 함께 시작된 CrashHandler만 종료했으며, 기존 Licensing Client는 종료하지 않았다. 그러므로 이번
  결과를 Unity 하네스 PASS로 기록하지 않는다.
- Unity Editor에서 `Tools > PPE > Validate Locomotion PPE Regressions`를 실행하고, Quest 양안에서 EXIT와
  위치 마커가 모두 보이며 플레이어·상하·기존 좌우 결과가 변하지 않았는지 확인해야 이 소비자를 완료 처리한다.
- 위 검증 전에는 퀴즈 복귀, A 입력, Returning 분기나 화질·Collider·거울 좌우 변경을 추가하지 않는다.

## 2026-09-04 code 5 격리 수정 적용 결과

### 적용한 변경

1. **태블릿 문서 샘플링:** 실제 표시 자산 `WorkPlan.png`, `WorkPlan_Leak.png`의 필터를 Trilinear로
   바꾸고 Android에 `CompressedHQ`, 품질 100 override를 작성했다. 원본 `1055 × 1491` 픽셀,
   문구, MeshRenderer, 태블릿 Transform과 런타임 전환 코드는 보존했다. 전체 Mobile Render Scale은
   Quest 72Hz GPU 근거가 없으므로 `0.9`, MSAA는 4x를 유지했다.
2. **진열장 충돌:** Unity Bounds 진단에서 가운데 `tripo_part_7`의 기존 Collider 덮임은 `18.7%`,
   오른쪽 긴 기둥 `tripo_part_3`은 `5.6%`였다. 오른쪽 기둥에는 Mesh Bounds BoxCollider를 추가했다.
   가운데 판의 전체 AABB BoxCollider는 PPE Marker를 막아 검증에서 폐기하고, 시각 Mesh를 그대로 쓰는
   정적 비볼록 MeshCollider로 교체했다. 두 파트만 Layer 2로 바꾸고 PPE·Marker Transform은 보존했다.
3. **거울 위치 마커:** `PlanarMirrorRenderer.reflectedLayers`에 `Teleport Target(31)`만 추가했다.
   사용자 모습까지 반대로 보일 위험이 있는 projection·카메라 Transform·출력 UV 좌우 반전은 변경하지 않았다.
4. **퀴즈 복귀:** `ReturnToModeChoices` 시작 시 `quizRoot`와 `resultRoot`를 먼저 닫고 그 뒤 기존
   암전·복귀·페이드인·모드 선택·완료 기록 순서를 실행한다. 점수와 완료 여부는 초기화 전에 보존한다.
5. **미니 가이드 A:** 씬 작성 `ControllerGuide_mini`를 director 직렬화 참조로 연결했다. 미니 가이드가
   실제 활성이고 상태가 `CardIntro` 또는 `PpeArea`일 때만 기존 오른손 A 입력이 상세
   `ControllerRay → ControllerMarker → ControllerRayT`로 진입한다. 완료 뒤 원래 상태로 음성 재생 없이
   복귀하며 모달·텔레포트·결과 상태의 A 입력 범위는 넓히지 않았다.
6. **재진입 사용자:** Returning 판정을 `Welcome_New` 청취 여부에서 앱 범위 Meta ID별 정상 실기 완료로
   변경했다. `mode_session_completed` 기록 호출 뒤 SHA-256 해시 키 `Tyche.MetaScenarioCompleted.*`를
   저장하며 원시 Meta ID는 PlayerPrefs 키에 쓰지 않는다. 완료 전에는 앱 재시작 후에도 FirstVisit이므로
   `Welcome_New → Simple → CardIntro`, 정상 완료 후에는 `Welcome_Old → CardIntro`가 된다.

### 완료한 검증

- `dotnet build Assembly-CSharp-Editor.csproj`: 오류 0, 기존 경고 68.
- Unity 6000.4.8f1 `PPE Room Collision` PASS: 가운데 MeshCollider, 오른쪽 BoxCollider, 기존 가로판·기둥,
  Wall Hanger/Front Wall 및 입력 레이어 분리 확인.
- Unity `PPE Train/Test Validation` PASS: 퀴즈 UI 선행 종료, 완료 복귀 순서, A 상세교육 재진입과
  원상태 복귀, 신규·복귀 사용자 분기 정적 계약 확인.
- Unity `PPE Locomotion Regression Validation` PASS: 거울 위치 마커 Layer, 미니 가이드 씬 참조,
  태블릿 Android HQ/Trilinear, Render Scale 0.9·MSAA 4x와 기존 이동/PPE 경로 확인.
- Unity `PPE Training Data Contract` PASS: 정상 완료·중도 EXIT 분리와 기존 25개 PPE 데이터 계약 확인.
- Unity `Meta Quest Android Build` PASS: Target SDK 34, Landscape, Automatic 설치 위치와 Android
  Meta Quest OpenXR Support 설정 확인.
- 처음 샌드박스 Unity 실행은 Licensing IPC에서 멈췄으나 권한 있는 배치 실행으로 같은 Editor 버전의
  실제 하네스 본문을 완료했다. 씬 저장 중 생긴 무관한 공백 정규화는 기준 씬 복구 후 Unity가 생성한
  Collider FileID와 의도한 직렬화 변경만 선별 재적용해 제거했다.

### Git 기준

- 클라이언트 구현 기준 커밋은 `27da7e8` (`Quest 실기 피드백 회귀 수정`)이다.
- 서버 미러 시작 기준은 `b38ab9e`이며, 이번 작업에서 서버 API·DB·텔레메트리 코드는 변경하지 않았다.
- 공용 회의록은 클라이언트를 기준본으로 두고 서버의 같은 상대 경로에 동일 내용으로 동기화한다.

### 아직 필요한 수동 출시 게이트

- Quest 2에서 같은 거리로 두 작업계획의 한글 획과 가독 거리를 code 4와 비교하고 72Hz GPU frame time을
  기록해야 한다. 이 근거 전에는 전역 Render Scale을 더 올리지 않는다.
- 손·컨트롤러로 가운데 판과 오른쪽 기둥 관통 차단, 모든 진열 PPE Grab 접근을 확인해야 한다.
- 양안에서 EXIT와 위치 마커가 함께 보이고 사용자·상하 정합이 유지되는지 확인한다. 방 좌우는 비대칭
  기준물의 실제 장면/거울 캡처로 물리 반사와 결함을 구분하기 전까지 수정하지 않는다.
- 오른손 A 상세 UI/음원, FirstVisit/Returning 두 Meta 계정 경로, 퀴즈 모달이 이동 전에 닫히는지를
  Quest 입력·오디오로 확인해야 한다. Android code 5 빌드·설치·새 JSONL 수집은 아직 수행하지 않았다.

## 2026-09-04 code 5 Play Mode 1차 피드백과 후속 결정

### 실제 관찰

1. **전체 화질과 태블릿 문서:** 전체 화질은 이전보다 조금 개선됐다. 밀폐공간 작업계획서는 읽기
   좋아졌지만 누출 작업계획서는 여전히 잘 보이지 않는다. 두 원본은 모두 `1055 × 1491`이지만
   `WorkPlan_Leak.png`는 밀폐공간 문서보다 본문 글자가 작고 얇으며 회색 대비가 낮고, 큰 여백 때문에
   같은 태블릿 면적에서 실제 글자 점유율도 낮다. 따라서 다음 수정은 전역 Render Scale 추가 상향이
   아니라 누출 문서만 밀폐공간 문서 수준의 최소 글자 크기·굵기·대비로 다시 작성하는 방향으로 잡는다.
2. **룸스케일 헤드·손 관통:** 사용자가 조이스틱이나 텔레포트를 사용한 것이 아니라 현실에서 몸을 움직여
   머리를 기울이거나 손을 뻗었을 때, HMD 시점과 추적 손 모델이 가상 벽·문·단스·진열장 표면을 넘어간다.
   벽 너머에는 PPE가 없으며 PPE Hover/Select/Grab 관통이 관찰된 것도 아니다. 앞선 환경 Collider와
   `CharacterController` 보완은 XR Origin의 스틱 이동을 제한하지만, HMD와 손의 로컬 추적 좌표를 직접
   제한하지 않으므로 이 현상의 완료 근거가 아니다. 이 현상은 정상 동작으로 수용하지 않고
   **실제 신체 추적에 의한 헤드·손 환경 관통** 결함으로 별도 진단한다. 카메라나 손 Transform에 Collider를
   바로 추가하면 추적 좌표와 물리가 충돌해 흔들림·멀미·입력 회귀가 생길 수 있으므로 원인 확인 없이 적용하지 않는다.
3. **중간 상세 컨트롤러 가이드 위치:** 조이스틱 클릭 미니 가이드는 XR 카메라 자식이지만, 오른손 A로
   여는 상세 `ControllerGuide`는 루트의 고정 World Space `Window Canvas` 아래에 있다. 따라서 사용자가
   시작 위치에서 이동한 뒤 상세 가이드를 열면 최초 시작 위치에 나타나는 현재 증상이 씬 직렬화 구조와
   일치한다.

### 현재 PPE Room 사용자 작성값 보존

- 사용자는 현재 PPE Room에 새 구조물을 배치했고 일부 벽 위치를 변경했다. 이 씬·Inspector 작성값은
  이후 작업의 기준이며 이전 커밋이나 과거 환경 Collider 좌표로 되돌리지 않는다.
- 관통 진단과 후속 구현은 새 구조물과 이동된 벽의 현재 Renderer/Collider Bounds를 다시 읽어 진행한다.
  기존 Editor 생성 명령을 재실행해 구조물·벽·자식 Transform·Collider를 일괄 재생성하지 않는다.
- 씬의 현재 변경을 사용자 작업으로 보존하며, 대상이 명확한 최소 변경 외에는 checkout·restore·자동 정렬을
  수행하지 않는다.

### 진열장 PPE 접근 불변조건

- 헤드는 벽·문·단스·진열장 고체 표면을 넘어가지 않아야 하지만, 손은 진열장 앞의 열린 공간을 통해
  각 단 위에 배치된 PPE까지 도달하고 기존 방식으로 Grab할 수 있어야 한다.
- 진열장 전체나 각 단의 빈 공간을 넓은 BoxCollider로 막지 않는다. 선반판·기둥처럼 실제 고체인 표면만
  현재 Mesh/Renderer Bounds에 맞춰 처리하고 PPE Marker, Grab Collider와 Interaction Layer는 보존한다.
- 헤드 침범 대응과 손 접촉 표현은 분리하며, 헤드 보호 기능이 손 추적 Pose나 Near/Far Interactor를
  비활성화하거나 PPE 선택 입력을 소비하지 않게 한다.

### 확정한 컨트롤러 가이드 표시 계약

1. 중간에 오른손 A로 상세 가이드를 요청하면 현재 HMD 위치와 수평 시선 방향을 기준으로 눈앞의 후보
   패널 위치를 계산한다. 상세 패널을 계속 머리에 붙여 따라오게 하지 않고, 표시가 시작되는 순간의 안전한
   위치에 한 번 고정한다.
2. 후보 위치의 실제 패널 면적이 벽·문·가구 등 환경 Collider와 겹치는지 표시 전에 검사한다. 겹쳐 있으면
   상세 패널을 표시하지 않고 본 상세 가이드 음성도 시작하지 않는다.
3. 겹침 대기 상태에서는 새 음원
   `Assets/Audio/Voice/4_PPE/4_VO_PPE_EDU_209_MoveBack.mp3`를 사용해 이동 조이스틱을 뒤로 당겨
   가이드가 보이는 위치로 이동하도록 안내한다. 이 음원과 Unity가 생성한 `.meta`는 현재 신규 자산이며,
   기존 `207`, `208` 음원과 같은 AudioImporter 설정을 사용한다.
4. 사용자가 조이스틱으로 이동하는 동안 현재 HMD 기준 후보 위치와 패널 겹침을 다시 평가한다. 처음 계산한
   막힌 월드 위치를 그대로 기다리지 않는다.
5. **패널과 환경 물체의 겹침이 해소된 순간을 상세 가이드 시작 시점으로 확정한다.** 이때 패널을 표시하고
   상세 컨트롤러 가이드의 첫 음성을 처음부터 재생한다. 가림 안내 음성과 상세 본 음성을 동시에 재생하거나,
   가려진 상태에서 상세 단계·입력 판정을 먼저 진행하지 않는다.
6. 미니 가이드도 표시 후보가 환경 물체에 가려지는 경우 같은 `MoveBack` 안내와 겹침 해소 게이트를
   사용한다. 미니 가이드는 상세교육 상태를 시작하지 않으며, 겹침이 해소된 뒤 기존 토글 표시만 완료한다.
7. 거리·높이·패널 크기·충돌 마스크·검사 간격은 런타임 상수로 하드코딩하지 않고 씬 또는 Inspector의
   직렬화 작성값을 기준으로 둔다. 기존 A 입력 소비자, 상세 단계 순서, 미니 가이드 토글, 텔레포트,
   PPE Grab과 음성 생략 규칙은 변경하지 않는다.

### 구현 전 남은 분리 진단과 검증 기준

- 누출 문서는 원본을 다시 작성한 뒤 같은 Quest 2·거리·IPD에서 밀폐공간 문서와 글자 획·가독 거리를
  비교한다. 전역 Render Scale은 72Hz GPU 근거 없이 추가 변경하지 않는다.
- 룸스케일 관통은 조이스틱으로 대상 앞까지 이동한 뒤 이동 입력을 놓고, `HMD 시점`, `왼손 모델`,
  `오른손 모델`을 실제 신체 움직임으로 벽·문·단스·진열장에 천천히 접근시켜 재현한다. 헤드는 표면 내부
  또는 반대편 장면이 보이는지, 손은 모델이 일부 또는 전체 통과하는지를 기록한다. PPE 선택 관통은 관찰된
  증상이 아니지만, 진열장 각 단의 PPE Grab은 보존해야 할 별도 회귀 항목으로 검사한다.
- 헤드는 침범 감지 뒤 시야 페이드·비네트 또는 XR Origin 보정 중 멀미 위험이 낮은 방식을 비교하고,
  손은 환경 접촉 시 렌더 숨김·접촉 표현 또는 시각 손 프록시 제한을 비교한다. 구체 방식은 재현 위치와
  침범 범위를 확인한 뒤 결정하며, 현재 Collider 보완만으로 해결됐다고 처리하지 않는다.
- 상세·미니 가이드 모두 열린 공간, 벽 정면, 문과 단스 근처에서 시험한다. 가림 상태에서는 `MoveBack`만
  들리고, 겹침 해소 뒤 패널 표시와 상세 첫 음성이 한 번 시작되며, 완료 뒤 원래 `CardIntro` 또는
  `PpeArea` 상태로 복귀해야 한다.
- 위 내용은 이번 피드백과 구현 계약을 문서화한 것이며, 이 절을 작성한 시점에는 누출 문서 재작성,
  손 관통 수정과 가이드 재배치·겹침 게이트 코드는 아직 적용하지 않았다.

### 사용자 수동 확인 항목

1. Quest의 안전 경계 안에서 실제 벽이나 가구와 부딪히지 않도록 주변 공간을 비우고, 가능하면 보조자가
   옆에서 확인한다. 큰 걸음을 내딛지 않고 상체와 손만 천천히 움직인다.
2. Codex가 PC에서 Quest 미러링 또는 녹화와 필요한 로그 수집을 먼저 시작한다. 사용자는 영상을 별도로
   촬영·전송하지 않고, 헤드셋 연결과 착용 및 실제 신체 움직임만 담당한다.
3. 사용자는 현재 PPE Room에서 조이스틱으로 가상 벽, 문, 단스, 진열장 앞의 안전한 거리까지 이동한다.
4. 대상 앞에 도착하면 조이스틱 이동 입력을 놓고, 머리만 천천히 앞으로 기울여 HMD 시점이 표면 내부 또는
   반대편으로 넘어가는 오브젝트를 기록한다.
5. 같은 위치에서 왼손과 오른손을 각각 뻗어 손 모델이 표면을 일부 또는 완전히 통과하는지 확인한다.
6. Codex는 미러링 영상에서 `접근 전 → 접촉 → 관통` 구간을 캡처하고 재현 오브젝트의 씬 계층과
   Collider·카메라·손 추적 경로를 대조한다. 캡처 결과는 `헤드`, `왼손`, `오른손`, `컨트롤러 사용 또는
   손 추적 모드`, `재현 오브젝트/위치`로 구분해 기록한다.
7. 재현 위치와 후속 검증은 새 구조물과 이동된 벽을 포함한 현재 씬을 기준으로 기록한다. 과거 벽 좌표와
   비교해 현재 씬을 원상복구하지 않는다.
8. 관통 재현과 별도로 진열장 각 단 위 PPE에 손을 뻗어 Hover/Select/Grab이 유지되는지 확인하고,
   Codex가 각 단별 성공·실패와 손이 막힌 표면을 기록한다.

## 2026-09-04 Codex 업데이트 중단 인수인계

### 중단 원인과 현재 기준

- Codex 앱 업데이트로 작업 창이 종료됐다. 이는 Unity, Quest/OpenXR 또는 PPE 런타임의 충돌 증거가 아니며
  제품 결함과 합쳐 기록하지 않는다.
- 중단 확인 시 클라이언트는 `main@f9f7390c4482cd729b614b9cac21c2a8bf1f6f8b`이고
  `Assets/Scenes/4_PPE_Room.unity`, `ProjectSettings/EditorBuildSettings.asset` 및 본 작업의 문서 2개가
  수정 상태다. 씬에는 사용자가 새로 배치한 구조물과 이동한 벽이 포함되므로 모두 보존한다.
- 서버는 `main@de4928510cf5dd97d65a7fe17ffb76bb4a682acc`이다. 공용 회의록 외에
  `public/site/starlight-sudoku-landing`과 `test/spark-site.test.js`의 별도 변경이 있으므로 이 작업에서
  수정·정리·커밋하지 않는다.
- 확인 시 Unity 6000.4.8f1 관련 `Unity.exe` 프로세스 3개가 실행 중이었다. 업데이트 중단을 이유로 강제
  종료하지 않으며, 재개 후 실제 Editor와 보조 프로세스 및 현재 열린 씬·`Scene.isDirty`를 먼저 확인한다.
- Codex 앱 업데이트가 완료되고 작업 창이 안정적으로 다시 열린 뒤에만 진단을 재개한다. 현재 단계에서는
  룸스케일 관통 수정, 가이드 재배치, 누출 문서 재작성과 code 5 출시 검증을 완료로 처리하지 않는다.

### 재개 시 보존 게이트

1. 클라이언트와 서버에서 `git status -sb` 및 현재 커밋을 다시 확인한다.
2. `4_PPE_Room.unity`의 현재 계층, 새 구조물, 이동된 벽 Transform, 열린 씬과 저장 여부를 확인한다.
3. 사용자 씬과 `EditorBuildSettings.asset`을 checkout·restore·reset하지 않는다. 기존 환경 생성기나 자동 정렬
   메뉴도 현재 구조를 대조하기 전에는 실행하지 않는다.
4. 서버의 별도 웹사이트·테스트 변경을 공용 문서 미러링과 함께 커밋하지 않는다.

### 후속 작업 우선순위

1. **미러링 확보:** Codex가 PC에서 Quest 미러링 또는 녹화와 로그 수집을 먼저 준비한다. 화면을 확보하지
   못하면 사용자에게 관통 재현을 시작하도록 요청하지 않는다.
2. **현재 구조 기준 재현:** 사용자는 조이스틱으로 새 구조물과 이동된 벽을 포함한 대상 앞까지 이동하고,
   이동 입력을 놓은 뒤 실제 머리와 양손을 천천히 움직인다. Codex가 헤드·왼손·오른손 관통 위치를 캡처한다.
3. **진열장 접근 회귀:** 진열장 각 단 위 PPE의 Hover/Select/Grab을 별도로 확인한다. 헤드 침범을 막더라도
   손이 열린 단 내부의 PPE까지 도달하는 기존 동작은 유지해야 한다.
4. **읽기 전용 원인 조사:** 현재 씬의 HMD Camera, 손 추적 Transform, XR Origin/CharacterController,
   환경 Renderer/Collider Bounds와 런타임 할당 경로를 대조한다. 조사 전에 카메라·손에 Collider나
   Rigidbody를 추가하지 않는다.
5. **해결안 결정:** 헤드는 침범 감지 뒤 페이드·비네트 또는 안전한 Origin 보정을 비교하고, 손은 PPE 접근을
   유지하는 접촉 표현·렌더 제한·시각 프록시 후보를 비교한다. 진열장 전체를 넓은 Collider로 막지 않는다.
6. **구현·검증:** 승인된 소비자 하나만 수정하고 Unity 정적/Editor 검증과 Quest 양안·실제 신체 이동을
   구분해 확인한다. 헤드·양손 관통 차단과 각 단 PPE Grab이 함께 성공해야 완료 처리한다.
7. **기존 미완료 후속:** 위 결함을 분리한 뒤 누출 작업계획서 가독성 개선, 상세·미니 가이드 HMD 기준 배치와
   `MoveBack` 겹침 게이트, code 5 APK 빌드·설치·72Hz·입력·오디오·JSONL·서버 적재 검증을 순서대로 재개한다.

### 커밋 상태

- 본 인수인계 작성 시점에는 클라이언트와 서버 모두 커밋·푸시하지 않았다.
- 공용 회의록은 양쪽 저장소에 같은 내용으로 미러링하되, 사용자 씬과 서버의 별도 변경은 각각 소유자의
  작업으로 분리해 보존한다.

## 2026-09-07 code 5 Alpha 당일 제출 단일 실행 게이트

### 현재 체크포인트

- 클라이언트 기준은 `main@428c0d5bd458719b53c11ae95899f250c4d7bc4e`, 서버 기준은
  `main@9a7da209ec166b7fd5c646c04b4bcdfff3aadee8`이다.
- code 5 Release APK `Builds/MetaHorizonAlpha/ChemicalSafetyVR_Alpha_0_1_0_5.apk`는 빌드, v2 서명,
  `versionCode=5`, Target SDK 34, landscape, install location auto, ARM64, Meta VR headtracking/category,
  `DEBUGGABLE` 비활성과 cleartext 차단을 확인했다.
- code 5 Development APK, `tyche_training_baseline` DB 연결·migration, 6개 조합 실플레이, 원본·DB·백업
  대조 및 Meta Alpha 배포본 최종 실기는 아직 완료되지 않았다.
- 2026-09-07 현재 `tyche_training_baseline` 전용 로컬 계정과 DB를 만들고 서버 현재 HEAD의 migration 16개를
  적용했다. 초기 수집량은 session 0건, event 0건이며 MariaDB TCP 3306을 확인했다.
- 기준 DB를 사용하는 Express를 시작했고 `/api/health`, `/telemetry-ingest-test/`, Bearer 인증
  `/api/training-telemetry/sessions?limit=1`이 모두 HTTP 200임을 확인했다.
- DB 비밀번호와 업로드 token은 Git에서 제외된 서버 로컬 `.env`에만 유지한다. 기존 root와 일반 앱 계정,
  기존 `tyche_training` DB는 변경하거나 초기화하지 않았다.
- 따라서 1·2단계는 준비됐고 현재 단일 다음 작업은 3단계 code 5 Development APK를 **Build만** 생성하는
  것이다. 설치, LAN 주입, 앱 실행과 실플레이는 APK 생성 후 Codex의 검사를 거쳐 진행한다.
- Release Player는 개발용 LAN 업로드를 활성화하지 않는다. Release APK의 JSONL 생성과 Development APK의
  Express·MySQL 전송 검증을 한 결과로 합치지 않는다.
- 집 환경에서 반복되는 HMD 주변 시야 깨짐은 제출 차단 결함이다. 룸스케일 헤드·손 관통 Known Issue와
  합치지 않으며, 미러링·logcat·기기 성능 수집을 시작하기 전에는 기준 실플레이를 요청하지 않는다.
- 기준 DB 확인 전 Development 앱을 실행하지 않는다. 일반 `tyche_training` DB로 받은 실행은 기준본으로
  승인하지 않는다.
- 당일 상태 확인은 `node Tools/MetaAlphaSubmissionGateHarness.mjs`를 단일 진입점으로 사용한다. 하네스
  `WAIT`는 실패가 아니라 출력된 첫 미완료 게이트부터 진행하라는 뜻이다.

### 1단계 — code 5 정적·Release 아티팩트 고정

1. **Codex:** Git 기준과 미커밋 diff를 확인하고 사용자 씬·Inspector 값을 보존한다.
2. **Codex:** Scene Dependency, Train/Test, Training Data Contract, Locomotion/PPE, Room Collision,
   Meta Quest Android, Meta Quest 72 Hz 하네스 결과와 Unity 컴파일 오류를 구분해 확인한다.
3. **Codex:** Release APK의 서명·Manifest·ABI·버전과 SHA-256을 검사한다.
4. **사용자:** Meta Dashboard Alpha 채널에 정확한 code 5 Release APK 업로드를 시작한다. 업로드 처리는
   2~6단계와 병행할 수 있지만, 채널 설치와 최종 제출 완료 판정은 7~8단계에서만 한다.

### 2단계 — 기준 DB 격리 준비

1. **Codex:** 서버를 시작하기 전에 `.env`의 `DB_NAME=tyche_training_baseline`, 전용 DB 사용자·비밀번호,
   `ENABLE_TRAINING_TELEMETRY_INGEST=true`와 업로드 token 존재를 비밀값 출력 없이 확인한다.
2. **Codex:** `tyche_training_baseline` 존재, 서버 현재 SHA의 `db/migrations/*.sql` 전체 적용,
   telemetry 세션·이벤트 초기 건수와 자동 purge 미적용을 읽기 전용으로 확인한다.
3. **Codex:** 기준 DB를 향하도록 Express를 시작하고 PC loopback·Quest 사설 LAN health, 인증된 텔레메트리
   조회와 `/telemetry-ingest-test/` 응답을 확인한다.
4. 이 단계가 PASS하기 전에는 Development APK를 Quest에서 실행하거나 기준 플레이를 시작하지 않는다.

### 3단계 — Development APK 빌드·설치·일회성 LAN 주입

1. **사용자:** Unity Android Build Profile에서 `Development Build`를 켜고 **Build만** 실행한다.
   `Build And Run`은 사용하지 않는다. 출력은
   `Builds/MetaHorizonAlpha/ChemicalSafetyVR_TelemetryDev_0_1_0_5.apk`로 고정한다.
2. **Codex:** APK의 package identifier가 Release와 같고 `versionCode=5`, `DEBUGGABLE` 활성,
   Development 전용 cleartext 설정인지 확인한 뒤 ADB로 설치한다.
3. **Codex:** 설치된 앱을 중지한 상태에서
   `Tools > PPE > Inject Quest Development LAN Configuration`과 같은 경로로 사설 LAN 주소와 token을
   한 번만 주입한다. 주소·token 원문은 로그·문서·Git에 남기지 않는다.
4. **Codex:** 주입 성공과 앱 중지 상태를 확인한 뒤에만 사용자에게 첫 플레이 시작을 요청한다.
5. 집 환경 검증은 Quest Link Play Mode가 아니라 Quest에 설치한 Development APK의 **독립 실행**으로 한다.
   USB는 ADB 로그·설정 주입에만 사용하고 Quest Link를 시작하지 않는다.

### 4단계 — 첫 기준 회차 게이트

1. **Codex·사용자:** 문서화된 약 `2:41` 이후 시야 깨짐보다 긴 **5분 독립 실행 안정성 스모크**를 먼저 한다.
   `0_App → PPE Room`까지만 진입하고 모드는 시작하지 않는다. Codex는 Quest 미러링 또는 녹화, ADB logcat과
   필요한 성능 관찰을 먼저 시작한다.
2. HMD 주변 시야 깨짐, 검은 화면 또는 정지감이 생기면 기준 수집을 시작하지 않는다. 스모크 JSONL과 로그는
   비기준 진단 증거로 보존하고 원인을 좁힌다. 안정하면 정상 종료하고 JSONL을 분리한 뒤 Development LAN
   일회성 설정을 다시 주입한다.
3. **사용자:** 테스트 Meta 계정과 같은 Quest 2에서 `0_App`부터 시작해 **ConfinedSpace/Test 한 판만**
   진행한다. 음성 생략, pause, HMD 이탈, 강제 종료, 네트워크 단절과 잘못된 PPE·퀴즈 선택 없이 첫 시도
   정상 완료하고 모드 선택 모달이 다시 보일 때 멈춘다.
4. **Codex:** 같은 `sessionId`와 `modeSessionId`로 Quest JSONL, 서버
   `training_telemetry_sessions/events`, event count, 마지막 sequence, `mode_session_started`,
   문제별 `quiz_answer_resolved`, `mode_session_completed`와 서버 checkpoint를 대조한다.
5. 누락·중복·ID 불일치·미완료가 하나라도 있으면 해당 회차는 비기준 후보로 보존하고 다음 조합으로 가지 않는다.

### 5단계 — 6개 조합 실플레이 기준본 수집

첫 회차 PASS 뒤 같은 앱 버전·HMD·조작자·네트워크와 정상 진행 조건을 유지해 아래 순서로 조합별 1회씩
수집한다.

`ConfinedSpace/Test → ConfinedSpace/Training → ConfinedSpace/Education → LeakResponse/Test → LeakResponse/Training → LeakResponse/Education`

- **사용자:** HMD와 앱 상태가 안정하면 6개 조합을 모두 진행한다. Codex가 직전 조합의 DB 대조 PASS를
  알린 뒤 다음 조합으로 넘어간다.
- **Codex:** 각 조합 직후 mode/workPlan, 앱 버전, `sessionId`, 고유 `modeSessionId`, 시작·완료 시각,
  퀴즈 문항·정답 수, PPE 오선택 수, `modeElapsedSec`, event count와 마지막 sequence를 확인한다.
- FirstVisit은 첫 정상 완료 전 `Welcome_New → Simple → CardIntro`, Returning은 정상 완료 후 앱 재실행에서
  `Welcome_Old → CardIntro`임을 별도 확인한다. 이 계정 경로 증거를 6개 모드 완료와 섞어 추정하지 않는다.
- 중도 EXIT, pause/resume, 오류 재현은 6개 정상 기준본과 같은 회차에 넣지 않는다.

#### HMD 불안정 시 이어서 수집하는 규칙

1. 시야 깨짐·검은 화면·정지감이 나타나면 현재 진행 중인 모드를 즉시 중단하고 다음 조합을 시작하지 않는다.
2. 증상 전에 `mode_session_completed`가 기록되고 Quest JSONL·DB 대조까지 PASS한 조합은 승인 후보로 보존한다.
   완료된 기준 회차를 처음부터 다시 수행하지 않는다.
3. 증상 발생 중 진행 중이던 조합과 완료·대조가 끝나지 않은 조합만 비기준 후보로 분리한다. 원본 이벤트는
   삭제하지 않는다.
4. 앱을 정상화한 뒤 독립 실행 안정성 스모크, Development LAN 재주입과 서버 checkpoint 확인을 다시 통과하면
   **아직 승인되지 않은 다음 조합부터** 수집을 재개한다.
5. 독립 실행 Development 또는 Meta Alpha Release에서도 시야 깨짐이 반복되면 데이터 수집을 강행하지 않고
   제출 차단 결함으로 처리한다. Quest Link에서만 재현된 결과를 독립 실행 APK 실패로 합치지 않는다.

### 6단계 — 원본·DB·백업 승인

1. **Codex:** 6개 `mode_session_started`와 같은 ID의 6개 `mode_session_completed`, sequence·eventId
   누락/중복 0, Quest JSONL과 DB event count·마지막 sequence 일치를 확인한다.
2. **Codex:** Quest 원본 JSONL, 서버 상세 조회 내보내기, 기준 DB 백업을 서로 다른 보관 단위로 저장하고
   SHA-256과 비식별 보존 목록을 기록한다. Meta ID·token 원문은 Git과 제출 문서에 넣지 않는다.
3. 위 조건을 모두 만족한 조합만 승인 기준본으로 표시한다. 비기준 후보를 삭제하거나 승인 데이터에 합치지 않는다.

### 7단계 — Meta Alpha Release 재설치·최종 실기

1. **사용자:** Quest Meta 라이브러리에서 Alpha code 5를 설치한다. Development sideload를 계속 실행하지 않는다.
2. **Codex:** `versionCode=5`, installer `com.oculus.ocms`, `DEBUGGABLE` 비활성, 개발 LAN 비밀값 미포함을
   확인한다.
3. **사용자:** Release 앱에서 `0_App → PPE Room` 진입, 양안, 72 Hz, 입력·오디오, 두 작업계획, 상세 A 가이드,
   퀴즈 모달 선행 종료, 위치 마커와 핵심 PPE Grab을 확인한다. HMD 주변 시야 깨짐·검은 화면·정지감은
   0건이어야 한다.
4. **Codex:** logcat의 FATAL EXCEPTION·ANR과 새 Release JSONL을 확인한다. Release에서 로컬 LAN 서버 업로드가
   없다는 사실을 전송 실패로 판정하지 않는다.

### 8단계 — Alpha 제출 완료 판정

다음 네 조건을 모두 만족할 때만 오늘 Alpha 제출 준비 완료로 판정한다.

1. Meta Alpha 채널에 code 5가 할당되고 Quest 설치본의 installer가 `com.oculus.ocms`다.
2. Development code 5의 6개 정상 기준본이 Quest JSONL·기준 DB·백업과 일치한다.
3. Release code 5의 양안·입력·오디오·핵심 흐름에서 제출 차단 결함이 없다.
4. 정적, Editor, Development 전송, 기준 데이터, Release 실기 증거를 공용 문서와 Git 상태에 구분해 기록했다.

## 2026-09-07 학원 PC·Workbench 환경 인수인계

### 인수인계 목적과 기준본

- 이 절은 집 PC에서 준비한 code 5 Alpha 데이터 수집 환경을 학원 PC에서 이어갈 때 Codex가 비밀번호 복구나 DB 초기화를 추측으로 반복하지 않도록 하는 실행 기준이다.
- 문서 기준본은 클라이언트 저장소의 이 파일이며, 서버 저장소의 같은 상대 경로 파일은 완전히 동일하게 미러링한다.
- Git은 서버 코드, `db/migrations/*.sql`, 검증 하네스와 이 문서만 전달한다. 각 PC의 `.env`, Workbench 연결 프로필, MariaDB 계정, DB 내용, 업로드 토큰과 Quest JSONL은 Git으로 전달되지 않는다.
- 학원 PC에 이미 유효한 서버 `.env`와 Workbench 연결이 있으면 그것을 우선 보존한다. 집 PC에서 만든 `tyche_baseline` 계정이나 비밀번호를 학원 PC에 복제할 필요가 없다.
- 비밀번호·토큰 원문을 문서, 콘솔 출력, 로그, 커밋 또는 채팅에 기록하지 않는다.

### 학원 Codex가 먼저 확인할 순서

1. 클라이언트에서 `node Tools/AgentHandoffHarness.mjs`를 실행하고 `Docs/ValidationHarnessGuide.md`를 읽는다.
2. 클라이언트와 서버 저장소에서 `git status -sb`, 현재 브랜치와 HEAD SHA를 각각 기록한다. 기존 미커밋 파일과 학원 로컬 `.env`를 덮어쓰거나 정리하지 않는다.
3. 두 저장소를 사용자가 지정한 기준 브랜치로 갱신한 뒤, 서버 `.env`가 Git에 추적되지 않는 로컬 파일임을 확인한다.
4. Workbench에서 기존 연결이 성공하면 그 연결을 관리자 암호 분실 문제로 재분류하지 않는다. 먼저 `.env`의 `DB_HOST`, `DB_PORT`, `DB_USER`, `DB_NAME`이 학원 MariaDB 인스턴스를 가리키는지 값은 노출하지 않고 이름별로 확인한다.
5. Alpha 기준본을 새로 수집할 때는 일반 개발 DB와 섞지 않고 `DB_NAME=tyche_training_baseline`을 사용한다. 동일 이름 DB가 이미 있으면 자동 삭제·초기화하지 않고 session/event 수와 기존 백업 여부부터 확인한다.
6. `ENABLE_TRAINING_TELEMETRY_INGEST=true`와 `TRAINING_TELEMETRY_UPLOAD_TOKEN` 존재를 확인한다. 로컬 기준 수집에 불필요하고 설정이 완성되지 않은 기능은 켜지 않는다. 현재 기준은 `ENABLE_CONTACT_FORM=false`, `ENABLE_LOCAL_TELEMETRY_READ=false`이다.
7. 서버 저장소에서 `npm run db:migrate`를 실행한다. 새 DB면 현재 `db/migrations/*.sql` 전체가 적용되어야 하고, 기존 DB면 적용 목록이 현재 서버 HEAD와 정확히 일치해야 한다.
8. 기준 DB를 사용하는 Express 서버를 시작한 뒤 `/api/health`, `/telemetry-ingest-test/`, Bearer 인증을 포함한 `/api/training-telemetry/sessions?limit=1`이 모두 HTTP 200인지 확인한다.
9. 클라이언트에서 `node Tools/MetaAlphaSubmissionGateHarness.mjs`를 실행한다. `WAIT`이면 출력된 첫 대기 항목만 해결하고, `NEXT`가 지시하는 단계보다 앞서 APK를 실행하거나 실플레이를 시작하지 않는다.

### 환경별 값의 소유권

| 항목 | Git으로 전달 | 학원 PC에서 사용할 기준 |
| --- | --- | --- |
| 서버 소스·migration | 예 | pull한 서버 HEAD |
| `.env` | 아니요 | 학원 PC의 기존 로컬 파일 보존 |
| `DB_USER`·`DB_PASSWORD` | 아니요 | 학원 MariaDB에 실제 존재하는 전용 계정 |
| `tyche_training_baseline` 내용 | 아니요 | 학원에서 새로 만들거나 승인된 백업만 복원 |
| `TRAINING_TELEMETRY_UPLOAD_TOKEN` | 아니요 | 서버 `.env`와 Unity 실행 사용자 환경의 `TYCHE_TELEMETRY_UPLOAD_TOKEN`이 동일해야 함 |
| Workbench 연결·저장 암호 | 아니요 | 학원 Windows 사용자 프로필의 기존 연결 |
| Quest JSONL | 아니요 | ADB로 별도 회수하고 해시와 회차 ID로 관리 |
| Release·Development APK | 기본적으로 아니요 | 문서의 정확한 경로와 code 5 속성을 다시 검증 |

### 연결 실패 시 분기

1. **Workbench 연결 성공 + 서버 연결 성공:** 계정 생성, 암호 변경, grant 복구를 하지 않는다. migration과 HTTP 게이트로 바로 진행한다.
2. **`Access denied`:** 먼저 `.env`의 host/user가 Workbench 연결 대상과 같은지 확인한다. 기존 root나 앱 계정 암호를 변경하지 않는다. 학원에서 권한을 가진 관리자 연결이 확인된 경우에만 `tyche_training_baseline` 전용 계정을 만들거나 권한을 부여한다.
3. **`Unknown database`:** 기존 일반 DB를 이름 변경하거나 삭제하지 않는다. `tyche_training_baseline`만 새로 만들고 `npm run db:migrate`를 실행한다.
4. **migration 불일치:** 테이블을 수동 수정하거나 DB를 purge하지 않는다. 서버 HEAD와 `schema_migrations` 차이를 기록하고 정상 migration 명령으로만 보완한다.
5. **TCP 3306 실패:** `.env`를 바꾸기 전에 학원 MariaDB 서비스, 실제 포트와 Workbench 연결 인스턴스를 확인한다.
6. **TCP 3000 또는 health 실패:** DB 암호를 다시 만들지 않는다. 서버 프로세스의 stderr와 `.env`의 선택 기능 설정 오류를 먼저 확인한다.
7. **HTTP 401:** DB 문제가 아니라 토큰 불일치로 분류한다. 서버의 `TRAINING_TELEMETRY_UPLOAD_TOKEN`과 Unity 사용자의 `TYCHE_TELEMETRY_UPLOAD_TOKEN`을 원문 출력 없이 맞춘 뒤 Development LAN 설정을 다시 한 번 주입한다.
8. **HTTP 200이지만 DB event가 증가하지 않음:** Quest의 실행 APK가 Development code 5인지, 일회성 LAN 주입이 재설치 뒤에도 유효한지, `modeSessionId`와 업로드 응답을 순서대로 확인한다. 기존 DB를 초기화하지 않는다.

### 집 PC 데이터와 학원 PC 데이터의 연속성

- 집에서 수집한 DB와 JSONL은 Git pull만으로 학원에 나타나지 않는다.
- 집의 완료 회차를 학원에서 이어서 사용할 때는 먼저 Quest JSONL, 기준 DB 백업, session/event 조회 결과와 SHA-256 목록을 만든다. 그 승인된 백업만 학원 기준 DB에 복원한다.
- 학원에서 빈 DB로 새 수집을 시작하면 집의 기준본과 별개의 수집 세트로 기록한다. 같은 `modeSessionId`인지 확인하지 않은 채 두 DB의 건수만 합치지 않는다.
- HMD 이상 전에 `mode_session_completed`와 JSONL·DB 대조가 PASS한 회차는 유효하다. PC를 옮겼다는 이유로 완료 회차를 자동 재실행하지 않는다.

### 학원 환경 준비 완료 판정

다음 조건을 모두 만족해야 학원 Codex가 Development APK 설치·LAN 주입 단계로 진행한다.

1. 학원 `.env`를 보존한 상태에서 `tyche_training_baseline` 연결이 성공한다.
2. 서버 HEAD의 migration 전체가 적용되어 있다.
3. 기준 DB의 시작 session/event 수를 기록했다.
4. health, 테스트 페이지, 인증 sessions 조회가 모두 HTTP 200이다.
5. `node Tools/MetaAlphaSubmissionGateHarness.mjs`가 DB·서버 관련 대기 항목 없이 Development APK 또는 그 이후 단계만 `NEXT`로 지시한다.
6. 이 조건이 충족되지 않으면 비밀번호 초기화, 기존 DB 삭제, 실플레이 시작으로 우회하지 않는다.

## 2026-09-08 다른 PC 아침 재개 체크포인트

### 중단 시점의 확정 상태

- Git 기준은 클라이언트 `main@5a408edb6eb11ab286cfc982d37e634ccdd4d65b`, 서버
  `main@2aa295de135d84af8ab12f8973e334729d4de45a`이며 두 커밋 모두 `origin/main`에 푸시됐다.
- Release code 5 APK는 앞 절의 서명·Manifest·ABI 검사를 통과한 제출 후보로 그대로 보존한다.
- Development APK는
  `Builds/MetaHorizonAlpha/ChemicalSafetyVR_TelemetryDev_0_1_0_5.apk`이며 크기 277,675,159 bytes,
  SHA-256 `B4149F82B75BD41DFD67598C45EFE5D6B15C205851E07008CD20B0F4E15313E1`이다.
- Development APK는 Quest 2에 ADB sideload로 설치됐다. 설치본은 package
  `com.tycheworks.immersa.safetyvr`, `versionCode=5`, `versionName=0.1.0`, ARM64, Target SDK 34,
  APK Signing v2, `DEBUGGABLE`, installer `com.android.shell`이다.
- 위 설치 상태는 집 Quest 2에만 해당한다. 아침 검증은 **다른 Quest 기기**에서 수행할 예정이므로 집 기기의
  설치·미실행 상태와 내부 LAN 설정이 전달됐다고 가정하지 않는다.
- 2026-09-08 중단 직전 `dumpsys package`의 사용자 상태는 `stopped=true`, `notLaunched=true`였다.
  사용자의 확인과 함께 앱·HMD 안정성 검사는 아직 시작하지 않은 것으로 기록한다.
- 집 PC의 Development LAN 설정은 중지된 앱 내부에 일회성 파일로 주입됐지만 집 사설 LAN 주소를 향한다.
  다른 PC·학원에서는 이 값을 재사용하지 않고 그 PC의 사설 LAN 주소와 기존 로컬 token으로 다시 주입한다.
- 집 PC 기준 DB는 migration 16개, 초기 session 0건/event 0건이고 health, 테스트 페이지, 인증 sessions 조회가
  모두 HTTP 200이었다. 이 DB·`.env`·실행 프로세스는 Git으로 다른 PC에 전달되지 않는다.
- 실제 HMD 영상은 수집되지 않았다. 화면 비활성 상태의 `screenrecord` 시도는 Quest에 0 byte 파일만 만들었고
  기준 증거가 아니다. `.baseline-preservation`의 ADB 로그와 진단 파일은 로컬·Git 제외 상태이며 다른 PC에서
  자동으로 이어지지 않는다.

### 다른 PC에서 재개하는 순서

1. 클라이언트와 서버의 `main`을 pull하고 위 기준 SHA가 포함됐는지 확인한다. 각 PC의 기존 `.env`, Workbench
   연결과 사용자 미커밋 파일은 보존한다.
2. 클라이언트에서 `node Tools/AgentHandoffHarness.mjs`를 실행하고 `Docs/ValidationHarnessGuide.md`와 이 절을
   읽는다. 서버에서는 학원 `.env`의 대상 DB가 `tyche_training_baseline`인지 확인한다.
3. 서버에서 `npm run db:migrate`를 실행하고 기준 DB의 시작 session/event 수를 기록한다. Express를 시작해
   health, `/telemetry-ingest-test/`, 인증 sessions 조회 HTTP 200을 확인한다.
4. Development APK는 Git에 포함되지 않는다. 다른 PC에 위 SHA-256과 일치하는 APK를 별도로 옮기거나,
   클라이언트 기준 커밋에서 Unity `Development Build`를 켜고 같은 파일명으로 **Build만** 다시 생성한다.
5. 다른 Quest를 USB로 연결하고 개발자 모드·USB 디버깅 승인을 확인한다. Development APK를 새로 빌드했다면
   package `com.tycheworks.immersa.safetyvr`, `versionCode=5`, `DEBUGGABLE`, ARM64와 v2 서명을 검사하고 새
   SHA-256을 기록한 뒤 ADB로 설치한다. 설치 후 `dumpsys package`로 실제 기기 상태를 다시 확인하며 Quest Link는
   시작하지 않는다.
6. 앱을 중지한 상태에서 다른 PC의 `TYCHE_QUEST_LAN_SERVER_BASE_URL`과
   `TYCHE_TELEMETRY_UPLOAD_TOKEN`으로 일회성 Development LAN 설정을 다시 주입한다. 주소·token 원문은
   로그·문서·Git에 남기지 않는다.
7. 먼저 화면 녹화 없이 ADB logcat만 수집한다. HMD 화면 녹화는 성능·재현 조건에 영향을 줄 수 있으므로
   사용자가 명시적으로 선택하거나 결함 재현 후 증거가 필요할 때 별도 비교 실행으로 수행한다.
8. Quest 앱 라이브러리의 **알 수 없는 출처(Unknown Sources)**에서 Development
   `Chemical Safety Training VR`을 실행한다. Meta Alpha Release 설치본과 혼동하지 않는다.
9. 같은 앱 실행을 유지한 채 `PPE Room 진입 위치에서 5분 → PPE 진열장 앞으로 이동 → 진열장 위치에서 5분`
   순서로 검사한다. 두 구간 모두 PPE 선택, 작업계획 선택과 모드 시작은 하지 않고 머리·손 움직임 조건을
   가능한 한 같게 유지한다. 중간에 앱, HMD 또는 세션을 끄거나 재시작하지 않는다.
10. 시야 깨짐이 발생하면 즉시 진행을 멈추고 `입구/진열장`, 해당 구간 경과시간, 왼눈/오른눈/양안,
    직전 이동·고개 방향을 기록한다. 첫 구간에서 발생하면 진열장으로 이동하지 않는다.
11. 두 구간이 모두 정상일 때만 비기준 스모크를 정상 종료하고 JSONL을 기준 데이터와 분리한다. Development
    LAN 설정을 다시 주입한 뒤 4단계의 `ConfinedSpace/Test` 첫 기준 회차 하나로 진행한다.
12. 첫 회차의 Quest JSONL·기준 DB 대조가 PASS하기 전에는 나머지 5개 조합을 시작하지 않는다.

### 역할과 다음 보고 형식

- **사용자:** 동일 세션의 입구 5분과 진열장 5분을 수행하고 `입구 정상/실패`, `진열장 정상/실패`, 실패 시
  경과시간과 눈을 보고한다.
- **Codex:** 실행 전 APK·DB·LAN·로그 준비를 확인하고, 사용자 보고 직후 logcat·JSONL·DB를 대조해 다음 한 단계만
  지시한다. 사용자가 실행하지 않았다고 말한 단계를 완료로 추정하지 않는다.

## 2026-09-08 Quest 앱 라이브러리 미표시 후속

### 확인된 상태

- Render Scale 1.0과 파트너 로고 mip bias -0.5를 반영한 Development APK를 Quest 2에 설치했다.
- APK Manifest에는 `android.hardware.vr.headtracking`, `android.intent.action.MAIN`,
  `android.intent.category.LAUNCHER`, `com.oculus.intent.category.VR`이 모두 포함되어 있다.
- Quest의 현재 사용자 0에서 package `com.tycheworks.immersa.safetyvr`는 `installed=true`,
  `hidden=false`, `suspended=false`, `enabled=0`으로 조회됐고 `UnityPlayerGameActivity`도 MAIN/VR
  실행 대상으로 정상 조회됐다.
- ADB 직접 실행에서는 앱이 기동됐지만 사용자가 Quest 앱 라이브러리의 `알 수 없는 출처`에서 앱 타일을
  찾지 못했다. 설치 성공이나 직접 실행 성공을 라이브러리 노출 성공으로 합쳐 쓰지 않는다.

### 시도했지만 해결되지 않은 항목

1. 동일 APK를 `adb install -r`로 갱신하고 앱을 한 번 실행한 뒤 Quest 홈으로 복귀했다.
2. 앱 데이터는 보존한 채 `cmd package uninstall -k` 후 다시 설치해 신규 package 등록을 발생시켰다.
3. `com.oculus.vrshell`만 강제 종료하고 HOME intent로 재기동했다. 새 런처 PID와 앱 package 경로는
   확인됐지만 사용자는 앱 타일이 계속 보이지 않는다고 보고했다.
4. USB 연결이 한 차례 끊겼으나 ADB 서버 재시작 뒤 Quest 2가 `device` 상태로 복구됐다. 같은 시점의
   전면 Activity는 `com.oculus.vrshell/.HomeActivity`였으므로 Quest Link 화면 안에서 PC 라이브러리를
   보고 있던 상태로 확정하지 않는다.

### 중단 상태와 다음 작업

- 사용자 요청에 따라 Quest 전체 재부팅은 이번 작업에서 실행하지 않고 후속으로 남긴다.
- 앱 데이터 삭제, package 이름 변경, version code 증가, Meta Quest PC 라이브러리 등록은 시도하지 않았다.
  Android Development APK는 Quest Link의 PC 앱 라이브러리에 표시되는 대상이 아니다.
- 다음 실기에서는 USB 케이블을 분리한 상태로 Quest를 완전히 재부팅하고 독립 실행형 홈의
  `알 수 없는 출처`를 먼저 확인한다. 그래도 비어 있으면 개발자 모드와 현재 Quest 계정 상태,
  package 추가 시점의 `vrshell` 로그를 수집한다.
- 라이브러리 노출 원인이 확정되기 전에는 package/Manifest를 다시 변경하지 않는다. 화질 비교만 필요하면
  현재 설치본을 ADB로 직접 실행하되, 그 결과는 앱 라이브러리 노출 검증과 분리한다.

## 2026-09-08 선생님 Quest 3 현장 설치 중단과 code 6 후속

### 이번 요청과 보존할 동작

- 이번 요청은 선생님 소유 Quest 3에 현재 화질 개선 설정이 반영된 앱을 설치하는 것이다.
- 기존 Meta Alpha code 5 배포본, 집 Quest 2의 Development 설치본과 6개 조합 기준 JSONL은 삭제하거나
  덮어쓰지 않는다. 현장 설치 문제를 해결하기 위해 앱 흐름, 입력, 오디오, 텔레메트리 동작을 변경하지 않는다.
- 화질 개선 기준은 `Assets/Settings/Mobile_RPAsset.asset`의 Render Scale 1.0과 파트너 로고 3종의
  mipmaps, Trilinear, aniso 8, mip bias -0.5다. 원본 해상도가 작은 로고는 이 설정만으로 원본 한계를
  없앨 수 없으며 고해상도 공식 원본 확보를 별도 후속으로 유지한다.

### 현장에서 확인된 사실

- PC의 Windows 장치 계층에는 `Quest 3`, `Reality Labs Composite ADB Interface`와 MTP가 정상으로
  표시됐다. 케이블이 충전 전용이거나 ADB 드라이버가 전혀 없는 상태는 아니다.
- MQDH와 MQDH 내장 `metavr device list`, `adb devices -l`에서 대상 Quest 3는 모두
  `unauthorized`로 표시됐다. MQDH 내장 설치 명령도
  `Installation failed: Device unauthorized. Please accept the debugging prompt on the device`로 실패했다.
- HMD는 선생님 프로필로 로그인돼 있고 개발자 모드가 켜졌다고 확인됐지만, HMD에는 `USB 연결됨`과
  `Link 연결`만 표시되고 `USB 디버깅 허용` 대화상자는 나타나지 않았다. USB 재연결, ADB 재시작,
  새 임시 ADB 키 요청, Meta Horizon Link 서비스 일시 중지 후 재요청으로도 상태가 바뀌지 않았다.
  임시 키는 제거했고 Meta Horizon Link 서비스는 `Running`으로 원상복구했다.
- APK를 MQDH의 장치 `Apps` 영역이 아니라 App Distribution 업로드 영역에 놓았을 때
  `APK_VERSION_DUPLICATE`가 발생했다. Meta 서버에 `versionCode 5`가 이미 존재한다는 뜻이며,
  HMD 직접 설치 실패와는 별개의 오류다.
- 선생님 Quest 3는 Meta 라이브러리에서 기존 code 5 Release를 다운로드했다. 이것은 현장 사용 가능한
  기존 배포본이지만, 이번 세션에서 마지막으로 조정한 화질 개선판과 동일하다고 확정하지 않는다.
- 최종 화질 후보 Development APK는
  `Builds/MetaHorizonAlpha/ChemicalSafetyVR_TelemetryDev_0_1_0_5.apk`, 크기 277,724,175 bytes,
  SHA-256 `81C59E9375AD347330992230A8382BECF88B8D021DDEE56FD7FE5D77A8E84DAF`다.
  `DEBUGGABLE`인 Development APK이므로 Meta Alpha 배포본으로 업로드하지 않는다.
- 학원 PC의 `ProjectSettings/ProjectSettings.asset`에서는 다음 Release를 준비하기 위해
  `AndroidBundleVersionCode=6`으로 올렸다. 현재 이 PC의 작업 트리는 아직
  `AndroidBundleVersionCode=5`이므로 두 PC의 설정 상태를 합쳐 쓰지 않는다. code 6 Release APK 빌드와
  업로드는 아직 실행하지 않았다.

### 근본 원인과 영향 범위

- 현장 직접 설치의 확정 차단점은 APK가 아니라 Quest 3의 USB ADB 인증 미완료다. 개발자 모드가 실제
  장치 정책에 반영되지 않았는지, 기존 인증 거부 상태인지, Quest OS 인증 UI 결함인지까지는 확정하지 않았다.
- `APK_VERSION_DUPLICATE`는 code 5를 Meta 서버에 다시 업로드하려 한 결과다. 직접 설치에는 version code
  증가가 필요 없지만, 새로운 Alpha Release 업로드에는 code 6 이상의 고유 version code가 필요하다.
- 선생님 HMD가 받은 기존 code 5로 당일 사용은 가능하지만, 이를 최신 화질 개선 검증 완료로 보고하지 않는다.
  최신 반영 여부는 code 6 Release 생성·업로드·라이브러리 업데이트와 HMD 실기 확인 뒤에만 완료 처리한다.

### 집에서 이어갈 후속 작업

1. Unity 6000.4.8f1에서 프로젝트를 열고 import, script compile과 domain reload가 끝났는지 확인한다.
   `AndroidBundleVersionCode=6`과 Render Scale 1.0, 로고 3종 import 설정을 다시 읽는다.
2. `PPELocomotionPpeRegressionValidationHarness`의 화질 설정 검사와 Meta Quest Android, Scene Dependency
   검사를 실행한다. `Tools/MetaAlphaSubmissionGateHarness.mjs`는 현재 code 5 경로와 값을 기준으로 하므로,
   code 6을 새 기준으로 확정할 때 기대 version과 Release 경로를 함께 갱신한 뒤 실행한다.
3. Unity의 `Tools > XR > Build Meta Quest Alpha Release`를 실행해
   `Builds/MetaHorizonAlpha/ChemicalSafetyVR_Alpha_0_1_0_6.apk`를 생성한다. Development APK를 Release
   이름으로 바꾸거나 Alpha 채널에 대신 올리지 않는다.
4. code 6 APK에서 package `com.tycheworks.immersa.safetyvr`, `versionCode=6`, Target SDK 34,
   landscape, install location auto, ARM64, Meta VR headtracking/category, APK Signature Scheme v2,
   `DEBUGGABLE` 비활성과 개발 LAN 비밀값 미포함을 검사하고 크기와 SHA-256을 문서에 추가한다.
5. 검증된 code 6 Release만 Meta App Distribution의 Alpha 채널에 업로드한다. 업로드 완료와 채널 할당을
   구분해 확인하며, 기존 code 5 배포본을 삭제하지 않는다.
6. 선생님 Meta 계정에 Alpha 채널 접근 권한이 유지되는지 확인한 뒤 Quest 3 라이브러리에서 업데이트한다.
   이 경로는 USB ADB 인증과 별개이므로 현장 `unauthorized` 해결을 최신 배포의 선행조건으로 묶지 않는다.
7. Quest 3에서 앱 version code 6 설치, 독립 실행, 타이틀 파트너 로고와 본문 글자 선명도, 양안 표시와 주변
   시야 안정성을 확인한다. MQDH/ADB 인증이 복구되면 `dumpsys package`의 installer와 versionCode를 추가로
   확인하되, 복구되지 않으면 Meta 라이브러리의 업데이트 표시와 HMD 실행 결과를 증거로 별도 기록한다.
8. USB 직접 설치 문제는 재현 가능한 환경 결함으로 분리한다. 다음 진단에서는 Quest 설정의 USB 디버깅
   인증 취소/재승인 가능 여부, 선생님 계정의 개발자 조직 검증, MQDH `Set Up New Device` 완료 상태와 Quest OS
   버전을 확인하고, 원인 확정 전 공장 초기화나 package 변경으로 우회하지 않는다.

### 검증 수준

- **정적 확인:** 화질 개선 Development APK의 파일·해시·Manifest 계열 속성, Windows Quest/ADB 장치와
  `ProjectSettings`의 code 6 설정을 확인했다.
- **Unity Editor 확인:** code 6 Release 빌드와 빌드 후 하네스 실행은 아직 하지 않았다.
- **Quest/OpenXR 확인:** 선생님 Quest 3에서 기존 code 5 다운로드까지만 확인했다. code 6 설치, 최신 화질,
  양안과 주변 시야는 미검증이다.

## 2026-09-08 단일 변경 후 검증 실행 계획

### 목적과 기준 데이터

- 알려진 PPE 음성 결함, 파트너 로고 거리와 Android 화질 후보를 한 APK에서 동시에 변경하지 않는다.
  각 단계는 `한 가지 변경 → 정적/Editor 검증 → Play 또는 Quest 확인 → 채택/복구 결정` 순서로 끝낸 뒤
  다음 단계로 진행한다.
- 2026-09-08에 채택한 Unity Editor + Quest Link 6개 회차는 변경 전 행동·시간 기준으로 보존한다.
  시각 설정 비교마다 6개 회차를 다시 수집하지 않고, 최종 Release 후보가 확정된 뒤 필요한 최소 통합 회차만
  새 기준과 구분해 수집한다.
- 현재 저장소 기준은 `main@f6db61b405563c49168f20872bb13430cc03dbb1`이다. 사용자 작업인 Android
  Keystore 경로 변경은 보존하고 이번 순차 실험의 변수로 취급하지 않는다.

### 1단계 — 정상 장화·안전모 Education Grab 음성

- **대응 요청:** 정상 장화와 정상 안전모를 최초 Grab했을 때 전용 교육 음원이 재생되지 않는 결함만 수정한다.
- **보존 동작:** Training/Test 음성 정책, 하자 PPE 선택·폐기, PPE 착용 순서, UI, 텔레포트, 화질과
  텔레메트리 계약을 변경하지 않는다.
- `PPEVoiceFlowDirector.m_HelmetActionPanel`을 정상 안전모 패널로 교정하고, `m_BootActionPanels`에는 기존
  하자 좌·우 장화를 보존한 채 정상 좌·우 장화를 추가했다. 정적 C# 빌드와
  `PPETrainTestModeValidationHarness.ValidateBatch`는 통과했다.
- **실행 게이트:** Education에서 방호복 착용 후 정상 안전모와 정상 장화 최초 Grab 음성이 각각 1회
  재생되고, 재잡기·반대쪽 장화에서는 중복되지 않아야 한다. 하자 안전모·장화 선택 처리와 Training/Test의
  교육 음성 미재생도 보존돼야 한다.
- Play Mode와 Quest/OpenXR의 실제 청취 및 `voice_playback_started` 대조가 끝나기 전에는 2단계 씬 값을
  변경하지 않는다.

### 2단계 — 파트너 로고 거리 단일 변수 비교

- **변경 전 공간 기준:** 대상은 `Assets/Scenes/1_Title.unity` 하나이며, World Space Canvas의 작성 Z는
  `200`, `PartnerLogos`의 현재 로컬 Z는 `0`이다. `TitleSplashController`는 런타임에 CanvasGroup alpha를
  변경하지만 `PartnerLogos`의 Z 위치를 덮어쓰지 않는다.
- **선행 진단:** Unity에서 `PartnerLogos`를 선택하고 공간 진단 하네스로 전체 부모 경로, Canvas와 카메라,
  월드 코너·스케일·거리·양안 방향을 기록한다. 씬 좌표만 보고 카메라 방향을 추정해 값을 바꾸지 않는다.
- **단일 변경:** `PartnerLogos`의 작성 Z만 한 단계 HMD 쪽으로 이동한다. 로고 RectTransform 크기, 앵커,
  피벗, 자식 크기, Canvas Z, Render Scale과 TextureImporter는 고정한다.
- **비교 조건:** 같은 Quest, 같은 시작 자세와 머리 이동 조건에서 정지 선명도, 시머링, 화면 점유율,
  Title 메인 로고와의 깊이·정렬, 양안 불일치와 시야 가장자리 깨짐을 변경 전 캡처와 비교한다.
- 개선이 없거나 깊이 분리·시머링이 악화되면 작성 Z를 기준값으로 복구한다. 통과 전에는 텍스처 설정을
  함께 조정하지 않는다.

### 3단계 — Android 텍스처 렌더링 단일 변수 비교

- 현재 기준은 Mobile URP Render Scale `1.0`, MSAA 4x, 파트너 로고 mipmap On, Trilinear, aniso 8,
  mip bias `-0.5`, Android `RGBA32 + Uncompressed`다.
- `to21_logo.png`와 `seoulit_logo.png`는 원본 해상도가 작으므로 압축 포맷 변경이 원본에 없는 픽셀을
  복원하지 못한다. 가능하면 고해상도 공식 원본 확보를 첫 자산 후보로 삼되, 원본 교체와 Importer 변경을
  같은 비교에 넣지 않는다.
- Android 포맷을 비교할 때는 대표 로고 하나에서 `RGBA32 + Uncompressed`와 한 가지 Android 후보만
  비교한다. 포맷, max size, mipmap, filter, aniso, mip bias와 Render Scale 중 둘 이상을 한 APK에서
  바꾸지 않는다.
- 각 후보는 APK의 실제 Android Import 결과, eye texture 크기, 정지 선명도, 머리 이동 시 시머링,
  GPU frame time과 양안 안정성을 기록한다. Game View 또는 빌드 성공만으로 채택하지 않는다.

### 4단계 — 기준 데이터와 code 6 Release

- 학원 PC에는 `AndroidBundleVersionCode=6`이 적용돼 있고 현재 이 PC에는 code 5가 남아 있다.
  1~3단계의 채택값이 확정되면 실제 Release를 생성할 단일 PC와 작업 트리를 먼저 확정하고, 그 기준에서
  `AndroidBundleVersionCode=6`과 `MetaAlphaSubmissionGateHarness`의 기대 version·Release 경로를 함께
  확인한다. 이미 적용된 학원 PC의 code 6을 미적용 또는 오류 상태로 소급해 기록하지 않는다.
- 보존된 6개 JSONL은 기준 DB 반영, 상세 조회 대조와 DB 백업을 완료해야 한다. 현재 게이트의
  `기준 Express 서버 TCP 3000 미기동`, 기준 DB `session 0 / event 0` 상태는 미완료로 유지한다.
- 최종 code 6 Release는 Android Manifest·ARM64·Target SDK 34·서명 v2·`DEBUGGABLE` 비활성·개발 LAN
  비밀값 미포함을 검사한 뒤 Alpha 채널에 업로드한다. 업로드, 채널 할당, Quest 3 설치, 실제 실행과 화질
  확인을 각각 분리해 기록한다.

### 단계별 완료 상태

| 단계 | 정적 확인 | Unity Editor | Play Mode | Quest/OpenXR |
| --- | --- | --- | --- | --- |
| 1. 장화·안전모 Grab 음성 | 완료 | 배치 하네스 PASS | 사용자 Game View 잠정 통과 | 미완료 |
| 2. 파트너 로고 거리 | Z `-20` 단일 diff 확인 | 씬 로드·시작 하네스 PASS | 미실행 | 미실행 |
| 3. Android 텍스처 | 현재 기준 확인 | 미실행 | 해당 없음 | 미실행 |
| 4. 기준 DB·code 6 Release | 계획 확정 | 미실행 | 미실행 | 미실행 |

## 2026-09-09 다음 작업 세션 인수인계

### 현재 확정 상태

- 정상 장화·안전모 Education Grab 음성 참조를 교정했고 정적 빌드와
  `PPETrainTestModeValidationHarness.ValidateBatch`가 PASS했다.
- 사용자는 방호복을 먼저 착용한 Game View 순서에서 음성 흐름이 확인된 것으로 보고했다. 이 결과는
  `Play Mode 잠정 통과`이며 Quest/OpenXR 실제 Grip과 원본 텔레메트리 대조는 아직 완료하지 않았다.
- `Assets/Scenes/1_Title.unity`의 `Canvas/PartnerLogos` 로컬 Z만 `0`에서 `-20`으로 변경했다. 크기,
  앵커, 피벗, 자식 로고, Canvas Z, Render Scale과 TextureImporter는 변경하지 않았다.
- 로고 변경 후 Unity 배치 Import에서 `1_Title`이 정상 로드됐고
  `AppStartupSynchronizationHarness.Validate`가 PASS했다. 이 결과는 씬 로드와 시작 계약 확인이며 Game
  View와 Quest/OpenXR 시각 결과는 미확인이다.

### 다음 세션 첫 실행 순서

1. Unity의 Play Mode와 컴파일·Import 진행 여부를 확인한다. 사용자가 보존해야 할 `4_PPE_Room`의 미저장
   변경이 있다면 먼저 저장 여부를 직접 판단한다.
2. 생산 대상인 `Assets/Scenes/1_Title.unity`만 열고 `PartnerLogos`의 작성 로컬 Z가 `-20`인지 확인한다.
   다른 Title variant나 다른 씬은 수정하지 않는다.
3. 같은 시작 자세에서 정지 선명도, 화면 점유율, 메인 로고와의 깊이 정렬을 Game View로 먼저 비교한다.
4. 같은 Quest와 머리 이동 조건에서 시머링, 양안 불일치와 주변 시야 깨짐을 확인한다.
5. 개선되면 `-20`을 채택하고 결과를 기존 Title 로고 버그 문서에 기록한다. 개선이 없거나 깊이 불편,
   시머링 또는 양안 문제가 생기면 다른 값을 탐색하지 않고 로컬 Z만 `0`으로 복구한다.
6. 로고 거리 결과가 확정되기 전에는 Android texture format, max size, mipmap, filter, aniso, mip bias와
   Render Scale을 변경하지 않는다.

### 다음 단계와 남은 검증

- 로고 거리 채택 또는 복구가 끝난 뒤에만 Android/AOS 텍스처 후보 하나를 선택해 별도 APK로 비교한다.
- 학원 PC의 code 6과 현재 PC의 code 5를 혼합하지 않는다. 최종 빌드 PC와 작업 트리를 확정한 뒤 기준 DB
  반영, Release 빌드, Meta Alpha 업로드 순서로 진행한다.
- 이번 인수인계 커밋에서는 사용자 개인 환경값인 `ProjectSettings/ProjectSettings.asset`의 Keystore 경로
  변경을 제외한다.

## 2026-09-09 결정: 심사 기간을 활용한 서버 우선 Alpha 전략

### 결정 이유

- 현재는 대시보드에 최종적으로 어떤 지표와 화면을 보여줄지 확정되지 않았다.
- 대시보드를 서둘러 고정하면 실제 훈련 데이터와 운영 요구를 확인하기 전에 지표·분류·상세 조회 구조를
  잘못 확정할 수 있다.
- 따라서 Alpha를 먼저 제출하고 Meta 심사 기간을 대시보드의 지표·화면·상세 조회 기준을 결정하고
  구현하는 기간으로 활용한다.
- 이 전략은 서버 연동을 미루는 뜻이 아니다. **제출용 Release Alpha는 원본 텔레메트리를 서버로 전송할
  수 있어야 한다.** 그래야 심사 기간에 실제 원본을 근거로 대시보드를 설계하고, 이미 배포된 Alpha의
  행동 데이터를 잃지 않는다.

### 제출과 심사 기간의 분리 계약

1. **Alpha 제출 전 필수:** Release 빌드에서 로컬 JSONL 원본을 보존하고, 운영 HTTPS와 릴리스용 인증을
   사용해 서버 수집 API로 전송하는 경로를 구현한다. 개발용 LAN 주소와 개발용 업로드 token을 Release에
   포함하지 않는다.
2. **Alpha 제출 전 검증:** Release 후보와 같은 전송 경로에서 `sessionId`, `eventId`, `sequence`,
   `schemaVersion`, `appVersion`, 원본 event payload의 서버 수락·저장·재조회 일치를 최소 한 세션으로
   확인한다. 서버 자동 테스트만으로 Quest Release 통합 성공을 대신하지 않는다.
3. **심사 기간에 확정:** 대시보드 표시 항목은 Meta 심사 기간에 확정한다. 교육·훈련·테스트별 KPI,
   합격 기준, 집계 카드, 필터와 상세 조회 화면은 원본 데이터와 사용자 결정을 근거로 순차 구현한다.
4. **원본 보존:** 원본 이벤트를 삭제하거나 대시보드 지표에 맞춰 축약하지 않는다. 대시보드 요구가
   바뀌어도 서버 원본에서 다시 계산할 수 있도록 스키마 버전, 식별자, 순서와 원본 payload를 보존한다.
5. **완료 상태 분리:** 대시보드 완성 여부를 Alpha 제출 완료 조건으로 묶지 않는다. 동시에 Release Alpha
   서버 전송과 대시보드 완성을 하나의 완료 상태로 합치지 않는다. 상태는 `Release 전송 구현`,
   `서버 반영`, `Release 통합 검증`, `대시보드 설계`, `대시보드 구현`으로 구분한다.

### 현재 상태와 하네스 판정

- **클라이언트 반영:** JSONL 원본 기록과 Editor·Android Development Build의 durable 업로드 경로는
  존재한다. 현재 `TycheTrainingTelemetryUploader`는 Release Player 전송을 명시적으로 차단하므로 이번
  전략의 Release 전송 조건은 아직 충족하지 않는다.
- **서버 반영:** 서버 `main@d380f106e461599641bc3f87a60cf91d16a2c9c9`에는 인증된 텔레메트리 세션·이벤트·완료
  API와 저장소가 있다. 이번 기록에서는 운영 HTTPS 배포와 Release용 인증의 실제 성공을 새로 검증하지
  않았다.
- **통합 검증:** 과거 Editor·Development 경로의 실제 적재 근거는 있지만 Release Alpha의 Quest → 운영
  서버 적재·재조회는 미검증이다. 이전 Development 성공을 Release 성공으로 확대하지 않는다.
- `node Tools/MetaAlphaSubmissionGateHarness.mjs`는 위 전략 문구를 검사하고, 업로더에
  `Release players never enable this transport` 차단 계약이 남아 있으면 FAIL한다. 이는 현재 미완료를
  숨기지 않기 위한 의도적인 red gate다. 릴리스 인증 구조가 확정되면 하네스에 HTTPS·인증·비밀값 미포함과
  실제 적재 증거의 positive gate를 추가한다.

### 변경 전 필수 질문 답변

1. **기존 Inspector/씬 작성값을 보존하는가?** 이번 변경은 문서와 Node 하네스만 수정하며 씬,
   Inspector, UI와 입력 작성값은 변경하지 않는다.
2. **단일 기준 오브젝트와 상태 소유자는 무엇인가?** 클라이언트 원본은
   `PPETrainingTelemetryCapture`, 전송 ACK와 재시도는 `TycheTrainingTelemetryUploader`, 서버 원본은
   training telemetry repository가 소유한다. 대시보드는 원본의 소비자이며 원본 상태 소유자가 아니다.
3. **입력 전체 경로는 무엇인가?** 이번 변경은 입력 장치, Interactor/Caster, Raycaster, Layer,
   Collider, press/select action과 handler를 변경하지 않는다.
4. **실패 시 자동 수리 대신 멈춰야 하는가?** Release 전송 경로가 없거나 실제 적재 증거가 없으면 제출
   하네스가 명확히 FAIL한다. 하네스가 서버 설정, 씬 또는 대시보드를 자동 생성·수리하지 않는다.
5. **함께 영향을 받는 소비자는 무엇인가?** Release 전송, 서버 수집·저장, 개인정보 고지와 향후
   대시보드가 영향을 받는다. UI, 텔레포트, PPE Grab, 거울, XR 양안과 음성 동작은 변경하지 않는다.
6. **변경 전후 비교 실행은 무엇인가?** 변경 전 하네스는 Release LAN 차단 문구를 필수 계약으로
   인정했다. 변경 후에는 같은 문구가 남아 있으면 전략 위반으로 FAIL하며 대시보드 미완성 자체는 실패로
   판정하지 않는다.
7. **검증 수준은 어디까지인가?** 이번 기록은 문서·하네스 정적 검증까지만 수행한다. Unity Editor,
   Play Mode, Quest/OpenXR, Release APK 네트워크와 운영 서버 적재는 새 실행 증거가 없으므로 미검증이다.

이번 변경이 대응하는 사용자 요청은 `대시보드 내용을 성급히 확정하지 않고 Meta 심사 기간을 전략적으로
활용하되, 제출 Alpha부터 서버에 재가공 가능한 원본 데이터를 보내는 판단을 하네스로 고정`하는 것이다.
보존해야 하는 기존 동작은 로컬 JSONL, durable 재전송, eventId 멱등성, Development LAN 검증과 기존
교육·훈련·테스트 흐름이며, 대시보드 지표나 새로운 사용자 분류를 이번 변경에서 만들지 않는다.

## 2026-09-09 완료 기록: 타이틀·로딩·보고자료·PPE 모달 점검

### 적용한 변경

- `1_Title`은 타이틀 로고를 카메라 쪽으로 당기고, 파트너 로고를 위쪽·앞쪽으로 이동했으며 버전 텍스트도
  파트너 로고와 같은 깊이로 맞췄다. 씬에서 직접 크기·위치·깊이를 조정할 수 있도록 세 로고의
  `CanvasGroup.alpha`를 `1`로 저장하고 런타임은 작성된 RectTransform과 글자 크기를 덮어쓰지 않는다.
- `3_Loading`은 Scene View에서 메인 로고와 진행 UI를 볼 수 있게 저장하되 원래 요구대로 파트너 로고는
  비활성 상태를 유지한다. 진행시간은 12초이며 메인 로고는 표시 즉시 반대 Y축 방향으로 약 1.11초 동안
  한 번 회전하고 0.5초 정지한 뒤 로딩 씬이 끝날 때까지 계속 반복한다.
- 사용자가 확인한 마우스 동작은 방향이나 너비가 아니라 속도 곡선 측정에만 사용했다. 초반 가속 뒤 길게
  감속하는 곡선과 프레임당 최대 진행량 `1/30초`를 적용해 초기화 프레임이 첫 회전을 건너뛰는 현상을
  방지했다.
- 현재 학원 PC의 `ProjectSettings/ProjectSettings.asset`은 Android build code `6`이다. 기존 code 5
  Release·Development APK는 존재하지만 code 6 Release APK는 아직 생성되지 않았다.
- `MetaAlphaSubmissionGateHarness`의 현재 Release 아티팩트와 `AndroidBundleVersionCode` 기대값을
  code 6으로 갱신했다. 대시보드 미완성은 Alpha 제출 실패로 보지 않지만, code 6 Release APK 부재와
  Release Player 서버 전송 차단은 계속 명시적인 제출 차단 조건이다.
- 상세기획서 기준본을 2026-09-09 상태로 갱신하고
  `https://immersa.tycheworks.com/chemical-safety-training/plan/`에 공개했다. 서버 `main` 반영 커밋은
  `7c87aa762ec444ff68dfd68c76b4f6fdeabcd589`, 운영 브랜치 반영 커밋은
  `63d5a424eacfa5b123ebb46d308406926e25eeff`다.
- 상세기획 요약 PPT 최종본은
  `Docs/PPT/TYCHE_화학물질_안전훈련_VR_상세기획_요약_6장_v2.pptx`이며, 첫 장에 사용자가 제공한 Trello
  진행 보드 초대 링크와 공개 상세기획서 주소를 연결했다. 초대 토큰이 포함된 Trello 링크는 외부 배포
  범위가 넓어지기 전에 읽기 전용 공개 링크로 교체하는 편이 안전하다.
- `4_PPE_Room`의 실제 흐름에서 사용하는 `Scenario Detail Modal`을 점검했다. 기본 상태는
  `Modal Canvas=활성`, `Scenario Detail Modal=비활성`, `Modal Panel_1=활성`, `1_EduChoice=활성`,
  `2_Mode=비활성`, `3_TestWorkPlan=비활성`이다. Canvas를 끄지 않고 모달 루트만 숨기는 현재 상태가
  XRI Raycaster와 시작 화면 모두에 맞다.
- 상단 `title_1`은 씬에 작성된 `PPE  착용 교육` 문자열을 유지한다. 이 오브젝트가 실제
  `titleText`와 `scenarioTitleObjects[0]`에 연결되어 있어 모드 선택·학습모드·작업 시나리오 선택 경로에서
  `ScenarioDetailModal.Show()`가 시나리오 데이터의 제목으로 덮어쓰지 않는다. 현재 문자열의 `PPE` 뒤
  공백 두 칸은 작성값 그대로 보존했다.

### 근본 원인과 결정

- 타이틀·로딩 이미지를 Scene View에서 조정할 수 없었던 직접 원인은 작성된 오브젝트가 아니라 저장된
  `CanvasGroup.alpha=0`과 런타임 표시 전제였다. 작성값을 보이게 저장하고 런타임이 위치·크기를 덮지
  않도록 역할을 분리했다.
- 로딩 첫 회전이 늦게 보인 원인은 시작 지연 자체뿐 아니라 OpenXR 초기화의 큰 첫 프레임 시간이 회전
  전체를 소비할 수 있었기 때문이다. 첫 프레임 표시와 회전 진행량 상한을 분리했다.
- 대시보드 항목을 아직 확정하지 못한 상태에서 화면부터 고정하지 않는다. Release Alpha의 재가공 가능한
  원본 서버 전송을 먼저 확보하고 Meta 심사 기간에 실제 원본을 보며 지표·필터·상세 화면을 결정한다.
- PPE 모달 타이틀 덮어쓰기 우려는 실제 직렬화 참조와 런타임 분기를 대조한 결과 현재 코드에서 재현되지
  않는다. 씬 작성 타이틀 배열이 존재하는 동안 동적 `detail.Title` 대입 경로는 실행되지 않는다.

### 영향 범위

- 변경 소비자는 `1_Title`의 타이틀·파트너·버전 표시, `3_Loading`의 메인 로고 회전·12초 진행,
  Alpha 제출 하네스, 상세기획서와 PPT다.
- PPE 모달은 사용자가 작성한 타이틀 문자열 한 건만 씬에 반영됐고 입력, Raycaster, 모드 전이,
  텔레포트, PPE Grab, 오디오와 퀴즈 동작은 변경하지 않았다.
- 서버에는 상세기획서 정적 파일과 참조 이미지, 공용 출시 문서만 반영했다. 클라이언트 저장소에 서버
  소스를 복제하지 않았고 운영 PM2·DB·마이그레이션은 변경하지 않았다.

### 완료한 검증

- 사용자는 PPE 음성이 정상이라고 확인했고, 타이틀 로고와 파트너 로고를 카메라 쪽으로 당긴 결과가 더
  선명하다고 확인했다.
- Unity Editor Play Mode에서 로딩 로고의 즉시 시작, 반대 방향, 계속 반복, 0.5초 회전 간격과 12초
  로딩을 사용자 피드백으로 조정했다.
- `dotnet build Assembly-CSharp.csproj --no-restore`와
  `dotnet build Assembly-CSharp-Editor.csproj --no-restore`는 로딩 작업 최종 상태에서 오류 0개였다.
- PPT 6장을 PNG로 렌더링해 잘림·겹침을 확인했고 PPT 패키지의 Trello·상세기획서 하이퍼링크와 6개
  슬라이드 구조를 확인했다.
- 서버 자동 테스트 86개, 운영 서버 `nginx -t`, 공개 상세기획서와 대표 이미지의 HTTPS `200` 응답을
  확인했다. 정적 파일 배포이므로 PM2 재시작과 DB 변경은 수행하지 않았다.
- PPE 모달은 `4_PPE_Room.unity`, `PPEVoiceFlowDirector`, `ScenarioDetailModal`의 참조·활성 상태·문자열
  대입 분기를 정적으로 대조했다. 이 점검만으로 Quest 표시 성공을 주장하지 않는다.

### 아직 필요한 수동 검증

- Quest/OpenXR 양안에서 타이틀·파트너·버전 로고의 선명도, 투명 잔상과 가장자리 시머링을 확인한다.
- Quest에서 로딩 첫 프레임 크기·위치가 튀지 않는지, 반대 Y축 회전과 0.5초 정지가 12초 동안 안정적으로
  반복되는지 확인한다.
- `PPE 착용 교육` 타이틀이 모드 선택·학습모드·두 작업 시나리오 선택에서 계속 유지되는지 Play Mode와
  Quest에서 확인한다. 현재 작성값의 공백 두 칸을 한 칸으로 정리할지는 별도 UI 작성 결정으로 남긴다.
- code 6 Release APK를 생성하고 Quest 실제 실행, 운영 HTTPS 인증 업로드, 같은 세션의 서버 적재·원본
  재조회 일치를 확인해야 Meta Alpha 제출용 서버 연동 완료로 판단할 수 있다.

## 2026-09-09 Alpha 제출 전 Codex·사용자 역할과 실행 순서

### 이번 작업이 대응하는 사용자 요청

- 사용자는 Alpha 제출 전에 Codex가 수행할 구현·자동 검증과 사용자가 수행할 계정·Quest·시각 검증의
  순서를 한 문서에서 확인하고, 문서화가 끝난 뒤 첫 구현 단계부터 시작하기를 요청했다.
- 이번 계획은 **대시보드 완성보다 Alpha 제출을 먼저 진행하되, 제출용 Release부터 재가공 가능한 원본
  텔레메트리를 서버에 보존한다**는 2026-09-09 서버 우선 Alpha 전략을 실행 가능한 인수인계 순서로
  구체화한다.
- PPE 교육·훈련·테스트, UI, 음성, 텔레포트, 거울과 기존 Development LAN 업로드 동작은 보존한다.

### 역할 분담

| 단계 | Codex가 수행할 작업 | 사용자가 수행할 작업 | 다음 단계 진입 조건 |
| --- | --- | --- | --- |
| 0. 기준 고정 | 클라이언트·서버 브랜치와 SHA, 공용 문서, 현재 제출 하네스와 APK 상태를 확인한다. | 미커밋 Unity 씬과 개인 설정의 저장 여부를 판단한다. 비밀값을 문서나 채팅에 공유하지 않는다. | 작업 기준과 보존 대상이 기록됨 |
| 1. 서버 Release 인증 | Meta User Proof 검증 API, 검증된 사용자용 단기 업로드 토큰, 만료·위조·불일치 거부와 자동 테스트를 구현한다. | Meta 개발자 대시보드의 앱·테스트 사용자 접근이 유지되는지만 확인한다. 실제 App Secret은 서버 환경에서만 관리한다. | 서버 자동 테스트 PASS와 요청·응답 계약 확정 |
| 2. 클라이언트 Release 전송 | 앱 범위 Meta ID와 User Proof 획득, 운영 HTTPS 인증, JSONL 우선 기록, ACK·재시도·재실행 복구를 연결한다. | 이 단계에서는 APK를 미리 만들지 않는다. Keystore 비밀번호와 운영 자격 증명은 로컬에서만 보존한다. | C# 컴파일과 정적·Editor 하네스 PASS |
| 3. Staging 통합 | 운영과 같은 HTTPS 경로에서 인증·세션·이벤트·완료·재조회와 중복 전송을 검사한다. | 필요할 때 Meta 테스트 계정으로 짧은 Quest 실행을 수행한다. | 같은 `sessionId`, 이벤트 수와 마지막 `sequence` 일치 |
| 4. XR 수동 회귀 | 확인할 씬·로그·원본 이벤트와 합격 기준을 제시하고 결과를 기록한다. | Quest에서 타이틀·로딩·PPE 모달·장화/안전모 음성·양안·프레임 안정성을 직접 확인한다. | 제출 차단 시각·입력·음성 회귀 없음 |
| 5. code 6 Release | Release 설정, Manifest, 비밀값 미포함과 제출 하네스를 점검하고 code 6 빌드 절차를 제공한다. | 본인 Keystore로 code 6 Release APK를 빌드하거나 Codex가 준비한 빌드 결과의 서명을 확인한다. | `DEBUGGABLE` 없음, cleartext 차단, 제출 게이트 PASS |
| 6. Alpha 종단간·업로드 | Quest 원본과 운영 서버 저장·재조회 결과를 대조하고 최종 체크리스트를 판정한다. | Meta Alpha 채널 설치·실행과 최종 업로드를 수행한다. | Quest Release 한 세션의 인증·원본·서버 적재 일치 |
| 7. 심사 기간 | 보존된 원본을 바탕으로 대시보드 지표·필터·상세 조회 후보를 구현한다. | 실제 운영에 필요한 KPI와 화면 우선순위를 결정한다. | 사용자 승인 지표만 대시보드 기준으로 채택 |

### 현재 시작점과 즉시 실행 순서

1. **Codex:** 서버 `main`의 현재 인증이 개발용 고정 Bearer token만 허용하는지 코드·테스트로 다시
   확정하고 Meta User Proof 검증 계약을 구현한다.
2. **Codex:** 서버 테스트가 통과하면 대상 브랜치와 커밋 전 변경 파일, 환경 변수 이름, 요청·응답 예시를
   클라이언트 인수인계에 기록한다. 운영 배포·PM2 재시작·운영 DB 변경은 사용자 승인 전 수행하지 않는다.
3. **Codex:** 클라이언트 `0_App`의 Inspector 작성값을 기준으로 Release 인증·HTTPS 업로더를 연결한다.
   누락 참조를 런타임에서 자동 생성하거나 운영 주소·비밀값을 코드에 하드코딩하지 않는다.
4. **사용자:** Codex가 정적·Editor 검증 완료를 보고하기 전에는 code 6 APK를 만들 필요가 없다.
5. **Codex와 사용자:** 구현이 고정된 뒤에만 Quest 수동 회귀, code 6 Release, 운영 서버 한 세션 대조,
   Alpha 업로드 순서로 진행한다.

### 단계별 중단·재개 규칙

- 각 단계는 위 표의 진입 조건을 만족한 뒤 다음 단계로 넘어간다. 서버 코드 존재를 클라이언트 연결 또는
  Quest 통합 성공으로 합쳐 쓰지 않는다.
- Codex가 사용자에게 요청하는 첫 수동 작업은 서버와 클라이언트 자동 검증이 끝난 뒤의 **짧은 Quest
  인증·전송 실행**이다. 그 전에는 APK 생성, 장시간 6개 조합 재수행이나 Alpha 업로드를 요청하지 않는다.
- 사용자가 직접 해야 하는 이유는 Meta 계정 접근, 개인 Keystore, HMD 양안·음성·체감 성능과 Meta 채널
  업로드가 코드 정적 검증으로 대체될 수 없기 때문이다.
- 실패 시 해당 단계에서 멈추고 원본 로그와 최근 변경을 먼저 비교한다. 다음 단계의 fallback, 자동 수리,
  UI 변경 또는 대시보드 기능을 섞지 않는다.

### 현재 확인 상태

- 클라이언트 기준: `main@b840b2dc2e4d0235c087fb4693a1486bd21dab9c`, 서버 구현·운영 반영 기준:
  `main@5b8138865991408215011078328f74a0df229982`.
- code 5 Development APK는 보존되어 있고 code 6 Release APK는 아직 없다. 이는 1~2단계 구현 중에는
  정상적인 대기 상태다.
- 서버 Release 인증과 클라이언트 Release HTTPS 전송 경로를 구현한 뒤
  로컬 기준 Express PM2 프로세스를 시작한 뒤 `MetaAlphaSubmissionGateHarness`는 code 6 Release APK가
  아직 없어서만 `WAIT`한다. 구현 실패 상태인 `FAIL`과 제출물 대기 상태인 `WAIT`를 구분한다.
- 서버의 `Docs/ChemicalSafetyVRSystemArchitecture.md`와 공유 문서 목록은 클라이언트 기준본에 같은 상대
  경로로 동기화했고 양쪽 내용을 대조했다.

### 완료 검증과 아직 필요한 검증

- 완료: 역할·순서·단계별 진입 조건 문서화, Meta User Proof 서버 검증, 검증 사용자용 단기 토큰,
  세션 소유권 검사, 클라이언트 Android Release HTTPS 인증·업로드 경로, 최신 `main` 기준 서버 자동
  테스트 93개 PASS,
  클라이언트 런타임·Editor C# 정적 빌드 오류 0, Unity Editor 재컴파일과 `App Startup Synchronization`,
  `PPE Training Data Contract`, `Documentation Policy` 하네스 PASS, 공용 문서 동기화를 확인했다.
- 운영 서버 반영 완료: 운영 `.env` 비밀값 설정, migration `009`~`012` 적용, 전체 migration 16개,
  텔레메트리 초기 참여자·세션·이벤트 0건, PM2 `online`, Nginx POST 전용 경로와 HTTPS/TLS를 확인했다.
- 미완료: 실제 Meta User Proof 왕복, code 6 Release APK, Quest 단독 실행, 같은 세션의 운영 서버
  적재·재조회와 Alpha 업로드.
- 대시보드 화면·KPI 확정은 Alpha 제출 완료 조건이 아니며 7단계까지 보류한다.

## 2026-09-10 단계 5 완료: code 6 Release APK

### 이번 실행 결과

- 역할표의 `5. code 6 Release`를 완료했다. 클라이언트 기준은
  `main@eba9e1a8d46d964ab4d31f4b07081b28fa861ed7`, 서버 구현 기준은
  `main@5b8138865991408215011078328f74a0df229982`, 서버 공용 문서 미러 기준은
  `main@5e13e0206b72f944b9b313f8eecb053d39594537`이다.
- 생성 파일은 `Builds/MetaHorizonAlpha/ChemicalSafetyVR_Alpha_0_1_0_6.apk`, 크기
  `219,710,171 bytes`, SHA-256
  `F092AA929888C713B738806E4624EA0AEB62CB0E7D8CC05289D253C1640AC4AF`다.
- 첫 빌드는 Unity 종료로 중단됐고, 재시작 뒤 동일 code 6 설정과 개인 서명정보를 다시 입력해 두 번째
  빌드가 완료됐다. 중단된 첫 시도의 오래된 Tundra 오류와 두 번째 빌드 진행 로그를 분리해 판정했다.

### 검증 수준

- **정적 확인:** package `com.tycheworks.immersa.safetyvr`, `versionCode=6`, `versionName=0.1.0`,
  최소 SDK 25, Target SDK 34, ARM64, 필수 VR headtracking, Meta VR category,
  `usesCleartextTraffic=false`, `android:debuggable` 없음, APK Signature Scheme v2를 확인했다.
- **Unity Editor 확인:** BuildReport의 `[Meta Quest Alpha Build] PASS`와 빌드 뒤 Editor 정상 복귀를
  확인했다. `MetaAlphaSubmissionGateHarness`는 Release·Development APK, 기준 DB와 로컬 기준 서버를
  검사해 `READY`를 반환했다.
- **Quest/OpenXR 확인:** 아직 수행하지 않았다. APK 생성과 Manifest 검증을 실제 HMD 양안·입력·오디오,
  Meta proof 또는 운영 서버 적재 성공으로 확대하지 않는다.

### 다음 사용자·Codex 순서

1. **사용자:** 이 code 6 APK를 Meta Dashboard의 Alpha 채널에 업로드하고 테스트 계정에 채널을 할당한다.
2. **사용자:** Quest에서 기존 앱을 완전히 종료한 뒤 Alpha 채널의 code 6을 설치하고 단독 실행한다.
3. **사용자와 Codex:** 타이틀·로딩·PPE 모달, 장화·안전모 음성, 양안·주변 시야, 입력과 프레임을 짧게
   확인한다.
4. **Codex:** 같은 회차의 Quest 원본, Meta 인증 결과, 서버 `sessionId`·이벤트 수·마지막 `sequence`와
   운영 MySQL 재조회를 대조한다.
5. 위 4단계가 모두 맞을 때만 `Release 통합 검증 완료`로 표시한다. Alpha 업로드 자체와 대시보드 완성은
   별도 상태로 유지한다.

## 2026-09-10 작업 종료 기록: Alpha 업로드와 다음 시작점

### 오늘 완료한 범위

- 사용자는 Meta 업로드 화면에서 code 6 Release APK 처리가 완료됐음을 확인했다. 현재 확정 가능한 상태는
  `APK 생성·정적 검증·Alpha 업로드 완료`까지다.
- 업로드 연령대는 아동용 앱이 아닌 화학물질 안전훈련의 실제 대상에 맞춰
  `Teens and Adults (13+)`를 사용했다.
- 릴리즈 노트에는 Release 인증·운영 HTTPS 전송, 타이틀·로딩 조정, 정상 장화·안전모 음성 참조 교정과
  Quest/Android Release 설정 보완을 기록했다.
- APK에는 프로젝트 아이콘이 포함돼 있지만 Meta Store/Library 목록 아이콘은 App Metadata에서 별도로
  관리된다. 512×512, 24-bit RGB, 불투명 정사각형 자산
  `Builds/MetaHorizonAlpha/SubmissionAssets/Meta_Horizon_Icon_512.png`를 준비했으며, 정식 심사 제출 버튼을
  누르지 않고도 나중에 metadata 초안에 저장할 수 있다.

### 완료로 확장하지 않는 항목

- Alpha 업로드 완료는 테스트 사용자 채널 할당, Quest 설치, 실제 Meta User Proof, 단기 token 발급,
  운영 서버 전송이나 MySQL 적재 성공을 뜻하지 않는다.
- 현재 Meta 처리 화면의 주황색 기본 아이콘은 APK 빌드 실패 증거가 아니다. App Metadata 아이콘 저장과
  Quest 라이브러리 반영은 별도로 확인한다.
- App Metadata 초안 작성과 Store 심사 제출은 다른 동작이다. Quest Alpha 통합 검증 전에는
  `Submit for Review`를 누르지 않는다.

### 다음 세션 시작 순서

1. Dashboard에서 `versionCode=6`, `ALPHA`와 테스트 사용자 할당을 캡처 또는 화면으로 확인한다.
2. Quest에 Alpha code 6을 설치하고 앱 정보의 version code를 확인한다.
3. 단독 실행에서 타이틀·로딩·PPE 음성·모달·입력·양안과 성능을 짧게 회귀 검증한다.
4. 한 회차를 완료해 Quest JSONL과 운영 서버 저장·재조회 결과를 같은 ID와 수량으로 대조한다.
5. 통합 PASS 뒤 App Metadata의 아이콘·설명·제출 이미지를 저장하고, Store 심사 제출 여부는 사용자 결정으로
   별도 진행한다.
