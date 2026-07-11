/* eslint-disable eqeqeq */
import React, { useState } from 'react';
import { useQuery } from '@apollo/client';
import { usePermissions } from '../hooks/usePermissions';
import { Link, useNavigate } from 'react-router-dom';
import './vehicleList.module.css';
import { GET_VEHICLES } from './vehicleQueries';

interface SearchTerms {
  licensePlate?: string | null;
  model?: string | null;
  apartmentNumber?: number | null;
  parkingSpaceNumber?: number | null;
  ownerName?: string | null;
  cpfCnpj?: string | null;
}

const VehicleListPage: React.FC = () => {
  const navigate = useNavigate();
  const [searchTerms, setSearchTerms] = useState<SearchTerms>({
    apartmentNumber: null,
    parkingSpaceNumber: null,
  });
  const [vehicles, setVehicles] = useState<any[]>([]);
  const [errorMessage, setErrorMessage] = useState<string | null>(null);
  const [hasSearched, setHasSearched] = useState(false);
  const { canEditVehicles, canViewVehicles } = usePermissions();

  const { loading, error, refetch } = useQuery(GET_VEHICLES, {
    variables: {
      filter: {
        licensePlate: searchTerms.licensePlate || null,
        model: searchTerms.model || null,
        apartmentNumber: searchTerms.apartmentNumber || null,
        parkingSpaceNumber: searchTerms.parkingSpaceNumber || null,
        ownerName: searchTerms.ownerName || null,
        registrationNumber: searchTerms.cpfCnpj || null,
      },
    },
    fetchPolicy: 'network-only',
    skip: !hasSearched,
  });

  const handleInputChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    const { name, value } = e.target;
    setHasSearched(false);
    //setSearchTerms((prev) => ({ ...prev, [name]: value }));
    if (name == 'apartmentNumber' || name == 'parkingSpaceNumber') {
      setSearchTerms((prev) => ({
        ...prev,
        [name]: value == '' ? null : Number(value),
      }));
    } else {
      setSearchTerms((prev) => ({ ...prev, [name]: value || null }));
    }
  };

  const handleSearch = async (e: React.FormEvent) => {
    e.preventDefault();
    setErrorMessage(null);

    const hasAtLeastOneFilter = Object.entries(searchTerms).some(
      ([key, value]) => {
        if (value == null) return false;
        return (
          typeof value == 'number' ||
          (typeof value == 'string' && value.trim() != '')
        );
      },
    );

    if (!hasAtLeastOneFilter) {
      setErrorMessage('Por favor, preencha pelo menos um campo de busca');
      return;
    }

    setHasSearched(true);
    try {
      const result = await refetch();
      if (result.data) {
        setVehicles(result.data.vehicles || []);
      }
    } catch (err: any) {
      if (err.message.includes('Nenhum parâmetro para veículo recebido')) {
        setErrorMessage('Por favor, preencha pelo menos um campo de busca');
      } else {
        setErrorMessage(`Erro na busca: ${err.message}`);
      }
    }
  };
  /*
  const handleReset = () => {
    setSearchTerms({});
    setHasSearched(false);
    setVehicles([]);
  };
*/

  const handleGoToDashboard = () => {
    navigate('/dashboard');
  };

  return (
    <div className="limiter">
      <div className="container-main">
        <div className="wrap-main">
          <div className="form-header">
            <h1 className="main-form-title">Busca de Veículos</h1>

            <form onSubmit={handleSearch} className="search-form">
              <div className="search-fields">
                <div className="wrap-input100 validate-input">
                  <input
                    className={`input100 ${
                      searchTerms.licensePlate ? 'has-val' : ''
                    }`}
                    type="text"
                    name="licensePlate"
                    value={searchTerms.licensePlate || ''}
                    onChange={handleInputChange}
                  />
                  <span
                    className="focus-input100"
                    data-placeholder="Placa"
                  ></span>
                </div>

                <div className="wrap-input100 validate-input">
                  <input
                    className={`input100 ${searchTerms.model ? 'has-val' : ''}`}
                    type="text"
                    name="model"
                    value={searchTerms.model || ''}
                    onChange={handleInputChange}
                  />
                  <span
                    className="focus-input100"
                    data-placeholder="Modelo"
                  ></span>
                </div>

                <div className="wrap-input100 validate-input">
                  <input
                    className={`input100 ${
                      searchTerms.apartmentNumber ? 'has-val' : ''
                    }`}
                    type="number"
                    name="apartmentNumber"
                    value={searchTerms.apartmentNumber || ''}
                    onChange={handleInputChange}
                  />
                  <span
                    className="focus-input100"
                    data-placeholder="Apartamento"
                  ></span>
                </div>

                <div className="wrap-input100 validate-input">
                  <input
                    className={`input100 ${
                      searchTerms.parkingSpaceNumber ? 'has-val' : ''
                    }`}
                    type="number"
                    name="parkingSpaceNumber"
                    value={searchTerms.parkingSpaceNumber || ''}
                    onChange={handleInputChange}
                  />
                  <span
                    className="focus-input100"
                    data-placeholder="Vaga"
                  ></span>
                </div>

                <div className="wrap-input100 validate-input">
                  <input
                    className={`input100 ${
                      searchTerms.ownerName ? 'has-val' : ''
                    }`}
                    type="text"
                    name="ownerName"
                    value={searchTerms.ownerName || ''}
                    onChange={handleInputChange}
                  />
                  <span
                    className="focus-input100"
                    data-placeholder="Nome do Condômino"
                  ></span>
                </div>

                <div className="wrap-input100 validate-input">
                  <input
                    className={`input100 ${
                      searchTerms.cpfCnpj ? 'has-val' : ''
                    }`}
                    type="text"
                    name="cpfCnpj"
                    value={searchTerms.cpfCnpj || ''}
                    onChange={handleInputChange}
                  />
                  <span
                    className="focus-input100"
                    data-placeholder="CPF/CNPJ"
                  ></span>
                </div>

                <div className="container-btn100-form-btn">
                  <div className="wrap-btn100-form-btn">
                    <div className="btn100-form-bgbtn"></div>
                    <button
                      type="submit"
                      className="btn100-form-btn"
                      disabled={loading}
                    >
                      {loading ? 'Buscando...' : 'Buscar'}
                    </button>
                  </div>
                </div>

                {/*
                <div className="container-btn100-form-btn">
                  <div className="wrap-btn100-form-btn">
                    <div className="btn100-form-bgbtn"></div>
                    <button
                      type="button"
                      className="btn100-form-btn"
                      onClick={handleReset}
                      disabled={loading}
                    >
                      Limpar
                    </button>
                  </div>
                </div>
              */}
              </div>
            </form>

            {errorMessage && (
              <div className="error-message">{errorMessage}</div>
            )}

            {hasSearched && !error && (
              <div className="table-responsive-wrapper">
                {vehicles.length > 0 ? (
                  <table className="user-table">
                    <thead>
                      <tr>
                        <th>Placa</th>
                        <th>Tipo</th>
                        <th>Modelo</th>
                        <th>Condômino</th>
                        <th>Apartamento</th>
                        <th>Visualizar</th>
                        <th>Editar</th>
                      </tr>
                    </thead>
                    <tbody>
                      {vehicles.map((vehicle: any) => (
                        <tr key={vehicle.id}>
                          <td>{vehicle.licensePlate}</td>
                          <td>
                            {vehicle.type === 'Car'
                              ? 'Carro'
                              : vehicle.type === 'Motorcycle'
                              ? 'Moto'
                              : vehicle.type === 'Truck'
                              ? 'Caminhão'
                              : vehicle.type}
                          </td>
                          <td>{vehicle.model}</td>
                          <td>{vehicle.user.name}</td>
                          <td>{vehicle.user.apartment}</td>
                          {/*
                          <td>{vehicle.enabled ? 'Ativo' : 'Inativo'}</td>
                          */}

                          <td>
                            {canViewVehicles && (
                              <Link
                                to={`/vehicles/${vehicle.id}/view`}
                                className="view-link"
                              >
                                Visualizar
                              </Link>
                            )}
                          </td>
                          <td>
                            {canEditVehicles && (
                              <Link
                                to={`/vehicles/${vehicle.id}/edit`}
                                className="edit-link"
                              >
                                Editar
                              </Link>
                            )}
                          </td>
                        </tr>
                      ))}
                    </tbody>
                  </table>
                ) : (
                  <div className="no-results">
                    <p>Nenhum veículo encontrado</p>
                  </div>
                )}
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
          </div>
        </div>
      </div>
    </div>
  );
};

export default VehicleListPage;
