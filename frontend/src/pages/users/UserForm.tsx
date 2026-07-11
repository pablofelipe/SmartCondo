/* eslint-disable eqeqeq */
import React, { useState, useEffect } from 'react';
import { useNavigate } from 'react-router-dom';
import './userForm.module.css';
import '../../styles/util.css';
import config from '../../config';
import { getAuthHeaders } from '../../utils/ApiUtils';
import { usePermissions } from '../hooks/usePermissions';
import { DeleteConfirmationModal } from '../../utils/DeleteConfirmationModal';

type UserFormMode = 'create' | 'edit' | 'view';

interface UserFormProps {
  mode?: UserFormMode;
  userId?: number;
}

const UserForm = ({ mode = 'create', userId }: UserFormProps) => {
  const navigate = useNavigate();
  const isViewMode = mode == 'view';

  interface UserData {
    name: string;
    address: string;
    userTypeId: number;
    registrationNumber: string;
    phone1: string;
    phone2: string;
    condominiumId?: number;
    towerId?: number;
    floorId?: number;
    apartment?: number;
    parkingSpaceNumber?: number;
  }

  const [userData, setUserData] = useState<UserData>({
    name: '',
    address: '',
    userTypeId: 0,
    registrationNumber: '',
    phone1: '',
    phone2: '',
    condominiumId: 0,
    towerId: 0,
    floorId: 0,
    apartment: 0,
    parkingSpaceNumber: 0,
  });

  interface LoginData {
    email: string;
    password: string;
    confirmPassword: string;
    expiration: string;
    enabled: boolean;
    keyId: string;
    showPasswordFields: boolean;
    passwordLength: number;
  }

  const [loginData, setLoginData] = useState({
    email: '',
    password: '',
    confirmPassword: '',
    expiration: '2100-12-31',
    enabled: true,
    keyId: '',
    showPasswordFields: false,
    passwordLength: 0,
  });

  interface UserFormDTO extends UserData {
    user: Omit<
      LoginData,
      'confirmPassword' | 'showPasswordFields' | 'passwordLength'
    > & {
      confirmPassword?: string;
    };
  }

  const [message, setMessage] = useState<{
    text: string;
    type: 'success' | 'error';
  } | null>(null);

  const [condominiums, setCondominiums] = useState([]);
  const [currentUserCondominium, setCurrentUserCondominium] =
    useState<any>(null);
  const [userTypes, setUserTypes] = useState([]);
  const [towers, setTowers] = useState([]);
  const [errors, setErrors] = useState({
    email: '',
    registration: '',
    phone1: '',
    password: '',
    confirmPassword: '',
    name: '',
    address: '',
  });

  // Estados de loading
  const [loading, setLoading] = useState({
    submit: false,
    update: false,
    delete: false,
    changePassword: false,
  });

  const [isDeleteModalOpen, setIsDeleteModalOpen] = useState(false);

  const { canRegisterUser, canManageAllCondominiums } = usePermissions();
  const filteredUserTypes = userTypes.filter((ut: any) =>
    canRegisterUser(ut.name),
  );

  useEffect(() => {
    const fetchUserData = async () => {
      try {
        const headers = getAuthHeaders();

        if (!headers.Authorization) {
          console.error('Token de autenticação não encontrado');
          return;
        }

        const response = await fetch(`${config.apiUrl}/UserProfile/${userId}`, {
          method: 'GET',
          headers: headers,
        });

        console.log(`response: ${JSON.stringify(response)}`);

        if (!response.ok) {
          throw new Error(`Erro ao carregar usuário: ${response.statusText}`);
        }

        const data = await response.json();

        setUserData((prev) => ({
          ...prev,
          name: data?.name || '',
          address: data?.address || '',
          userTypeId: data?.userTypeId || 0,
          registrationNumber: data?.registrationNumber || '',
          phone1: data?.phone1 || '',
          phone2: data?.phone2 || '',
          condominiumId: data?.condominiumId || 0,
          towerId: data?.towerId || 0,
          floorId: data?.floorId || 0,
          apartment: data?.apartment || 0,
          parkingSpaceNumber: data?.parkingSpaceNumber || 0,
        }));

        setLoginData((prev) => ({
          ...prev,
          email: data?.email || '',
          password: '',
          confirmPassword: '',
          expiration: data?.expiration || '2100-12-31',
          enabled: data?.enabled != undefined ? data?.enabled : true,
          keyId: data?.keyId || '',
          passwordLength: data.passwordLength || 8,
        }));
      } catch (error) {
        console.error('Erro ao carregar usuário:', error);
        setMessage({
          text: 'Erro ao carregar dados do usuário',
          type: 'error',
        });
      }
    };

    if ((mode == 'edit' || mode == 'view') && userId) {
      fetchUserData();
    }
  }, [mode, userId]);

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

          setUserData((prev) => ({
            ...prev,
            condominiumId: data.id,
          }));
        }
      } catch (error) {
        console.error('Erro ao carregar condomínios:', error);
      }
    };

    fetchCondominiums();
  }, []);

  useEffect(() => {
    const fetchUserTypes = async () => {
      try {
        const headers = getAuthHeaders();

        if (!headers.Authorization) {
          return;
        }

        const response = await fetch(`${config.apiUrl}/UserType`, {
          method: 'GET',
          headers: headers,
        });

        const data = await response.json();
        setUserTypes(data);
      } catch (error) {
        console.error('Erro ao carregar tipos de usuario:', error);
      }
    };

    fetchUserTypes();
  }, []);

  const managesAllCondominiums = canManageAllCondominiums();

  useEffect(() => {
    const fetchTowers = async () => {
      try {
        const condominiumId = managesAllCondominiums
          ? userData.condominiumId
          : currentUserCondominium?.id;

        if (!condominiumId || condominiumId == 0) {
          setTowers([]);
          setUserData((prev) => ({
            ...prev,
            towerId: 0,
          }));
          return;
        }

        const headers = getAuthHeaders();
        if (!headers.Authorization) return;

        const fullUrl = `${config.apiUrl}/Tower/byCondominium/${condominiumId}`;
        const response = await fetch(fullUrl, { method: 'GET', headers });

        if (!response.ok) throw new Error('Erro na resposta da API');

        const data = await response.json();
        setTowers(data);
      } catch (error) {
        console.error('Erro ao carregar torres:', error);
      }
    };

    fetchTowers();
  }, [userData.condominiumId, currentUserCondominium, managesAllCondominiums]);

  const handleGoToDashboard = () => {
    navigate('/dashboard');
  };

  const handleUserChange = (e: any) => {
    const { name, value } = e.target;
    setUserData({
      ...userData,
      [name]: value,
    });
  };

  const handleLoginChange = (e: any) => {
    const { name, value } = e.target;
    setLoginData({
      ...loginData,
      [name]: value,
    });
  };

  const handleBlur = (
    e: React.FocusEvent<HTMLInputElement | HTMLSelectElement>,
  ) => {
    const { name, value } = e.target;
    validateField(name, value);
  };

  const validateField = (name: string, value: string) => {
    const newErrors = { ...errors };

    switch (name) {
      case 'email':
        if (!validateEmail(value)) {
          newErrors.email = 'Email inválido';
        } else {
          newErrors.email = '';
        }
        break;

      case 'registrationNumber':
        value = removeNonNumber(value);
        if (!validateCPF(value) && !validateCNPJ(value)) {
          newErrors.registration =
            'CPF deve ter 11 caracteres, CNPJ deve ter 14 caracteres';
        } else {
          newErrors.registration = '';
        }
        break;

      case 'phone1':
        value = removeNonNumber(value);
        if (!validatePhone(value)) {
          newErrors.phone1 = 'Telefone deve ter pelo menos 9 números';
        } else {
          newErrors.phone1 = '';
        }
        break;

      case 'password':
        if (!validatePassword(value)) {
          newErrors.password =
            'A senha deve ter pelo menos 6 caracteres, 1 maiúscula, 1 minúscula, 1 número e 1 caractere especial.';
        } else {
          newErrors.password = '';
        }
        break;

      case 'confirmPassword':
        if (value != loginData.password) {
          newErrors.confirmPassword = 'As senhas não coincidem.';
        } else {
          newErrors.confirmPassword = '';
        }
        break;

      case 'name':
        if (!value || value.length < 3) {
          newErrors.name = `Campo Nome obrigatório e deve ter pelo menos 3 caracteres`;
        } else {
          newErrors.name = '';
        }
        break;

      case 'address':
        if (!value || value.length < 3) {
          newErrors.address = `Campo Endereço obrigatório e deve ter pelo menos 3 caracteres`;
        } else {
          newErrors.address = '';
        }
        break;

      default:
        break;
    }

    setErrors(newErrors);
  };

  const handleDelete = async () => {
    // Se já está carregando, não faz nada
    if (loading.delete) return;

    setLoading((prev) => ({ ...prev, delete: true }));

    try {
      const response = await fetch(`${config.apiUrl}/UserProfile/${userId}`, {
        method: 'DELETE',
        headers: getAuthHeaders(),
      });

      if (response.ok) {
        setMessage({ text: 'Usuário deletado com sucesso', type: 'success' });
        navigate('/users');
      }
    } catch (error) {
      setMessage({ text: 'Erro ao deletar usuário', type: 'error' });
    } finally {
      setLoading((prev) => ({ ...prev, delete: false }));
      setIsDeleteModalOpen(false);
    }
  };

  const validateEmail = (email: string) => {
    const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
    return emailRegex.test(email);
  };

  function removeNonNumber(doc: string): string {
    return doc
      .split('')
      .filter((c) => '0123456789'.includes(c))
      .join('');
  }

  const validateCPF = (cpf: string) => {
    return cpf.length == 11;
  };

  const validateCNPJ = (cnpj: string) => {
    return cnpj.length == 14;
  };

  const validatePhone = (phone: string) => {
    return phone.length >= 9;
  };

  const validatePassword = (password: string) => {
    const passwordRegex = /^(?=.*[A-Z])(?=.*[a-z])(?=.*\d)(?=.*[\W_]).{6,}$/;
    return passwordRegex.test(password);
  };

  const clearForm = () => {
    setUserData({
      name: '',
      address: '',
      userTypeId: 0,
      registrationNumber: '',
      phone1: '',
      phone2: '',
      condominiumId: 0,
      towerId: 0,
      floorId: 0,
      apartment: 0,
      parkingSpaceNumber: 0,
    });

    setLoginData({
      email: '',
      password: '',
      confirmPassword: '',
      expiration: '2100-12-31',
      enabled: true,
      keyId: '',
      showPasswordFields: false,
      passwordLength: 0,
    });
  };

  const isApartmentOwner =
    userData?.userTypeId == 3 || userData?.userTypeId == 13;
  const isSysAdmin = userData?.userTypeId == 1;

  const validateForm = (): boolean => {
    if (
      errors.email ||
      errors.registration ||
      errors.phone1 ||
      errors.password ||
      errors.confirmPassword ||
      errors.name ||
      errors.address
    ) {
      setMessage({
        text: 'Corrija os erros antes de enviar o formulário!',
        type: 'error',
      });
      return false;
    }
    return true;
  };

  const prepareFormData = async (): Promise<{
    userFormDTO: UserFormDTO;
    jsonData: string;
  }> => {
    const userFormDTO: UserFormDTO = {
      ...userData,
      user: {
        email: loginData.email,
        password: loginData.password,
        confirmPassword: '',
        expiration: loginData.expiration,
        enabled: loginData.enabled,
        keyId: loginData.keyId,
      },
    };

    if (!isApartmentOwner) {
      delete userFormDTO.towerId;
      delete userFormDTO.floorId;
      delete userFormDTO.apartment;
      delete userFormDTO.parkingSpaceNumber;
    }

    if (userFormDTO.userTypeId === 1) {
      delete userFormDTO.condominiumId;
    }

    const { confirmPassword, ...userWithoutConfirm } = userFormDTO.user;
    userFormDTO.user = userWithoutConfirm;

    userFormDTO.phone1 = removeNonNumber(userFormDTO.phone1);
    userFormDTO.phone2 = removeNonNumber(userFormDTO.phone2);
    userFormDTO.registrationNumber = removeNonNumber(
      userFormDTO.registrationNumber,
    );

    return {
      userFormDTO,
      jsonData: JSON.stringify(userFormDTO),
    };
  };

  const handleApiError = async (response: Response): Promise<never> => {
    const errorData = await response.json();

    let errorMessage = '';
    if (errorData.errors) {
      errorMessage = Object.values(errorData.errors).flat().join('\n');
    } else {
      errorMessage = errorData.message || 'Erro desconhecido';
    }

    switch (response.status) {
      case 400:
        throw new Error(errorMessage);
      case 401:
        throw new Error(errorMessage);
      case 500:
        throw new Error(
          errorMessage || 'Erro no servidor. Tente novamente mais tarde.',
        );
      default:
        throw new Error('Erro inesperado.');
    }
  };

  const handleGenericError = (err: unknown): string => {
    if (err instanceof TypeError && err.message == 'Failed to fetch') {
      return 'Não foi possível conectar ao servidor. Verifique sua conexão.';
    }
    if (err instanceof Error) {
      return err.message;
    }
    return 'Erro inesperado.';
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();

    // Se já está carregando, não faz nada
    if (loading.submit) return;

    if (!validateForm()) return;

    setMessage(null);
    setLoading((prev) => ({ ...prev, submit: true }));

    try {
      const headers = getAuthHeaders();
      if (!headers.Authorization) return;

      const { jsonData } = await prepareFormData();

      const response = await fetch(`${config.apiUrl}/UserProfile`, {
        method: 'POST',
        headers,
        body: jsonData,
      });

      if (!response.ok) {
        await handleApiError(response);
      }

      const data = await response.json();
      setMessage({ text: data.message, type: 'success' });
      clearForm();
    } catch (err) {
      const error = handleGenericError(err);
      console.error('Erro ao cadastrar usuário:', error);
      setMessage({ text: error, type: 'error' });
    } finally {
      setLoading((prev) => ({ ...prev, submit: false }));
    }
  };

  const handleUpdate = async (e: React.FormEvent) => {
    e.preventDefault();

    // Se já está carregando, não faz nada
    if (loading.update) return;

    if (!validateForm() || !userId) return;

    setMessage(null);
    setLoading((prev) => ({ ...prev, update: true }));

    try {
      const { jsonData } = await prepareFormData();
      const headers = getAuthHeaders();
      if (!headers.Authorization) return;

      const response = await fetch(`${config.apiUrl}/UserProfile/${userId}`, {
        method: 'PUT',
        headers,
        body: jsonData,
      });

      if (!response.ok) {
        await handleApiError(response);
      }

      const data = await response.json();
      setMessage({ text: data.message, type: 'success' });

      setLoginData((prev) => ({ ...prev, showPasswordFields: false }));
    } catch (err) {
      const error = handleGenericError(err);
      console.error('Erro ao atualizar usuário:', error);
      setMessage({ text: error, type: 'error' });
    } finally {
      setLoading((prev) => ({ ...prev, update: false }));
    }
  };

  const handleChangePasswordClick = () => {
    setLoginData((prev) => ({
      ...prev,
      showPasswordFields: true,
      password: '',
      confirmPassword: '',
    }));
  };

  const handleCancelPasswordChange = () => {
    setLoginData((prev) => ({
      ...prev,
      showPasswordFields: false,
      password: '',
      confirmPassword: '',
    }));

    setErrors((prev) => ({
      ...prev,
      password: '',
      confirmPassword: '',
    }));
  };

  return (
    <div className="limiter">
      <div className="container-main">
        <div className="wrap-main">
          <form onSubmit={mode == 'create' ? handleSubmit : handleUpdate}>
            <span className="main-form-title">Cadastro de Usuários</span>

            {/* Campos de Login */}
            <div
              className="wrap-input100 validate-input"
              data-validate="Email válido: a@b.c"
            >
              <input
                className={`input100 ${loginData.email ? 'has-val' : ''}`}
                autoComplete="off"
                name="email"
                type="email"
                value={loginData.email}
                onChange={handleLoginChange}
                onBlur={handleBlur}
                required
                disabled={isViewMode}
              />
              <span className="focus-input100" data-placeholder="Email"></span>
              {errors.email && <p className="error-message">{errors.email}</p>}
            </div>

            {/* Campo de Senha - Versão para edição/visualização */}
            {mode != 'create' && !loginData.showPasswordFields ? (
              <div className="wrap-input100 validate-input">
                <input
                  autoComplete="off"
                  className="input100 has-val"
                  type="text"
                  value={'*'.repeat(loginData.passwordLength)}
                  readOnly
                  disabled
                />
                <span
                  className="focus-input100"
                  data-placeholder="Senha"
                ></span>
                {!isViewMode && (
                  <button
                    type="button"
                    className="btn-change-password"
                    onClick={handleChangePasswordClick}
                    disabled={loading.changePassword}
                  >
                    {loading.changePassword ? 'Carregando...' : 'Alterar Senha'}
                  </button>
                )}
              </div>
            ) : (
              <>
                {/* Campo de Nova Senha */}
                <div
                  className="wrap-input100 validate-input"
                  data-validate="Senha obrigatória"
                >
                  <input
                    className={`input100 ${
                      loginData.password ? 'has-val' : ''
                    }`}
                    autoComplete="off"
                    name="password"
                    type="password"
                    value={loginData.password}
                    onChange={handleLoginChange}
                    onBlur={handleBlur}
                    required={mode == 'create'}
                    disabled={isViewMode}
                  />
                  <span
                    className="focus-input100"
                    data-placeholder={mode == 'create' ? 'Senha' : 'Nova Senha'}
                  ></span>
                  {errors.password && (
                    <p className="error-message">{errors.password}</p>
                  )}
                </div>

                {/* Campo de Confirmação */}
                <div
                  className="wrap-input100 validate-input"
                  data-validate="Confirme a senha"
                >
                  <input
                    className={`input100 ${
                      loginData.confirmPassword ? 'has-val' : ''
                    }`}
                    autoComplete="off"
                    name="confirmPassword"
                    type="password"
                    value={loginData.confirmPassword}
                    onChange={handleLoginChange}
                    onBlur={handleBlur}
                    required={mode == 'create'}
                    disabled={isViewMode}
                  />
                  <span
                    className="focus-input100"
                    data-placeholder={
                      mode == 'create'
                        ? 'Confirmar Senha'
                        : 'Confirmar Nova Senha'
                    }
                  ></span>
                  {errors.confirmPassword && (
                    <p className="error-message">{errors.confirmPassword}</p>
                  )}
                </div>

                {/* Botão para cancelar alteração de senha */}
                {mode != 'create' && loginData.showPasswordFields && (
                  <button
                    type="button"
                    className="btn-cancel-password"
                    onClick={handleCancelPasswordChange}
                    disabled={loading.changePassword}
                  >
                    Cancelar Alteração
                  </button>
                )}
              </>
            )}

            {/* Campos do Usuário */}
            <div
              className="wrap-input100 validate-input"
              data-validate="Nome obrigatório"
            >
              <input
                className={`input100 ${userData.name ? 'has-val' : ''}`}
                name="name"
                type="text"
                value={userData.name}
                onChange={handleUserChange}
                onBlur={handleBlur}
                required
                disabled={isViewMode}
              />
              <span className="focus-input100" data-placeholder="Nome"></span>
              {errors.name && <p className="error-message">{errors.name}</p>}
            </div>

            <div
              className="wrap-input100 validate-input"
              data-validate="Endereço obrigatório"
            >
              <input
                className={`input100 ${userData.address ? 'has-val' : ''}`}
                name="address"
                type="text"
                value={userData.address}
                onChange={handleUserChange}
                onBlur={handleBlur}
                required
                disabled={isViewMode}
              />
              <span
                className="focus-input100"
                data-placeholder="Endereço"
              ></span>
              {errors.address && (
                <p className="error-message">{errors.address}</p>
              )}
            </div>

            <div
              className="wrap-input100 validate-input"
              data-validate="CPF/CNPJ obrigatório"
            >
              <input
                className={`input100 ${
                  userData.registrationNumber ? 'has-val' : ''
                }`}
                name="registrationNumber"
                type="text"
                value={userData.registrationNumber}
                onChange={handleUserChange}
                onBlur={handleBlur}
                required
                disabled={isViewMode}
              />
              <span
                className="focus-input100"
                data-placeholder="CPF ou CNPJ"
              ></span>
              {errors.registration && (
                <p className="error-message">{errors.registration}</p>
              )}
            </div>

            <div
              className="wrap-input100 validate-input"
              data-validate="Telefone obrigatório"
            >
              <input
                className={`input100 ${userData.phone1 ? 'has-val' : ''}`}
                name="phone1"
                type="text"
                value={userData.phone1}
                onChange={handleUserChange}
                onBlur={handleBlur}
                required
                disabled={isViewMode}
              />
              <span
                className="focus-input100"
                data-placeholder="Telefone"
              ></span>
              {errors.phone1 && (
                <p className="error-message">{errors.phone1}</p>
              )}
            </div>

            <div
              className="wrap-input100 validate-input"
              data-validate="Tipo de usuário obrigatório"
            >
              <label htmlFor="user-type" className="input-label">
                Tipo de Usuário
              </label>
              <select
                className={`input100 ${userData.userTypeId ? 'has-val' : ''}`}
                name="userTypeId"
                value={userData.userTypeId}
                onChange={handleUserChange}
                onBlur={handleBlur}
                required
                disabled={isViewMode}
              >
                <option value="">Selecione</option>
                {filteredUserTypes.map((ut: any) => (
                  <option key={ut.id} value={ut.id}>
                    {ut.description}
                  </option>
                ))}
              </select>
              <span
                className="focus-input100"
                data-placeholder="Tipo de Usuário"
              ></span>
            </div>

            {/* Campos de Condomínio, Torre, Andar e Apartamento */}
            {canManageAllCondominiums() && !isSysAdmin && (
              <div className="wrap-input100 validate-input">
                <label htmlFor="condominium" className="input-label">
                  Condomínio
                </label>
                <select
                  className={`input100 ${
                    userData.condominiumId ? 'has-val' : ''
                  }`}
                  name="condominiumId"
                  value={userData.condominiumId}
                  onChange={handleUserChange}
                  required
                  disabled={isViewMode}
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

            {!canManageAllCondominiums() && currentUserCondominium && (
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

            {isApartmentOwner && (
              <>
                <div
                  className="wrap-input100 validate-input"
                  data-validate="Torre obrigatória"
                >
                  <label htmlFor="tower" className="input-label">
                    Torre
                  </label>
                  <select
                    className={`input100 ${userData.towerId ? 'has-val' : ''}`}
                    name="towerId"
                    value={userData.towerId}
                    onChange={handleUserChange}
                    onBlur={handleBlur}
                    required={isApartmentOwner}
                    disabled={
                      !userData.condominiumId ||
                      userData.condominiumId == 0 ||
                      isViewMode
                    }
                  >
                    <option value="">Selecione</option>
                    {towers.map((tower: any) => (
                      <option key={tower.id} value={tower.id}>
                        {tower.number} - {tower.name}
                      </option>
                    ))}
                  </select>
                  <span
                    className="focus-input100"
                    data-placeholder="Torre"
                  ></span>
                </div>

                <div
                  className="wrap-input100 validate-input"
                  data-validate="Andar obrigatório"
                >
                  <input
                    className={`input100 ${userData.floorId ? 'has-val' : ''}`}
                    name="floorId"
                    type="number"
                    value={userData.floorId}
                    onChange={handleUserChange}
                    onBlur={handleBlur}
                    required={isApartmentOwner}
                  />
                  <span
                    className="focus-input100 focus-number"
                    data-placeholder="Andar"
                  ></span>
                </div>

                <div
                  className="wrap-input100 validate-input"
                  data-validate="Apartamento obrigatório"
                >
                  <input
                    className={`input100 ${
                      userData.apartment ? 'has-val' : ''
                    }`}
                    name="apartment"
                    type="number"
                    value={userData.apartment}
                    onChange={handleUserChange}
                    onBlur={handleBlur}
                    required={isApartmentOwner}
                  />
                  <span
                    className="focus-input100 focus-number"
                    data-placeholder="Apartamento"
                  ></span>
                </div>

                <div
                  className="wrap-input100 validate-input"
                  data-validate="Numero da vaga obrigatório"
                >
                  <input
                    className={`input100 ${
                      userData.parkingSpaceNumber ? 'has-val' : ''
                    }`}
                    name="parkingSpaceNumber"
                    type="number"
                    value={userData.parkingSpaceNumber}
                    onChange={handleUserChange}
                    onBlur={handleBlur}
                    required={isApartmentOwner}
                  />
                  <span
                    className="focus-input100 focus-number"
                    data-placeholder="Vaga Garagem"
                  ></span>
                </div>
              </>
            )}

            {(mode == 'create' || mode == 'edit') && (
              <div className="container-btn100-form-btn">
                <div className="wrap-btn100-form-btn">
                  <div className="btn100-form-bgbtn"></div>
                  <button
                    type="submit"
                    className="btn100-form-btn"
                    disabled={loading.submit || loading.update}
                  >
                    {loading.submit || loading.update
                      ? 'Carregando...'
                      : mode == 'create'
                      ? 'Cadastrar'
                      : 'Atualizar'}
                  </button>
                </div>
              </div>
            )}

            {mode == 'edit' && (
              <div
                className="container-btn100-form-btn"
                style={{ marginTop: '20px' }}
              >
                <div
                  className="wrap-btn100-form-btn"
                  style={{ backgroundColor: '#ff4d4d' }}
                >
                  <div className="btn100-form-bgbtn"></div>
                  <button
                    type="button"
                    className="btn100-form-btn"
                    onClick={() => setIsDeleteModalOpen(true)}
                    disabled={loading.delete}
                  >
                    {loading.delete ? 'Deletando...' : 'Deletar'}
                  </button>
                </div>
              </div>
            )}

            <div className="text-center p-t-20">
              <span
                className="txt1"
                style={{ cursor: 'pointer' }}
                onClick={handleGoToDashboard}
              >
                Voltar ao Dashboard
              </span>
            </div>
          </form>

          {message && (
            <div className={`message ${message.type} text-center`}>
              {message.text}
            </div>
          )}
        </div>
      </div>

      <DeleteConfirmationModal
        isOpen={isDeleteModalOpen}
        onClose={() => setIsDeleteModalOpen(false)}
        onConfirm={handleDelete}
        isLoading={loading.delete}
      />
    </div>
  );
};

export default UserForm;
