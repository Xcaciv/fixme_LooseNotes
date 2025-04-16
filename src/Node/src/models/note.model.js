/**
 * Note model schema
 * @module models/note
 */

const mongoose = require('mongoose');
const Schema = mongoose.Schema;

/**
 * Note attachment schema definition
 */
const attachmentSchema = new Schema({
  filename: {
    type: String,
    required: [true, 'Filename is required'],
  },
  originalName: {
    type: String,
    required: [true, 'Original file name is required'],
  },
  mimetype: {
    type: String,
    required: [true, 'File type is required'],
  },
  size: {
    type: Number,
    required: [true, 'File size is required'],
  },
  path: {
    type: String,
    required: [true, 'File path is required'],
  },
  uploadedAt: {
    type: Date,
    default: Date.now,
  },
});

/**
 * Note schema definition
 */
const noteSchema = new Schema(
  {
    title: {
      type: String,
      required: [true, 'Title is required'],
      trim: true,
      minlength: [3, 'Title must be at least 3 characters'],
      maxlength: [100, 'Title cannot exceed 100 characters'],
    },
    content: {
      type: String,
      required: [true, 'Content is required'],
      trim: true,
    },
    owner: {
      type: Schema.Types.ObjectId,
      ref: 'User',
      required: [true, 'Note owner is required'],
    },
    isPublic: {
      type: Boolean,
      default: false,
    },
    tags: [{
      type: String,
      trim: true,
      lowercase: true,
    }],
    attachments: [attachmentSchema],
    shareToken: {
      type: String,
      default: null,
    },
    shareTokenExpiresAt: {
      type: Date,
      default: null,
    },
    viewCount: {
      type: Number,
      default: 0,
    },
    averageRating: {
      type: Number,
      default: 0,
      min: 0,
      max: 5,
    },
    ratingCount: {
      type: Number,
      default: 0,
    },
  },
  {
    timestamps: true,
    toJSON: { virtuals: true },
    toObject: { virtuals: true },
  }
);

// Create indices for better performance
noteSchema.index({ title: 'text', content: 'text' });
noteSchema.index({ owner: 1 });
noteSchema.index({ isPublic: 1 });
noteSchema.index({ tags: 1 });

/**
 * Virtual field for ratings
 */
noteSchema.virtual('ratings', {
  ref: 'Rating',
  localField: '_id',
  foreignField: 'note',
});

/**
 * Generate share token for the note
 * @returns {string} - Generated share token and expiration date
 */
noteSchema.methods.generateShareToken = function (expiresInDays = 7) {
  const crypto = require('crypto');
  const shareToken = crypto.randomBytes(20).toString('hex');
  this.shareToken = shareToken;
  this.shareTokenExpiresAt = new Date(Date.now() + expiresInDays * 24 * 60 * 60 * 1000);
  return {
    shareToken,
    expiresAt: this.shareTokenExpiresAt,
  };
};

/**
 * Check if a share token is valid
 * @param {string} token - The share token to validate
 * @returns {boolean} - True if the token is valid, false otherwise
 */
noteSchema.methods.isShareTokenValid = function (token) {
  return (
    this.shareToken === token && 
    this.shareTokenExpiresAt && 
    this.shareTokenExpiresAt > new Date()
  );
};

/**
 * Mongoose pre-save middleware to sanitize note content
 * to prevent XSS attacks
 */
noteSchema.pre('save', function (next) {
  if (this.isModified('content')) {
    // Basic HTML sanitization - in production, use a library like DOMPurify
    // This is a simple placeholder implementation
    this.content = this.content
      .replace(/<script\b[^<]*(?:(?!<\/script>)<[^<]*)*<\/script>/gi, '')
      .replace(/javascript:/gi, 'blocked:');
  }
  next();
});

const Note = mongoose.model('Note', noteSchema);

module.exports = Note;