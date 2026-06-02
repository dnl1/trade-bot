.DEFAULT_GOAL := help
COMPOSE = docker compose

##@ Setup

.PHONY: setup
setup: ## Copy .env.example → .env and create logs folder with correct permissions
	@[ -f .env ] || (cp .env.example .env && echo "✔  .env created — fill in your credentials")
	@mkdir -p logs
	@chmod 755 logs
	@echo "UID=$$(id -u)" >> .env.uid 2>/dev/null || true; rm -f .env.uid
	@echo "✔  Ready. Edit .env before deploying."

.PHONY: uid
uid: ## Export current UID/GID so container writes logs as your user
	@echo "UID=$$(id -u)" > .env.uid
	@echo "GID=$$(id -g)" >> .env.uid
	@echo "✔  .env.uid created (UID=$$(id -u) GID=$$(id -g))"

##@ Build

.PHONY: build
build: ## Build Docker image (no cache)
	$(COMPOSE) build --no-cache trade-bot

.PHONY: build-cache
build-cache: ## Build Docker image (using cache)
	$(COMPOSE) build trade-bot

##@ Deploy / Run

.PHONY: up
up: ## Start all services (postgres + migrations + bot)
	$(COMPOSE) up -d

.PHONY: down
down: ## Stop and remove containers
	$(COMPOSE) down

.PHONY: restart
restart: ## Restart the bot only
	$(COMPOSE) restart trade-bot

.PHONY: deploy
deploy: build up ## Full deploy: build + up

##@ Logs

.PHONY: logs
logs: ## Follow bot logs in real time
	$(COMPOSE) logs -f trade-bot

.PHONY: logs-all
logs-all: ## Follow logs for all services
	$(COMPOSE) logs -f

.PHONY: logs-file
logs-file: ## Show last 100 lines of today's log file
	@tail -100 logs/tradebot-$$(date +%Y-%m-%d).log 2>/dev/null || echo "No log file found for today"

##@ Monitoring

.PHONY: status
status: ## Show status of all containers
	$(COMPOSE) ps

.PHONY: health
health: ## Check bot health status
	@docker inspect trade-bot --format='Status: {{.State.Health.Status}}' 2>/dev/null || echo "Container not found"

##@ Maintenance

.PHONY: clean
clean: ## Remove containers, images and volumes (WARNING: deletes Postgres data)
	$(COMPOSE) down -v --rmi local

.PHONY: update
update: ## Pull latest code + rebuild + redeploy
	git pull
	$(MAKE) deploy

##@ Tests

.PHONY: test
test: ## Run unit tests
	dotnet test

##@ Help

.PHONY: help
help: ## Show this message
	@awk 'BEGIN {FS = ":.*##"; printf "\nUsage: make \033[36m<target>\033[0m\n"} /^[a-zA-Z_-]+:.*?##/ { printf "  \033[36m%-15s\033[0m %s\n", $$1, $$2 } /^##@/ { printf "\n\033[1m%s\033[0m\n", substr($$0, 5) } ' $(MAKEFILE_LIST)
