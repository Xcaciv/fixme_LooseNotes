/**
 * Rating model schema
 * @module models/rating
 */

const mongoose = require('mongoose');
const Schema = mongoose.Schema;

/**
 * Rating schema definition
 */
const ratingSchema = new Schema(
  {
    note: {
      type: Schema.Types.ObjectId,
      ref: 'Note',
      required: [true, 'Note reference is required'],
    },
    user: {
      type: Schema.Types.ObjectId,
      ref: 'User',
      required: [true, 'User reference is required'],
    },
    value: {
      type: Number,
      required: [true, 'Rating value is required'],
      min: [1, 'Rating must be at least 1'],
      max: [5, 'Rating cannot exceed 5'],
    },
    comment: {
      type: String,
      trim: true,
      maxlength: [500, 'Comment cannot exceed 500 characters'],
    },
  },
  {
    timestamps: true,
    toJSON: { virtuals: true },
    toObject: { virtuals: true },
  }
);

// Create compound index to ensure one rating per user per note
ratingSchema.index({ note: 1, user: 1 }, { unique: true });

/**
 * Static method to calculate average rating for a note
 * @param {string} noteId - The ID of the note
 * @returns {Promise<{averageRating: number, count: number}>} - Average rating and count
 */
ratingSchema.statics.calculateAverageRating = async function (noteId) {
  const result = await this.aggregate([
    {
      $match: { note: new mongoose.Types.ObjectId(noteId) },
    },
    {
      $group: {
        _id: '$note',
        averageRating: { $avg: '$value' },
        count: { $sum: 1 },
      },
    },
  ]);

  // Return 0 if no ratings exist
  if (result.length === 0) {
    return { averageRating: 0, count: 0 };
  }

  return {
    averageRating: parseFloat(result[0].averageRating.toFixed(1)),
    count: result[0].count,
  };
};

/**
 * Middleware to update the note's average rating after saving a rating
 */
ratingSchema.post('save', async function () {
  // Use the static method to calculate average
  const { averageRating, count } = await this.constructor.calculateAverageRating(
    this.note
  );

  // Update the note with new average and count
  await mongoose.model('Note').findByIdAndUpdate(this.note, {
    averageRating,
    ratingCount: count,
  });
});

/**
 * Middleware to update the note's average rating after updating a rating
 */
ratingSchema.post('findOneAndUpdate', async function () {
  const rating = await this.model.findOne(this.getQuery());
  
  if (rating) {
    // Use the static method to calculate average
    const { averageRating, count } = await this.model.calculateAverageRating(
      rating.note
    );

    // Update the note with new average and count
    await mongoose.model('Note').findByIdAndUpdate(rating.note, {
      averageRating,
      ratingCount: count,
    });
  }
});

/**
 * Middleware to update the note's average rating after deleting a rating
 */
ratingSchema.post('findOneAndDelete', async function (doc) {
  if (doc) {
    // Use the static method to calculate average
    const { averageRating, count } = await this.model.calculateAverageRating(
      doc.note
    );

    // Update the note with new average and count
    await mongoose.model('Note').findByIdAndUpdate(doc.note, {
      averageRating,
      ratingCount: count,
    });
  }
});

const Rating = mongoose.model('Rating', ratingSchema);

module.exports = Rating;