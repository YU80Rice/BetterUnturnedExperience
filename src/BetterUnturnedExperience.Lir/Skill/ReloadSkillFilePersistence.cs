using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace BetterUnturnedExperience.Lir
{
    /// <summary>
    /// DEV-V5-07 等级账的自有文件持久化（票面：由更好的换弹体验按玩家/角色
    /// 持久化——不是 *.bue-settings 偏好文档、不是原版存档/技能数组）。行式
    /// `steamId|characterKey|level`；键侧竖线/换行消毒（角色名理论无竖线，脏
    /// 数据不毁账本格式）。写=临时文件+替换（进程被杀不裂账）；读=缺档=空账、
    /// 坏行整条丢弃并计数进 error（脏账绝不进权威——Restore 侧同样
    /// fail-closed，这里只做语法层）。路径经工厂注入：生产=宿主设置根目录
    /// （Plugin 组合），测试=内存假件（本类不在宿主路径实例化）。
    /// </summary>
    internal sealed class ReloadSkillFilePersistence : IReloadSkillPersistence
    {
        internal const string FileName = "better-inplace-reload.skill-levels.dat";
        internal const char FieldSeparator = '|';
        private const string TempSuffix = ".tmp";
        private const int MaxLineChars = 256;

        private readonly string path;

        internal ReloadSkillFilePersistence(string path)
        {
            this.path = path ?? throw new ArgumentNullException(nameof(path));
        }

        public bool TryLoad(out List<ReloadSkillRecord> records, out string error)
        {
            records = new List<ReloadSkillRecord>();
            error = null;
            try
            {
                if (!File.Exists(path)) return true; // 缺档=新档不送等级（空账本，非错误）
                var skipped = 0;
                foreach (var line in File.ReadLines(path, Encoding.UTF8))
                {
                    if (string.IsNullOrEmpty(line)) continue;
                    var parsed = ParseLine(line);
                    if (parsed.HasValue) records.Add(parsed.Value);
                    else skipped++;
                }
                if (skipped > 0) error = "跳过坏行 " + skipped;
                return true;
            }
            catch (Exception readError)
            {
                error = readError.Message;
                return false; // 读失败=本轮用空账但可继续（调用方记诊断；不炸 Start）
            }
        }

        public bool TrySave(IReadOnlyList<ReloadSkillRecord> records, out string error)
        {
            error = null;
            try
            {
                var directory = Path.GetDirectoryName(path);
                if (!string.IsNullOrEmpty(directory)) Directory.CreateDirectory(directory);
                var builder = new StringBuilder();
                for (var i = 0; i < records.Count; i++)
                {
                    var record = records[i];
                    builder.Append(record.SteamId.ToString(System.Globalization.CultureInfo.InvariantCulture))
                        .Append(FieldSeparator)
                        .Append(Sanitize(record.CharKey))
                        .Append(FieldSeparator)
                        .Append(((int)record.Level).ToString(System.Globalization.CultureInfo.InvariantCulture))
                        .Append('\n');
                }
                var temp = path + TempSuffix;
                File.WriteAllText(temp, builder.ToString(), Encoding.UTF8);
                if (File.Exists(path)) File.Delete(path);
                File.Move(temp, path);
                return true;
            }
            catch (Exception saveError)
            {
                error = saveError.Message;
                return false;
            }
        }

        private static Nullable<ReloadSkillRecord> ParseLine(string line)
        {
            if (line.Length > MaxLineChars) return null;
            var first = line.IndexOf(FieldSeparator);
            var second = first >= 0 ? line.IndexOf(FieldSeparator, first + 1) : -1;
            if (first <= 0 || second <= first + 1 || second == line.Length - 1) return null;
            if (!ulong.TryParse(line.Substring(0, first), System.Globalization.NumberStyles.None,
                    System.Globalization.CultureInfo.InvariantCulture, out var steamId) || steamId == 0UL) return null;
            if (!int.TryParse(line.Substring(second + 1), System.Globalization.NumberStyles.None,
                    System.Globalization.CultureInfo.InvariantCulture, out var level)
                || level < 0 || level > ReloadSkillPolicy.MaxSkillLevel) return null;
            var charKey = line.Substring(first + 1, second - first - 1);
            return new ReloadSkillRecord(steamId, ReloadSkillStore.NormalizeCharKey(charKey), (byte)level);
        }

        private static string Sanitize(string charKey)
        {
            var normalized = ReloadSkillStore.NormalizeCharKey(charKey);
            if (normalized.Length == 0) return normalized;
            var builder = new StringBuilder(normalized.Length);
            for (var i = 0; i < normalized.Length; i++)
            {
                var c = normalized[i];
                builder.Append(c == FieldSeparator || c == '\r' || c == '\n' ? '~' : c);
            }
            return builder.ToString();
        }
    }
}
