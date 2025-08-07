# Stage 1: Build .NET SwiftRunner app
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build-runner

WORKDIR /src
COPY ["Runners/SwiftRunner/", "Runners/SwiftRunner/"]
COPY ["Runners/Runners.Shared/", "Runners/Runners.Shared/"]
COPY ["Shared/", "Shared/"]

WORKDIR /src/Runners/SwiftRunner
RUN dotnet restore
RUN dotnet publish -c Release -o /app/publish


# Stage 2: Swift full image (compiler + runtime)
FROM swift:5.10-bookworm AS swift-full


# Stage 3: Final runtime image with minimal Swift runtime + .NET runtime
FROM mcr.microsoft.com/dotnet/runtime:9.0-bookworm-slim AS final

# Copy swift compiler and swift binary
COPY --from=swift-full /usr/bin/swiftc /usr/bin/
COPY --from=swift-full /usr/bin/swift /usr/bin/

# Copy entire Swift libraries folder (linux, host, clang)
COPY --from=swift-full /usr/lib/swift/ /usr/lib/swift/

# Install runtime dependencies (including clang for swiftc)
RUN apt-get update && apt-get install -y --no-install-recommends \
    libatomic1 \
    libicu72 \
    libxml2 \
    libsqlite3-0 \
    libncurses6 \
    libtinfo6 \
    libz3-4 \
    libc6-dev \
    libstdc++6 \
    clang \
    build-essential \
    binutils \
    libuuid1 \       
    zlib1g \         
    && apt-get clean && rm -rf /var/lib/apt/lists/*

# Clean Swift libraries, оставляем только необходимые *.so и *.swiftmodule/*.swiftinterface
RUN find /usr/lib/swift/linux -type f ! \( \
        -name 'libswiftCore.so' -o \
        -name 'libFoundation.so' -o \
        -name 'libdispatch.so' -o \
        -name 'libswiftGlibc.so' -o \
        -name 'libswift_Concurrency.so' -o \
        -name 'libBlocksRuntime.so' -o \
        -name 'libswiftSwiftOnoneSupport.so' -o \
        -name 'libswift_StringProcessing.so' -o \
        -name 'libswift_RegexParser.so' -o \
        -name 'libswiftDispatch.so' -o \
        -name '*.swiftmodule' -o \
        -name '*.swiftinterface' \
    \) -delete && \
    find /usr/lib/swift/linux -type f -name '*.a' -delete && \
    find /usr/lib/swift/linux -type f -name '*.swiftdoc' -delete

# Не трогаем /usr/lib/swift/host — там много либ для swiftc

# Set LD_LIBRARY_PATH для поиска swift runtime libs и компилятора
ENV LD_LIBRARY_PATH=/usr/lib/swift/linux:/usr/lib/swift/host:/usr/lib/swift/clang/lib/linux:/usr/lib/x86_64-linux-gnu:/lib/x86_64-linux-gnu

# Copy published .NET app
COPY --from=build-runner /app/publish /app

WORKDIR /app

# Создаем непривилегированного пользователя
RUN useradd -u 1001 -m runneruser

# Создаем тестовый swift файл для проверки Dispatch
RUN echo 'import Foundation' > /app/test.swift && \
    echo 'import Dispatch' >> /app/test.swift && \
    echo '' >> /app/test.swift && \
    echo 'let queue = DispatchQueue.global()' >> /app/test.swift && \
    echo '' >> /app/test.swift && \
    echo 'queue.asyncAfter(deadline: .now() + 1.0) {' >> /app/test.swift && \
    echo '    print("Hello from Dispatch after 1 second!")' >> /app/test.swift && \
    echo '    exit(0)' >> /app/test.swift && \
    echo '}' >> /app/test.swift && \
    echo '' >> /app/test.swift && \
    echo 'print("Waiting...")' >> /app/test.swift && \
    echo 'dispatchMain()' >> /app/test.swift

# Компиляция swift теста + проверка зависимостей
RUN swiftc /app/test.swift -o /app/test && \
    echo "ldd output for /app/test:" && ldd /app/test

USER 1001

ENTRYPOINT ["dotnet", "SwiftRunner.dll"]
