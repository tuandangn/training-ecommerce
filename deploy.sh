#!/bin/bash
# deploy.sh
set -e

echo "🚀 Bắt đầu deploy NamEcommerce..."

if [ ! -f .env ]; then
    echo "❌ Không tìm thấy file .env"
    exit 1
fi

# Pull code mới nhất
if [ -d .git ]; then
    echo "📥 Pull code mới..."
    git pull origin main
fi

# Build images
echo "🔨 Build Docker images..."
docker compose build --no-cache

# Khởi động
echo "▶️  Khởi động services..."
docker compose up -d

echo ""
echo "✅ Deploy thành công!"
echo ""
echo "📋 Lệnh hữu ích:"
echo "   docker compose logs -f web            # log admin"
echo "   docker compose logs -f customer-api   # log api"
echo "   docker compose logs -f customer-client # log shop"
echo "   docker compose ps                     # trạng thái"
echo "   docker compose down                   # tắt tất cả"
