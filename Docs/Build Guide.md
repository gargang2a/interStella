# interStella Build Guide

## 문서 운영 규칙
- 이 문서는 Append-Only로 운영한다.
- 기존 절차는 삭제하지 않고, 변경 시 최신 절차를 하단에 추가한다.
- 어떤 절차가 더 최신인지 반드시 날짜로 구분한다.

## 환경 기준
- Unity Editor: 2022.3.62f2
- 프로젝트 루트: `C:\Unity\interStella`

## 기본 실행 절차 (로컬)
1. Unity Hub에서 `C:\Unity\interStella` 프로젝트를 연다.
2. `Assets/Scenes/SampleScene.unity` 또는 MVP용 Vertical Slice 씬을 연다.
3. Play Mode로 로컬 이동/연료/테더/수리 루프를 점검한다.

## 테스트 절차 (AGENTS 기준)
- movement 변경:
  - 로컬 체감
  - Host/Client 일관성
  - tether 상호작용
- tether 변경:
  - 한 명 정지/한 명 이동
  - 반대 방향 이동
  - 하드리밋 도달
- netcode 변경:
  - host/owner/remote 관점 확인
  - spawn/despawn
  - 보정 동작 확인

## Headless 테스트 커맨드 (환경변수 설정 시)
### EditMode
`"$UNITY_EDITOR_PATH" -batchmode -nographics -quit -projectPath . -runTests -testPlatform EditMode -testResults ./Logs/editmode-results.xml`

### PlayMode
`"$UNITY_EDITOR_PATH" -batchmode -nographics -quit -projectPath . -runTests -testPlatform PlayMode -testResults ./Logs/playmode-results.xml`

## Steam 통합 준비 체크
- [ ] 로컬 2인 세션에서 한 판 루프 완료 가능
- [ ] 호스트 권한 상태 정의(연료/테더/목표 진행도) 문서화
- [ ] transient event와 durable state 전송 경계 정의
- [ ] Steam 로비/초대 실패 시 사용자 메시지 처리 정의

## 변경 이력 (Append-Only)
### 2026-03-05 18:03 (KST)
- 문서 신규 생성
- 로컬 실행/테스트 기준 절차 등록
- Steam 통합 전 체크리스트 등록

## 문서 업데이트 절차 (Obsidian MCP)
1. 기능/설계 변경 발생
2. `기획서`에 의도/규칙 변경점 추가
3. `TASK`에 실행 항목 또는 상태 변경 추가
4. `Build Guide`에 검증/실행 절차 변경 추가
5. 기존 항목 삭제 없이 하단에 시간과 함께 누적

### 2026-03-05 18:08 (KST)
- Obsidian MCP 기반 문서 운영 절차 추가
- Append-Only 운영 확인

### 2026-03-05 17:58 (KST)
- 사용자 요청 반영: "옵시디언에 기록해"
- Obsidian 운영 절차에 따라 변경 사항을 Append-Only 방식으로 반영

### 2026-03-05 18:24 (KST)
- Unity MCP 설치 반영:
  - `Packages/manifest.json`에 `com.coplaydev.unity-mcp`를 `file:com.coplaydev.unity-mcp`로 설정
  - `Packages/com.coplaydev.unity-mcp` embedded package 배치
- Codex MCP 설정 반영:
  - `~/.codex/config.toml`의 `[mcp_servers.unityMCP]`를 stdio(uvx) 모드로 전환
- 후속 연결 확인:
  - Codex 재시작 후 `unityMCP` handshake 확인 필요

### 2026-03-05 18:43 (KST)
- Unity MCP 연결 확인 절차(실사용):
  1. `manage_scene(action=get_active)`로 active scene 확인
  2. `manage_editor(action=play)` -> `manage_editor(action=stop)` 왕복 확인
  3. `read_console(action=get, types=["all"])`로 런타임 로그 확인
- 현재 확인된 기준 결과:
  - `Assets/Game/Scenes/VerticalSlice/VerticalSlice_MVP.unity` 로드 상태에서 위 3단계 통과
  - 치명 예외는 미검출
- 알려진 제한:
  - `read_mcp_resource(mcpforunity://scene/gameobject/{id}/components)` 호출 시
    서버 응답 포맷 이슈(`contents ... got MCPResponse`)로 상세 필드 조회 실패 가능
  - 임시 우회: PlayMode 스모크 + 콘솔 기반 검증 우선

### 2026-03-05 18:53 (KST)
- 비주얼 프록시 패스 적용 절차 (현재 기준):
  1. `Assets/Game/Art/Materials`에 프록시 재질 생성
  2. 핵심 게임오브젝트(`PlayerA/B`, `RepairStation`, `Scrap_*`, `TetherSystem`)에 재질 할당
  3. `VisualProxy` 루트에 행성/소행성 프록시 배치
  4. 프록시 배경 오브젝트 콜라이더 제거(게임플레이 간섭 방지)
  5. `manage_scene(action=screenshot)`로 시각 검수
- 비교 스크린샷:
  - Before: `Assets/Screenshots/verticalslice_before_visual_pass.png`
  - After: `Assets/Screenshots/verticalslice_after_visual_pass.png`
  - PlayMode: `Assets/Screenshots/verticalslice_playmode_visual_pass.png`

### 2026-03-05 21:19 (KST)
- 사용자 에셋 적용 절차(실적용):
  1. 행성: `Earth.prefab` 직접 사용 시 Missing Script 오류 발생 가능성 확인
  2. 대체: `Geometry/Earth.fbx` 인스턴스 + `M_Earth_Builtin(Standard)` 적용
  3. 스카이박스: Main Camera에 `Skybox` 컴포넌트 추가 후 `Skybox1.mat` 할당
  4. 플레이어: `Player.fbx`를 `PlayerA/B` 하위 비주얼로 배치하고 캡슐 렌더 비활성화
  5. PlayMode에서 콘솔 오류 확인 후 저장
