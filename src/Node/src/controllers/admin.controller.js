/**
 * Admin controller
 * @module controllers/admin
 */

const User = require('../models/user.model');
const Note = require('../models/note.model');
const Rating = require('../models/rating.model');
const { AppError } = require('../middlewares/error.middleware');

/**
 * Get system statistics
 * @param {Object} req - Express request object
 * @param {Object} res - Express response object
 * @param {Function} next - Express next middleware function
 */
exports.getSystemStats = async (req, res, next) => {
  try {
    const [userCount, noteCount, ratingCount, publicNoteCount] = await Promise.all([
      User.countDocuments(),
      Note.countDocuments(),
      Rating.countDocuments(),
      Note.countDocuments({ isPublic: true })
    ]);

    // Calculate average ratings
    const ratingStats = await Rating.aggregate([
      {
        $group: {
          _id: null,
          averageRating: { $avg: '$value' },
          totalRatings: { $sum: 1 }
        }
      }
    ]);

    // Get most active users (users with most notes)
    const mostActiveUsers = await User.aggregate([
      {
        $lookup: {
          from: 'notes',
          localField: '_id',
          foreignField: 'owner',
          as: 'notes'
        }
      },
      {
        $project: {
          _id: 1,
          email: 1,
          name: 1,
          noteCount: { $size: '$notes' }
        }
      },
      { $sort: { noteCount: -1 } },
      { $limit: 5 }
    ]);

    // Get most popular tags
    const popularTags = await Note.aggregate([
      { $unwind: '$tags' },
      {
        $group: {
          _id: '$tags',
          count: { $sum: 1 }
        }
      },
      { $sort: { count: -1 } },
      { $limit: 10 }
    ]);

    res.status(200).json({
      status: 'success',
      data: {
        users: {
          total: userCount
        },
        notes: {
          total: noteCount,
          public: publicNoteCount,
          private: noteCount - publicNoteCount
        },
        ratings: {
          total: ratingCount,
          average: ratingStats.length > 0 ? ratingStats[0].averageRating : 0
        },
        mostActiveUsers,
        popularTags: popularTags.map(tag => ({ name: tag._id, count: tag.count }))
      }
    });
  } catch (err) {
    next(new AppError(`Error fetching system statistics: ${err.message}`, 500));
  }
};

/**
 * Get all users
 * @param {Object} req - Express request object
 * @param {Object} res - Express response object
 * @param {Function} next - Express next middleware function
 */
exports.getAllUsers = async (req, res, next) => {
  try {
    const page = parseInt(req.query.page, 10) || 1;
    const limit = parseInt(req.query.limit, 10) || 10;
    const skip = (page - 1) * limit;

    // Set up filters
    const filter = {};

    // Name search
    if (req.query.name) {
      filter.name = { $regex: req.query.name, $options: 'i' };
    }

    // Email search
    if (req.query.email) {
      filter.email = { $regex: req.query.email, $options: 'i' };
    }

    // Role filter
    if (req.query.role) {
      filter.role = req.query.role;
    }

    // Status filter
    if (req.query.active !== undefined) {
      filter.active = req.query.active === 'true';
    }

    // Execute query
    const users = await User.find(filter)
      .select('-password -passwordChangedAt -passwordResetToken -passwordResetExpires')
      .skip(skip)
      .limit(limit)
      .sort(req.query.sort || '-createdAt');

    // Count total results
    const total = await User.countDocuments(filter);

    res.status(200).json({
      status: 'success',
      results: users.length,
      pagination: {
        total,
        page,
        limit,
        pages: Math.ceil(total / limit)
      },
      data: { users }
    });
  } catch (err) {
    next(new AppError(`Error fetching users: ${err.message}`, 500));
  }
};

/**
 * Get user by ID
 * @param {Object} req - Express request object
 * @param {Object} res - Express response object
 * @param {Function} next - Express next middleware function
 */
exports.getUserById = async (req, res, next) => {
  try {
    const user = await User.findById(req.params.id).select('-password -passwordChangedAt -passwordResetToken -passwordResetExpires');
    
    if (!user) {
      return next(new AppError('User not found', 404));
    }

    res.status(200).json({
      status: 'success',
      data: { user }
    });
  } catch (err) {
    next(new AppError(`Error fetching user: ${err.message}`, 500));
  }
};

/**
 * Update user
 * @param {Object} req - Express request object
 * @param {Object} res - Express response object
 * @param {Function} next - Express next middleware function
 */
