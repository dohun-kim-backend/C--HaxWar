FROM mcr.microsoft.com/dotnet/sdk:9.0
WORKDIR /app

# 캐시 최적화 및 프로젝트 구조 복사
COPY HexWar.sln ./
COPY src/ src/
COPY tests/ tests/

# 의존성 패키지 복원
RUN dotnet restore tests/HexWar.Benchmarks/HexWar.Benchmarks.csproj

# BenchmarkDotNet은 자체 컴파일을 위해 SDK 환경이 꼭 필요하므로 SDK 이미지 내에서 직접 릴리즈 빌드로 구동합니다.
ENTRYPOINT ["dotnet", "run", "-c", "Release", "--project", "tests/HexWar.Benchmarks"]
