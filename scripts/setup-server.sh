#!/bin/bash
# =============================================================================
# Trade Bot — initial server setup (Ubuntu/Debian) — interactive edition
# Usage: curl -fsSL https://raw.githubusercontent.com/dnl1/trade-bot/main/scripts/setup-server.sh | sudo bash
#    or: chmod +x setup-server.sh && sudo ./setup-server.sh
# =============================================================================
set -euo pipefail

REPO_URL="https://github.com/dnl1/trade-bot.git"
INSTALL_DIR="/opt/trade-bot"
SERVICE_USER="${SUDO_USER:-$USER}"

# ── Colors ────────────────────────────────────────────────────────────────────
RED='\033[0;31m'; GREEN='\033[0;32m'; YELLOW='\033[1;33m'
CYAN='\033[0;36m'; BOLD='\033[1m'; DIM='\033[2m'; NC='\033[0m'

info()    { echo -e "${CYAN}==>${NC} $*"; }
success() { echo -e "${GREEN}✔${NC}  $*"; }
warn()    { echo -e "${YELLOW}⚠${NC}  $*"; }
error()   { echo -e "${RED}✘${NC}  $*"; exit 1; }
header()  { echo -e "\n${BOLD}$*${NC}"; echo "$(printf '─%.0s' {1..60})"; }
label()   { echo -e "  ${BOLD}$1${NC}${DIM}$2${NC}"; }

# ── Interactive input helpers — always read from /dev/tty (works with curl|bash)
ask() {
    # ask VAR "Prompt" "default" — prints prompt, reads into VAR
    local __var="$1" prompt="$2" default="${3:-}"
    local hint=""
    [ -n "$default" ] && hint=" ${DIM}[${default}]${NC}"
    printf "  %b%s%b " "${CYAN}" "${prompt}${hint}" "${NC}" >/dev/tty
    local val
    IFS= read -r val </dev/tty
    [ -z "$val" ] && val="$default"
    printf -v "$__var" '%s' "$val"
}

ask_secret() {
    # ask_secret VAR "Prompt" — reads without echo
    local __var="$1" prompt="$2"
    printf "  %b%s%b " "${CYAN}" "${prompt}" "${NC}" >/dev/tty
    local val
    IFS= read -rs val </dev/tty
    echo >/dev/tty
    printf -v "$__var" '%s' "$val"
}

ask_yn() {
    # ask_yn "Question?" default(y/n) — returns 0=yes 1=no
    local prompt="$1" default="${2:-y}"
    local hint="[Y/n]"; [ "$default" = "n" ] && hint="[y/N]"
    printf "  %b%s %s%b " "${YELLOW}" "$prompt" "$hint" "${NC}" >/dev/tty
    local val
    IFS= read -r val </dev/tty
    [ -z "$val" ] && val="$default"
    [[ "$val" =~ ^[Yy] ]]
}

