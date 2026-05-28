#!/bin/bash
# redeploy.sh — Cập nhật và redeploy NamEcommerce
# Dùng: bash redeploy.sh [web|customer-api|customer-client|all]
# Ví dụ:
#   bash redeploy.sh           # redeploy tất cả
#   bash redeploy.sh web       # chỉ redeploy admin web
#   bash redeploy.sh customer-api
#   bash redeploy.sh customer-client

set -e

SERVICE=${1:-all}

echo "📥 Pull code mới từ GitHub..."
git stash
git pull --rebase origin main
git stash pop || true

redeploy_service() {
    local svc=$1
    echo "🔨 Build $svc..."
    docker compose build --no-cache $svc
    echo "▶️  Restart $svc..."
    docker compose up -d --force-recreate $svc
    echo "✅ $svc đã cập nhật!"
}

case $SERVICE in
    web)
        redeploy_service web
        ;;
    customer-api)
        redeploy_service customer-api
        ;;
    customer-client)
        redeploy_service customer-client
        ;;
    all)
        redeploy_service web
        redeploy_service customer-api
        redeploy_service customer-client
        ;;
    *)
        echo "❌ Service không hợp lệ: $SERVICE"
        echo "   Dùng: web | customer-api | customer-client | all"
        exit 1
        ;;
esac

echo ""
echo "📋 Trạng thái hiện tại:"
docker compose ps
