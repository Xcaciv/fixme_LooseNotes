/**
 * Database configuration
 * @module config/database
 */

const mongoose = require('mongoose');
const logger = require('./logger');

// Set mongoose options
mongoose.set('strictQuery', false);

// MongoDB connection options
const connectionOptions = {
  autoIndex: process.env.NODE_ENV !== 'production', // Don't build indexes in production
  maxPoolSize: 10, // Maintain up to 10 socket connections
  serverSelectionTimeoutMS: 5000, // Keep trying to send operations for 5 seconds
  socketTimeoutMS: 45000, // Close sockets after 45 seconds of inactivity
  family: 4 // Use IPv4, skip trying IPv6
};

/**
 * Connect to MongoDB database
 * @param {string} mongoUri - MongoDB connection URI
 * @returns {Promise} Mongoose connection
 */
const connectDb = async (mongoUri) => {
  try {
    const connection = await mongoose.connect(mongoUri, connectionOptions);
    logger.info(`MongoDB connected: ${connection.connection.host}`);
    return connection;
  } catch (error) {
    logger.error(`MongoDB connection error: ${error.message}`);
    process.exit(1);
  }
};

module.exports = {
  connectDb,
  connection: mongoose.connection
};