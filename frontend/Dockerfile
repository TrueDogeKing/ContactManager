# Multi-stage build for the frontend (Vite/React) served by nginx, using Vite+ (vp).
FROM node:22-bookworm-slim AS build
WORKDIR /app

# curl + ca-certificates are required by the Vite+ installer.
RUN apt-get update \
    && apt-get install -y --no-install-recommends curl ca-certificates \
    && rm -rf /var/lib/apt/lists/*

# Install Vite+ (vp). The installer honours VP_HOME.
ENV VP_HOME=/root/.vite-plus
RUN curl -fsSL https://vite.plus | bash
ENV PATH="${PATH}:${VP_HOME}/bin"

COPY package.json package-lock.json ./
RUN vp install --frozen-lockfile

COPY . .
RUN vp build

FROM nginx:alpine
COPY nginx.conf /etc/nginx/conf.d/default.conf
COPY --from=build /app/dist /usr/share/nginx/html
EXPOSE 8080
CMD ["nginx", "-g", "daemon off;"]
