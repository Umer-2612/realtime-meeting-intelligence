# syntax=docker/dockerfile:1

FROM ubuntu:22.04 AS build
ENV DEBIAN_FRONTEND=noninteractive
RUN apt-get update && apt-get install -y \
    build-essential \
    cmake \
    pkg-config \
    zlib1g-dev \
    libglib2.0-dev \
    libgtk-3-dev \
    libgio2.0-dev \
    libcurl4-openssl-dev \
    libpulse-dev \
    qtbase5-dev \
    qtchooser \
    qtbase5-dev-tools \
    && rm -rf /var/lib/apt/lists/*

WORKDIR /workspace
COPY ./src/demo /workspace/demo
RUN cmake -S demo -B build -G "Unix Makefiles"
RUN cmake --build build --config Release -- -j$(nproc)

FROM ubuntu:22.04 AS runtime
ENV DEBIAN_FRONTEND=noninteractive
RUN apt-get update && apt-get install -y \
    libglib2.0-0 \
    libgtk-3-0 \
    libgio2.0-0 \
    libcurl4 \
    libpulse0 \
    qtbase5-dev \
    && rm -rf /var/lib/apt/lists/*

WORKDIR /opt/zoom-bot
COPY --from=build /workspace/demo/bin ./bin

ENV LD_LIBRARY_PATH="/opt/zoom-bot/bin:${LD_LIBRARY_PATH}"
CMD ["/opt/zoom-bot/bin/meetingSDKDemo"]
