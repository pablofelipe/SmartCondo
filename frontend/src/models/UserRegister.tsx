import { Service } from "./Service";

export class UserRegister {
  email: string;
  password: string;
  confirmPassword: string;
  expiration: Date;
  enabled: boolean;
  name: string;
  address: string;
  type: number;
  services: Service[] | null;

  constructor(
    email: string,
    password: string,
    confirmPassword: string,
    expiration: Date,
    enabled: boolean,
    name: string,
    address: string,
    type: number,
    services: Service[] | null = null
  ) {
    this.email = email;
    this.password = password;
    this.confirmPassword = confirmPassword;
    this.expiration = expiration;
    this.enabled = enabled;
    this.name = name;
    this.address = address;
    this.type = type;
    this.services = services;
  }
}