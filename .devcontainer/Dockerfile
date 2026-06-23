# Dev container: Node (frontend) + .NET 10 SDK (backend) + Vite+ (vp/vpr) w jednym obrazie.
FROM mcr.microsoft.com/devcontainers/typescript-node:1-22-bookworm

# .NET 10 SDK z oficjalnego repozytorium Microsoftu (Debian 12 / bookworm)
# + curl/ca-certificates wymagane przez instalator Vite+.
RUN wget https://packages.microsoft.com/config/debian/12/packages-microsoft-prod.deb -O packages-microsoft-prod.deb && \
    dpkg -i packages-microsoft-prod.deb && \
    rm packages-microsoft-prod.deb && \
    apt-get update && \
    apt-get install -y --no-install-recommends dotnet-sdk-10.0 curl ca-certificates && \
    rm -rf /var/lib/apt/lists/*

# Vite+ (komenda vp) instalowane dla użytkownika node, do katalogu w jego HOME.
# `vpr` (= `vp run`) dorzucamy jako deterministyczny wrapper, bo instalator nie zawsze
# tworzy ten symlink (zależnie od użytkownika/środowiska).
USER node
ENV VP_HOME=/home/node/.vite-plus
RUN curl -fsSL https://vite.plus | bash && \
    printf '#!/bin/sh\nexec vp run "$@"\n' > "${VP_HOME}/bin/vpr" && \
    chmod +x "${VP_HOME}/bin/vpr"
# vp/vpr na PATH – dopisane na KOŃCU, by nie przesłaniać systemowego node/npm.
ENV PATH="${PATH}:${VP_HOME}/bin"

# mise (task runner z mise.toml — db:up/down/reset/logs, ef:add, ef:update).
ENV MISE_DATA_DIR=/home/node/.local/share/mise
RUN curl -fsSL https://mise.run | sh
ENV PATH="${PATH}:/home/node/.local/bin"
