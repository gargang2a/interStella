# interStella TASK Dashboard

> [!summary] 사용 원칙
> - 이 문서는 Obsidian에서 보기 좋게 읽기 위한 대시보드다.
> - 상세 이력의 원본은 `TASK.md`다.
> - 작업 상태를 갱신할 때는 `TASK.md`를 먼저 append하고, 필요 시 이 대시보드를 갱신한다.

## Current Focus

> [!tip] 현재 최우선 목표
> `client local owner/input/camera` 보정 코드는 들어갔다. 이제 실제 Steam 2실계정 smoke로 재검증하는 것.

- 배경:
  - 로컬 ignore 자산 때문에 clone 기준 화면 차이가 생길 수 있다.
  - 그래서 팀원과의 실제 Steam 검증은 editor clone보다 build smoke가 더 적합하다.
- 현재 준비 완료:
  - Steam lobby create/join
  - Steam invite 발신 경계
  - Steam transport binder
  - build smoke용 Windows64 빌드 메뉴
  - build host/client launch helper

## Latest Snapshot

### 2026-03-08 18:53 (KST)

> [!success] Obsidian task snippet installed
> 완료 태스크가 Reading View와 Live Preview에서 더 비슷하게 보이도록 CSS snippet을 추가했다.

- snippet:
  - `.obsidian/snippets/interstella-task-complete.css`
- 상태:
  - `appearance.json`에 활성화 반영 완료
- 다음 확인:
  - Obsidian에서 snippet reload 또는 앱 재시작

### 2026-03-08 18:56 (KST)

> [!note] Obsidian snippet tuned
> 완료 task 아래 nested bullet까지 같이 묶이도록 2차 조정했다.

### 2026-03-08 19:06 (KST)

> [!note] Obsidian snippet tuned again
> Live Preview에서 완료 task 아래 child bullet과 wrapped line까지 더 넓게 묶도록 3차 조정했다.

### 2026-03-08 19:12 (KST)

> [!summary] Build smoke prep complete, teammate smoke pending
> build 생성 경로와 launcher, 전달 가이드는 준비 완료다. 다만 팀원과의 실제 Steam smoke는 아직 미실행 상태다.

### 2026-03-09 00:03 (KST)

> [!success] Steam smoke connected, but client control failed
> host/client는 같은 세션에 들어갔다. 다만 client는 조작이 되지 않았고 host와 같은 오브젝트 시점을 보고 있었다.

### 2026-03-09 04:35 (KST)

> [!success] Owner-aware camera retarget and simple batch launchers added
> Main Camera가 local owner 기준으로 target을 다시 잡도록 보정했고, build 폴더에 `RunHost.bat` / `RunClient.bat`를 자동 생성하도록 했다.

### 2026-03-09 04:48 (KST)

> [!success] Desktop/laptop Git smoke workflow added
> 두 머신이 같은 브랜치를 직접 pull/build/run하도록 `sync-and-build-steam-smoke.ps1`, `.bat`, `build-info.txt` 경계를 추가했다.

### 2026-03-09 05:00 (KST)

> [!success] Clone path auto-detect fixed
> 노트북 clone 경로가 `C:\Unity\interStella`가 아니어도 workflow script가 현재 저장소 위치를 기준으로 동작하도록 수정했다.

### 2026-03-09 05:08 (KST)

> [!success] OneDrive publish workflow added
> 데스크탑 build를 OneDrive 공유 폴더로 복사하고, 노트북은 그 폴더에서 바로 `RunClient.bat`를 실행하는 경로를 추가했다.

### 2026-03-09 06:15 (KST)

> [!success] Shared lobby file automation added
> `current-steam-lobby.txt`를 기준으로 host가 최신 lobbyId를 공유하고, client는 `RunClient.bat` 실행 시 그 값을 자동으로 읽도록 바꿨다.

- 확인된 실제 실패 원인:
  - host와 client가 서로 다른 lobbyId를 사용했다.
  - 따라서 노트북의 정지 화면은 실제 소유권 이슈 재현이 아니라 join 실패 false-negative였다.
- 새 운영 방식:
  - 데스크탑 `RunHost.bat`
  - 노트북 `RunClient.bat`
  - 수동 `lobbyId` 입력은 기본 경로가 아니다.

### 2026-03-08 18:47 (KST)

> [!success] Play Mode startup cleanup done
> direct Play Mode 기준으로 Steam warning과 Fishy null exception을 제거했다.

- 정리된 항목:
  - `Steam is probably not running` warning 제거
  - `FishySteamworks.Update()` `NullReferenceException` 제거
- 검증:
  - EditMode tests `17/17 PASS`
  - direct Play Mode 재검증 통과

### 2026-03-08 18:19 (KST)

> [!success] Steam build smoke workflow ready
> Unity 메뉴 빌드, build 실행 helper, 문서/Obsidian 동기화까지 완료.

- 빌드 메뉴:
  - `Tools/InterStella/Build/Build Steam Smoke Windows64`
- 산출물:
  - `Builds/SteamSmokeWindows64/interStella-Smoke.exe`
  - `Builds/SteamSmokeWindows64/steam_appid.txt`
- 실행 helper:
  - `.codex/workflows/netcode/launch-steam-build-smoke.ps1`
- 운영 결정:
  - 팀원과의 실제 Steam smoke는 build 경로를 우선 사용
  - 같은 프로젝트가 열린 상태의 batch build는 미지원

## Next Actions

> [!warning] 아직 직접 확인이 필요한 항목

- 수정 후 실제 Steam 2실계정 smoke 재실행
- client log에서 `PlayerOwnershipInputGate` 상태 확인
- host/local owner vs remote observer 표현 경계 재검증
- `build-info.txt` 기준 최신 build인지 먼저 확인

## Quick Run

### 1. Build

```text
Unity Menu
Tools/InterStella/Build/Build Steam Smoke Windows64
```

### 2. Host

```powershell
powershell -ExecutionPolicy Bypass -File .\.codex\workflows\netcode\launch-steam-build-smoke.ps1 -Mode host -WaitForBoot
```

### 3. Client

```powershell
powershell -ExecutionPolicy Bypass -File .\.codex\workflows\netcode\launch-steam-build-smoke.ps1 -Mode client -JoinArgs "-interstella-provider steam +connect_lobby <lobbyId>" -StrictSteamRelay -WaitForBoot
```

## Source Notes

- 상세 작업 로그:
  - `TASK.md`
- 실행 절차:
  - `Build Guide.md`
- 설계/의사결정:
  - `기획서.md`
