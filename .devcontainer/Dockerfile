# Dev container: Node (frontend) + .NET 10 SDK (backend) w jednym obrazie.
FROM mcr.microsoft.com/devcontainers/typescript-node:1-22-bookworm

# Instalacja .NET 10 SDK z oficjalnego repozytorium Microsoftu (Debian 12 / bookworm).
RUN wget https://packages.microsoft.com/config/debian/12/packages-microsoft-prod.deb -O packages-microsoft-prod.deb && \
    dpkg -i packages-microsoft-prod.deb && \
    rm packages-microsoft-prod.deb && \
    apt-get update && \
    apt-get install -y dotnet-sdk-10.0 && \
    rm -rf /var/lib/apt/lists/*
