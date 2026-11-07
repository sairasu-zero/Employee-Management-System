import React from 'react';
import { AlertCircle } from 'lucide-react';
import { FormInputProps } from '../../types';

export const FormInput: React.FC<FormInputProps> = ({
  label,
  name,
  error,
  helperText,
  icon: Icon,
  rightElement,
  required,
  disabled,
  containerClassName = '',
  className = '',
  id,
  ...props
}) => {
  const inputId = id || `input-${name}`;

  return (
    <div className={`form-group ${containerClassName}`}>
      <label htmlFor={inputId} className="form-label">
        <span>
          {label}
          {required && <span className="form-label-required">*</span>}
        </span>
      </label>

      <div
        className={`input-container ${error ? 'has-error' : ''} ${
          disabled ? 'disabled' : ''
        }`}
      >
        {Icon && (
          <span className="input-icon-left">
            <Icon size={18} />
          </span>
        )}

        <input
          id={inputId}
          name={name}
          disabled={disabled}
          className={`form-control ${className}`}
          {...props}
        />

        {rightElement && (
          <div className="input-action-right">{rightElement}</div>
        )}
      </div>

      {error && (
        <div className="form-error-msg" role="alert">
          <AlertCircle size={14} />
          <span>{error}</span>
        </div>
      )}

      {!error && helperText && (
        <div className="form-helper-text">{helperText}</div>
      )}
    </div>
  );
};

export default FormInput;
