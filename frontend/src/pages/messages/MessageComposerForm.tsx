/* eslint-disable eqeqeq */
import React, { useEffect, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { sendMessage } from '../../services/messageService';
import { MessageCreateDto } from '../../types/message';
import './messageComposerForm.module.css';
import config from '../../config';
import { getAuthHeaders } from '../../utils/ApiUtils';
import { usePermissions } from '../hooks/usePermissions';

interface Tower {
  id: number;
  number: string;
  name: string;
}

const MessageComposerForm: React.FC = () => {
  const navigate = useNavigate();
  const [formData, setFormData] = useState<MessageCreateDto>({
    content: '',
    scope: 'individual',
  });
  const [error, setError] = useState<string>('');
  const [selectedRecipientType, setSelectedRecipientType] =
    useState<string>('');

  const [towers, setTowers] = useState<Tower[]>([]);
  const [isLoadingTowers, setIsLoadingTowers] = useState(false);

  const {
    canSendToGroups,
    canSendToIndividuals,
    canManageAllCondominiums,
    getAllowedRecipientTypes,
    getUserTypeId,
    getUserTypeDescriptionByName,
  } = usePermissions();

  const [condominiums, setCondominiums] = useState([]);

  const [currentUserCondominium, setCurrentUserCondominium] =
    useState<any>(null);

  useEffect(() => {
    const fetchCondominiums = async () => {
      try {
        const headers = getAuthHeaders();
        if (!headers.Authorization) return;

        const userString = localStorage.getItem('user');
        if (!userString) return;

        const user = JSON.parse(userString) as {
          condominiumId: number;
          role: string;
        };

        if (user.role == 'SystemAdministrator') {
          const response = await fetch(`${config.apiUrl}/Condominium`, {
            method: 'GET',
            headers,
          });
          const data = await response.json();
          setCondominiums(data);
        } else if (user.condominiumId) {
          const response = await fetch(
            `${config.apiUrl}/Condominium/${user.condominiumId}`,
            {
              method: 'GET',
              headers,
            },
          );
          const data = await response.json();
          setCurrentUserCondominium(data);
        }
      } catch (error) {
        console.error('Erro ao carregar condomínios:', error);
      }
    };

    fetchCondominiums();
  }, []);

  const userString = localStorage.getItem('user');
  const user = userString
    ? (JSON.parse(userString) as { condominiumId: number; role: string })
    : null;

  const isSysAdmin = user?.role == 'SystemAdministrator';
  const condominiumId = canManageAllCondominiums()
    ? user?.condominiumId
    : currentUserCondominium?.id;

  useEffect(() => {
    const fetchTowers = async () => {
      if (
        !condominiumId ||
        (formData.scope !== 'tower' && formData.scope !== 'floor')
      )
        return;

      setIsLoadingTowers(true);
      try {
        const headers = getAuthHeaders();
        if (!headers.Authorization) return;

        const response = await fetch(
          `${config.apiUrl}/Tower/byCondominium/${condominiumId}`,
          {
            method: 'GET',
            headers,
          },
        );

        if (!response.ok) throw new Error('Erro ao carregar torres');

        const data = await response.json();
        setTowers(data);
      } catch (error) {
        console.error('Erro ao carregar torres:', error);
        setError('Erro ao carregar lista de torres');
      } finally {
        setIsLoadingTowers(false);
      }
    };

    fetchTowers();
  }, [condominiumId, formData.scope]);

  const handleChange = (
    e: React.ChangeEvent<
      HTMLInputElement | HTMLTextAreaElement | HTMLSelectElement
    >,
  ) => {
    const { name, value } = e.target;
    setError('');
    setFormData((prev) => ({
      ...prev,
      [name]:
        name == 'recipientId' ||
        name == 'condominiumId' ||
        name == 'towerId' ||
        name == 'floorId'
          ? Number(value)
          : value,
    }));
  };

  type UserProfileSearchResultDto = {
    id: number;
    name: string;
    registrationNumber?: string;
    type: string;
  };

  const [searchTerm, setSearchTerm] = useState({
    name: '',
    registrationNumber: '',
  });
  const [searchResults, setSearchResults] = useState<
    UserProfileSearchResultDto[]
  >([]);
  const [isSearching, setIsSearching] = useState(false);

  const [showSuccessToast, setShowSuccessToast] = useState(false);

  const allowedRecipientTypes = getAllowedRecipientTypes() || [];

  const handleSearch = async () => {
    if (!selectedRecipientType) {
      setError('Selecione um tipo de destinatário');
      return;
    }

    setError('');
    setSearchResults([]);
    setIsSearching(true);

    try {
      const params = new URLSearchParams();

      if (selectedRecipientType == 'Resident') {
        if (searchTerm.name == '' && searchTerm.registrationNumber == '') {
          setError('Preencha pelo menos um campo de busca');
          return;
        }

        params.append('Name', searchTerm.name);
        params.append('RegistrationNumber', searchTerm.registrationNumber);
      }

      params.append('Type', getUserTypeId(selectedRecipientType).toString());

      const headers = getAuthHeaders();

      if (!headers.Authorization) {
        return;
      }

      const userString = localStorage.getItem('user');
      if (!userString) {
        return;
      }

      const user = JSON.parse(userString) as { condominiumId: number };

      var fullUrl = `${config.apiUrl}/Condominium/${user.condominiumId}/users/search?${params}`;

      console.log(`Searching url: ${fullUrl}`);

      const response = await fetch(fullUrl, {
        headers: headers,
      });
      const data = await response.json();
      if (data.length == 0) {
        setError('Nenhum resultado encontrado');
        return;
      }
      setSearchResults(data);
    } catch (err) {
      setError('Falha na busca');
    } finally {
      setIsSearching(false);
    }
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setError('');

    try {
      const userString = localStorage.getItem('user');
      if (!userString) {
        return;
      }

      const user = JSON.parse(userString) as { condominiumId: number };

      formData.condominiumId = user.condominiumId;

      await sendMessage(formData);

      setShowSuccessToast(true);
      // navegar após um pequeno delay para o usuário ver a mensagem
      setTimeout(() => navigate('/dashboard'), 2000);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Erro ao enviar mensagem');
    }
  };

  const shouldShowNameField =
    selectedRecipientType == 'Resident' ||
    selectedRecipientType == 'ResidentCommitteeMember';

  const shouldShowRegistrationField =
    selectedRecipientType == 'Resident' ||
    selectedRecipientType == 'ResidentCommitteeMember';

  //console.log(`selectedRecipientType: ${selectedRecipientType}`);

  return (
    <div className="limiter">
      <div className="container-main">
        <div className="wrap-main">
          <div className="form-header">
            <h2 className="form-title">Enviar Mensagem</h2>
            <p className="form-subtitle">
              Preencha os campos abaixo para enviar uma nova mensagem
            </p>
          </div>

          <form onSubmit={handleSubmit}>
            {/* Campo de Escopo */}
            <div className="wrap-input100 validate-input">
              <select
                className={`input100 ${formData.scope ? 'has-val' : ''}`}
                name="scope"
                value={formData.scope}
                onChange={handleChange}
                required
              >
                {canSendToIndividuals && (
                  <option value="individual">Individual</option>
                )}
                {canSendToGroups && (
                  <option value="condominium">Condomínio</option>
                )}
                {canSendToGroups && <option value="tower">Torre</option>}
                {canSendToGroups && <option value="floor">Andar</option>}
              </select>
              <span className="focus-input100" data-placeholder="Escopo"></span>
            </div>

            {/* Campo de Destinatário (condicional) */}
            {canSendToIndividuals && formData.scope == 'individual' && (
              <div className="user-search-section">
                <div className="wrap-input100 validate-input">
                  <select
                    className={`input100 ${
                      selectedRecipientType ? 'has-val' : ''
                    }`}
                    value={selectedRecipientType}
                    onChange={(e) => {
                      setSelectedRecipientType(e.target.value);
                      setSearchResults([]);
                      setFormData((prev) => ({
                        ...prev,
                        recipientId: undefined,
                      }));
                    }}
                    required
                  >
                    <option value="">Selecione o tipo de destinatário</option>
                    {allowedRecipientTypes.map((type) => (
                      <option key={type} value={type}>
                        {getUserTypeDescriptionByName(type)}
                      </option>
                    ))}
                  </select>
                  <span
                    className="focus-input100"
                    data-placeholder="Tipo de Destinatário"
                  ></span>
                </div>

                {/* Container de busca responsivo */}
                <div className="search-container" style={{ marginTop: '15px' }}>
                  <div
                    className="search-fields"
                    style={{
                      display: 'flex',
                      gap: '10px',
                      alignItems: 'center',
                      flexWrap: 'wrap',
                    }}
                  >
                    {shouldShowNameField && (
                      <input
                        placeholder="Nome"
                        value={searchTerm.name}
                        onChange={(e) =>
                          setSearchTerm({ ...searchTerm, name: e.target.value })
                        }
                        style={{
                          flex: '1 1 150px',
                          minWidth: '120px',
                          marginBottom: '10px',
                        }}
                      />
                    )}

                    {shouldShowRegistrationField && (
                      <input
                        placeholder="CPF/CNPJ"
                        value={searchTerm.registrationNumber}
                        onChange={(e) =>
                          setSearchTerm({
                            ...searchTerm,
                            registrationNumber: e.target.value,
                          })
                        }
                        style={{
                          flex: '1 1 150px',
                          minWidth: '120px',
                          marginBottom: '10px',
                        }}
                      />
                    )}

                    {(shouldShowNameField || shouldShowRegistrationField) && (
                      <div
                        className="container-btn100-form-btn"
                        style={{
                          flex: '1 1 100%',
                          minWidth: '100px',
                          marginBottom: '10px',
                        }}
                      >
                        <div className="wrap-btn100-form-btn">
                          <div className="btn100-form-bgbtn"></div>
                          <button
                            type="button"
                            className="btn100-form-btn"
                            onClick={handleSearch}
                            disabled={isSearching || !selectedRecipientType}
                            style={{ width: '100%' }}
                          >
                            {isSearching ? 'Buscando...' : 'Buscar'}
                          </button>
                        </div>
                      </div>
                    )}
                  </div>
                </div>

                {searchResults.length > 0 && (
                  <div style={{ marginTop: '15px' }}>
                    <div className="wrap-input100 validate-input">
                      <select
                        className={`input100 ${
                          formData.recipientUserId ? 'has-val' : ''
                        }`}
                        value={formData.recipientUserId || ''}
                        onChange={(e) =>
                          setFormData({
                            ...formData,
                            recipientUserId: Number(e.target.value),
                          })
                        }
                        required
                        style={{ width: '100%' }}
                      >
                        <option value="">Selecione um usuário</option>
                        {searchResults.map((user) => (
                          <option key={user.id} value={user.id}>
                            {user.name}
                            {user.registrationNumber &&
                              ` - ${user.registrationNumber}`}
                          </option>
                        ))}
                      </select>
                      <span
                        className="focus-input100"
                        data-placeholder="Destinatário"
                      ></span>
                    </div>
                  </div>
                )}
              </div>
            )}

            {/* SystemAdmin: Mostra dropdown com todos os condomínios */}
            {formData.scope != 'individual' &&
              canManageAllCondominiums() &&
              !isSysAdmin && (
                <div className="wrap-input100 validate-input">
                  <label htmlFor="condominium" className="input-label">
                    Condomínio
                  </label>
                  <select
                    className={`input100 ${
                      formData.condominiumId ? 'has-val' : ''
                    }`}
                    name="condominiumId"
                    value={formData.condominiumId}
                    onChange={handleChange}
                    required
                  >
                    <option value="">Selecione</option>
                    {condominiums.map((condo: any) => (
                      <option key={condo.id} value={condo.id}>
                        {condo.name}
                      </option>
                    ))}
                  </select>
                </div>
              )}

            {/* Não-SystemAdmin: Mostra apenas o condomínio vinculado (sem dropdown) */}
            {formData.scope != 'individual' &&
              !canManageAllCondominiums() &&
              currentUserCondominium && (
                <div className="wrap-input100">
                  <label className="input-label">Condomínio</label>
                  <input
                    type="text"
                    className="input100"
                    value={currentUserCondominium.name}
                    readOnly
                  />
                  <input
                    type="hidden"
                    name="condominiumId"
                    value={currentUserCondominium.id}
                  />
                </div>
              )}

            {/* Seletor de Torre */}
            {(formData.scope === 'tower' || formData.scope === 'floor') && (
              <div
                className="wrap-input100 validate-input"
                style={{ marginTop: '15px' }}
              >
                <select
                  className={`input100 ${formData.towerId ? 'has-val' : ''}`}
                  name="towerId"
                  value={formData.towerId || ''}
                  onChange={handleChange}
                  required
                  disabled={isLoadingTowers || !condominiumId}
                >
                  <option value="">Selecione a Torre</option>
                  {isLoadingTowers ? (
                    <option value="" disabled>
                      Carregando torres...
                    </option>
                  ) : (
                    towers.map((tower) => (
                      <option key={tower.id} value={tower.id}>
                        {tower.number} - {tower.name}
                      </option>
                    ))
                  )}
                </select>
                <span
                  className="focus-input100"
                  data-placeholder="Torre"
                ></span>
              </div>
            )}

            {/* Andar */}
            {formData.scope === 'floor' && (
              <div className="wrap-input100 validate-input">
                <input
                  className={`input100 ${formData.floorId ? 'has-val' : ''}`}
                  name="floorId"
                  type="number"
                  value={formData.floorId}
                  onChange={handleChange}
                  required={!formData.towerId}
                />
                <span
                  className="focus-input100 focus-number"
                  data-placeholder="Andar"
                ></span>
              </div>
            )}

            {/* Campo de Conteúdo da Mensagem */}
            <div className="wrap-input100 validate-input">
              <textarea
                className={`input100 ${formData.content ? 'has-val' : ''}`}
                name="content"
                value={formData.content}
                onChange={handleChange}
                required
                rows={5}
              />
              <span
                className="focus-input100"
                data-placeholder="Mensagem"
              ></span>
            </div>

            {/* Botão de Envio */}
            <div className="container-btn100-form-btn">
              <div className="wrap-btn100-form-btn">
                <div className="btn100-form-bgbtn"></div>
                <button type="submit" className="btn100-form-btn">
                  Enviar
                </button>
              </div>
            </div>

            {error && (
              <p className="text-center" style={{ color: '#c80000' }}>
                {error}
              </p>
            )}

            {showSuccessToast && (
              <div className="toast-success">
                Mensagem enviada com sucesso!
                <button onClick={() => setShowSuccessToast(false)}>×</button>
              </div>
            )}

            <div className="text-center p-t-20">
              <span
                className="txt1"
                onClick={() => navigate('/dashboard')}
                style={{ cursor: 'pointer' }}
              >
                Voltar ao Dashboard
              </span>
            </div>
          </form>
        </div>
      </div>
    </div>
  );
};

export default MessageComposerForm;
