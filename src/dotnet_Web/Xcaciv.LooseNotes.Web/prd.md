# Project Requirements Document - Loose Notes Web Application

## 1. Introduction

The Loose Notes Web Application is a multi-user note-taking platform that allows users to create, manage, share, and rate notes. Users can upload attachments to notes, make notes public or private, and share them with others using generated share links.

This document outlines the functional and security requirements of the application, including known vulnerabilities that need to be addressed.

## 2. Requirements Table

| Requirement ID | Description | User Story | Expected Behavior/Outcome |
|----------------|-------------|------------|---------------------------|
| REQ-001 | User Registration | As a visitor, I want to create an account so that I can save my notes and access them later. | Users should be able to register with a username, email, and password. The system should verify that the username is unique. |
| REQ-002 | User Login/Authentication | As a registered user, I want to log in to access my notes. | Users should be able to log in with their username and password. The system should authenticate users and establish a session. |
| REQ-003 | Password Reset | As a user who forgot their password, I want to reset it. | Users should be able to request a password reset using their email. The system should generate a secure token and allow the user to create a new password. |
| REQ-004 | Note Creation | As a logged-in user, I want to create notes with titles and content. | Users should be able to create notes with a title and content. The note should be associated with the user's account. |
| REQ-005 | File Attachment | As a user, I want to attach files to my notes. | Users should be able to upload files to be attached to their notes. The system should securely store these files. |
| REQ-006 | Note Editing | As a note owner, I want to edit my notes. | Users should be able to edit the title, content, and public/private status of their notes. |
| REQ-007 | Note Deletion | As a note owner, I want to delete my notes when I no longer need them. | Users should be able to delete their notes. The system should confirm the action before deletion. |
| REQ-008 | Note Sharing | As a note owner, I want to share my notes with specific people. | Users should be able to generate a share link for a note. Anyone with the link should be able to view the note. |
| REQ-009 | Public/Private Notes | As a note owner, I want to control whether my notes are publicly accessible. | Users should be able to mark notes as public or private. Public notes can be found in search results by any user. |
| REQ-010 | Note Rating | As a user, I want to rate notes to provide feedback. | Users should be able to give a star rating (1-5) and leave a comment on notes. |
| REQ-011 | Rating Management | As a note owner, I want to see ratings for my notes. | Users should be able to view all ratings for their notes. |
| REQ-012 | Note Search | As a user, I want to search for notes by keywords. | Users should be able to search for notes by terms that match title or content. Private notes should only be visible to their owners. |
| REQ-013 | Admin Dashboard | As an administrator, I want to manage users and content. | Admins should have access to a dashboard to view all users and perform administrative actions. |
| REQ-014 | User Profile Management | As a user, I want to edit my profile information. | Users should be able to update their username, email, and password. |
| REQ-015 | Top Rated Notes | As a user, I want to see the highest-rated notes. | Users should be able to view a list of notes sorted by average rating. |
| REQ-016 | Note Ownership Reassignment | As an admin, I want to reassign notes to different users if needed. | Admins should be able to change the owner of a note from one user to another. |

## 3. Security Vulnerabilities

The application contains numerous security vulnerabilities that need to be addressed:

| Vulnerability ID | Description | Location | Impact | Mitigation Required |
|------------------|-------------|----------|--------|---------------------|
| SEC-001 | Plaintext Password Storage | User model | Passwords could be stolen if the database is compromised | Implement password hashing using a strong algorithm (bcrypt, Argon2) |
| SEC-002 | SQL Injection | NoteService (SearchNotes method) | Attackers could execute arbitrary SQL commands | Use parameterized queries or ORM with proper escaping |
| SEC-003 | Cross-Site Scripting (XSS) | Note content rendering (RawContent action) | Attackers could inject and execute malicious scripts | Properly sanitize and encode note content before rendering |
| SEC-004 | Cross-Site Request Forgery (CSRF) | Note editing and deletion | Attackers could trick users into performing unwanted actions | Add CSRF tokens to all state-changing requests |
| SEC-005 | Insecure Direct Object References (IDOR) | Note editing and viewing | Users could access notes they don't own | Add proper authorization checks to all note operations |
| SEC-006 | Path Traversal | File download functionality | Attackers could access files outside the intended directory | Validate and sanitize file paths, use safe file access methods |
| SEC-007 | Command Injection | ExecuteCommand method | Attackers could execute arbitrary system commands | Remove this functionality or implement strict input validation |
| SEC-008 | Insecure File Upload | Note attachment handling | Attackers could upload malicious files | Validate file types, sanitize filenames, use secure storage techniques |
| SEC-009 | Insufficient Session Security | Login/session management | Session hijacking or fixation attacks | Implement secure session management practices |
| SEC-010 | Information Disclosure | Error handling throughout application | Sensitive information could be leaked | Implement proper error handling, hide stack traces |
| SEC-011 | Weak Password Reset | Password reset functionality | Account takeover via weak reset tokens | Implement secure token generation and validation |
| SEC-012 | XML External Entity (XXE) | XML processing | Server-side request forgery and information disclosure | Disable external entity processing in XML parsers |
| SEC-013 | Weak Role-Based Access Control | Admin functionality | Privilege escalation | Implement proper role-based authorization checks |
| SEC-014 | Insecure Logging | Throughout application | Sensitive data leakage | Review and secure logging practices |
| SEC-015 | Missing Input Validation | Throughout application | Various injection attacks | Implement strict input validation for all user inputs |
| SEC-016 | Weak Share Token Generation | Note sharing | Unauthorized access to shared notes | Use cryptographically secure tokens with proper expiration |

## 4. Feature Requirements by User Role

### Regular Users
- Register and log in to the application
- Create, edit, view, and delete personal notes
- Attach files to notes
- Search for notes (owned and public)
- Share notes with others via share links
- Rate and comment on notes
- Mark notes as public or private
- View profile information and edit personal details
- Reset password if forgotten

### Admin Users
- All regular user functionalities
- View all users and their information
- Reassign note ownership between users
- View activity logs
- Execute administrative commands
- Generate reports

## 5. Technical Requirements

- The application is built using ASP.NET Core MVC
- Data should be stored in a relational database (using Entity Framework Core)
- The application should support file uploads and storage
- User interface should be responsive and accessible
- The application should implement proper authentication and authorization
- The application must address all identified security vulnerabilities
- The system should log appropriate information for troubleshooting

## 6. Conclusion

The Loose Notes Web Application provides a feature-rich platform for creating, sharing, and rating notes. However, it contains numerous security vulnerabilities that must be addressed before it can be considered ready for production use. This document serves as a guide for both implementing the required features and addressing the security concerns to create a robust and secure application.