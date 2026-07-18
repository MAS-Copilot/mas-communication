// =============================================================================
// MAS.Communication
// https://www.mas-automation.com/
//
// Copyright 2026 MAS (厦门威光) Corporation
//
// Licensed under the Apache License, Version 2.0
// See LICENSE file in the project root for full license information.
// =============================================================================

namespace MAS.CommunicationUnitTest.WebSocketProtocol.Helpers;

/// <summary>
/// 测试用的条件等待辅助方法
/// </summary>
internal static class TestWait {
    /// <summary>
    /// 轮询等待条件成立；超时返回条件的最终状态
    /// </summary>
    public static async Task<bool> UntilAsync(Func<bool> condition, int timeoutMs = 5000, int pollMs = 20) {
        long deadline = Environment.TickCount64 + timeoutMs;
        while (Environment.TickCount64 < deadline) {
            if (condition()) {
                return true;
            }

            await Task.Delay(pollMs);
        }

        return condition();
    }
}