- 신규 검수 스크린샷:
  - Asset Apply(Edit): `Assets/Screenshots/verticalslice_asset_apply_preview3.png`
  - Asset Apply(PlayMode): `Assets/Screenshots/verticalslice_asset_apply_playmode2.png`

### 2026-03-05 21:35 (KST)
- `Unity-Verlet-Rope` 테스트 절차:
  1. GitHub 소스 클론: `https://github.com/jongallant/Unity-Verlet-Rope`
  2. 2D 데모 스크립트를 직접 이식하지 않고, 프로젝트용 3D 뷰 컴포넌트(`TetherVerletRopeView`)로 적용
  3. `TetherSystem`에서 `TetherView`를 비활성화하고 `TetherVerletRopeView` 활성화
  4. `LineRenderer`는 기존 컴포넌트를 재사용
  5. PlayMode 캡처로 시각 확인
- 튜닝 파라미터(현재값):
  - `_nodeCount=24`, `_solverIterations=28`, `_damping=0.996`, `_gravity=(0,-0.03,0)`, `_smoothing=0.6`
- 검수 스크린샷:
  - Verlet Rope(PlayMode): `Assets/Screenshots/verticalslice_verlet_rope_playmode_final_tune.png`

### 2026-03-05 22:45 (KST)
- 테더 시각 상태 검증 절차(추가):
  1. 슬랙 케이스: `PlayerA/B` 근접 배치 후 PlayMode 캡처
  2. 하드리밋 케이스: `PlayerA/B` 원거리 배치 후 PlayMode 캡처
  3. 색상 반영 확인: 슬랙(청록) vs 하드리밋(주황) 비교
- 색상 반영 필수 조건:
  - `TetherVerletRopeView`에서 `EvaluateTensionLevel(distance)` 기반 상태 색상 계산
  - LineRenderer 재질은 vertex color 반영 가능한 재질 사용 (`Sprites/Default` 기반)
  - 현재 재질: `Assets/Game/Art/Materials/M_Tether_Dynamic.mat`
- 진단 유틸:
  - 메뉴: `Tools/interStella/Diagnostics/Scan Missing Scripts (Active Scene)`
  - 스크립트: `Assets/Editor/MissingScriptScanner.cs`
- 추가 검수 스크린샷:
  - Slack: `Assets/Screenshots/tether_caseF_slack_dynamicmat.png`
  - HardLimit: `Assets/Screenshots/tether_caseG_hardlimit_dynamicmat.png`
  - Final smoke: `Assets/Screenshots/verticalslice_verlet_after_validation.png`

### 2026-03-05 23:46 (KST)
- 카메라 모드 전환 사용 가이드(VerticalSlice_MVP):
  1. 씬 실행: `Assets/Game/Scenes/VerticalSlice/VerticalSlice_MVP.unity`
  2. 이동 입력: `WASD`, `R/F`, `Shift`, `Space`
  3. 시점 전환:
     - `1`: 1인칭
     - `2`: 전체시점(오버뷰)
     - `3`: 3인칭
- 관련 컴포넌트/파일:
  - 카메라 제어: `Assets/Game/Features/Player/PlayerCameraModeController.cs` (Main Camera)
  - 감도 조정: `Assets/Game/Features/Player/PlayerInputReader.cs` (`_lookSensitivity`)
- 참고:
  - 현재 시점 전환은 로컬 테스트용이며 네트워크 권한 상태와 분리된 presentation 동작이다.

### 2026-03-06 00:14 (KST)
- FishNet 도입(1차) 절차:
  1. `Packages/manifest.json`에 FishNet UPM Git 의존성 추가
     - `"com.firstgeargames.fishnet": "https://github.com/FirstGearGames/FishNet.git?path=Assets/FishNet"`
  2. Unity 리프레시/컴파일 수행 후 `Packages/packages-lock.json`에 반영 확인
  3. 씬 `MatchSystems`에 `NetworkManager` + FishNet 어댑터 컴포넌트 추가
     - `FishNetSessionService`
     - `FishNetAuthorityGateway`
  4. 참조 전환
     - `StationMatchController._sessionServiceBehaviour` -> `FishNetSessionService`
     - `PlayerNetworkBridge._authorityGatewayBehaviour` -> `FishNetAuthorityGateway`
- 검증 기준(현재 통과):
  - 컴파일 오류 없음
  - PlayMode 스모크에서 게임 오류/예외 없음(MCP 연결 로그 제외)
- 현재 제한:
  - 이 단계는 "세션/권한 어댑터" 전환까지만 완료
  - NetworkObject 스폰/복제/재조정(Replicate/Reconcile) 단계는 다음 작업
- 시각 검수 스크린샷:
  - `Assets/Screenshots/verticalslice_fishnet_adapter_and_player_split.png`

### 2026-03-06 00:28 (KST)
- FishNet 씬 슬롯 스폰 가이드(1차):
  1. `VerticalSlice_MVP`에서 `MatchSystems`에 다음 컴포넌트 존재 확인
     - `NetworkManager`
     - `FishNetSessionService`
     - `FishNetAuthorityGateway`
     - `FishNetScenePlayerAssigner`
  2. `PlayerA`, `PlayerB`에 다음 컴포넌트 존재 확인
     - `NetworkObject`
     - `NetworkTransform`
     - `PlayerNetworkBridge`
     - `PlayerOwnershipInputGate`
  3. 실행 시 `FishNetSessionService(Host)`가 서버/클라를 시작하고,
     `OnClientLoadedStartScenes(asServer=true)` 시점에 슬롯 할당/스폰 진행
