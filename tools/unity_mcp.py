#!/usr/bin/env python3
"""
Minimal client for the Unity MCP server (ai-game-developer, streamable HTTP) so scripts and agents without
native MCP tools can drive the running Unity editor of the MAIN project.

Usage:
  python tools/unity_mcp.py list                          # list tool names
  python tools/unity_mcp.py call <tool> '<json-args>'     # call a tool, print text result
  python tools/unity_mcp.py call <tool> @args.json        # args from file

Image results (screenshots) are saved under Logs/mcp/ and their paths printed.

Only the Director should drive the main editor (see CLAUDE.md).
"""
import base64
import json
import sys
import time
import urllib.error
import urllib.request
from pathlib import Path

DEFAULT_URL = "http://localhost:25906"
ENDPOINTS = ("/mcp", "/")


class McpClient:
    def __init__(self, base=DEFAULT_URL, timeout=300):
        self.base = base.rstrip("/")
        self.timeout = timeout
        self.session = None
        self.endpoint = None
        self._id = 0

    def _post(self, url, payload):
        headers = {"Content-Type": "application/json", "Accept": "application/json, text/event-stream"}
        if self.session:
            headers["Mcp-Session-Id"] = self.session
        req = urllib.request.Request(url, data=json.dumps(payload).encode(), headers=headers, method="POST")
        with urllib.request.urlopen(req, timeout=self.timeout) as resp:
            sid = resp.headers.get("Mcp-Session-Id")
            if sid:
                self.session = sid
            body = resp.read().decode("utf-8", errors="replace")
            ctype = resp.headers.get("Content-Type", "")
        if not body.strip():
            return None
        if "text/event-stream" in ctype:
            result = None
            for line in body.splitlines():
                if line.startswith("data:"):
                    msg = json.loads(line[5:].strip())
                    if msg.get("id") == payload.get("id"):
                        result = msg
            return result
        return json.loads(body)

    def request(self, method, params=None):
        self._id += 1
        payload = {"jsonrpc": "2.0", "id": self._id, "method": method, "params": params or {}}
        msg = self._post(self.endpoint, payload)
        if msg is None:
            raise RuntimeError(f"no response to {method}")
        if "error" in msg:
            raise RuntimeError(f"{method} failed: {msg['error']}")
        return msg["result"]

    def connect(self):
        init = {"protocolVersion": "2025-06-18", "capabilities": {},
                "clientInfo": {"name": "moonproject-tools", "version": "1.0"}}
        last = None
        for ep in ENDPOINTS:
            self.endpoint = self.base + ep
            try:
                self.request("initialize", init)
                self._post(self.endpoint, {"jsonrpc": "2.0", "method": "notifications/initialized"})
                return self
            except (urllib.error.HTTPError, RuntimeError, json.JSONDecodeError) as exc:
                last = exc
                self.session = None
        raise RuntimeError(f"could not initialise MCP session at {self.base}: {last}")

    def list_tools(self):
        return self.request("tools/list")["tools"]

    def call(self, name, arguments):
        return self.request("tools/call", {"name": name, "arguments": arguments})


def save_image(tool, index, item):
    """Writes an MCP image result to Logs/mcp/ (git-ignored) and returns the path."""
    ext = {"image/png": "png", "image/jpeg": "jpg"}.get(item.get("mimeType"), "bin")
    out_dir = Path(__file__).resolve().parent.parent / "Logs" / "mcp"
    out_dir.mkdir(parents=True, exist_ok=True)
    path = out_dir / f"{time.strftime('%Y%m%d-%H%M%S')}-{tool}-{index}.{ext}"
    path.write_bytes(base64.b64decode(item["data"]))
    return path


def main():
    if len(sys.argv) < 2 or sys.argv[1] not in ("list", "call"):
        sys.exit(__doc__)
    client = McpClient().connect()
    if sys.argv[1] == "list":
        for tool in client.list_tools():
            print(tool["name"])
        return
    name = sys.argv[2]
    raw = sys.argv[3] if len(sys.argv) > 3 else "{}"
    if raw.startswith("@"):
        with open(raw[1:], encoding="utf-8") as f:
            raw = f.read()
    result = client.call(name, json.loads(raw))
    for index, item in enumerate(result.get("content", [])):
        if item.get("type") == "text":
            print(item["text"])
        elif item.get("type") == "image":
            print(f"image saved: {save_image(name, index, item)}")
        else:
            print(f"<{item.get('type')} content omitted>")
    if result.get("isError"):
        sys.exit(1)


if __name__ == "__main__":
    main()
