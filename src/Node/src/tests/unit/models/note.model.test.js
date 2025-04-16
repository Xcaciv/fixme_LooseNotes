/**
 * Note model unit tests
 */

const mongoose = require('mongoose');
const Note = require('../../../models/note.model');
const User = require('../../../models/user.model');

describe('Note Model Tests', () => {
  let testUser;
  
  // Create a test user before running tests
  beforeEach(async () => {
    testUser = await new User({
      username: 'noteowner',
      email: 'noteowner@example.com',
      password: 'Password123!'
    }).save();
  });
  
  const noteData = {
    title: 'Test Note',
    content: 'This is a test note content.',
    isPublic: false,
    tags: ['test', 'unit-testing']
  };
  
  it('should create and save a note successfully', async () => {
    const validNote = new Note({
      ...noteData,
      owner: testUser._id
    });
    
    const savedNote = await validNote.save();
    
    // Verify saved note
    expect(savedNote._id).toBeDefined();
    expect(savedNote.title).toBe(noteData.title);
    expect(savedNote.content).toBe(noteData.content);
    expect(savedNote.isPublic).toBe(noteData.isPublic);
    expect(savedNote.tags).toEqual(expect.arrayContaining(noteData.tags));
    expect(savedNote.owner.toString()).toBe(testUser._id.toString());
    expect(savedNote.viewCount).toBe(0);
    expect(savedNote.averageRating).toBe(0);
    expect(savedNote.ratingCount).toBe(0);
    expect(savedNote.attachments).toHaveLength(0);
  });
  
  it('should fail to save a note without required fields', async () => {
    const invalidNote = new Note({
      owner: testUser._id,
      isPublic: true
    });
    
    await expect(invalidNote.save()).rejects.toThrow(
      mongoose.Error.ValidationError
    );
  });
  
  it('should fail to save a note with title too short', async () => {
    const invalidNote = new Note({
      ...noteData,
      title: 'AB', // Less than 3 characters
      owner: testUser._id
    });
    
    await expect(invalidNote.save()).rejects.toThrow(
      mongoose.Error.ValidationError
    );
  });
  
  it('should fail to save a note with title too long', async () => {
    // Create a title that is over 100 characters
    const longTitle = 'A'.repeat(101);
    
    const invalidNote = new Note({
      ...noteData,
      title: longTitle,
      owner: testUser._id
    });
    
    await expect(invalidNote.save()).rejects.toThrow(
      mongoose.Error.ValidationError
    );
  });
  
  it('should generate a share token successfully', async () => {
    const note = new Note({
      ...noteData,
      owner: testUser._id
    });
    
    await note.save();
    
    const { shareToken, expiresAt } = note.generateShareToken(5); // 5 days expiry
    
    expect(shareToken).toBeDefined();
    expect(note.shareToken).toBe(shareToken);
    expect(note.shareTokenExpiresAt).toBeDefined();
    
    // Token should expire 5 days in the future
    const fiveDaysLater = new Date();
    fiveDaysLater.setDate(fiveDaysLater.getDate() + 5);
    
    expect(note.shareTokenExpiresAt.getDate()).toBe(fiveDaysLater.getDate());
  });
  
  it('should validate a valid share token', async () => {
    const note = new Note({
      ...noteData,
      owner: testUser._id
    });
    
    await note.save();
    
    const { shareToken } = note.generateShareToken();
    await note.save();
    
    expect(note.isShareTokenValid(shareToken)).toBe(true);
  });
  
  it('should not validate an invalid share token', async () => {
    const note = new Note({
      ...noteData,
      owner: testUser._id
    });
    
    await note.save();
    
    const { shareToken } = note.generateShareToken();
    await note.save();
    
    expect(note.isShareTokenValid('invalid-token')).toBe(false);
  });
  
  it('should sanitize content with script tags', async () => {
    const noteWithScript = new Note({
      title: 'XSS Test Note',
      content: '<p>Normal content</p><script>alert("XSS")</script>',
      owner: testUser._id,
      isPublic: true
    });
    
    await noteWithScript.save();
    
    // Script tag should be removed
    expect(noteWithScript.content).not.toContain('<script>');
    expect(noteWithScript.content).not.toContain('</script>');
  });
  
  it('should replace javascript: URLs', async () => {
    const noteWithJavaScriptURL = new Note({
      title: 'URL Test Note',
      content: '<a href="javascript:alert(\'XSS\')">Click me</a>',
      owner: testUser._id,
      isPublic: true
    });
    
    await noteWithJavaScriptURL.save();
    
    // javascript: protocol should be replaced
    expect(noteWithJavaScriptURL.content).not.toContain('javascript:');
    expect(noteWithJavaScriptURL.content).toContain('blocked:');
  });
});