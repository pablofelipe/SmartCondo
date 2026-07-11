import React, { useState } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { usePermissions } from '../hooks/usePermissions';
import config from '../../config';
import { getAuthHeaders } from '../../utils/ApiUtils';
import './condominiumList.module.css';

const CondominiumListPage = () => {
  const navigate = useNavigate();
  const [searchTerm, setSearchTerm] = useState({
    name: '',
  });
  const [searchResults, setSearchResults] = useState([]);
  const [isSearching, setIsSearching] = useState(false);
  const [hasSearched, setHasSearched] = useState(false);
  const [error, setError] = useState('');
  const { canEditCondominiums, canViewCondominiums } = usePermissions();

  const handleSearch = async (e: React.FormEvent) => {
    e.preventDefault();

    if (!searchTerm.name) {
      setError('Preencha o nome para busca');
      return;
    }

    if (searchTerm.name.length < 3) {
      setError('O nome deve ter pelo menos 3 caracteres');
      return;
    }

    setHasSearched(true);
    setIsSearching(true);
    setError('');

    try {
      const params = new URLSearchParams();
      params.append('Name', searchTerm.name);

      const headers = getAuthHeaders();
      if (!headers.Authorization) return;

      const fullUrl = `${config.apiUrl}/Condominium/search?${params}`;

      const response = await fetch(fullUrl, {
        method: 'GET',
        headers: headers,
      });

      if (response.ok) {
        const data = await response.json();
        setSearchResults(data);
      }
    } catch (error) {
      console.error('Erro na busca:', error);
      setError('Ocorreu um erro na busca');
    } finally {
      setIsSearching(false);
    }
  };

  const handleCreateNewCondominium = () => {
    navigate('/condominiums/new');
  };

  return (
    <div className="limiter">
      <div className="container-main">
        <div className="wrap-main">
          <div className="form-header">
            <h1 className="main-form-title">Busca de Condomínios</h1>

            <form onSubmit={handleSearch} className="search-form">
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
                    data-placeholder="Nome"
                  ></span>
                </div>

                <div className="container-btn100-form-btn">
                  <div className="wrap-btn100-form-btn">
                    <div className="btn100-form-bgbtn"></div>
                    <button
                      type="submit"
                      className="btn100-form-btn"
                      onClick={handleSearch}
                      disabled={isSearching}
                    >
                      {isSearching ? 'Buscando...' : 'Buscar'}
                    </button>
                  </div>
                </div>
              </div>

              <div className="container-btn100-form-btn">
                <div className="wrap-btn100-form-btn">
                  <div className="btn100-form-bgbtn"></div>
                  <button
                    type="button"
                    className="btn100-form-btn"
                    onClick={handleCreateNewCondominium}
                  >
                    Novo Condomínio
                  </button>
                </div>
              </div>

              {error && <div className="error-message">{error}</div>}
            </form>

            {hasSearched ? (
              searchResults.length > 0 ? (
                <div className="table-responsive-wrapper">
                  <table className="condominium-table">
                    <thead>
                      <tr>
                        <th>Nome</th>
                        <th>Endereço</th>
                        <th>Torres</th>
                        <th>Visualizar</th>
                        <th>Editar</th>
                      </tr>
                    </thead>
                    <tbody>
                      {searchResults.map((condominium: any) => (
                        <tr key={condominium.id}>
                          <td>{condominium.name}</td>
                          <td>{condominium.address}</td>
                          <td>{condominium.towerCount}</td>
                          <td>
                            {canViewCondominiums && (
                              <Link
                                to={`/condominiums/${condominium.id}/view`}
                                className="view-link"
                              >
                                Visualizar
                              </Link>
                            )}
                          </td>
                          <td>
                            {canEditCondominiums && (
                              <Link
                                to={`/condominiums/${condominium.id}/edit`}
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
                </div>
              ) : (
                <div className="no-results">
                  {isSearching ? (
                    <p>Carregando resultados...</p>
                  ) : (
                    <p>Nenhum resultado encontrado. Faça uma busca.</p>
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
                Voltar ao Dashboard
              </span>
            </div>
          </div>
        </div>
      </div>
    </div>
  );
};

export default CondominiumListPage;
