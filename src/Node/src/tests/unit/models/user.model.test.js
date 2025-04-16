/**
 * User model unit tests
 */

const mongoose = require('mongoose');
const User = require('../../../models/user.model');

describe('User Model Tests', () => {
  const userData = {
    username: 'testuser',
    email: 'test@example.com',
    password: 'Password123!',
  };

  it('should create and save a user successfully', async () => {
    const validUser = new User(userData);
    const savedUser = await validUser.save();

    // Verify saved user
    expect(savedUser._id).toBeDefined();
    expect(savedUser.username).toBe(userData.username);
    expect(savedUser.email).toBe(userData.email);
    // Password should be hashed and not the original
    expect(savedUser.password).not.toBe(userData.password);
    expect(savedUser.role).toBe('user'); // Default role
    expect(savedUser.isActive).toBe(true); // Default active status
  });

  it('should fail to save a user with duplicate email', async () => {
    // First create a user
    await new User(userData).save();

    // Try to create another user with the same email
    const duplicateUser = new User({
      ...userData,
      username: 'anotheruser',
    });

    // Check that validation fails
    await expect(duplicateUser.save()).rejects.toThrow(
      mongoose.Error.ValidationError
    );
  });

  it('should fail to save a user with duplicate username', async () => {
    // First create a user
    await new User(userData).save();

    // Try to create another user with the same username
    const duplicateUser = new User({
      ...userData,
      email: 'another@example.com',
    });

    // Check that validation fails
    await expect(duplicateUser.save()).rejects.toThrow(
      mongoose.Error.ValidationError
    );
  });

  it('should validate a correct password', async () => {
    const user = await new User(userData).save();
    const isMatch = await user.comparePassword(userData.password);
    expect(isMatch).toBe(true);
  });

  it('should not validate an incorrect password', async () => {
    const user = await new User(userData).save();
    const isMatch = await user.comparePassword('wrongpassword');
    expect(isMatch).toBe(false);
  });

  it('should generate a password reset token', async () => {
    const user = await new User(userData).save();
    const resetToken = user.generatePasswordResetToken();

    expect(resetToken).toBeDefined();
    expect(user.passwordResetToken).toBeDefined();
    expect(user.passwordResetExpires).toBeDefined();
    
    // Token should expire in the future (10 minutes)
    expect(user.passwordResetExpires.getTime()).toBeGreaterThan(Date.now());
  });

  it('should fail to save a user with invalid email', async () => {
    const invalidUser = new User({
      ...userData,
      email: 'invalidemail',
    });

    await expect(invalidUser.save()).rejects.toThrow(
      mongoose.Error.ValidationError
    );
  });

  it('should fail to save a user with password too short', async () => {
    const invalidUser = new User({
      ...userData,
      password: 'short',
    });

    await expect(invalidUser.save()).rejects.toThrow(
      mongoose.Error.ValidationError
    );
  });
});