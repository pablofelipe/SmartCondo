resource "azurerm_resource_group" "this" {
  name     = var.resource_group_name
  location = var.location
}

# Burstable B1MS is the cheapest compute tier PostgreSQL Flexible Server offers - proportionate
# for a portfolio demo, not a production workload.
resource "azurerm_postgresql_flexible_server" "this" {
  name                   = "smartcondo-db"
  resource_group_name    = azurerm_resource_group.this.name
  location               = azurerm_resource_group.this.location
  version                = "16"
  administrator_login    = var.db_admin_username
  administrator_password = var.db_admin_password
  storage_mb             = 32768
  sku_name               = "B_Standard_B1ms"

  # Public access, no VNet integration - matches ADR-0011's "reproducible in under 30 minutes"
  # goal; a private-networked setup is more production-realistic but a much bigger unit of work
  # for a demo that isn't meant to stay online.
  public_network_access_enabled = true

  lifecycle {
    # Azure auto-assigns an availability zone on creation since none is specified here (there's
    # no HA requirement for a demo); the API only allows changing it via the high-availability
    # standby-zone exchange mechanism, so let Azure's choice stand instead of fighting it.
    ignore_changes = [zone]
  }
}

resource "azurerm_postgresql_flexible_server_firewall_rule" "allow_azure_services" {
  name             = "AllowAzureServices"
  server_id        = azurerm_postgresql_flexible_server.this.id
  start_ip_address = "0.0.0.0"
  end_ip_address   = "0.0.0.0"
}

resource "azurerm_postgresql_flexible_server_database" "smartcondo" {
  name      = "smartcondo"
  server_id = azurerm_postgresql_flexible_server.this.id
  charset   = "UTF8"
  collation = "en_US.utf8"
}

resource "azurerm_log_analytics_workspace" "this" {
  name                = "smartcondo-logs"
  resource_group_name = azurerm_resource_group.this.name
  location            = azurerm_resource_group.this.location
  sku                 = "PerGB2018"
  retention_in_days   = 30
}

resource "azurerm_container_app_environment" "this" {
  name                       = "smartcondo-env"
  resource_group_name        = azurerm_resource_group.this.name
  location                   = azurerm_resource_group.this.location
  log_analytics_workspace_id = azurerm_log_analytics_workspace.this.id
}

resource "azurerm_container_app" "backend" {
  name                         = "smartcondo-backend"
  resource_group_name          = azurerm_resource_group.this.name
  container_app_environment_id = azurerm_container_app_environment.this.id
  revision_mode                = "Single"

  secret {
    name  = "db-password"
    value = var.db_admin_password
  }
  secret {
    name  = "jwt-key"
    value = var.jwt_key
  }
  secret {
    name  = "migration-key"
    value = var.migration_auth_key
  }
  secret {
    name  = "admin-password"
    value = var.admin_password
  }

  template {
    # min_replicas = 0 is the whole point: Consumption-plan Container Apps scale to zero and
    # cost ~$0 while idle, matching ADR-0011's "does not need to stay online" success criterion.
    min_replicas = 0
    max_replicas = 2

    container {
      name   = "smartcondo-backend"
      image  = var.docker_image
      cpu    = 0.25
      memory = "0.5Gi"

      env {
        name  = "ASPNETCORE_ENVIRONMENT"
        value = "Production"
      }
      env {
        name  = "ASPNETCORE_URLS"
        value = "http://+:8080"
      }
      env {
        name  = "DB_HOST"
        value = azurerm_postgresql_flexible_server.this.fqdn
      }
      env {
        name  = "DB_NAME"
        value = azurerm_postgresql_flexible_server_database.smartcondo.name
      }
      env {
        name  = "DB_USER"
        value = var.db_admin_username
      }
      env {
        name        = "DB_PASSWORD"
        secret_name = "db-password"
      }
      env {
        name        = "JWT_KEY"
        secret_name = "jwt-key"
      }
      env {
        name        = "MIGRATION_AUTH_KEY"
        secret_name = "migration-key"
      }
      env {
        name  = "ADMIN_EMAIL"
        value = var.admin_email
      }
      env {
        name        = "ADMIN_PASSWORD"
        secret_name = "admin-password"
      }
    }
  }

  ingress {
    external_enabled = true
    target_port      = 8080
    traffic_weight {
      latest_revision = true
      percentage      = 100
    }
  }
}
