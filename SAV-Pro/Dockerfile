# ---------- Build stage ----------
FROM node:20-alpine AS build

WORKDIR /app

# Install dependencies first for better Docker cache
COPY package*.json ./

RUN npm ci

# Copy source code
COPY . .

# Build-time API URL.
# For Kubernetes/Nginx reverse proxy, keep it as /api
ARG VITE_API_BASE_URL=/api
ENV VITE_API_BASE_URL=$VITE_API_BASE_URL

RUN npm run build


# ---------- Runtime stage ----------
FROM nginx:1.27-alpine AS runtime

# Remove default nginx static files
RUN rm -rf /usr/share/nginx/html/*

# Copy frontend build output
COPY --from=build /app/dist /usr/share/nginx/html

# Copy custom nginx config
COPY nginx.conf /etc/nginx/conf.d/default.conf

EXPOSE 80

CMD ["nginx", "-g", "daemon off;"]