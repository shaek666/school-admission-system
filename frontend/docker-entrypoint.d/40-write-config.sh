#!/bin/sh
# Writes /usr/share/nginx/html/config.js from API_BASE_URL at container start.
set -eu

url="${API_BASE_URL:-http://localhost:8080}"
# Escape backslash and double quote; drop control characters (newlines etc.)
# so a hostile value cannot break out of the JS string.
escaped=$(printf '%s' "$url" | tr -d '\000-\037' | sed -e 's/\\/\\\\/g' -e 's/"/\\"/g' -e 's#</#<\\/#g')

printf 'window.APP_CONFIG = { apiBaseUrl: "%s" };\n' "$escaped" > /usr/share/nginx/html/config.js
