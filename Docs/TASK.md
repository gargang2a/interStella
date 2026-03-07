# interStella TASK

## 문서 운영 규칙
- 이 문서는 Append-Only로 운영한다.
- 완료된 작업도 삭제하지 않고 상태를 갱신해 이력으로 남긴다.
- 신규 항목은 기존 항목 아래에 이어서 추가한다.

## 현재 스프린트 백로그 (MVP 기반)
- [ ] 공통 상태 타입 정리: MatchPhase, FuelState, TetherState, ScrapState, RepairState
- [ ] Player 분리 구조 골격: InputReader, Motor, Fuel, Interaction, Presentation, NetworkBridge
- [ ] 무중력 6DoF 로컬 이동 구현
- [ ] 연료 소모/회복/고갈 규칙 구현
- [ ] 테더 상태 모델 및 제약 솔버 분리 구현
- [ ] 스크랩 픽업/드롭/운반 루프 구현
- [ ] 정거장 수리 목표(요구량/납품량/완료) 구현
- [ ] 승리/실패 판정 및 매치 페이즈 전이 구현
- [ ] 로컬 Host/Client 검증 시나리오 정리
- [ ] Steam 통합 전 체크리스트 확정

## 실행 순서 (권장)
1. 로컬 단일 플레이 루프 완성
2. 로컬 2인 Host/Client 동기화
3. FishNet 권한/복제/보정 정리
4. Steam 로비/초대/릴레이 통합
5. Steam 2인 E2E 플레이 테스트

## 변경 이력 (Append-Only)
### 2026-03-05 18:03 (KST)
- 문서 신규 생성
- MVP 기준 백로그 초기 등록
- 실행 순서(로컬 완성 후 Steam 통합) 고정

### 2026-03-05 18:08 (KST)
- 반복 운영 태스크 추가: 기능 변경 후 `기획서/TASK/Build Guide`에 변경 요약 누적 기록
- 규칙 재확인: 체크박스 항목은 삭제 대신 상태 전환으로 이력 유지

### 2026-03-05 17:58 (KST)
- 사용자 요청 반영: "옵시디언에 기록해"
- 문서 운영 태스크 실행: 3개 기준 문서에 동시 누적 기록

### 2026-03-05 18:24 (KST)
- [x] Unity MCP 패키지 설치 (`com.coplaydev.unity-mcp` embedded)
- [x] Codex config의 `unityMCP`를 stdio(uvx) 모드로 전환
- [ ] Unity Editor 내 stdio bridge 활성화 확인 (`:6400` 리스닝 확인)
- [ ] Codex 세션 재시작 후 `unityMCP` MCP handshake 재검증

### 2026-03-05 18:43 (KST) 진행 스냅샷
- [x] Unity MCP 연결/제어 검증 (scene 조회, play/stop, console 조회)
- [x] Vertical Slice MVP 씬 생성 및 기본 오브젝트 배치
- [x] Player/Tether/Scavenge/Repair/Stations 1차 코드 골격 반영
- [x] 로컬 PlayMode 스모크 테스트 (치명 에러/예외 미검출)
- [ ] 인스펙터급 참조 검증 자동화 (`resources/read` 이슈 우회 또는 수정)
- [ ] FishNet Host/Client 권한 경계 구현 1차 (durable/transient 분리 전송)
- [ ] Steam 로비/초대/릴레이 통합 전 로컬 2인 완주 테스트 케이스 확정

### 2026-03-05 18:53 (KST) 진행 스냅샷
- [x] 비주얼 프록시 패스 1차 완료 (플레이어/정거장/스크랩/테더 식별 색)
- [x] 우주 배경용 행성/소행성 프록시 배치 (콜라이더 제거)
- [x] 카메라 우주 톤 배경 적용
- [x] PlayMode 스모크 테스트 재검증 (치명 에러/예외 미검출)
- [ ] 실제 아트 에셋(모델/스카이박스/VFX) 수급 시 프록시 치환 파이프라인 적용
- [ ] FishNet 권한 경계 구현 1차 (durable/transient 분리 전송)

### 2026-03-05 21:19 (KST) 진행 스냅샷
- [x] 사용자 지정 에셋 반영: Earth(FBX), Player(FBX), Skybox1(mat)
- [x] Earth 프리팹 Missing Script 오류 우회 (프리팹 제거 -> FBX 기반 대체)
- [x] PlayMode 재검증 (Missing Script 오류 미재현, MCP 로그만 확인)
- [ ] 플레이어 2인 식별 색/코스튬 분리(현재 동일 모델)
- [ ] 로프 비주얼 에셋 후보 선정(기능과 네트워크 제약 분리 원칙 유지)

### 2026-03-05 21:35 (KST) 진행 스냅샷
- [x] `Unity-Verlet-Rope` 레퍼런스 클론 및 코드 검토
- [x] `TetherVerletRopeView` 구현/연결 (시각 전용)
- [x] `TetherSystem`에 Verlet View 적용, 기존 `TetherView` 비활성화
- [x] PlayMode 시각 검증 스크린샷 확보
- [ ] 2인 이동 중 로프 안정성(반대방향, 하드리밋) 수동 체감 테스트
- [ ] FishNet Host/Client에서 로프 시각 일관성 점검

### 2026-03-05 22:45 (KST) 진행 스냅샷
- [x] 테더 시각 시나리오 재검증(슬랙/원거리 하드리밋) 캡처
- [x] `TetherVerletRopeView` 상태 색상 반영 보강(거리 기반 tension 평가)
- [x] LineRenderer 동적 색상 재질 교체(`M_Tether_Dynamic.mat`)
- [x] Missing Script 진단 유틸 추가 및 활성 씬 스캔 완료(누락 없음)
- [ ] FishNet 패키지 도입
- [ ] FishNet Host/Client 시각 일관성 점검 (패키지 도입 후)

### 2026-03-05 23:16 (KST) 진행 스냅샷
- [x] 플레이어 이동 불가 원인 확인: `PlayerMotor`의 `_inputReader`/`_playerFuel` 씬 참조 누락
- [x] `PlayerMotor` 방어 패치: `Awake`에서 `GetComponent` 자동 연결
- [x] 에디터 누락 복구 보강: `OnValidate` 자동 연결 추가
- [x] Unity MCP 스크립트 검증/리프레시 수행 (컴파일 오류 없음)
- [ ] VerticalSlice_MVP 실제 조작 체감 재검증 (WASD/R/F, 마우스, Q/E)

### 2026-03-05 23:46 (KST) 진행 스냅샷
- [x] 마우스 감도 완화: `PlayerInputReader`에 `_lookSensitivity` 추가(기본 0.25)
- [x] 회전 과민 완화: `VerticalSlice_MVP`의 PlayerA/B `_lookAcceleration` 4 -> 2 조정
- [x] 카메라 모드 전환 구현: `PlayerCameraModeController` 추가 및 `Main Camera` 연결
- [x] 단축키 매핑 반영: `1=1인칭`, `2=전체시점`, `3=3인칭`
- [ ] PlayMode 체감 검증(시점별 조작감/멀미/가시성)

