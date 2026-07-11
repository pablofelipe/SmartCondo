import React from 'react';
import { useParams } from 'react-router-dom';
import VehicleForm from './VehicleForm';

const VehicleViewPage = () => {
  const { vehicleId } = useParams<{ vehicleId: string }>();
  return (
    <div>
      <VehicleForm mode="view" vehicleId={Number(vehicleId)} />
    </div>
  );
};

export default VehicleViewPage;
