using PainForGlory_Common.DTOs;

namespace PainForGlory_Common.Helpers
{
    public static class AccountHistoryFormatter
    {
        public static List<FormattedAccountHistory> BuildFormattedHistoryList(
            List<PreviousAccountInfo> historyList,
            string? currentUsername,
            string? currentEmail)
        {
            var ordered = historyList.OrderBy(h => h.ChangedAt).ToList();
            var result = new List<FormattedAccountHistory>();

            string? lastUsername = null;
            string? lastEmail = null;

            // Collect username changes
            var usernameChanges = ordered
                .Where(h => h.OldUsername != null)
                .Select(h => new { h.ChangedAt, Old = h.OldUsername! })
                .ToList();

            if (!string.IsNullOrEmpty(currentUsername))
            {
                usernameChanges.Add(new { ChangedAt = DateTime.UtcNow, Old = currentUsername });
            }

            for (int i = 0; i < usernameChanges.Count - 1; i++)
            {
                result.Add(new FormattedAccountHistory
                {
                    ChangedAt = usernameChanges[i].ChangedAt,
                    FormattedText = $"Date: {usernameChanges[i].ChangedAt.ToLocalTime():g} | Previous Username: {usernameChanges[i].Old} | New Username: {usernameChanges[i + 1].Old}"
                });
            }

            // Collect email changes
            var emailChanges = ordered
                .Where(h => h.OldEmail != null)
                .Select(h => new { h.ChangedAt, Old = h.OldEmail! })
                .ToList();

            if (!string.IsNullOrEmpty(currentEmail))
            {
                emailChanges.Add(new { ChangedAt = DateTime.UtcNow, Old = currentEmail });
            }

            for (int i = 0; i < emailChanges.Count - 1; i++)
            {
                result.Add(new FormattedAccountHistory
                {
                    ChangedAt = emailChanges[i].ChangedAt,
                    FormattedText = $"Date: {emailChanges[i].ChangedAt.ToLocalTime():g} | Previous Email: {emailChanges[i].Old} | New Email: {emailChanges[i + 1].Old}"
                });
            }

            return result.OrderByDescending(e => e.ChangedAt).ToList();
        }
    }
}