### 2026-03-06 00:14 (KST) 진행 스냅샷
- [x] FishNet 패키지 도입 (`Packages/manifest.json`, `packages-lock.json` 반영)
- [x] Netcode 어댑터 1차 구현:
  - `FishNetSessionService` 추가 (`ISessionService`)
  - `FishNetAuthorityGateway` 추가 (`INetworkAuthorityGateway`)
- [x] 씬 참조 전환:
  - `StationMatchController._sessionServiceBehaviour` -> `FishNetSessionService`
  - `PlayerA/B PlayerNetworkBridge._authorityGatewayBehaviour` -> `FishNetAuthorityGateway`
- [x] `MatchSystems`에 `NetworkManager` 추가(TransportManager 기본 Tugboat 경로)
- [x] 플레이어 식별성 보강:
  - `PlayerPresentation` 자동 참조 복구 추가
  - PlayerA/B 기본 색 분리(청록/오렌지)
- [x] PlayMode 스모크 재검증(게임 오류/예외 미검출, MCP 연결 로그 제외)
- [ ] FishNet 실제 플레이어 스폰/소유권/복제 경계(Host/Owner/Remote) 구현
- [ ] 로컬 2인 Host/Client 완주 테스트 시나리오 실행

### 2026-03-06 00:28 (KST) 진행 스냅샷
- [x] FishNet 씬 플레이어 슬롯 할당기 추가 (`FishNetScenePlayerAssigner`)
- [x] PlayerA/B 네트워크 컴포넌트 부착 (`NetworkObject`, `NetworkTransform`)
- [x] 소유권 입력 게이트 추가 (`PlayerOwnershipInputGate`)
- [x] `PlayerNetworkBridge.SetOwnerId(int)` 추가
- [x] ownerId 기본값 `-1` 적용(접속 전 입력 차단)
- [x] Host PlayMode 스모크: Tugboat server/client started, remote connection started 확인
- [ ] 2프로세스 Host/Client 실테스트(Owner 입력, Remote 관찰, 해제/재접속)
- [ ] 연료/수리/운반 상태의 durable/transient 동기화 분리 구현
- [ ] 테더 제약의 네트워크 의미(연결/길이/브레이크) 동기화 1차

### 2026-03-06 00:41 (KST) 진행 스냅샷
- [x] FishNet `NetworkObject` 재직렬화 자동화 유틸 추가
  - `Assets/Game/Netcode/Editor/FishNetOpenSceneReserializeUtility.cs`
  - 메뉴: `Tools/InterStella/Netcode/Reserialize Open Scene NetworkObjects`
- [x] `VerticalSlice_MVP` 열린 씬 기준 sceneId 재생성 수행
  - 로그 확인: `Checked 4 NetworkObjects over 1 scenes. 4 sceneIds were generated.`
- [x] PlayMode 재검증에서 기존 경고 미재현
  - 기존 경고: `TetherSystem ... expected to be initialized but was not`
- [ ] 2프로세스 Host/Client 실테스트(Owner 입력, Remote 관찰, 해제/재접속)
- [ ] 연료/수리/운반 상태의 durable/transient 동기화 분리 구현 고도화

### 2026-03-06 10:45 (KST) 진행 스냅샷
- [x] 입력 권한 방어 보강(동시 조작 비의도 동작 대응)
  - `FishNetAuthorityGateway`: 음수 owner/비정상 client connection 차단
  - `PlayerNetworkBridge`: `ownerId < 0` 즉시 비권한 처리
  - `PlayerInputReader`: 비활성화 시 입력 샘플 초기화(`ClearSample`, `OnDisable`)
  - `PlayerOwnershipInputGate`: 비활성 전 샘플 초기화 호출
- [x] PlayMode 스모크 재검증
  - 서버/클라 시작 로그 정상
  - 신규 컴파일/런타임 에러 미검출
- [ ] 체감 검증: “한 키로 양 플레이어 동시 직접 조작” 재현 여부 확인(사용자 플레이 확인)

### 2026-03-06 10:57 (KST) 진행 스냅샷
- [x] 2프로세스 대비 소유권 판정 보강
  - `PlayerNetworkBridge`가 `NetworkObject.IsOwner/OwnerId`를 직접 사용하도록 수정
- [x] 세션 런타임 오버라이드 추가
  - `FishNetSessionService`에 CLI/ENV 모드 전환 추가(`host/client/server`)
- [x] durable/transient 보강 1차
  - `PlayerFuelNetworkState`: 요청자 권한 검증 + 연료 점프값 검증
  - `RepairObjectiveNetworkState`: 변화 없을 때 재동기화 억제
  - `TetherNetworkStateReplicator`: 중복 상태 apply 억제
- [x] Host PlayMode 스모크 재검증(컴파일/런타임 치명 오류 없음)
- [ ] 2프로세스 Host/Client 자동 E2E 검증(MCP 단일 인스턴스 노출 제약 해소 후 재실행)
- [ ] 2프로세스 수동 검증(Owner 입력, Remote 관찰, 해제/재접속 슬롯 복구) 결과 캡처

### 2026-03-06 10:59 (KST) 진행 스냅샷
- [x] 슬롯 이벤트 로그 추가 (`FishNetScenePlayerAssigner`)
- [x] Host 실행 시 슬롯 할당 로그 확인
  - `Assigned client 0 to slot 0 (PlayerA)`
- [ ] 재접속 시 슬롯 회수/재할당 로그 확인(2프로세스 환경 필요)

### 2026-03-06 11:24 (KST) 진행 스냅샷
- [x] Unity MCP 소스 업데이트(`.tmp/unity-mcp-src` -> `beta@1fde301`)
- [x] 임베디드 패키지 동기화(`Packages/com.coplaydev.unity-mcp`)
- [x] 버전 상승 확인(`9.4.8-beta.17` -> `9.4.8-beta.19`)
- [x] Unity 런타임 로그 확인
  - `server=9.4.8-beta.19`
  - `Updated stdio MCP configs to package version 9.4.8-beta.19.`

### 2026-03-06 11:27 (KST) 진행 스냅샷
- [x] 다음 작업 착수 전 현재 상태 재점검 완료
  - 활성 씬: `Assets/Game/Scenes/VerticalSlice/VerticalSlice_MVP.unity`
  - 핵심 네트워크 대상 컴포넌트 존재 확인:
    - `PlayerFuelNetworkState` (PlayerA/B)
    - `RepairObjectiveNetworkState` (RepairStation)
    - `TetherNetworkStateReplicator` (TetherSystem)
    - `FishNetScenePlayerAssigner` (MatchSystems)
- [x] 콘솔 기준 치명 오류/예외 미검출 (MCP 연결 로그 외)
- [ ] 2프로세스 Host/Client 실검증
  - Owner만 입력 가능
  - Remote는 관찰만 가능
  - 연결 해제/재접속 시 슬롯 회수/재할당 로그 확인
- [ ] durable vs transient 전송 경계 2차 보강
  - 현재 durable: 연료/수리/테더 SyncVar 경로 확인
  - 남은 과제: transient 이벤트(픽업/드롭/브레이크 등) 중복/누락 방어 검증

### 2026-03-06 11:42 (KST) 진행 스냅샷
- [x] ECC 부분 설치(프로젝트 맞춤) 적용
  - `.codex/config.toml` 추가
  - `Docs/Codex Workflow Pack.md` 추가