- 확인 로그(Host 스모크):
  - `Local server is started for Tugboat.`
  - `Remote connection started for Id 0.`
  - `Local client is started for Tugboat.`
- 현재 제한:
  - 이 단계는 씬 슬롯 스폰/소유권 게이트까지 완료
  - 2프로세스 원격 클라이언트 기준 입력/동기화 검증은 별도 수행 필요
- 스크린샷:
  - `Assets/Screenshots/verticalslice_fishnet_scene_slot_assigner.png`

### 2026-03-06 00:41 (KST)
- FishNet `NetworkObject` 초기화 경고 복구 절차(현재 기준):
  1. Play Mode 종료
  2. 메뉴 실행:
     - `Tools/InterStella/Netcode/Reserialize Open Scene NetworkObjects`
  3. 로그 확인:
     - `Scene NetworkObjects refreshed.`
     - `... sceneIds were generated.`
  4. 씬 저장:
     - `Assets/Game/Scenes/VerticalSlice/VerticalSlice_MVP.unity`
  5. PlayMode 재실행 후 경고 재발생 여부 확인
- 자동화 유틸 파일:
  - `Assets/Game/Netcode/Editor/FishNetOpenSceneReserializeUtility.cs`
- 비고:
  - 유틸은 FishNet 기본 메뉴 로직을 Reflection으로 호출한다.
  - 목적은 씬 `NetworkObject` 직렬화 누락 복구이며, gameplay 로직 변경은 포함하지 않는다.

### 2026-03-06 10:45 (KST)
- 입력 권한 동작 확인 절차(동시 조작 이슈 대응):
  1. `VerticalSlice_MVP` 실행 후 `WASD/R/F/Mouse` 입력
  2. 기대 결과:
     - 로컬 소유 플레이어만 직접 조작
     - 비소유 플레이어는 같은 입력으로 직접 회전/추진하지 않음
  3. 추가 확인:
     - 입력 게이트가 닫힌 플레이어는 이전 프레임 입력이 잔류하지 않아야 함
- 관련 수정 파일:
  - `Assets/Game/Netcode/Runtime/FishNetAuthorityGateway.cs`
  - `Assets/Game/Features/Player/PlayerNetworkBridge.cs`
  - `Assets/Game/Features/Player/PlayerInputReader.cs`
  - `Assets/Game/Features/Player/PlayerOwnershipInputGate.cs`

### 2026-03-06 10:57 (KST)
- 2프로세스 실행 오버라이드 가이드(현재 기준):
  1. Host 실행(기본값 유지):
     - `_startupMode = Host`
  2. Client 실행(런타임 오버라이드):
     - CLI 예시:
       - `-interstella-mode client -interstella-address 127.0.0.1 -interstella-port 7770`
     - 또는 ENV:
       - `INTERSTELLA_MODE=client`
       - `INTERSTELLA_ADDRESS=127.0.0.1`
       - `INTERSTELLA_PORT=7770`
- 연료 상태 요청 검증(신규):
  - `PlayerFuelNetworkState`는 ServerRpc 수신 시
    1) 요청자=오너 매칭 검증
    2) 허용 연료 변화량(초당 델타) 검증
  - 비정상 요청은 폐기
- 동기화 최적화(신규):
  - `RepairObjectiveNetworkState`: delivered 변경 시에만 발행
  - `TetherNetworkStateReplicator`: 동일 상태 반복 apply 억제
- 현재 제한:
  - MCP에서 활성 Unity 인스턴스가 단일 포트(`6401`)만 노출되는 경우
    자동 다중 인스턴스 E2E 검증은 제한될 수 있다.

### 2026-03-06 10:59 (KST)
- 슬롯 할당 검수 로그(Host):
  - `FishNetScenePlayerAssigner`가 다음 형식으로 로그를 출력한다.
    - `Assigned client {id} to slot {index} ({playerName})`
- 단일 프로세스 기준 확인값:
  - `Assigned client 0 to slot 0 (PlayerA)`
- 주의:
  - 슬롯 회수(`Released slot ...`) 검증은 원격 클라이언트 연결/해제 이벤트가 필요하다.

### 2026-03-06 11:24 (KST)
- Unity MCP 업데이트 절차(현재 적용본):
  1. `.tmp/unity-mcp-src`에서 `origin/beta` 최신으로 Fast-forward
  2. `MCPForUnity` 폴더를 `Packages/com.coplaydev.unity-mcp`로 동기화
  3. Unity 리프레시/컴파일 실행
  4. 콘솔에서 버전 확인:
     - `MCP-FOR-UNITY ... server=9.4.8-beta.19`
     - `Updated stdio MCP configs to package version 9.4.8-beta.19.`
- 현재 기준 버전:
  - 패키지: `9.4.8-beta.19`
  - stdio 서버: `9.4.8-beta.19`

### 2026-03-06 11:27 (KST)
- 다음 검증 우선순위(현재 기준):
  1. 2프로세스 Host 실행
     - Host: 기본 실행(Host 모드)
     - Client: `-interstella-mode client -interstella-address 127.0.0.1 -interstella-port 7770`
  2. 소유권/입력 검증
     - Owner 슬롯만 직접 이동/회전 입력 반응
     - 비소유 슬롯은 동일 입력에 직접 반응하지 않아야 함
  3. 연결 해제/재접속 검증
     - `Released slot ...` 로그 확인
     - 재접속 시 `Assigned client ...` 로그 재확인
  4. 상태 동기화 검증
     - durable: 연료/수리/테더 상태 불연속 튐 여부 확인
     - transient: 이벤트 중복/누락 발생 여부 확인

