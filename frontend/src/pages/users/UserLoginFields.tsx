/* eslint-disable eqeqeq */
import React from 'react';
import { UserFormMode, LoginData, UserFormErrors } from './UserForm.types';

interface UserLoginFieldsProps {
  mode: UserFormMode;
  isViewMode: boolean;
  loginData: LoginData;
  errors: Pick<UserFormErrors, 'email' | 'password' | 'confirmPassword'>;
  isChangingPassword: boolean;
  onChange: (e: React.ChangeEvent<HTMLInputElement>) => void;
  onBlur: (e: React.FocusEvent<HTMLInputElement>) => void;
  onChangePasswordClick: () => void;
  onCancelPasswordChange: () => void;
}

export const UserLoginFields: React.FC<UserLoginFieldsProps> = ({
  mode,
  isViewMode,
  loginData,
  errors,
  isChangingPassword,
  onChange,
  onBlur,
  onChangePasswordClick,
  onCancelPasswordChange,
}) => {
  return (
    <>
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
          onChange={onChange}
          onBlur={onBlur}
          required
          disabled={isViewMode}
        />
        <span className="focus-input100" data-placeholder="Email"></span>
        {errors.email && <p className="error-message">{errors.email}</p>}
      </div>

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
          <span className="focus-input100" data-placeholder="Password"></span>
          {!isViewMode && (
            <button
              type="button"
              className="btn-change-password"
              onClick={onChangePasswordClick}
              disabled={isChangingPassword}
            >
              {isChangingPassword ? 'Loading...' : 'Change Password'}
            </button>
          )}
        </div>
      ) : (
        <>
          <div
            className="wrap-input100 validate-input"
            data-validate="Password is required"
          >
            <input
              className={`input100 ${loginData.password ? 'has-val' : ''}`}
              autoComplete="off"
              name="password"
              type="password"
              value={loginData.password}
              onChange={onChange}
              onBlur={onBlur}
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
              onChange={onChange}
              onBlur={onBlur}
              required={mode == 'create'}
              disabled={isViewMode}
            />
            <span
              className="focus-input100"
              data-placeholder={
                mode == 'create' ? 'Confirm Password' : 'Confirm New Password'
              }
            ></span>
            {errors.confirmPassword && (
              <p className="error-message">{errors.confirmPassword}</p>
            )}
          </div>

          {mode != 'create' && loginData.showPasswordFields && (
            <button
              type="button"
              className="btn-cancel-password"
              onClick={onCancelPasswordChange}
              disabled={isChangingPassword}
            >
              Cancel Change
            </button>
          )}
        </>
      )}
    </>
  );
};
