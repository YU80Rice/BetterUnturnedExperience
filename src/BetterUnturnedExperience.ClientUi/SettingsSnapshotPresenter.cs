using System;
using System.Collections.Generic;
using BetterUnturnedExperience.Contracts;

namespace BetterUnturnedExperience.ClientUi.Internal
{
    internal sealed class SettingsSnapshotPresenter
    {
        internal IReadOnlyList<SettingEntryView> GetVisibleEntries(FeatureSettingsSnapshot snapshot)
        {
            var visible = new List<SettingEntryView>();
            if (snapshot.Entries == null) return visible.AsReadOnly();
            for (var index = 0; index < snapshot.Entries.Count; index++)
            {
                var entry = snapshot.Entries[index];
                if (entry.IsVisible) visible.Add(entry);
            }
            return visible.AsReadOnly();
        }
    }
}
