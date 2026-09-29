# Build context is frontend/portal-ui itself (see .gitlab-ci.yml):
#   docker build -f frontend/portal-ui/Dockerfile frontend/portal-ui
FROM node:20-alpine AS build
WORKDIR /src

COPY package.json package-lock.json ./
RUN npm ci

COPY . .
RUN npm run build

# nginxinc/nginx-unprivileged: same reasoning as the backend image — runs as
# an arbitrary non-root UID, listens on 8080, compatible with OpenShift's
# default restricted SCC with no extra `oc adm policy` grants needed.
FROM nginxinc/nginx-unprivileged:1.27-alpine
COPY --from=build /src/dist /usr/share/nginx/html
COPY nginx.conf /etc/nginx/conf.d/default.conf
COPY docker-entrypoint.d/env-config.sh /docker-entrypoint.d/90-env-config.sh

EXPOSE 8080
