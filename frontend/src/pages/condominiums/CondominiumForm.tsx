import React, { useState, useEffect } from 'react';
import { useNavigate } from 'react-router-dom';
import '../../styles/util.css';
import './condominiumForm.css';
import { usePermissions } from '../hooks/usePermissions';
import { DeleteConfirmationModal } from '../../utils/DeleteConfirmationModal';
import {
  getCondominium,
  createCondominium,
  updateCondominium,
  deleteCondominium,
} from '../../services/condominiumService';

type CondominiumFormMode = 'create' | 'edit' | 'view';

interface CondominiumFormProps {
  mode?: CondominiumFormMode;
  condominiumId?: number;
}

interface CondominiumData {
  name: string;
  address: string;
  towerCount: number;
  maxUsers: number;
  enabled: boolean;
  towers: TowerData[];
}

interface TowerData {
  id?: number;
  number: number;
  name: string;
  floorCount: number;
}

const CondominiumForm = ({
  mode = 'create',
  condominiumId,
}: CondominiumFormProps) => {
  const navigate = useNavigate();
  const isViewMode = mode === 'view';
  const { canRegisterCondominiums, canEditCondominiums } = usePermissions();

  const [condominiumData, setCondominiumData] = useState<CondominiumData>({
    name: '',
    address: '',
    towerCount: 0,
    maxUsers: 0,
    enabled: true,
    towers: [],
  });

  const [showTowerForm, setShowTowerForm] = useState(false);
  const [editingTowerIndex, setEditingTowerIndex] = useState<number | null>(
    null,
  );
  const [currentTower, setCurrentTower] = useState<TowerData>({
    number: 0,
    name: '',
    floorCount: 0,
  });

  const [message, setMessage] = useState<{
    text: string;
    type: 'success' | 'error';
  } | null>(null);

  const [isDeleteModalOpen, setIsDeleteModalOpen] = useState(false);

  // Loading state per button
  const [loading, setLoading] = useState({
    submit: false,
    delete: false,
    saveTower: false,
    addTower: false,
    editTower: false,
    removeTower: false,
  });

  useEffect(() => {
    const fetchCondominiumData = async () => {
      try {
        const data = await getCondominium(condominiumId!);
        setCondominiumData({
          name: data.name,
          address: data.address,
          towerCount: data.towerCount,
          maxUsers: data.maxUsers,
          enabled: data.enabled,
          towers: data.towers || [],
        });
      } catch (error) {
        setMessage({ text: 'Error loading condominium', type: 'error' });
      }
    };

    if (mode !== 'create' && condominiumId) {
      fetchCondominiumData();
    }
  }, [mode, condominiumId]);

  const handleChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    const { name, value } = e.target;
    setCondominiumData({
      ...condominiumData,
      [name]:
        name === 'towerCount' || name === 'maxUsers'
          ? parseInt(value) || 0
          : value,
    });
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();

    // Already loading, do nothing
    if (loading.submit) return;

    // Basic validation
    if (!condominiumData.name || condominiumData.maxUsers <= 0) {
      setMessage({
        text: 'Fill in all required fields',
        type: 'error',
      });
      return;
    }

    setLoading((prev) => ({ ...prev, submit: true }));

    try {
      const condoPayload = {
        ...condominiumData,
        towers: condominiumData.towers.map((t) => ({
          number: t.number,
          name: t.name,
          floorCount: t.floorCount,
        })),
      };

      const data =
        mode === 'create'
          ? await createCondominium(condoPayload)
          : await updateCondominium(condominiumId!, condoPayload);

      if (mode === 'create') {
        navigate(`/condominiums/${data.id}/edit`);
      } else {
        setMessage({
          text: 'Condominium updated successfully',
          type: 'success',
        });
      }
    } catch (error) {
      setMessage({
        text: 'Error saving condominium',
        type: 'error',
      });
    } finally {
      setLoading((prev) => ({ ...prev, submit: false }));
    }
  };

  const handleDelete = async () => {
    // Already loading, do nothing
    if (loading.delete) return;

    setLoading((prev) => ({ ...prev, delete: true }));

    try {
      await deleteCondominium(condominiumId!);
      navigate('/condominiums');
    } catch (error) {
      setMessage({ text: 'Error deleting condominium', type: 'error' });
    } finally {
      setLoading((prev) => ({ ...prev, delete: false }));
      setIsDeleteModalOpen(false);
    }
  };

  const handleAddTowerClick = () => {
    setCurrentTower({ number: 0, name: '', floorCount: 0 });
    setEditingTowerIndex(null);
    setShowTowerForm(true);
  };

  const handleEditTower = (index: number) => {
    setCurrentTower(condominiumData.towers[index]);
    setEditingTowerIndex(index);
    setShowTowerForm(true);
  };

  const handleSaveTower = async () => {
    // Already loading, do nothing
    if (loading.saveTower) return;

    if (!currentTower.name || currentTower.floorCount <= 0) {
      setMessage({ text: 'Fill in all tower fields', type: 'error' });
      return;
    }

    const loadingKey = editingTowerIndex !== null ? 'editTower' : 'addTower';
    setLoading((prev) => ({ ...prev, [loadingKey]: true }));

    try {
      const updatedTowers = [...condominiumData.towers];

      if (editingTowerIndex !== null) {
        updatedTowers[editingTowerIndex] = currentTower;
      } else {
        updatedTowers.push(currentTower);
      }

      setCondominiumData({
        ...condominiumData,
        towers: updatedTowers,
        towerCount: updatedTowers.length,
      });

      setShowTowerForm(false);
      setMessage({
        text: `Tower ${
          editingTowerIndex !== null ? 'updated' : 'added'
        } successfully`,
        type: 'success',
      });
    } catch (error) {
      setMessage({ text: 'Error saving tower', type: 'error' });
    } finally {
      setLoading((prev) => ({ ...prev, [loadingKey]: false }));
    }
  };

  const handleRemoveTower = async (index: number) => {
    // Already loading, do nothing
    if (loading.removeTower) return;

    setLoading((prev) => ({ ...prev, removeTower: true }));

    try {
      const updatedTowers = [...condominiumData.towers];
      updatedTowers.splice(index, 1);
      setCondominiumData({
        ...condominiumData,
        towers: updatedTowers,
        towerCount: updatedTowers.length,
      });

      setMessage({ text: 'Tower removed successfully', type: 'success' });
    } catch (error) {
      setMessage({ text: 'Error removing tower', type: 'error' });
    } finally {
      setLoading((prev) => ({ ...prev, removeTower: false }));
    }
  };

  const handleCancelTowerEdit = () => {
    setShowTowerForm(false);
  };

  const renderTowerInfo = (tower: TowerData) => {
    return (
      <div className="tower-info">
        <span className="tower-number">Tower {tower.number}</span>
        <span className="tower-name">{tower.name}</span>
        <span className="tower-floors">
          {tower.floorCount} {tower.floorCount === 1 ? 'floor' : 'floors'}
        </span>
      </div>
    );
  };

  return (
    <div className="limiter">
      <div className="container-main">
        <div className="wrap-main">
          <form onSubmit={handleSubmit}>
            <span className="main-form-title">
              {mode === 'create'
                ? 'Registration'
                : mode === 'edit'
                ? 'Edit'
                : 'View'}{' '}
              Condominium
            </span>

            {/* Condominium fields */}
            <div className="wrap-input100 validate-input">
              <input
                className={`input100 ${condominiumData.name ? 'has-val' : ''}`}
                name="name"
                type="text"
                value={condominiumData.name}
                onChange={handleChange}
                required
                disabled={isViewMode}
              />
              <span className="focus-input100" data-placeholder="Name"></span>
            </div>

            <div className="wrap-input100 validate-input">
              <input
                className={`input100 ${
                  condominiumData.address ? 'has-val' : ''
                }`}
                name="address"
                type="text"
                value={condominiumData.address}
                onChange={handleChange}
                required
                disabled={isViewMode}
              />
              <span
                className="focus-input100"
                data-placeholder="Address"
              ></span>
            </div>

            <div className="wrap-input100 validate-input">
              <input
                className={`input100 ${
                  condominiumData.maxUsers ? 'has-val' : ''
                }`}
                name="maxUsers"
                type="number"
                min="1"
                value={condominiumData.maxUsers}
                onChange={handleChange}
                required
                disabled={isViewMode}
              />
              <span
                className="focus-input100"
                data-placeholder="Maximum Users"
              ></span>
            </div>

            {/* Towers section - list */}
            <div className="tower-section">
              <div className="tower-header">
                <h3>Towers</h3>
                {!isViewMode && !showTowerForm && (
                  <button
                    type="button"
                    className="btn-add-tower"
                    onClick={handleAddTowerClick}
                    disabled={loading.addTower}
                  >
                    {loading.addTower ? 'Loading...' : '+ Add Tower'}
                  </button>
                )}
              </div>

              {/* Tower list */}
              {condominiumData.towers.length > 0 ? (
                <div className="tower-list">
                  {condominiumData.towers.map((tower, index) => (
                    <div key={index} className="tower-item">
                      {renderTowerInfo(tower)}
                      {!isViewMode && (
                        <div className="tower-actions">
                          <button
                            type="button"
                            className="btn-edit-tower"
                            onClick={() => handleEditTower(index)}
                            disabled={loading.editTower}
                          >
                            {loading.editTower ? 'Loading...' : 'Edit'}
                          </button>
                          <button
                            type="button"
                            className="btn-remove-tower"
                            onClick={() => handleRemoveTower(index)}
                            disabled={loading.removeTower}
                          >
                            {loading.removeTower ? 'Removing...' : 'Remove'}
                          </button>
                        </div>
                      )}
                    </div>
                  ))}
                </div>
              ) : (
                !showTowerForm && (
                  <p className="no-towers">No towers registered</p>
                )
              )}

              {/* Tower form */}
              {showTowerForm && (
                <div className="tower-form">
                  <h4 className="tower-form-title">
                    {editingTowerIndex !== null
                      ? 'Edit Tower'
                      : 'Add New Tower'}
                  </h4>

                  <div className="form-group">
                    <label htmlFor="tower-number">Tower Number</label>
                    <input
                      id="tower-number"
                      className="form-control"
                      name="number"
                      type="number"
                      min="1"
                      value={currentTower.number}
                      onChange={(e) =>
                        setCurrentTower({
                          ...currentTower,
                          number: parseInt(e.target.value) || 0,
                        })
                      }
                    />
                  </div>

                  <div className="form-group">
                    <label htmlFor="tower-name">Tower Name</label>
                    <input
                      id="tower-name"
                      className="form-control"
                      name="name"
                      type="text"
                      value={currentTower.name}
                      onChange={(e) =>
                        setCurrentTower({
                          ...currentTower,
                          name: e.target.value,
                        })
                      }
                    />
                  </div>

                  <div className="form-group">
                    <label htmlFor="tower-floors">Number of Floors</label>
                    <input
                      id="tower-floors"
                      className="form-control"
                      name="floorCount"
                      type="number"
                      min="1"
                      value={currentTower.floorCount}
                      onChange={(e) =>
                        setCurrentTower({
                          ...currentTower,
                          floorCount: parseInt(e.target.value) || 0,
                        })
                      }
                    />
                  </div>

                  <div className="form-actions">
                    <button
                      type="button"
                      className="btn btn-primary"
                      onClick={handleSaveTower}
                      disabled={
                        loading.saveTower ||
                        loading.addTower ||
                        loading.editTower
                      }
                    >
                      {loading.saveTower ||
                      loading.addTower ||
                      loading.editTower
                        ? 'Saving...'
                        : editingTowerIndex !== null
                        ? 'Update'
                        : 'Save'}{' '}
                      Tower
                    </button>
                    <button
                      type="button"
                      className="btn btn-secondary"
                      onClick={handleCancelTowerEdit}
                      disabled={
                        loading.saveTower ||
                        loading.addTower ||
                        loading.editTower
                      }
                    >
                      Cancel
                    </button>
                  </div>
                </div>
              )}
            </div>

            {/* Action buttons */}
            {(canRegisterCondominiums && mode === 'create') ||
            (canEditCondominiums && mode === 'edit') ? (
              <div className="container-btn100-form-btn">
                <div className="wrap-btn100-form-btn">
                  <div className="btn100-form-bgbtn"></div>
                  <button
                    type="submit"
                    className="btn100-form-btn"
                    disabled={loading.submit}
                  >
                    {loading.submit
                      ? 'Loading...'
                      : mode === 'create'
                      ? 'Register'
                      : 'Update'}
                  </button>
                </div>
              </div>
            ) : null}

            {mode === 'edit' && (
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
                onClick={() => navigate('/condominiums')}
              >
                Back to List
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

export default CondominiumForm;
