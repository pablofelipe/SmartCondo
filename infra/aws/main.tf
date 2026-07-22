data "aws_vpc" "default" {
  default = true
}

data "aws_subnets" "default" {
  filter {
    name   = "vpc-id"
    values = [data.aws_vpc.default.id]
  }
}

# --- Networking ---

resource "aws_security_group" "fargate" {
  name        = "smartcondo-fargate-sg"
  description = "SmartCondo Fargate task inbound"
  vpc_id      = data.aws_vpc.default.id

  ingress {
    from_port   = 8080
    to_port     = 8080
    protocol    = "tcp"
    cidr_blocks = ["0.0.0.0/0"]
  }

  egress {
    from_port   = 0
    to_port     = 0
    protocol    = "-1"
    cidr_blocks = ["0.0.0.0/0"]
  }
}

resource "aws_security_group" "rds" {
  name        = "smartcondo-rds-sg"
  description = "SmartCondo RDS inbound"
  vpc_id      = data.aws_vpc.default.id

  # 0.0.0.0/0 on the DB port is a demo-only shortcut (no VPC-scoped access), matching the
  # "reproducible in under 30 minutes, not a permanent production system" framing of ADR-0011 -
  # a real production setup would scope this to the Fargate security group only.
  ingress {
    from_port   = 5432
    to_port     = 5432
    protocol    = "tcp"
    cidr_blocks = ["0.0.0.0/0"]
  }

  egress {
    from_port   = 0
    to_port     = 0
    protocol    = "-1"
    cidr_blocks = ["0.0.0.0/0"]
  }
}

# --- Database ---

# db.t4g.micro was the cheapest instance class verified against the real AWS Price List API.
resource "aws_db_instance" "this" {
  identifier             = "smartcondo-db"
  engine                 = "postgres"
  engine_version         = "16"
  instance_class         = "db.t4g.micro"
  allocated_storage      = 20
  db_name                = "smartcondo"
  username               = "smartcondoadmin"
  password               = var.db_password
  publicly_accessible    = true
  vpc_security_group_ids = [aws_security_group.rds.id]
  backup_retention_period = 0
  multi_az                = false
  skip_final_snapshot     = true
}

# --- ECS/Fargate ---

resource "aws_iam_role" "ecs_task_execution" {
  name = "smartcondoEcsTaskExecutionRole"

  assume_role_policy = jsonencode({
    Version = "2012-10-17"
    Statement = [{
      Effect    = "Allow"
      Principal = { Service = "ecs-tasks.amazonaws.com" }
      Action    = "sts:AssumeRole"
    }]
  })
}

resource "aws_iam_role_policy_attachment" "ecs_task_execution" {
  role       = aws_iam_role.ecs_task_execution.name
  policy_arn = "arn:aws:iam::aws:policy/service-role/AmazonECSTaskExecutionRolePolicy"
}

# The managed AmazonECSTaskExecutionRolePolicy covers CreateLogStream/PutLogEvents but not
# CreateLogGroup - discovered the hard way during manual provisioning (a task failed at
# startup with AccessDeniedException). Creating the log group here avoids needing to widen
# the role's permissions for something Terraform can just do itself.
resource "aws_cloudwatch_log_group" "backend" {
  name              = "/ecs/smartcondo-backend"
  retention_in_days = 7
}

resource "aws_ecs_cluster" "this" {
  name = "smartcondo-cluster"
}

resource "aws_ecs_task_definition" "backend" {
  family                   = "smartcondo-backend"
  requires_compatibilities = ["FARGATE"]
  network_mode             = "awsvpc"
  cpu                      = "256"
  memory                   = "512"
  execution_role_arn       = aws_iam_role.ecs_task_execution.arn

  container_definitions = jsonencode([
    {
      name  = "smartcondo-backend"
      image = var.image_uri
      portMappings = [{ containerPort = 8080, protocol = "tcp" }]
      environment = [
        { name = "ASPNETCORE_ENVIRONMENT", value = "Production" },
        { name = "ASPNETCORE_URLS", value = "http://+:8080" },
        { name = "DB_HOST", value = aws_db_instance.this.address },
        { name = "DB_NAME", value = "smartcondo" },
        { name = "DB_USER", value = "smartcondoadmin" },
        { name = "DB_PASSWORD", value = var.db_password },
        { name = "JWT_KEY", value = var.jwt_key },
        { name = "MIGRATION_AUTH_KEY", value = var.migration_auth_key },
        { name = "ADMIN_EMAIL", value = var.admin_email },
        { name = "ADMIN_PASSWORD", value = var.admin_password },
      ]
      logConfiguration = {
        logDriver = "awslogs"
        options = {
          "awslogs-group"         = aws_cloudwatch_log_group.backend.name
          "awslogs-region"        = var.region
          "awslogs-stream-prefix" = "ecs"
        }
      }
    }
  ])
}

# A bare ECS Service (no load balancer) - App Runner was the original ADR-0011 target, but it
# requires the AWS account to be on a Paid plan (SubscriptionRequiredException on a Free-plan
# account), so this validates the same portability claim via ECS/Fargate instead. desired_count
# is intentionally a variable, not hardcoded to 1, so this can be applied at 0 (defined, not
# running - $0 compute) and scaled up only to actually demo it.
resource "aws_ecs_service" "backend" {
  name            = "smartcondo-backend"
  cluster         = aws_ecs_cluster.this.id
  task_definition = aws_ecs_task_definition.backend.arn
  desired_count   = var.desired_count
  launch_type     = "FARGATE"

  network_configuration {
    subnets          = data.aws_subnets.default.ids
    security_groups  = [aws_security_group.fargate.id]
    assign_public_ip = true
  }
}
