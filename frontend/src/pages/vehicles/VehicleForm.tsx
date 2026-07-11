/* eslint-disable eqeqeq */
import React, { useState } from 'react';
import { useNavigate, Link } from 'react-router-dom';
import { useQuery, useMutation } from '@apollo/client';
import {
  GET_VEHICLE,
  GET_USER_PROFILE,
  CREATE_VEHICLE,
  UPDATE_VEHICLE,
  DELETE_VEHICLE,
} from './vehicleQueries';
import './vehicleForm.module.css';
import '../../styles/util.css';
import { DeleteConfirmationModal } from '../../utils/DeleteConfirmationModal';

type VehicleFormMode = 'create' | 'edit' | 'view';

interface VehicleFormProps {
  mode?: VehicleFormMode;
  vehicleId?: number;
  userId?: number;
}

enum VehicleType {
  Car = 'CAR',
  Motorcycle = 'MOTORCYCLE',
  Truck = 'TRUCK',
  Other = 'OTHER',
}

const VehicleForm = ({
  mode = 'create',
  vehicleId,
  userId,
}: VehicleFormProps) => {
  const navigate = useNavigate();
  const isViewMode = mode == 'view';
  const [ownerUserId, setOwnerUserId] = useState<number | null>(null);

  // Definindo o tipo de dados do veículo
  interface VehicleData {
    id?: string;
    type: VehicleType;
    licensePlate: string;
    brand: string;
    model: string;
    color: string;
    enabled: boolean;
    userId?: number;
  }

  const [vehicleData, setVehicleData] = useState<VehicleData>({
    type: VehicleType.Car,
    licensePlate: '',
    brand: '',
    model: '',
    color: '',
    enabled: true,
    userId: userId || 0,
  });

  // Estados de loading
  const [loading, setLoading] = useState({
    submit: false,
    update: false,
    delete: false,
  });

  const [message, setMessage] = useState<{
    text: string;
    type: 'success' | 'error';
  } | null>(null);

  const [errors, setErrors] = useState({
    licensePlate: '',
    brand: '',
    model: '',
    color: '',
  });

  const [isDeleteModalOpen, setIsDeleteModalOpen] = useState(false);
  const [ownerDetails, setOwnerDetails] = useState<{
    name: string;
    id: string;
  } | null>(null);

  const handleGoToDashboard = () => {
    navigate('/dashboard');
  };

  const handleGoBack = () => {
    navigate(ownerUserId ? `/users/${ownerUserId}/view` : '/vehicles');
  };

  // Consultas GraphQL
  const { loading: vehicleLoading } = useQuery(GET_VEHICLE, {
    variables: { id: String(vehicleId) },
    skip: mode == 'create' || !vehicleId,
    onCompleted: (data) => {
      if (data?.vehicle) {
        setVehicleData({
          ...data.vehicle,
          type: data.vehicle.type || VehicleType.Car,
        });

        setOwnerUserId(Number(data.vehicle.user?.id));
      }
    },
    onError: (error) => {
      console.error('Erro GET_VEHICLE:', JSON.stringify(error, null, 2));
      setMessage({
        text: `Erro ao carregar veículo: ${error.message}`,
        type: 'error',
      });
    },
  });

  const { loading: ownerLoading } = useQuery(GET_USER_PROFILE, {
    variables: { id: ownerUserId },
    skip: !ownerUserId,
    onCompleted: (data) => {
      if (data?.user) {
        setOwnerDetails(data.user);
      }
    },
  });

  // Mutations GraphQL
  const [createVehicle] = useMutation(CREATE_VEHICLE, {
    onCompleted: () => {
      setMessage({ text: 'Veículo criado com sucesso', type: 'success' });
      handleGoToDashboard();
    },
    onError: (error) => {
      console.error('Erro CREATE_VEHICLE:', JSON.stringify(error, null, 2));
      setMessage({
        text: `Erro ao criar veículo: ${error.message}`,
        type: 'error',
      });
    },
  });

  const [updateVehicle] = useMutation(UPDATE_VEHICLE, {
    onCompleted: () => {
      setMessage({ text: 'Veículo atualizado com sucesso', type: 'success' });
    },
    onError: (error) => {
      console.error('Erro UPDATE_VEHICLE:', JSON.stringify(error, null, 2));
      setMessage({
        text: `Erro ao atualizar veículo: ${error.message}`,
        type: 'error',
      });
    },
  });

  const [deleteVehicle] = useMutation(DELETE_VEHICLE, {
    onCompleted: () => {
      setMessage({ text: 'Veículo deletado com sucesso', type: 'success' });
      handleGoToDashboard();
    },
    onError: (error) => {
      console.error('Erro DELETE_VEHICLE:', JSON.stringify(error, null, 2));
      setMessage({
        text: `Erro ao deletar veículo: ${error.message}`,
        type: 'error',
      });
    },
  });

  const handleVehicleChange = (
    e: React.ChangeEvent<HTMLInputElement | HTMLSelectElement>,
  ) => {
    const { name, value, type } = e.target;
    const checked =
      type == 'checkbox'
        ? (e as React.ChangeEvent<HTMLInputElement>).target.checked
        : undefined;

    setVehicleData((prev) => ({
      ...prev,
      [name]: type == 'checkbox' ? checked : value,
    }));
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
      case 'licensePlate':
        newErrors.licensePlate =
          !value || value.length < 6
            ? 'Placa deve ter pelo menos 6 caracteres'
            : '';
        break;
      case 'brand':
        newErrors.brand =
          !value || value.length < 2 ? 'Marca é obrigatória' : '';
        break;
      case 'model':
        newErrors.model =
          !value || value.length < 1 ? 'Modelo é obrigatório' : '';
        break;
      case 'color':
        newErrors.color = !value || value.length < 3 ? 'Cor é obrigatória' : '';
        break;
      default:
        break;
    }

    setErrors(newErrors);
  };

  const handleDelete = async () => {
    // Se já está carregando, não faz nada
    if (loading.delete || !vehicleId) return;

    setLoading((prev) => ({ ...prev, delete: true }));

    try {
      await deleteVehicle({ variables: { id: vehicleId } });
    } catch (error) {
      console.error('Erro ao deletar veículo:', error);
    } finally {
      setLoading((prev) => ({ ...prev, delete: false }));
      setIsDeleteModalOpen(false);
    }
  };

  const validateForm = (): boolean => {
    const requiredFields = [
      vehicleData.licensePlate,
      vehicleData.brand,
      vehicleData.model,
      vehicleData.color,
    ];

    const hasAllRequiredFields = requiredFields.every(
      (field) => field && field.trim() != '',
    );
    const hasNoErrors = Object.values(errors).every((error) => !error);

    return hasAllRequiredFields && hasNoErrors;
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();

    // Se já está carregando, não faz nada
    if (loading.submit) return;

    if (!validateForm()) return;

    setMessage(null);
    setLoading((prev) => ({ ...prev, submit: true }));

    const input = {
      ...vehicleData,
      userId: vehicleData.userId || userId,
    };

    try {
      await createVehicle({ variables: { input } });
      // Redirecionamento após sucesso
      navigate(userId ? `/users/${userId}/view` : '/vehicles');
    } catch (error) {
      console.error('Erro na cadastro de veiculos:', error);
      setMessage({
        text: 'Erro ao cadastrar veiculos',
        type: 'error',
      });
    } finally {
      setLoading((prev) => ({ ...prev, submit: false }));
    }
  };

  const handleUpdate = async (e: React.FormEvent) => {
    e.preventDefault();

    // Se já está carregando, não faz nada
    if (loading.update || !vehicleId) return;

    if (!validateForm()) return;

    setMessage(null);
    setLoading((prev) => ({ ...prev, update: true }));

    const input = {
      type: vehicleData.type,
      licensePlate: vehicleData.licensePlate,
      brand: vehicleData.brand,
      model: vehicleData.model,
      color: vehicleData.color,
      enabled: vehicleData.enabled,
      userId: ownerUserId,
    };

    try {
      await updateVehicle({ variables: { id: vehicleId, input } });
    } catch (error) {
      console.error('Erro ao atualizar veículo:', error);
    } finally {
      setLoading((prev) => ({ ...prev, update: false }));
    }
  };

  const vehicleTypeOptions = [
    { value: VehicleType.Car, label: 'Carro' },
    { value: VehicleType.Motorcycle, label: 'Moto' },
    { value: VehicleType.Truck, label: 'Caminhão' },
    { value: VehicleType.Other, label: 'Outro' },
  ];

  if (vehicleLoading || ownerLoading) {
    return <div>Carregando...</div>;
  }

  return (
    <div className="limiter">
      <div className="container-main">
        <div className="wrap-main">
          <form onSubmit={mode == 'create' ? handleSubmit : handleUpdate}>
            <span className="main-form-title">
              {mode == 'create'
                ? 'Cadastro de Veículo'
                : mode == 'edit'
                ? 'Editar Veículo'
                : 'Detalhes do Veículo'}
            </span>

            {/* Owner information (if available) */}
            {ownerDetails && (
              <div className="wrap-input100">
                <label className="input-label">Proprietário</label>
                <Link
                  to={`/users/${ownerDetails.id}/view`}
                  className="owner-link"
                >
                  {ownerDetails.name}
                </Link>
                <input type="hidden" name="userId" value={ownerDetails.id} />
              </div>
            )}

            {/* Vehicle Type */}
            <div className="wrap-input100 validate-input">
              <label htmlFor="type" className="input-label">
                Tipo de Veículo
              </label>
              <select
                className={`input100 ${vehicleData.type ? 'has-val' : ''}`}
                name="type"
                value={vehicleData.type}
                onChange={handleVehicleChange}
                disabled={isViewMode}
              >
                {vehicleTypeOptions.map((option) => (
                  <option key={option.value} value={option.value}>
                    {option.label}
                  </option>
                ))}
              </select>
            </div>

            {/* License Plate */}
            <div className="wrap-input100 validate-input">
              <input
                className={`input100 ${
                  vehicleData.licensePlate ? 'has-val' : ''
                }`}
                name="licensePlate"
                type="text"
                value={vehicleData.licensePlate}
                onChange={handleVehicleChange}
                onBlur={handleBlur}
                required
                disabled={isViewMode}
              />
              <span className="focus-input100" data-placeholder="Placa"></span>
              {errors.licensePlate && (
                <p className="error-message">{errors.licensePlate}</p>
              )}
            </div>

            {/* Brand */}
            <div className="wrap-input100 validate-input">
              <input
                className={`input100 ${vehicleData.brand ? 'has-val' : ''}`}
                name="brand"
                type="text"
                value={vehicleData.brand}
                onChange={handleVehicleChange}
                onBlur={handleBlur}
                required
                disabled={isViewMode}
              />
              <span className="focus-input100" data-placeholder="Marca"></span>
              {errors.brand && <p className="error-message">{errors.brand}</p>}
            </div>

            {/* Model */}
            <div className="wrap-input100 validate-input">
              <input
                className={`input100 ${vehicleData.model ? 'has-val' : ''}`}
                name="model"
                type="text"
                value={vehicleData.model}
                onChange={handleVehicleChange}
                onBlur={handleBlur}
                required
                disabled={isViewMode}
              />
              <span className="focus-input100" data-placeholder="Modelo"></span>
              {errors.model && <p className="error-message">{errors.model}</p>}
            </div>

            {/* Color */}
            <div className="wrap-input100 validate-input">
              <input
                className={`input100 ${vehicleData.color ? 'has-val' : ''}`}
                name="color"
                type="text"
                value={vehicleData.color}
                onChange={handleVehicleChange}
                onBlur={handleBlur}
                required
                disabled={isViewMode}
              />
              <span className="focus-input100" data-placeholder="Cor"></span>
              {errors.color && <p className="error-message">{errors.color}</p>}
            </div>

            {/* Enabled checkbox */}
            <div
              className="wrap-input100 validate-input"
              style={{ flexDirection: 'row', alignItems: 'center' }}
            >
              <input
                id="enabled"
                name="enabled"
                type="checkbox"
                checked={vehicleData.enabled}
                onChange={handleVehicleChange}
                disabled={isViewMode}
                style={{ width: 'auto', marginRight: '10px' }}
              />
              <label htmlFor="enabled" style={{ marginBottom: 0 }}>
                Ativo
              </label>
            </div>

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
                onClick={handleGoBack}
              >
                {ownerUserId
                  ? 'Voltar ao Proprietário'
                  : 'Voltar à Lista de Veículos'}
              </span>
            </div>
          </form>

          <div className="text-center p-t-20">
            <span
              className="txt1"
              style={{ cursor: 'pointer' }}
              onClick={handleGoToDashboard}
            >
              Voltar ao Dashboard
            </span>
          </div>

          {/* Exibir mensagem */}
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

export default VehicleForm;
