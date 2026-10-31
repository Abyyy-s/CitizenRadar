#!/usr/bin/env bash
# ==============================================================================
# ⚡ CITIZEN RADAR — STOP LIVE SHARING SCRIPT
# ==============================================================================

CYAN='\033[0;36m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BOLD='\033[1m'
NC='\033[0m'

echo -e "${CYAN}Stopping CitizenRadar and Cloudflare live tunnels...${NC}"

# Kill cloudflared
if pgrep -f "cloudflared tunnel --url http://localhost:5000" >/dev/null 2>&1; then
    pkill -f "cloudflared tunnel --url http://localhost:5000" 2>/dev/null || true
    echo -e "${GREEN}✓ Cloudflare tunnel terminated.${NC}"
else
    echo -e "${YELLOW}• No active Cloudflare tunnel found.${NC}"
fi

# Kill CitizenRadar process or free port 5000
if ss -tulpn 2>/dev/null | grep -q ":5000 "; then
    fuser -k 5000/tcp 2>/dev/null || true
    echo -e "${GREEN}✓ Released port 5000.${NC}"
fi

pkill -f "CitizenRadar" 2>/dev/null || true
rm -f /tmp/citizenradar_*.log

echo -e "${BOLD}${GREEN}✓ All CitizenRadar services cleanly stopped.${NC}"
