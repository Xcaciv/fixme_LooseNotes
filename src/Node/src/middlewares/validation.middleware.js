/**
 * Validation middleware
 * @module middlewares/validation
 */

const Joi = require('joi');

/**
 * Create validation middleware
 * @param {Object} schema - Joi schema for validation
 * @returns {Function} - Express middleware function
 */
exports.validate = (schema) => {
  return (req, res, next) => {
    const { error } = schema.validate(req.body, {
      abortEarly: false,
      stripUnknown: true,
      errors: {
        wrap: {
          label: '',
        },
      },
    });

    if (error) {
      const errorMessage = error.details.map((detail) => detail.message).join(', ');
      const validationError = new Error(errorMessage);
      validationError.statusCode = 400;
      validationError.isOperational = true;
      return next(validationError);
    }

    next();
  };
};

/**
 * User schemas
 */
exports.userSchemas = {
  register: Joi.object({
    username: Joi.string().trim().min(3).max(30).required()
      .messages({
        'string.min': 'Username must be at least 3 characters long',
        'string.max': 'Username cannot be longer than 30 characters',
        'string.empty': 'Username is required',
        'any.required': 'Username is required',
      }),
    email: Joi.string().trim().email().required()
      .messages({
        'string.email': 'Please provide a valid email address',
        'string.empty': 'Email is required',
        'any.required': 'Email is required',
      }),
    password: Joi.string().trim().min(8).required().pattern(/^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[@$!%*?&])[A-Za-z\d@$!%*?&]{8,}$/)
      .messages({
        'string.min': 'Password must be at least 8 characters long',
        'string.empty': 'Password is required',
        'any.required': 'Password is required',
        'string.pattern.base': 'Password must contain at least one uppercase letter, one lowercase letter, one number and one special character',
      }),
    confirmPassword: Joi.string().valid(Joi.ref('password')).required()
      .messages({
        'any.only': 'Passwords do not match',
        'any.required': 'Password confirmation is required',
      }),
    role: Joi.string().valid('user', 'admin').default('user'),
    fullName: Joi.string().trim().max(100),
    bio: Joi.string().trim().max(500),
  }),

  login: Joi.object({
    email: Joi.string().trim().email().required()
      .messages({
        'string.email': 'Please provide a valid email address',
        'string.empty': 'Email is required',
        'any.required': 'Email is required',
      }),
    password: Joi.string().trim().required()
      .messages({
        'string.empty': 'Password is required',
        'any.required': 'Password is required',
      }),
  }),

  updateProfile: Joi.object({
    username: Joi.string().trim().min(3).max(30)
      .messages({
        'string.min': 'Username must be at least 3 characters long',
        'string.max': 'Username cannot be longer than 30 characters',
      }),
    fullName: Joi.string().trim().max(100),
    bio: Joi.string().trim().max(500),
    email: Joi.string().trim().email()
      .messages({
        'string.email': 'Please provide a valid email address',
      }),
  }),

  changePassword: Joi.object({
    currentPassword: Joi.string().trim().required()
      .messages({
        'string.empty': 'Current password is required',
        'any.required': 'Current password is required',
      }),
    newPassword: Joi.string().trim().min(8).required().pattern(/^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[@$!%*?&])[A-Za-z\d@$!%*?&]{8,}$/)
      .messages({
        'string.min': 'New password must be at least 8 characters long',
        'string.empty': 'New password is required',
        'any.required': 'New password is required',
        'string.pattern.base': 'New password must contain at least one uppercase letter, one lowercase letter, one number and one special character',
      }),
    confirmNewPassword: Joi.string().valid(Joi.ref('newPassword')).required()
      .messages({
        'any.only': 'Passwords do not match',
        'any.required': 'Password confirmation is required',
      }),
  }),
};

/**
 * Note schemas
 */
exports.noteSchemas = {
  create: Joi.object({
    title: Joi.string().trim().min(1).max(100).required()
      .messages({
        'string.min': 'Title cannot be empty',
        'string.max': 'Title cannot be longer than 100 characters',
        'string.empty': 'Title is required',
        'any.required': 'Title is required',
      }),
    content: Joi.string().trim().required()
      .messages({
        'string.empty': 'Content is required',
        'any.required': 'Content is required',
      }),
    isPublic: Joi.boolean().default(false),
    tags: Joi.array().items(Joi.string().trim()).default([]),
    sharedWith: Joi.array().items(Joi.string().trim().regex(/^[0-9a-fA-F]{24}$/)).default([])
      .messages({
        'string.pattern.base': 'Invalid user ID format',
      }),
  }),

  update: Joi.object({
    title: Joi.string().trim().min(1).max(100)
      .messages({
        'string.min': 'Title cannot be empty',
        'string.max': 'Title cannot be longer than 100 characters',
      }),
    content: Joi.string().trim(),
    isPublic: Joi.boolean(),
    tags: Joi.array().items(Joi.string().trim()),
    sharedWith: Joi.array().items(Joi.string().trim().regex(/^[0-9a-fA-F]{24}$/))
      .messages({
        'string.pattern.base': 'Invalid user ID format',
      }),
  }),
};

/**
 * Rating schemas
 */
exports.ratingSchemas = {
  create: Joi.object({
    value: Joi.number().integer().min(1).max(5).required()
      .messages({
        'number.base': 'Rating value must be a number',
        'number.integer': 'Rating value must be an integer',
        'number.min': 'Rating value must be between 1 and 5',
        'number.max': 'Rating value must be between 1 and 5',
        'any.required': 'Rating value is required',
      }),
    comment: Joi.string().trim().max(500).allow('', null)
      .messages({
        'string.max': 'Comment cannot be longer than 500 characters',
      }),
  }),

  update: Joi.object({
    value: Joi.number().integer().min(1).max(5)
      .messages({
        'number.base': 'Rating value must be a number',
        'number.integer': 'Rating value must be an integer',
        'number.min': 'Rating value must be between 1 and 5',
        'number.max': 'Rating value must be between 1 and 5',
      }),
    comment: Joi.string().trim().max(500).allow('', null)
      .messages({
        'string.max': 'Comment cannot be longer than 500 characters',
      }),
  }),
};