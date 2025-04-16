/**
 * Test teardown for Jest
 * Performs global cleanup after all tests
 */

module.exports = async () => {
  // Any global teardown operations can be added here
  // This file is referenced in the jest.config.js as globalTeardown
  console.log('Tests completed, global teardown executed.');
};