# interStella Codex Workflow Pack

## 목적
- `everything-claude-code`에서 interStella(Unity/FishNet) 작업에 직접 도움이 되는 운영 요소만 부분 차용한다.
- 전체 도구 체인 설치 대신, 현재 프로젝트 규칙과 충돌 없는 실행 패턴만 고정한다.

## 설치된 구성 (Partial)
1. Codex 실행 프로필
- 경로: `.codex/config.toml`
- 포함: 권한 정책, 작업 루프, 네트워크 DoD, 문서 Append-Only 강제 지침

2. 표준 작업 루프
- 탐색 -> 최소 변경 구현 -> 검증 -> 문서 누적 기록 -> 보고
- 네트워크 변경 시 로컬 스모크만으로 완료 처리하지 않는다.

3. 네트워크 DoD (완료 정의)
- Owner만 입력 가능
- Remote는 관찰만 가능
- 연결 해제 시 슬롯 회수 로그 확인
- 재접속 시 슬롯 재할당 로그 확인
- durable/transient 경계가 코드와 로그에서 확인 가능

4. 실행/검증 명령 스니펫
```powershell
# Host (기본)
# FishNetSessionService 기본 startupMode=Host 사용
```

```powershell
# Client override (같은 머신 2프로세스 기준)
-interstella-mode client -interstella-address 127.0.0.1 -interstella-port 7770
```

```text
# 슬롯 검증 로그 패턴
Assigned client {id} to slot {index} ({playerName})
Released slot {index} from client {id}
```

5. 문서 운영 포맷
- 변경 발생 시 3개 문서에 Append-Only로 누적:
  - `Docs/기획서.md`
  - `Docs/TASK.md`
  - `Docs/Build Guide.md`
- 권장 3줄 포맷:
  - 변경 이유
  - 검증 결과
  - 다음 리스크/다음 작업

## 주의
- 본 팩은 Unity/C# 중심 프로젝트에 맞춘 최소 적용이다.
- 외부 레포의 언어별 규칙(TypeScript/Python 등)은 interStella MVP 범위에서는 적용하지 않는다.
