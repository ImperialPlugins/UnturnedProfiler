﻿#region Copyright
/*
 *  Unturned Profiler - A plugin for profiling Unturned servers and analyzing lag causes
 *  Copyright (C) 2017-2019 Enes Sadık Özbek <esozbek.me>
 *
 *  This program is free software: you can redistribute it and/or modify
 *  it under the terms of the GNU Affero General Public License as
 *  published by the Free Software Foundation, either version 3 of the
 *  License, or (at your option) any later version.
 *
 *  This program is distributed in the hope that it will be useful,
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 *  GNU Affero General Public License for more details.
 *
 *  You should have received a copy of the GNU Affero General Public License
 *  along with this program.  If not, see <http://www.gnu.org/licenses/>.
 */
#endregion

using System;
using System.Threading;
using UnityEngine;

namespace ImperialPlugins.UnturnedProfiler.Watchdog
{
    public class UnityWatchdogComponent : MonoBehaviour
    {
        public TimeSpan Timeout { get; set; }
        private ThreadWatchdog m_Watchdog;

        public void Awake()
        {
            m_Watchdog = new ThreadWatchdog(Thread.CurrentThread, Timeout);
            m_Watchdog.Start();
        }

        public void OnDestroy()
        {
            m_Watchdog?.Dispose();
            m_Watchdog = null;
        }

        public void Update()
        {
            m_Watchdog.NotifyAlive();
        }
    }
}