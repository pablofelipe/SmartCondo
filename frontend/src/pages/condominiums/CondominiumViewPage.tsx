import React from 'react';
import { useParams } from 'react-router-dom';
import CondominiumForm from './CondominiumForm';

const CondominiumViewPage = () => {
  const { condominiumId } = useParams<{ condominiumId: string }>();

  return (
    <div>
      <CondominiumForm mode="view" condominiumId={Number(condominiumId)} />
    </div>
  );
};

export default CondominiumViewPage;
