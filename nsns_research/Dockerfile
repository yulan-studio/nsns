FROM node:22-bookworm-slim

RUN apt-get update \
    && apt-get install -y --no-install-recommends python3 python3-pip \
    && rm -rf /var/lib/apt/lists/*

WORKDIR /app

COPY requirements.txt ./
RUN pip3 install --no-cache-dir --break-system-packages -r requirements.txt

COPY package.json ./
COPY server.js ./
COPY public ./public
COPY scripts ./scripts

ENV NODE_ENV=production
ENV PYTHON_BIN=python3
ENV DATA_DIR=/tmp/research-assistant

EXPOSE 8080
CMD ["npm", "start"]
