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
        console.error('Error loading messages:', error);
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
      console.error('Error marking as read:', error);
    }
  };

  const handleCreateNewMessage = () => {
    navigate('/messages/new');
  };

  const formatDate = (dateString: string) => {
    const date = new Date(dateString);
    return (
      date.toLocaleDateString('en-US') +
      ' ' +
      date.toLocaleTimeString('en-US', { hour: '2-digit', minute: '2-digit' })
    );
  };

  const getRecipientInfo = (message: Message) => {
    if (message.scope === 'individual' && message.recipientName) {
      return message.recipientName;
    }
    if (message.scope === 'condominium' && message.condominiumName) {
      return `Condominium: ${message.condominiumName}`;
    }
    if (message.scope === 'tower' && message.towerName) {
      return `Tower: ${message.towerName}`;
    }
    if (message.scope === 'floor' && message.floorId) {
      return `Floor: ${message.floorId}`;
    }
    return 'Multiple recipients';
  };

  if (loading) return <div className="loading">Loading...</div>;

  return (
    <div className="limiter">
      <div className="container-main">
        <div className="wrap-main">
          <div className="form-header">
            <h1 className="main-form-title">Messages</h1>

            {/* Tabs for received/sent messages */}
            <div className="message-tabs">
              <button
                className={`tab-button ${
                  activeTab === 'received' ? 'active' : ''
                }`}
                onClick={() => setActiveTab('received')}
              >
                Received
              </button>
              <button
                className={`tab-button ${activeTab === 'sent' ? 'active' : ''}`}
                onClick={() => setActiveTab('sent')}
              >
                Sent
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
                          From: {message.senderName}
                        </span>
                      ) : (
                        <span className="message-recipient">
                          To: {getRecipientInfo(message)}
                        </span>
                      )}
                      {activeTab === 'received' && !message.isRead && (
                        <button
                          className="mark-as-read-btn"
                          onClick={() => handleMarkAsRead(message.id)}
                        >
                          Mark as read
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
                        ? ' Type: Individual'
                        : message.scope === 'condominium'
                        ? 'Type: Condominium'
                        : message.scope === 'tower'
                        ? 'Type: Tower'
                        : message.scope === 'floor'
                        ? 'Type: Floor'
                        : ''}
                    </div>
                  </div>
                ))
              ) : (
                <div className="no-messages">
                  No messages{' '}
                  {activeTab === 'received' ? 'received' : 'sent'}
                </div>
              )}
            </div>

            {/* Action buttons */}

            <div className="container-btn100-form-btn">
              <div className="wrap-btn100-form-btn">
                <div className="btn100-form-bgbtn"></div>
                <button
                  type="submit"
                  className="btn100-form-btn"
                  onClick={handleCreateNewMessage}
                >
                  New Message
                </button>
              </div>
            </div>

            <div className="text-center p-t-20">
              <span
                className="txt1"
                onClick={() => navigate('/dashboard')}
                style={{ cursor: 'pointer' }}
              >
                Back to Dashboard
              </span>
            </div>
          </div>
        </div>
      </div>
    </div>
  );
};

export default MessageListPage;
