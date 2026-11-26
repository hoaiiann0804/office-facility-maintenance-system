# Multi-stage Dockerfile cho Frontend SPA (build context: ./clients)
FROM node:22.14-alpine AS build
WORKDIR /app

# Pin đúng phiên bản pnpm theo packageManager trong package.json
RUN corepack enable && corepack prepare pnpm@10.30.2 --activate

COPY package.json pnpm-lock.yaml ./
RUN pnpm install --frozen-lockfile

COPY . .

ARG VITE_API_BASE_URL=/api
ARG VITE_GOOGLE_CLIENT_ID=""
ARG VITE_ENABLE_DEMO_MODE=true

ENV VITE_API_BASE_URL=$VITE_API_BASE_URL \
    VITE_GOOGLE_CLIENT_ID=$VITE_GOOGLE_CLIENT_ID \
    VITE_ENABLE_DEMO_MODE=$VITE_ENABLE_DEMO_MODE

RUN pnpm build

FROM nginx:1.27-alpine AS final
COPY nginx.conf /etc/nginx/conf.d/default.conf
COPY --from=build /app/dist /usr/share/nginx/html
EXPOSE 80
CMD ["nginx", "-g", "daemon off;"]

