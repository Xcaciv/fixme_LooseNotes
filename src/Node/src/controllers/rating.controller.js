/**
 * Rating controller
 * @module controllers/rating
 */

const Rating = require('../models/rating.model');
const Note = require('../models/note.model');
const logger = require('../config/logger');
const mongoose = require('mongoose');

/**
 * Create or update a rating for a note
 * @async
 * @param {Object} req - Express request object
 * @param {Object} res - Express response object
 * @param {Function} next - Express next middleware function
 * @returns {Object} JSON response with created/updated rating
 */
exports.rateNote = async (req, res, next) => {
  try {
    const { id: noteId } = req.params;
    const { value, comment } = req.body;
    const userId = req.user._id;

    // Check if the note exists
    const note = await Note.findById(noteId);
    if (!note) {
      return res.status(404).json({
        success: false,
        message: 'Note not found',
      });
    }

    // Check if the user has already rated the note
    let rating = await Rating.findOne({
      note: noteId,
      user: userId,
    });

    if (rating) {
      // Update existing rating
      rating.value = value;
      if (comment !== undefined) {
        rating.comment = comment;
      }
      
      await rating.save();
      
      logger.info(`Rating updated for note: ${noteId} by user: ${userId}`);
      
      res.status(200).json({
        success: true,
        message: 'Rating updated successfully',
        data: rating,
      });
    } else {
      // Create new rating
      rating = new Rating({
        note: noteId,
        user: userId,
        value,
        comment: comment || '',
      });
      
      await rating.save();
      
      logger.info(`Rating created for note: ${noteId} by user: ${userId}`);
      
      res.status(201).json({
        success: true,
        message: 'Rating submitted successfully',
        data: rating,
      });
    }
  } catch (error) {
    next(error);
  }
};

/**
 * Get all ratings for a note
 * @async
 * @param {Object} req - Express request object
 * @param {Object} res - Express response object
 * @param {Function} next - Express next middleware function
 * @returns {Object} JSON response with ratings array
 */
exports.getNoteRatings = async (req, res, next) => {
  try {
    const { id: noteId } = req.params;
    const { page = 1, limit = 10 } = req.query;
    
    // Set up pagination
    const pageNum = parseInt(page);
    const limitNum = parseInt(limit);
    const skip = (pageNum - 1) * limitNum;
    
    // Get ratings for the note with pagination
    const ratings = await Rating.find({ note: noteId })
      .sort({ createdAt: -1 })
      .skip(skip)
      .limit(limitNum)
      .populate('user', 'username');
    
    // Get total count for pagination
    const total = await Rating.countDocuments({ note: noteId });
    
    res.status(200).json({
      success: true,
      data: ratings,
      pagination: {
        total,
        page: pageNum,
        limit: limitNum,
        pages: Math.ceil(total / limitNum),
      },
    });
  } catch (error) {
    next(error);
  }
};

/**
 * Get a rating by ID
 * @async
 * @param {Object} req - Express request object
 * @param {Object} res - Express response object
 * @param {Function} next - Express next middleware function
 * @returns {Object} JSON response with rating data
 */
exports.getRatingById = async (req, res, next) => {
  try {
    const { id: ratingId } = req.params;
    
    const rating = await Rating.findById(ratingId)
      .populate('user', 'username')
      .populate('note', 'title');
    
    if (!rating) {
      return res.status(404).json({
        success: false,
        message: 'Rating not found',
      });
    }
    
    res.status(200).json({
      success: true,
      data: rating,
    });
  } catch (error) {
    next(error);
  }
};

/**
 * Update a rating
 * @async
 * @param {Object} req - Express request object
 * @param {Object} res - Express response object
 * @param {Function} next - Express next middleware function
 * @returns {Object} JSON response with updated rating
 */
exports.updateRating = async (req, res, next) => {
  try {
    const { id: ratingId } = req.params;
    const { value, comment } = req.body;
    
    // Find the rating
    const rating = await Rating.findById(ratingId);
    
    if (!rating) {
      return res.status(404).json({
        success: false,
        message: 'Rating not found',
      });
    }
    
    // Ensure the user is updating their own rating
    if (rating.user.toString() !== req.user._id.toString()) {
      return res.status(403).json({
        success: false,
        message: 'You can only update your own ratings',
      });
    }
    
    // Update the rating
    if (value !== undefined) {
      rating.value = value;
    }
    
    if (comment !== undefined) {
      rating.comment = comment;
    }
    
    await rating.save();
    
    logger.info(`Rating ${ratingId} updated by user: ${req.user._id}`);
    
    res.status(200).json({
      success: true,
      message: 'Rating updated successfully',
      data: rating,
    });
  } catch (error) {
    next(error);
  }
};

/**
 * Delete a rating
 * @async
 * @param {Object} req - Express request object
 * @param {Object} res - Express response object
 * @param {Function} next - Express next middleware function
 * @returns {Object} JSON response with success message
 */
exports.deleteRating = async (req, res, next) => {
  try {
    const { id: ratingId } = req.params;
    
    // Find the rating
    const rating = await Rating.findById(ratingId);
    
    if (!rating) {
      return res.status(404).json({
        success: false,
        message: 'Rating not found',
      });
    }
    
    // Check if the user is the owner of the rating or an admin
    if (rating.user.toString() !== req.user._id.toString() && req.user.role !== 'admin') {
      return res.status(403).json({
        success: false,
        message: 'Unauthorized to delete this rating',
      });
    }
    
    await Rating.findByIdAndDelete(ratingId);
    
    logger.info(`Rating ${ratingId} deleted by user: ${req.user._id}`);
    
    res.status(200).json({
      success: true,
      message: 'Rating deleted successfully',
    });
  } catch (error) {
    next(error);
  }
};

/**
 * Get all ratings by a user
 * @async
 * @param {Object} req - Express request object
 * @param {Object} res - Express response object
 * @param {Function} next - Express next middleware function
 * @returns {Object} JSON response with ratings array
 */
exports.getUserRatings = async (req, res, next) => {
  try {
    const userId = req.params.userId || req.user._id;
    const { page = 1, limit = 10 } = req.query;
    
    // Set up pagination
    const pageNum = parseInt(page);
    const limitNum = parseInt(limit);
    const skip = (pageNum - 1) * limitNum;
    
    // Get ratings by user with pagination
    const ratings = await Rating.find({ user: userId })
      .sort({ createdAt: -1 })
      .skip(skip)
      .limit(limitNum)
      .populate('note', 'title');
    
    // Get total count for pagination
    const total = await Rating.countDocuments({ user: userId });
    
    res.status(200).json({
      success: true,
      data: ratings,
      pagination: {
        total,
        page: pageNum,
        limit: limitNum,
        pages: Math.ceil(total / limitNum),
      },
    });
  } catch (error) {
    next(error);
  }
};