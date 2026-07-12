FROM node:24-alpine AS build
WORKDIR /app
COPY src/EcoAssetHub.Client/package*.json ./
RUN npm ci
COPY src/EcoAssetHub.Client/ ./
RUN npm run build

FROM nginx:1.27-alpine AS runtime
COPY src/EcoAssetHub.Client/nginx.conf /etc/nginx/conf.d/default.conf
COPY --from=build /app/dist /usr/share/nginx/html
EXPOSE 80
