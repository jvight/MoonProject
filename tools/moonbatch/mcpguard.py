"""
Keeps a batch Unity on a worktree away from the main editor's Unity MCP server (com.ivanmurzak.unity.mcp).

Without this, the plugin in a worktree editor would, on startup:
  * download its ~100 MB server binary into the worktree Library and auto-start a second server,
  * connect to a server (Cloud mode by default, or whatever host a copied config names),
  * regenerate skill files into the tracked .claude/skills folder if an agent is selected in PlayerPrefs.

The plugin reads, in priority order, CLI flags > UNITY_MCP_* environment variables > the worktree's
UserSettings/AI-Game-Developer-Config.json > built-in defaults, and treats CI=true as "no download, no server
auto-start, no auto-connect". We set all three layers so the batch editor never opens a socket to the MCP server.
The host points at the TCP discard port on loopback, which nothing listens on.
"""
import json
from pathlib import Path

CONFIG_RELATIVE = Path("UserSettings") / "AI-Game-Developer-Config.json"
UNREACHABLE_HOST = "http://127.0.0.1:9"

ENVIRONMENT = {
    "CI": "true",
    "UNITY_MCP_KEEP_CONNECTED": "false",
    "UNITY_MCP_START_SERVER": "false",
    "UNITY_MCP_CONNECTION_MODE": "Custom",
    "UNITY_MCP_HOST": UNREACHABLE_HOST,
}

SAFE_CONFIG = {
    "host": UNREACHABLE_HOST,
    "keepServerRunning": False,
    "keepConnected": False,
    "connectionMode": "Custom",
    "generateSkillFiles": False,
    "skillAutoGenerate": {},
}


def child_environment(base_env):
    env = dict(base_env)
    env.update(ENVIRONMENT)
    return env


def write_worktree_config(project_root):
    """Merges the safe values into the worktree's (git-ignored) plugin config. Returns the config path."""
    path = Path(project_root) / CONFIG_RELATIVE
    data = {}
    if path.exists():
        try:
            data = json.loads(path.read_text(encoding="utf-8-sig"))
        except ValueError:
            data = {}
        if not isinstance(data, dict):
            data = {}
    if all(data.get(key) == value for key, value in SAFE_CONFIG.items()):
        return path
    data.update(SAFE_CONFIG)
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(json.dumps(data, indent=2) + "\n", encoding="utf-8")
    return path
