import React, { useState, useEffect } from 'react';
import {
  getReceivedMessages,
  getSentMessages,
  markAsRead,
} from '../../services/messageService';
import { Message } from '../../types/message';
import { useNavigate } from 'react-router-dom';
import '../../styles/messageList.css';

const MessageListPage: React.FC = () => {
  const navigate = useNavigate();
  const [messages, setMessages] = useState<Message[]>([]);
  const [loading, setLoading] = useState<boolean>(true);
  const [activeTab, setActiveTab] = useState<'received' | 'sent'>('received');

  useEffect(() => {
    const loadMessages = async () => {
      try {
        setLoading(true);
        const data =
          activeTab === 'received'
            ? await getReceivedMessages()
            : await getSentMessages();
        setMessages(data);
      } catch (error) {
        console.error('Erro ao carregar mensagens:', error);
      } finally {
        setLoading(false);
      }
    };

    loadMessages();
  }, [activeTab]);

  const handleMarkAsRead = async (messageId: number) => {
    try {
      await markAsRead(messageId);
      setMessages(
        messages.map((msg) =>
          msg.id === messageId
            ? { ...msg, isRead: true, readDate: new Date().toISOString() }
            : msg,
        ),
      );
    } catch (error) {
      console.error('Erro ao marcar como lida:', error);
    }
  };

  const handleCreateNewMessage = () => {
    navigate('/messages/new');
  };

  const formatDate = (dateString: string) => {
    const date = new Date(dateString);
    return (
      date.toLocaleDateString('pt-BR') +
      ' ' +
      date.toLocaleTimeString('pt-BR', { hour: '2-digit', minute: '2-digit' })
    );
  };

  const getRecipientInfo = (message: Message) => {
    if (message.scope === 'individual' && message.recipientName) {
      return message.recipientName;
    }
    if (message.scope === 'condominium' && message.condominiumName) {
      return `Condomínio: ${message.condominiumName}`;
    }
    if (message.scope === 'tower' && message.towerName) {
      return `Torre: ${message.towerName}`;
    }
    if (message.scope === 'floor' && message.floorId) {
      return `Andar: ${message.floorId}`;
    }
    return 'Vários destinatários';
  };

  if (loading) return <div className="loading">Carregando...</div>;

  return (
    <div className="limiter">
      <div className="container-main">
        <div className="wrap-main">
          <div className="form-header">
            <h1 className="main-form-title">Mensagens</h1>

            {/* Abas para mensagens recebidas/enviadas */}
            <div className="message-tabs">
              <button
                className={`tab-button ${
                  activeTab === 'received' ? 'active' : ''
                }`}
                onClick={() => setActiveTab('received')}
              >
                Recebidas
              </button>
              <button
                className={`tab-button ${activeTab === 'sent' ? 'active' : ''}`}
                onClick={() => setActiveTab('sent')}
              >
                Enviadas
              </button>
            </div>

            <div className="message-list-simple">
              {messages.length > 0 ? (
                messages.map((message) => (
                  <div
                    key={message.id}
                    className={`message-item ${
                      !message.isRead && activeTab === 'received'
                        ? 'unread'
                        : ''
                    }`}
                  >
                    <div className="message-header">
                      <span className="message-date">
                        {formatDate(message.sentDate)}
                      </span>
                      {activeTab === 'received' ? (
                        <span className="message-sender">
                          De: {message.senderName}
                        </span>
                      ) : (
                        <span className="message-recipient">
                          Para: {getRecipientInfo(message)}
                        </span>
                      )}
                      {activeTab === 'received' && !message.isRead && (
                        <button
                          className="mark-as-read-btn"
                          onClick={() => handleMarkAsRead(message.id)}
                        >
                          Marcar como lida
                        </button>
                      )}
                    </div>
                    <div className="message-content">
                      {message.content.length > 100
                        ? `${message.content.substring(0, 100)}...`
                        : message.content}
                    </div>
                    <div className="message-scope">
                      {message.scope === 'individual'
                        ? ' Tipo: Individual'
                        : message.scope === 'condominium'
                        ? 'Tipo: Condomínio'
                        : message.scope === 'tower'
                        ? 'Tipo: Torre'
                        : message.scope === 'floor'
                        ? 'Tipo: Andar'
                        : ''}
                    </div>
                  </div>
                ))
              ) : (
                <div className="no-messages">
                  Nenhuma mensagem{' '}
                  {activeTab === 'received' ? 'recebida' : 'enviada'}
                </div>
              )}
            </div>

            {/* Botões de ação */}

            <div className="container-btn100-form-btn">
              <div className="wrap-btn100-form-btn">
                <div className="btn100-form-bgbtn"></div>
                <button
                  type="submit"
                  className="btn100-form-btn"
                  onClick={handleCreateNewMessage}
                >
                  Nova Mensagem
                </button>
              </div>
            </div>

            <div className="text-center p-t-20">
              <span
                className="txt1"
                onClick={() => navigate('/dashboard')}
                style={{ cursor: 'pointer' }}
              >
                Voltar ao Dashboard
              </span>
            </div>
          </div>
        </div>
      </div>
    </div>
  );
};

export default MessageListPage;
