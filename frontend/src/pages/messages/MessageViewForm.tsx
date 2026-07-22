interface MessageFormProps {
  messageId?: number;
}

const MessageViewForm = ({ messageId }: MessageFormProps) => {
  return (
    <div>
      <h1>Message View</h1>
      {/* Message view form */}
      messageId
    </div>
  );
};

export default MessageViewForm;
