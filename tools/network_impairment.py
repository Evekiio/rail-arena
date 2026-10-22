"""Local-only UDP impairment relay for repeatable standalone tests."""
import argparse
import heapq
import json
import os
from pathlib import Path
import random
import select
import socket
import time


def run(args):
    server = ("127.0.0.1", args.server_port)
    client = None
    rng = random.Random(args.seed)
    queue = []
    sequence = 0
    last_sent = {"to_host": -1, "to_client": -1}
    totals = {"received": 0, "forwarded": 0, "dropped": 0, "foreign": 0,
              "reordered": 0, "queueOverflow": 0, "maximumQueued": 0,
              "to_host": 0, "to_client": 0, "dropped_to_host": 0, "dropped_to_client": 0}
    delay_total = 0.0
    delay_min = float("inf")
    delay_max = 0.0
    started = time.monotonic()
    last_save = float("-inf")

    def save(running):
        result = dict(totals, running=running, configuredOneWayMs=args.delay_ms,
                      configuredJitterMs=args.jitter_ms, configuredLoss=args.loss,
                      seed=args.seed, elapsedSeconds=time.monotonic() - started,
                      meanAppliedMs=delay_total / max(1, totals["forwarded"]),
                      minimumAppliedMs=delay_min if totals["forwarded"] else 0,
                      maximumAppliedMs=delay_max, pending=len(queue))
        temporary = args.stats.with_suffix(".tmp")
        temporary.write_text(json.dumps(result, indent=2) + "\n", encoding="utf-8")
        # Windows readers can briefly deny delete sharing while observing the report.
        # A status-file collision must not stop live packet delivery.
        for attempt in range(100 if not running else 3):
            try:
                os.replace(temporary, args.stats)
                return
            except PermissionError:
                time.sleep(.025)
        if not running:
            raise PermissionError(f"Could not publish final relay statistics: {args.stats}")

    args.stats.parent.mkdir(parents=True, exist_ok=True)
    with socket.socket(socket.AF_INET, socket.SOCK_DGRAM) as udp:
        udp.bind(("127.0.0.1", args.listen_port))
        udp.setblocking(False)
        save(True)
        while time.monotonic() - started < args.duration and not args.stop_file.exists():
            now = time.monotonic()
            timeout = min(.1, max(0, queue[0][0] - now)) if queue else .1
            readable, _, _ = select.select([udp], [], [], timeout)
            if readable:
                for _ in range(128):
                    try:
                        payload, sender = udp.recvfrom(65536)
                    except (BlockingIOError, ConnectionResetError):
                        break
                    now = time.monotonic()
                    if sender == server:
                        if client is None:
                            totals["foreign"] += 1
                            continue
                        destination, direction = client, "to_client"
                    elif client is None or sender == client:
                        client = sender
                        destination, direction = server, "to_host"
                    else:
                        totals["foreign"] += 1
                        continue
                    totals["received"] += 1
                    sequence += 1
                    if rng.random() < args.loss:
                        totals["dropped"] += 1
                        totals["dropped_" + direction] += 1
                        continue
                    if len(queue) >= 4096:
                        totals["queueOverflow"] += 1
                        continue
                    delay = max(0, args.delay_ms + rng.uniform(-args.jitter_ms, args.jitter_ms)) / 1000
                    heapq.heappush(queue, (now + delay, sequence, now, destination, direction, payload))
                    totals["maximumQueued"] = max(totals["maximumQueued"], len(queue))
            now = time.monotonic()
            while queue and queue[0][0] <= now:
                due, order, received, destination, direction, payload = heapq.heappop(queue)
                try:
                    udp.sendto(payload, destination)
                except (BlockingIOError, ConnectionResetError):
                    totals["dropped"] += 1
                    totals["dropped_" + direction] += 1
                    continue
                applied = (time.monotonic() - received) * 1000
                delay_total += applied
                delay_min = min(delay_min, applied)
                delay_max = max(delay_max, applied)
                totals["forwarded"] += 1
                totals[direction] += 1
                if order < last_sent[direction]:
                    totals["reordered"] += 1
                last_sent[direction] = max(last_sent[direction], order)
            if now - last_save >= 1:
                save(True)
                last_save = now
        save(False)


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--listen-port", type=int, required=True)
    parser.add_argument("--server-port", type=int, required=True)
    parser.add_argument("--delay-ms", type=float, default=25)
    parser.add_argument("--jitter-ms", type=float, default=10)
    parser.add_argument("--loss", type=float, default=.05)
    parser.add_argument("--seed", type=int, default=7191)
    parser.add_argument("--duration", type=float, default=180)
    parser.add_argument("--stats", type=Path, required=True)
    parser.add_argument("--stop-file", type=Path, required=True)
    options = parser.parse_args()
    if not (1 <= options.listen_port <= 65535 and 1 <= options.server_port <= 65535 and
            options.listen_port != options.server_port and 0 <= options.delay_ms <= 1000 and
            0 <= options.jitter_ms <= 1000 and 0 <= options.loss < 1 and 1 <= options.duration <= 1800):
        parser.error("Invalid port, delay, jitter, loss or duration.")
    run(options)
