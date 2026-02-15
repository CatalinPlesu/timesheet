# Build stage
FROM node:20-alpine AS build
WORKDIR /app

# Accept API URL as build argument
ARG VITE_API_URL=http://localhost:5000

# Copy package files
COPY package.json package-lock.json ./

# Install dependencies
RUN npm ci

# Copy source files
COPY . .

# Build the application with environment variable
ENV VITE_API_URL=$VITE_API_URL
RUN npm run build

# Runtime stage - serve with nginx
FROM nginx:alpine AS runtime

# Copy built files to nginx
COPY --from=build /app/build /usr/share/nginx/html

# Copy nginx configuration
COPY nginx.conf /etc/nginx/conf.d/default.conf

# Expose port
EXPOSE 80

# Health check
HEALTHCHECK --interval=30s --timeout=10s --start-period=5s --retries=3 \
  CMD wget --no-verbose --tries=1 --spider http://localhost/ || exit 1

CMD ["nginx", "-g", "daemon off;"]
