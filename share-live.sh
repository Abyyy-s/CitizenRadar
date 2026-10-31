#!/usr/bin/env bash
# ==============================================================================
# ⚡ CITIZEN RADAR — ONE-CLICK LIVE SHARING SCRIPT
# ==============================================================================
# Instantly launches CitizenRadar with CUDA GPU acceleration and creates a free,
# secure public HTTPS tunnel (via Cloudflare) so you can share your live AI
# traffic dashboard and stream with friends anywhere in the world.
# ==============================================================================

set -e

# ANSI Color Codes
CYAN='\033[0;36m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
RED='\033[0;31m'
PURPLE='\033[0;35m'
BOLD='\033[1m'
NC='\033[0m' # No Color

PROJECT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
cd "$PROJECT_DIR"

# Ensure local bin is in PATH
export PATH="$HOME/.local/bin:$PATH"

DOTNET_PID=""
CF_PID=""
STARTED_DOTNET=0

# Clean shutdown handler
cleanup() {
    echo ""
    echo -e "${YELLOW}🛑 Shutting down live sharing session...${NC}"
    if [ -n "$CF_PID" ] && kill -0 "$CF_PID" 2>/dev/null; then
        kill "$CF_PID" 2>/dev/null || true
        wait "$CF_PID" 2>/dev/null || true
    fi
    if [ "$STARTED_DOTNET" -eq 1 ] && [ -n "$DOTNET_PID" ] && kill -0 "$DOTNET_PID" 2>/dev/null; then
        echo -e "${YELLOW}Stopping CitizenRadar backend (PID $DOTNET_PID)...${NC}"
        kill "$DOTNET_PID" 2>/dev/null || true
        wait "$DOTNET_PID" 2>/dev/null || true
    fi
    echo -e "${GREEN}✓ Done. Port 5000 and Cloudflare tunnel released.${NC}"
    exit 0
}

trap cleanup SIGINT SIGTERM EXIT

echo -e "${CYAN}╔══════════════════════════════════════════════════════════════════════════════╗${NC}"
echo -e "${CYAN}║                    ⚡ CITIZEN RADAR — LIVE SHARING LAUNCHER                   ║${NC}"
echo -e "${CYAN}║               AI Monocular Traffic Watchdog & Real-Time Stream               ║${NC}"
echo -e "${CYAN}╚══════════════════════════════════════════════════════════════════════════════╝${NC}"
echo ""

# 1. Check for cloudflared, install if missing
if ! command -v cloudflared &>/dev/null; then
    echo -e "${YELLOW}⚙️  cloudflared not detected. Downloading official binary to ~/.local/bin...${NC}"
    mkdir -p "$HOME/.local/bin"
    curl -sL https://github.com/cloudflare/cloudflared/releases/latest/download/cloudflared-linux-amd64 -o "$HOME/.local/bin/cloudflared"
    chmod +x "$HOME/.local/bin/cloudflared"
    export PATH="$HOME/.local/bin:$PATH"
    echo -e "${GREEN}✓ cloudflared installed successfully.${NC}"
fi

# 2. Check if CitizenRadar is already running on port 5000
IS_RUNNING=0
if curl -s -f -m 1 http://localhost:5000/api/status >/dev/null 2>&1; then
    IS_RUNNING=1
    echo -e "${GREEN}✓ CitizenRadar is already running and active on http://localhost:5000.${NC}"
else
    echo -e "${PURPLE}🚀 Starting CitizenRadar backend with CUDA GPU acceleration...${NC}"
    
    # Check if port 5000 is held by another dead process
    if ss -tulpn 2>/dev/null | grep -q ":5000 "; then
        echo -e "${YELLOW}⚠️  Port 5000 in use by another process. Freeing port...${NC}"
        fuser -k 5000/tcp 2>/dev/null || true
        sleep 1
    fi

    # Launch dotnet run in background
    mkdir -p data models
    if [ ! -f "models/yolov8n.onnx" ]; then
        echo -e "${YELLOW}Downloading YOLOv8n ONNX model...${NC}"
        curl -sL https://github.com/ultralytics/assets/releases/download/v8.2.0/yolov8n.onnx -o models/yolov8n.onnx
    fi

    dotnet run -c Release data/traffic.mp4 models/yolov8n.onnx > /tmp/citizenradar_backend.log 2>&1 &
    DOTNET_PID=$!
    STARTED_DOTNET=1

    echo -ne "${YELLOW}⏳ Waiting for neural engine and CUDA initialization...${NC}"
    TRIES=0
    MAX_TRIES=45
    until curl -s -f -m 1 http://localhost:5000/api/status >/dev/null 2>&1; do
        if ! kill -0 "$DOTNET_PID" 2>/dev/null; then
            echo ""
            echo -e "${RED}❌ CitizenRadar backend failed to start. Log output:${NC}"
            cat /tmp/citizenradar_backend.log
            exit 1
        fi
        sleep 1
        echo -ne "."
        TRIES=$((TRIES + 1))
        if [ "$TRIES" -ge "$MAX_TRIES" ]; then
            echo ""
            echo -e "${RED}❌ Timed out waiting for backend to start.${NC}"
            cat /tmp/citizenradar_backend.log
            exit 1
        fi
    done
    echo ""
    echo -e "${GREEN}✓ CitizenRadar backend is healthy and processing frames!${NC}"
