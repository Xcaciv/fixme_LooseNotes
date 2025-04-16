/**
 * Note routes
 * @module routes/note
 */

const express = require('express');
const router = express.Router();
const noteController = require('../controllers/note.controller');
const { authenticate, isNoteOwnerOrAdmin, canAccessNote } = require('../middlewares/auth.middleware');
const { validateNoteCreation, validateNoteUpdate } = require('../middlewares/validation.middleware');
const multer = require('multer');
const path = require('path');
const crypto = require('crypto');

// Configure multer storage for file uploads
const storage = multer.diskStorage({
  destination: (req, file, cb) => {
    cb(null, path.join(__dirname, '../public/uploads'));
  },
  filename: (req, file, cb) => {
    // Create a unique filename to prevent clashes
    const uniqueSuffix = `${Date.now()}-${crypto.randomBytes(6).toString('hex')}`;
    const fileExt = path.extname(file.originalname);
    cb(null, `${uniqueSuffix}${fileExt}`);
  }
});

// Configure file filter to restrict file types
const fileFilter = (req, file, cb) => {
  // Define allowed file types
  const allowedFileTypes = /jpeg|jpg|png|gif|pdf|doc|docx|txt|md|mp3|mp4|zip|rar|xls|xlsx|ppt|pptx/;
  const extname = allowedFileTypes.test(path.extname(file.originalname).toLowerCase());
  const mimetype = allowedFileTypes.test(file.mimetype);

  if (extname && mimetype) {
    return cb(null, true);
  } else {
    return cb(new Error('Only certain file types are allowed'), false);
  }
};

// Initialize multer upload middleware
const upload = multer({
  storage,
  fileFilter,
  limits: {
    fileSize: parseInt(process.env.MAX_FILE_SIZE) || 10485760 // Default to 10MB
  }
});

/**
 * @route POST /api/notes
 * @description Create a new note
 * @access Private
 */
router.post('/', authenticate, validateNoteCreation, noteController.createNote);

/**
 * @route GET /api/notes
 * @description Get all accessible notes
 * @access Private
 */
router.get('/', authenticate, noteController.getNotes);

/**
 * @route GET /api/notes/top-rated
 * @description Get top rated notes
 * @access Public
 */
router.get('/top-rated', noteController.getTopRatedNotes);

/**
 * @route GET /api/notes/search
 * @description Search for notes by text
 * @access Private
 */
router.get('/search', authenticate, noteController.searchNotes);

/**
 * @route GET /api/notes/:id
 * @description Get a note by ID
 * @access Private/Public (depending on note visibility)
 */
router.get('/:id', canAccessNote, noteController.getNoteById);

/**
 * @route PUT /api/notes/:id
 * @description Update a note by ID
 * @access Private (owner or admin)
 */
router.put('/:id', authenticate, isNoteOwnerOrAdmin, validateNoteUpdate, noteController.updateNote);

/**
 * @route DELETE /api/notes/:id
 * @description Delete a note by ID
 * @access Private (owner or admin)
 */
router.delete('/:id', authenticate, isNoteOwnerOrAdmin, noteController.deleteNote);

/**
 * @route POST /api/notes/:id/share
 * @description Generate or refresh share token for a note
 * @access Private (owner or admin)
 */
router.post('/:id/share', authenticate, isNoteOwnerOrAdmin, noteController.generateShareToken);

/**
 * @route DELETE /api/notes/:id/share
 * @description Revoke share token for a note
 * @access Private (owner or admin)
 */
router.delete('/:id/share', authenticate, isNoteOwnerOrAdmin, noteController.revokeShareToken);

/**
 * @route POST /api/notes/:id/attachments
 * @description Upload files to a note
 * @access Private (owner or admin)
 */
router.post(
  '/:id/attachments', 
  authenticate, 
  isNoteOwnerOrAdmin, 
  upload.array('files', 5), // Allow up to 5 files
  noteController.uploadAttachments
);

/**
 * @route DELETE /api/notes/:id/attachments/:attachmentId
 * @description Delete an attachment from a note
 * @access Private (owner or admin)
 */
router.delete(
  '/:id/attachments/:attachmentId',
  authenticate,
  isNoteOwnerOrAdmin,
  noteController.deleteAttachment
);

module.exports = router;