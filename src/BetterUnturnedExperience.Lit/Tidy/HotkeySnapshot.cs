using System;
using System.Collections.Generic;
using SDG.Unturned;

// DEV-V2-15: migrated from LaunchInventoryTidy (author: YU80Rice, MIT License,
// Copyright (c) 2026 YU80Rice; local source of truth: Archive/2-未闭环验证项目/LaunchInventoryTidy,
// retired by the wayfinder single-source decision). Attribution:
// docs/third-party/LaunchInventoryTidy-attribution.md. Behavior migrated as-is
// unless a DEV-V2-15 note says otherwise.
namespace BetterUnturnedExperience.Lit
{
    /// <summary>
    /// 快捷键快照：整理前捕获的本地 3-0 数字键绑定。
    /// 旧插件经网络上传服务器验证（DEV-V2-21 联机路径恢复该形态）；
    /// 单人路径（DEV-V2-15）在同一进程内完成捕获 → 验证 → 整理后按新坐标重绑。
    ///
    /// HotkeyInfo（PlayerEquipment.cs:24-42）只含 id/page/x/y，无实例 ID，
    /// 因此"同一 ID 多实例"必须通过旧 (page,x,y) -> 新 (page,x,y) 坐标映射来恢复。
    /// </summary>
    internal struct HotkeySnapshot
    {
        /// <summary>快捷键索引（0..7 对应数字键 3-0）。</summary>
        public byte HotkeyIndex;

        /// <summary>整理前该快捷键指向的物品 ID（用于服务器验证）。</summary>
        public ushort ExpectedItemId;

        /// <summary>整理前该快捷键指向的页码。</summary>
        public byte OldPage;

        /// <summary>整理前该快捷键指向的 X 坐标。</summary>
        public byte OldX;

        /// <summary>整理前该快捷键指向的 Y 坐标。</summary>
        public byte OldY;

        public HotkeySnapshot(byte hotkeyIndex, ushort expectedItemId, byte oldPage, byte oldX, byte oldY)
        {
            HotkeyIndex = hotkeyIndex;
            ExpectedItemId = expectedItemId;
            OldPage = oldPage;
            OldX = oldX;
            OldY = oldY;
        }
    }

    /// <summary>
    /// 快捷键快照工具：客户端捕获本地 _hotkeys，服务器验证旧坐标。
    /// </summary>
    internal static class HotkeySnapshotUtil
    {
        /// <summary>Unturned 数字键快捷键数量固定为 8（3-0 + 4-9 中可绑定的槽位）。</summary>
        public const int HOTKEY_COUNT = 8;

        /// <summary>可整理的页范围（SLOTS=2 至 PANTS=6，不含 STORAGE=7 容器页）。
        /// PlayerInventory.SLOTS/PANTS 是 static readonly 不是 const，这里用硬编码值。</summary>
        public const byte TIDYABLE_PAGE_MIN = 2; // = PlayerInventory.SLOTS
        public const byte TIDYABLE_PAGE_MAX = 6; // = PlayerInventory.PANTS

        /// <summary>
        /// 客户端：捕获本地玩家的 _hotkeys 数组。
        /// 仅 LocalPlayer 的 _hotkeys 已初始化（PlayerEquipment.cs:3290 在 channel.IsLocalPlayer 内），
        /// 服务器端 _hotkeys 为 null。
        /// </summary>
        public static List<HotkeySnapshot> CaptureLocalHotkeys()
        {
            var list = new List<HotkeySnapshot>(HOTKEY_COUNT);
            Player player = Player.LocalPlayer;
            if (player?.equipment == null) return list;

            // PlayerEquipment.hotkeys 是 _hotkeys 的公开属性（PlayerEquipment.cs:238）
            HotkeyInfo[] hotkeys = player.equipment.hotkeys;
            if (hotkeys == null) return list;

            PlayerInventory inv = player.inventory;
            if (inv?.items == null) return list;

            for (byte i = 0; i < hotkeys.Length && i < HOTKEY_COUNT; i++)
            {
                HotkeyInfo info = hotkeys[i];
                if (info == null) continue;
                if (info.id == 0) continue; // 空槽
                if (info.page < TIDYABLE_PAGE_MIN || info.page > TIDYABLE_PAGE_MAX) continue;

                // 验证旧坐标确实存在 ItemJar 且 id 匹配
                Items pageItems = inv.items[info.page];
                if (pageItems == null) continue;
                byte jarIdx = pageItems.getIndex(info.x, info.y);
                if (jarIdx == byte.MaxValue) continue;
                ItemJar jar = pageItems.getItem(jarIdx);
                if (jar?.item == null) continue;
                if (jar.item.id != info.id) continue;

                list.Add(new HotkeySnapshot(i, info.id, info.page, info.x, info.y));
            }
            return list;
        }

