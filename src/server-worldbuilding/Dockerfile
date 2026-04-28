# ---------- Stage 1: Build ----------
FROM node:22-alpine AS builder

# Enable corepack so pnpm is available without a separate install step
RUN corepack enable

WORKDIR /app

# Copy only manifest files first for better layer caching
COPY package.json ./
COPY pnpm-lock.yaml ./
RUN pnpm install --frozen-lockfile

COPY . .

RUN pnpm run build
RUN pnpm exec tsc-alias -p tsconfig.json

# ---------- Stage 2: Production ----------
FROM node:22-alpine AS production

WORKDIR /app

COPY --from=builder --chown=node:node --chmod=555 /app/dist ./dist
COPY --from=builder --chown=node:node --chmod=555 /app/node_modules ./node_modules
COPY --chown=node:node --chmod=444 package.json ./

# Drop privileges: run as the unprivileged "node" user shipped with the image.
USER node

EXPOSE 3000

HEALTHCHECK --interval=30s --timeout=5s --start-period=10s --retries=3 \
  CMD wget --no-verbose --tries=1 --spider http://localhost:3000/health || exit 1

CMD ["node", "dist/index.js"]
