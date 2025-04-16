/**
 * E2E tests for rating functionality
 */

const request = require('supertest');
const app = require('../../app');
const User = require('../../models/user.model');
const Note = require('../../models/note.model');
const Rating = require('../../models/rating.model');

describe('Rating E2E Tests', () => {
  let testUser, authorUser;
  let authToken, authorToken;
  let testNote;
  
  const userData = {
    username: 'ratingtester',
    email: 'ratingtester@example.com',
    password: 'TestPassword123!',
    confirmPassword: 'TestPassword123!'
  };
  
  const authorData = {
    username: 'noteauthor',
    email: 'noteauthor@example.com',
    password: 'AuthorPassword123!',
    confirmPassword: 'AuthorPassword123!'
  };
  
  const noteData = {
    title: 'Note to be Rated',
    content: 'This is a test note that will receive ratings.',
    isPublic: true,
    tags: ['test', 'rating']
  };
  
  const ratingData = {
    value: 4,
    comment: 'This is a test rating comment.'
  };
  
  // Helper function to register and login a user
  const registerAndLogin = async (userData) => {
    // Register user
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
    
    return loginResponse.body.data.token;
  };
  
  beforeEach(async () => {
    // Clean up existing test data
    await User.deleteMany({ email: { $in: [userData.email, authorData.email] } });
    await Note.deleteMany({ title: noteData.title });
    await Rating.deleteMany({});
    
    // Set up users
    authToken = await registerAndLogin(userData);
    testUser = await User.findOne({ email: userData.email });
    
    authorToken = await registerAndLogin(authorData);
    authorUser = await User.findOne({ email: authorData.email });
    
    // Create a note by author for testing
    const noteResponse = await request(app)
      .post('/api/notes')
      .set('Authorization', `Bearer ${authorToken}`)
      .send(noteData);
    
    testNote = noteResponse.body.data;
  });
  
  it('should create a rating for a note', async () => {
    const response = await request(app)
      .post(`/api/ratings/${testNote._id}`)
      .set('Authorization', `Bearer ${authToken}`)
      .send(ratingData)
      .expect(201);
    
    expect(response.body).toHaveProperty('success', true);
    expect(response.body).toHaveProperty('data');
    expect(response.body.data).toHaveProperty('value', ratingData.value);
    expect(response.body.data).toHaveProperty('comment', ratingData.comment);
    expect(response.body.data).toHaveProperty('user', testUser._id.toString());
    expect(response.body.data).toHaveProperty('note', testNote._id);
  });
  
  it('should not allow creating a rating for a non-public note without access', async () => {
    // Create a private note
    const privateNoteResponse = await request(app)
      .post('/api/notes')
      .set('Authorization', `Bearer ${authorToken}`)
      .send({
        ...noteData,
        title: 'Private Note',
        isPublic: false
      });
    
    const privateNoteId = privateNoteResponse.body.data._id;
    
    // Try to rate the private note
    const response = await request(app)
      .post(`/api/ratings/${privateNoteId}`)
      .set('Authorization', `Bearer ${authToken}`)
      .send(ratingData)
      .expect(403);
    
    expect(response.body).toHaveProperty('success', false);
  });
  
  it('should update a rating for a note', async () => {
    // First create a rating
    await request(app)
      .post(`/api/ratings/${testNote._id}`)
      .set('Authorization', `Bearer ${authToken}`)
      .send(ratingData);
    
    // Update the rating
    const updatedRatingData = {
      value: 5,
      comment: 'Updated comment for this note.'
    };
    
    const response = await request(app)
      .put(`/api/ratings/${testNote._id}`)
      .set('Authorization', `Bearer ${authToken}`)
      .send(updatedRatingData)
      .expect(200);
    
    expect(response.body).toHaveProperty('success', true);
    expect(response.body).toHaveProperty('data');
    expect(response.body.data).toHaveProperty('value', updatedRatingData.value);
    expect(response.body.data).toHaveProperty('comment', updatedRatingData.comment);
  });
  
  it('should delete a rating', async () => {
    // First create a rating
    await request(app)
      .post(`/api/ratings/${testNote._id}`)
      .set('Authorization', `Bearer ${authToken}`)
      .send(ratingData);
    
    // Delete the rating
    const response = await request(app)
      .delete(`/api/ratings/${testNote._id}`)
      .set('Authorization', `Bearer ${authToken}`)
      .expect(200);
    
    expect(response.body).toHaveProperty('success', true);
    
    // Verify the rating is deleted by trying to get it
    const getResponse = await request(app)
      .get(`/api/ratings/${testNote._id}`)
      .set('Authorization', `Bearer ${authToken}`)
      .expect(404);
    
    expect(getResponse.body).toHaveProperty('success', false);
  });
  
  it('should not allow creating multiple ratings for the same note by same user', async () => {
    // Create first rating
    await request(app)
      .post(`/api/ratings/${testNote._id}`)
      .set('Authorization', `Bearer ${authToken}`)
      .send(ratingData)
      .expect(201);
    
    // Try to create second rating
    const response = await request(app)
      .post(`/api/ratings/${testNote._id}`)
      .set('Authorization', `Bearer ${authToken}`)
      .send({
        value: 3,
        comment: 'This should fail'
      })
      .expect(409);
    
    expect(response.body).toHaveProperty('success', false);
    expect(response.body.message).toContain('already rated');
  });
  
  it('should get all ratings for a note', async () => {
    // Create a rating
    await request(app)
      .post(`/api/ratings/${testNote._id}`)
      .set('Authorization', `Bearer ${authToken}`)
      .send(ratingData);
    
    // Get all ratings for the note
    const response = await request(app)
      .get(`/api/ratings/note/${testNote._id}`)
      .expect(200);
    
    expect(response.body).toHaveProperty('success', true);
    expect(response.body).toHaveProperty('data');
    expect(Array.isArray(response.body.data)).toBe(true);
    expect(response.body.data.length).toBeGreaterThan(0);
    expect(response.body.data[0]).toHaveProperty('value', ratingData.value);
  });
  
  it('should update note averageRating when a rating is created', async () => {
    // Create a rating
    await request(app)
      .post(`/api/ratings/${testNote._id}`)
      .set('Authorization', `Bearer ${authToken}`)
      .send(ratingData);
    
    // Get the updated note
    const response = await request(app)
      .get(`/api/notes/${testNote._id}`)
      .expect(200);
    
    expect(response.body).toHaveProperty('success', true);
    expect(response.body).toHaveProperty('data');
    expect(response.body.data).toHaveProperty('averageRating', ratingData.value);
    expect(response.body.data).toHaveProperty('ratingCount', 1);
  });
  
  it('should calculate correct average when multiple ratings exist', async () => {
    // Create first rating
    await request(app)
      .post(`/api/ratings/${testNote._id}`)
      .set('Authorization', `Bearer ${authToken}`)
      .send(ratingData); // value: 4
    
    // Register another user
    const anotherUserData = {
      username: 'anotheruser',
      email: 'another@example.com',
      password: 'AnotherPass123!',
      confirmPassword: 'AnotherPass123!'
    };
    
    const anotherToken = await registerAndLogin(anotherUserData);
    
    // Create second rating
    await request(app)
      .post(`/api/ratings/${testNote._id}`)
      .set('Authorization', `Bearer ${anotherToken}`)
      .send({
        value: 2,
        comment: 'Not as good'
      });
    
    // Get the updated note
    const response = await request(app)
      .get(`/api/notes/${testNote._id}`)
      .expect(200);
    
    expect(response.body).toHaveProperty('success', true);
    expect(response.body).toHaveProperty('data');
    expect(response.body.data).toHaveProperty('averageRating', 3); // Average of 4 and 2
    expect(response.body.data).toHaveProperty('ratingCount', 2);
  });
});