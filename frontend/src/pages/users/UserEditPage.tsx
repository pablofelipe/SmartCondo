import React from 'react';
import { useParams } from 'react-router-dom';
import UserForm from './UserForm';

const UserEditPage = () => {
  const { userId } = useParams<{ userId: string }>();

  return (
    <div>
      <UserForm mode="edit" userId={Number(userId)} />
    </div>
  );
};

export default UserEditPage;