### 2026-03-06 11:42 (KST)
- ECC 부분 설치 반영 절차:
  1. `.codex/config.toml` 추가 (프로젝트 Codex 실행 프로필)
  2. `Docs/Codex Workflow Pack.md` 추가 (작업 루프, DoD, 로그 패턴)
  3. 기존 3개 운영 문서에 변경 이력 Append
- 적용 후 검증 절차:
  1. Host/Client 2프로세스 실행
  2. Owner/Remote 입력 경계 확인
  3. disconnect/reconnect 시 슬롯 로그 확인
  4. 결과를 `기획서/TASK/Build Guide`에 동일 타임스탬프로 누적

### 2026-03-06 11:47 (KST)
- 운영 문서 확장:
  - ECC 부분 설치의 상세 설명/활용법 전용 노트 추가
  - 경로: `Docs/ECC 부분 설치 상세 가이드.md`
- 사용 지침:
  - 새 작업자 온보딩 또는 작업 시작 전, 본 노트 우선 확인

### 2026-03-06 12:20 (KST)
- 2프로세스 검증 실행 결과(현재):
  1. Host PlayMode 실행 후 기본 로그 확인
     - `Starting session mode=Host`
     - `Assigned client 0 to slot 0 (PlayerA)`
  2. Unity 추가 프로세스를 client 인자로 실행 시도
     - `-interstella-mode client -interstella-address 127.0.0.1 -interstella-port 7770`
  3. 제약 확인
     - MCP 제어 가능 인스턴스: `interStella@b68d5cf0 (port 6401)` 1개
     - Host 로그에서 `Remote connection started for Id 1` 미확인
- 현재 결론:
  - 단일 인스턴스 Host 검증은 통과
  - 원격 client 검증은 별도 제어 가능한 두 번째 런타임 확보 후 재실행 필요

### 2026-03-06 12:43 (KST)
- 2프로세스 실검증 절차(확정):
  1. 테스트용 클론 프로젝트 준비
     - `C:\Unity\interStellaClient` (원본 제외 디렉터리: Library/Temp/Logs/obj/.vs/.tmp/UserSettings)
  2. Host 실행
     - 원본 프로젝트(`C:\Unity\interStella`)에서 Play
  3. Client 실행(자동 Play)
     - `-projectPath "C:\Unity\interStellaClient"`
     - `-interstella-mode client -interstella-address 127.0.0.1 -interstella-port 7770`
     - `-executeMethod InterStella.EditorTools.InterStellaClientAutoPlayBootstrap.StartClientPlay`
  4. Host 로그 검증
     - 접속: `Remote connection started for Id 1`
     - 할당: `Assigned client 1 to slot 1 (PlayerB)`
     - 해제: `Released slot 1 from client 1; ownership removed from PlayerB`
     - 재할당: `Assigned client 2 to slot 1 (PlayerB)`
- 추가 파일:
  - `Assets/Game/Netcode/Editor/InterStellaClientAutoPlayBootstrap.cs`

### 2026-03-06 13:00 (KST)
- 상호작용 네트워크 경계 변경(신규):
  1. `PlayerInteractionNetworkRelay`를 플레이어 네트워크 오브젝트에 추가한다.
  2. 동작 원리:
     - Host/로컬 권한 인스턴스: `PlayerInteraction.TryInteractAuthoritative()` 직접 실행
     - Client Owner 인스턴스: `PlayerInteractionNetworkRelay.TryRequestServerInteraction()` 호출
     - Host가 `ServerRpc`를 수신해 최종 `TryInteractAuthoritative()`를 실행
- 적용 체크:
  1. 씬 `VerticalSlice_MVP`에서 `PlayerA`, `PlayerB`에 `PlayerInteractionNetworkRelay`가 붙어 있어야 한다.
  2. `PlayerOwnershipInputGate`는 interaction 활성 기준으로 `IsAuthoritativeOwner`를 사용해야 한다.
- 검증 절차(업데이트):
  1. Host 실행 + Client(클론 프로젝트) 접속
  2. Client Owner에서 `E` 입력으로 스크랩/수리 상호작용 시도
  3. Host 로그와 진행 상태에서 상호작용 커밋 여부 확인
  4. Remote 관점에서 중복 상호작용 이벤트(연속 요청) 없는지 확인
- 관련 파일:
  - `Assets/Game/Netcode/Runtime/PlayerInteractionNetworkRelay.cs`
  - `Assets/Game/Features/Player/PlayerInteraction.cs`
  - `Assets/Game/Features/Player/PlayerOwnershipInputGate.cs`

### 2026-03-06 13:06 (KST)
- 스크랩 동기화 구성 절차(신규):
  1. `ScrapItem`에 상태 적용 API가 있어야 한다.
     - `SetCarriedStateAuthoritative(PlayerCarrySocket)`
     - `SetWorldStateAuthoritative(Vector3, bool)`
  2. 각 스크랩 오브젝트(`Scrap_01~03`)에 아래 컴포넌트를 붙인다.
     - `NetworkObject`
     - `ScrapCarryNetworkState`
  3. 메뉴 실행:
     - `Tools/InterStella/Netcode/Reserialize Open Scene NetworkObjects`
  4. 씬 저장:
     - `Assets/Game/Scenes/VerticalSlice/VerticalSlice_MVP.unity`
