/**
 * Rating model unit tests
 */

const mongoose = require('mongoose');
const Rating = require('../../../models/rating.model');
const User = require('../../../models/user.model');
const Note = require('../../../models/note.model');

describe('Rating Model Tests', () => {
  let testUser, testNote;
  
  // Create a test user and note before each test
  beforeEach(async () => {
    testUser = await new User({
      username: 'ratinguser',
      email: 'ratinguser@example.com',
      password: 'Password123!'
    }).save();
    
    testNote = await new Note({
      title: 'Note to Rate',
      content: 'This is a note that will receive ratings.',
      owner: testUser._id,
      isPublic: true
    }).save();
  });
  
  const ratingData = {
    value: 4,
    comment: 'This is a test rating comment.'
  };
  
  it('should create and save a rating successfully', async () => {
    const validRating = new Rating({
      ...ratingData,
      note: testNote._id,
      user: testUser._id
    });
    
    const savedRating = await validRating.save();
    
    // Verify saved rating
    expect(savedRating._id).toBeDefined();
    expect(savedRating.value).toBe(ratingData.value);
    expect(savedRating.comment).toBe(ratingData.comment);
    expect(savedRating.note.toString()).toBe(testNote._id.toString());
    expect(savedRating.user.toString()).toBe(testUser._id.toString());
  });
  
  it('should fail to save a rating without required fields', async () => {
    const invalidRating = new Rating({
      comment: 'Missing required fields'
    });
    
    await expect(invalidRating.save()).rejects.toThrow(
      mongoose.Error.ValidationError
    );
  });
  
  it('should fail to save a rating with value out of range', async () => {
    // Value below minimum
    const lowRating = new Rating({
      value: 0, // Min is 1
      note: testNote._id,
      user: testUser._id
    });
    
    await expect(lowRating.save()).rejects.toThrow(
      mongoose.Error.ValidationError
    );
    
    // Value above maximum
    const highRating = new Rating({
      value: 6, // Max is 5
      note: testNote._id,
      user: testUser._id
    });
    
    await expect(highRating.save()).rejects.toThrow(
      mongoose.Error.ValidationError
    );
  });
  
  it('should update note averageRating when rating is saved', async () => {
    // Create a rating
    const rating = new Rating({
      value: 4,
      note: testNote._id,
      user: testUser._id
    });
    
    await rating.save();
    
    // Get the updated note
    const updatedNote = await Note.findById(testNote._id);
    
    // Check that the note's rating has been updated
    expect(updatedNote.averageRating).toBe(4);
    expect(updatedNote.ratingCount).toBe(1);
  });
  
  it('should calculate average of multiple ratings', async () => {
    // Create additional test user
    const anotherUser = await new User({
      username: 'anotheruser',
      email: 'another@example.com',
      password: 'Password123!'
    }).save();
    
    // Create two ratings
    await new Rating({
      value: 5,
      note: testNote._id,
      user: testUser._id
    }).save();
    
    await new Rating({
      value: 3,
      note: testNote._id,
      user: anotherUser._id
    }).save();
    
    // Get the updated note
    const updatedNote = await Note.findById(testNote._id);
    
    // Check that the note's rating has been updated with average of 4
    expect(updatedNote.averageRating).toBe(4.0);
    expect(updatedNote.ratingCount).toBe(2);
  });
  
  it('should only allow one rating per user per note', async () => {
    // Create a rating
    await new Rating({
      value: 4,
      note: testNote._id,
      user: testUser._id
    }).save();
    
    // Try to create another rating by the same user for the same note
    const duplicateRating = new Rating({
      value: 5,
      note: testNote._id,
      user: testUser._id
    });
    
    // This should fail due to the unique compound index
    await expect(duplicateRating.save()).rejects.toThrow();
  });
  
  it('should calculate correct average with the static method', async () => {
    // Create additional test user
    const anotherUser = await new User({
      username: 'anotheruser',
      email: 'another@example.com',
      password: 'Password123!'
    }).save();
    
    // Create ratings
    await new Rating({
      value: 5,
      note: testNote._id,
      user: testUser._id
    }).save();
    
    await new Rating({
      value: 3,
      note: testNote._id,
      user: anotherUser._id
    }).save();
    
    // Use the static method to calculate average
    const result = await Rating.calculateAverageRating(testNote._id);
    
    // Check the calculation
    expect(result.averageRating).toBe(4.0);
    expect(result.count).toBe(2);
  });
});