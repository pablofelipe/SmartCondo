import React, { useState, useEffect } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { usePermissions } from '../hooks/usePermissions';
import { Condominium, getCondominiums } from '../../services/condominiumService';
import {
  UserSearchResult,
  searchUsersInCondominium,
} from '../../services/userService';
import './userList.module.css';

const UserListPage = () => {
  const navigate = useNavigate();
  const [searchTerm, setSearchTerm] = useState({
    name: '',
    registrationNumber: '',
  });
  const [searchResults, setSearchResults] = useState<UserSearchResult[]>([]);
  const [isSearching, setIsSearching] = useState(false);
  const [hasSearched, setHasSearched] = useState(false);
  const [error, setError] = useState('');
  const [condominiums, setCondominiums] = useState<Condominium[]>([]);
  const [selectedCondominium, setSelectedCondominium] = useState<number | null>(
    null,
  );
  const { canEditUsers, canViewUsers, canManageAllCondominiums } =
    usePermissions();
  const isAdmin = canManageAllCondominiums();

  useEffect(() => {
    if (isAdmin) {
      fetchCondominiums();
    }
  }, [isAdmin]);

  const fetchCondominiums = async () => {
    try {
      const data = await getCondominiums();
      setCondominiums(data);
    } catch (error) {
      console.error('Error fetching condominiums:', error);
    }
  };

  const handleSearch = async (e: React.FormEvent) => {
    e.preventDefault();

    if (!searchTerm.name && !searchTerm.registrationNumber) {
      setError('Fill in at least one search field');
      return;
    }

    if (searchTerm.name && searchTerm.name.length < 3) {
      setError('The name must have at least 3 characters');
      return;
    }

    if (
      searchTerm.registrationNumber &&
      searchTerm.registrationNumber.length < 11
    ) {
      setError('The registration number must have at least 11 characters');
      return;
    }

    setHasSearched(true);
    setIsSearching(true);
    setError('');

    try {
      const params = new URLSearchParams();

      if (searchTerm.name) params.append('Name', searchTerm.name);
      if (searchTerm.registrationNumber)
        params.append('RegistrationNumber', searchTerm.registrationNumber);

      let condominiumId;
      if (isAdmin && selectedCondominium) {
        condominiumId = selectedCondominium;
      } else {
        const userString = localStorage.getItem('user');
        if (!userString) return;
        const user = JSON.parse(userString) as { condominiumId: number };
        condominiumId = user.condominiumId;
      }

      const data = await searchUsersInCondominium(condominiumId, params);
      setSearchResults(data);
    } catch (error) {
      console.error('Search error:', error);
      setError('An error occurred while searching');
    } finally {
      setIsSearching(false);
    }
  };

  const handleCreateNewUser = () => {
    navigate('/users/new');
  };

  return (
    <div className="limiter">
      <div className="container-main">
        <div className="wrap-main">
          <div className="form-header">
            <h1 className="main-form-title">User Search</h1>

            <form onSubmit={handleSearch} className="search-form">
              {isAdmin && (
                <div className="wrap-input100 validate-input">
                  <select
                    className="input100"
                    value={selectedCondominium || ''}
                    onChange={(e) =>
                      setSelectedCondominium(Number(e.target.value) || null)
                    }
                    required
                  >
                    <option value="">Select a condominium</option>
                    {condominiums.map((condo) => (
                      <option key={condo.id} value={condo.id}>
                        {condo.name}
                      </option>
                    ))}
                  </select>
                  <span
                    className="focus-input100"
                    data-placeholder="Condominium"
                  ></span>
                </div>
              )}

              <div className="search-fields">
                <div className="wrap-input100 validate-input">
                  <input
                    className={`input100 ${searchTerm.name ? 'has-val' : ''}`}
                    type="text"
                    value={searchTerm.name}
                    onChange={(e) =>
                      setSearchTerm({ ...searchTerm, name: e.target.value })
                    }
                  />
                  <span
                    className="focus-input100"
                    data-placeholder="Name"
                  ></span>
                </div>

                <div className="wrap-input100 validate-input">
                  <input
                    className={`input100 ${
                      searchTerm.registrationNumber ? 'has-val' : ''
                    }`}
                    type="text"
                    value={searchTerm.registrationNumber}
                    onChange={(e) =>
                      setSearchTerm({
                        ...searchTerm,
                        registrationNumber: e.target.value,
                      })
                    }
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
                      onClick={handleSearch}
                      disabled={
                        isSearching || (isAdmin && !selectedCondominium)
                      }
                    >
                      {isSearching ? 'Searching...' : 'Search'}
                    </button>
                  </div>
                </div>
              </div>

              <div className="container-btn100-form-btn">
                <div className="wrap-btn100-form-btn">
                  <div className="btn100-form-bgbtn"></div>
                  <button
                    type="submit"
                    className="btn100-form-btn"
                    onClick={handleCreateNewUser}
                  >
                    New User
                  </button>
                </div>
              </div>

              {error && <div className="error-message">{error}</div>}
            </form>

            {hasSearched ? (
              searchResults.length > 0 ? (
                <div className="table-responsive-wrapper">
                  <table className="user-table">
                    <thead>
                      <tr>
                        <th>Name</th>
                        <th>Registration number</th>
                        <th>View</th>
                        <th>Edit</th>
                        <th>Vehicles</th>
                      </tr>
                    </thead>
                    <tbody>
                      {searchResults.map((user) => (
                        <tr key={user.id}>
                          <td>{user.name}</td>
                          <td>{user.registrationNumber}</td>
                          <td>
                            {canViewUsers && (
                              <Link
                                to={`/users/${user.id}/view`}
                                className="view-link"
                              >
                                View
                              </Link>
                            )}
                          </td>
                          <td>
                            {canEditUsers && (
                              <Link
                                to={`/users/${user.id}/edit`}
                                className="edit-link"
                              >
                                Edit
                              </Link>
                            )}
                          </td>
                          <td>
                            <Link
                              to={`/vehicles/new/${user.id}`}
                              className="add-vehicle-link"
                            >
                              Add
                            </Link>
                          </td>
                        </tr>
                      ))}
                    </tbody>
                  </table>
                </div>
              ) : (
                <div className="no-results">
                  {isSearching ? (
                    <p>Loading results...</p>
                  ) : (
                    <p>No results found. Try a search.</p>
                  )}
                </div>
              )
            ) : null}

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

export default UserListPage;
