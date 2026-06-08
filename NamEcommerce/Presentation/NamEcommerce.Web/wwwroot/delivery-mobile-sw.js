const cacheName = 'nam-delivery-mobile-v2';
const staticAssets = [
    '/delivery-mobile.webmanifest',
    '/lib/bootstrap/dist/css/bootstrap.min.css',
    '/lib/bootstrap-icons-1.13.1/bootstrap-icons.min.css',
    '/modules/DeliveryMobileCache.js',
    '/favicon.ico'
];

self.addEventListener('install', event => {
    event.waitUntil(caches.open(cacheName).then(cache => cache.addAll(staticAssets)));
    self.skipWaiting();
});

self.addEventListener('activate', event => {
    event.waitUntil(
        caches.keys().then(keys => Promise.all(keys
            .filter(key => key !== cacheName)
            .map(key => caches.delete(key))))
    );
    self.clients.claim();
});

self.addEventListener('fetch', event => {
    const request = event.request;
    if (request.method !== 'GET') {
        return;
    }

    const url = new URL(request.url);
    if (url.pathname.startsWith('/DeliveryMobile')) {
        event.respondWith(networkFirst(request));
        return;
    }

    if (url.pathname.startsWith('/Picture/')) {
        event.respondWith(cacheFirst(request));
        return;
    }

    if (staticAssets.includes(url.pathname)) {
        event.respondWith(cacheFirst(request));
    }
});

async function networkFirst(request) {
    const cache = await caches.open(cacheName);
    try {
        const response = await fetch(request);
        cache.put(request, response.clone());
        return response;
    } catch {
        return await cache.match(request) || await cache.match('/DeliveryMobile');
    }
}

async function cacheFirst(request) {
    const cached = await caches.match(request);
    if (cached) {
        return cached;
    }

    const response = await fetch(request);
    const cache = await caches.open(cacheName);
    cache.put(request, response.clone());
    return response;
}
