import React from 'react';
import { UserType } from '../../services/userService';
import { UserData, UserFormErrors } from './UserForm.types';

interface UserProfileFieldsProps {
  isViewMode: boolean;
  userData: Pick<
    UserData,
    'name' | 'address' | 'registrationNumber' | 'phone1' | 'userTypeId'
  >;
  errors: Pick<
    UserFormErrors,
    'name' | 'address' | 'registration' | 'phone1'
  >;
  filteredUserTypes: UserType[];
  onChange: (
    e: React.ChangeEvent<HTMLInputElement | HTMLSelectElement>,
  ) => void;
  onBlur: (
    e: React.FocusEvent<HTMLInputElement | HTMLSelectElement>,
  ) => void;
}

export const UserProfileFields: React.FC<UserProfileFieldsProps> = ({
  isViewMode,
  userData,
  errors,
  filteredUserTypes,
  onChange,
  onBlur,
}) => {
  return (
    <>
      <div
        className="wrap-input100 validate-input"
        data-validate="Name is required"
      >
        <input
          className={`input100 ${userData.name ? 'has-val' : ''}`}
          name="name"
          type="text"
          value={userData.name}
          onChange={onChange}
          onBlur={onBlur}
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
          onChange={onChange}
          onBlur={onBlur}
          required
          disabled={isViewMode}
        />
        <span className="focus-input100" data-placeholder="Address"></span>
        {errors.address && <p className="error-message">{errors.address}</p>}
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
          onChange={onChange}
          onBlur={onBlur}
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
          onChange={onChange}
          onBlur={onBlur}
          required
          disabled={isViewMode}
        />
        <span className="focus-input100" data-placeholder="Phone"></span>
        {errors.phone1 && <p className="error-message">{errors.phone1}</p>}
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
          onChange={onChange}
          onBlur={onBlur}
          required
          disabled={isViewMode}
        >
          <option value="">Select</option>
          {filteredUserTypes.map((ut) => (
            <option key={ut.id} value={ut.id}>
              {ut.description}
            </option>
          ))}
        </select>
        <span className="focus-input100" data-placeholder="User Type"></span>
      </div>
    </>
  );
};
