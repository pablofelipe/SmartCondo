variable "region" {
  description = "us-east-1/us-east-2/us-west-2 were tied as the cheapest for RDS db.t4g.micro PostgreSQL when checked against the real AWS Price List API - verify before changing regions, prices vary meaningfully by region (sa-east-1 was ~2x more expensive)."
  type        = string
  default     = "us-east-1"
}

variable "image_uri" {
  description = "ECR Public (or private ECR) image URI. App Runner only pulls from ECR, unlike Azure Container Apps which accepts any public registry - that's why this differs from the Azure module's Docker Hub reference. Built and pushed outside Terraform."
  type        = string
}

variable "db_password" {
  type      = string
  sensitive = true
}

variable "jwt_key" {
  type      = string
  sensitive = true
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

variable "desired_count" {
  description = "Number of running Fargate tasks. Default 0 (defined but not running - no compute cost); set to 1 to actually demo it, then back to 0 or `terraform destroy` when done, per this project's cloud-cost standing rule."
  type        = number
  default     = 0
}
