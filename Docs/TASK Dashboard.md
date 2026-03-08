# interStella TASK Dashboard

> [!summary] 사용 원칙
> - 이 문서는 Obsidian에서 보기 좋게 읽기 위한 대시보드다.
> - 상세 이력의 원본은 `TASK.md`다.
> - 작업 상태를 갱신할 때는 `TASK.md`를 먼저 append하고, 필요 시 이 대시보드를 갱신한다.

## Current Focus

> [!tip] 현재 최우선 목표
> Steam 2실계정 기준 `host build / client build` smoke를 실제로 1회 성공시키는 것.

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

- 팀원과 실제 2실계정 `host build / client build` smoke 실행
- host log에서 lobby 생성 확인
- client log에서 lobby join / binder 적용 확인
- 성공/실패 로그 패턴을 운영 가이드에 고정

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
