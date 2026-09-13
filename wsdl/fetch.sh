#!/bin/sh
# Downloads every WSDL/XSD SharpOnvif generates from, mirroring the remote URL layout
# under this directory so that relative schemaLocation/location references resolve offline.
#
# The generator never touches the network: it reads the mirrored copies committed here.
# Re-run this only to pick up upstream specification changes, then review the diff.
set -e
cd "$(dirname "$0")"

fetch() {
    dest="$(printf '%s' "$1" | sed 's|^https\{0,1\}://||')"
    mkdir -p "$(dirname "$dest")"
    printf '%-72s' "$dest"
    curl -sSL --fail -m 60 -o "$dest" "$1"
    printf 'ok (%s bytes)\n' "$(wc -c < "$dest" | tr -d ' ')"
}

while IFS= read -r url; do
    case "$url" in ''|\#*) continue ;; esac
    fetch "$url"
done < sources.txt