- 검증 포인트:
  1. 픽업 시 Remote에서도 스크랩이 운반 상태로 전환되는지
  2. 드롭 시 Remote에서도 월드 위치/상태가 일치하는지
  3. 납품 시 스크랩이 재활성되지 않고 Delivered 상태로 유지되는지
- 관련 파일:
  - `Assets/Game/Netcode/Runtime/ScrapCarryNetworkState.cs`
  - `Assets/Game/Features/Scavenge/ScrapItem.cs`

### 2026-03-06 15:12 (KST)
- disconnect carry 처리 검증 절차(확정):
  1. Host Play 시작
  2. Client 접속 확인 (`Assigned client 1 ...`)
  3. 디버그 메뉴로 `PlayerB` 강제 운반 상태 설정
     - `Tools/InterStella/Debug/Force PlayerB Carry Scrap_01`
  4. Client 프로세스 강제 종료
  5. Host 로그 확인:
     - timeout
     - `Forced scrap drop on disconnect ...`
     - `Released slot ...`
  6. Client 재실행 후 재할당 로그 확인
     - `Assigned client 2 to slot 1 (PlayerB)`
- 관련 코드:
  - `Assets/Game/Features/Scavenge/PlayerCarrySocket.cs` (`TryForceDropWithoutImpulse`)
  - `Assets/Game/Netcode/Runtime/FishNetScenePlayerAssigner.cs` (ReleaseSlot 경로)
  - `Assets/Game/Netcode/Editor/InterStellaNetcodeDebugActions.cs` (검증 메뉴)
- 매치 재시작 안정화:
  - `StationMatchController`는 `StartMatch/ResetMatch`에서 수리 목표/연료를 시작 상태로 리셋
  - `PlayerFuel.ResetToStartupFuel()` 사용

### 2026-03-06 16:12 (KST)
- Client 자동 상호작용 E2E 검증 절차(리로드 내성 반영):
  1. Host(`C:\Unity\interStella`) Play 시작
  2. 필요 시 스크랩 배치:
     - `Tools/InterStella/Debug/Place Scrap_03 In Front Of PlayerB`
  3. Client 실행:
     - `-projectPath "C:\Unity\interStellaClient"`
     - `-interstella-mode client -interstella-address 127.0.0.1 -interstella-port 7770`
     - `-interstella-auto-interact 1`
     - `-executeMethod InterStella.EditorTools.InterStellaClientAutoPlayBootstrap.StartClientPlay`
  4. Host 로그 확인:
     - `Remote connection started for Id 1`
     - `Assigned client 1 to slot 1 (PlayerB)`
     - `[PlayerInteractionNetworkRelay] Accepted interaction request... committed=True`
  5. Client 로그 확인(예: `-logFile`):
     - `ClientAutoPlayBootstrap: auto-interact mode enabled.`
     - `auto-interact attempt 1/24, accepted=True, owner=PlayerB`
- disconnect carry-drop 재검증 절차:
  1. `Force PlayerB Carry Scrap_01`
  2. Client 프로세스 종료
  3. timeout 후 Host 로그 확인:
     - `Forced scrap drop on disconnect for client ...`
     - `Released slot ... ownership removed ...`
- 주의사항:
  - `Force Start Match`는 이미 진행 중인 세션을 재시작하므로, 클린 접속 검증 시에는 Host Play 직후 기본 세션을 우선 사용한다.
  - FishNet 클라이언트 ID는 재접속 시 증가가 아닌 재사용이 발생할 수 있으므로, `slot 재할당` 로그를 성공 기준으로 삼는다.

### 2026-03-06 16:18 (KST)
- auto-interact OFF 검증 절차(신규):
  1. Client 실행 인자에 `-interstella-auto-interact 0` 지정
  2. Client 로그에서 아래 항목이 없어야 정상:
     - `ClientAutoPlayBootstrap: auto-interact mode enabled.`
     - `auto-interact attempt ...`
  3. Host에서는 접속/슬롯 할당 로그만 확인:
     - `Assigned client 1 to slot 1 (PlayerB)`
- 목적:
  - 자동 상호작용을 검증 시에만 명시적으로 켜고, 일반 접속 검증에서는 끌 수 있게 보장

### 2026-03-06 21:28 (KST)
- 2회 자동 상호작용(픽업+수리 납품) 검증 절차(신규):
  1. Host Play 시작
  2. 메뉴 실행:
     - `Tools/InterStella/Debug/Place RepairStation In Front Of PlayerB`
     - `Tools/InterStella/Debug/Place Scrap_03 In Front Of PlayerB`
  3. Client 실행 인자:
     - `-interstella-auto-interact 1`
     - `-interstella-auto-interact-count 2`
  4. 성공 기준 로그:
     - Client: `targetSuccesses=2`, `successes=1/2`, `successes=2/2`
     - Host: `PlayerInteractionNetworkRelay ... committed=True` (2회)
     - Host: `[RepairStationObjective] Delivery accepted ...`
- 회귀 절차(유지):
  - carry 상태에서 client 종료 -> timeout -> forced drop -> slot release -> reconnect
- OFF 검증 절차(유지):
  - `-interstella-auto-interact 0`에서 auto-interact 관련 로그가 없어야 정상

### 2026-03-06 22:06 (KST)
- GitHub MCP 설정 절차(신규):
  1. `C:\Users\gar\.codex\config.toml` 또는 프로젝트 `.codex/config.toml`에 아래 추가
     - `[mcp_servers.github]`
     - `url = "https://api.githubcopilot.com/mcp/"`
     - `bearer_token_env_var = "GITHUB_PAT_TOKEN"`
  2. 사용자 환경변수 설정
     - `GITHUB_PAT_TOKEN`
  3. 검증
     - `powershell -ExecutionPolicy Bypass -File .\.codex\workflows\git\setup-github-mcp.ps1`
  4. Codex 앱 재시작
