import React from 'react';
import VehicleForm from './VehicleForm';
import { useParams } from 'react-router-dom';

const VehicleCreatePage = () => {
  const { userId } = useParams<{ userId?: string }>();
  return (
    <div>
      <VehicleForm mode="create" userId={userId ? Number(userId) : 0} />
    </div>
  );
};

export default VehicleCreatePage;
