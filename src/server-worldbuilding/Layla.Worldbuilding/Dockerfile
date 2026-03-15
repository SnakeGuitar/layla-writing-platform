# ---------- Stage 1: Build ----------
FROM node:22-alpine AS builder

# Enable corepack so pnpm is available without a separate install step
RUN corepack enable

WORKDIR /app

# Copy only manifest files first for better layer caching
COPY package.json pnpm-lock.yaml ./
RUN pnpm install --no-frozen-lockfile
RUN pnpm add -D tsc-alias

COPY . .
RUN pnpm run build
RUN pnpm exec tsc-alias -p tsconfig.json

# ---------- Stage 2: Production ----------
FROM node:22-alpine AS production

WORKDIR /app

COPY --from=builder /app/dist ./dist
COPY --from=builder /app/node_modules ./node_modules
COPY package.json ./

EXPOSE 3000
CMD ["node", "dist/index.js"]
