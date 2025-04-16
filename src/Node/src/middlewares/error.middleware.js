/**
 * Global error handling middleware
 * @module middlewares/error
 */

const mongoose = require('mongoose');
const logger = require('../config/logger');

/**
 * Handle JWT errors
 * @param {Error} err - Error object
 * @returns {Error} - Modified error object
 */
const handleJwtError = (err) => {
  const message = 'Invalid token. Please log in again!';
  err.statusCode = 401;
  err.message = message;
  return err;
};

/**
 * Handle JWT expired error
 * @param {Error} err - Error object
 * @returns {Error} - Modified error object
 */
const handleJwtExpiredError = (err) => {
  const message = 'Your token has expired! Please log in again.';
  err.statusCode = 401;
  err.message = message;
  return err;
};

/**
 * Handle MongoDB cast error (invalid IDs)
 * @param {Error} err - Error object
 * @returns {Error} - Modified error object
 */
const handleCastErrorDB = (err) => {
  const message = `Invalid ${err.path}: ${err.value}.`;
  err.statusCode = 400;
  err.message = message;
  return err;
};

/**
 * Handle MongoDB duplicate fields error
 * @param {Error} err - Error object
 * @returns {Error} - Modified error object
 */
const handleDuplicateFieldsDB = (err) => {
  const value = err.errmsg.match(/(["'])(\\?.)*?\1/)[0];
  const message = `Duplicate field value: ${value}. Please use another value!`;
  err.statusCode = 409;
  err.message = message;
  return err;
};

/**
 * Handle MongoDB validation error
 * @param {Error} err - Error object
 * @returns {Error} - Modified error object
 */
const handleValidationErrorDB = (err) => {
  const errors = Object.values(err.errors).map((el) => el.message);
  const message = `Invalid input data. ${errors.join('. ')}`;
  err.statusCode = 400;
  err.message = message;
  return err;
};

/**
 * Send error response for development environment
 * @param {Error} err - Error object
 * @param {Object} res - Express response object
 */
const sendErrorDev = (err, res) => {
  // Log the error
  logger.debug(err);

  res.status(err.statusCode).json({
    success: false,
    error: err,
    message: err.message,
    stack: err.stack,
  });
};

/**
 * Send error response for production environment
 * @param {Error} err - Error object
 * @param {Object} res - Express response object
 */
const sendErrorProd = (err, res) => {
  // Operational, trusted error: send message to client
  if (err.isOperational) {
    res.status(err.statusCode).json({
      success: false,
      message: err.message,
    });
  } else {
    // Programming or other unknown error: don't leak error details
    
    // 1) Log error
    logger.error('ERROR 💥', err);
    
    // 2) Send generic message
    res.status(500).json({
      success: false,
      message: 'Something went wrong',
    });
  }
};

/**
 * Global error handling middleware
 * @param {Error} err - Error object
 * @param {Object} req - Express request object
 * @param {Object} res - Express response object
 * @param {Function} next - Express next middleware function
 */
module.exports = (err, req, res, next) => {
  err.statusCode = err.statusCode || 500;
  
  // Default error message if none exists
  if (!err.message) {
    err.message = 'Server error';
  }
  
  // Mark certain types of errors as operational
  if (
    err.name === 'ValidationError' ||
    err.name === 'CastError' ||
    err.name === 'MongoError' ||
    err.name === 'JsonWebTokenError' ||
    err.name === 'TokenExpiredError'
  ) {
    err.isOperational = true;
  }
  
  if (process.env.NODE_ENV === 'development') {
    sendErrorDev(err, res);
  } else {
    let error = { ...err };
    error.message = err.message;
    error.name = err.name;
    error.stack = err.stack;
    error.isOperational = err.isOperational;
    
    // Handle specific error types
    if (error.name === 'CastError') error = handleCastErrorDB(error);
    if (error.code === 11000) error = handleDuplicateFieldsDB(error);
    if (error.name === 'ValidationError') error = handleValidationErrorDB(error);
    if (error.name === 'JsonWebTokenError') error = handleJwtError(error);
    if (error.name === 'TokenExpiredError') error = handleJwtExpiredError(error);
    
    sendErrorProd(error, res);
  }
};