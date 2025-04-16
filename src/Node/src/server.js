/**
 * Server entry point for Loose Notes application
 */

require('dotenv').config();
const http = require('http');
const mongoose = require('mongoose');

// Import app
const app = require('./app');

// Import database connection
const { connect } = require('./config/database');

// Import logger
const logger = require('./config/logger');

// Get port from environment or default to 3000
const PORT = process.env.PORT || 3000;
app.set('port', PORT);

// Create HTTP server
const server = http.createServer(app);

/**
 * Handle uncaught exceptions
 */
process.on('uncaughtException', (err) => {
  logger.error('UNCAUGHT EXCEPTION! 💥 Shutting down...', {
    error: err.name,
    message: err.message,
    stack: err.stack,
  });
  process.exit(1);
});

/**
 * Start the server
 */
const startServer = async () => {
  try {
    // Connect to database
    await connect();
    logger.info('Successfully connected to database');

    // Start listening on port
    server.listen(PORT, () => {
      logger.info(`Server running in ${process.env.NODE_ENV} mode on port ${PORT}`);
      logger.info(`API documentation available at http://localhost:${PORT}/api/docs`);
    });
  } catch (error) {
    logger.error('Error starting server:', error);
    process.exit(1);
  }
};

// Start the server
startServer();

/**
 * Handle unhandled promise rejections
 */
process.on('unhandledRejection', (err) => {
  logger.error('UNHANDLED REJECTION! 💥 Shutting down...', {
    error: err.name,
    message: err.message,
    stack: err.stack,
  });
  // Give server time to finish current requests before shutting down
  server.close(() => {
    process.exit(1);
  });
});

/**
 * Handle SIGTERM signal
 */
process.on('SIGTERM', () => {
  logger.info('👋 SIGTERM RECEIVED. Shutting down gracefully');
  server.close(() => {
    mongoose.connection.close(false, () => {
      logger.info('💥 Process terminated!');
      process.exit(0);
    });
  });
});

/**
 * Handle SIGINT signal
 */
process.on('SIGINT', () => {
  logger.info('👋 SIGINT RECEIVED. Shutting down gracefully');
  server.close(() => {
    mongoose.connection.close(false, () => {
      logger.info('💥 Process terminated!');
      process.exit(0);
    });
  });
});