fi

# 3. Detect Local Network IP
LOCAL_IP=$(hostname -I 2>/dev/null | awk '{print $1}')
if [ -z "$LOCAL_IP" ]; then
    LOCAL_IP="localhost"
fi

# 4. Launch Cloudflare Tunnel
echo -e "${PURPLE}🌐 Establishing secure global HTTPS tunnel via Cloudflare...${NC}"
CF_LOG="/tmp/citizenradar_cloudflared.log"
rm -f "$CF_LOG"

cloudflared tunnel --url http://localhost:5000 > "$CF_LOG" 2>&1 &
CF_PID=$!

# Wait for tunnel URL in log
PUBLIC_URL=""
for i in {1..30}; do
    if grep -q "trycloudflare.com" "$CF_LOG" 2>/dev/null; then
        PUBLIC_URL=$(grep -o 'https://[-a-zA-Z0-9.]*\.trycloudflare\.com' "$CF_LOG" | head -n 1)
        if [ -n "$PUBLIC_URL" ]; then
            break
        fi
    fi
    sleep 0.5
done

if [ -z "$PUBLIC_URL" ]; then
    echo -e "${RED}❌ Failed to retrieve Cloudflare public URL. Log:${NC}"
    cat "$CF_LOG"
    exit 1
fi

# 5. Display Dashboard Info
clear || true
echo -e "${CYAN}╔══════════════════════════════════════════════════════════════════════════════╗${NC}"
echo -e "${CYAN}║                    ⚡ CITIZEN RADAR — LIVE SHARING ACTIVE                     ║${NC}"
echo -e "${CYAN}║            Real-Time Edge AI Traffic Watchdog & Speed Radar                  ║${NC}"
echo -e "${CYAN}╚══════════════════════════════════════════════════════════════════════════════╝${NC}"
echo ""
echo -e "${BOLD}${GREEN}🎉 YOUR PROJECT IS LIVE AND READY TO SHARE!${NC}"
echo ""
echo -e "  ${BOLD}🌐 PUBLIC URL (Share with ANYONE across the world):${NC}"
echo -e "     👉  ${BOLD}${CYAN}${PUBLIC_URL}${NC}"
echo -e "     ${GREEN}✓ No signups, no port-forwarding, secure SSL/HTTPS${NC}"
echo ""
echo -e "  ${BOLD}📱 LOCAL WI-FI URL (For friends on your home/office Wi-Fi):${NC}"
echo -e "     👉  ${BOLD}${YELLOW}http://${LOCAL_IP}:5000${NC}"
echo ""
echo -e "  ${BOLD}🚀 HARDWARE ACCELERATION:${NC}"
echo -e "     NVIDIA GeForce RTX 4050 Laptop GPU (CUDA 13.4, cuDNN 9, ~95 FPS)"
echo ""

# 6. Generate and print QR Code for phone scanning
echo -e "  ${BOLD}📲 SCAN QR CODE WITH PHONE TO OPEN DASHBOARD:${NC}"
echo ""
curl -s -m 3 "https://qrenco.de/${PUBLIC_URL}" || true
echo ""
echo -e "${CYAN}────────────────────────────────────────────────────────────────────────────────${NC}"
echo -e "${BOLD}📡 Live Streaming is running.${NC} Friends can open the link above on PC or phone."
echo -e "⌨️  Press ${BOLD}${RED}[Ctrl + C]${NC} to stop the live session anytime."
echo -e "${CYAN}────────────────────────────────────────────────────────────────────────────────${NC}"
echo ""

# Keep running and wait for user interrupt
wait "$CF_PID"
