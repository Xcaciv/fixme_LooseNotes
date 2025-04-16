/**
 * E2E tests for note operations
 */

const request = require('supertest');
const app = require('../../app');
const User = require('../../models/user.model');
const Note = require('../../models/note.model');

describe('Note E2E Tests', () => {
  let testUser;
  let authToken;
  let testNote;
  
  const userData = {
    username: 'noteuser',
    email: 'noteuser@example.com',
    password: 'TestPassword123!',
    confirmPassword: 'TestPassword123!'
  };
  
  const noteData = {
    title: 'Test Note Title',
    content: 'This is test note content for E2E testing.',
    isPublic: false,
    tags: ['test', 'e2e']
  };
  
  // Helper function to register a user and get auth token
  const registerAndLogin = async () => {
    // Register a user
    await request(app)
      .post('/api/auth/register')
      .send(userData);
    
    // Login to get token
    const loginResponse = await request(app)
      .post('/api/auth/login')
      .send({
        email: userData.email,
        password: userData.password
      });
    
    authToken = loginResponse.body.data.token;
    
    // Get user from DB
    testUser = await User.findOne({ email: userData.email });
  };
  
  beforeEach(async () => {
    // Clean up existing test data
    await User.deleteMany({ email: userData.email });
    await Note.deleteMany({ title: noteData.title });
    
    // Set up user and auth token for tests
    await registerAndLogin();
  });
  
  // Test note creation
  it('should create a new note', async () => {
    const response = await request(app)
      .post('/api/notes')
      .set('Authorization', `Bearer ${authToken}`)
      .send(noteData)
      .expect(201);
    
    expect(response.body).toHaveProperty('success', true);
    expect(response.body).toHaveProperty('data');
    expect(response.body.data).toHaveProperty('title', noteData.title);
    expect(response.body.data).toHaveProperty('content', noteData.content);
    expect(response.body.data).toHaveProperty('owner', testUser._id.toString());
    expect(response.body.data).toHaveProperty('isPublic', noteData.isPublic);
    expect(response.body.data.tags).toEqual(expect.arrayContaining(noteData.tags));
    
    // Save note ID for later tests
    testNote = response.body.data;
  });
  
  // Test note retrieval
  it('should get all user notes', async () => {
    // First create a note
    const createResponse = await request(app)
      .post('/api/notes')
      .set('Authorization', `Bearer ${authToken}`)
      .send(noteData);
    
    // Then get all notes
    const response = await request(app)
      .get('/api/notes')
      .set('Authorization', `Bearer ${authToken}`)
      .expect(200);
    
    expect(response.body).toHaveProperty('success', true);
    expect(response.body).toHaveProperty('data');
    expect(Array.isArray(response.body.data)).toBe(true);
    expect(response.body.data.length).toBeGreaterThan(0);
    
    // Verify the created note is in the response
    const foundNote = response.body.data.find(
      note => note._id === createResponse.body.data._id
    );
    expect(foundNote).toBeDefined();
  });
  
  // Test single note retrieval
  it('should get a specific note by ID', async () => {
    // First create a note
    const createResponse = await request(app)
      .post('/api/notes')
      .set('Authorization', `Bearer ${authToken}`)
      .send(noteData);
    
    const noteId = createResponse.body.data._id;
    
    // Then get the specific note
    const response = await request(app)
      .get(`/api/notes/${noteId}`)
      .set('Authorization', `Bearer ${authToken}`)
      .expect(200);
    
    expect(response.body).toHaveProperty('success', true);
    expect(response.body).toHaveProperty('data');
    expect(response.body.data).toHaveProperty('_id', noteId);
    expect(response.body.data).toHaveProperty('title', noteData.title);
    expect(response.body.data).toHaveProperty('viewCount', 1); // View count should be incremented
  });
  
  // Test note update
  it('should update a note', async () => {
    // First create a note
    const createResponse = await request(app)
      .post('/api/notes')
      .set('Authorization', `Bearer ${authToken}`)
      .send(noteData);
    
    const noteId = createResponse.body.data._id;
    
    // Updated note data
    const updateData = {
      title: 'Updated Note Title',
      content: 'This content has been updated for testing.',
      isPublic: true
    };
    
    // Update the note
    const response = await request(app)
      .put(`/api/notes/${noteId}`)
      .set('Authorization', `Bearer ${authToken}`)
      .send(updateData)
      .expect(200);
    
    expect(response.body).toHaveProperty('success', true);
    expect(response.body).toHaveProperty('data');
    expect(response.body.data).toHaveProperty('title', updateData.title);
    expect(response.body.data).toHaveProperty('content', updateData.content);
    expect(response.body.data).toHaveProperty('isPublic', updateData.isPublic);
  });
  
  // Test note deletion
  it('should delete a note', async () => {
    // First create a note
    const createResponse = await request(app)
      .post('/api/notes')
      .set('Authorization', `Bearer ${authToken}`)
      .send(noteData);
    
    const noteId = createResponse.body.data._id;
    
    // Delete the note
    const response = await request(app)
      .delete(`/api/notes/${noteId}`)
      .set('Authorization', `Bearer ${authToken}`)
      .expect(200);
    
    expect(response.body).toHaveProperty('success', true);
    
    // Verify the note is deleted
    const getResponse = await request(app)
      .get(`/api/notes/${noteId}`)
      .set('Authorization', `Bearer ${authToken}`)
      .expect(404);
    
    expect(getResponse.body).toHaveProperty('success', false);
  });
  
  // Test note sharing
  it('should generate a share token for a note', async () => {
    // First create a note
    const createResponse = await request(app)
      .post('/api/notes')
      .set('Authorization', `Bearer ${authToken}`)
      .send(noteData);
    
    const noteId = createResponse.body.data._id;
    
    // Generate share token
    const response = await request(app)
      .post(`/api/notes/${noteId}/share`)
      .set('Authorization', `Bearer ${authToken}`)
      .send({ expiresInDays: 5 })
      .expect(200);
    
    expect(response.body).toHaveProperty('success', true);
    expect(response.body).toHaveProperty('data');
    expect(response.body.data).toHaveProperty('shareToken');
    expect(response.body.data).toHaveProperty('shareUrl');
    expect(response.body.data).toHaveProperty('expiresAt');
    
    const shareToken = response.body.data.shareToken;
    
    // Access note with share token (without auth)
    const accessResponse = await request(app)
      .get(`/api/notes/${noteId}?token=${shareToken}`)
      .expect(200);
    
    expect(accessResponse.body).toHaveProperty('success', true);
    expect(accessResponse.body).toHaveProperty('data');
    expect(accessResponse.body.data).toHaveProperty('_id', noteId);
  });
  
  // Test search notes
  it('should search for notes by text', async () => {
    // First create a note
    await request(app)
      .post('/api/notes')
      .set('Authorization', `Bearer ${authToken}`)
      .send({
        ...noteData,
        title: 'Unique Search Term Note',
        content: 'This note contains searchable content'
      });
    
    // Search for the note
    const response = await request(app)
      .get('/api/notes/search?q=Unique Search Term')
      .set('Authorization', `Bearer ${authToken}`)
      .expect(200);
    
    expect(response.body).toHaveProperty('success', true);
    expect(response.body).toHaveProperty('data');
    expect(Array.isArray(response.body.data)).toBe(true);
    expect(response.body.data.length).toBeGreaterThan(0);
    expect(response.body.data[0].title).toContain('Unique Search Term');
  });
  
  // Test top rated notes
  it('should get top rated notes', async () => {
    // Create a public note
    const createResponse = await request(app)
      .post('/api/notes')
      .set('Authorization', `Bearer ${authToken}`)
      .send({
        ...noteData,
        isPublic: true
      });
    
    // Get top rated notes (this will be empty as no ratings yet)
    const response = await request(app)
      .get('/api/notes/top-rated')
      .expect(200);
    
    expect(response.body).toHaveProperty('success', true);
    expect(response.body).toHaveProperty('data');
    expect(Array.isArray(response.body.data)).toBe(true);
    // There should be no notes in top rated as we haven't rated any
    expect(response.body.data.length).toBe(0);
  });
});