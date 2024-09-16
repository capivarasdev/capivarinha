## build runner
FROM node:18-buster-slim AS build-runner

# Set temp directory
WORKDIR /tmp/app

# Copy necessary files
COPY package.json .
COPY package-lock.json .
COPY tsconfig.json .
COPY src ./src

# Install dependencies
RUN npm ci 
# Build project
RUN npm run build

## production runner
FROM node:18-buster-slim AS prod-runner

# Set the working directory inside the container
WORKDIR /app
# Copy package.json and package-lock.json from build-runner
COPY --from=build-runner /tmp/app/package.json /app/package.json
COPY --from=build-runner /tmp/app/package-lock.json /app/package-lock.json
# Move build files
COPY --from=build-runner /tmp/app/dist /app/dist

# Install dependencies from package-lock.json
# and install dependencies for plugins (because plugins:install script doesn't work in docker)
RUN npm ci --omit=dev && npm cache clean --force

# Set the command to run the bot
CMD ["node", "dist/bot.js"]