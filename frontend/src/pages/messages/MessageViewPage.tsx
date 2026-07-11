import React from 'react';
import { useParams } from 'react-router-dom';
import MessageForm from './MessageViewForm';

const MessageViewPage = () => {
  const { messageId } = useParams<{ messageId: string }>();

  return (
    <div>
      <MessageForm messageId={Number(messageId)} />
    </div>
  );
};

export default MessageViewPage;
