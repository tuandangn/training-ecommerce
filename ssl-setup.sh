#!/bin/bash
# ssl-setup.sh — Xin SSL cert cho 3 subdomain
# Chạy TRƯỚC khi deploy, sau khi đã trỏ DNS về IP của VM
set -e

DOMAIN="tuankhoivlxd.vn"

echo "📦 Cài Certbot..."
sudo apt-get update -q
sudo apt-get install -y certbot

echo "🔐 Xin SSL cert cho 3 subdomain..."

# Dừng nginx nếu đang chạy (để certbot dùng port 80)
docker compose down nginx 2>/dev/null || true

sudo certbot certonly --standalone \
    -d admin.$DOMAIN \
    --non-interactive --agree-tos \
    --email admin@$DOMAIN

sudo certbot certonly --standalone \
    -d api.$DOMAIN \
    --non-interactive --agree-tos \
    --email admin@$DOMAIN

sudo certbot certonly --standalone \
    -d shop.$DOMAIN \
    --non-interactive --agree-tos \
    --email admin@$DOMAIN

echo "📁 Copy cert vào thư mục ssl/..."
mkdir -p ssl/admin ssl/api ssl/shop

sudo cp /etc/letsencrypt/live/admin.$DOMAIN/fullchain.pem ssl/admin/
sudo cp /etc/letsencrypt/live/admin.$DOMAIN/privkey.pem   ssl/admin/
sudo cp /etc/letsencrypt/live/api.$DOMAIN/fullchain.pem   ssl/api/
sudo cp /etc/letsencrypt/live/api.$DOMAIN/privkey.pem     ssl/api/
sudo cp /etc/letsencrypt/live/shop.$DOMAIN/fullchain.pem  ssl/shop/
sudo cp /etc/letsencrypt/live/shop.$DOMAIN/privkey.pem    ssl/shop/

sudo chmod -R 644 ssl/

echo ""
echo "✅ SSL cert đã sẵn sàng!"
echo "   Giờ chạy: bash deploy.sh"
