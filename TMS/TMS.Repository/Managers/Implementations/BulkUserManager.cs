using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TMS.Common;
using TMS.Models.Account;
using TMS.Models.Masters;
using TMS.Repository.Repositories;
using TMS.ViewModels;

namespace TMS.Repository.Managers.Implementations
{
    /// <summary>
    /// Bulk user creation manager — independent of course enrollment.
    /// Creates users of any role (Admin, Faculty, Student) with secure password handling.
    /// Supports modes: CreateOnly, SkipExisting, UpdateExisting.
    /// </summary>
    internal class BulkUserManager : IBulkUserManager
    {
        private readonly IBaseModelRepository<UserMaster> _userRepository;
        private readonly IBaseModelRepository<RoleMaster> _roleRepository;

        public BulkUserManager(
            IBaseModelRepository<UserMaster> userRepository,
            IBaseModelRepository<RoleMaster> roleRepository)
        {
            _userRepository = userRepository;
            _roleRepository = roleRepository;
        }

        /// <summary>
        /// Preview/validate rows without saving. Returns each row with status.
        /// </summary>
        public async Task<List<BulkUserUploadRow>> ValidateRowsAsync(List<BulkUserUploadRow> rows)
        {
            var allRoles = await _roleRepository.GetAsync(null).ToListAsync();
            var roleMap = allRoles
                .Where(r => !string.IsNullOrWhiteSpace(r.Name))
                .ToDictionary(r => r.Name!.Trim().ToLower(), r => r);

            var seenEmails = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            int rowNum = 0;

            foreach (var row in rows)
            {
                rowNum++;
                row.RowNumber = rowNum;

                // Required fields
                if (string.IsNullOrWhiteSpace(row.Name))
                {
                    row.Status = "Error"; row.Message = "Name is required."; continue;
                }
                if (string.IsNullOrWhiteSpace(row.Email))
                {
                    row.Status = "Error"; row.Message = "Email is required."; continue;
                }
                if (string.IsNullOrWhiteSpace(row.Role))
                {
                    row.Status = "Error"; row.Message = "Role is required."; continue;
                }

                var email = row.Email.Trim();
                if (!email.Contains("@") || !email.Contains("."))
                {
                    row.Status = "Error"; row.Message = "Invalid email format."; continue;
                }

                // Duplicate in file
                if (!seenEmails.Add(email))
                {
                    row.Status = "Duplicate"; row.Message = "Duplicate email in file."; continue;
                }

                // Role validation
                if (!roleMap.ContainsKey(row.Role.Trim().ToLower()))
                {
                    row.Status = "Error";
                    row.Message = $"Invalid role '{row.Role}'. Valid: {string.Join(", ", roleMap.Keys)}.";
                    continue;
                }

                // Check DB
                var exists = await _userRepository.GetAsync(null)
                    .Where(u => u.Email == email).AnyAsync();

                if (exists)
                {
                    row.Status = "Exists"; row.Message = "User already exists in database.";
                }
                else
                {
                    row.Status = "Valid"; row.Message = "Ready to create.";
                }
            }

            return rows;
        }

        /// <summary>
        /// Process upload with mode support.
        /// </summary>
        public async Task<BulkUserUploadResult> ProcessBulkUploadAsync(
            List<BulkUserUploadRow> rows, int createdByUserId, BulkUploadMode mode = BulkUploadMode.SkipExisting)
        {
            var result = new BulkUserUploadResult();

            // 1) Cache all roles
            var allRoles = await _roleRepository.GetAsync(null).ToListAsync();
            var roleMap = allRoles
                .Where(r => !string.IsNullOrWhiteSpace(r.Name))
                .ToDictionary(r => r.Name!.Trim().ToLower(), r => r);

            // 2) Track in-batch duplicates
            var seenEmails = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            // 3) Default password — securely hashed
            const string defaultPassword = "LMS@2026";
            string hashedPassword = SecurityUtility.HashPassword(defaultPassword);

            foreach (var row in rows)
            {
                result.TotalProcessed++;
                int rowNum = result.TotalProcessed;

                try
                {
                    // ── Validate required fields ──
                    if (string.IsNullOrWhiteSpace(row.Email))
                    {
                        result.FailedCount++;
                        result.Errors.Add($"Row {rowNum}: Email is required.");
                        continue;
                    }
                    if (string.IsNullOrWhiteSpace(row.Name))
                    {
                        result.FailedCount++;
                        result.Errors.Add($"Row {rowNum}: Name is required.");
                        continue;
                    }
                    if (string.IsNullOrWhiteSpace(row.Role))
                    {
                        result.FailedCount++;
                        result.Errors.Add($"Row {rowNum}: Role is required.");
                        continue;
                    }

                    string emailTrimmed = row.Email.Trim();
                    string roleTrimmed = row.Role.Trim().ToLower();

                    if (!emailTrimmed.Contains("@") || !emailTrimmed.Contains("."))
                    {
                        result.FailedCount++;
                        result.Errors.Add($"Row {rowNum}: Invalid email format '{emailTrimmed}'.");
                        continue;
                    }

                    if (!seenEmails.Add(emailTrimmed))
                    {
                        result.SkippedCount++;
                        result.Warnings.Add($"Row {rowNum}: Duplicate email '{emailTrimmed}' in file — skipped.");
                        continue;
                    }

                    if (!roleMap.TryGetValue(roleTrimmed, out var role))
                    {
                        result.FailedCount++;
                        result.Errors.Add($"Row {rowNum}: Invalid role '{row.Role}'. Valid roles: {string.Join(", ", roleMap.Keys)}.");
                        continue;
                    }

                    // ── Check if user already exists ──
                    var existingUser = await _userRepository.GetAsync(null)
                        .Where(u => u.Email == emailTrimmed)
                        .FirstOrDefaultAsync();

                    if (existingUser != null)
                    {
                        switch (mode)
                        {
                            case BulkUploadMode.CreateOnly:
                                result.FailedCount++;
                                result.Errors.Add($"Row {rowNum}: User '{emailTrimmed}' already exists (Create Only mode).");
                                break;

                            case BulkUploadMode.SkipExisting:
                                result.SkippedCount++;
                                result.Warnings.Add($"Row {rowNum}: User '{emailTrimmed}' already exists — skipped.");
                                break;

                            case BulkUploadMode.UpdateExisting:
                                existingUser.Name = row.Name.Trim();
                                existingUser.ContactNo = int.TryParse(row.Phone?.Trim(), out int ph) ? ph : existingUser.ContactNo;
                                existingUser.RoleId = role.Id;
                                existingUser.UpdatedOn = DateTime.Now;
                                existingUser.UpdatedBy = createdByUserId;
                                await _userRepository.UpdateAsync(existingUser);
                                await _userRepository.SaveChangesAsync();
                                result.UpdatedCount++;
                                break;
                        }
                        continue;
                    }

                    // ── Create new user ──
                    var newUser = new UserMaster
                    {
                        Name = row.Name.Trim(),
                        Email = emailTrimmed,
                        ContactNo = int.TryParse(row.Phone?.Trim(), out int phone) ? phone : null,
                        RoleId = role.Id,
                        Password = hashedPassword,
                        ForcePasswordChange = true,
                        IsActive = true,
                        CreatedOn = DateTime.Now,
                        CreatedBy = createdByUserId
                    };

                    await _userRepository.AddAsync(newUser);
                    await _userRepository.SaveChangesAsync();
                    result.CreatedCount++;
                }
                catch (Exception ex)
                {
                    result.FailedCount++;
                    result.Errors.Add($"Row {rowNum}: {ex.Message}");
                }
            }

            return result;
        }
    }
}