- 자동 Git 워크플로우 실행 순서(신규):
  1. 브랜치 생성
     - `auto-branch.ps1 -Name <task> -Base main -SyncBase`
  2. 최신 동기화
     - `auto-pull.ps1`
  3. 커밋(+선택 푸시)
     - `auto-commit.ps1 -Message "..." -Push`
  4. 원샷 흐름(선택)
     - `auto-workflow.ps1 -Task <task> -Base main -CommitMessage "..." -Push`
- 보호 동작:
  - Git 저장소가 아니면 스크립트는 즉시 중단(`Current directory is not a Git repository.`)

### 2026-03-06 22:21 (KST)
- GitHub 초기 푸시 실전 절차(검증 완료):
  1. 대용량 파일 점검(100MB 초과 파일 제외)
     - `.gitignore`에 `*.unitypackage` 추가
  2. 초기 커밋
     - `git commit -m "chore: bootstrap interStella MVP foundation"`
  3. 브랜치 정리
     - `git branch -M main`
  4. 원격 연결
     - `git remote add origin https://github.com/gargang2a/interStella.git`
  5. 첫 푸시
     - `git push -u origin main`
  6. 검증
     - `git rev-parse HEAD` == `git rev-parse origin/main`
     - 웹 URL: `https://github.com/gargang2a/interStella`

### 2026-03-06 22:56 (KST)
- Git 자동 워크플로우 E2E 실제 실행 결과:
  1. `auto-branch.ps1` 실행 -> `codex/git-workflow-e2e` 생성
  2. `auto-commit.ps1 -Push` 실행 -> 커밋/원격 브랜치 생성
  3. `auto-pull.ps1` 실행 -> `PULL_COMPLETED`
  4. 완료 브랜치 PR 생성 -> `#1`
- 수정 사항:
  - `auto-commit.ps1`: `Invoke-Git add -A` -> `Invoke-Git add --all`

### 2026-03-06 23:22 (KST)
- netcode durable/transient 보강 적용 파일:
  1. Assets/Game/Netcode/Runtime/PlayerFuelNetworkState.cs
     - ServerRpc 소유권 요구 강화
     - submit sequence 기반 stale/duplicate 제출 차단
  2. Assets/Game/Netcode/Runtime/RepairObjectiveNetworkState.cs
     - delivery transient ObserversRpc 분리
     - durable deliveredCount SyncVar와 경계 분리
  3. Assets/Game/Netcode/Runtime/TetherNetworkStateReplicator.cs
     - break transient RPC sequence 도입
     - 중복 break 적용 완화
- 검증 명세:
  - Unity MCP validate_script 3개 파일 통과(diagnostics 없음)

### 2026-03-06 23:28 (KST)
- 브랜치/PR 결과:
  - branch: codex/netcode-durable-transient-hardening
  - commit: 7b2bef2
  - PR: #2 (main 대상)
- 후속 실행:
  - PR 머지 후 Host/Client 2프로세스 검증 체크리스트 실행

### 2026-03-07 00:08 (KST)
- 2프로세스 재검증 실행 기록(신규):
  1. Host Play 시작 상태에서 Client(Editor) 실행
  2. Client 인자:
     - `-interstella-auto-interact 1`
     - `-interstella-auto-interact-count 2`
  3. 성공 로그 확인:
     - Host: `Remote connection started for Id 1`
     - Host: `Assigned client 1 to slot 1 (PlayerB)`
     - Host: `PlayerInteractionNetworkRelay ... committed=True` (2회)
     - Host: `Delivery accepted. delivered=1/3`
     - Client: `auto-interact ... successes=2/2`
  4. 오류 필터 확인:
     - Host 콘솔 `PacketId`/`unhandled` 검색 결과 0건
- disconnect/reconnect 체크(부분):
  - 기존 Client 종료 시 timeout -> slot release 정상
  - 신규 Client 재실행은 라이선싱 초기화 지연이 길면 접속 확인까지 대기 필요
  - 권장: 재실행 후 Host 콘솔에 `Remote connection started`가 찍힐 때까지 1~2분 대기

### 2026-03-07 00:13 (KST)
- 재접속 검증 중 환경 이슈 기록:
  - Client 재실행 로그에서 Licensing 단계 타임아웃(60s) 후 code 199 종료
  - 이 경우 접속 검증 자체가 시작되지 않으므로 네트워크 성공/실패로 판정하지 않음
- 운영 메모:
  - 먼저 Client Editor가 정상 진입했는지 확인 후 접속 로그(`Starting session mode=ClientOnly`)를 기준으로 검증 시작

### 2026-03-07 00:16 (KST)
- reconnect 최종 검증 실패 케이스(신규):
  - 로그 파일:
    - `Logs/client-reconnect-check1-20260307-000832.log`
    - `Logs/client-reconnect-check2-20260307-001023.log`
  - 실패 패턴:
    - `Timed-out after 60.00s, waiting for channel: "LicenseClient-gar"`
    - `Application will terminate with return code 199`
- 조치 결과:
  - `Unity.Licensing.Client` 재기동 후에도 동일
- 판정 규칙:
  - 위 패턴 발생 시 네트워크/게임플레이 검증 실패가 아닌 `에디터 런타임 환경 블로커`로 분류

### 2026-03-07 00:24 (KST)
- sequence 안정성 회귀 테스트(신규):
  1. `NetworkSequenceComparerTests` 실행(EditMode)
  2. 기대 결과:
     - 총 6개 테스트 통과
     - wrap-around 구간에서 최신/구버전 판정 정확