- [x] 표준 작업 루프/네트워크 DoD/명령 스니펫 기준선 문서화
- [ ] 실검증 적용
  - 2프로세스 Host/Client 검증을 신규 DoD 체크리스트 기준으로 재실행
  - 슬롯 회수/재할당 로그 캡처 누적

### 2026-03-06 11:47 (KST) 진행 스냅샷
- [x] ECC 부분 설치 상세 노트 신규 작성
  - `Docs/ECC 부분 설치 상세 가이드.md`
  - 포함: 설치 이유, 설치 항목, 활용법, 체크리스트
- [x] Obsidian 새 노트 반영 준비 완료(동기화 단계 진행)

### 2026-03-06 12:20 (KST) 진행 스냅샷
- [x] Host PlayMode 재검증 로그 수집
  - `Starting session mode=Host`
  - `Assigned client 0 to slot 0 (PlayerA)`
- [x] 2프로세스 검증 시도
  - Unity 추가 프로세스 client 인자로 실행 시도(`-interstella-mode client ...`)
  - 로컬 포트/프로세스 기준 다중 Unity 프로세스 존재 확인
- [ ] 2프로세스 실검증 완료
  - 현재 MCP가 제어 가능한 인스턴스는 `interStella@b68d5cf0 (port 6401)` 1개만 확인
  - Host 콘솔에서 `Remote connection started for Id 1` 미확인 (현재 `Id 0`만 확인)
- [x] ECC 영향 관찰 기록
  - DoD 기준(Owner/Remote/Reconnect)을 우선 체크포인트로 고정
  - 실행 시도도 로그 근거(세션 시작/슬롯 할당/원격 ID) 중심으로 수집

### 2026-03-06 12:43 (KST) 진행 스냅샷
- [x] 2프로세스 Host/Client 실검증 성공 (클론 프로젝트 경로 활용)
  - Host: `C:\Unity\interStella` (MCP 제어)
  - Client: `C:\Unity\interStellaClient` (Unity 별도 프로세스)
- [x] 접속/슬롯 할당 검증
  - `Remote connection started for Id 1`
  - `Assigned client 1 to slot 1 (PlayerB)`
- [x] 연결 해제/재접속 슬롯 복구 검증
  - `Id [1] Address [127.0.0.1] has timed out`
  - `Released slot 1 from client 1; ownership removed from PlayerB`
  - `Remote connection stopped for Id 1`
  - `Remote connection started for Id 2`
  - `Assigned client 2 to slot 1 (PlayerB)`
- [x] 검증 보조 유틸 추가
  - `Assets/Game/Netcode/Editor/InterStellaClientAutoPlayBootstrap.cs`
  - 목적: `-executeMethod`로 클라이언트 에디터 자동 Play 진입
- [ ] 잔여 검증
  - Owner/Remote 입력 체감(조작 반응) 수동 검증 캡처
  - transient 이벤트(스크랩 픽업/드롭) 네트워크 경계 보강

### 2026-03-06 13:00 (KST) 진행 스냅샷
- [x] transient 상호작용 요청 경계 추가
  - 신규: `Assets/Game/Netcode/Runtime/PlayerInteractionNetworkRelay.cs`
  - `ServerRpc(RequireOwnership=true)`로 Owner 요청만 허용
  - 서버 쿨다운(`_serverRequestCooldown`)으로 과도 요청 중복 방어
- [x] Player 상호작용 권한 게이트 정합성 보정
  - `Assets/Game/Features/Player/PlayerInteraction.cs`
  - `Assets/Game/Features/Player/PlayerOwnershipInputGate.cs`
- [x] 씬 구성 반영
  - `VerticalSlice_MVP`의 `PlayerA/PlayerB`에 `PlayerInteractionNetworkRelay` 부착
- [x] 스크립트 검증
  - 수정 파일 3개 `validate_script` 통과(에러 0)
- [ ] 실플레이 검증(2프로세스)
  - Client Owner가 `E` 입력 시 Host에서 상호작용(픽업/드롭/수리) 처리되는지 로그/체감 확인
- [ ] 잔여 네트워크 보강
  - 스크랩 durable 시각 상태(운반/월드/납품) 동기화 컴포넌트 설계 및 적용

### 2026-03-06 13:06 (KST) 진행 스냅샷
- [x] 스크랩 durable 동기화 컴포넌트 추가
  - `Assets/Game/Netcode/Runtime/ScrapCarryNetworkState.cs`
- [x] 스크랩 authoritative 상태 적용 메서드 보강
  - `Assets/Game/Features/Scavenge/ScrapItem.cs`
  - `SetCarriedStateAuthoritative`, `SetWorldStateAuthoritative`
- [x] 납품 처리 안정화
  - `MarkDelivered`에서 `gameObject.SetActive(false)` 제거
  - 렌더러/콜라이더 기반 비활성으로 전환
- [x] 씬 네트워크 구성 확장
  - `Scrap_01~03`에 `NetworkObject + ScrapCarryNetworkState` 부착
- [x] FishNet 재직렬화 및 저장
  - 로그: `Checked 7 NetworkObjects over 1 scenes. 7 sceneIds were generated.`
- [x] 컴파일/스모크 검증
  - 스크립트 검증 통과, PlayMode 경고/에러 없음(MCP 연결 로그 제외)
- [ ] 2프로세스 상호작용 실검증
  - Client Owner의 픽업/드롭/수리 동작을 Host/Remote에서 체감 확인

### 2026-03-06 15:12 (KST) 진행 스냅샷
- [x] 재시작 이후 2프로세스 네트워크 재검증 완료
  - 접속(Id 1) -> 타임아웃 해제 -> 재접속(Id 2) 로그 재확인
- [x] disconnect 시 운반 스크랩 강제 드롭 반영
  - `Assets/Game/Features/Scavenge/PlayerCarrySocket.cs`
  - `Assets/Game/Netcode/Runtime/FishNetScenePlayerAssigner.cs`
- [x] 슬롯 해제 경로에서 강제 드롭 로그 확인
  - `Forced scrap drop on disconnect for client 1 at slot 1`
- [x] 재접속 슬롯 복구 로그 확인
  - `Assigned client 2 to slot 1 (PlayerB)`
- [x] 매치 시작 리셋 안정화 반영
  - `Assets/Game/Features/Player/PlayerFuel.cs`
  - `Assets/Game/Features/Stations/StationMatchController.cs`
- [x] 테스트 유틸 추가
  - `Assets/Game/Netcode/Editor/InterStellaNetcodeDebugActions.cs`
- [x] `ScrapItem` kinematic velocity 경고 완화 패치
  - `Assets/Game/Features/Scavenge/ScrapItem.cs`
- [ ] 실플레이 체감 검증(수동 입력)
  - Client Owner `E` 입력으로 픽업/드롭/수리 루프를 직접 수행해 최종 체감/영상/스크린샷 확보

### 2026-03-06 16:12 (KST) 진행 스냅샷
- [x] 2프로세스 Owner 상호작용 RPC 경로 검증(자동)
  - Host 로그: `[PlayerInteractionNetworkRelay] Accepted interaction request. caller=1, owner=1, committed=True, object=PlayerB`
  - Client 로그: `auto-interact attempt 1/24, accepted=True, owner=PlayerB`
