# Étape 1 — Build
FROM node:20-alpine AS build
WORKDIR /app

# Copier package.json et installer les dépendances
COPY package.json package-lock.json ./
RUN npm ci

# Copier le code et builder
COPY . .
RUN npm run build

# Étape 2 — Serveur nginx pour servir le frontend
FROM nginx:alpine AS runtime

# Copier le build React vers nginx
COPY --from=build /app/dist /usr/share/nginx/html

# Configuration nginx pour React Router (SPA)
RUN echo 'server { \
    listen 80; \
    root /usr/share/nginx/html; \
    index index.html; \
    location / { \
        try_files $uri $uri/ /index.html; \
    } \
    location /api { \
        proxy_pass http://backend:5000; \
    } \
    location /hubs { \
        proxy_pass http://backend:5000; \
        proxy_http_version 1.1; \
        proxy_set_header Upgrade $http_upgrade; \
        proxy_set_header Connection "upgrade"; \
    } \
}' > /etc/nginx/conf.d/default.conf

EXPOSE 80
