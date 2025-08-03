#!/bin/sh

echo "<============================ Running migration script ============================>"

./migrator --connection "$DB_CONNECTION_STRING"