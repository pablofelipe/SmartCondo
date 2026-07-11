import React from 'react';
import { useParams } from 'react-router-dom';
import VehicleForm from './VehicleForm';

const VehicleEditPage = () => {
  const { vehicleId } = useParams<{ vehicleId: string }>();
  return (
    <div>
      <VehicleForm mode="edit" vehicleId={Number(vehicleId)} />
    </div>
  );
};

export default VehicleEditPage;
