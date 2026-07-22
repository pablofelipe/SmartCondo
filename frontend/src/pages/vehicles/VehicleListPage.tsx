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
      setErrorMessage('Please fill in at least one search field');
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
        setErrorMessage('Please fill in at least one search field');
      } else {
        setErrorMessage(`Search error: ${err.message}`);
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
            <h1 className="main-form-title">Vehicle Search</h1>

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
                    data-placeholder="License Plate"
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
                    data-placeholder="Model"
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
                    data-placeholder="Apartment"
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
                    data-placeholder="Parking Space"
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
                    data-placeholder="Owner Name"
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
                    data-placeholder="Registration number"
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
                      {loading ? 'Searching...' : 'Search'}
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
                      Clear
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
                        <th>License Plate</th>
                        <th>Type</th>
                        <th>Model</th>
                        <th>Owner</th>
                        <th>Apartment</th>
                        <th>View</th>
                        <th>Edit</th>
                      </tr>
                    </thead>
                    <tbody>
                      {vehicles.map((vehicle: any) => (
                        <tr key={vehicle.id}>
                          <td>{vehicle.licensePlate}</td>
                          <td>
                            {vehicle.type === 'Car'
                              ? 'Car'
                              : vehicle.type === 'Motorcycle'
                              ? 'Motorcycle'
                              : vehicle.type === 'Truck'
                              ? 'Truck'
                              : vehicle.type}
                          </td>
                          <td>{vehicle.model}</td>
                          <td>{vehicle.user.name}</td>
                          <td>{vehicle.user.apartment}</td>
                          {/*
                          <td>{vehicle.enabled ? 'Active' : 'Inactive'}</td>
                          */}

                          <td>
                            {canViewVehicles && (
                              <Link
                                to={`/vehicles/${vehicle.id}/view`}
                                className="view-link"
                              >
                                View
                              </Link>
                            )}
                          </td>
                          <td>
                            {canEditVehicles && (
                              <Link
                                to={`/vehicles/${vehicle.id}/edit`}
                                className="edit-link"
                              >
                                Edit
                              </Link>
                            )}
                          </td>
                        </tr>
                      ))}
                    </tbody>
                  </table>
                ) : (
                  <div className="no-results">
                    <p>No vehicles found</p>
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
                Back to Dashboard
              </span>
            </div>
          </div>
        </div>
      </div>
    </div>
  );
};

export default VehicleListPage;
