terraform {
  required_version = ">= 1.5"

  required_providers {
    azurerm = {
      source  = "hashicorp/azurerm"
      version = "~> 4.0"
    }
  }
}

provider "azurerm" {
  features {}

  # The provider's default behavior registers ~50 Azure resource providers on init, most
  # irrelevant to this module and prone to failing outright under flaky network conditions
  # (observed directly: unrelated registrations like Microsoft.HealthcareApis blocked apply).
  # The providers this module actually needs (Microsoft.App, Microsoft.DBforPostgreSQL,
  # Microsoft.OperationalInsights) are registered once via
  # `az provider register --namespace <ns> --wait`, not by Terraform.
  resource_provider_registrations = "none"
}
