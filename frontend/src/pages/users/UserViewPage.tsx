import React from 'react';
import { useParams } from 'react-router-dom';
import UserForm from './UserForm';

const UserViewPage = () => {
  const { userId } = useParams<{ userId: string }>();

  return (
    <div>
      <UserForm mode="view" userId={Number(userId)} />
    </div>
  );
};

export default UserViewPage;
