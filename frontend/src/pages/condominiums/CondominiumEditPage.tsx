import React from 'react';
import { useParams } from 'react-router-dom';
import CondominiumForm from './CondominiumForm';

const CondominiumEditPage = () => {
  const { condominiumId } = useParams<{ condominiumId: string }>();

  return (
    <div>
      <CondominiumForm mode="edit" condominiumId={Number(condominiumId)} />
    </div>
  );
};

export default CondominiumEditPage;
