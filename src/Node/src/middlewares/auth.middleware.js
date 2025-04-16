/**
 * Authentication middleware
 * @module middlewares/auth
 */

const passport = require('passport');
const jwt = require('jsonwebtoken');
const { JWT_SECRET } = require('../config/auth');
const Note = require('../models/note.model');
const User = require('../models/user.model');
const logger = require('../config/logger');

/**
 * Protect routes that require authentication
 * @returns {Function} - Express middleware function
 */
exports.protect = async (req, res, next) => {
  try {
    let token;

    // Get token from Authorization header
    if (
      req.headers.authorization &&
      req.headers.authorization.startsWith('Bearer')
    ) {
      token = req.headers.authorization.split(' ')[1];
    }

    // Check if token exists
    if (!token) {
      const error = new Error('Not authenticated, please login');
      error.statusCode = 401;
      error.isOperational = true;
      return next(error);
    }

    try {
      // Verify token
      const decoded = jwt.verify(token, JWT_SECRET);

      // Check if user still exists
      const user = await User.findById(decoded.id).select('-password');
      if (!user) {
        const error = new Error('The user belonging to this token no longer exists');
        error.statusCode = 401;
        error.isOperational = true;
        return next(error);
      }

      // Grant access to protected route
      req.user = user;
      next();
    } catch (error) {
      // Handle JWT verification errors
      error.statusCode = 401;
      error.isOperational = true;
      error.message = 'Invalid token or token expired, please login again';
      next(error);
    }
  } catch (error) {
    next(error);
  }
};

/**
 * Restrict routes to specific roles
 * @param {...String} roles - Allowed roles
 * @returns {Function} - Express middleware function
 */
exports.restrictTo = (...roles) => {
  return (req, res, next) => {
    if (!roles.includes(req.user.role)) {
      const error = new Error('You do not have permission to perform this action');
      error.statusCode = 403;
      error.isOperational = true;
      return next(error);
    }
    next();
  };
};

/**
 * Check if user is the owner of a resource or an admin
 * @param {Function} getOwnerId - Function that returns the owner ID of the resource
 * @returns {Function} - Express middleware function
 */
exports.isResourceOwnerOrAdmin = (getOwnerId) => {
  return async (req, res, next) => {
    try {
      const ownerId = await getOwnerId(req);
      
      // Check if user is the owner or admin
      if (
        req.user.role === 'admin' ||
        req.user._id.toString() === ownerId.toString()
      ) {
        return next();
      }
      
      const error = new Error('You do not have permission to access this resource');
      error.statusCode = 403;
      error.isOperational = true;
      next(error);
    } catch (error) {
      next(error);
    }
  };
};

/**
 * Middleware to authenticate JWT token
 * @param {Object} req - Express request object
 * @param {Object} res - Express response object
 * @param {Function} next - Express next middleware function
 */
exports.authenticate = (req, res, next) => {
  passport.authenticate('jwt', { session: false }, (err, user, info) => {
    if (err) {
      logger.error('Authentication error:', err);
      return next(err);
    }

    if (!user) {
      const error = new Error(info?.message || 'Authentication required');
      error.statusCode = 401;
      error.isOperational = true;
      return next(error);
    }

    // Set authenticated user in request object
    req.user = user;
    next();
  })(req, res, next);
};

/**
 * Middleware to authorize admin role
 * @param {Object} req - Express request object
 * @param {Object} res - Express response object
 * @param {Function} next - Express next middleware function
 */
exports.authorizeAdmin = (req, res, next) => {
  if (!req.user) {
    const error = new Error('Authentication required');
    error.statusCode = 401;
    error.isOperational = true;
    return next(error);
  }

  if (req.user.role !== 'admin') {
    const error = new Error('Admin privileges required');
    error.statusCode = 403;
    error.isOperational = true;
    return next(error);
  }

  next();
};

/**
 * Middleware to authorize note owner
 * @param {Object} req - Express request object
 * @param {Object} res - Express response object
 * @param {Function} next - Express next middleware function
 */
exports.authorizeNoteOwner = async (req, res, next) => {
  try {
    const noteId = req.params.id;
    
    // Find note by ID
    const note = await Note.findById(noteId);
    
    // Check if note exists
    if (!note) {
      const error = new Error('Note not found');
      error.statusCode = 404;
      error.isOperational = true;
      return next(error);
    }
    
    // Check if user is authenticated
    if (!req.user) {
      const error = new Error('Authentication required');
      error.statusCode = 401;
      error.isOperational = true;
      return next(error);
    }
    
    // Check if user is note owner or admin
    if (note.owner.toString() !== req.user._id.toString() && req.user.role !== 'admin') {
      const error = new Error('Unauthorized to modify this note');
      error.statusCode = 403;
      error.isOperational = true;
      return next(error);
    }
    
    // Add note to request for further use
    req.note = note;
    next();
  } catch (error) {
    next(error);
  }
};

/**
 * Middleware to check if user can access a note
 * Allows access if:
 * - Note is public
 * - User is note owner
 * - User has a valid share token
 * @param {Object} req - Express request object
 * @param {Object} res - Express response object
 * @param {Function} next - Express next middleware function
 */
exports.canAccessNote = async (req, res, next) => {
  try {
    const noteId = req.params.id;
    const shareToken = req.query.token;
    
    // Find note by ID
    const note = await Note.findById(noteId);
    
    // Check if note exists
    if (!note) {
      const error = new Error('Note not found');
      error.statusCode = 404;
      error.isOperational = true;
      return next(error);
    }
    
    // Check access conditions:
    
    // 1. Note is public
    if (note.isPublic) {
      req.note = note;
      return next();
    }
    
    // 2. User is authenticated and is note owner or admin
    if (req.user && (note.owner.toString() === req.user._id.toString() || req.user.role === 'admin')) {
      req.note = note;
      return next();
    }
    
    // 3. User has valid share token
    if (shareToken && note.isShareTokenValid(shareToken)) {
      req.note = note;
      return next();
    }
    
    // If none of above, deny access
    const error = new Error('Unauthorized to access this note');
    error.statusCode = 403;
    error.isOperational = true;
    return next(error);
  } catch (error) {
    next(error);
  }
};

/**
 * Optional authentication middleware
 * Authenticates user if token is present but doesn't fail if not
 * @param {Object} req - Express request object
 * @param {Object} res - Express response object
 * @param {Function} next - Express next middleware function
 */
exports.optionalAuthenticate = (req, res, next) => {
  passport.authenticate('jwt', { session: false }, (err, user) => {
    if (user) {
      req.user = user;
    }
    next();
  })(req, res, next);
};