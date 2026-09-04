# syntax=docker/dockerfile:1
# Build the SPA and serve it with nginx. Run from frontend/library-web:
#   docker build -t library-web .

FROM node:22-alpine AS build
WORKDIR /app
COPY package.json package-lock.json ./
RUN npm ci
COPY . .
# The API base URL is same-origin in the container (nginx proxies /api).
ENV VITE_API_BASE_URL=/api
RUN npm run build

FROM nginx:1.27-alpine AS runtime
COPY nginx.conf /etc/nginx/conf.d/default.conf
COPY --from=build /app/dist /usr/share/nginx/html
EXPOSE 80