- [x] auto-interact 부트스트랩 안정화
  - 도메인 리로드 복구(SessionState)
  - `-interstella-auto-interact 0/false` 파싱 버그 수정
- [x] 디버그 검증 유틸 확장
  - `Force Start Match`
  - `Place Scrap_03 In Front Of PlayerB`
- [x] disconnect carry-drop 경계 재검증
  - `Forced scrap drop on disconnect for client 1 at slot 1`
  - `Released slot 1 from client 1; ownership removed from PlayerB`
- [x] reconnect 슬롯 복구 재확인
  - `Assigned client 1 to slot 1 (PlayerB)` (ID 재사용 케이스)
- [ ] 수동 체감 검증(사용자 입력)
  - 실제 키입력 기준 1인칭/3인칭 전환, 이동 감도, 수리 완료까지 플레이 체감 캡처

### 2026-03-06 16:18 (KST) 진행 스냅샷
- [x] `-interstella-auto-interact 0` 비활성 파싱 검증
  - auto-interact 로그 미발생 확인
  - `Assigned client 1 to slot 1 (PlayerB)` 접속 경로는 정상

### 2026-03-06 21:28 (KST) 진행 스냅샷
- [x] 수리(Repair) 자동 상호작용 검증 경로 추가
  - `-interstella-auto-interact-count` 구현
  - `Place RepairStation In Front Of PlayerB` 디버그 메뉴 구현
- [x] 2회 자동 상호작용(픽업+수리 납품) 로그 검증
  - Host: `committed=True` 2회
  - Host: `[RepairStationObjective] Delivery accepted. delivered=1/3`
- [x] disconnect/reconnect 회귀 재검증
  - `Forced scrap drop on disconnect ...`
  - `Released slot ... ownership removed ...`
  - 재접속 `Assigned client 1 to slot 1 (PlayerB)`
- [x] auto-interact OFF 경로 재확인
  - `-interstella-auto-interact 0`에서 auto-interact 로그 미발생
- [ ] 수동 체감 검증(사용자 입력)
  - 이동 손맛/시점 전환/수리 완주를 실제 조작으로 체감 확인

### 2026-03-06 22:06 (KST) 진행 스냅샷
- [x] GitHub MCP 연결 설정 반영
  - Codex config에 `mcp_servers.github` 추가
- [x] 자동 브랜치/커밋/풀 워크플로우 스크립트 추가
  - `auto-branch.ps1`
  - `auto-commit.ps1`
  - `auto-pull.ps1`
  - `auto-workflow.ps1`
- [x] MCP 연결 체크 스크립트 추가
  - `setup-github-mcp.ps1`
- [x] 워크플로우 안내 문서 추가
  - `Docs/GitHub MCP & Git Workflow.md`
- [ ] Git 저장소 활성화
  - 현재 `C:\Unity\interStella`에 `.git`이 없어 스크립트는 보호 중단 동작

### 2026-03-06 22:21 (KST) 진행 스냅샷
- [x] Git 저장소 활성화
  - git init 후 초기 커밋 완료 (711deb0)
- [x] GitHub 원격 저장소 생성
  - gargang2a/interStella 생성
- [x] main 브랜치 푸시 및 추적 설정
  - main...origin/main 동기화 확인
- [x] 대용량 파일 푸시 실패 리스크 제거
  - *.unitypackage ignore 추가
  - SpaceSkyboxes4K.unitypackage 인덱스 제외
- [ ] 다음 단계
  - 자동 브랜치/자동 커밋/자동 풀 스크립트를 실제 기능 작업 브랜치에서 1회 E2E 검증

### 2026-03-06 22:54 (KST) 진행 스냅샷
- [x] Git 자동 워크플로우 E2E 검증 (브랜치 단위)
  - auto-branch: codex/git-workflow-e2e 생성/전환 성공
  - auto-commit: 문서 변경 자동 add/commit 대상 준비
  - auto-pull: upstream 기반 pull 경로 검증 예정
- [ ] 후속
  - PR 생성 규칙(완료 기준 충족 시) 실제 1회 적용

### 2026-03-06 22:56 (KST) 진행 스냅샷
- [x] 자동 브랜치/자동 커밋/자동 풀 스크립트 E2E 검증
  - branch: codex/git-workflow-e2e
  - commit/push: bee3248
  - pull: PULL_COMPLETED
- [x] PR 자동 생성(완료 단위)
  - PR: https://github.com/gargang2a/interStella/pull/1
- [ ] 다음 단계
  - 네트워크/플레이 루프 수동 체감 검증(이동 손맛/시점/수리 완주)

### 2026-03-06 23:22 (KST) 진행 스냅샷
- [x] durable vs transient 전송 경계 2차 보강
  - PlayerFuelNetworkState: submit sequence + ownership 강제 강화
  - RepairObjectiveNetworkState: delivery transient 이벤트 분리
  - TetherNetworkStateReplicator: break transient sequence 도입
- [x] 스크립트 검증 완료
  - validate_script(3/3) errors=0, warnings=0
- [ ] 다음 단계
  - 2프로세스 Host/Client 실검증(Owner 입력, Remote 관찰, reconnect 슬롯/상태 복구)

### 2026-03-06 23:28 (KST) 진행 스냅샷
- [x] netcode 경계 보강 브랜치 커밋/푸시 완료
  - commit: 7b2bef2
- [x] PR 생성 완료
  - PR #2: https://github.com/gargang2a/interStella/pull/2
- [ ] 다음 단계
  - PR #2 리뷰/머지 후 2프로세스 실검증 재실행

### 2026-03-07 00:08 (KST) 진행 스냅샷
- [x] 2프로세스 Host/Client 재검증(Owner 상호작용)
  - Host: `Remote connection started for Id 1`
  - Host: `Assigned client 1 to slot 1 (PlayerB)`
  - Host: `PlayerInteractionNetworkRelay ... committed=True` (2회)
  - Host: `[RepairStationObjective] Delivery accepted. delivered=1/3`
  - Client: `auto-interact attempt ... successes=2/2`
- [x] durable/transient 오류 필터 점검
  - Host 콘솔 `PacketId`, `unhandled` 0건
- [x] disconnect 슬롯 해제 경계 재확인
  - `Id [1] Address [127.0.0.1] has timed out`
  - `Released slot 1 from client 1; ownership removed from PlayerB`
- [ ] reconnect 최종 완료 재확인(동일 세션)
  - 목표 로그: `Assigned client <newId> to slot 1 (PlayerB)`
  - 현상: 재실행 Client가 라이선싱 초기화 단계에서 접속 로그 미출력

### 2026-03-07 00:13 (KST) 진행 스냅샷
- [x] 문서/브랜치 진행 상태 원격 반영
  - branch: `codex/host-client-e2e-recheck`
  - commit: `1474ae6`
  - push 완료
- [x] 환경 블로커 원인 분리
  - Client 재실행 실패 원인: Unity Licensing 초기화 타임아웃(code 199)
  - 범주: 네트코드 로직 이슈 아님(에디터 실행 환경)
- [ ] reconnect 최종 완료 재확인
  - 라이선싱 정상화 후 동일 시나리오 재실행 필요

