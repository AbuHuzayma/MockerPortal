#!/bin/sh
# Regenerates env-config.js from environment variables at container start —
# nginx-unprivileged runs every executable script under
# /docker-entrypoint.d/ before starting nginx. This is what lets one built
# image be promoted across DEV/QA/PREPROD (docs/13-deployment.md §7)
# instead of baking VITE_API_BASE_URL in at build time.
set -eu

CONFIG_FILE=/usr/share/nginx/html/env-config.js

cat > "$CONFIG_FILE" <<EOF
window.__ENV__ = {
  VITE_API_BASE_URL: "${VITE_API_BASE_URL:-/api/v1}"
};
EOF
