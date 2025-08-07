# Stage 1: Скачиваем и распаковываем Swift
FROM ubuntu:22.04 AS swift-download

RUN apt-get update && apt-get install -y curl ca-certificates tar
ENV SWIFT_VERSION=5.10
ENV SWIFT_URL=https://download.swift.org/swift-${SWIFT_VERSION}-release/ubuntu2204/swift-${SWIFT_VERSION}-RELEASE/swift-${SWIFT_VERSION}-RELEASE-ubuntu22.04.tar.gz

WORKDIR /swift
RUN curl -LO "$SWIFT_URL" \
    && tar -xzf swift-${SWIFT_VERSION}-RELEASE-ubuntu22.04.tar.gz \
    && mv swift-${SWIFT_VERSION}-RELEASE-ubuntu22.04 swift

RUN rm -rf swift/usr/lib/swift_static \
           swift/usr/lib/swift/pm \
           swift/usr/lib/swiftPrivate \
           swift/usr/lib/swift/linux/libXCTest* \
           swift/usr/lib/swift/linux/liblldb* \
           swift/usr/lib/swift/Block*

RUN find swift/usr/bin -type f -executable -exec strip --strip-unneeded {} + || true

# Stage 2: Сборка .NET раннера
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build

WORKDIR /src
COPY ["Runners/SwiftRunner/", "Runners/SwiftRunner/"]
COPY ["Runners/Runners.Shared/", "Runners/Runners.Shared/"]
COPY ["Shared/", "Shared/"]
WORKDIR /src/Runners/SwiftRunner
RUN dotnet restore
RUN dotnet publish -c Release -o /app/publish

# Stage 3: Финальный рантайм
FROM mcr.microsoft.com/dotnet/runtime:9.0

RUN apt-get update && apt-get install -y \
    libicu-dev libxml2-dev libz-dev libsqlite3-dev clang libpython3.10 \
    && rm -rf /var/lib/apt/lists/*

COPY --from=swift-download /swift/swift/usr/bin /swift/usr/bin
COPY --from=swift-download /swift/swift/usr/lib /swift/usr/lib

ENV PATH="/swift/usr/bin:$PATH"

COPY --from=build /app/publish /app
WORKDIR /app

RUN useradd -u 1001 -m runneruser
USER 1001

ENTRYPOINT ["dotnet", "SwiftRunner.dll"]
