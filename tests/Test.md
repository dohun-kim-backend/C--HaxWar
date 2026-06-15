# .NET CLI 프로젝트 관리 명령어 정리

## 1. 솔루션(.sln) 관리 명령어

### 솔루션에 프로젝트 추가 (`dotnet sln add`)
```bash
dotnet sln [솔루션파일명] add [프로젝트파일경로]
```
* **예시**: `dotnet sln HexWar.sln add tests\HexWar.LoadTests\HexWar.LoadTests.csproj`

### 솔루션에서 프로젝트 제거 (`dotnet sln remove`)
> [!NOTE]
> 솔루션 구조에서만 연결이 해제되며, 실제 하드디스크의 물리 폴더나 소스코드는 지워지지 않습니다.
```bash
dotnet sln [솔루션파일명] remove [프로젝트파일경로]
```
* **예시**: `dotnet sln HexWar.sln remove tests\HexWar.Domain.Tests\HexWar.Domain.Tests.csproj`

---

## 2. 프로젝트 생성 및 의존성 관리 명령어

### 새 프로젝트 생성 (`dotnet new`)
```bash
dotnet new [템플릿명] -n [프로젝트명] -o [디렉터리경로]
```
* **콘솔 앱 예시**: `dotnet new console -n HexWar.LoadTests -o tests/HexWar.LoadTests`
* **NUnit 테스트 예시**: `dotnet new nunit -n HexWar.LoadTests -o tests/HexWar.LoadTests`

### 프로젝트 간 참조 추가 (`dotnet add reference`)
```bash
dotnet add [대상프로젝트] reference [참조할프로젝트]
```
* **예시**: `dotnet add tests/HexWar.LoadTests/HexWar.LoadTests.csproj reference src/HexWar.Domain/HexWar.Domain.csproj`

### 외부 NuGet 패키지 추가 (`dotnet add package`)
```bash
dotnet add [대상프로젝트] package [패키지명] --version [버전]
```
* **예시**: `dotnet add tests/HexWar.Benchmarks/HexWar.Benchmarks.csproj package NUnit`

---

## 3. 디렉터리 물리 삭제 (OS 쉘 명령어)
솔루션에서 프로젝트 연결을 끊은 후 디스크에서 실제 소스코드 폴더를 영구 삭제할 때 사용합니다.

### PowerShell (Windows Terminal 기본값)
```powershell
Remove-Item -Recurse -Force [폴더경로]
```
* **예시**: `Remove-Item -Recurse -Force tests\HexWar.Domain.Tests`

### Windows CMD (명령 프롬프트)
```cmd
rmdir /s /q [폴더경로]
```
* **예시**: `rmdir /s /q tests\HexWar.Domain.Tests`