### 2026-03-07 00:16 (KST) 진행 스냅샷
- [x] reconnect 최종 검증 재시도 2회
  - 결과: Client 라이선싱 타임아웃(code 199)으로 검증 단계 진입 실패
- [x] 라이선싱 프로세스 재기동 조치
  - `Unity.Licensing.Client` 재기동 후에도 동일 증상
- [ ] reconnect 최종 완료 재확인
  - 선행조건: Client Editor 라이선싱 정상 진입

### 2026-03-07 00:24 (KST) 진행 스냅샷
- [x] netcode sequence 공통 비교 유틸 추가
  - `Assets/Game/Netcode/Runtime/NetworkSequenceComparer.cs`
- [x] Fuel/Tether sequence 판정 보강
  - `PlayerFuelNetworkState`: submit dedupe 기준 공통화
  - `TetherNetworkStateReplicator`: break sequence wrap-around 안전 비교
- [x] EditMode 테스트 추가 및 통과(6/6)
  - `NetworkSequenceComparerTests`
- [ ] reconnect 최종 완료 재확인
  - Client Editor 라이선싱 블로커(code 199) 해소 후 재시도 필요

### 2026-03-07 00:33 (KST) 진행 스냅샷
- [x] 라이선싱 채널 우회 재시도
  - Hub client를 `--namedPipe LicenseClient-gar`로 직접 실행
- [x] 재시도 결과 수집
  - `client-reconnect-check4-20260307-002847.log`
  - 실패 패턴: channel not exist -> refused -> timeout(code 199)
- [ ] reconnect 최종 완료 재확인
  - Unity 라이선싱 IPC 정상화 이후 재실행 필요

### 2026-03-07 00:39 (KST) 진행 스냅샷
- [x] reconnect 재시도(check5)
  - 로그: `Logs/client-reconnect-check5-20260307-003438.log`
  - 결과: licensing channel refused -> timeout(code 199)
- [x] 라이선싱 프로세스 정리
  - 보조 인스턴스 종료, 기본 에디터 채널 인스턴스만 유지
- [ ] reconnect 최종 완료 재확인
  - 환경 복구 후 재실행 필요

### 2026-03-07 00:47 (KST) 진행 스냅샷
- [x] PR 생성
  - PR #3: https://github.com/gargang2a/interStella/pull/3
- [x] PR 상태 명시
  - reconnect 최종 검증은 licensing IPC blocker로 pending
- [ ] merge 여부 결정
  - reconnect 완료 로그 확보 후 머지 또는 blocker 명시 상태로 머지 판단

### 2026-03-07 00:58 (KST) 진행 스냅샷
- [x] Owner/Remote 입력 경계 강화(이중 가드)
  - 파일: `Assets/Game/Features/Player/PlayerMotor.cs`
  - 변경: non-owner인 경우 입력 기반 시뮬레이션/연료 소모 경로 차단
- [x] 스크립트 검증
  - `PlayerMotor.cs` validate_script 통과
- [ ] reconnect 최종 완료 재확인
  - 라이선싱 IPC 블로커 해소 후 재실행

### 2026-03-07 01:05 (KST) 진행 스냅샷
- [x] reconnect 재시도(check6)
  - 로그: `Logs/client-reconnect-check6-20260307-010236.log`
  - 결과: channel not exist/refused -> timeout(code 199)
- [ ] reconnect 최종 완료 재확인
  - 환경(licensing IPC) 정상화 필요

### 2026-03-07 02:31 (KST) 진행 스냅샷
- [x] Unity 라이선싱 재진입 경로 복구
  - client-reconnect-check10-20260307-021055.log 기준 Client Editor가 code 199 없이 정상 진입
  - FishNetSessionService Starting session mode=ClientOnly + auto-interact 로그 재확인
- [x] Netcode 컴파일 경고 제거(CS0114)
  - PlayerFuelNetworkState, RepairObjectiveNetworkState, TetherNetworkStateReplicator의 OnValidate를 protected override로 수정
- [x] Host 권한 커밋 경계 버그 수정
  - 파일: Assets/Game/Netcode/Runtime/FishNetAuthorityGateway.cs
  - 수정: Host(ServerStarted)에서는 검증된 원격 요청을 authoritative commit 가능하도록 변경
- [x] 재접속 슬롯 경합(race) 버그 수정
  - 파일: Assets/Game/Netcode/Runtime/FishNetScenePlayerAssigner.cs
  - 수정: 슬롯 부족 시 pending queue 보관 후, 슬롯 해제 시 자동 재할당
  - 신규 로그: Queued pending client 2 for next available slot., No available slot for client 2. Queued for reassignment.
- [x] 실검증 로그 확보(Host Editor.log)
  - Id [1] ... timed out 이후 Released slot 1 ... 다음에 Assigned client 2 to slot 1 (PlayerB) 확인
  - 원격 상호작용 커밋 committed=True 및 Delivery accepted. delivered=1/3 케이스 재확인
- [ ] 후속
  - interStellaClient 복제 프로젝트와 본 프로젝트 코드 동기화 자동화(검증 일관성용)

### 2026-03-07 02:46 (KST) 원격 rewrite 동기화
- [x] 원격 상태 재검증
  - GitHub API 확인: visibility=public, main=cd99e0c (docs: add copilot review instructions)
  - ruleset 확인: PR Reaview, target=branch, enforcement=active
- [x] 안전 동기화 수행
  - 현재 브랜치 백업 생성: codex/licensing-ipc-recovery-pre-sync-20260307-024024
  - codex/licensing-ipc-recovery를 origin/main 기준으로 rebase
  - 중복 패치(commit 8b98947)는 upstream 반영 상태로 자동 skip
- [x] 원격 브랜치 정렬
  - git push --force-with-lease origin codex/licensing-ipc-recovery 완료
  - 현재 분기 상태: origin/main 대비 ahead 1 (d3f60e7)
- [x] PR 상태 재검증
  - PR #4 head=d3f60e7, base=cd99e0c
  - mergeable_state=clean

### 2026-03-07 02:46 (KST)
- force-push rewrite 이후 안전 동기화 체크리스트:
  1. git status로 미커밋 변경 유무 확인
  2. git fetch --prune --tags로 원격 ref 갱신
  3. 작업 브랜치 백업 브랜치 생성
  4. 최신 origin/main 기준 rebase
  5. git push --force-with-lease로 원격 브랜치 업데이트
  6. PR head/base 및 mergeable 상태 재확인
- 원칙:
  - rewrite 이후에는 일반 push 대신 --force-with-lease 사용
  - 미커밋 변경이 있으면 먼저 보존(backup/stash) 후 진행

### 2026-03-07 03:16 (KST) 진행 스냅샷
- [x] PR #4 머지 완료
  - merge commit: a859dc7 (`merge: PR #4 licensing-ipc-recovery`)
  - main 최신 기준선으로 로컬 동기화 완료
- [x] 다음 작업 브랜치 생성
  - branch: codex/client-sync-automation
- [x] interStellaClient 동기화 자동화 스크립트 추가
  - `.codex/workflows/client/sync-interstella-client.ps1`
  - 기본 동작: `Assets/Game`, package manifest/lock, 핵심 ProjectSettings 파일 동기화
  - 실행 검증: dry-run + 실제 동기화 완료