exports.updateUser = async (req, res, next) => {
  try {
    // Disallow password updates through this route
    if (req.body.password || req.body.passwordConfirm) {
      return next(new AppError('This route is not for password updates', 400));
    }

    // Only allow specific fields to be updated
    const allowedFields = ['name', 'email', 'role', 'active'];
    const filteredBody = {};
    Object.keys(req.body).forEach(key => {
      if (allowedFields.includes(key)) {
        filteredBody[key] = req.body[key];
      }
    });

    const updatedUser = await User.findByIdAndUpdate(req.params.id, filteredBody, {
      new: true,
      runValidators: true
    }).select('-password -passwordChangedAt -passwordResetToken -passwordResetExpires');

    if (!updatedUser) {
      return next(new AppError('User not found', 404));
    }

    res.status(200).json({
      status: 'success',
      data: { user: updatedUser }
    });
  } catch (err) {
    next(new AppError(`Error updating user: ${err.message}`, 500));
  }
};

/**
 * Delete user
 * @param {Object} req - Express request object
 * @param {Object} res - Express response object
 * @param {Function} next - Express next middleware function
 */
exports.deleteUser = async (req, res, next) => {
  try {
    const user = await User.findByIdAndDelete(req.params.id);

    if (!user) {
      return next(new AppError('User not found', 404));
    }

    // Delete all user's notes and their associated ratings
    const userNotes = await Note.find({ owner: req.params.id });
    const noteIds = userNotes.map(note => note._id);

    await Promise.all([
      Note.deleteMany({ owner: req.params.id }),
      Rating.deleteMany({ 
        $or: [
          { user: req.params.id }, 
          { note: { $in: noteIds } }
        ] 
      })
    ]);

    res.status(204).json({
      status: 'success',
      data: null
    });
  } catch (err) {
    next(new AppError(`Error deleting user: ${err.message}`, 500));
  }
};

/**
 * Get user activity
 * @param {Object} req - Express request object
 * @param {Object} res - Express response object
 * @param {Function} next - Express next middleware function
 */
exports.getUserActivity = async (req, res, next) => {
  try {
    const userId = req.params.id;

    // Check if user exists
    const user = await User.findById(userId);
    if (!user) {
      return next(new AppError('User not found', 404));
    }

    // Get user's notes
    const notes = await Note.find({ owner: userId })
      .select('title createdAt lastUpdatedAt isPublic views')
      .sort('-createdAt');

    // Get user's ratings given
    const ratingsGiven = await Rating.find({ user: userId })
      .select('value note createdAt')
      .populate('note', 'title');

    // Get ratings received on user's notes
    const userNoteIds = notes.map(note => note._id);
    const ratingsReceived = await Rating.find({ note: { $in: userNoteIds } })
      .select('value note user createdAt')
      .populate('note', 'title')
      .populate('user', 'name email');

    // Calculate total views across all notes
    const totalViews = notes.reduce((sum, note) => sum + note.views, 0);

    res.status(200).json({
      status: 'success',
      data: {
        user: {
          id: user._id,
          name: user.name,
          email: user.email
        },
        activity: {
          totalNotes: notes.length,
          totalPublicNotes: notes.filter(note => note.isPublic).length,
          totalPrivateNotes: notes.filter(note => !note.isPublic).length,
          totalViews,
          ratingsGiven: ratingsGiven.length,
          ratingsReceived: ratingsReceived.length,
          averageRatingReceived: ratingsReceived.length > 0 
            ? ratingsReceived.reduce((sum, rating) => sum + rating.value, 0) / ratingsReceived.length 
            : 0,
          lastActive: user.lastActive
        },
        notes,
        ratingsGiven,
        ratingsReceived
      }
    });
  } catch (err) {
    next(new AppError(`Error fetching user activity: ${err.message}`, 500));
  }
};

/**
 * Get system health check
 * @param {Object} req - Express request object
 * @param {Object} res - Express response object
 * @param {Function} next - Express next middleware function
 */
exports.getHealthCheck = async (req, res, next) => {
  try {
    // Check database connection
    const dbStatus = mongoose.connection.readyState === 1 ? 'connected' : 'disconnected';
    
    // Basic process stats
    const uptime = process.uptime();
    const memoryUsage = process.memoryUsage();
    
    res.status(200).json({
      status: 'success',
      data: {
        service: 'Loose Notes API',
        version: process.env.API_VERSION || 'v1',
        timestamp: new Date(),
        uptime: `${Math.floor(uptime / 3600)}h ${Math.floor((uptime % 3600) / 60)}m ${Math.floor(uptime % 60)}s`,
        database: {
          status: dbStatus
        },
        system: {
          memory: {
            rss: `${Math.round(memoryUsage.rss / 1024 / 1024 * 100) / 100} MB`,
            heapTotal: `${Math.round(memoryUsage.heapTotal / 1024 / 1024 * 100) / 100} MB`,
            heapUsed: `${Math.round(memoryUsage.heapUsed / 1024 / 1024 * 100) / 100} MB`
          }
        }
      }
    });
  } catch (err) {
    next(new AppError(`Health check error: ${err.message}`, 500));
  }
};