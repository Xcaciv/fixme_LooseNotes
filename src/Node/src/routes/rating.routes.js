/**
 * Rating routes
 * @module routes/rating
 */

const express = require('express');
const router = express.Router();
const ratingController = require('../controllers/rating.controller');
const { authenticate, canAccessNote } = require('../middlewares/auth.middleware');
const { validateRatingCreation } = require('../middlewares/validation.middleware');

/**
 * @route POST /api/ratings/notes/:id
 * @description Rate a note
 * @access Private
 */
router.post('/notes/:id', authenticate, validateRatingCreation, ratingController.rateNote);

/**
 * @route GET /api/ratings/notes/:id
 * @description Get all ratings for a note
 * @access Private/Public (depending on note visibility)
 */
router.get('/notes/:id', canAccessNote, ratingController.getNoteRatings);

/**
 * @route GET /api/ratings/:id
 * @description Get a specific rating
 * @access Public
 */
router.get('/:id', ratingController.getRatingById);

/**
 * @route PUT /api/ratings/:id
 * @description Update a rating
 * @access Private (rating owner)
 */
router.put('/:id', authenticate, validateRatingCreation, ratingController.updateRating);

/**
 * @route DELETE /api/ratings/:id
 * @description Delete a rating
 * @access Private (rating owner or admin)
 */
router.delete('/:id', authenticate, ratingController.deleteRating);

/**
 * @route GET /api/ratings/user
 * @description Get all ratings by the current user
 * @access Private
 */
router.get('/user/me', authenticate, ratingController.getUserRatings);

/**
 * @route GET /api/ratings/user/:userId
 * @description Get all ratings by a specific user
 * @access Private (admin)
 */
router.get('/user/:userId', authenticate, ratingController.getUserRatings);

module.exports = router;