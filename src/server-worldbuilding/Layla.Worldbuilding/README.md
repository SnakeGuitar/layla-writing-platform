Scripts pasados package.json:
 "scripts": {
    "test": "echo \"Error: no test specified\" && exit 1",
    "lint": "eslint . --ext .ts,.tsx,.js,.jsx",
    "lint:fix": "eslint . --ext .ts,.tsx,.js,.jsx --fix",
    "format": "prettier --write .",
    "format:check": "prettier --check .",
    "dev": "ts-node-dev --respawn --transpile-only -r tsconfig-paths/register src/index.ts",
    "build": "tsc -p tsconfig.json",
    "devv": "vite"
  },