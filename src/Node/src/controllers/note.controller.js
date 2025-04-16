/**
 * Note controller
 * @module controllers/note
 */

const Note = require('../models/note.model');
const logger = require('../config/logger');
const path = require('path');
const fs = require('fs');
const mongoose = require('mongoose');

/**
 * Create a new note
 * @async
 * @param {Object} req - Express request object
 * @param {Object} res - Express response object
 * @param {Function} next - Express next middleware function
 * @returns {Object} JSON response with created note
 */
exports.createNote = async (req, res, next) => {
  try {
    const { title, content, isPublic, tags } = req.body;

    // Create new note with current user as owner
    const newNote = new Note({
      title,
      content,
      owner: req.user._id,
      isPublic: isPublic || false,
      tags: tags || [],
    });

    // Save note to database
    await newNote.save();

    logger.info(`Note created: ${newNote._id} by user: ${req.user._id}`);

    res.status(201).json({
      success: true,
      message: 'Note created successfully',
      data: newNote,
    });
  } catch (error) {
    next(error);
  }
};

/**
 * Get all notes accessible by the current user
 * @async
 * @param {Object} req - Express request object
 * @param {Object} res - Express response object
 * @param {Function} next - Express next middleware function
 * @returns {Object} JSON response with notes array
 */
exports.getNotes = async (req, res, next) => {
  try {
    const { page = 1, limit = 10, search, tags, sortBy, sortOrder = 'desc', isPublic } = req.query;
    
    // Build query
    const query = {};
    
    // Filter by ownership or public status
    query.$or = [
      { owner: req.user._id },
      { isPublic: true }
    ];
    
    // Filter by public status if specified
    if (isPublic !== undefined) {
      // Override the $or with specific public filter
      delete query.$or;
      query.isPublic = isPublic === 'true';
      
      // If filtering for private notes, only show user's own private notes
      if (isPublic === 'false') {
        query.owner = req.user._id;
      }
    }
    
    // Add search functionality
    if (search) {
      query.$text = { $search: search };
    }
    
    // Filter by tags
    if (tags) {
      const tagArray = tags.split(',').map(tag => tag.trim().toLowerCase());
      query.tags = { $in: tagArray };
    }
    
    // Set up pagination
    const pageNum = parseInt(page);
    const limitNum = parseInt(limit);
    const skip = (pageNum - 1) * limitNum;
    
    // Set up sorting
    const sortOptions = {};
    if (sortBy) {
      sortOptions[sortBy] = sortOrder === 'asc' ? 1 : -1;
    } else {
      sortOptions.createdAt = -1; // Default sort by creation date, newest first
    }
    
    // Execute query with pagination and sorting
    const notes = await Note.find(query)
      .sort(sortOptions)
      .skip(skip)
      .limit(limitNum)
      .populate('owner', 'username');
    
    // Get total count for pagination
    const total = await Note.countDocuments(query);
    
    res.status(200).json({
      success: true,
      data: notes,
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
 * Get a note by ID
 * @async
 * @param {Object} req - Express request object
 * @param {Object} res - Express response object
 * @param {Function} next - Express next middleware function
 * @returns {Object} JSON response with note data
 */
exports.getNoteById = async (req, res, next) => {
  try {
    // Note is already attached to req by the canAccessNote middleware
    const note = req.note;
    
    // Increment view count
    note.viewCount = note.viewCount + 1;
    await note.save();
    
    // Populate note owner details
    await note.populate('owner', 'username');
    
    res.status(200).json({
      success: true,
      data: note,
    });
  } catch (error) {
    next(error);
  }
};

/**
 * Update a note by ID
 * @async
 * @param {Object} req - Express request object
 * @param {Object} res - Express response object
 * @param {Function} next - Express next middleware function
 * @returns {Object} JSON response with updated note
 */
exports.updateNote = async (req, res, next) => {
  try {
    // Note is already attached to req by the isNoteOwnerOrAdmin middleware
    const note = req.note;
    const { title, content, isPublic, tags } = req.body;
    
    // Update fields if they are provided
    if (title !== undefined) note.title = title;
    if (content !== undefined) note.content = content;
    if (isPublic !== undefined) note.isPublic = isPublic;
    if (tags !== undefined) note.tags = tags;
    
    // Save updated note
    await note.save();
    
    logger.info(`Note updated: ${note._id} by user: ${req.user._id}`);
    
    res.status(200).json({
      success: true,
      message: 'Note updated successfully',
      data: note,
    });
  } catch (error) {
    next(error);
  }
};

/**
 * Delete a note by ID
 * @async
 * @param {Object} req - Express request object
 * @param {Object} res - Express response object
 * @param {Function} next - Express next middleware function
 * @returns {Object} JSON response with success message
 */
exports.deleteNote = async (req, res, next) => {
  try {
    // Note is already attached to req by the isNoteOwnerOrAdmin middleware
    const note = req.note;
    
    // Delete file attachments if any
    if (note.attachments && note.attachments.length > 0) {
      for (const attachment of note.attachments) {
        const filePath = path.join(__dirname, '..', attachment.path);
        if (fs.existsSync(filePath)) {
          fs.unlinkSync(filePath);
        }
      }
    }
    
    // Delete the note
    await note.deleteOne();
    
    // Also delete any ratings associated with this note
    const Rating = require('../models/rating.model');
    await Rating.deleteMany({ note: note._id });
    
    logger.info(`Note deleted: ${note._id} by user: ${req.user._id}`);
    
    res.status(200).json({
      success: true,
      message: 'Note deleted successfully',
    });
  } catch (error) {
    next(error);
  }
};

/**
 * Generate or refresh share token for a note
 * @async
 * @param {Object} req - Express request object
 * @param {Object} res - Express response object
 * @param {Function} next - Express next middleware function
 * @returns {Object} JSON response with share token
 */
exports.generateShareToken = async (req, res, next) => {
  try {
    // Note is already attached to req by the isNoteOwnerOrAdmin middleware
    const note = req.note;
    const { expiresInDays } = req.body;
    
    // Generate a new share token
    const { shareToken, expiresAt } = note.generateShareToken(expiresInDays || 7);
    
    // Save the note with new token
    await note.save();
    
    // Create share URL
    const shareUrl = `${req.protocol}://${req.get('host')}/api/notes/${note._id}?token=${shareToken}`;
    
    logger.info(`Share token generated for note: ${note._id} by user: ${req.user._id}`);
    
    res.status(200).json({
      success: true,
      message: 'Share token generated successfully',
      data: {
        shareToken,
        expiresAt,
        shareUrl,
      },
    });
  } catch (error) {
    next(error);
  }
};

/**
 * Revoke share token for a note
 * @async
 * @param {Object} req - Express request object
 * @param {Object} res - Express response object
 * @param {Function} next - Express next middleware function
 * @returns {Object} JSON response with success message
 */
exports.revokeShareToken = async (req, res, next) => {
  try {
    // Note is already attached to req by the isNoteOwnerOrAdmin middleware
    const note = req.note;
    
    // Remove the share token
    note.shareToken = null;
    note.shareTokenExpiresAt = null;
    
    // Save the note
    await note.save();
    
    logger.info(`Share token revoked for note: ${note._id} by user: ${req.user._id}`);
    
    res.status(200).json({
      success: true,
      message: 'Share token revoked successfully',
    });
  } catch (error) {
    next(error);
  }
};

/**
 * Upload file attachments to a note
 * @async
 * @param {Object} req - Express request object
 * @param {Object} res - Express response object
 * @param {Function} next - Express next middleware function
 * @returns {Object} JSON response with updated note including attachments
 */
exports.uploadAttachments = async (req, res, next) => {
  try {
    // Note is already attached to req by the isNoteOwnerOrAdmin middleware
    const note = req.note;
    
    // Check if files are uploaded
    if (!req.files || req.files.length === 0) {
      return res.status(400).json({
        success: false,
        message: 'No files uploaded',
      });
    }
    
    // Process uploaded files
    const attachments = req.files.map(file => ({
      filename: file.filename,
      originalName: file.originalname,
      mimetype: file.mimetype,
      size: file.size,
      path: `public/uploads/${file.filename}`,
    }));
    
    // Add new attachments to the note
    note.attachments = [...note.attachments, ...attachments];
    
    // Save the note
    await note.save();
    
    logger.info(`Attachments added to note: ${note._id} by user: ${req.user._id}`);
    
    res.status(200).json({
      success: true,
      message: 'Files attached successfully',
      data: {
        attachments: note.attachments,
      },
    });
  } catch (error) {
    next(error);
  }
};

/**
 * Delete an attachment from a note
 * @async
 * @param {Object} req - Express request object
 * @param {Object} res - Express response object
 * @param {Function} next - Express next middleware function
 * @returns {Object} JSON response with success message
 */
exports.deleteAttachment = async (req, res, next) => {
  try {
    // Note is already attached to req by the isNoteOwnerOrAdmin middleware
    const note = req.note;
    const { attachmentId } = req.params;
    
    // Find the attachment in the note
    const attachmentIndex = note.attachments.findIndex(
      a => a._id.toString() === attachmentId
    );
    
    if (attachmentIndex === -1) {
      return res.status(404).json({
        success: false,
        message: 'Attachment not found',
      });
    }
    
    // Get the attachment to delete
    const attachment = note.attachments[attachmentIndex];
    
    // Delete the file from the file system
    const filePath = path.join(__dirname, '..', attachment.path);
    if (fs.existsSync(filePath)) {
      fs.unlinkSync(filePath);
    }
    
    // Remove the attachment from the note
    note.attachments.splice(attachmentIndex, 1);
    
    // Save the note
    await note.save();
    
    logger.info(`Attachment deleted from note: ${note._id} by user: ${req.user._id}`);
    
    res.status(200).json({
      success: true,
      message: 'Attachment deleted successfully',
    });
  } catch (error) {
    next(error);
  }
};

/**
 * Get top rated notes
 * @async
 * @param {Object} req - Express request object
 * @param {Object} res - Express response object
 * @param {Function} next - Express next middleware function
 * @returns {Object} JSON response with top rated notes
 */
exports.getTopRatedNotes = async (req, res, next) => {
  try {
    const { limit = 10, minRating = 4 } = req.query;
    
    // Build query for public notes with minimum rating
    const query = {
      isPublic: true,
      averageRating: { $gte: parseFloat(minRating) },
      ratingCount: { $gt: 0 },
    };
    
    // Execute query with limit and sort by rating
    const notes = await Note.find(query)
      .sort({ averageRating: -1, ratingCount: -1 })
      .limit(parseInt(limit))
      .populate('owner', 'username');
    
    res.status(200).json({
      success: true,
      data: notes,
    });
  } catch (error) {
    next(error);
  }
};

/**
 * Search notes by text
 * @async
 * @param {Object} req - Express request object
 * @param {Object} res - Express response object
 * @param {Function} next - Express next middleware function
 * @returns {Object} JSON response with matching notes
 */
exports.searchNotes = async (req, res, next) => {
  try {
    const { q, page = 1, limit = 10 } = req.query;
    
    if (!q) {
      return res.status(400).json({
        success: false,
        message: 'Search query is required',
      });
    }
    
    // Build query - search in public notes or user's own notes
    const query = {
      $text: { $search: q },
      $or: [
        { isPublic: true },
        { owner: req.user._id }
      ]
    };
    
    // Set up pagination
    const pageNum = parseInt(page);
    const limitNum = parseInt(limit);
    const skip = (pageNum - 1) * limitNum;
    
    // Execute query with text score sorting
    const notes = await Note.find(query, { score: { $meta: 'textScore' } })
      .sort({ score: { $meta: 'textScore' } })
      .skip(skip)
      .limit(limitNum)
      .populate('owner', 'username');
    
    // Get total count for pagination
    const total = await Note.countDocuments(query);
    
    res.status(200).json({
      success: true,
      data: notes,
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