mask() {
    # Print first 8 chars then ***
    local s="$1"
    [ ${#s} -le 8 ] && echo "${s}" && return
    echo "${s:0:8}***"
}

# Load existing .env values if present
load_env() {
    local envfile="$1"
    [ -f "$envfile" ] || return
    while IFS='=' read -r key value; do
        [[ "$key" =~ ^#.*$ || -z "$key" ]] && continue
        value="${value%%#*}"            # strip inline comments
        value="${value%"${value##*[![:space:]]}"}"  # trim trailing space
        export "${key}=${value}"
    done < "$envfile"
}

# ── Pre-checks ────────────────────────────────────────────────────────────────
[ "$(id -u)" -eq 0 ] || error "Run with sudo: sudo bash setup-server.sh"
command -v apt-get &>/dev/null || error "This script requires a Debian/Ubuntu system."

header "Trade Bot — Server Setup"
info "System:   $(lsb_release -ds 2>/dev/null || grep PRETTY /etc/os-release | cut -d= -f2 | tr -d '"')"
info "User:     $SERVICE_USER"
info "Install:  $INSTALL_DIR"
echo ""

# ── 1. Update packages ────────────────────────────────────────────────────────
header "1. Updating system packages"

# Remove stale third-party apt sources that lack a Release file (e.g. Cloudflare)
# and would cause apt-get update to fail with "does not have a Release file".
find /etc/apt/sources.list.d/ -name "cloudflare*.list" -delete 2>/dev/null || true

apt-get update -qq
apt-get install -y -qq \
  ca-certificates curl gnupg lsb-release git make \
  2>&1 | grep -v "^(Reading|Hit|Get|Fetched|Building)" || true
success "Base packages installed"

# ── 2. Docker ─────────────────────────────────────────────────────────────────
header "2. Installing Docker Engine"
if command -v docker &>/dev/null; then
  DOCKER_VER=$(docker --version | awk '{print $3}' | tr -d ',')
  success "Docker already installed: $DOCKER_VER"
else
  info "Adding official Docker repository..."
  install -m 0755 -d /etc/apt/keyrings
  curl -fsSL https://download.docker.com/linux/ubuntu/gpg \
    | gpg --dearmor -o /etc/apt/keyrings/docker.gpg
  chmod a+r /etc/apt/keyrings/docker.gpg

  echo "deb [arch=$(dpkg --print-architecture) \
    signed-by=/etc/apt/keyrings/docker.gpg] \
    https://download.docker.com/linux/ubuntu \
    $(lsb_release -cs) stable" \
    > /etc/apt/sources.list.d/docker.list

  apt-get update -qq
  apt-get install -y -qq docker-ce docker-ce-cli containerd.io docker-compose-plugin
  success "Docker installed: $(docker --version)"
fi

# ── 3. Add user to docker group ───────────────────────────────────────────────
header "3. User permissions"
if groups "$SERVICE_USER" | grep -q docker; then
  success "$SERVICE_USER is already in the docker group"
else
  usermod -aG docker "$SERVICE_USER"
  success "$SERVICE_USER added to docker group (effective after re-login)"
fi

systemctl enable --now docker &>/dev/null
success "Docker enabled on boot"

# ── 4. Clone repository ───────────────────────────────────────────────────────
header "4. Installing Trade Bot at $INSTALL_DIR"
if [ -d "$INSTALL_DIR/.git" ]; then
  info "Repository already exists — updating..."
  git -C "$INSTALL_DIR" pull --ff-only
  success "Code updated"
else
  git clone "$REPO_URL" "$INSTALL_DIR"
  success "Repository cloned"
fi

chown -R "$SERVICE_USER:$SERVICE_USER" "$INSTALL_DIR"
mkdir -p "$INSTALL_DIR/logs"
chmod 755 "$INSTALL_DIR/logs"
chown "$SERVICE_USER:$SERVICE_USER" "$INSTALL_DIR/logs"

# ── 5. Interactive credentials ────────────────────────────────────────────────
header "5. Credentials"

ENV_FILE="$INSTALL_DIR/.env"
RECONFIGURE=false

# Load existing values so we can show them as defaults
BINANCE_API_KEY="${BINANCE_API_KEY:-}"
BINANCE_SECRET_KEY="${BINANCE_SECRET_KEY:-}"
BINANCE_RSA_KEY_PATH="${BINANCE_RSA_KEY_PATH:-}"
POSTGRES_PASSWORD="${POSTGRES_PASSWORD:-}"
TELEGRAM_BOT_ID="${TELEGRAM_BOT_ID:-}"
TELEGRAM_CHAT_ID="${TELEGRAM_CHAT_ID:-}"

if [ -f "$ENV_FILE" ]; then
  load_env "$ENV_FILE"
  echo ""
  echo -e "  ${GREEN}Existing .env found${NC} — current values:"
  [ -n "$BINANCE_API_KEY"    ] && echo -e "    BINANCE_API_KEY      = $(mask "$BINANCE_API_KEY")"
  [ -n "$BINANCE_SECRET_KEY" ] && echo -e "    BINANCE_SECRET_KEY   = $(mask "$BINANCE_SECRET_KEY")"
  [ -n "$BINANCE_RSA_KEY_PATH" ] && echo -e "    BINANCE_RSA_KEY_PATH = $BINANCE_RSA_KEY_PATH"
  [ -n "$POSTGRES_PASSWORD"  ] && echo -e "    POSTGRES_PASSWORD    = $(mask "$POSTGRES_PASSWORD")"
  [ -n "$TELEGRAM_BOT_ID"    ] && echo -e "    TELEGRAM_BOT_ID      = $(mask "$TELEGRAM_BOT_ID")"
  [ -n "$TELEGRAM_CHAT_ID"   ] && echo -e "    TELEGRAM_CHAT_ID     = $TELEGRAM_CHAT_ID"
  echo ""
  if ask_yn "Reconfigure credentials?" "n"; then
    RECONFIGURE=true
  else
    success "Keeping existing credentials"
  fi
fi

if [ ! -f "$ENV_FILE" ] || [ "$RECONFIGURE" = true ]; then
  echo ""

  # ── Binance ──────────────────────────────────────────────────────────────────
  label "Binance API Key" "  (required — https://www.binance.com/en/my/settings/api-management)"
  ask BINANCE_API_KEY "API Key:" "$BINANCE_API_KEY"

  echo ""
  label "Authentication method" ""
  echo -e "    ${DIM}[1]${NC} HMAC secret key  (simpler, both in the same Binance API page)"
  echo -e "    ${DIM}[2]${NC} RSA private key  (more secure, recommended by Binance)"
  echo ""
  ask AUTH_METHOD "Choose [1/2]:" "1"

  if [ "$AUTH_METHOD" = "2" ]; then
    label "RSA Private Key" "  (absolute path on this server — tilde ~ is NOT expanded)"
    ask BINANCE_RSA_KEY_PATH "Key path:" "${BINANCE_RSA_KEY_PATH:-/opt/trade-bot/binance_key.pem}"
    BINANCE_SECRET_KEY=""
    echo ""
    if [ ! -f "$BINANCE_RSA_KEY_PATH" ]; then
      warn "File not found: $BINANCE_RSA_KEY_PATH"
      warn "Copy your RSA key there before starting the bot."
    else
      success "RSA key found at $BINANCE_RSA_KEY_PATH"
    fi
  else
    label "HMAC Secret Key" "  (from the same Binance API page)"
    ask_secret BINANCE_SECRET_KEY "Secret Key (hidden):"
    BINANCE_RSA_KEY_PATH=""
  fi

  # ── PostgreSQL ────────────────────────────────────────────────────────────────
  echo ""
  label "PostgreSQL" "  (database password for the local container)"
  ask_secret POSTGRES_PASSWORD "DB Password (hidden, default=tradebot):"
  [ -z "$POSTGRES_PASSWORD" ] && POSTGRES_PASSWORD="tradebot"

  # ── Telegram ─────────────────────────────────────────────────────────────────
  echo ""
  label "Telegram" "  (optional — receive trade alerts on your phone)"
  echo -e "    ${DIM}Press Enter to skip${NC}"
  echo ""
  ask TELEGRAM_BOT_ID "Bot Token   :" "$TELEGRAM_BOT_ID"
  ask TELEGRAM_CHAT_ID "Chat ID    :" "$TELEGRAM_CHAT_ID"

  # ── Write .env ────────────────────────────────────────────────────────────────
  echo ""
  cat > "$ENV_FILE" <<ENVEOF
# ── Binance ───────────────────────────────────────────────────────────────────
BINANCE_API_KEY=${BINANCE_API_KEY}
BINANCE_SECRET_KEY=${BINANCE_SECRET_KEY}
# Use an absolute path — docker-compose does NOT expand ~ (tilde)
BINANCE_RSA_KEY_PATH=${BINANCE_RSA_KEY_PATH}

# ── PostgreSQL ─────────────────────────────────────────────────────────────────
POSTGRES_PASSWORD=${POSTGRES_PASSWORD}

# ── Telegram (optional) ───────────────────────────────────────────────────────
TELEGRAM_BOT_ID=${TELEGRAM_BOT_ID}
TELEGRAM_CHAT_ID=${TELEGRAM_CHAT_ID}
ENVEOF

  chown "$SERVICE_USER:$SERVICE_USER" "$ENV_FILE"
  chmod 600 "$ENV_FILE"
  success ".env written to $ENV_FILE"

  # Warn about missing required fields
  if [ -z "$BINANCE_API_KEY" ]; then
    warn "BINANCE_API_KEY is empty — the bot will not start without it."
  fi
  if [ -z "$BINANCE_SECRET_KEY" ] && [ -z "$BINANCE_RSA_KEY_PATH" ]; then
    warn "No authentication method set. Add BINANCE_SECRET_KEY or BINANCE_RSA_KEY_PATH."
  fi
fi

# ── 6. Install systemd service ────────────────────────────────────────────────
header "6. Registering systemd service"
SERVICE_FILE="/etc/systemd/system/trade-bot.service"

cat > "$SERVICE_FILE" <<EOF
[Unit]
Description=Trade Bot (Binance BollingerBands)
Requires=docker.service
After=docker.service network-online.target
Wants=network-online.target

[Service]
Type=oneshot
RemainAfterExit=yes
WorkingDirectory=$INSTALL_DIR
User=$SERVICE_USER
Group=docker

ExecStart=/usr/bin/docker compose up -d --build
ExecStop=/usr/bin/docker compose down
ExecReload=/usr/bin/docker compose restart trade-bot

TimeoutStartSec=300
TimeoutStopSec=60
StandardOutput=journal
StandardError=journal
SyslogIdentifier=trade-bot

[Install]
WantedBy=multi-user.target
EOF

systemctl daemon-reload
systemctl enable trade-bot
success "trade-bot service registered and enabled on boot"

# ── 7. Log rotation ───────────────────────────────────────────────────────────
header "7. Configuring log rotation"
cat > /etc/logrotate.d/trade-bot <<EOF
$INSTALL_DIR/logs/tradebot-*.log {
    daily
    rotate 30
    compress
    delaycompress
    missingok
    notifempty
    copytruncate
}
EOF
success "Logrotate configured (30-day retention)"

# ── Summary ───────────────────────────────────────────────────────────────────
header "✔  Setup complete!"
echo ""
echo -e "  ${BOLD}Start the bot:${NC}"
echo ""
echo -e "     ${CYAN}sudo systemctl start trade-bot${NC}"
echo ""
echo -e "  ${BOLD}Follow logs:${NC}"
echo ""
echo -e "     ${CYAN}journalctl -u trade-bot -f${NC}"
echo -e "     ${CYAN}tail -f $INSTALL_DIR/logs/tradebot-\$(date +%Y-%m-%d).log${NC}"
echo ""
if [ -z "${BINANCE_API_KEY:-}" ]; then
  echo -e "  ${RED}⚠  Configure credentials before starting:${NC}"
  echo -e "     ${CYAN}nano $ENV_FILE${NC}"
  echo ""
fi
echo -e "  ${YELLOW}Note:${NC} Re-login (or run 'newgrp docker') to use docker without sudo."
echo ""