- [x] 재접속 회귀 자동 판정 스크립트 추가
  - `.codex/workflows/netcode/run-reconnect-regression.ps1`
  - 시나리오: Client A 접속 -> 강제 종료 -> Client B 조기 재접속 -> Host 로그 패턴 검증
- [x] 회귀 자동 판정 실행 결과
  - `RECONNECT_REGRESSION_PASS`
  - summary: `Logs/reconnect-regression-summary-20260307-031141.json`
  - releasedClientId=1, reassignedClientId=2
- [ ] 후속
  - 스크립트를 `.codex/workflows` 루트 인덱스 문서에 통합 안내

### 2026-03-07 03:19 (KST) 진행 스냅샷
- [x] `.codex/workflows` 루트 인덱스 문서 추가
  - `.codex/workflows/README.md`
  - Git/Client Sync/Netcode Regression 스크립트 경로 통합 안내

### 2026-03-07 03:36 (KST) 진행 스냅샷
- [x] one-command E2E 래퍼 추가
  - `.codex/workflows/netcode/run-e2e-sync-regression.ps1`
  - 기능: `client sync -> reconnect regression` 순차 실행
- [x] 래퍼 안정성 보강
  - Host preflight: UDP 7770 listening 체크 (미충족 시 즉시 실패)
  - regression 재시도 옵션: `-RegressionMaxAttempts`, `-RetryDelaySec`
- [x] 실제 실행 검증
  - 명령: `run-e2e-sync-regression.ps1 -RegressionMaxAttempts 3 -RetryDelaySec 10`
  - 결과: `E2E_SYNC_REGRESSION_PASS`
  - summary: `Logs/reconnect-regression-summary-20260307-033146.json`
  - releasedClientId=1, reassignedClientId=2

### 2026-03-07 12:52 (KST) 진행 스냅샷
- [x] Git 자동화에 PR 생성 단계 추가
  - 신규: .codex/workflows/git/auto-pr.ps1
  - 기능: GITHUB_PAT_TOKEN 존재 시 GitHub REST API로 PR 생성, 미존재 시 Compare URL 출력
- [x] one-shot 워크플로우에 PR 연동 옵션 추가
  - 수정: .codex/workflows/git/auto-workflow.ps1
  - 신규 옵션: -CreatePr, -PrBase, -PrTitle, -PrBody, -PrDraft
- [x] 워크플로우 문서 갱신
  - .codex/workflows/git/README.md
  - .codex/workflows/README.md
- [x] 실행 검증
  - 명령: powershell -ExecutionPolicy Bypass -File .\.codex\workflows\git\auto-pr.ps1 -Base main
  - 결과: TOKEN_MISSING_GITHUB_PAT_TOKEN + PR_CREATE_URL=.../compare/main...codex/e2e-workflow-orchestrator?expand=1
- [ ] 후속
  - 사용자 환경변수 GITHUB_PAT_TOKEN 설정 후 API 기반 PR 자동 생성까지 실검증

### 2026-03-07 16:56 (KST) 진행 스냅샷
- [x] 브랜치 반영
  - branch: codex/e2e-workflow-orchestrator
  - commit: 84f961e (feat: add auto-pr workflow and fallback compare URL)
  - push: origin 반영 완료
- [x] PR 존재 여부 확인
  - GitHub API 조회 결과: OPEN_PR_NONE
- [ ] PR 생성
  - 토큰 미설정(GITHUB_PAT_TOKEN)으로 자동 생성 대신 URL fallback 사용
  - 생성 URL: https://github.com/gargang2a/interStella/compare/main...codex/e2e-workflow-orchestrator?expand=1

### 2026-03-07 17:36 (KST) 진행 스냅샷
- [x] PR #6 생성 및 머지 완료
  - PR: https://github.com/gargang2a/interStella/pull/6
  - merge commit: f3a33c6 (`merge: PR #6 e2e-workflow-orchestrator`)
- [x] `main` 기준선 동기화 완료
  - local main = origin/main = f3a33c6
- [x] Host/Client E2E 재검증 PASS (머지 후)
  - 명령: `run-e2e-sync-regression.ps1 -RegressionMaxAttempts 3 -RetryDelaySec 10`
  - 결과: `E2E_SYNC_REGRESSION_PASS`
  - summary: `Logs/reconnect-regression-summary-20260307-173410.json`
  - releasedClientId=2, reassignedClientId=4
- [x] 운영 블로커 해소
  - Unity MCP로 Host Play 모드 진입 후 UDP 7770 listening 확인
  - 검증 종료 후 Play 모드 stop 처리
- [ ] 다음 단계
  - Steam 로비/초대/릴레이 통합 전 체크리스트 고정
  - 2프로세스 수동 체감 검증 결과 캡처(시점 전환/이동/수리 완주)

### 2026-03-07 18:56 (KST) 진행 스냅샷
- [x] 무료 플랜 대체 PR 리뷰 봇 워크플로우 추가
  - 파일: `.github/workflows/pr-guardrails-review.yml`
  - 트리거: pull_request(opened/reopened/synchronize/ready_for_review)
- [x] 자동 코멘트 방식
  - PR 변경 패치 휴리스틱 점검 후 marker 기반 upsert 코멘트 생성/갱신
- [ ] 후속
  - 워크플로우 머지 후 PR #8 또는 신규 PR에서 Actions 실행/코멘트 생성 실검증

### 2026-03-07 19:16 (KST) 진행 스냅샷
- [x] fallback PR 리뷰 봇 실검증 완료
  - PR #9에서 워크플로우 실행 성공: `PR Guardrails Review`
  - run: https://github.com/gargang2a/interStella/actions/runs/22797087540
  - 코멘트 작성자: `github-actions[bot]` (`<!-- interstella-pr-guardrails -->`)
- [x] main 머지 후 기존 PR 재검증 완료
  - PR #8에 재트리거 커밋 push 후 동일 워크플로우 실행 성공
  - run: https://github.com/gargang2a/interStella/actions/runs/22797135237
  - guardrails 코멘트 생성 확인
- [x] 테스트 PR 정리
  - PR #8 closed (probe 목적 완료, 비머지)
- [ ] 후속
  - 필요 시 guardrails 휴리스틱 룰(권한/핫패스/테더 의미) 프로젝트 맞춤 확장

### 2026-03-07 19:25 (KST) 진행 스냅샷
- [x] Steam 통합 전 게이트 체크리스트 v1 고정 (SSOT)
  - 기준 문서: `Docs/Build Guide.md`의 "Steam 통합 전 게이트 체크리스트 v1 (SSOT)"
- [x] 범위/비지원 명시
  - MVP 범위: 2인, Host-authoritative, VerticalSlice 1개 루프
  - 비지원: late join, host migration
- [ ] Go/No-Go 판정 실행
  - 체크리스트 항목별 PASS/FAIL 실제 입력
  - P1 이슈 0건 확인
- [ ] 수동 체감 검증 캡처
  - 1/2/3 시점 전환, 이동 체감, 수리 납품 루프

### 2026-03-07 19:37 (KST) 진행 스냅샷
- [x] Steam 게이트 실행 v1 (자동 검증)
  - E2E PASS: `Logs/reconnect-regression-summary-20260307-193420.json`
  - 운영 품질 PASS: open P1=0, guardrails run success