- 적용 파일:
  - `Assets/Game/Netcode/Runtime/NetworkSequenceComparer.cs`
  - `Assets/Game/Netcode/Runtime/PlayerFuelNetworkState.cs`
  - `Assets/Game/Netcode/Runtime/TetherNetworkStateReplicator.cs`
- 테스트 결과:
  - `passed=6, failed=0, skipped=0`

### 2026-03-07 00:33 (KST)
- 라이선싱 우회 실험(신규):
  1. Hub licensing client 수동 실행
     - `Unity.Licensing.Client.exe --namedPipe LicenseClient-gar --cloudEnvironment production`
  2. Client Editor 재기동
  3. 로그 확인
     - `Channel LicenseClient-gar doesn't exist`
     - `Connection to channel LicenseClient-gar refused`
     - `Timed-out after 60.00s ... code 199`
- 판정:
  - 네트워크/게임플레이 문제가 아닌 라이선싱 IPC 계층 블로커

### 2026-03-07 00:39 (KST)
- reconnect 재시도(check5) 결과:
  - `Connection to channel LicenseClient-gar refused`
  - `Timed-out after 60.00s, waiting for channel ...`
  - 종료 코드 `199`
- 운영 조치:
  - 실패 재시도 후 남는 보조 Licensing client 프로세스는 정리하여 기본 인스턴스만 남김
- 현재 판정:
  - 세션 내 자동 재검증은 중단, 라이선싱 정상화 후 재개

### 2026-03-07 00:47 (KST)
- GitHub PR 상태(신규):
  - PR #3 생성 완료
  - URL: https://github.com/gargang2a/interStella/pull/3
- 포함 범위:
  - netcode sequence 보강
  - EditMode 테스트 추가/통과
  - reconnect 검증 블로커 문서화

### 2026-03-07 00:58 (KST)
- 입력 권한 이중 가드(신규):
  - 1차: `PlayerOwnershipInputGate`에서 InputReader/Interaction enable 제어
  - 2차: `PlayerMotor`에서 non-owner 입력 시뮬레이션 차단
- 회귀 의도:
  - Owner가 아닌 플레이어가 동일 입력으로 같이 움직이던 경로를 코드 레벨에서 봉쇄

### 2026-03-07 01:05 (KST)
- reconnect check6 실패 패턴:
  - `Channel LicenseClient-gar doesn't exist`
  - `Connection to channel LicenseClient-gar refused`
  - `Timed-out after 60.00s ... return code 199`
- 판정 유지:
  - 네트워크/게임플레이 레이어가 아닌 라이선싱 IPC 블로커

### 2026-03-07 02:31 (KST)
- Host/Client 재접속 실검증(경합 케이스) 절차 보강:
  1. Host Play 시작(VerticalSlice_MVP)
  2. Client A 실행 후 연결 확인
  3. Client A 강제 종료
  4. timeout 전에 Client B 즉시 실행
  5. Host 로그에서 아래 순서 확인
     - No available slot for client <id>. Queued for reassignment.
     - Id [<oldId>] ... timed out
     - Released slot 1 ...
     - Assigned client <newId> to slot 1 (PlayerB)
- 권한 커밋 검증 포인트:
  - Host 로그 PlayerInteractionNetworkRelay ... committed=True
  - 필요 시 디버그 메뉴로 상호작용 타겟 배치
    - Tools/InterStella/Debug/Place Scrap_03 In Front Of PlayerB
    - Tools/InterStella/Debug/Place RepairStation In Front Of PlayerB
- 라이선싱 운영 메모:
  - 안정적으로 붙는 경로는 Host Hub 세션 인자(-hubSessionId, -accessToken, -licensingIpc)를 재사용한 Client 실행
  - 실패 패턴(code 199) 발생 시 editor licensing IPC 충돌 여부를 먼저 확인

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

### 2026-03-07 03:16 (KST)
- 신규 워크플로우 추가:
  1. Client 동기화
     - `powershell -ExecutionPolicy Bypass -File .\.codex\workflows\client\sync-interstella-client.ps1`
     - 옵션: `-WhatIf`, `-Mirror`
  2. 재접속 회귀 자동 판정
     - `powershell -ExecutionPolicy Bypass -File .\.codex\workflows\netcode\run-reconnect-regression.ps1`
     - 느린 환경 권장: `-ClientBootTimeoutSec 360`
- 최신 실행 결과:
  - PASS 로그: `Logs/reconnect-regression-summary-20260307-031141.json`
  - 핵심 판정: queued/released/reassigned 패턴 모두 감지

### 2026-03-07 03:19 (KST)
- 워크플로우 인덱스 문서:
  - `.codex/workflows/README.md`

### 2026-03-07 03:36 (KST)
- 신규 통합 워크플로우:
  - `powershell -ExecutionPolicy Bypass -File .\.codex\workflows\netcode\run-e2e-sync-regression.ps1`
- 옵션:
  - 재시도: `-RegressionMaxAttempts 3 -RetryDelaySec 10`
- 운영 메모:
  - Host가 UDP 7770을 열지 않은 상태면 래퍼가 즉시 실패 처리

### 2026-03-07 12:52 (KST)
- Git 자동화 확장(신규):
  1. PR 생성 스크립트
     - powershell -ExecutionPolicy Bypass -File .\.codex\workflows\git\auto-pr.ps1 -Base main
  2. 원샷 + PR
     - powershell -ExecutionPolicy Bypass -File .\.codex\workflows\git\auto-workflow.ps1 -Task "e2e-check" -Base main -CommitMessage "chore: checkpoint" -Push -CreatePr -PrTitle "chore: e2e-check"