        /// <summary>
        /// 服务器端：验证快捷键快照的旧坐标在 sender 的 inventory 中存在 ItemJar 且 id 匹配。
        /// 通过验证的快照条目 + 对应的旧 ItemJar 实例将作为事务映射保存。
        /// </summary>
        public static Dictionary<ItemJar, HotkeySnapshot> ValidateAndResolve(
            PlayerInventory inv, List<HotkeySnapshot> snapshots)
        {
            var resolved = new Dictionary<ItemJar, HotkeySnapshot>();
            if (inv?.items == null || snapshots == null) return resolved;

            var seenIndexes = new HashSet<byte>();
            for (int i = 0; i < snapshots.Count; i++)
            {
                HotkeySnapshot snap = snapshots[i];
                if (snap.HotkeyIndex >= HOTKEY_COUNT) continue;
                if (!seenIndexes.Add(snap.HotkeyIndex)) continue; // 同一索引不得重复
                if (snap.OldPage < TIDYABLE_PAGE_MIN || snap.OldPage > TIDYABLE_PAGE_MAX) continue;

                Items pageItems = inv.items[snap.OldPage];
                if (pageItems == null) continue;
                if (snap.OldX >= pageItems.width || snap.OldY >= pageItems.height) continue;

                byte jarIdx = pageItems.getIndex(snap.OldX, snap.OldY);
                if (jarIdx == byte.MaxValue) continue;
                ItemJar jar = pageItems.getItem(jarIdx);
                if (jar?.item == null) continue;
                if (jar.item.id != snap.ExpectedItemId) continue;

                resolved[jar] = snap;
            }
            return resolved;
        }
    }

    /// <summary>
    /// 单条快捷键恢复条目：整理完成后，原 (oldX, oldY) 的物品已迁移到 (newX, newY)，
    /// 按新坐标调用 ServerBindItemHotkey。
    ///
    /// DEV-V2-15 迁入拆分：原类型在旧插件的 TidyTransaction.cs（联机事务件）里，
    /// 但单人本地恢复链同样需要它；事务管理器（PendingHotkeyRestore/
    /// TidyTransactionManager）随联机路径（DEV-V2-21）迁入，本类型随单人路径先行。
    ///
    /// Round 9 口径保留：服务端在整理前从已解析的真实 ItemJar 取得 trusted
    /// fingerprint，绝不信任快照上传的 quality/state；恢复阶段按完整指纹校验
    /// 目标 ItemJar，避免同 ID 不同实例错位绑定。
    /// </summary>
    internal sealed class HotkeyRestoreEntry
    {
        public byte HotkeyIndex { get; private set; }
        public byte NewPage { get; private set; }
        public byte NewX { get; private set; }
        public byte NewY { get; private set; }
        public ItemFingerprint ExpectedFingerprint { get; private set; }

        public HotkeyRestoreEntry(byte hotkeyIndex, byte newPage, byte newX, byte newY,
            ItemFingerprint expectedFingerprint)
        {
            HotkeyIndex = hotkeyIndex;
            NewPage = newPage;
            NewX = newX;
            NewY = newY;
            // ItemFingerprint 的 State 是数组，重新按值构造，禁止外部可变引用进入恢复条目。
            ExpectedFingerprint = new ItemFingerprint(
                expectedFingerprint.Id,
                expectedFingerprint.Amount,
                expectedFingerprint.Quality,
                expectedFingerprint.State);
        }
    }
}
