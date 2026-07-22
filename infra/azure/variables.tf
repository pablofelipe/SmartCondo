variable "location" {
  description = "Azure region. westus3 was the cheapest unrestricted region for PostgreSQL Flexible Server on the validating subscription - verify with `az postgres flexible-server list-skus --location <region>` before changing, since new subscriptions can have per-region provisioning restrictions."
  type        = string
  default     = "westus3"
}

variable "resource_group_name" {
  type    = string
  default = "smartcondo-rg"
}

variable "docker_image" {
  description = "Public image reference, e.g. a Docker Hub repo. Built and pushed outside Terraform (see docker/backend.Dockerfile) - this module provisions infrastructure, not the image."
  type        = string
}

variable "db_admin_username" {
  type    = string
  default = "smartcondoadmin"
}

variable "db_admin_password" {
  type      = string
  sensitive = true
}

variable "jwt_key" {
  description = "Base64 string decoding to >= 32 bytes."
  type        = string
  sensitive   = true
}

variable "migration_auth_key" {
  type      = string
  sensitive = true
}

variable "admin_email" {
  type    = string
  default = "admin@example.com"
}

variable "admin_password" {
  type      = string
  sensitive = true
}
