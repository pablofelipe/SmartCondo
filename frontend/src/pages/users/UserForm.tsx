/* eslint-disable eqeqeq */
import React, { useState, useEffect } from 'react';
import { useNavigate } from 'react-router-dom';
import './userForm.module.css';
import '../../styles/util.css';
import { usePermissions } from '../hooks/usePermissions';
import { DeleteConfirmationModal } from '../../utils/DeleteConfirmationModal';
import {
  getUser,
  createUser,
  updateUser,
  deleteUser,
  getUserTypes,
  UserType,
} from '../../services/userService';
import {
  getCondominiums,
  getCondominium,
  Condominium,
  CondominiumDetail,
} from '../../services/condominiumService';
import { getTowersByCondominium, Tower } from '../../services/towerService';
import {
  UserFormMode,
  UserData,
  LoginData,
  UserFormDTO,
  UserFormErrors,
} from './UserForm.types';
import { UserLoginFields } from './UserLoginFields';
import { UserProfileFields } from './UserProfileFields';
import { UserLocationFields } from './UserLocationFields';

interface UserFormProps {
  mode?: UserFormMode;
  userId?: number;
}

const UserForm = ({ mode = 'create', userId }: UserFormProps) => {
  const navigate = useNavigate();
  const isViewMode = mode == 'view';

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

  const [loginData, setLoginData] = useState<LoginData>({
    email: '',
    password: '',
    confirmPassword: '',
    expiration: '2100-12-31',
    enabled: true,
    keyId: '',
    showPasswordFields: false,
    passwordLength: 0,
  });

  const [message, setMessage] = useState<{
    text: string;
    type: 'success' | 'error';
  } | null>(null);

  const [condominiums, setCondominiums] = useState<Condominium[]>([]);
  const [currentUserCondominium, setCurrentUserCondominium] =
    useState<CondominiumDetail | null>(null);
  const [userTypes, setUserTypes] = useState<UserType[]>([]);
  const [towers, setTowers] = useState<Tower[]>([]);
  const [errors, setErrors] = useState<UserFormErrors>({
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
  const filteredUserTypes = userTypes.filter((ut) =>
    canRegisterUser(ut.name),
  );

  useEffect(() => {
    const fetchUserData = async () => {
      try {
        const data = await getUser(userId!);

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
        const userString = localStorage.getItem('user');
        if (!userString) return;

        const user = JSON.parse(userString) as {
          condominiumId: number;
          role: string;
        };

        if (user.role == 'SystemAdministrator') {
          const data = await getCondominiums();
          setCondominiums(data);
        } else if (user.condominiumId) {
          const data = await getCondominium(user.condominiumId);
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
        const data = await getUserTypes();
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

        const data = await getTowersByCondominium(condominiumId);
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
      await deleteUser(userId!);
      setMessage({ text: 'User deleted successfully', type: 'success' });
      navigate('/users');
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

    return { userFormDTO };
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
      const { userFormDTO } = await prepareFormData();

      const data = await createUser(userFormDTO);
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
      const { userFormDTO } = await prepareFormData();

      const data = await updateUser(userId, userFormDTO);
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
            <UserLoginFields
              mode={mode}
              isViewMode={isViewMode}
              loginData={loginData}
              errors={errors}
              isChangingPassword={loading.changePassword}
              onChange={handleLoginChange}
              onBlur={handleBlur}
              onChangePasswordClick={handleChangePasswordClick}
              onCancelPasswordChange={handleCancelPasswordChange}
            />

            {/* User fields */}
            <UserProfileFields
              isViewMode={isViewMode}
              userData={userData}
              errors={errors}
              filteredUserTypes={filteredUserTypes}
              onChange={handleUserChange}
              onBlur={handleBlur}
            />

            {/* Condominium, Tower, Floor and Apartment fields */}
            <UserLocationFields
              isViewMode={isViewMode}
              canManageAllCondominiums={canManageAllCondominiums()}
              isSysAdmin={isSysAdmin}
              isApartmentOwner={isApartmentOwner}
              userData={userData}
              condominiums={condominiums}
              currentUserCondominium={currentUserCondominium}
              towers={towers}
              onChange={handleUserChange}
              onBlur={handleBlur}
            />

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