- [ ] 수동 검증 라운드(필수)
  - Gate 2: Owner 입력/Remote 관찰 경계
  - Gate 3: 수집/운반/수리 루프 1회 완주
  - Gate 4: 1/2/3 시점 전환 + 조작감/가시성
- [ ] 최종 Go/No-Go 갱신
  - Gate 2/3/4 PASS 시 Steam 통합 착수

### 2026-03-07 20:03 (KST) 진행 스냅샷
- [x] Gate 2/3 자동 증거 수집 스크립트 추가
  - `.codex/workflows/netcode/run-interaction-regression.ps1`
  - `.codex/workflows/netcode/README.md` 사용법 반영
- [x] 상호작용 회귀 재검증 PASS
  - summary: `Logs/interaction-regression-summary-20260307-200114.json`
  - assignedClientDetected=true
  - ownerBoundaryPass=true
  - acceptedCommittedCount=2
  - deliveryAcceptedCount=1
- [x] Steam Gate 판정 갱신
  - Gate 2 PASS(로그 기반)
  - Gate 3 PASS(최소 1회 납품)
- [ ] 잔여
  - Gate 4(1/2/3 시점 전환 + 조작감/가시성) 수동 체감 검증

### 2026-03-07 20:42 (KST) 진행 스냅샷
- [x] Gate 4 검증 보조 코드 추가
  - `PlayerCameraModeController`에 디버그 강제 전환 API 추가
    - `SetFirstPersonMode`, `SetOverviewMode`, `SetThirdPersonMode`, `ForceSnapToCurrentMode`
  - `InterStellaNetcodeDebugActions`에 카메라 메뉴 추가
    - `Tools/InterStella/Debug/Camera/Set First Person`
    - `Tools/InterStella/Debug/Camera/Set Overview`
    - `Tools/InterStella/Debug/Camera/Set Third Person`
    - `Tools/InterStella/Debug/Camera/Run Mode Smoke (1-2-3)`
- [x] Gate 4 스모크 실행/증거 확보
  - 콘솔: `[InterStella][CameraSmoke] PASS firstDistance=0.35 thirdDistance=4.22 overviewHeight=18.00 mode=Overview`
  - 스크린샷:
    - `Assets/Screenshots/gate4_camera_firstperson_v2.png`
    - `Assets/Screenshots/gate4_camera_thirdperson_v2.png`
    - `Assets/Screenshots/gate4_camera_overview_v2.png`
- [x] Steam Gate 판정 갱신
  - Gate 4 PASS(카메라 전환/가시성 스모크 기준)
  - Gate 1~5 전체 PASS -> Steam 통합 착수 가능 상태
- [ ] 후속(권장)
  - 사용자 장시간 체감 라운드(멀미/조작 피로) 1회 추가 기록

### 2026-03-07 21:12 (KST) 진행 스냅샷
- [x] Steam 통합 준비용 세션 부트스트랩 경계 추가
  - 파일: `Assets/Game/Netcode/Runtime/FishNetSessionService.cs`
  - `ConnectionProvider` 추가: `Direct`, `SteamRelay`
  - 런타임 인자/환경변수 파싱 추가:
    - `-interstella-provider` / `INTERSTELLA_PROVIDER`
    - `-interstella-lobby-id` / `INTERSTELLA_STEAM_LOBBY_ID`
    - `-interstella-steam-host-id` / `INTERSTELLA_STEAM_HOST_ID`
- [x] 명시적 비지원 처리 반영
  - 현재 `SteamRelay` 선택 시 실제 Steam transport wiring 미구현 상태를 경고 로그로 명시
  - `_allowSteamFallbackToDirect=false`면 세션 시작 차단(침묵 fallback 금지)
- [x] 검증
  - `validate_script`: `FishNetSessionService.cs` errors/warnings 0
  - PlayMode 로그: `[FishNetSessionService] Starting session provider=Direct, mode=Host, address=127.0.0.1, port=7770.`
- [ ] 다음 단계
  - Steam lobby/초대/릴레이 실제 어댑터(`ISessionService` 교체 가능 구조) 구현

### 2026-03-07 21:26 (KST) 진행 스냅샷
- [x] `ISessionService` 교체형 Steam 어댑터 1차 구현
  - 신규 파일: `Assets/Game/Netcode/Runtime/SteamSessionService.cs`
  - 상태머신: `Idle -> InvitePending -> LobbyReady -> SessionActive -> Failed`
  - 핵심 메서드:
    - `QueueInvite(lobbyId, hostSteamId)`
    - `TryJoinLobby(lobbyId, hostSteamId)`
    - `TryCreateHostLobby()`
    - `StartSession()` / `StopSession()`
- [x] FishNet 부트스트랩 주입 경계 공개
  - `FishNetSessionService`에 공개 메서드 추가:
    - `UseDirectBootstrap()`
    - `UseSteamBootstrap(lobbyId, hostId, allowDirectFallback)`
  - 읽기용 상태 노출:
    - `ActiveConnectionProvider`, `ActiveSteamLobbyId`, `ActiveSteamHostId`
- [x] 검증
  - `validate_script`:
    - `FishNetSessionService.cs` 통과
    - `SteamSessionService.cs` 통과
  - Unity refresh/compile 통과
- [ ] 다음 단계
  - `VerticalSlice_MVP`에서 `StationMatchController._sessionServiceBehaviour`를 `SteamSessionService`로 전환한 통합 스모크 1회
  - Steam SDK/Transport 실제 wiring 시 `SteamRelay` 경로 실동작 검증

### 2026-03-07 21:32 (KST) 진행 스냅샷
- [x] Steam 어댑터 상태 전이 EditMode 테스트 추가
  - 신규: `Assets/Game/Netcode/Editor/Tests/SteamSessionServiceTests.cs`
  - 케이스:
    - `StartSession_HostAutoCreateLobby_Succeeds`
    - `StartSession_ClientWithoutInvite_Fails`
    - `StartSession_ClientWithQueuedInvite_Succeeds`
- [x] 테스트 실행 결과
  - EditMode 전체: total=9, passed=9, failed=0
  - 기존 `NetworkSequenceComparerTests` 6건 + 신규 `SteamSessionServiceTests` 3건 통과
- [ ] 다음 단계
  - Steam 어댑터 씬 통합(서비스 참조 전환) 전/후 회귀 스모크 비교 로그 누적

### 2026-03-07 21:45 (KST) 진행 스냅샷
- [x] Steam 어댑터 씬 통합 1차 완료
  - `VerticalSlice_MVP`의 `MatchSystems`에 `SteamSessionService` 컴포넌트 추가
  - 씬 저장: `Assets/Game/Scenes/VerticalSlice/VerticalSlice_MVP.unity`
- [x] 세션 서비스 해석 경계 보강
  - 파일: `Assets/Game/Features/Stations/StationMatchController.cs`
  - 동작: 씬에 `SteamSessionService`가 존재하면 `_sessionServiceBehaviour` 설정값보다 우선 사용
