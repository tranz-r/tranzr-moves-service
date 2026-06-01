#!/usr/bin/env bash
# Renders all documents/**/*.puml to PNG via PlantUML Server (Docker).
# Usage (from repository root): ./scripts/render-plantuml.sh

set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$ROOT_DIR"

PLANTUML_IMAGE="${PLANTUML_IMAGE:-plantuml/plantuml-server:jetty}"
PLANTUML_CONTAINER="${PLANTUML_CONTAINER:-plantuml-render}"
PLANTUML_SERVER_URL="${PLANTUML_SERVER_URL:-http://localhost:8080}"
STARTED_CONTAINER=false

cleanup() {
  if [[ "$STARTED_CONTAINER" == true ]]; then
    docker rm -f "$PLANTUML_CONTAINER" >/dev/null 2>&1 || true
  fi
}
trap cleanup EXIT

health_check_payload() {
  local probe_file
  probe_file="$(mktemp)"
  printf '@startuml\na -> b\n@enduml\n' >"$probe_file"
  if curl -sf "${PLANTUML_SERVER_URL}/png" -o /dev/null -X POST \
    -H "Content-Type: text/plain" --data-binary "@${probe_file}" 2>/dev/null; then
    rm -f "$probe_file"
    return 0
  fi
  rm -f "$probe_file"
  return 1
}

wait_for_server() {
  local attempts=0
  sleep 3
  until health_check_payload; do
    attempts=$((attempts + 1))
    if [[ $attempts -ge 90 ]]; then
      echo "PlantUML server did not become ready at ${PLANTUML_SERVER_URL}" >&2
      exit 1
    fi
    sleep 2
  done
}

ensure_server() {
  if health_check_payload; then
    echo "Using existing PlantUML server at ${PLANTUML_SERVER_URL}"
    return
  fi

  if docker ps -a --format '{{.Names}}' | grep -qx "$PLANTUML_CONTAINER"; then
    docker rm -f "$PLANTUML_CONTAINER" >/dev/null
  fi

  echo "Starting PlantUML server (${PLANTUML_IMAGE})..."
  docker run -d --name "$PLANTUML_CONTAINER" -p 8080:8080 "$PLANTUML_IMAGE" >/dev/null
  STARTED_CONTAINER=true
  wait_for_server
  echo "PlantUML server ready."
}

render_puml() {
  local puml_file="$1"
  local feature_dir images_dir base_name out_file

  feature_dir="$(dirname "$puml_file")"
  images_dir="${feature_dir}/images"
  base_name="$(basename "$puml_file" .puml)"
  out_file="${images_dir}/${base_name}.png"

  mkdir -p "$images_dir"

  echo "Rendering ${puml_file} -> ${out_file}"
  if ! curl -sf "${PLANTUML_SERVER_URL}/png" -o "$out_file" -X POST \
    -H "Content-Type: text/plain" --data-binary "@${puml_file}"; then
    echo "Failed to render ${puml_file}" >&2
    return 1
  fi

  if [[ ! -s "$out_file" ]]; then
    echo "Output file is empty: ${out_file}" >&2
    return 1
  fi
}

main() {
  if ! command -v docker >/dev/null 2>&1; then
    echo "Docker is required to run this script." >&2
    exit 1
  fi

  if ! command -v curl >/dev/null 2>&1; then
    echo "curl is required to run this script." >&2
    exit 1
  fi

  mapfile -t puml_files < <(find documents -name '*.puml' -type f | sort)
  if [[ ${#puml_files[@]} -eq 0 ]]; then
    echo "No .puml files found under documents/" >&2
    exit 1
  fi

  ensure_server

  local failed=0
  for puml_file in "${puml_files[@]}"; do
    if ! render_puml "$puml_file"; then
      failed=1
    fi
  done

  if [[ $failed -ne 0 ]]; then
    exit 1
  fi

  echo "Done. Rendered ${#puml_files[@]} diagram(s)."
}

main "$@"
