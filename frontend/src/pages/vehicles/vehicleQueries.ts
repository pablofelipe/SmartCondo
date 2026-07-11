import { gql } from '@apollo/client';

export const GET_VEHICLES = gql`
  query GetVehicles($filter: VehicleFilterInput) {
    vehicles(filter: $filter) {
      id
      type
      licensePlate
      brand
      model
      color
      enabled
      user {
        id
        name
        registrationNumber
        apartment
        parkingSpaceNumber
      }
    }
  }
`;

export const GET_VEHICLE = gql`
  query GetVehicle($id: ID!) {
    vehicle(id: $id) {
      id
      type
      licensePlate
      brand
      model
      color
      enabled
      user {
        id
        name
      }
    }
  }
`;

export const GET_USER_PROFILE = gql`
  query GetUserProfile($id: ID!) {
    user(id: $id) {
      id
      name
    }
  }
`;

export const CREATE_VEHICLE = gql`
  mutation CreateVehicle($input: VehicleInput!) {
    createVehicle(input: $input) {
      id
      licensePlate
    }
  }
`;

export const UPDATE_VEHICLE = gql`
  mutation UpdateVehicle($id: ID!, $input: VehicleInput!) {
    updateVehicle(id: $id, input: $input) {
      id
      licensePlate
    }
  }
`;

export const DELETE_VEHICLE = gql`
  mutation DeleteVehicle($id: ID!) {
    deleteVehicle(id: $id)
  }
`;