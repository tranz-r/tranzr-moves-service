#!/bin/sh
set -e

echo "<====================== Running notifications migration ======================>"

if [ -z "$DB_CONNECTION_STRING" ]; then
  echo "DB_CONNECTION_STRING is required"
  exit 1
fi

./migrator --connection "$DB_CONNECTION_STRING"
