FROM node:24-alpine AS build
WORKDIR /app
RUN npm install -g npm@12.0.1
COPY src/TimeLens.Client/package*.json ./
RUN npm ci
COPY src/TimeLens.Client/ ./
RUN npm run build

FROM nginx:1.27-alpine AS runtime
COPY src/TimeLens.Client/nginx.conf /etc/nginx/conf.d/default.conf
COPY --from=build /app/dist /usr/share/nginx/html
EXPOSE 80
