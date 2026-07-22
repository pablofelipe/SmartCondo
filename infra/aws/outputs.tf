output "cluster_name" {
  value = aws_ecs_cluster.this.name
}

output "how_to_get_public_ip" {
  description = "ECS/Fargate doesn't expose a task's dynamically-assigned public IP as a Terraform-known value. After desired_count > 0 and the task reaches RUNNING, fetch it via the AWS CLI."
  value       = "aws ecs describe-tasks --cluster ${aws_ecs_cluster.this.name} --tasks $(aws ecs list-tasks --cluster ${aws_ecs_cluster.this.name} --query 'taskArns[0]' --output text) --query 'tasks[0].attachments[0].details[?name==`networkInterfaceId`].value' --output text | xargs -I{} aws ec2 describe-network-interfaces --network-interface-ids {} --query 'NetworkInterfaces[0].Association.PublicIp' --output text"
}

output "db_endpoint" {
  value = aws_db_instance.this.address
}
