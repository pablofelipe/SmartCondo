# Infrastructure

Two independent Terraform root modules, one per cloud, per [ADR-0011](../docs/adr/0011-container-first-cloud-agnostic-deployment.md) — deliberately not a single cross-cloud abstraction, since Azure and AWS's resource models differ enough that unifying them would only add complexity.

Both provision infrastructure around the **same, unmodified** Docker image (`docker/backend.Dockerfile`). Building and publishing that image is outside Terraform's scope — push it to a public registry first (Docker Hub for `infra/azure`, ECR Public for `infra/aws`, since AWS App Runner/ECS only pull from ECR).

## `infra/azure` — Azure Container Apps + PostgreSQL Flexible Server

```bash
cd infra/azure
cp terraform.tfvars.example terraform.tfvars   # fill in real values, never commit this file
terraform init
terraform apply
```

Outputs the public HTTPS URL. Container Apps is Consumption-tier with `min_replicas = 0` — costs ~$0 while idle. PostgreSQL is the one resource that accrues real cost while running; stop it when done:

```bash
az postgres flexible-server stop --resource-group smartcondo-rg --name smartcondo-db
```

Or tear down everything:

```bash
terraform destroy
```

## `infra/aws` — ECS/Fargate + RDS PostgreSQL

AWS App Runner was ADR-0011's original target but requires the account to be on a Paid plan (a Free-plan account gets `SubscriptionRequiredException`); this module uses ECS/Fargate instead, which proves the same portability claim.

```bash
cd infra/aws
cp terraform.tfvars.example terraform.tfvars   # fill in real values, never commit this file
terraform init
terraform apply                                 # desired_count defaults to 0 - defines the service, doesn't run it
terraform apply -var desired_count=1            # actually start the task
```

Fargate has no built-in scale-to-zero, unlike Container Apps — `desired_count = 0` is how this module gets the equivalent. RDS is the other real cost driver; stop it or tear everything down when done:

```bash
aws rds stop-db-instance --db-instance-identifier smartcondo-db --region us-east-1
# or
terraform destroy
```

## Cost discipline

Standing project rule: after any validation against real cloud resources, stop or tear down every paid-tier resource on both clouds before finishing. Neither module is meant to stay online permanently — reproducible in under 30 minutes from a clean `terraform apply` is the actual goal.
