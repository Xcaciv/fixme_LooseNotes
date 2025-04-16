/**
 * E2E tests for authentication flows
 */

const request = require('supertest');
const app = require('../../app');
const User = require('../../models/user.model');

describe('Authentication E2E Tests', () => {
  const testUser = {
    username: 'e2etester',
    email: 'e2e@example.com',
    password: 'TestPassword123!',
    confirmPassword: 'TestPassword123!'
  };
  
  let authToken;
  
  beforeEach(async () => {
    // Clean up any existing test users
    await User.deleteMany({ email: testUser.email });
  });
  
  it('should register a new user', async () => {
    const response = await request(app)
      .post('/api/auth/register')
      .send(testUser)
      .expect(201);
    
    // Check response structure
    expect(response.body).toHaveProperty('success', true);
    expect(response.body).toHaveProperty('data.user');
    expect(response.body.data.user).toHaveProperty('id');
    expect(response.body.data.user).toHaveProperty('username', testUser.username);
    expect(response.body.data.user).toHaveProperty('email', testUser.email);
    expect(response.body.data.user).toHaveProperty('role', 'user');
    expect(response.body.data).toHaveProperty('token');
    expect(response.body.data).toHaveProperty('refreshToken');
  });
  
  it('should not allow registration with duplicate email', async () => {
    // First create a user
    await request(app)
      .post('/api/auth/register')
      .send(testUser);
    
    // Try to create another with the same email
    const response = await request(app)
      .post('/api/auth/register')
      .send({
        ...testUser,
        username: 'differentUsername'
      })
      .expect(409);
    
    expect(response.body).toHaveProperty('success', false);
    expect(response.body.message).toContain('Email is already in use');
  });
  
  it('should not allow registration with duplicate username', async () => {
    // First create a user
    await request(app)
      .post('/api/auth/register')
      .send(testUser);
    
    // Try to create another with the same username
    const response = await request(app)
      .post('/api/auth/register')
      .send({
        ...testUser,
        email: 'different@example.com'
      })
      .expect(409);
    
    expect(response.body).toHaveProperty('success', false);
    expect(response.body.message).toContain('Username is already taken');
  });
  
  it('should login a registered user', async () => {
    // First register a user
    await request(app)
      .post('/api/auth/register')
      .send(testUser);
    
    // Then login
    const loginResponse = await request(app)
      .post('/api/auth/login')
      .send({
        email: testUser.email,
        password: testUser.password
      })
      .expect(200);
    
    // Check login response
    expect(loginResponse.body).toHaveProperty('success', true);
    expect(loginResponse.body).toHaveProperty('data.user');
    expect(loginResponse.body.data).toHaveProperty('token');
    expect(loginResponse.body.data).toHaveProperty('refreshToken');
    
    // Store token for further tests
    authToken = loginResponse.body.data.token;
  });
  
  it('should not login with incorrect password', async () => {
    // First register a user
    await request(app)
      .post('/api/auth/register')
      .send(testUser);
    
    // Try to login with wrong password
    const response = await request(app)
      .post('/api/auth/login')
      .send({
        email: testUser.email,
        password: 'wrongpassword'
      })
      .expect(401);
    
    expect(response.body).toHaveProperty('success', false);
    expect(response.body.message).toContain('Invalid credentials');
  });
  
  it('should refresh an auth token', async () => {
    // First register and login to get tokens
    await request(app)
      .post('/api/auth/register')
      .send(testUser);
    
    const loginResponse = await request(app)
      .post('/api/auth/login')
      .send({
        email: testUser.email,
        password: testUser.password
      });
    
    const { refreshToken } = loginResponse.body.data;
    
    // Use the refresh token to get a new access token
    const response = await request(app)
      .post('/api/auth/refresh-token')
      .send({ refreshToken })
      .expect(200);
    
    expect(response.body).toHaveProperty('success', true);
    expect(response.body).toHaveProperty('data.token');
  });
  
  it('should request password reset', async () => {
    // First register a user
    await request(app)
      .post('/api/auth/register')
      .send(testUser);
    
    // Request password reset
    const response = await request(app)
      .post('/api/auth/forgot-password')
      .send({ email: testUser.email })
      .expect(200);
    
    expect(response.body).toHaveProperty('success', true);
    // In our test environment, we return the token for testing purposes
    expect(response.body).toHaveProperty('resetToken');
    expect(response.body).toHaveProperty('resetUrl');
  });
});