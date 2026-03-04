#!/bin/sh

prog="WaveLink.Plugin"
log="${prog}.log.txt"

# ensure executable
chmod +x "./$prog"

# find running PID(s) of the exact command "./WaveLink.Plugin"
pids="$(ps -ax -o pid= -o command= | awk -v p="./$prog" '$0 ~ ("^ *[0-9]+ " p "$") {print $1}')"

if [ -n "$pids" ]; then
  echo "$(date +"%F %T%Z"): $prog already running, killing it to start again"
  for pid in $pids; do
    kill -9 "$pid" 2>/dev/null
  done
  sleep 1
fi

# start in background, redirect stdout/stderr
"./$prog" > "$log" 2>&1 &