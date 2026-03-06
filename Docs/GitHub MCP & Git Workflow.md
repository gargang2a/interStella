# GitHub MCP & Git Workflow

## 목적
- GitHub MCP 연결
- 자동 브랜치 생성
- 자동 커밋
- 자동 풀

## 1) GitHub MCP 연결 설정
적용 위치:
- 홈 설정: `C:\Users\gar\.codex\config.toml`
- 프로젝트 설정: `C:\Unity\interStella\.codex\config.toml`

적용 값:
```toml
[mcp_servers.github]
url = "https://api.githubcopilot.com/mcp/"
bearer_token_env_var = "GITHUB_PAT_TOKEN"
```

토큰 환경변수(사용자 범위) 설정:
```powershell
[Environment]::SetEnvironmentVariable("GITHUB_PAT_TOKEN", "<YOUR_PAT>", "User")
```

검증 헬퍼:
```powershell
powershell -ExecutionPolicy Bypass -File .\.codex\workflows\git\setup-github-mcp.ps1
```

주의:
- 설정 반영 후 Codex 데스크톱 앱 재시작이 필요하다.

## 2) 자동 Git 워크플로우 스크립트
위치: `.codex/workflows/git`

파일:
- `auto-branch.ps1`
- `auto-commit.ps1`
- `auto-pull.ps1`
- `auto-workflow.ps1`
- `setup-github-mcp.ps1`

## 3) 실행 예시
브랜치 자동 생성(기준 브랜치 동기화 포함):
```powershell
powershell -ExecutionPolicy Bypass -File .\.codex\workflows\git\auto-branch.ps1 -Name "repair-loop-netcode" -Base main -SyncBase
```

현재 브랜치 자동 pull(rebase):
```powershell
powershell -ExecutionPolicy Bypass -File .\.codex\workflows\git\auto-pull.ps1
```

자동 add/commit/push:
```powershell
powershell -ExecutionPolicy Bypass -File .\.codex\workflows\git\auto-commit.ps1 -Message "feat: owner interaction flow hardening" -Push
```

원샷 워크플로우(브랜치 생성 + 선택적 커밋):
```powershell
powershell -ExecutionPolicy Bypass -File .\.codex\workflows\git\auto-workflow.ps1 -Task "steam-session-fix" -Base main -CommitMessage "chore: steam session checkpoint" -Push
```

## 4) 현재 상태 메모
- 현재 작업 폴더 `C:\Unity\interStella`는 `.git`이 없어 Git 자동화 스크립트 실행 시 안전하게 중단된다.
- 실제 자동 브랜치/커밋/풀을 사용하려면:
  1. 이 폴더를 Git 저장소로 초기화하거나
  2. Git 저장소를 다시 클론한 경로에서 실행해야 한다.

## 5) 2026-03-06 실전 적용 결과
- 로컬 상태:
  - `main` 브랜치, 워킹트리 clean
  - HEAD: `711deb0a0cc6b574051b12ae795acde5a3fecd05`
- 원격 상태:
  - `origin = https://github.com/gargang2a/interStella.git`
  - `origin/main` SHA 동일 확인
- 웹 확인:
  - [interStella Repository](https://github.com/gargang2a/interStella)
- 이슈/해결:
  - 첫 푸시 중 타임아웃 발생 가능(대용량 초기 업로드)
  - 재시도에서 `reference already exists`가 나와도, 원격 SHA가 동일하면 이미 푸시 완료 상태로 판단

## 7) 2026-03-06 저장소 가시성 전환
- 대상 저장소:
  - gargang2a/interStella
- 변경 내용:
  - public -> private
- 검증:
  - 인증 조회 결과 private=true, visibility=private
  - 비인증 조회는 404(Not Found)로 비공개 정책 정상 동작

## 8) 2026-03-06 22:54 Git 자동 워크플로우 E2E
- 실행 브랜치:
  - codex/git-workflow-e2e
- 검증 순서:
  1. auto-branch.ps1 -Name git-workflow-e2e -Base main -SyncBase
  2. 변경 생성(문서 누적 로그)
  3. auto-commit.ps1 -Message "chore: verify git auto workflow e2e" -Push
  4. auto-pull.ps1 재검증
- 기대 결과:
  - 브랜치 생성/전환, 커밋/푸시, 추적 브랜치 pull 동작이 모두 자동화 스크립트로 완료
