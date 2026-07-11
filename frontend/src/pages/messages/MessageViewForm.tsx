interface MessageFormProps {
  messageId?: number;
}

const MessageViewForm = ({ messageId }: MessageFormProps) => {
  return (
    <div>
      <h1>Visualização de Mensagem</h1>
      {/* Formulário de visualização de mensagem */}
      messageId
    </div>
  );
};

export default MessageViewForm;
