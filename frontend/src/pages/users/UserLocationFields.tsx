/* eslint-disable eqeqeq */
import React from 'react';
import {
  Condominium,
  CondominiumDetail,
} from '../../services/condominiumService';
import { Tower } from '../../services/towerService';
import { UserData } from './UserForm.types';

interface UserLocationFieldsProps {
  isViewMode: boolean;
  canManageAllCondominiums: boolean;
  isSysAdmin: boolean;
  isApartmentOwner: boolean;
  userData: Pick<
    UserData,
    'condominiumId' | 'towerId' | 'floorId' | 'apartment' | 'parkingSpaceNumber'
  >;
  condominiums: Condominium[];
  currentUserCondominium: CondominiumDetail | null;
  towers: Tower[];
  onChange: (
    e: React.ChangeEvent<HTMLInputElement | HTMLSelectElement>,
  ) => void;
  onBlur: (
    e: React.FocusEvent<HTMLInputElement | HTMLSelectElement>,
  ) => void;
}

export const UserLocationFields: React.FC<UserLocationFieldsProps> = ({
  isViewMode,
  canManageAllCondominiums,
  isSysAdmin,
  isApartmentOwner,
  userData,
  condominiums,
  currentUserCondominium,
  towers,
  onChange,
  onBlur,
}) => {
  return (
    <>
      {canManageAllCondominiums && !isSysAdmin && (
        <div className="wrap-input100 validate-input">
          <label htmlFor="condominium" className="input-label">
            Condominium
          </label>
          <select
            className={`input100 ${userData.condominiumId ? 'has-val' : ''}`}
            name="condominiumId"
            value={userData.condominiumId}
            onChange={onChange}
            required
            disabled={isViewMode}
          >
            <option value="">Select</option>
            {condominiums.map((condo) => (
              <option key={condo.id} value={condo.id}>
                {condo.name}
              </option>
            ))}
          </select>
        </div>
      )}

      {!canManageAllCondominiums && currentUserCondominium && (
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
              onChange={onChange}
              onBlur={onBlur}
              required={isApartmentOwner}
              disabled={
                !userData.condominiumId ||
                userData.condominiumId == 0 ||
                isViewMode
              }
            >
              <option value="">Select</option>
              {towers.map((tower) => (
                <option key={tower.id} value={tower.id}>
                  {tower.number} - {tower.name}
                </option>
              ))}
            </select>
            <span className="focus-input100" data-placeholder="Tower"></span>
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
              onChange={onChange}
              onBlur={onBlur}
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
              className={`input100 ${userData.apartment ? 'has-val' : ''}`}
              name="apartment"
              type="number"
              value={userData.apartment}
              onChange={onChange}
              onBlur={onBlur}
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
              onChange={onChange}
              onBlur={onBlur}
              required={isApartmentOwner}
            />
            <span
              className="focus-input100 focus-number"
              data-placeholder="Parking Space"
            ></span>
          </div>
        </>
      )}
    </>
  );
};
