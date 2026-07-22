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

  // Loading state
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
          console.error('Authentication token not found');
          return;
        }

        const response = await fetch(`${config.apiUrl}/UserProfile/${userId}`, {
          method: 'GET',
          headers: headers,
        });

        console.log(`response: ${JSON.stringify(response)}`);

        if (!response.ok) {
          throw new Error(`Failed to load user: ${response.statusText}`);
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
        console.error('Error loading user:', error);
        setMessage({
          text: 'Failed to load user data',
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
        console.error('Error loading condominiums:', error);
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
        console.error('Error loading user types:', error);
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

        if (!response.ok) throw new Error('API response error');

        const data = await response.json();
        setTowers(data);
      } catch (error) {
        console.error('Error loading towers:', error);
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
          newErrors.email = 'Invalid email';
        } else {
          newErrors.email = '';
        }
        break;

      case 'registrationNumber':
        value = removeNonNumber(value);
        if (!validateCPF(value) && !validateCNPJ(value)) {
          newErrors.registration =
            'The registration number must be 11 or 14 digits';
        } else {
          newErrors.registration = '';
        }
        break;

      case 'phone1':
        value = removeNonNumber(value);
        if (!validatePhone(value)) {
          newErrors.phone1 = 'Phone number must have at least 9 digits';
        } else {
          newErrors.phone1 = '';
        }
        break;

      case 'password':
        if (!validatePassword(value)) {
          newErrors.password =
            'Password must have at least 6 characters, 1 uppercase letter, 1 lowercase letter, 1 number and 1 special character.';
        } else {
          newErrors.password = '';
        }
        break;

      case 'confirmPassword':
        if (value != loginData.password) {
          newErrors.confirmPassword = 'Passwords do not match.';
        } else {
          newErrors.confirmPassword = '';
        }
        break;

      case 'name':
        if (!value || value.length < 3) {
          newErrors.name = `Name is required and must have at least 3 characters`;
        } else {
          newErrors.name = '';
        }
        break;

      case 'address':
        if (!value || value.length < 3) {
          newErrors.address = `Address is required and must have at least 3 characters`;
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
    // Already loading, do nothing
    if (loading.delete) return;

    setLoading((prev) => ({ ...prev, delete: true }));

    try {
      const response = await fetch(`${config.apiUrl}/UserProfile/${userId}`, {
        method: 'DELETE',
        headers: getAuthHeaders(),
      });

      if (response.ok) {
        setMessage({ text: 'User deleted successfully', type: 'success' });
        navigate('/users');
      }
    } catch (error) {
      setMessage({ text: 'Failed to delete user', type: 'error' });
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
        text: 'Fix the errors before submitting the form!',
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
      errorMessage = errorData.message || 'Unknown error';
    }

    switch (response.status) {
      case 400:
        throw new Error(errorMessage);
      case 401:
        throw new Error(errorMessage);
      case 500:
        throw new Error(
          errorMessage || 'Server error. Please try again later.',
        );
      default:
        throw new Error('Unexpected error.');
    }
  };

  const handleGenericError = (err: unknown): string => {
    if (err instanceof TypeError && err.message == 'Failed to fetch') {
      return 'Could not connect to the server. Check your connection.';
    }
    if (err instanceof Error) {
      return err.message;
    }
    return 'Unexpected error.';
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();

    // Already loading, do nothing
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
      console.error('Failed to register user:', error);
      setMessage({ text: error, type: 'error' });
    } finally {
      setLoading((prev) => ({ ...prev, submit: false }));
    }
  };

  const handleUpdate = async (e: React.FormEvent) => {
    e.preventDefault();

    // Already loading, do nothing
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
      console.error('Failed to update user:', error);
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
            <span className="main-form-title">User Registration</span>

            {/* Login fields */}
            <div
              className="wrap-input100 validate-input"
              data-validate="Valid e-mail: a@b.c"
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

            {/* Password field - edit/view version */}
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
                  data-placeholder="Password"
                ></span>
                {!isViewMode && (
                  <button
                    type="button"
                    className="btn-change-password"
                    onClick={handleChangePasswordClick}
                    disabled={loading.changePassword}
                  >
                    {loading.changePassword ? 'Loading...' : 'Change Password'}
                  </button>
                )}
              </div>
            ) : (
              <>
                {/* New password field */}
                <div
                  className="wrap-input100 validate-input"
                  data-validate="Password is required"
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
                    data-placeholder={mode == 'create' ? 'Password' : 'New Password'}
                  ></span>
                  {errors.password && (
                    <p className="error-message">{errors.password}</p>
                  )}
                </div>

                {/* Confirmation field */}
                <div
                  className="wrap-input100 validate-input"
                  data-validate="Confirm the password"
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
                        ? 'Confirm Password'
                        : 'Confirm New Password'
                    }
                  ></span>
                  {errors.confirmPassword && (
                    <p className="error-message">{errors.confirmPassword}</p>
                  )}
                </div>

                {/* Cancel password change button */}
                {mode != 'create' && loginData.showPasswordFields && (
                  <button
                    type="button"
                    className="btn-cancel-password"
                    onClick={handleCancelPasswordChange}
                    disabled={loading.changePassword}
                  >
                    Cancel Change
                  </button>
                )}
              </>
            )}

            {/* User fields */}
            <div
              className="wrap-input100 validate-input"
              data-validate="Name is required"
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
              <span className="focus-input100" data-placeholder="Name"></span>
              {errors.name && <p className="error-message">{errors.name}</p>}
            </div>

            <div
              className="wrap-input100 validate-input"
              data-validate="Address is required"
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
                data-placeholder="Address"
              ></span>
              {errors.address && (
                <p className="error-message">{errors.address}</p>
              )}
            </div>

            <div
              className="wrap-input100 validate-input"
              data-validate="Registration number is required"
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
                data-placeholder="Registration number"
              ></span>
              {errors.registration && (
                <p className="error-message">{errors.registration}</p>
              )}
            </div>

            <div
              className="wrap-input100 validate-input"
              data-validate="Phone number is required"
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
                data-placeholder="Phone"
              ></span>
              {errors.phone1 && (
                <p className="error-message">{errors.phone1}</p>
              )}
            </div>

            <div
              className="wrap-input100 validate-input"
              data-validate="User type is required"
            >
              <label htmlFor="user-type" className="input-label">
                User Type
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
                <option value="">Select</option>
                {filteredUserTypes.map((ut: any) => (
                  <option key={ut.id} value={ut.id}>
                    {ut.description}
                  </option>
                ))}
              </select>
              <span
                className="focus-input100"
                data-placeholder="User Type"
              ></span>
            </div>

            {/* Condominium, Tower, Floor and Apartment fields */}
            {canManageAllCondominiums() && !isSysAdmin && (
              <div className="wrap-input100 validate-input">
                <label htmlFor="condominium" className="input-label">
                  Condominium
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
                  <option value="">Select</option>
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
                <label className="input-label">Condominium</label>
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
                  data-validate="Tower is required"
                >
                  <label htmlFor="tower" className="input-label">
                    Tower
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
                    <option value="">Select</option>
                    {towers.map((tower: any) => (
                      <option key={tower.id} value={tower.id}>
                        {tower.number} - {tower.name}
                      </option>
                    ))}
                  </select>
                  <span
                    className="focus-input100"
                    data-placeholder="Tower"
                  ></span>
                </div>

                <div
                  className="wrap-input100 validate-input"
                  data-validate="Floor is required"
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
                    data-placeholder="Floor"
                  ></span>
                </div>

                <div
                  className="wrap-input100 validate-input"
                  data-validate="Apartment is required"
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
                    data-placeholder="Apartment"
                  ></span>
                </div>

                <div
                  className="wrap-input100 validate-input"
                  data-validate="Parking space number is required"
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
                    data-placeholder="Parking Space"
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
                      ? 'Loading...'
                      : mode == 'create'
                      ? 'Register'
                      : 'Update'}
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
                    {loading.delete ? 'Deleting...' : 'Delete'}
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
                Back to Dashboard
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