- [x] 통합 PlayMode 스모크 PASS (로그 체인)
  - `[StationMatchController] Session service switched to discovered SteamSessionService.`
  - `[SteamSessionService] Host lobby created ...`
  - `[SteamSessionService] Applied Steam bootstrap to FishNet. provider=SteamRelay ...`
  - `[FishNetSessionService] Steam relay bootstrap requested ...`
  - `[FishNetSessionService] Falling back to direct endpoint ...`
  - `[SteamSessionService] Session started ... networkHost=True`
- [x] 회귀 테스트
  - EditMode: total=9, passed=9, failed=0
- [ ] 다음 단계
  - Steam SDK/Relay transport 실제 wiring 연결 후 fallback 비활성(`_allowSteamFallbackToDirect=false`) 경로 실검증

### 2026-03-07 22:19 (KST) 진행 스냅샷
- [x] Steam relay transport binder 경계 추가
  - 신규 인터페이스: `Assets/Game/Netcode/Runtime/ISteamRelayTransportBinder.cs`
  - 신규 구현: `Assets/Game/Netcode/Runtime/SteamRelayLoopbackTransportBinder.cs`
    - Host mode: relay bootstrap 수락
    - Client mode: `hostId`의 `address:port` 파싱 또는 fallback endpoint 사용
- [x] FishNet relay wiring 경로 보강
  - 파일: `Assets/Game/Netcode/Runtime/FishNetSessionService.cs`
  - 변경: SteamRelay 선택 시 binder `TryApplyBootstrap(...)` 호출
  - binder 없음/실패 시 fallback 정책(`_allowSteamFallbackToDirect`)에 따라 차단 또는 direct 전환
- [x] 씬 통합 보강
  - `VerticalSlice_MVP`의 `MatchSystems`에 `SteamRelayLoopbackTransportBinder` 추가
- [x] 통합 스모크 PASS
  - `[SteamSessionService] Applied Steam bootstrap to FishNet ... binder=True`
  - `[FishNetSessionService] Steam relay transport binder applied ...`
- [x] 테스트 확장 및 통과
  - 신규: `Assets/Game/Netcode/Editor/Tests/SteamRelayLoopbackTransportBinderTests.cs`
  - EditMode summary: total=11, passed=11, failed=0
- [ ] 다음 단계
  - Steam SDK transport binder(실제 Steam API 호출 구현체) 추가
  - fallback 비활성(strict relay)에서 Host/Client E2E 재검증

### 2026-03-07 23:08 (KST) 진행 스냅샷
- [x] Steam strict 상호작용 회귀 실패 원인 고정
  - 원인: 요청은 서버 도달했지만 `committed=False`로만 처리되어 납품 로그가 생성되지 않음
  - 조치 파일: `Assets/Game/Netcode/Runtime/FishNetScenePlayerAssigner.cs`
  - 조치 내용: 원격 슬롯(기본 slot 1) 할당 시 회귀 보조 시드 적용
    - `Scrap_03`를 PlayerB에 선지급
    - `RepairStation`을 PlayerB 전방으로 재배치
    - 로그: `[FishNetScenePlayerAssigner] Regression seed ready ...`
- [x] 회귀 스크립트 판독 보강
  - 파일: `.codex/workflows/netcode/run-interaction-regression.ps1`
  - 신규 summary 필드:
    - `acceptedAnyCount`
    - `acceptedUncommittedCount`
    - `regressionSeedAppliedInHost`
- [x] Steam strict E2E 재실행 PASS
  - 실행: `powershell -ExecutionPolicy Bypass -File .\\.codex\\workflows\\netcode\\run-interaction-regression.ps1 -UseSteamBootstrap -StrictSteamRelay`
  - 결과: `INTERACTION_REGRESSION_PASS ... ASSIGNED=1 COMMITTED=1 DELIVERIES=1`
  - summary: `Logs/interaction-regression-summary-20260307-230730.json`
    - `passed=true`
    - `steamPass=true`
    - `acceptedCommittedCount=1`
    - `deliveryAcceptedCount=1`
- [x] 회귀 안정성 확인
  - EditMode tests: `total=11, passed=11, failed=0`
- [ ] 다음 단계
  - host측 binder auto-resolve 경로를 프리팹/씬 직렬화 의존 없이 고정하는 리팩터 1회
  - durable/transient 대상(`Fuel/Repair/Tether`) strict-path 회귀 자동화 항목 확장

### 2026-03-07 23:28 (KST) 진행 스냅샷
- [x] durable/transient strict-path 회귀 자동화 확장 (Fuel/Repair/Tether)
  - `PlayerFuelNetworkState`
    - owner 변경 시 submit sequence 리셋 보강(`_lastOwnerIdForSubmitSequence`)
    - 회귀 마커 로그 추가: transient submit accepted/rejected, durable apply
  - `RepairObjectiveNetworkState`
    - 회귀 마커 로그 추가: durable repair sync published, transient delivery event received
  - `TetherNetworkStateReplicator`
    - 회귀 마커 로그 추가: durable sync published/applied, transient break received
    - 서버 스냅샷 마커 API 추가: `LogRegressionSnapshot()`
  - `FishNetScenePlayerAssigner`
    - slot 할당/회귀 시드 시점에 tether 스냅샷 마커 호출
- [x] 회귀 스크립트 판정 확장
  - 파일: `.codex/workflows/netcode/run-interaction-regression.ps1`
  - 신규 요약/판정 항목:
    - `durableTransientPass`
    - `authorityMismatchAcceptedCount`
    - `fuelTransientAcceptedInHost`, `fuelRejectedCountInHost`
    - `repairDurablePublishedInHost`
    - `tetherDurablePublishedInHost`
    - `deliveryDuplicateDetectedInHost`, `deliveryMonotonicInHost`
    - client 관측: fuel/repair/tether durable/transient marker count
- [x] Steam strict 재검증 PASS
  - sync: `CLIENT_SYNC_COMPLETED MODE=INCREMENTAL ... SYNCED=9`
  - 실행: `run-interaction-regression.ps1 -UseSteamBootstrap -StrictSteamRelay`
  - 결과: `INTERACTION_REGRESSION_PASS ... ASSIGNED=1 COMMITTED=1 DELIVERIES=1`
  - summary: `Logs/interaction-regression-summary-20260307-232755.json`
    - `passed=true`
    - `durableTransientPass=true`
    - `steamPass=true`
    - `authorityMismatchAcceptedCount=0`
    - `repairTransientDuplicateDetected=false`
- [x] 안정성 검증
  - EditMode tests: `total=11, passed=11, failed=0`
- [ ] 다음 단계
  - `run-reconnect-regression`에도 동일 durable/transient 마커 판정 일부 이식
  - Steam 실제 transport binder 구현체(Loopback 대체) 착수

### 2026-03-07 23:32 (KST) 재검증 스냅샷
- [x] 클라이언트 미러 동기화 후 strict 회귀 재확인
  - `CLIENT_SYNC_COMPLETED ... SYNCED=9`
  - `INTERACTION_REGRESSION_PASS ... ASSIGNED=1 COMMITTED=1 DELIVERIES=1`
  - summary: `Logs/interaction-regression-summary-20260307-233110.json`
    - `durableTransientPass=true`
    - `fuelRejectedCountInHost=0`
    - `repairTransientEventCountInClient=1`
    - `tetherDurablePublishedInHost=true`
- [x] EditMode 회귀 테스트 재확인
  - `total=11, passed=11, failed=0`
