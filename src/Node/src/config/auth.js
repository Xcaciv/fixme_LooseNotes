/**
 * Passport authentication configuration
 * @module config/auth
 */

const passport = require('passport');
const { Strategy: JwtStrategy, ExtractJwt } = require('passport-jwt');
const User = require('../models/user.model');
const logger = require('./logger');

// Configure JWT options
const opts = {
  jwtFromRequest: ExtractJwt.fromAuthHeaderAsBearerToken(),
  secretOrKey: process.env.JWT_SECRET || 'your-secret-key-change-in-production',
};

// JWT Strategy for token authentication
passport.use(
  new JwtStrategy(opts, async (jwtPayload, done) => {
    try {
      // Find the user specified in the token
      const user = await User.findById(jwtPayload.id);

      // If user doesn't exist or is inactive
      if (!user || !user.isActive) {
        return done(null, false, { message: 'User not found or inactive' });
      }

      // Check if token was issued before password was changed
      if (user.passwordChangedAt) {
        const changedTimestamp = parseInt(
          user.passwordChangedAt.getTime() / 1000,
          10
        );

        // If password was changed after token was issued
        if (jwtPayload.iat < changedTimestamp) {
          return done(null, false, {
            message: 'User recently changed password. Please log in again.',
          });
        }
      }

      // Update last login timestamp
      user.lastActive = new Date();
      await user.save({ validateBeforeSave: false });

      // Authentication succeeded
      return done(null, user);
    } catch (err) {
      logger.error('Error in JWT strategy:', err);
      return done(err, false);
    }
  })
);

module.exports = passport;