- 동작 규칙:
  - GITHUB_PAT_TOKEN이 있으면 REST API로 PR 생성
  - 토큰이 없으면 실패하지 않고 PR_CREATE_URL을 출력(웹에서 즉시 생성 가능)
- 검증 결과:
  - 현재 환경은 토큰 미설정으로 URL fallback 경로 확인 완료

### 2026-03-07 16:56 (KST)
- 실행 결과 업데이트:
  - 신규 워크플로우 커밋/푸시 완료 (84f961e)
  - 현재 브랜치 기준 open PR 없음 확인
- 즉시 생성 URL:
  - https://github.com/gargang2a/interStella/compare/main...codex/e2e-workflow-orchestrator?expand=1

### 2026-03-07 17:36 (KST)
- 머지 후 회귀 검증 절차(실행 완료):
  1. Unity MCP로 Host Editor Play 진입
  2. UDP 7770 listening 확인
  3. `run-e2e-sync-regression.ps1` 실행
  4. PASS 및 summary 확인
  5. Host Editor Play stop
- 최신 PASS 산출물:
  - `Logs/reconnect-regression-summary-20260307-173410.json`
  - 핵심 판정: queue/release/reassign 모두 true
  - client IDs: released=2, reassigned=4
- 운영 상태:
  - PR #6 머지 완료 후 main 기준선에서도 동일 회귀 경로가 정상 동작

### 2026-03-07 18:56 (KST)
- Copilot 자동리뷰 대체 경로 구축:
  - GitHub Actions `PR Guardrails Review` 추가
- 점검 룰(휴리스틱):
  - Update/FixedUpdate에서 `GetComponent`, `FindObjectOfType`, `GameObject.Find`, LINQ, `Debug.Log`
  - Gameplay 경로에서 `NetworkTransform` 의존
- 결과 출력:
  - PR 코멘트 1개를 marker 기준 upsert
- 주의:
  - `pull_request` 워크플로우는 base(main)에 머지된 이후부터 안정적으로 전 PR에서 동작

### 2026-03-07 19:16 (KST)
- PR Guardrails Review 운영 검증 완료:
  1. PR #9에서 신규 워크플로우 최초 실행/성공
  2. main 반영 후 PR #8에서 재실행/성공
- 확인 포인트:
  - Actions run success
  - PR Conversation에 marker 코멘트 upsert
- 결론:
  - Free 플랜 환경에서도 PR 자동 리뷰 코멘트 경로 확보

### 2026-03-07 19:25 (KST)
## Steam 통합 전 게이트 체크리스트 v1 (SSOT)
대상 범위:
- 2인 코옵 MVP
- Host-authoritative 세션
- VerticalSlice_MVP 1개 루프(접속 -> 이동/테더/연료 -> 수리 -> 종료)

비지원(명시적):
- late join
- host migration

Gate 항목(모두 PASS 필요):
1. 세션/재접속 안정성
- `run-e2e-sync-regression.ps1` 기준 PASS
- summary에서 `queueDetected/releaseDetected/reassignedAfterReleaseDetected=true`
- reconnect 시 슬롯 release -> 재할당 로그 확인

2. 권한 경계 일관성
- Owner만 입력/상호작용 반영
- Remote는 관찰만 가능(로컬 입력으로 이동 금지)
- 권한 위반 요청이 authoritative commit으로 반영되지 않음

3. 핵심 루프 성립
- 스크랩 픽업/운반/수리 납품 최소 1회 성공
- 연료/테더/수리 상태가 Host/Client에서 동일 의미로 보임

4. 조작/가시성
- 1/2/3 시점 전환 정상
- 이동 감도/멀미/가시성 치명 이슈 없음

5. 운영/품질
- main 기준 open P1 이슈 0건
- PR 자동 리뷰 경로(PR Guardrails Review) 정상 동작

Go/No-Go 규칙:
- 모든 Gate PASS + P1 0건이면 Steam 로비/초대/릴레이 통합 착수
- 하나라도 FAIL이면 Steam 통합 보류, 실패 항목 먼저 수정

### 2026-03-07 19:37 (KST)
## Steam 게이트 실행 결과 v1 (자동 검증 라운드)
실행 근거:
- E2E 명령: `run-e2e-sync-regression.ps1 -RegressionMaxAttempts 3 -RetryDelaySec 10`
- E2E summary: `Logs/reconnect-regression-summary-20260307-193420.json`
- 운영 품질: `OPEN_ISSUES=0`, `OPEN_P1=0`
- Guardrails run: `completed/success`
  - https://github.com/gargang2a/interStella/actions/runs/22797341792

Gate 판정:
1. 세션/재접속 안정성: PASS
- queue/release/reassign 모두 true 확인

2. 권한 경계 일관성: PENDING(수동 검증 필요)
- Owner 입력/Remote 관찰 체감 검증은 사용자 플레이 라운드에서 판정

3. 핵심 루프 성립: PENDING(수동 검증 필요)
- 수집/운반/수리 1회 완주 체감 검증 필요

4. 조작/가시성: PENDING(수동 검증 필요)
- 1/2/3 시점 전환, 이동감/멀미/가시성은 수동 라운드에서 판정

5. 운영/품질: PASS
- open P1 이슈 0건
- PR Guardrails Review 정상 동작

현재 Go/No-Go:
- NO-GO (수동 검증 미완료)
- Steam 통합 착수 전, Gate 2/3/4를 동일 세션에서 PASS로 채워야 함
